"""API module for Project Dualis."""
from fastapi import FastAPI, WebSocket
from fastapi.middleware.cors import CORSMiddleware

from app.api.chat import router as chat_router
from app.api.memory import router as memory_router
from app.api.sandbox import router as sandbox_router
from app.api.websocket import websocket_endpoint
from app.api.tts import router as tts_router
from app.api.stt import router as stt_router
from app.core.config import settings


def create_app() -> FastAPI:
    """Create and configure the FastAPI application."""
    app = FastAPI(
        title="Project Dualis API",
        description="Backend API for the Dualis AI virtual entity",
        version="0.1.0",
        docs_url="/docs",
        redoc_url="/redoc",
    )

    # Configure CORS
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],  # Configure appropriately for production
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    # Include routers
    app.include_router(chat_router)
    app.include_router(memory_router)
    app.include_router(sandbox_router)
    app.include_router(tts_router)
    app.include_router(stt_router)

    # WebSocket endpoint for Unity client
    @app.websocket("/ws")
    async def websocket_route(
        websocket: WebSocket,
        client_id: str | None = None,
    ):
        """WebSocket endpoint for real-time Unity client communication."""
        from app.companion.companion import get_companion_service

        await websocket_endpoint(
            websocket,
            client_id,
            companion=get_companion_service(),
        )

    # Health check
    @app.get("/health")
    async def health_check() -> dict[str, str]:
        """Health check endpoint."""
        from app.memory.storage import get_memory_store
        from app.sandbox.executor import get_executor

        memory_store = get_memory_store()
        executor = get_executor()

        return {
            "status": "healthy",
            "mode": settings.default_mode,
            "qdrant_connected": "connected" if memory_store.test_connection() else "disconnected",
            "docker_available": "available" if executor.test_connection() else "unavailable",
        }

    # Startup event
    @app.on_event("startup")
    async def startup_event():
        """Initialize services on startup."""
        from app.memory.embeddings import get_embedding_service
        from app.memory.storage import get_memory_store
        from app.companion.companion import get_companion_service

        # Pre-initialize services
        get_embedding_service()
        get_memory_store()
        get_companion_service()

    return app


__all__ = ["create_app"]
