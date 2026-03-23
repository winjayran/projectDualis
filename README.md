# 📦 Project Dualis

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Python 3.10+](https://img.shields.io/badge/python-3.10+-blue.svg)](https://www.python.org/downloads/)
[![Unity 3D](https://img.shields.io/badge/Client-Unity-lightgrey.svg)](https://unity.com/)
[![DeepSeek API](https://img.shields.io/badge/LLM-DeepSeek-success.svg)](https://www.deepseek.com/)

> **"More than a tool, beyond a virtual idol. A self-evolving digital entity with persistent memory and a physical presence, growing alongside you."**

**Project Dualis** is an open-source initiative to build a next-generation 3D AI virtual entity. It seamlessly bridges the gap between a deeply empathetic **Emotional Companion** and a highly capable **Personal Assistant**. Built as a standalone Unity desktop application and powered by advanced LLM reasoning (e.g., DeepSeek API), Dualis features a groundbreaking non-linear memory architecture and a safe containerized environment for self-coded skill evolution—all optimized to run under 12GB of VRAM.

---

## 🎯 1. Project Vision

The current AI ecosystem is heavily polarized: we have sterile productivity tools lacking emotional resonance, or role-playing chatbots lacking practical utility and continuous memory.

**Our mission is to build an AI operating system with a physical-like presence that can:**
1. **Solve the Context Window Bottleneck:** By utilizing advanced memory management, the AI can hold a "lifetime" of memories and skills, dynamically retrieving only what is necessary for the current context.
2. **Achieve True Self-Evolution:** Break free from hard-coded tools. The AI can write, test, and deploy its own Python scripts to handle new tasks, expanding its capabilities autonomously.
3. **Provide Immersive Interaction:** Deliver a high-quality 3D avatar living natively on your desktop (via Unity), rendering emotional feedback and lip-syncing in real-time.

---

## ✨ 2. Summary of Core Requirements

This project is meticulously designed around five core pillars:

- 🎭 **Dual-Mode System**
  - **Companion Mode:** Excels at emotional support, empathy, and analyzing shared memories. It acts as a trusted confidant.
  - **Assistant Mode:** A highly logical project builder and task executor. It can learn new skills, manage files, and write code to assist with your daily workflows.

- 🖥️ **Standalone Unity 3D Avatar**
  - Developed as an independent desktop software using the **Unity Engine** for superior rendering quality and performance.
  - Real-time 3D rendering with STT (Speech-to-Text) and TTS (Text-to-Speech) for immersive lip-syncing and expression control.
  - Strictly optimized to keep the entire system's VRAM usage **under 12GB**.

- 🧠 **Deep LLM Integration**
  - Powered by API-driven large language models with exceptional reasoning capabilities (like **DeepSeek API**), ensuring high logical ceilings and low local hardware requirements.

- 📚 **Advanced Memory OS**
  - **Categorized Hierarchical Management:** Memories are classified by type (working context, episodic events, semantic facts, and skill schemas).
  - **Non-Linear Chained Retrieval:** Instead of simple semantic search, the AI uses reasoning to traverse associative chains of memory (e.g., retrieving a past emotional event, which links to a specific project, which links to a required skill), solving the conflict between limited context windows and the need for massive context loading.

- ⚡ **Self-Evolving Skill Sandbox**
  - When encountering a new problem in Assistant Mode, the AI can write custom Python code/plugins.
  - **Safe Execution:** The code is autonomously tested and executed within a secure **Docker or Apptainer sandbox**. Once verified, the AI saves this tool to its permanent skill repository for future use.

---

## 🗺️ 3. Implementation Roadmap

The development of Project Dualis is structured into four main phases:

### 📍 Phase 1: Advanced Memory OS (Backend) ✅ *Completed*
*Goal: Establish the advanced memory architecture with non-linear chained retrieval.*
- [x] Develop the backend framework (Python/FastAPI).
- [x] Design the **Categorized Hierarchical Memory System** (4 categories).
- [x] Implement the **Non-Linear Chained Retrieval** using Qdrant vector database.
- [x] Create embedding service for semantic search.
- [x] Add graceful degradation when Qdrant is unavailable.
- [ ] Complete memory consolidation and forgetting mechanisms.
- [ ] Add memory importance scoring and decay.
- [ ] **Qdrant integration not yet tested** (Docker WSL2 issues)

### 📍 Phase 2: Companion Ability (Backend) ✅ *Completed*
*Goal: Build the emotional AI companion integrated with memory.*
- [x] Integrate OpenAI-compatible API (DeepSeek support).
- [x] Implement dynamic System Prompt routing (Companion vs Assistant modes).
- [x] Create emotion detection and tracking.
- [x] Integrate memory system for personalized responses.
- [ ] Add more sophisticated emotion classification.
- [ ] Implement sentiment-aware memory retrieval.

### 📍 Phase 2.5: Self-Evolving Skill Sandbox (Backend) ✅ *Completed*
*Goal: Enable the AI to write, test, and memorize its own plugins safely.*
- [x] Set up the **Docker** integration for the execution sandbox.
- [x] Provide the AI with meta-tools: `write_code`, `test_in_sandbox`, and `register_skill`.
- [x] Create the dynamic Skill Registry for context-aware tool loading.
- [x] Implement code validation with import whitelist and pattern blocking.
- [ ] Add skill versioning and rollback capability.
- [ ] Implement skill marketplace/sharing between instances.

### 📍 Phase 3: Unity 3D Desktop Client (Frontend) ⚠️ *Partial Implementation*
*Goal: Give the AI a face, a voice, and an independent desktop presence.*

**Implemented:**
- [x] Initialize the Unity 3D project structure with C# scripts.
- [x] Implement WebSocket communication with backend (Unity native + fallback).
- [x] Create VRM/VTuber avatar loading framework with UniVRM integration.
- [x] Implement placeholder avatar system for testing without VRM models.
- [x] Implement lip-sync system framework with blend shape control.
- [x] Set up TTS/STT audio pipeline framework.
- [x] Implement emotion display on avatar (with color changes).
- [x] Add Edge-TTS backend integration (MP3 streaming).
- [x] Add Whisper STT backend support (API + local models).
- [x] Create Main scene with manager setup.
- [x] Implement transparent window mode (Windows DWM API).
- [x] Create Unity UI framework and editor scripts.
- [x] Add STT HTTP client for Unity (Whisper API integration).
- [x] Create comprehensive integration tests (Unity + Python).
- [x] Add editor tools for config asset creation and scene setup.
- [x] **Web-based Chat UI** for easy testing without Unity

**Issues Encountered:**
- Docker registry connection issues in WSL preventing Qdrant from running
- No VRM model included (user needs to download separately)
- Unity scene setup requires manual editor configuration
- Standalone build packaging not completed
- **Qdrant not yet tested** (networking issues in WSL2)

**Remaining Tasks:**
- [ ] Complete Unity UI Canvas setup and element connection
- [ ] Test end-to-end chat flow with actual VRM model
- [ ] Resolve Docker networking issues for WSL2
- [ ] Test Qdrant memory system functionality
- [ ] Package standalone build (requires Unity Editor build process)
- [ ] Test transparent window mode on actual Windows system

**See `PHASE3LEFT.md` for detailed Phase 3 status and remaining tasks.**

### 📍 Phase 4: Optimization & Packaging 📋 *Pending*
*Goal: Ensure efficiency and lower the barrier to entry.*
- [ ] VRAM profiling to ensure total system stays under 12GB.
- [ ] Add optional offline fallback with local quantized models.
- [ ] Package into unified installer for Windows/Linux.

---

## 🚀 Quick Start

### Prerequisites
- Python 3.10+
- OpenAI-compatible API key (DeepSeek recommended)
- Docker (optional, for Qdrant vector database)

### Installation

1. **Clone the repository:**
```bash
git clone https://github.com/yourusername/projectDualis.git
cd projectDualis
```

2. **Configure environment:**
```bash
# Copy the example and add your API key
cp env_example.sh env.sh
# Edit env.sh and set your LLM_API_KEY
```

3. **Source the environment:**
```bash
source env.sh
```

4. **Install Python dependencies:**
```bash
pip install -r backend/requirements.txt
```

5. **Run the backend server:**
```bash
./start.sh
# Select option 1 (Quick Start - Backend only)
```

The backend will start in "graceful degradation" mode without Qdrant - chat, TTS, and emotions will work, but memory features will be disabled.

6. **Access the Web Chat UI:**
- Web Chat: http://localhost:8000 or http://localhost:8000/static/chat.html
- API Documentation: http://localhost:8000/docs
- Health Check: http://localhost:8000/health

### Environment Variables

The `env.sh` file contains all configuration options. Key variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `LLM_API_KEY` | Your DeepSeek/OpenAI API key | *required* |
| `LLM_BASE_URL` | LLM API endpoint | `https://api.deepseek.com/v1` |
| `LLM_MODEL` | Model name | `deepseek-chat` |
| `LLM_MAX_TOKENS` | Max tokens per response (max 8192 for DeepSeek) | `8192` |
| `QDRANT_HOST` | Qdrant host | `localhost` |
| `SANDBOX_ENABLED` | Enable code sandbox | `true` |

---

## 🛠️ Technology Stack
- **AI Core:** OpenAI-compatible API (DeepSeek, OpenAI, etc.)
- **Backend:** Python 3.10+, FastAPI, Pydantic
- **Memory Storage:** Qdrant (Vector DB), Sentence Transformers (embeddings)
- **Frontend / Avatar:** Unity 3D (C#, UniVRM, placeholder system)
- **Web UI:** HTML/CSS/JavaScript with WebSocket support
- **Audio Processing:** Whisper (STT), Edge-TTS (TTS)
- **Sandbox:** Docker / Apptainer (for skill evolution)
- **Communication:** WebSocket (real-time Unity-backend IPC)

---

## 🧪 Testing

### Backend Tests
```bash
# Run all tests
pytest backend/tests/ -v

# Run Phase 3 integration tests specifically
pytest backend/tests/test_phase3_integration.py -v

# Test individual endpoints
curl http://localhost:8000/api/v1/tts/voices
curl http://localhost:8000/api/v1/stt/models
```

### Web Chat UI Test
1. Start backend with `./start.sh` (option 1)
2. Open http://localhost:8000 in your browser
3. Send messages to test the chat interface

### Unity Tests
1. Copy frontend to Windows (Unity doesn't work well with WSL filesystem)
2. Open Unity project in Unity Hub
3. Open Main scene
4. Press Play to test basic functionality
5. Check console for WebSocket connection status

---

## 📝 Known Issues

### ⚠️ Important Limitations
1. **Qdrant Not Tested**: Docker registry connection issues prevent Qdrant from running in WSL2. Memory features have graceful degradation but are not yet tested.
2. **No VRM Model Included**: The project uses VRM format for 3D avatars, but no model is included due to licensing. Users must download their own VRM models.
3. **Unity on WSL**: Unity Editor cannot run directly in WSL. The frontend must be copied to Windows for development.
4. **Transparent Window**: Only implemented for Windows via DWM API; Linux/macOS require native plugins.

### Workarounds
- **No Qdrant**: Backend runs in "graceful degradation" mode - chat works but memory features are disabled
- **No VRM Model**: A placeholder 3D figure (capsule/sphere) is created automatically for testing
- **Web UI**: Use the web chat interface at http://localhost:8000 for quick testing without Unity
- **Unity Development**: Copy `frontend/` folder to Windows filesystem before opening in Unity Hub

---

## 📖 Recent Updates

### Backend (March 2026)
- Added **Web-based Chat UI** at `backend/static/chat.html` for easy testing
- Fixed `LLM_MAX_TOKENS` compatibility with DeepSeek API (changed from 16384 to 8192)
- Added static file serving to FastAPI for web UI access
- Root URL now redirects to the chat interface

### Unity Frontend (March 2026)
- Fixed all namespace conflicts (`ProjectDualis.Debug` → `ProjectDualis.DebugUI`)
- Added `using Debug = UnityEngine.Debug;` alias to all files using Debug class
- Fixed `textmeshpo` typo in manifest.json → `textmeshpro`
- Fixed `DestroyImmediate` calls to use `Object.DestroyImmediate`
- Removed invalid MonoBehaviour components from Main.unity scene
- Added missing Windows API constants (`SWP_NOOWNERZORDER`)
- Fixed nullable bool operator issues in AutoSetupUI.cs
- Added proper using statements for all namespace references
- Created minimal Main.unity scene with just Camera and Light

---

> **Join us in building the next step in human-computer interaction.**
> Feel free to open an Issue or submit a Pull Request!
