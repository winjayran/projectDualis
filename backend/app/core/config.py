"""Core configuration for Project Dualis."""
from functools import lru_cache
from typing import Literal

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )

    # API Configuration
    api_host: str = Field(default="0.0.0.0", description="API host to bind to")
    api_port: int = Field(default=8000, description="API port to bind to")
    api_reload: bool = Field(default=False, description="Enable auto-reload in dev")

    # LLM Configuration (OpenAI-compatible)
    llm_api_key: str = Field(default="", description="API key for LLM provider")
    llm_base_url: str = Field(
        default="https://api.deepseek.com/v1",
        description="Base URL for OpenAI-compatible API",
    )
    llm_model: str = Field(
        default="deepseek-chat",
        description="Model name to use",
    )
    llm_temperature: float = Field(
        default=0.7,
        ge=0.0,
        le=2.0,
        description="Default temperature for responses",
    )
    llm_max_tokens: int = Field(
        default=2048,
        ge=1,
        description="Max tokens in response",
    )

    # Embedding Configuration
    embedding_model: str = Field(
        default="sentence-transformers/all-MiniLM-L6-v2",
        description="Model for text embeddings",
    )
    embedding_device: str = Field(
        default="cpu",
        description="Device for embeddings (cpu/cuda)",
    )

    # Qdrant Configuration
    qdrant_host: str = Field(default="localhost", description="Qdrant host")
    qdrant_port: int = Field(default=6333, description="Qdrant gRPC port")
    qdrant_http_port: int = Field(default=6334, description="Qdrant HTTP port")
    qdrant_collection_prefix: str = Field(
        default="dualis",
        description="Prefix for Qdrant collections",
    )

    # Memory Configuration
    memory_working_ttl: int = Field(
        default=3600,
        description="TTL for working context memories (seconds)",
    )
    memory_max_retrieval: int = Field(
        default=20,
        description="Max memories to retrieve per query",
    )
    memory_chain_depth: int = Field(
        default=3,
        description="Max depth for chained retrieval",
    )

    # Sandbox Configuration (Phase 2.5)
    sandbox_enabled: bool = Field(default=False, description="Enable code sandbox")
    sandbox_docker_image: str = Field(
        default="python:3.10-slim",
        description="Docker image for sandbox",
    )
    sandbox_timeout: int = Field(
        default=30,
        description="Sandbox execution timeout (seconds)",
    )

    # Application Mode
    default_mode: Literal["companion", "assistant"] = Field(
        default="companion",
        description="Default AI mode",
    )


@lru_cache
def get_settings() -> Settings:
    """Get cached settings instance."""
    return Settings()


# Global settings instance
settings = get_settings()
