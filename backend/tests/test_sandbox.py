"""Tests for the sandbox system."""
import pytest

from app.sandbox.models import (
    CodeExecutionRequest,
    CodeExecutionResult,
    ExecutionStatus,
    SkillInfo,
    SkillRegistrationRequest,
    SkillSchema,
    SkillType,
)


def test_skill_creation():
    """Test basic skill object creation."""
    skill = SkillSchema(
        name="test_skill",
        description="A test skill",
        skill_type=SkillType.FUNCTION,
        code="def test_skill(): return 'hello'",
        tags=["test"],
    )

    assert skill.name == "test_skill"
    assert skill.skill_type == SkillType.FUNCTION
    assert skill.verified is False
    assert "test" in skill.tags


def test_skill_types():
    """Test that all skill types are defined."""
    assert SkillType.FUNCTION == "function"
    assert SkillType.TOOL == "tool"
    assert SkillType.AGENT == "agent"


def test_execution_status():
    """Test execution status enum."""
    assert ExecutionStatus.SUCCESS == "success"
    assert ExecutionStatus.FAILED == "failed"
    assert ExecutionStatus.TIMEOUT == "timeout"


def test_code_execution_request():
    """Test code execution request creation."""
    request = CodeExecutionRequest(
        code="print('hello')",
        timeout=10,
        imports=["json"],
    )

    assert request.code == "print('hello')"
    assert request.timeout == 10
    assert "json" in request.imports


def test_skill_registration_request():
    """Test skill registration request creation."""
    request = SkillRegistrationRequest(
        name="my_skill",
        description="Does something useful",
        skill_type=SkillType.TOOL,
        code="def my_skill(): pass",
        tags=["utility"],
    )

    assert request.name == "my_skill"
    assert request.skill_type == SkillType.TOOL
    assert request.tags == ["utility"]


def test_skill_info():
    """Test skill info model."""
    from uuid import uuid4

    info = SkillInfo(
        id=uuid4(),
        name="test",
        description="Test skill",
        skill_type=SkillType.FUNCTION,
        tags=["test"],
        verified=True,
        usage_count=5,
    )

    assert info.name == "test"
    assert info.verified is True
    assert info.usage_count == 5
