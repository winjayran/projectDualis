"""Embedding service for text vectorization."""
from typing import Any

from loguru import logger
from sentence_transformers import SentenceTransformer

from app.core.config import settings


class EmbeddingService:
    """Service for generating text embeddings using sentence-transformers."""

    def __init__(
        self,
        model_name: str | None = None,
        device: str | None = None,
    ) -> None:
        """Initialize the embedding model.

        Args:
            model_name: HuggingFace model name or path
            device: Device to run on ('cpu' or 'cuda')
        """
        self.model_name = model_name or settings.embedding_model
        self.device = device or settings.embedding_device
        self._model: SentenceTransformer | None = None

    @property
    def model(self) -> SentenceTransformer:
        """Lazy-load the embedding model."""
        if self._model is None:
            logger.info(f"Loading embedding model: {self.model_name} on {self.device}")
            self._model = SentenceTransformer(self.model_name, device=self.device)
        return self._model

    @property
    def dimension(self) -> int:
        """Get the embedding dimension."""
        # Common dimensions for popular models
        known_dimensions = {
            "sentence-transformers/all-MiniLM-L6-v2": 384,
            "sentence-transformers/all-mpnet-base-v2": 768,
            "sentence-transformers/all-MiniLM-L12-v2": 384,
        }
        if self.model_name in known_dimensions:
            return known_dimensions[self.model_name]
        # Infer from model if loaded
        if self._model is not None:
            return self._model.get_sentence_embedding_dimension()
        # Default fallback
        return 384

    def embed(self, text: str | list[str]) -> Any:
        """Generate embedding(s) for the given text(s).

        Args:
            text: Single text string or list of texts

        Returns:
            Numpy array of embeddings
        """
        return self.model.encode(
            text,
            convert_to_numpy=True,
            show_progress_bar=False,
            normalize_embeddings=True,
        )

    async def aembed(self, text: str | list[str]) -> list[float] | list[list[float]]:
        """Async wrapper for embedding generation.

        Args:
            text: Single text string or list of texts

        Returns:
            List of embedding vectors
        """
        import asyncio

        # Run in thread pool to avoid blocking
        result = await asyncio.to_thread(self.embed, text)

        # Convert to list format
        if isinstance(text, str):
            return result.tolist()
        return [r.tolist() for r in result]

    def embed_single(self, text: str) -> list[float]:
        """Generate a single embedding vector.

        Args:
            text: Text to embed

        Returns:
            List of floats representing the embedding vector
        """
        return self.embed(text).tolist()


# Global embedding service instance
_embedding_service: EmbeddingService | None = None


def get_embedding_service() -> EmbeddingService:
    """Get or create the global embedding service instance."""
    global _embedding_service
    if _embedding_service is None:
        _embedding_service = EmbeddingService()
    return _embedding_service


__all__ = ["EmbeddingService", "get_embedding_service"]
