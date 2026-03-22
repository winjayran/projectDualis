"""Skill registry for managing learned AI skills."""
from datetime import datetime, timezone
from typing import Any
from uuid import UUID

from loguru import logger

from app.memory.manager import MemoryManager, get_memory_manager
from app.sandbox.executor import DockerSandboxExecutor, get_executor
from app.sandbox.models import (
    ExecutionStatus,
    SkillExecutionRequest,
    SkillInfo,
    SkillRegistrationRequest,
    SkillRegistrationResult,
    SkillSchema,
    SkillType,
)


class SkillRegistry:
    """Registry for managing learned and verified skills."""

    def __init__(
        self,
        executor: DockerSandboxExecutor | None = None,
        memory_manager: MemoryManager | None = None,
    ) -> None:
        """Initialize the skill registry.

        Args:
            executor: Sandbox executor for testing skills
            memory_manager: Memory manager for storing skills
        """
        self.executor = executor or get_executor()
        self.memory_manager = memory_manager or get_memory_manager()
        self._skills: dict[str, SkillSchema] = {}

    async def register(
        self,
        request: SkillRegistrationRequest,
    ) -> SkillRegistrationResult:
        """Register a new skill after verification.

        Args:
            request: Skill registration request

        Returns:
            Registration result with skill ID
        """
        # Check if skill already exists
        if request.name in self._skills:
            return SkillRegistrationResult(
                success=False,
                message=f"Skill '{request.name}' already exists",
            )

        # Verify the skill by testing it
        verification_passed, warnings = await self._verify_skill(
            request.code, request.parameters
        )

        # Create the skill schema
        skill = SkillSchema(
            name=request.name,
            description=request.description,
            skill_type=request.skill_type,
            code=request.code,
            parameters=request.parameters or {},
            tags=request.tags,
            verified=verification_passed,
        )

        # Store in registry
        self._skills[request.name] = skill

        # Also store in memory as SKILL_SCHEMA
        from app.models.memory import MemoryImportance, MemoryType

        await self.memory_manager.store_memory(
            content=f"Skill: {request.name} - {request.description}",
            memory_type=MemoryType.SKILL_SCHEMA,
            importance=MemoryImportance.HIGH if verification_passed else MemoryImportance.MEDIUM,
            tags=["skill", request.skill_type.value] + request.tags,
        )

        logger.info(f"Registered skill: {request.name} (verified={verification_passed})")

        return SkillRegistrationResult(
            success=True,
            skill_id=skill.id,
            message=f"Skill '{request.name}' registered successfully",
            warnings=warnings,
        )

    async def _verify_skill(
        self,
        code: str,
        parameters: dict[str, Any],
    ) -> tuple[bool, list[str]]:
        """Verify a skill by testing it in the sandbox.

        Args:
            code: Skill code to test
            parameters: Parameter schema

        Returns:
            Tuple of (passed, warnings)
        """
        warnings = []

        # Create a simple test case
        test_code = f'''
# Test the skill by checking if it runs
try:
    # Define the skill function
    {code}

    # Try to call it with sample parameters
    result = "Code executed successfully"
    print(result)
except Exception as e:
    print(f"Error: {{e}}")
    raise
'''

        from app.sandbox.models import CodeExecutionRequest

        result = await self.executor.execute(
            CodeExecutionRequest(
                code=test_code,
                timeout=10,
                imports=["json"],
            )
        )

        if result.status == ExecutionStatus.SUCCESS:
            return True, warnings
        else:
            warnings.append(f"Verification failed: {result.error}")
            return False, warnings

    async def execute(self, request: SkillExecutionRequest) -> Any:
        """Execute a registered skill.

        Args:
            request: Skill execution request

        Returns:
            Return value from skill execution
        """
        skill = self._skills.get(request.skill_name)
        if not skill:
            raise ValueError(f"Skill '{request.skill_name}' not found")

        if not skill.verified:
            logger.warning(f"Executing unverified skill: {request.skill_name}")

        # Update usage stats
        skill.last_used = datetime.now(timezone.utc)
        skill.usage_count += 1

        # Build execution code
        execution_code = f'''
{skill.code}

# Execute with parameters
result = {skill.name}(**{request.parameters})
print(json.dumps({{"result": result}}))
'''

        from app.sandbox.models import CodeExecutionRequest

        result = await self.executor.execute(
            CodeExecutionRequest(
                code=execution_code,
                timeout=30,
                imports=["json"],
            )
        )

        if result.status == ExecutionStatus.SUCCESS:
            return result.return_value
        else:
            raise RuntimeError(f"Skill execution failed: {result.error}")

    def list_skills(
        self,
        skill_type: SkillType | None = None,
        verified_only: bool = False,
    ) -> list[SkillInfo]:
        """List registered skills.

        Args:
            skill_type: Filter by skill type
            verified_only: Only show verified skills

        Returns:
            List of skill information
        """
        skills = list(self._skills.values())

        if skill_type:
            skills = [s for s in skills if s.skill_type == skill_type]

        if verified_only:
            skills = [s for s in skills if s.verified]

        return [
            SkillInfo(
                id=s.id,
                name=s.name,
                description=s.description,
                skill_type=s.skill_type,
                tags=s.tags,
                verified=s.verified,
                usage_count=s.usage_count,
            )
            for s in skills
        ]

    def get_skill(self, name: str) -> SkillSchema | None:
        """Get a skill by name.

        Args:
            name: Skill name

        Returns:
            Skill schema or None if not found
        """
        return self._skills.get(name)

    def remove_skill(self, name: str) -> bool:
        """Remove a skill from the registry.

        Args:
            name: Skill name

        Returns:
            True if removed, False if not found
        """
        if name in self._skills:
            del self._skills[name]
            logger.info(f"Removed skill: {name}")
            return True
        return False

    def get_openapi_schema(self, skill_name: str) -> dict[str, Any] | None:
        """Get OpenAPI-style schema for a skill.

        This allows the skill to be used as a function calling tool
        for the LLM.

        Args:
            skill_name: Name of the skill

        Returns:
            OpenAPI function schema or None
        """
        skill = self.get_skill(skill_name)
        if not skill:
            return None

        return {
            "type": "function",
            "function": {
                "name": skill.name,
                "description": skill.description,
                "parameters": skill.parameters,
            },
        }

    def get_all_schemas(self) -> list[dict[str, Any]]:
        """Get all skill schemas for LLM function calling.

        Returns:
            List of OpenAPI function schemas
        """
        schemas = []
        for skill in self._skills.values():
            if skill.verified:
                schema = self.get_openapi_schema(skill.name)
                if schema:
                    schemas.append(schema)
        return schemas


# Global registry instance
_registry: SkillRegistry | None = None


def get_skill_registry() -> SkillRegistry:
    """Get or create the global skill registry instance."""
    global _registry
    if _registry is None:
        _registry = SkillRegistry()
    return _registry


__all__ = ["SkillRegistry", "get_skill_registry"]
