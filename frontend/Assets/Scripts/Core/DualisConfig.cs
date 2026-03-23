using UnityEngine;

namespace ProjectDualis.Core
{
    /// <summary>
    /// Central configuration for Project Dualis Unity client.
    /// Configured via ScriptableObject in Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "DualisConfig", menuName = "ProjectDualis/Config")]
    public class DualisConfig : ScriptableObject
    {
        [Header("Backend Connection")]
        [Tooltip("WebSocket server URL for Python backend")]
        public string backendUrl = "ws://localhost:8000/ws";

        [Tooltip("HTTP API URL for REST endpoints")]
        public string apiUrl = "http://localhost:8000/api/v1";

        [Tooltip("Connection timeout in seconds")]
        public float connectionTimeout = 10f;

        [Tooltip("Reconnection attempt interval")]
        public float reconnectInterval = 3f;

        [Header("Audio Settings")]
        [Tooltip("Text-to-Speech enabled")]
        public bool enableTTS = true;

        [Tooltip("Speech-to-Text enabled")]
        public bool enableSTT = true;

        [Tooltip("Audio output sample rate")]
        public int sampleRate = 24000;

        [Tooltip("Microphone input device (empty for default)")]
        public string microphoneDevice = "";

        [Header("Avatar Settings")]
        [Tooltip("Default VRM model path in Resources")]
        public string defaultVRMModel = "Models/Avatar";

        [Tooltip("Enable lip sync")]
        public bool enableLipSync = true;

        [Tooltip("Lip sync sensitivity")]
        [Range(0.1f, 2f)]
        public float lipSyncSensitivity = 1f;

        [Header("Display Settings")]
        [Tooltip("Transparent background for desktop overlay")]
        public bool transparentBackground = true;

        [Tooltip("Always on top")]
        public bool alwaysOnTop = true;

        [Tooltip("Window scale")]
        [Range(0.5f, 2f)]
        public float windowScale = 1f;
    }
}
