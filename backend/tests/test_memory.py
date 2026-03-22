"""Tests for the memory system."""
import pytest

from app.models.memory import (
    Memory,
    MemoryImportance,
    MemoryMetadata,
    MemoryType,
)


def test_memory_creation():
    """Test basic memory object creation."""
    metadata = MemoryMetadata(
        importance=MemoryImportance.HIGH,
        tags=["test", "example"],
    )
    memory = Memory(
        content="Test memory content",
        memory_type=MemoryType.EPISODIC_EVENT,
        metadata=metadata,
    )

    assert memory.content == "Test memory content"
    assert memory.memory_type == MemoryType.EPISODIC_EVENT
    assert memory.metadata.importance == MemoryImportance.HIGH
    assert "test" in memory.metadata.tags


def test_memory_importance_ordering():
    """Test that memory importance levels are ordered correctly."""
    assert MemoryImportance.CRITICAL > MemoryImportance.HIGH
    assert MemoryImportance.HIGH > MemoryImportance.MEDIUM
    assert MemoryImportance.MEDIUM > MemoryImportance.LOW
    assert MemoryImportance.LOW > MemoryImportance.TRIVIAL


def test_memory_types():
    """Test that all memory types are defined."""
    assert MemoryType.WORKING_CONTEXT
    assert MemoryType.EPISODIC_EVENT
    assert MemoryType.SEMANTIC_FACT
    assert MemoryType.SKILL_SCHEMA
