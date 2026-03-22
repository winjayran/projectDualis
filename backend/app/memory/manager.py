"""Memory manager with non-linear chained retrieval."""
from datetime import datetime, timedelta, timezone
from typing import Any
from uuid import UUID

from loguru import logger

from app.memory.embeddings import get_embedding_service
from app.memory.storage import QdrantMemoryStore, get_memory_store
from app.models.memory import (
    EmotionType,
    Memory,
    MemoryChain,
    MemoryImportance,
    MemoryMetadata,
    MemoryQuery,
    MemorySearchResult,
    MemoryStoreResult,
    MemoryType,
)


class MemoryManager:
    """Manager for hierarchical memory with non-linear chained retrieval."""

    def __init__(
        self,
        store: QdrantMemoryStore | None = None,
    ) -> None:
        """Initialize the memory manager.

        Args:
            store: QdrantMemoryStore instance (uses global if None)
        """
        self.store = store or get_memory_store()
        self.embedding_service = get_embedding_service()

    async def store_memory(
        self,
        content: str,
        memory_type: MemoryType,
        importance: MemoryImportance = MemoryImportance.MEDIUM,
        emotion: EmotionType | None = None,
        user_id: str | None = None,
        session_id: str | None = None,
        tags: list[str] | None = None,
        linked_memories: list[UUID] | None = None,
        ttl_seconds: int | None = None,
    ) -> MemoryStoreResult:
        """Store a new memory with automatic embedding.

        Args:
            content: Memory content text
            memory_type: Type of memory
            importance: Importance level for retention
            emotion: Associated emotion (for episodic memories)
            user_id: Optional user identifier
            session_id: Optional session identifier
            tags: Optional tags for categorization
            linked_memories: IDs of related memories
            ttl_seconds: Time-to-live for working context

        Returns:
            MemoryStoreResult with memory ID
        """
        # Create metadata
        created_at = datetime.now(timezone.utc)
        expires_at = None
        if ttl_seconds:
            expires_at = created_at + timedelta(seconds=ttl_seconds)

        metadata = MemoryMetadata(
            importance=importance,
            emotion=emotion,
            created_at=created_at,
            expires_at=expires_at,
            tags=tags or [],
            linked_memories=linked_memories or [],
            user_id=user_id,
            session_id=session_id,
        )

        memory = Memory(
            content=content,
            memory_type=memory_type,
            metadata=metadata,
        )

        return await self.store.store(memory)

    async def search(self, query: MemoryQuery) -> MemorySearchResult:
        """Search memories with optional chained retrieval.

        Args:
            query: Search query with filters and chain options

        Returns:
            MemorySearchResult with memories and chains
        """
        # Generate query embedding
        query_embedding = await self.embedding_service.aembed(query.query)

        # Determine which memory types to search
        memory_types = query.memory_types or list(MemoryType)

        all_memories: list[Memory] = []
        all_chains: list[MemoryChain] = []

        for memory_type in memory_types:
            # Retrieve similar memories
            results = await self.store.retrieve(
                query_embedding=query_embedding,
                memory_type=memory_type,
                limit=query.limit,
                filters={
                    "user_id": query.user_id,
                    "session_id": query.session_id,
                    "min_importance": query.min_importance.value if query.min_importance else None,
                },
            )

            # Convert to Memory objects
            memories = self._results_to_memories(results, memory_type)
            all_memories.extend(memories)

            # Perform chained retrieval if depth > 1
            if query.chain_depth > 1:
                chains = await self._chain_retrieve(
                    memories, query.chain_depth, query
                )
                all_chains.extend(chains)

        # Update access for retrieved memories
        for memory in all_memories:
            await self.store.update_access(memory.id, memory.memory_type)

        return MemorySearchResult(
            query=query.query,
            memories=all_memories,
            chains=all_chains,
            total_found=len(all_memories),
        )

    async def _chain_retrieve(
        self,
        seed_memories: list[Memory],
        depth: int,
        query: MemoryQuery,
    ) -> list[MemoryChain]:
        """Perform non-linear chained retrieval from seed memories.

        This implements the core non-linear memory traversal:
        1. Start with seed memories from initial search
        2. Follow their linked_memories to find related memories
        3. Optionally continue to linked memories of linked memories
        4. Score chains based on relevance

        Args:
            seed_memories: Initial memories from search
            depth: How many levels deep to traverse
            query: Original query for context

        Returns:
            List of MemoryChains with their relevance scores
        """
        chains: list[MemoryChain] = []

        for seed in seed_memories:
            chain = MemoryChain(memories=[seed])
            visited = {seed.id}

            # Traverse links up to depth
            current_level = [seed]
            for _ in range(depth - 1):
                next_level: list[Memory] = []

                for memory in current_level:
                    # Get linked memory IDs
                    for link_id in memory.metadata.linked_memories:
                        if link_id in visited:
                            continue

                        # Retrieve the linked memory
                        link_type = self._infer_memory_type(link_id)
                        if not link_type:
                            continue

                        result = await self.store.get_by_id(link_id, link_type)
                        if result:
                            linked = self._payload_to_memory(result["payload"], link_id)
                            if linked:
                                next_level.append(linked)
                                visited.add(link_id)

                if next_level:
                    chain.memories.extend(next_level)
                    current_level = next_level
                else:
                    break

            # Score the chain
            chain.score = self._score_chain(chain, query.query)
            if chain.score > 0.3:  # Minimum relevance threshold
                chains.append(chain)

        # Sort by score
        chains.sort(key=lambda c: c.score, reverse=True)
        return chains[:10]  # Top 10 chains

    def _infer_memory_type(self, memory_id: UUID) -> MemoryType | None:
        """Try to infer memory type from ID or other heuristics.

        In production, would use a separate index or metadata store.
        For now, tries all collections.
        """
        # Simple approach: try each collection
        # In production, maintain a separate ID->type mapping
        return None  # Placeholder - requires implementation

    def _score_chain(self, chain: MemoryChain, query: str) -> float:
        """Score a memory chain's relevance to the query.

        Args:
            chain: The chain to score
            query: Original query text

        Returns:
            Relevance score between 0 and 1
        """
        if not chain.memories:
            return 0.0

        # Base score from importance and recency
        scores = []
        now = datetime.now(timezone.utc)

        for memory in chain.memories:
            # Importance contribution
            imp_score = memory.metadata.importance.value / 5.0

            # Recency contribution (memories accessed recently score higher)
            days_since_access = (now - memory.metadata.last_accessed).days
            recency_score = max(0.1, 1.0 - (days_since_access / 365.0))

            # Access frequency
            access_score = min(1.0, memory.metadata.access_count / 10.0)

            scores.append((imp_score + recency_score + access_score) / 3.0)

        return sum(scores) / len(scores)

    def _results_to_memories(
        self,
        results: list[dict[str, Any]],
        memory_type: MemoryType,
    ) -> list[Memory]:
        """Convert Qdrant results to Memory objects."""
        memories = []
        for result in results:
            payload = result["payload"]
            memory = self._payload_to_memory(payload, result["id"], memory_type)
            if memory:
                memories.append(memory)
        return memories

    def _payload_to_memory(
        self,
        payload: dict[str, Any],
        memory_id: UUID | str,
        memory_type: MemoryType | None = None,
    ) -> Memory | None:
        """Convert Qdrant payload to Memory object."""
        try:
            return Memory(
                id=UUID(memory_id) if isinstance(memory_id, str) else memory_id,
                content=payload.get("content", ""),
                memory_type=MemoryType(payload.get("memory_type", "semantic_fact")),
                metadata=MemoryMetadata(
                    importance=MemoryImportance(payload.get("importance", 3)),
                    emotion=EmotionType(payload["emotion"]) if payload.get("emotion") else None,
                    access_count=payload.get("access_count", 0),
                    last_accessed=datetime.fromisoformat(payload.get("last_accessed", datetime.now(timezone.utc).isoformat())),
                    created_at=datetime.fromisoformat(payload.get("created_at", datetime.now(timezone.utc).isoformat())),
                    expires_at=datetime.fromisoformat(payload["expires_at"]) if payload.get("expires_at") else None,
                    tags=payload.get("tags", []),
                    linked_memories=[UUID(mid) for mid in payload.get("linked_memories", [])],
                    user_id=payload.get("user_id"),
                    session_id=payload.get("session_id"),
                ),
                summary=payload.get("summary"),
            )
        except Exception as e:
            logger.error(f"Failed to convert payload to memory: {e}")
            return None

    async def consolidate(
        self,
        session_id: str,
        user_id: str | None = None,
    ) -> int:
        """Consolidate working context memories into episodic/semantic.

        Args:
            session_id: Session to consolidate
            user_id: Optional user ID

        Returns:
            Number of memories consolidated
        """
        # Query working context for the session
        query = MemoryQuery(
            query=f"Session {session_id} summary",
            memory_types=[MemoryType.WORKING_CONTEXT],
            session_id=session_id,
            user_id=user_id,
            limit=50,
        )

        result = await self.search(query)
        consolidated = 0

        for memory in result.memories:
            # Move important working memories to episodic
            if memory.metadata.importance >= MemoryImportance.HIGH:
                await self.store_memory(
                    content=memory.content,
                    memory_type=MemoryType.EPISODIC_EVENT,
                    importance=memory.metadata.importance,
                    emotion=memory.metadata.emotion,
                    user_id=memory.metadata.user_id,
                    tags=memory.metadata.tags + ["consolidated"],
                    linked_memories=memory.metadata.linked_memories,
                )

                # Delete from working context
                await self.store.delete(memory.id, MemoryType.WORKING_CONTEXT)
                consolidated += 1

        return consolidated

    async def forget(
        self,
        user_id: str | None = None,
        days_threshold: int = 90,
        max_importance: MemoryImportance = MemoryImportance.LOW,
    ) -> int:
        """Forget old, low-importance memories.

        Args:
            user_id: User ID filter
            days_threshold: Age in days
            max_importance: Maximum importance level to forget

        Returns:
            Number of memories forgotten
        """
        # This is a simplified implementation
        # In production, would scan all collections and apply criteria
        logger.info(f"Forget: user={user_id}, days={days_threshold}, max_imp={max_importance}")
        return 0  # Placeholder


# Global memory manager instance
_memory_manager: MemoryManager | None = None


def get_memory_manager() -> MemoryManager:
    """Get or create the global memory manager instance."""
    global _memory_manager
    if _memory_manager is None:
        _memory_manager = MemoryManager()
    return _memory_manager


__all__ = ["MemoryManager", "get_memory_manager"]
