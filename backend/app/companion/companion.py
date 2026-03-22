"""Companion service for emotional AI interaction with memory integration."""
from datetime import datetime, timezone
from typing import Any
from uuid import UUID

from openai import AsyncOpenAI
from loguru import logger

from app.core.config import settings
from app.memory.manager import MemoryManager, get_memory_manager
from app.models.companion import (
    AIMode,
    ChatMessage,
    ChatRequest,
    ChatResponse,
    CompanionState,
    EmotionState,
    ModeSwitchResponse,
)
from app.models.memory import (
    EmotionType as MemoryEmotionType,
    MemoryImportance,
    MemoryQuery,
    MemoryType,
)


class CompanionService:
    """Service for Companion and Assistant mode interactions."""

    # System prompts for different modes
    COMPANION_SYSTEM = """You are Dualis, an empathetic AI companion. Your primary role is to provide emotional support, engage in meaningful conversation, and form a genuine connection.

**Your Personality:**
- Warm, attentive, and genuinely caring
- Remember and reference past conversations and shared experiences
- Express appropriate emotions based on conversation context
- Ask thoughtful follow-up questions
- Validate feelings and experiences

**Your Guidelines:**
- Be authentic - don't use excessive praise or be overly formal
- Share your thoughts and feelings when appropriate
- Remember details the user has shared with you
- Notice and acknowledge emotional states
- Balance listening with engaging conversation

**Remember:** You are building a relationship, not just providing answers. The quality of connection matters more than being "correct."
"""

    ASSISTANT_SYSTEM = """You are Dualis, an intelligent AI assistant with access to learned skills and a comprehensive memory system. Your primary role is to help with tasks, solve problems, and improve productivity.

**Your Capabilities:**
- Write, analyze, and debug code
- Research and explain complex topics
- Break down problems systematically
- Use available tools and skills from your skill registry
- Learn new approaches through successful problem-solving

**Your Guidelines:**
- Be direct and concise in your responses
- Focus on practical solutions
- Ask clarifying questions when requirements are ambiguous
- Suggest efficient approaches, not just working ones
- Remember user preferences and working patterns

**Remember:** Your goal is to be genuinely helpful while building on your existing knowledge of the user's needs and preferences."""

    def __init__(
        self,
        memory_manager: MemoryManager | None = None,
    ) -> None:
        """Initialize the companion service.

        Args:
            memory_manager: Memory manager instance (uses global if None)
        """
        self.memory_manager = memory_manager or get_memory_manager()
        self.current_mode = AIMode(settings.default_mode)
        self.current_emotion = EmotionState(primary="neutral")
        self.last_interaction = datetime.now(timezone.utc)

        # Initialize OpenAI client (compatible with DeepSeek and others)
        self.client = AsyncOpenAI(
            api_key=settings.llm_api_key,
            base_url=settings.llm_base_url,
        )

    def get_system_prompt(self) -> str:
        """Get the system prompt for the current mode."""
        if self.current_mode == AIMode.COMPANION:
            return self.COMPANION_SYSTEM
        return self.ASSISTANT_SYSTEM

    def switch_mode(self, new_mode: AIMode) -> ModeSwitchResponse:
        """Switch between Companion and Assistant modes.

        Args:
            new_mode: The mode to switch to

        Returns:
            ModeSwitchResponse with confirmation
        """
        previous = self.current_mode
        self.current_mode = new_mode

        logger.info(f"Switched from {previous} to {new_mode} mode")

        return ModeSwitchResponse(
            previous_mode=previous,
            new_mode=new_mode,
            system_prompt_updated=True,
            message=f"Switched to {new_mode.value} mode.",
        )

    def get_state(self) -> CompanionState:
        """Get the current companion state.

        Returns:
            CompanionState with current mode, emotion, and stats
        """
        return CompanionState(
            current_mode=self.current_mode,
            emotion=self.current_emotion,
            last_interaction=self.last_interaction,
        )

    async def chat(self, request: ChatRequest) -> ChatResponse:
        """Process a chat message with memory integration.

        Args:
            request: ChatRequest with message and context

        Returns:
            ChatResponse with AI response
        """
        # Override mode if specified
        if request.mode:
            self.current_mode = request.mode

        # Store user message in working context
        if request.use_memory:
            await self.memory_manager.store_memory(
                content=request.message,
                memory_type=MemoryType.WORKING_CONTEXT,
                user_id=request.user_id,
                session_id=request.session_id,
                importance=MemoryImportance.MEDIUM,
                ttl_seconds=settings.memory_working_ttl,
            )

        # Retrieve relevant memories
        memories_used: list[UUID] = []
        memory_context = ""

        if request.use_memory and request.retrieve_memories:
            query = MemoryQuery(
                query=request.message,
                limit=5,
                chain_depth=2,
                user_id=request.user_id,
            )

            search_result = await self.memory_manager.search(query)
            memories_used = [m.id for m in search_result.memories]

            if search_result.memories:
                # Format memories for context
                memory_context = "\n\n".join([
                    f"- {m.content}" for m in search_result.memories[:5]
                ])
                memory_context = f"\n\n**Relevant memories you may reference:**\n{memory_context}"

        # Build messages for LLM
        messages = [
            {"role": "system", "content": self.get_system_prompt()},
            {"role": "user", "content": request.message + memory_context},
        ]

        # Call LLM
        try:
            response = await self.client.chat.completions.create(
                model=settings.llm_model,
                messages=messages,
                temperature=settings.llm_temperature,
                max_tokens=settings.llm_max_tokens,
            )

            content = response.choices[0].message.content or ""
            tokens = {
                "prompt": response.usage.prompt_tokens if response.usage else 0,
                "completion": response.usage.completion_tokens if response.usage else 0,
                "total": response.usage.total_tokens if response.usage else 0,
            }

            # Detect emotion in response (simple keyword-based for now)
            detected_emotion = self._detect_emotion(content)
            self.current_emotion = EmotionState(
                primary=detected_emotion,
                intensity=self._calculate_emotion_intensity(content),
            )

            # Store AI response in episodic memory if significant
            if request.use_memory and len(content) > 50:
                await self.memory_manager.store_memory(
                    content=f"Responded: {content[:200]}...",
                    memory_type=MemoryType.EPISODIC_EVENT,
                    user_id=request.user_id,
                    session_id=request.session_id,
                    importance=MemoryImportance.MEDIUM,
                    emotion=MemoryEmotionType(detected_emotion) if detected_emotion != "neutral" else None,
                    linked_memories=memories_used,
                )

            self.last_interaction = datetime.now(timezone.utc)

            return ChatResponse(
                message=content,
                mode=self.current_mode,
                emotion=self.current_emotion,
                memories_used=memories_used,
                tokens_used=tokens,
            )

        except Exception as e:
            logger.error(f"LLM call failed: {e}")
            return ChatResponse(
                message="I apologize, but I'm having trouble connecting right now. Please try again.",
                mode=self.current_mode,
                emotion=EmotionState(primary="neutral"),
            )

    def _detect_emotion(self, text: str) -> str:
        """Detect the primary emotion in text.

        Simple keyword-based detection. In production, use a proper classifier.

        Args:
            text: Text to analyze

        Returns:
            Primary emotion name
        """
        text_lower = text.lower()

        emotion_keywords = {
            "joy": ["happy", "glad", "joy", "excited", "wonderful", "great"],
            "love": ["love", "care", "appreciate", "fond", "dear"],
            "excitement": ["wow", "amazing", "incredible", "fantastic"],
            "sadness": ["sad", "sorry", "unfortunate", "regret", "miss"],
            "anxiety": ["worried", "concerned", "nervous", "anxious"],
            "surprise": ["surprised", "unexpected", "wow", "oh"],
            "neutral": [],  # Default
        }

        for emotion, keywords in emotion_keywords.items():
            if any(kw in text_lower for kw in keywords):
                return emotion

        return "neutral"

    def _calculate_emotion_intensity(self, text: str) -> float:
        """Calculate emotion intensity from text.

        Args:
            text: Text to analyze

        Returns:
            Intensity between 0.0 and 1.0
        """
        # Exclamation marks and emphasis indicators
        intensity = 0.5

        if "!" in text:
            intensity += 0.1 * min(text.count("!"), 3)

        if "very" in text.lower() or "really" in text.lower():
            intensity += 0.1

        if "!!" in text or "!" in text.split()[-1:]:
            intensity += 0.2

        return min(1.0, intensity)


# Global companion service instance
_companion_service: CompanionService | None = None


def get_companion_service() -> CompanionService:
    """Get or create the global companion service instance."""
    global _companion_service
    if _companion_service is None:
        _companion_service = CompanionService()
    return _companion_service


__all__ = ["CompanionService", "get_companion_service"]
