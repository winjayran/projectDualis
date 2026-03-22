"""Docker-based sandbox executor for safe code execution."""
import json
from pathlib import Path

from docker import from_env
from docker.models.containers import Container

from app.core.config import settings
from app.sandbox.models import (
    CodeExecutionRequest,
    CodeExecutionResult,
    ExecutionStatus,
)

# Allowed imports for security (whitelist approach)
ALLOWED_IMPORTS = {
    # Standard library - safe modules
    "datetime",
    "json",
    "re",
    "math",
    "random",
    "statistics",
    "collections",
    "itertools",
    "functools",
    "typing",
    "dataclasses",
    "enum",
    "decimal",
    "fractions",
    "hashlib",
    "base64",
    "uuid",
    "time",
    "os",
    "pathlib",
    # Third-party - data science
    "numpy",
    "pandas",
    # Add more as needed
}

# Blocked operations (keyword blacklist)
BLOCKED_PATTERNS = [
    "__import__",
    "eval(",
    "exec(",
    "compile(",
    "open(",
    "file(",
    "input(",
    "importlib",
    "subprocess",
    "sys.",
    "globals(",
    "locals(",
    "vars(",
    "getattr(",
    "setattr(",
    "delattr(",
    "__",
]


class DockerSandboxExecutor:
    """Executor that runs Python code in isolated Docker containers."""

    def __init__(self) -> None:
        """Initialize the Docker client."""
        self.client = from_env()
        self.image = settings.sandbox_docker_image
        self.timeout = settings.sandbox_timeout

    def _validate_code(self, code: str, imports: list[str]) -> tuple[bool, list[str]]:
        """Validate code for security issues.

        Args:
            code: Python code to validate
            imports: List of imports the code requests

        Returns:
            Tuple of (is_valid, list_of_errors)
        """
        errors = []

        # Check imports against whitelist
        for imp in imports:
            base_module = imp.split(".")[0]
            if base_module not in ALLOWED_IMPORTS:
                errors.append(f"Import '{imp}' is not allowed")

        # Check for blocked patterns
        for pattern in BLOCKED_PATTERNS:
            if pattern in code:
                errors.append(f"Blocked pattern detected: {pattern}")

        # Check for common escape attempts
        suspicious = [
            "class Monkey",
            "object.__",
            "type.__",
            "__class__",
        ]
        for s in suspicious:
            if s in code:
                errors.append(f"Suspicious pattern detected: {s}")

        return len(errors) == 0, errors

    async def execute(self, request: CodeExecutionRequest) -> CodeExecutionResult:
        """Execute code in a Docker container.

        Args:
            request: Execution request with code and parameters

        Returns:
            CodeExecutionResult with status and output
        """
        import time

        # Validate code first
        is_valid, errors = self._validate_code(request.code, request.imports)
        if not is_valid:
            return CodeExecutionResult(
                status=ExecutionStatus.FAILED,
                error="Validation failed: " + "; ".join(errors),
            )

        # Prepare the execution script
        wrapper_script = self._create_wrapper(request.code, request.imports)

        start_time = time.time()

        try:
            # Create and run container
            container: Container = self.client.containers.run(
                self.image,
                command=["python", "-c", wrapper_script],
                detach=True,
                network_disabled=True,  # No network access
                mem_limit="256m",  # Memory limit
                cpu_quota=50000,  # CPU limit (50% of one core)
                pids_limit=50,  # Process limit
                read_only=True,  # Read-only filesystem
                # Temporary filesystem for writes
                tmpfs={"/tmp": "size=100m"},
            )

            # Wait for completion with timeout
            result = container.wait(timeout=request.timeout or self.timeout)

            # Get logs
            logs = container.logs(stdout=True, stderr=True).decode("utf-8")

            # Clean up
            container.remove(force=True)

            execution_time = time.time() - start_time

            # Parse result
            if result["StatusCode"] == 0:
                # Try to parse JSON output
                try:
                    output = json.loads(logs)
                    return CodeExecutionResult(
                        status=ExecutionStatus.SUCCESS,
                        output=output.get("output", ""),
                        return_value=output.get("return_value"),
                        stdout=output.get("stdout", ""),
                        stderr=output.get("stderr", ""),
                        execution_time=execution_time,
                    )
                except json.JSONDecodeError:
                    return CodeExecutionResult(
                        status=ExecutionStatus.SUCCESS,
                        output=logs,
                        execution_time=execution_time,
                    )
            else:
                return CodeExecutionResult(
                    status=ExecutionStatus.FAILED,
                    error=logs,
                    execution_time=execution_time,
                )

        except Exception as e:
            execution_time = time.time() - start_time
            return CodeExecutionResult(
                status=ExecutionStatus.ERROR,
                error=str(e),
                execution_time=execution_time,
            )

    def _create_wrapper(self, code: str, imports: list[str]) -> str:
        """Create a wrapper script for safe execution.

        Args:
            code: User code to wrap
            imports: List of imports to include

        Returns:
            Complete Python script as string
        """
        # Build imports
        import_lines = [f"import {imp}" for imp in imports]

        # Wrap in try-except and capture output
        wrapper = f'''
import sys
import json
import traceback

{chr(10).join(import_lines)}

# Redirect stdout/stderr
class Capturer:
    def __init__(self):
        self.outputs = []

    def write(self, data):
        self.outputs.append(data)

    def flush(self):
        pass

    def get_output(self):
        return "".join(self.outputs)

stdout_capture = Capturer()
stderr_capture = Capturer()
sys.stdout = stdout_capture
sys.stderr = stderr_capture

# Execute user code
return_value = None
try:
{chr(10).join("    " + line for line in code.split(chr(10)))}
except Exception as e:
    return_value = {{"error": str(e), "traceback": traceback.format_exc()}}

# Output result
result = {{
    "output": stdout_capture.get_output(),
    "stderr": stderr_capture.get_output(),
    "return_value": return_value,
}}
print(json.dumps(result))
'''
        return wrapper

    def test_connection(self) -> bool:
        """Test Docker connection."""
        try:
            self.client.ping()
            return True
        except Exception:
            return False


# Global executor instance
_executor: DockerSandboxExecutor | None = None


def get_executor() -> DockerSandboxExecutor:
    """Get or create the global executor instance."""
    global _executor
    if _executor is None:
        _executor = DockerSandboxExecutor()
    return _executor


__all__ = ["DockerSandboxExecutor", "get_executor"]
