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
│   │   │   ├── chat.py
│   │   │   ├── websocket.py    # Real-time Unity communication
│   │   │   ├── tts.py          # Text-to-Speech (Edge-TTS)
│   │   │   └── stt.py          # Speech-to-Text (Whisper)
│   │   ├── core/         # Config, security, dependencies
│   │   ├── memory/       # Memory OS (Phase 1)
│   │   ├── companion/    # Companion mode (Phase 2)
│   │   ├── sandbox/      # Skill evolution (Phase 2.5)
│   │   └── models/       # Data models
│   ├── tests/
│   │   ├── test_phase3_integration.py
│   │   ├── test_companion.py
│   │   ├── test_memory.py
│   │   └── test_sandbox.py
│   └── requirements.txt
├── frontend/             # Unity project (Phase 3)
│   └── Assets/
│       └── Scripts/
│           ├── Core/           # GameManager, Config, WindowController
│           ├── Network/        # WebSocket, STT client
│           ├── Audio/          # TTS/STT handling
│           ├── Avatar/         # AvatarManager, VRMLoader, PlaceholderAvatar
│           ├── UI/             # UI Manager
│           ├── Debug/          # Debug display for testing
│           └── Editor/         # Editor tools and scripts
├── logs/                # Runtime logs
├── CLAUDE.md
├── README.md
└── PHASE3LEFT.md
```

## Implementation Status

### ✅ Completed (Phase 1, 2, 2.5)
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
- Backend TTS/STT endpoints (Edge-TTS, Whisper API)

### ⚠️ Phase 3: Unity 3D Client (Partial Implementation)
**Implemented:**
- **Unity 3D project structure** with all C# scripts
- **WebSocket communication** (Unity native `System.Net.WebSockets.ClientWebSocket`)
- **Placeholder avatar system** - creates a simple 3D figure when no VRM model
- **AvatarManager** with emotion-to-color mapping for placeholder
- **AudioManager** for TTS/STT handling
- **STTClient** HTTP client for Whisper API
- **DebugDisplay** on-screen debug UI for testing
- **WindowController** for transparent windows (Windows DWM API)
- **Editor tools**: Config asset creation, scene setup helpers
- **Integration tests** for Phase 3 components

**Issues Encountered:**
- Docker registry connection fails in WSL2, preventing Qdrant from running
- VRM model not included (user must obtain separately due to licensing)
- Unity UI framework exists but requires manual setup in Editor
- Full end-to-end testing not completed

**Files Added in Phase 3:**
```
frontend/Assets/Scripts/
├── Core/
│   ├── DualisConfig.cs         # ScriptableObject configuration
│   ├── DualisGameManager.cs     # Main coordinator
│   ├── DualisRuntimeInitializer.cs  # Runtime config creation
│   └── WindowController.cs      # Transparent window support
├── Network/
│   ├── WebSocketClient.cs       # WebSocket communication
│   ├── NativeWebSocketClient.cs # Unity native WS impl
│   └── STTClient.cs             # HTTP STT client
├── Audio/
│   └── AudioManager.cs          # TTS/STT handling
├── Avatar/
│   ├── AvatarManager.cs         # VRM control & emotions
│   ├── VRMLoader.cs             # VRM model loading
│   └── PlaceholderAvatar.cs    # Simple 3D placeholder
├── UI/
│   └── DualisUIManager.cs        # Chat UI
├── Debug/
│   └── DebugDisplay.cs          # On-screen debug UI
└── Editor/
    ├── DualisConfigEditor.cs    # Config asset editor
    ├── SceneSetup.cs            # Scene setup utility
    ├── AutoSetupUI.cs           # UI element connection
    └── SceneSetupHelper.cs      # Auto-add debug components
```

### 📋 In Progress
- Memory consolidation and forgetting mechanisms
- Enhanced emotion classification
- Complete Unity UI Canvas setup with full chat interface
- Local Whisper model Python integration (framework exists, not implemented)

### 🚧 Pending
- Unity standalone build packaging
- Transparent window mode testing on actual Windows system
- VRM model selection UI
- Skill versioning and rollback capability
- Docker networking fix for WSL2 environments

---

## Development Phases

### Phase 1: Memory OS ✅ *Completed*
Implemented 4-category hierarchical memory with Qdrant vector storage and non-linear chained retrieval.

### Phase 2: Companion Ability ✅ *Completed*
Emotional AI companion with dual-mode prompts and emotion detection.

### Phase 2.5: Skill Sandbox ✅ *Completed*
Self-evolving skill system with Docker sandbox for secure code execution.

### Phase 3: 3D Figure (Unity) ⚠️ *Partial*
Unity desktop client framework is implemented but has deployment issues:
- Backend API fully functional (TTS, STT, WebSocket)
- Unity scripts complete but require manual Editor setup
- Placeholder avatar works for basic testing
- VRM model loading framework ready but needs actual model files

**Current State:**
- Backend runs successfully without Qdrant (graceful degradation)
- Unity placeholder avatar appears with color-based emotions
- WebSocket connection and chat messaging functional
- TTS audio generation works (Edge-TTS)
- Debug UI displays connection status and messages

**To Test Phase 3:**
1. Start backend: `./start.sh` → option 1 (Quick Start)
2. Open Unity project in Unity Hub
3. Open Main scene and press Play
4. See placeholder avatar and debug UI
5. Click "Send Test Hello" to test chat

---

## Coding Conventions

### Python (Backend)
- Use **async/await** for all I/O operations
- Type hints required for all function signatures
- Docstrings for all public methods (Google style)
- Environment variables for all secrets (use `pydantic-settings`)
- Structured logging with `loguru`

### C# (Frontend)
- Follow Unity conventions for component lifecycle
- Use `[SerializeField]` for Editor-exposed fields
- Event-driven architecture for WebSocket communication
- Reflection-based component detection for optional dependencies (UniVRM)

### File Organization
- One class per file where practical
- Separate business logic from API routes
- Repository pattern for database operations

## Configuration

### Backend (env.sh)
```bash
# LLM Configuration
export LLM_API_KEY="your-api-key-here"
export LLM_BASE_URL="https://api.deepseek.com/v1"
export LLM_MODEL="deepseek-chat"

# Qdrant (optional - graceful degradation if unavailable)
export QDRANT_HOST="localhost"
export QDRANT_PORT=6333

# Sandbox
export SANDBOX_ENABLED="true"

# TTS/STT
export TTS_ENABLED="true"
export STT_ENABLED="false"  # Requires API key
```

### Unity (DualisConfig)
Create via `ProjectDualis → Create Config Asset` in Unity Editor:
- `backendUrl`: `ws://localhost:8000/ws`
- `apiUrl`: `http://localhost:8000/api/v1`
- `enableTTS`: true
- `enableSTT`: false (requires Whisper API)
- `enableLipSync`: true
- `defaultVRMModel`: Path to VRM in Resources (empty for placeholder)

## WebSocket Protocol

### Client → Server
```json
{"type": "chat", "data": {"message": "Hello", "use_memory": true}}
{"type": "mode_switch", "data": {"mode": "assistant"}}
{"type": "ping", "data": {}}
```

### Server → Client
```json
{"type": "chat_response", "data": {"message": "...", "emotion": {...}, "audio_base64": "..."}}
{"type": "emotion", "data": {"primary": "joy", "intensity": 0.8}}
{"type": "state", "data": {"current_mode": "companion", "emotion": {...}}}
```

## Dependencies

### Backend (requirements.txt)
```
fastapi>=0.109.0
uvicorn[standard]>=0.27.0
pydantic>=2.5.0
pydantic-settings>=2.1.0
openai>=1.10.0
httpx>=0.26.0
qdrant-client>=1.7.0
sentence-transformers>=2.3.0
python-dotenv>=1.0.0
python-multipart>=0.0.9
loguru>=0.7.0
docker>=7.0.0
edge-tts>=6.1.0
pytest>=7.4.0
pytest-asyncio>=0.23.0
```

### Unity (Packages/manifest.json)
- UniVRM 2.1.0 (for VRM model loading - optional)
- TextMeshPro (for UI)
- Newtonsoft.Json (for JSON serialization)

## VRAM Constraint
Total system must stay under 12GB VRAM:
- Backend: Qdrant + embeddings (~2GB)
- Unity client: Optimized 3D rendering

## Testing Strategy
- Unit tests for memory operations
- Integration tests for API endpoints
- Sandbox isolation tests for code execution
- Phase 3 integration tests (TTS, STT, WebSocket)

## Known Issues & Workarounds

### Docker in WSL2
**Issue**: Cannot pull images from Docker registry
```
Error: failed to resolve reference "docker.io/qdrant/qdrant:latest"
```
**Workaround**: Run backend without Qdrant - graceful degradation mode disables memory features but keeps chat working.

### Unity VRM Models
**Issue**: No VRM model included due to licensing
**Workaround**: Placeholder avatar system creates simple 3D figure automatically

### Scene Setup
**Issue**: Requires manual Unity Editor configuration
**Workaround**: Run editor scripts: `ProjectDualis → Setup Main Scene` → `Auto-Connect UI Elements`

## Quick Reference

### Starting the System
```bash
# Backend only (no Qdrant - memory disabled)
./start.sh  # Option 1

# With Qdrant (if Docker works)
./start.sh  # Option 3
```

### Testing Chat
```bash
# Via API
curl -X POST http://localhost:8000/api/v1/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello!", "use_memory": false}'

# Via Unity
# Press Play → Click "Send Test Hello" button in debug UI
```

### Viewing Logs
```bash
./start.sh  # Option 5 (View Backend Logs)
# Or directly: tail -f logs/backend.log
```
