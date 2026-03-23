"""TTS API endpoints using Edge-TTS."""
import io
from typing import Any

from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse
from loguru import logger

try:
    import edge_tts
    EDGE_TTS_AVAILABLE = True
except ImportError:
    EDGE_TTS_AVAILABLE = False
    logger.warning("edge-tts not installed. TTS features will be unavailable.")

router = APIRouter(prefix="/api/v1/tts", tags=["tts"])

# Available voices for Edge-TTS
VOICE_OPTIONS = {
    "female": "en-US-JennyNeural",
    "male": "en-US-GuyNeural",
    "female_ja": "ja-JP-NanamiNeural",
    "male_ja": "ja-JP-KeitaNeural",
}


@router.get("/voices")
async def list_voices() -> dict[str, Any]:
    """List available TTS voices."""
    return {
        "voices": VOICE_OPTIONS,
        "default": VOICE_OPTIONS["female"],
    }


@router.post("/speak")
async def text_to_speech(
    text: str,
    voice: str = VOICE_OPTIONS["female"],
    rate: str = "+0%",
    pitch: str = "+0Hz",
    volume: str = "+0%",
) -> StreamingResponse:
    """Convert text to speech using Edge-TTS.

    Args:
        text: Text to convert to speech
        voice: Voice name (default: en-US-JennyNeural)
        rate: Speaking rate (e.g., "+10%", "-20%")
        pitch: Pitch adjustment (e.g., "+10Hz", "-5Hz")
        volume: Volume adjustment (e.g., "+10%", "-20%")

    Returns:
        Audio stream (MP3 format)
    """
    if not EDGE_TTS_AVAILABLE:
        raise HTTPException(
            status_code=501,
            detail="Edge-TTS not installed. Install with: pip install edge-tts",
        )

    if not text or len(text.strip()) == 0:
        raise HTTPException(status_code=400, detail="Text cannot be empty")

    try:
        communicate = edge_tts.Communicate(
            text=text,
            voice=voice,
            rate=rate,
            pitch=pitch,
            volume=volume,
        )

        # Generate audio data
        audio_data = b""
        async for chunk in communicate.stream():
            if chunk["type"] == "audio":
                audio_data += chunk["data"]

        if not audio_data:
            raise HTTPException(status_code=500, detail="Failed to generate audio")

        return StreamingResponse(
            io.BytesIO(audio_data),
            media_type="audio/mpeg",
            headers={
                "Content-Disposition": f"attachment; filename=tts.mp3",
                "Content-Length": str(len(audio_data)),
            },
        )

    except Exception as e:
        logger.error(f"TTS generation failed: {e}")
        raise HTTPException(status_code=500, detail=f"TTS generation failed: {e}")


@router.post("/speak/ws")
async def text_to_speech_websocket(
    text: str,
    voice: str = VOICE_OPTIONS["female"],
) -> dict[str, Any]:
    """Generate TTS and return base64 encoded audio for WebSocket.

    This endpoint is optimized for WebSocket communication with the Unity client.

    Args:
        text: Text to convert to speech
        voice: Voice name

    Returns:
        Dictionary with audio data in base64 format
    """
    import base64

    if not EDGE_TTS_AVAILABLE:
        return {"error": "Edge-TTS not available"}

    try:
        communicate = edge_tts.Communicate(text=text, voice=voice)

        audio_data = b""
        async for chunk in communicate.stream():
            if chunk["type"] == "audio":
                audio_data += chunk["data"]

        return {
            "success": True,
            "audio_base64": base64.b64encode(audio_data).decode("utf-8"),
            "format": "mp3",
            "length": len(audio_data),
        }

    except Exception as e:
        logger.error(f"TTS generation failed: {e}")
        return {"success": False, "error": str(e)}


__all__ = ["router"]
