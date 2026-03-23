"""WebSocket endpoint for real-time Unity frontend communication."""
from typing import Any
from uuid import UUID

from fastapi import WebSocket, WebSocketDisconnect, status, Depends
from loguru import logger

from app.companion.companion import CompanionService, get_companion_service
from app.models.companion import AIMode, ChatRequest, EmotionState


class WebSocketConnectionManager:
    """Manages WebSocket connections for Unity clients."""

    def __init__(self) -> None:
        self.active_connections: dict[str, WebSocket] = {}

    async def connect(self, client_id: str, websocket: WebSocket) -> None:
        """Accept and register a new WebSocket connection."""
        await websocket.accept()
        self.active_connections[client_id] = websocket
        logger.info(f"WebSocket client connected: {client_id}")

    def disconnect(self, client_id: str) -> None:
        """Remove a WebSocket connection."""
        if client_id in self.active_connections:
            del self.active_connections[client_id]
            logger.info(f"WebSocket client disconnected: {client_id}")

    async def send_message(
        self, client_id: str, message_type: str, data: dict[str, Any]
    ) -> bool:
        """Send a message to a specific client."""
        import json
        from datetime import datetime, timezone

        if client_id not in self.active_connections:
            return False

        websocket = self.active_connections[client_id]
        message = {
            "type": message_type,
            "data": json.dumps(data) if not isinstance(data, str) else data,
            "timestamp": int(datetime.now(timezone.utc).timestamp() * 1000),
        }

        try:
            await websocket.send_json(message)
            return True
        except Exception as e:
            logger.error(f"Failed to send message to {client_id}: {e}")
            self.disconnect(client_id)
            return False

    async def broadcast(self, message_type: str, data: dict[str, Any]) -> None:
        """Broadcast a message to all connected clients."""
        import json
        from datetime import datetime, timezone

        message = {
            "type": message_type,
            "data": json.dumps(data) if not isinstance(data, str) else data,
            "timestamp": int(datetime.now(timezone.utc).timestamp() * 1000),
        }

        disconnected = []
        for client_id, websocket in self.active_connections.items():
            try:
                await websocket.send_json(message)
            except Exception:
                disconnected.append(client_id)

        for client_id in disconnected:
            self.disconnect(client_id)


# Global connection manager
manager = WebSocketConnectionManager()


async def websocket_endpoint(
    websocket: WebSocket,
    client_id: str | None = None,
    companion: CompanionService = Depends(get_companion_service),
) -> None:
    """WebSocket endpoint for real-time Unity communication.

    Handles:
    - Chat messages with streaming responses
    - Mode switching
    - Emotion updates
    - State synchronization
    """
    from app.api.websocket import manager

    if not client_id:
        # Use the websocket's client ID if not provided
        client_id = id(websocket)

    await manager.connect(client_id, websocket)

    try:
        while True:
            # Receive message from Unity client
            data = await websocket.receive_json()
            message_type = data.get("type", "")
            message_data = data.get("data", {})

            if message_type == "chat":
                await handle_chat_message(client_id, message_data, companion)

            elif message_type == "mode_switch":
                await handle_mode_switch(client_id, message_data, companion)

            elif message_type == "ping":
                await manager.send_message(client_id, "pong", {})

            elif message_type == "get_state":
                state = companion.get_state()
                await manager.send_message(
                    client_id,
                    "state",
                    {
                        "current_mode": state.current_mode.value,
                        "emotion": state.emotion.to_dict(),
                        "last_interaction": (
                            state.last_interaction.isoformat()
                            if state.last_interaction
                            else None
                        ),
                    },
                )

            else:
                logger.warning(f"Unknown message type: {message_type}")

    except WebSocketDisconnect:
        manager.disconnect(client_id)
    except Exception as e:
        logger.error(f"WebSocket error for {client_id}: {e}")
        manager.disconnect(client_id)


async def handle_chat_message(
    client_id: str,
    data: dict[str, Any],
    companion: CompanionService,
) -> None:
    """Handle chat message from Unity client."""
    from app.api.websocket import manager

    try:
        # Create chat request
        request = ChatRequest(
            message=data.get("message", ""),
            mode=AIMode(data["mode"]) if data.get("mode") else None,
            session_id=data.get("session_id", client_id),
            user_id=data.get("user_id"),
            use_memory=data.get("use_memory", True),
            retrieve_memories=data.get("retrieve_memories", True),
        )

        # Get response from companion
        response = await companion.chat(request)

        # Generate TTS audio if requested
        audio_data = None
        if data.get("enable_tts", True):
            audio_data = await generate_tts_audio(response.message)

        # Send response back to Unity
        await manager.send_message(
            client_id,
            "chat_response",
            {
                "message": response.message,
                "mode": response.mode.value,
                "emotion": {
                    "primary": response.emotion.primary if response.emotion else "neutral",
                    "secondary": (
                        response.emotion.secondary if response.emotion else None
                    ),
                    "intensity": (
                        response.emotion.intensity if response.emotion else 0.5
                    ),
                },
                "memories_used": [str(m) for m in response.memories_used],
                "tokens_used": response.tokens_used,
                "audio_base64": audio_data,
            },
        )

        # Send emotion update separately for avatar control
        if response.emotion:
            await manager.send_message(
                client_id,
                "emotion",
                {
                    "primary": response.emotion.primary,
                    "secondary": response.emotion.secondary,
                    "intensity": response.emotion.intensity,
                },
            )

    except Exception as e:
        logger.error(f"Error handling chat message: {e}")
        await manager.send_message(
            client_id,
            "error",
            {"message": "Failed to process chat message"},
        )


async def generate_tts_audio(text: str) -> str | None:
    """Generate TTS audio and return as base64 encoded string."""
    try:
        import base64
        import edge_tts

        communicate = edge_tts.Communicate(text=text, voice="en-US-JennyNeural")
        audio_data = b""
        async for chunk in communicate.stream():
            if chunk["type"] == "audio":
                audio_data += chunk["data"]

        if audio_data:
            return base64.b64encode(audio_data).decode("utf-8")
    except Exception as e:
        logger.warning(f"TTS generation failed: {e}")
    return None


async def handle_mode_switch(
    client_id: str,
    data: dict[str, Any],
    companion: CompanionService,
) -> None:
    """Handle mode switch request from Unity client."""
    from app.api.websocket import manager

    try:
        new_mode = AIMode(data.get("mode", "companion"))
        result = companion.switch_mode(new_mode)

        await manager.send_message(
            client_id,
            "mode_switched",
            {
                "previous_mode": result.previous_mode.value,
                "new_mode": result.new_mode.value,
                "message": result.message,
            },
        )

    except Exception as e:
        logger.error(f"Error handling mode switch: {e}")


__all__ = ["websocket_endpoint", "manager"]
