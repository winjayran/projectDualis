"""Sandbox API endpoints for Project Dualis."""
from fastapi import APIRouter, Depends, HTTPException

from app.sandbox.executor import DockerSandboxExecutor, get_executor
from app.sandbox.models import (
    CodeExecutionRequest,
    CodeExecutionResult,
    SkillExecutionRequest,
    SkillInfo,
    SkillRegistrationRequest,
    SkillRegistrationResult,
    SkillType,
)
from app.sandbox.registry import SkillRegistry, get_skill_registry

router = APIRouter(prefix="/api/v1/sandbox", tags=["sandbox"])


@router.post("/execute", response_model=CodeExecutionResult)
async def execute_code(
    request: CodeExecutionRequest,
    executor: DockerSandboxExecutor = Depends(get_executor),
) -> CodeExecutionResult:
    """Execute Python code in a secure Docker sandbox.

    The code runs with:
    - Network disabled
    - Memory limit: 256MB
    - Timeout: 30 seconds (configurable)
    - Read-only filesystem (except /tmp)
    - Whitelisted imports only
    """
    if not executor.test_connection():
        raise HTTPException(
            status_code=503,
            detail="Docker is not available. Please ensure Docker is running.",
        )

    return await executor.execute(request)


@router.post("/skills/register", response_model=SkillRegistrationResult)
async def register_skill(
    request: SkillRegistrationRequest,
    registry: SkillRegistry = Depends(get_skill_registry),
) -> SkillRegistrationResult:
    """Register a new skill after verification.

    The skill will be tested in the sandbox before being registered.
    Verified skills can be used by the AI for function calling.
    """
    return await registry.register(request)


@router.post("/skills/execute")
async def execute_skill(
    request: SkillExecutionRequest,
    registry: SkillRegistry = Depends(get_skill_registry),
):
    """Execute a registered skill.

    Returns the result of the skill execution.
    """
    try:
        result = await registry.execute(request)
        return {"success": True, "result": result}
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except RuntimeError as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/skills", response_model=list[SkillInfo])
async def list_skills(
    skill_type: SkillType | None = None,
    verified_only: bool = False,
    registry: SkillRegistry = Depends(get_skill_registry),
) -> list[SkillInfo]:
    """List all registered skills.

    Can filter by skill type and verification status.
    """
    return registry.list_skills(skill_type=skill_type, verified_only=verified_only)


@router.get("/skills/schemas")
async def get_skill_schemas(
    registry: SkillRegistry = Depends(get_skill_registry),
):
    """Get all skill schemas in OpenAI function format.

    This endpoint provides the schemas needed for LLM function calling.
    """
    return {
        "tools": registry.get_all_schemas(),
        "count": len(registry.get_all_schemas()),
    }


@router.delete("/skills/{skill_name}")
async def delete_skill(
    skill_name: str,
    registry: SkillRegistry = Depends(get_skill_registry),
):
    """Delete a skill from the registry."""
    if registry.remove_skill(skill_name):
        return {"success": True, "message": f"Skill '{skill_name}' deleted"}
    raise HTTPException(status_code=404, detail=f"Skill '{skill_name}' not found")


@router.get("/health")
async def sandbox_health(
    executor: DockerSandboxExecutor = Depends(get_executor),
):
    """Check if the sandbox (Docker) is available."""
    docker_available = executor.test_connection()

    return {
        "status": "healthy" if docker_available else "degraded",
        "docker_available": docker_available,
    }


__all__ = ["router"]
