# Project Dualis - Developer Guide

## Project Overview
Project Dualis is a next-generation 3D AI virtual entity combining an emotional companion with a capable personal assistant. It features advanced memory management, self-evolving skills, and a Unity 3D avatar.

## Architecture
- **Monorepo structure**: `backend/` (Python) and `frontend/` (Unity C#)
- **LLM**: OpenAI-compatible endpoint (supports DeepSeek, OpenAI, etc.)
- **Memory**: Qdrant vector DB + RAG with 4-category hierarchical system
- **Execution**: Docker/Apptainer sandbox for self-written plugins

## Project Structure
```
projectDualis/
├── backend/
│   ├── app/
│   │   ├── api/          # FastAPI routes
│   │   ├── core/         # Config, security, dependencies
│   │   ├── memory/       # Memory OS (Phase 1)
│   │   ├── companion/    # Companion mode (Phase 2)
│   │   ├── sandbox/      # Skill evolution (Phase 2.5)
│   │   └── models/       # Data models
│   ├── tests/
│   └── requirements.txt
├── frontend/             # Unity project (Phase 3)
├── CLAUDE.md
└── README.md
```

## Implementation Status

### ✅ Completed (Phase 1, 2, & 2.5)
- FastAPI backend framework with modular architecture
- 4-category hierarchical memory system (working, episodic, semantic, skill)
- Qdrant vector database integration with collection management
- Non-linear chained retrieval with depth control
- OpenAI-compatible LLM integration (DeepSeek support)
- Dual-mode system (Companion/Assistant) with dynamic prompts
- Emotion detection and tracking
- Memory-aware chat responses
- **Docker-based sandbox for secure code execution**
- **Skill registry with verification and OpenAI schema export**
- **Import whitelist and pattern blocking for security**

### 📋 In Progress
- Memory consolidation and forgetting mechanisms
- Enhanced emotion classification

### 🚧 Pending (Phase 3)
- Unity 3D project initialization
- VRM/VTuber model integration
- WebSocket IPC with backend
- TTS/STT audio pipeline

---

## Development Phases

Create a hierarchical memory system with 4 categories:
1. **Working Context**: Short-term, session-specific (ephemeral)
2. **Episodic Events**: Personal experiences, conversations (autobiographical)
3. **Semantic Facts**: General knowledge, user preferences (declarative)
4. **Skill Schemas**: Learned capabilities and tool definitions (procedural)

**Key Features**:
- Non-linear chained retrieval (reasoning-based associative linking)
- Qdrant vector storage with embeddings
- RAG (Retrieval-Augmented Generation)
- Memory consolidation and forgetting mechanisms

**Tech Stack**:
- `qdrant-client` for vector DB
- `sentence-transformers` or OpenAI embeddings
- `fastapi` for API layer

### Phase 2: Companion Ability
**Status**: Pending

Emotional/empathetic AI companion integrated with memory system:
- Dynamic system prompts for Companion vs Assistant modes
- Sentiment analysis and emotional state tracking
- Memory-aware personalized responses
- Context switching between modes

### Phase 2.5: Self-Evolving Skill Sandbox
**Status**: Pending

AI can write, test, and deploy its own Python plugins:
- `write_code()`: Generate Python code for new tasks
- `test_in_sandbox()`: Execute in Docker/Apptainer container
- `register_skill()`: Save verified tools to skill registry
- Dynamic tool loading into LLM context

### Phase 3: 3D Figure (Unity)
**Status**: Pending

Unity desktop client with:
- VRM/VTuber model loading
- Lip-sync and facial expressions
- WebSocket IPC with backend
- STT/TTS integration (Whisper, Edge-TTS)

## Coding Conventions

### Python (Backend)
- Use **async/await** for all I/O operations
- Type hints required for all function signatures
- Docstrings for all public methods (Google style)
- Environment variables for all secrets (use `pydantic-settings`)
- Structured logging with `loguru`

### File Organization
- One class per file where practical
- Use `dataclasses` or `pydantic` models for data structures
- Separate business logic from API routes
- Repository pattern for database operations

## Configuration
- Use `pydantic-settings` for config management
- All config via environment variables or `.env` files
- Never commit secrets or API keys
- OpenAI-compatible endpoint URL should be configurable

## Memory System Design

### Memory Categories
```python
enum MemoryType:
    WORKING_CONTEXT    # Session-specific, short TTL
    EPISODIC_EVENT     # User experiences, conversations
    SEMANTIC_FACT      # Preferences, knowledge
    SKILL_SCHEMA       # Learned tools and capabilities
```

### Non-Linear Chained Retrieval
Instead of simple semantic search, memories form associative chains:
- Each memory can link to related memories
- LLM reasoning traverses chains dynamically
- Enables "retrieve emotion → find related event → load required skill"

### Memory Operations
- `store(memory)`: Write with embedding and metadata
- `retrieve(query, type, depth)`: Chained retrieval
- `consolidate()`: Move working → episodic/semantic
- `forget(criteria)`: Prune old or low-importance memories

## API Design (FastAPI)

### Endpoints to Implement
```
POST   /api/v1/chat          # Main chat interface
POST   /api/v1/memory/store  # Store new memory
GET    /api/v1/memory/search # Search memories
POST   /api/v1/mode/switch   # Companion <-> Assistant
POST   /api/v1/skill/execute # Run learned skill
POST   /api/v1/sandbox/run   # Execute code safely
```

## Dependencies (Initial)
```
fastapi
uvicorn
qdrant-client
pydantic
pydantic-settings
openai  # For OpenAI-compatible API
python-dotenv
loguru
httpx
```

## VRAM Constraint
Total system must stay under 12GB VRAM:
- Backend: Qdrant + embeddings (~2GB)
- Optional local models (Phase 4): quantized models only
- Unity client (Phase 3): Optimized 3D rendering

## Testing Strategy
- Unit tests for memory operations
- Integration tests for API endpoints
- Sandbox isolation tests for code execution
- End-to-end tests for chat flows

## Notes
- Prioritize memory system first
- Keep LLM endpoint flexible (OpenAI-compatible)
- Design for extensibility—more memory types and skills can be added
- Unity client is last phase—focus on backend first
