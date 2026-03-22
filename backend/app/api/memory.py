"""Memory API endpoints for Project Dualis."""
from typing import Any
from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException, Query
from pydantic import BaseModel, Field

from app.memory.manager import MemoryManager, get_memory_manager
from app.models.memory import (
    MemoryImportance,
    MemoryQuery,
    MemorySearchResult,
    MemoryStoreResult,
    MemoryType,
)

router = APIRouter(prefix="/api/v1/memory", tags=["memory"])


class StoreMemoryRequest(BaseModel):
    """Request to store a new memory."""

    content: str = Field(..., min_length=1, description="Memory content")
    memory_type: MemoryType = Field(..., description="Type of memory")
    importance: MemoryImportance = Field(
        default=MemoryImportance.MEDIUM,
        description="Importance for retention",
    )
    user_id: str | None = Field(None, description="User identifier")
    session_id: str | None = Field(None, description="Session identifier")
    tags: list[str] = Field(default_factory=list, description="Tags for categorization")
    ttl_seconds: int | None = Field(
        None,
        description="Time-to-live for working context (seconds)",
    )


@router.post("", response_model=MemoryStoreResult)
async def store_memory(
    request: StoreMemoryRequest,
    manager: MemoryManager = Depends(get_memory_manager),
) -> MemoryStoreResult:
    """Store a new memory in the hierarchical system.

    The memory will be embedded and stored in the appropriate Qdrant collection
    based on its type.
    """
    try:
        return await manager.store_memory(
            content=request.content,
            memory_type=request.memory_type,
            importance=request.importance,
            user_id=request.user_id,
            session_id=request.session_id,
            tags=request.tags,
            ttl_seconds=request.ttl_seconds,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/search", response_model=MemorySearchResult)
async def search_memory(
    query: str = Query(..., description="Search query"),
    memory_types: list[MemoryType] | None = Query(None, description="Filter by memory type"),
    limit: int = Query(10, ge=1, le=100, description="Maximum results"),
    chain_depth: int = Query(1, ge=1, le=5, description="Chain retrieval depth"),
    user_id: str | None = Query(None, description="Filter by user"),
    session_id: str | None = Query(None, description="Filter by session"),
    manager: MemoryManager = Depends(get_memory_manager),
) -> MemorySearchResult:
    """Search memories with non-linear chained retrieval.

    Performs semantic search across memory types and follows associative
    links between memories for deeper context retrieval.
    """
    try:
        memory_query = MemoryQuery(
            query=query,
            memory_types=memory_types,
            limit=limit,
            chain_depth=chain_depth,
            user_id=user_id,
            session_id=session_id,
        )
        return await manager.search(memory_query)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/consolidate")
async def consolidate_memory(
    session_id: str = Query(..., description="Session to consolidate"),
    user_id: str | None = Query(None, description="User identifier"),
    manager: MemoryManager = Depends(get_memory_manager),
) -> dict[str, Any]:
    """Consolidate working context memories into episodic/semantic.

    Moves important session memories from temporary working context
    to long-term episodic or semantic storage.
    """
    try:
        count = await manager.consolidate(session_id, user_id)
        return {
            "success": True,
            "consolidated_count": count,
            "message": f"Consolidated {count} memories",
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/types", response_model=list[str])
async def list_memory_types() -> list[str]:
    """List available memory types."""
    return [t.value for t in MemoryType]


@router.get("/stats")
async def get_memory_stats(
    manager: MemoryManager = Depends(get_memory_manager),
) -> dict[str, Any]:
    """Get statistics about stored memories."""
    stats = {}
    for memory_type in MemoryType:
        info = manager.store.get_collection_info(memory_type)
        stats[memory_type.value] = {
            "points_count": info.get("points_count", 0),
            "vector_size": info.get("vector_size", 0),
        }
    return stats


__all__ = ["router"]
