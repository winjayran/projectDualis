#!/bin/bash
# Project Dualis - Environment Configuration
# Source this file before running the project: source env.sh

# LLM Configuration (OpenAI-compatible)
export LLM_API_KEY="your_deepseek_api_key_here"
export LLM_BASE_URL="https://api.deepseek.com/v1"
export LLM_MODEL="deepseek-chat"
export LLM_TEMPERATURE="0.7"
export LLM_MAX_TOKENS="2048"

# API Configuration
export API_HOST="0.0.0.0"
export API_PORT="8000"

# Qdrant Configuration
export QDRANT_HOST="localhost"
export QDRANT_PORT="6333"
export QDRANT_HTTP_PORT="6334"

# Memory Configuration
export MEMORY_WORKING_TTL="3600"
export MEMORY_MAX_RETRIEVAL="20"
export MEMORY_CHAIN_DEPTH="3"

# Sandbox Configuration
export SANDBOX_ENABLED="true"
export SANDBOX_DOCKER_IMAGE="python:3.10-slim"
export SANDBOX_TIMEOUT="30"

# Application Mode
export DEFAULT_MODE="companion"
