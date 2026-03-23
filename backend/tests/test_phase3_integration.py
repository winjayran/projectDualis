"""Integration tests for Phase 3 (Unity frontend) components.

Tests WebSocket communication, TTS streaming, and STT endpoints.
"""
import asyncio
import base64
import json
import os
from pathlib import Path
from typing import AsyncGenerator

import pytest
import httpx
from fastapi import WebSocket
from fastapi.testclient import TestClient

from app.api import create_app
from app.api.websocket import manager, websocket_endpoint
from app.companion.companion import CompanionService, get_companion_service
from app.models.companion import AIMode, ChatRequest

# Create the FastAPI app for testing
app = create_app()


@pytest.fixture
def test_client():
    """Create a test client for the FastAPI app."""
    return TestClient(app)


@pytest.fixture
async def companion_service() -> AsyncGenerator[CompanionService, None]:
    """Create a companion service instance for testing."""
    service = CompanionService()
    yield service
    # Cleanup


class TestTTEndpoints:
    """Test Text-to-Speech endpoints."""

    def test_list_voices(self, test_client: TestClient):
        """Test listing available TTS voices."""
        response = test_client.get("/api/v1/tts/voices")
        assert response.status_code == 200

        data = response.json()
        assert "voices" in data
        # voices is a dict, not a list
        assert isinstance(data["voices"], dict)
        assert len(data["voices"]) > 0

        # Check voice options exist
        assert "female" in data["voices"]
        assert "male" in data["voices"]

    def test_generate_tts(self, test_client: TestClient):
        """Test TTS audio generation via WebSocket endpoint."""
        response = test_client.post(
            "/api/v1/tts/speak/ws",
            params={"text": "Hello, this is a test.", "voice": "en-US-JennyNeural"}
        )
        assert response.status_code == 200

        data = response.json()
        assert "success" in data
        assert "format" in data

        if data.get("success"):
            assert "audio_base64" in data

            # Verify base64 can be decoded
            audio_data = base64.b64decode(data["audio_base64"])
            assert len(audio_data) > 0

    def test_stream_tts(self, test_client: TestClient):
        """Test TTS audio streaming."""
        response = test_client.post(
            "/api/v1/tts/speak",
            params={"text": "Streaming test.", "voice": "en-US-JennyNeural"}
        )
        assert response.status_code == 200

        # Check if audio data is returned
        audio_data = response.content
        assert len(audio_data) > 0


class TestSTTEndpoints:
    """Test Speech-to-Text endpoints."""

    def test_list_models(self, test_client: TestClient):
        """Test listing available Whisper models."""
        response = test_client.get("/api/v1/stt/models")
        assert response.status_code == 200

        data = response.json()
        # Check for both API and local models
        assert "api_models" in data
        assert "local_models" in data
        assert isinstance(data["api_models"], list)
        assert isinstance(data["local_models"], list)

    def test_transcribe_with_audio_file(self, test_client: TestClient):
        """Test audio transcription with a small audio file."""
        # Create a small dummy WAV file
        # For real testing, use actual audio files
        dummy_audio = b"RIFF" + b"\x00" * 100  # Minimal WAV header

        response = test_client.post(
            "/api/v1/stt/transcribe",
            files={"audio": ("test.wav", dummy_audio, "audio/wav")},
            data={"model": "tiny"}
        )

        # Response should either be successful or indicate missing audio
        assert response.status_code in [200, 400, 422]


class TestWebSocketEndpoint:
    """Test WebSocket endpoint for Unity communication."""

    @pytest.mark.asyncio
    async def test_websocket_connection(self, test_client: TestClient):
        """Test WebSocket connection and ping/pong."""
        # Note: TestClient doesn't support WebSockets directly
        # This is a placeholder for real WebSocket testing
        pass

    @pytest.mark.asyncio
    async def test_websocket_chat_message(self):
        """Test chat message through WebSocket."""
        # This would require a real WebSocket client
        # For now, test the companion service directly
        service = CompanionService()

        request = ChatRequest(
            message="Hello, test message.",
            mode=AIMode.COMPANION,
            session_id="test_session",
            use_memory=False
        )

        response = await service.chat(request)

        assert response.message is not None
        assert len(response.message) > 0
        assert response.mode is not None

    @pytest.mark.asyncio
    async def test_websocket_mode_switch(self):
        """Test mode switching through WebSocket."""
        service = CompanionService()

        # Start in companion mode
        initial_state = service.get_state()
        assert initial_state.current_mode == AIMode.COMPANION

        # Switch to assistant
        result = service.switch_mode(AIMode.ASSISTANT)
        assert result.new_mode == AIMode.ASSISTANT
        assert result.previous_mode == AIMode.COMPANION

        # Switch back
        result = service.switch_mode(AIMode.COMPANION)
        assert result.new_mode == AIMode.COMPANION


class TestIntegrationScenarios:
    """Test complete integration scenarios."""

    @pytest.mark.asyncio
    async def test_chat_with_tts_flow(self):
        """Test complete flow: Chat -> TTS audio generation."""
        service = CompanionService()

        request = ChatRequest(
            message="Say something brief.",
            mode=AIMode.COMPANION,
            session_id="test_session",
            use_memory=False
        )

        response = await service.chat(request)

        # Generate TTS for the response
        try:
            import edge_tts
            communicate = edge_tts.Communicate(text=response.message[:50], voice="en-US-JennyNeural")

            audio_data = b""
            async for chunk in communicate.stream():
                if chunk["type"] == "audio":
                    audio_data += chunk["data"]

            assert len(audio_data) > 0
        except ImportError:
            pytest.skip("edge-tts not installed")

    @pytest.mark.asyncio
    async def test_emotion_detection_flow(self):
        """Test emotion detection in chat response."""
        service = CompanionService()

        # Send an emotional message
        request = ChatRequest(
            message="I'm so happy today!",
            mode=AIMode.COMPANION,
            session_id="test_session",
            use_memory=False
        )

        response = await service.chat(request)

        # Check if emotion was detected
        assert response.emotion is not None
        assert response.emotion.primary is not None


def test_tts_audio_format():
    """Test that TTS generates valid audio format."""
    try:
        import edge_tts

        async def generate_test_audio():
            communicate = edge_tts.Communicate(text="Test", voice="en-US-JennyNeural")
            audio_data = b""
            async for chunk in communicate.stream():
                if chunk["type"] == "audio":
                    audio_data += chunk["data"]
            return audio_data

        audio = asyncio.run(generate_test_audio())

        # Check for MP3 header (multiple possible formats)
        assert len(audio) > 2
        # MP3 can start with various byte patterns:
        # - ID3 tag: b"ID3"
        # - MPEG sync: b"\xff\xfx" where x is high bits set
        # - The actual format depends on the encoder
        is_valid_mp3 = (
            audio[:3] == b"ID3" or
            (audio[0] == 0xFF and (audio[1] & 0xE0) == 0xE0)
        )
        assert is_valid_mp3, f"Audio does not appear to be MP3: {audio[:4].hex()}"

    except ImportError:
        pytest.skip("edge-tts not installed")


def test_websocket_message_serialization():
    """Test WebSocket message serialization format."""
    from datetime import datetime, timezone

    message_type = "chat_response"
    data = {"message": "Hello", "emotion": {"primary": "joy", "intensity": 0.8}}

    message = {
        "type": message_type,
        "data": json.dumps(data),
        "timestamp": int(datetime.now(timezone.utc).timestamp() * 1000),
    }

    # Verify JSON serialization
    message_json = json.dumps(message)
    parsed = json.loads(message_json)

    assert parsed["type"] == message_type
    assert parsed["timestamp"] > 0


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
