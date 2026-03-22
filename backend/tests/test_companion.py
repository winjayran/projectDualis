"""Tests for the companion system."""
import pytest

from app.models.companion import (
    AIMode,
    ChatRequest,
    ChatResponse,
    CompanionState,
    EmotionState,
    ModeSwitchRequest,
)


def test_emotion_state():
    """Test emotion state creation and to_dict conversion."""
    emotion = EmotionState(primary="joy", secondary="excitement", intensity=0.8)
    emotion_dict = emotion.to_dict()

    assert emotion.primary == "joy"
    assert emotion.secondary == "excitement"
    assert emotion.intensity == 0.8
    assert "primary" in emotion_dict
    assert emotion_dict["intensity"] == 0.8


def test_chat_request():
    """Test chat request creation."""
    request = ChatRequest(
        message="Hello, how are you?",
        mode=AIMode.COMPANION,
        use_memory=True,
    )

    assert request.message == "Hello, how are you?"
    assert request.mode == AIMode.COMPANION
    assert request.use_memory is True


def test_ai_modes():
    """Test that all AI modes are defined."""
    assert AIMode.COMPANION == "companion"
    assert AIMode.ASSISTANT == "assistant"


def test_companion_state():
    """Test companion state creation."""
    state = CompanionState(
        current_mode=AIMode.COMPANION,
        emotion=EmotionState(primary="neutral"),
    )

    assert state.current_mode == AIMode.COMPANION
    assert state.emotion.primary == "neutral"
    assert state.memory_count == 0


def test_mode_switch_request():
    """Test mode switch request."""
    request = ModeSwitchRequest(
        new_mode=AIMode.ASSISTANT,
        reason="Need help with coding",
    )

    assert request.new_mode == AIMode.ASSISTANT
    assert request.reason == "Need help with coding"
