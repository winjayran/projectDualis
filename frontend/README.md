# Project Dualis - Unity Frontend (Phase 3)

This is the Unity 3D desktop client for Project Dualis, providing a virtual avatar with speech capabilities and real-time interaction with the Python backend.

## Features

- **VRM/VTuber Avatar Support**: Load and display VRM 3D models with full animation support
- **Real-time Lip Sync**: Automatic lip synchronization with TTS audio output
- **WebSocket Communication**: Low-latency bidirectional communication with backend
- **TTS/STT Integration**: Text-to-speech and speech-to-text for natural voice interaction
- **Emotion Display**: Avatar expressions change based on AI emotion state
- **Desktop Overlay**: Transparent background mode for desktop companion experience

## Prerequisites

- Unity 2022.3 LTS or later
- Windows 10/11 or macOS 12+
- Backend server running (see `/backend`)

## Installation

### 1. Unity Package Installation

Open this project in Unity Editor. The required packages are defined in `Packages/manifest.json`:

- **UniVRM** (`com.vrmc.univrm`): VRM model loading and control
- **TextMeshPro** (`com.unity.textmeshpro`): Advanced text rendering

### 2. Install UniVRM

UniVRM is required for VRM model support. Install via:

1. Window > Package Manager
2. Click "+" > "Add package from git URL"
3. Enter: `https://github.com/vrm-c/UniVRM.git?path=Assets/UniVRM`

### 3. Install WebSocket Library

For WebSocket communication, we recommend NativeWebSocket:

1. Window > Package Manager
2. Click "+" > "Add package from git URL"
3. Enter: `https://github.com/endel/NativeWebSocket.git#upm`

### 4. Install Newtonsoft.JSON

Required for JSON serialization:

1. Window > Package Manager
2. Click "+" > "Add package by name"
3. Enter: `Newtonsoft.Json`

## Configuration

1. Create a configuration asset:
   - Right-click in Project window > Create > ProjectDualis > Config
   - Name it "DualisConfig"
   - Place in `Resources/` folder

2. Configure settings in Inspector:
   - **Backend URL**: WebSocket server address (default: `ws://localhost:8000/ws`)
   - **API URL**: REST API endpoint (default: `http://localhost:8000/api/v1`)
   - **TTS/STT**: Enable/disable text-to-speech and speech-to-text
   - **Avatar**: Default VRM model path
   - **Display**: Transparent background, always on top

## Usage

### Running in Unity Editor

1. Start the backend server (see `/backend/README.md`)
2. Open the Main scene (`Assets/Scenes/Main.unity`)
3. Press Play

### Building Standalone

1. File > Build Settings
2. Add Main scene
3. Select target platform (Windows/Mac)
4. Click "Build and Run"

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/              # Core systems (GameManager, Config)
│   ├── Network/           # WebSocket client
│   ├── Audio/             # TTS/STT audio pipeline
│   ├── Avatar/            # VRM avatar control & lip sync
│   └── UI/                # User interface
├── Resources/
│   ├── Models/            # VRM models
│   └── DualisConfig.asset # Configuration asset
└── Scenes/                # Unity scenes
```

## C# Scripts Overview

| Script | Purpose |
|--------|---------|
| `DualisGameManager.cs` | Main coordinator, initializes all subsystems |
| `DualisConfig.cs` | ScriptableObject configuration |
| `WebSocketClient.cs` | WebSocket communication with backend |
| `AvatarManager.cs` | VRM avatar loading and emotion control |
| `AudioManager.cs` | TTS playback and STT recording |
| `DualisUIManager.cs` | Chat UI and status display |

## WebSocket Protocol

### Client → Server

```json
{
  "type": "chat",
  "data": {
    "message": "Hello Dualis!",
    "session_id": "unique-device-id",
    "use_memory": true
  },
  "timestamp": 1640000000000
}
```

### Server → Client

```json
{
  "type": "chat_response",
  "data": {
    "message": "Hello! How can I help you today?",
    "mode": "companion",
    "emotion": {
      "primary": "joy",
      "secondary": null,
      "intensity": 0.8
    },
    "memories_used": ["uuid1", "uuid2"],
    "tokens_used": {"prompt": 150, "completion": 50}
  },
  "timestamp": 1640000001000
}
```

## VRM Model Requirements

For best results, VRM models should include:

- **BlendShapes** for expressions (A, I, U, E, O, Joy, Angry, Sorrow, etc.)
- **SpringBone** for hair/clothing physics
- **LookAt** for eye tracking

Recommended VRM models:
- [VRoid Studio](https://vroid.com/) - Free character creator
- [VRM Marketplace](https://3d.vroid.com/) - Community models

## Troubleshooting

### Connection Failed

- Verify backend server is running
- Check firewall settings
- Ensure correct backend URL in config

### Avatar Not Showing

- Verify VRM model is in `Resources/Models/`
- Check UniVRM package is installed
- Look for import errors in Console

### No Audio

- Check microphone permissions
- Verify TTS is enabled in config
- Test with system audio output

## Development Status

**Phase 3 (Current)**: Unity client implementation - Framework Complete

- [x] WebSocket communication (Unity native implementation)
- [x] VRM model loading framework with UniVRM integration
- [x] Lip sync system with blend shape control
- [x] TTS/STT integration (Edge-TTS backend, WebSocket audio streaming)
- [x] Emotion display on avatar
- [x] Main scene with manager setup
- [ ] Native WebSocket package (optional, Unity native fallback available)
- [ ] Transparent window mode (platform-specific)
- [ ] Whisper STT integration (local model support added)
- [ ] UI implementation (chat interface)

## License

MIT License - See root directory
