"""Chat API endpoints for Project Dualis."""
from fastapi import APIRouter, Depends, HTTPException

from app.companion.companion import CompanionService, get_companion_service
from app.models.companion import (
    AIMode,
    ChatRequest,
    ChatResponse,
    CompanionState,
    ModeSwitchRequest,
    ModeSwitchResponse,
)

router = APIRouter(prefix="/api/v1/chat", tags=["chat"])


@router.post("", response_model=ChatResponse)
async def chat(
    request: ChatRequest,
    companion: CompanionService = Depends(get_companion_service),
) -> ChatResponse:
    """Send a chat message and receive an AI response.

    The AI will use its current mode (Companion/Assistant) and integrate
    with the memory system for personalized responses.
    """
    try:
        return await companion.chat(request)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/mode", response_model=ModeSwitchResponse)
async def switch_mode(
    request: ModeSwitchRequest,
    companion: CompanionService = Depends(get_companion_service),
) -> ModeSwitchResponse:
    """Switch between Companion and Assistant modes."""
    return companion.switch_mode(request.new_mode)


@router.get("/state", response_model=CompanionState)
async def get_state(
    companion: CompanionService = Depends(get_companion_service),
) -> CompanionState:
    """Get the current companion state including mode and emotion."""
    return companion.get_state()


@router.get("/modes", response_model=list[str])
async def list_modes() -> list[str]:
    """List available AI modes."""
    return [mode.value for mode in AIMode]


__all__ = ["router"]
