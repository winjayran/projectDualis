# Project Dualis - Phase 3 Status Report

**Last Updated**: 2026-03-23

## Overview

Phase 3 (Unity 3D Desktop Client) focuses on creating a desktop application that provides the AI with a visual avatar, voice interaction capabilities, and real-time communication with the Python backend.

---

## ✅ Completed Work

### Backend Enhancements

#### New API Modules

**`app/api/tts.py` - Text-to-Speech Service**
- Edge-TTS integration for MP3 audio generation
- Multiple voice options (male/female, English/Japanese)
- Streaming endpoint for real-time audio delivery
- Base64 encoding support for WebSocket transmission
- Voice listing endpoint

**`app/api/stt.py` - Speech-to-Text Service**
- Whisper API integration via OpenAI-compatible endpoints
- Local Whisper model support (with `openai-whisper` package)
- Audio file upload handling (mp3, wav, m4a, etc.)
- Model size selection (tiny, base, small, medium, large)

**`app/api/websocket.py` - Real-time Communication**
- WebSocket connection manager for Unity clients
- Chat message handling with TTS audio streaming
- Mode switching support
- Emotion update broadcasting
- Automatic TTS generation with chat responses
- Ping/pong heartbeat support

#### WebSocket Protocol

**Client → Server Messages:**
- `chat` - Send chat message (includes session_id, use_memory flag)
- `mode_switch` - Switch between Companion/Assistant modes
- `ping` - Connection heartbeat
- `get_state` - Request current AI state

**Server → Client Messages:**
- `chat_response` - AI response with emotion, TTS audio (base64), tokens
- `emotion` - Emotion update for avatar expression
- `state` - Current mode and emotion state
- `pong` - Heartbeat response
- `error` - Error messages

### Unity Frontend

#### Core System Scripts

**`Assets/Scripts/Core/DualisConfig.cs`**
- ScriptableObject-based configuration
- Settings: backend URLs, WebSocket parameters, TTS/STT flags, VRM paths
- Editor-assignable for easy configuration

**`Assets/Scripts/Core/DualisGameManager.cs`**
- Main coordinator for all subsystems
- Singleton pattern with DontDestroyOnLoad
- Event subscription to WebSocket messages
- Automatic TTS playback on chat response
- Automatic avatar emotion updates
- Integrated STT client and window controller

**`Assets/Scripts/Core/WindowController.cs`**
- Windows DWM API integration for transparent windows
- Always-on-top functionality
- Platform detection for transparent window support
- Runtime window settings application

#### Network Layer

**`Assets/Scripts/Network/WebSocketClient.cs`**
- WebSocket client for backend communication
- Event-driven architecture (OnConnected, OnChatResponse, OnEmotionUpdate, etc.)
- Automatic reconnection with configurable interval
- JSON message serialization with Newtonsoft.Json

**`Assets/Scripts/Network/NativeWebSocketClient.cs`**
- Unity native WebSocket implementation
- Fallback when NativeWebSocket package unavailable
- Uses `System.Net.WebSockets.ClientWebSocket`
- Async receive loop with proper exception handling

**`Assets/Scripts/Network/STTClient.cs`**
- HTTP client for Whisper STT API
- Multipart form data upload for audio files
- Support for both file path and byte array input
- Model listing endpoint support
- Event-driven transcription results

#### Avatar System

**`Assets/Scripts/Avatar/AvatarManager.cs`**
- VRM avatar loading and control
- Emotion-to-blend-shape mapping (Neutral, Joy, Sadness, Anger, etc.)
- Lip-sync value processing for mouth blend shapes (A, I, U, E, O)
- UniVRM BlendShapeProxy integration (reflection-based)
- Animation and look-at control

**`Assets/Scripts/Avatar/VRMLoader.cs`**
- VRM model loading from byte arrays
- UniVRM VRMImporterContext integration (reflection-based)
- VRM10Loader support for UniVRM 1.x
- Resources folder loading support
- Placeholder model generation when VRM unavailable
- Model setup with animator and blend shape proxy

**Emotion Mapping:**
| Emotion | Blend Shapes |
|---------|--------------|
| Neutral | Neutral (1.0) |
| Joy | Fun (1.0), A (0.3) |
| Sadness | Sorrow (1.0), Blink (0.5) |
| Anger | Angry (1.0), BrowL/R (0.5) |
| Surprise | Surprise (1.0), E (0.3) |
| Love | Joy (0.8), Blink_L/R (0.3) |
| Excitement | Fun (1.0), I (0.5), U (0.3) |

#### Audio System

**`Assets/Scripts/Audio/AudioManager.cs`**
- TTS audio playback from base64 encoded MP3
- Microphone recording for STT
- Voice activity detection (VAD)
- Silence detection for automatic recording stop
- Lip-sync data extraction from audio
- Convert bytes to AudioClip for Unity
- Automatic lip-sync update during playback

#### UI System

**`Assets/Scripts/UI/DualisUIManager.cs`**
- Chat input field with send button
- Mode toggle (Companion/Assistant)
- Voice recording button with STT integration
- Connection status indicator
- Emotion indicator color display
- Message display with formatting
- Event-driven UI updates from backend messages

#### Editor Tools

**`Assets/Scripts/Editor/DualisConfigEditor.cs`**
- Editor window for creating config assets
- Custom inspector for DualisConfig
- Quick test connection button
- Reset to defaults functionality

**`Assets/Scripts/Editor/SceneSetup.cs`**
- Automated scene setup via menu
- Creates complete UI hierarchy with Canvas
- Chat panel with scrollable display
- Input field and buttons
- Status bar with indicators
- Settings panel (hidden by default)

**`Assets/Scripts/Editor/DualisIntegrationTest.cs`**
- Unit tests for core components
- Runtime integration tests
- WebSocket connection testing

#### Project Structure

```
frontend/Assets/
├── Scripts/
│   ├── Core/
│   │   ├── DualisConfig.cs         # Configuration asset
│   │   ├── DualisGameManager.cs     # Main coordinator
│   │   └── WindowController.cs      # Transparent window support
│   ├── Network/
│   │   ├── WebSocketClient.cs       # WebSocket communication
│   │   ├── NativeWebSocketClient.cs # Unity native WS impl
│   │   └── STTClient.cs             # HTTP STT client
│   ├── Audio/
│   │   └── AudioManager.cs          # TTS/STT handling
│   ├── Avatar/
│   │   ├── AvatarManager.cs         # VRM control & emotions
│   │   └── VRMLoader.cs             # VRM model loading
│   ├── UI/
│   │   └── DualisUIManager.cs        # Chat UI
│   └── Editor/
│       ├── DualisConfigEditor.cs    # Config asset editor
│       ├── SceneSetup.cs            # Scene setup utility
│       └── DualisIntegrationTest.cs # Integration tests
├── Resources/
│   └── Models/                      # VRM models go here
├── Scenes/
│   └── Main.unity                   # Main scene
└── Packages/
    └── manifest.json                # Package dependencies
```

---

## 🚧 Remaining Work

### High Priority

#### 1. Unity Scene UI Setup
**Status**: Script complete, needs manual execution in Unity Editor

**Required Steps:**
- [ ] Open Unity project
- [ ] Navigate to `ProjectDualis/Setup Main Scene` in menu bar
- [ ] Run the setup to create UI hierarchy
- [ ] Connect UI elements to DualisUIManager component
- [ ] Create DualisConfig asset via `ProjectDualis/Create Config Asset`

#### 2. Transparent Window Mode
**Status**: Implementation complete, needs testing

**Windows Implementation:**
- [ ] Test on Windows with DWM API
- [ ] Verify transparency works correctly
- [ ] Test always-on-top functionality
- [ ] Ensure background clears to transparent color

**Linux Implementation:**
- [ ] Consider X11 compositing or special window managers
- [ ] Consider OBS Studio browser source as alternative

**macOS Implementation:**
- [ ] Use `NSWindow` with `opaque = false`
- [ ] Requires native plugin (not yet implemented)

#### 3. VRM Model Integration
**Status**: Framework complete, needs actual VRM model

**Required:**
- [ ] Download or create a VRM model
- [ ] Place in `Assets/Resources/Models/`
- [ ] Update DualisConfig defaultVRMModel path
- [ ] Test model loading and emotions

#### 4. STT Integration Testing
**Status**: Client complete, needs backend testing

**Required:**
- [ ] Test Whisper API with actual audio files
- [ ] Verify transcription accuracy
- [ ] Test local Whisper model if desired
- [ ] Verify audio recording quality

### Medium Priority

#### 5. Enhanced Lip Sync
**Status**: Basic implementation complete

**Enhancements:**
- [ ] Use uLipSync library for more accurate lip sync
- [ ] Phoneme-based mouth shapes
- [ ] Viseme blending for smoother transitions
- [ ] Voice/activity detection improvements

#### 6. Avatar Animations
**Status**: Framework complete, no animations yet

**Required:**
- [ ] Idle animations
- [ ] Talking gesture animations
- [ ] Emotion-specific animations
- [ ] Breathing/blinking automation

#### 7. Settings UI
**Status**: Panel created, needs implementation

**Required:**
- [ ] Connect settings panel to actual config
- [ ] Implement settings save/load
- [ ] Add edit capability for all config fields
- [ ] Apply changes at runtime

### Low Priority

#### 8. Audio Quality Improvements
**Status**: Basic MP3 playback

**Enhancements:**
- [ ] Audio format conversion (MP3 → WAV for better quality)
- [ ] Audio normalization
- [ ] Echo cancellation for voice input
- [ ] Noise reduction

#### 9. Connection Quality Indicators
**Status**: Basic logging

**Enhancements:**
- [ ] Ping/latency display
- [ ] Connection strength indicator
- [ ] Reconnection progress
- [ ] Error messages to user

---

## 📦 Dependencies to Install

### Backend
```bash
# Already in requirements.txt
pip install edge-tts>=6.1.0

# Optional for local STT
pip install openai-whisper
```

### Unity Packages

Install via Package Manager or manifest.json:

1. **UniVRM** (VRM model loading)
   ```
   https://github.com/vrm-c/UniVRM.git?path=Assets/UniVRM#v2.1.0
   ```

2. **TextMeshPro** (Advanced text rendering)
   - Included with Unity 2022.3+

3. **Newtonsoft.Json** (JSON serialization)
   ```
   com.unity.nuget.newtonsoft-json@3.2.1
   ```

---

## 🔧 Configuration Files

### Backend Environment Variables (env.sh)
```bash
# Existing variables plus:
export TTS_ENABLED="true"
export TTS_DEFAULT_VOICE="en-US-JennyNeural"
export TTS_RATE="+0%"
export TTS_PITCH="+0Hz"
export STT_ENABLED="true"
export STT_MODEL="base"  # For local Whisper
```

### Unity Configuration (DualisConfig)
Create at `Resources/DualisConfig.asset` via editor:
- `backendUrl`: `ws://localhost:8000/ws`
- `apiUrl`: `http://localhost:8000/api/v1`
- `enableTTS`: true
- `enableSTT`: true
- `enableLipSync`: true
- `defaultVRMModel`: Path to VRM in Resources
- `transparentBackground`: true (Windows only)
- `alwaysOnTop`: true (Windows only)

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

### Unity Tests
1. **Editor Tests:**
   - Open Unity Test Runner (Window > General > Test Runner)
   - Run `ProjectDualis.Editor.DualisIntegrationTest`
   - Verify all unit tests pass

2. **Runtime Tests:**
   - Add `DualisRuntimeTest` component to a GameObject
   - Press Play
   - Check console for connection status
   - WebSocket should auto-connect to backend

3. **Manual Testing:**
   - Open Main scene
   - Press Play
   - Run `ProjectDualis/Setup Main Scene` (first time only)
   - Check console for connection status
   - WebSocket should auto-connect to backend

---

## 📝 Testing Checklist

Before marking Phase 3 complete, verify:

- [ ] Backend server starts without errors
- [ ] Qdrant is running (or graceful degradation works)
- [ ] WebSocket endpoint accepts connections
- [ ] Chat messages flow from Unity → Backend → Unity
- [ ] TTS audio generates and plays in Unity
- [ ] Avatar displays emotions based on responses
- [ ] Mode switching works (Companion ↔ Assistant)
- [ ] Voice recording captures audio
- [ ] STT transcribes audio to text (if enabled)
- [ ] Transparent window works (Windows)
- [ ] VRM model loads and displays
- [ ] Lip-sync animates during TTS playback

---

## 🎯 Next Steps

### Immediate:
1. **Unity Editor Setup** - Run scene setup script and create config asset
2. **Backend Testing** - Run integration tests
3. **End-to-End Test** - Test full conversation flow with TTS

### Short-term:
1. Obtain/test with actual VRM model
2. Implement settings panel functionality
3. Add error handling UI for user feedback

### Long-term:
1. Package standalone build
2. Performance profiling
3. Enhanced animations and lip-sync

---

## 📝 Notes for Contributors

1. **UniVRM Integration**: The VRMLoader uses reflection to detect UniVRM at runtime, allowing the project to work without the package installed (with placeholder model).

2. **WebSocket Fallback**: NativeWebSocketClient uses Unity's built-in `System.Net.WebSockets.ClientWebSocket`, avoiding external package dependencies.

3. **TTS Audio Format**: Edge-TTS generates MP3 format. Unity doesn't natively decode MP3 from bytes - current implementation uses placeholder conversion. For production, use NAudio package or convert to WAV in backend.

4. **Threading**: WebSocket receive loop runs on background thread. Unity API calls must be dispatched to main thread using coroutine-based queuing.

5. **Memory Management**: VRM models can be large (10-50MB). Ensure proper cleanup when unloading models.

6. **Platform Support**: Transparent window mode is Windows-only via DWM API. Linux and macOS require native plugins.

---

*For questions or clarifications, refer to `CLAUDE.md` or open an issue.*
