"""Main entry point for Project Dualis backend server."""
import uvicorn

from app.api import create_app
from app.core.config import settings


def main() -> None:
    """Run the FastAPI server."""
    app = create_app()

    uvicorn.run(
        app,
        host=settings.api_host,
        port=settings.api_port,
        reload=settings.api_reload,
        log_level="info",
    )


if __name__ == "__main__":
    main()
