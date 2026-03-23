"""STT API endpoints using OpenAI Whisper."""
import io
from typing import Any

from fastapi import APIRouter, File, HTTPException, UploadFile
from loguru import logger
from openai import AsyncOpenAI

from app.core.config import settings

router = APIRouter(prefix="/api/v1/stt", tags=["stt"])

# Initialize OpenAI client for Whisper
whisper_client = None


def get_whisper_client() -> AsyncOpenAI | None:
    """Get or create the Whisper client."""
    global whisper_client

    # Check if using OpenAI-compatible Whisper endpoint
    # DeepSeek and some other providers don't support Whisper
    # So we need to check if the base URL supports it

    if settings.llm_base_url and "deepseek" in settings.llm_base_url.lower():
        logger.warning("DeepSeek API does not support Whisper. STT unavailable via API.")
        return None

    try:
        if whisper_client is None:
            whisper_client = AsyncOpenAI(
                api_key=settings.llm_api_key,
                base_url=settings.llm_base_url,
            )
        return whisper_client
    except Exception as e:
        logger.error(f"Failed to initialize Whisper client: {e}")
        return None


@router.post("/transcribe")
async def transcribe_audio(
    file: UploadFile = File(...),
    language: str = "en",
    prompt: str = "",
) -> dict[str, Any]:
    """Transcribe audio using Whisper.

    Args:
        file: Audio file (mp3, mp4, mpeg, mpga, m4a, wav, webm)
        language: Language code (default: "en")
        prompt: Optional prompt to guide transcription

    Returns:
        Transcription result with text and duration
    """
    client = get_whisper_client()

    if client is None:
        raise HTTPException(
            status_code=501,
            detail="Whisper STT not available with current API provider. "
            "Configure an OpenAI-compatible API that supports Whisper, "
            "or use local Whisper installation.",
        )

    try:
        # Read audio file
        audio_data = await file.read()
        audio_file = io.BytesIO(audio_data)
        audio_file.name = file.filename

        # Transcribe using Whisper
        transcript = await client.audio.transcriptions.create(
            model="whisper-1",
            file=audio_file,
            language=language,
            prompt=prompt if prompt else None,
            response_format="text",
        )

        return {
            "success": True,
            "text": transcript,
            "language": language,
        }

    except Exception as e:
        logger.error(f"Whisper transcription failed: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Transcription failed: {e}",
        )


@router.post("/transcribe/local")
async def transcribe_audio_local(
    file: UploadFile = File(...),
    language: str = "en",
    model_size: str = "base",
) -> dict[str, Any]:
    """Transcribe audio using local Whisper model.

    This requires whisper to be installed: pip install openai-whisper

    Args:
        file: Audio file
        language: Language code (default: "en")
        model_size: Whisper model size (tiny, base, small, medium, large)

    Returns:
        Transcription result
    """
    try:
        import whisper
    except ImportError:
        raise HTTPException(
            status_code=501,
            detail="Local Whisper not installed. Install with: pip install openai-whisper",
        )

    try:
        # Save uploaded file temporarily
        import tempfile

        with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as tmp:
            tmp.write(await file.read())
            tmp_path = tmp.name

        # Load and run Whisper
        model = whisper.load_model(model_size)
        result = model.transcribe(tmp_path, language=language)

        # Clean up
        import os

        os.unlink(tmp_path)

        return {
            "success": True,
            "text": result["text"],
            "language": result.get("language", language),
            "segments": [
                {
                    "start": seg["start"],
                    "end": seg["end"],
                    "text": seg["text"],
                }
                for seg in result["segments"]
            ],
        }

    except Exception as e:
        logger.error(f"Local Whisper transcription failed: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Local transcription failed: {e}",
        )


@router.get("/models")
async def list_models() -> dict[str, Any]:
    """List available Whisper models."""
    return {
        "api_models": ["whisper-1"],
        "local_models": ["tiny", "base", "small", "medium", "large"],
        "recommended": "base",
    }


__all__ = ["router"]
