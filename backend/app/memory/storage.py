"""Qdrant-based memory storage with hierarchical collections."""
from datetime import datetime, timezone
from typing import Any
from uuid import UUID

from loguru import logger
from qdrant_client import QdrantClient
from qdrant_client.models import (
    Distance,
    FieldCondition,
    MatchValue,
    PointStruct,
    Range,
    Filter,
    VectorParams,
)

from app.core.config import settings
from app.memory.embeddings import get_embedding_service
from app.models.memory import (
    Memory,
    MemoryMetadata,
    MemoryStoreResult,
    MemoryType,
)


class QdrantMemoryStore:
    """Qdrant-based storage for hierarchical memory system."""

    def __init__(
        self,
        host: str | None = None,
        port: int | None = None,
        http_port: int | None = None,
    ) -> None:
        """Initialize the Qdrant client and create collections.

        Args:
            host: Qdrant host address
            port: Qdrant gRPC port
            http_port: Qdrant HTTP port
        """
        self.host = host or settings.qdrant_host
        self.port = port or settings.qdrant_port
        self.http_port = http_port or settings.qdrant_http_port
        self.prefix = settings.qdrant_collection_prefix
        self._degraded_mode = False

        # Connect via HTTP
        self.client = QdrantClient(
            host=self.host,
            port=self.http_port,
            prefer_grpc=False,
        )
        self.embedding_service = get_embedding_service()

        # Initialize collections for each memory type
        self._initialize_collections()

    def _get_collection_name(self, memory_type: MemoryType) -> str:
        """Get the Qdrant collection name for a memory type."""
        return f"{self.prefix}_{memory_type.value}"

    def _initialize_collections(self) -> None:
        """Create Qdrant collections for each memory type if they don't exist."""
        # Test connection first
        try:
            self.client.get_collections()
        except Exception as e:
            self._degraded_mode = True
            logger.warning(f"Qdrant connection failed: {e}")
            logger.warning("Memory system starting in degraded mode - memories will not be persisted")
            return

        vector_size = self.embedding_service.dimension

        for memory_type in MemoryType:
            collection_name = self._get_collection_name(memory_type)

            try:
                # Check if collection exists
                collections = self.client.get_collections().collections
                collection_names = {c.name for c in collections}

                if collection_name not in collection_names:
                    logger.info(f"Creating collection: {collection_name}")
                    self.client.create_collection(
                        collection_name=collection_name,
                        vectors_config=VectorParams(
                            size=vector_size,
                            distance=Distance.COSINE,
                        ),
                    )
            except Exception as e:
                logger.warning(f"Could not initialize collection {collection_name}: {e}")
                logger.warning("Memory system will be in degraded mode until Qdrant is available")
                self._degraded_mode = True

    async def store(self, memory: Memory) -> MemoryStoreResult:
        """Store a memory in the appropriate collection.

        Args:
            memory: The memory to store

        Returns:
            MemoryStoreResult with success status and memory ID
        """
        if self._degraded_mode:
            return MemoryStoreResult(
                success=False,
                message="Qdrant connection unavailable - memory system in degraded mode",
            )

        try:
            # Generate embedding if not present
            if memory.embedding is None:
                memory.embedding = await self.embedding_service.aembed(
                    memory.content
                )

            collection_name = self._get_collection_name(memory.memory_type)

            # Prepare payload
            payload = {
                "content": memory.content,
                "memory_type": memory.memory_type.value,
                "importance": memory.metadata.importance.value,
                "emotion": memory.metadata.emotion.value if memory.metadata.emotion else None,
                "created_at": memory.metadata.created_at.isoformat(),
                "last_accessed": memory.metadata.last_accessed.isoformat(),
                "access_count": memory.metadata.access_count,
                "expires_at": memory.metadata.expires_at.isoformat()
                if memory.metadata.expires_at
                else None,
                "tags": memory.metadata.tags,
                "linked_memories": [str(uuid) for uuid in memory.metadata.linked_memories],
                "user_id": memory.metadata.user_id,
                "session_id": memory.metadata.session_id,
                "summary": memory.summary,
            }

            # Insert/Update the point
            self.client.upsert(
                collection_name=collection_name,
                points=[
                    PointStruct(
                        id=str(memory.id),
                        vector=memory.embedding,
                        payload=payload,
                    )
                ],
            )

            logger.debug(f"Stored memory {memory.id} in {collection_name}")
            return MemoryStoreResult(success=True, memory_id=memory.id)

        except Exception as e:
            logger.error(f"Failed to store memory: {e}")
            return MemoryStoreResult(
                success=False, message=f"Storage failed: {e}"
            )

    async def retrieve(
        self,
        query_embedding: list[float],
        memory_type: MemoryType,
        limit: int = 10,
        score_threshold: float = 0.5,
        filters: dict[str, Any] | None = None,
    ) -> list[dict[str, Any]]:
        """Retrieve memories by similarity.

        Args:
            query_embedding: Query vector
            memory_type: Type of memory to search
            limit: Maximum results
            score_threshold: Minimum similarity score
            filters: Optional filters (user_id, session_id, etc.)

        Returns:
            List of memory records with payloads and scores
        """
        collection_name = self._get_collection_name(memory_type)

        # Build filter conditions
        filter_conditions = None
        if filters:
            conditions = []
            if "user_id" in filters and filters["user_id"]:
                conditions.append(
                    FieldCondition(
                        key="user_id", match=MatchValue(value=filters["user_id"])
                    )
                )
            if "session_id" in filters and filters["session_id"]:
                conditions.append(
                    FieldCondition(
                        key="session_id", match=MatchValue(value=filters["session_id"])
                    )
                )
            if "min_importance" in filters:
                conditions.append(
                    FieldCondition(
                        key="importance",
                        range=Range(gte=filters["min_importance"]),
                    )
                )
            if conditions:
                filter_conditions = Filter(must=conditions)

        try:
            results = self.client.search(
                collection_name=collection_name,
                query_vector=query_embedding,
                limit=limit,
                score_threshold=score_threshold,
                query_filter=filter_conditions,
                with_payload=True,
            )

            return [
                {
                    "id": result.id,
                    "score": result.score,
                    "payload": result.payload,
                }
                for result in results
            ]

        except Exception as e:
            logger.error(f"Retrieval failed: {e}")
            return []

    async def get_by_id(self, memory_id: UUID | str, memory_type: MemoryType) -> dict[str, Any] | None:
        """Get a specific memory by ID.

        Args:
            memory_id: UUID of the memory
            memory_type: Type of the memory

        Returns:
            Memory record or None if not found
        """
        collection_name = self._get_collection_name(memory_type)

        try:
            results = self.client.retrieve(
                collection_name=collection_name,
                ids=[str(memory_id)],
                with_payload=True,
                with_vectors=False,
            )

            if results:
                return {
                    "id": results[0].id,
                    "payload": results[0].payload,
                }
            return None

        except Exception as e:
            logger.error(f"Failed to get memory {memory_id}: {e}")
            return None

    async def update_access(self, memory_id: UUID | str, memory_type: MemoryType) -> bool:
        """Update last_accessed timestamp and increment access_count.

        Args:
            memory_id: UUID of the memory
            memory_type: Type of the memory

        Returns:
            True if successful, False otherwise
        """
        collection_name = self._get_collection_name(memory_type)

        try:
            # Get current payload to preserve access_count
            result = await self.get_by_id(memory_id, memory_type)
            if not result:
                return False

            payload = result["payload"]
            access_count = payload.get("access_count", 0) + 1

            # Update payload
            self.client.set_payload(
                collection_name=collection_name,
                payload={
                    "last_accessed": datetime.now(timezone.utc).isoformat(),
                    "access_count": access_count,
                },
                points=[str(memory_id)],
            )
            return True

        except Exception as e:
            logger.error(f"Failed to update access for {memory_id}: {e}")
            return False

    async def delete(self, memory_id: UUID | str, memory_type: MemoryType) -> bool:
        """Delete a memory.

        Args:
            memory_id: UUID of the memory
            memory_type: Type of the memory

        Returns:
            True if successful, False otherwise
        """
        collection_name = self._get_collection_name(memory_type)

        try:
            self.client.delete(
                collection_name=collection_name,
                points_selector=[str(memory_id)],
            )
            return True

        except Exception as e:
            logger.error(f"Failed to delete memory {memory_id}: {e}")
            return False

    async def cleanup_expired(self) -> int:
        """Remove expired working context memories.

        Returns:
            Number of memories removed
        """
        collection_name = self._get_collection_name(MemoryType.WORKING_CONTEXT)
        now = datetime.now(timezone.utc).isoformat()

        try:
            # Scan for expired memories
            results = self.client.scroll(
                collection_name=collection_name,
                limit=1000,
                with_payload=True,
            )[0]

            expired_ids = []
            for point in results:
                expires_at = point.payload.get("expires_at")
                if expires_at and expires_at < now:
                    expired_ids.append(point.id)

            if expired_ids:
                self.client.delete(
                    collection_name=collection_name,
                    points_selector=expired_ids,
                )

            return len(expired_ids)

        except Exception as e:
            logger.error(f"Cleanup failed: {e}")
            return 0

    def get_collection_info(self, memory_type: MemoryType) -> dict[str, Any]:
        """Get information about a collection.

        Args:
            memory_type: Type of memory

        Returns:
            Collection info
        """
        collection_name = self._get_collection_name(memory_type)

        try:
            info = self.client.get_collection(collection_name)
            return {
                "name": collection_name,
                "points_count": info.points_count,
                "vector_size": info.config.params.vectors.size,
            }
        except Exception as e:
            logger.error(f"Failed to get collection info: {e}")
            return {"name": collection_name, "error": str(e)}

    def test_connection(self) -> bool:
        """Test if Qdrant connection is available.

        Returns:
            True if connection is successful, False otherwise
        """
        try:
            self.client.get_collections()
            return True
        except Exception:
            return False

    @property
    def degraded_mode(self) -> bool:
        """Check if memory store is in degraded mode.

        Returns:
            True if Qdrant is unavailable, False otherwise
        """
        return self._degraded_mode


# Global memory store instance
_memory_store: QdrantMemoryStore | None = None


def get_memory_store() -> QdrantMemoryStore:
    """Get or create the global memory store instance."""
    global _memory_store
    if _memory_store is None:
        _memory_store = QdrantMemoryStore()
    return _memory_store


__all__ = ["QdrantMemoryStore", "get_memory_store"]
