"""Sandbox models for code execution and skill management."""
from datetime import datetime, timezone
from enum import Enum
from typing import Any
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class ExecutionStatus(str, Enum):
    """Status of code execution in sandbox."""

    PENDING = "pending"
    RUNNING = "running"
    SUCCESS = "success"
    FAILED = "failed"
    TIMEOUT = "timeout"
    ERROR = "error"


class SkillType(str, Enum):
    """Types of skills that can be registered."""

    FUNCTION = "function"  # Single function call
    TOOL = "tool"  # Multi-use tool with persistent state
    AGENT = "agent"  # Autonomous agent behavior


class CodeExecutionRequest(BaseModel):
    """Request to execute code in the sandbox."""

    code: str = Field(..., description="Python code to execute")
    timeout: int = Field(default=30, ge=1, le=300, description="Timeout in seconds")
    imports: list[str] = Field(
        default_factory=list,
        description="Required imports (whitelist check)",
    )
    session_id: str | None = None


class CodeExecutionResult(BaseModel):
    """Result of code execution."""

    status: ExecutionStatus
    output: str = ""
    error: str = ""
    return_value: Any = None
    execution_time: float = 0.0
    stdout: str = ""
    stderr: str = ""


class SkillSchema(BaseModel):
    """Schema for a registered skill."""

    id: UUID = Field(default_factory=uuid4)
    name: str = Field(..., description="Unique skill name")
    description: str = Field(..., description="What this skill does")
    skill_type: SkillType
    code: str = Field(..., description="Python implementation")
    parameters: dict[str, Any] = Field(
        default_factory=dict,
        description="JSON Schema for parameters",
    )
    created_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    last_used: datetime | None = None
    usage_count: int = 0
    verified: bool = Field(default=False, description="Passed safety checks")
    tags: list[str] = Field(default_factory=list)


class SkillRegistrationRequest(BaseModel):
    """Request to register a new skill."""

    name: str
    description: str
    skill_type: SkillType
    code: str
    parameters: dict[str, Any] | None = None
    tags: list[str] = Field(default_factory=list)


class SkillRegistrationResult(BaseModel):
    """Result of skill registration."""

    success: bool
    skill_id: UUID | None = None
    message: str
    warnings: list[str] = Field(default_factory=list)


class SkillExecutionRequest(BaseModel):
    """Request to execute a registered skill."""

    skill_name: str
    parameters: dict[str, Any] = Field(default_factory=dict)


class SkillInfo(BaseModel):
    """Information about a skill for listing."""

    id: UUID
    name: str
    description: str
    skill_type: SkillType
    tags: list[str]
    verified: bool
    usage_count: int


__all__ = [
    "ExecutionStatus",
    "SkillType",
    "CodeExecutionRequest",
    "CodeExecutionResult",
    "SkillSchema",
    "SkillRegistrationRequest",
    "SkillRegistrationResult",
    "SkillExecutionRequest",
    "SkillInfo",
]
