using System;
using UnityEngine;
using ProjectDualis.Network;
using ProjectDualis.Audio;
using ProjectDualis.Avatar;
using ProjectDualis.UI;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Core
{
    /// <summary>
    /// Main game manager for Project Dualis Unity client.
    /// Coordinates all subsystems and manages application lifecycle.
    /// </summary>
    public class DualisGameManager : MonoBehaviour
    {
        private static DualisGameManager _instance;
        public static DualisGameManager Instance => _instance;

        [Header("Configuration")]
        [SerializeField] private DualisConfig config;

        // Subsystem references
        public WebSocketClient WebSocket { get; private set; }
        public AudioManager AudioManager { get; private set; }
        public AvatarManager AvatarManager { get; private set; }
        public STTClient STTClient { get; private set; }
        public WindowController WindowController { get; private set; }
        public DualisUIManager UI { get; private set; }

        // State
        public bool IsInitialized { get; private set; }
        public bool IsConnected => WebSocket != null && WebSocket.IsConnected;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            Debug.Log("[Dualis] Initializing Project Dualis...");

            // Load configuration
            if (config == null)
            {
                config = Resources.Load<DualisConfig>("DualisConfig");
                if (config == null)
                {
                    // Create runtime default config
                    Debug.LogWarning("[Dualis] No config found. Creating default runtime config.");
                    config = ScriptableObject.CreateInstance<DualisConfig>();
                    config.name = "DualisConfig (Runtime Default)";

                    // Set default values via reflection
                    var backendUrlField = typeof(DualisConfig).GetField("backendUrl",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (backendUrlField != null) backendUrlField.SetValue(config, "ws://localhost:8000/ws");

                    var apiUrlField = typeof(DualisConfig).GetField("apiUrl",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (apiUrlField != null) apiUrlField.SetValue(config, "http://localhost:8000/api/v1");

                    var enableTTSField = typeof(DualisConfig).GetField("enableTTS",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (enableTTSField != null) enableTTSField.SetValue(config, true);

                    var enableSTTField = typeof(DualisConfig).GetField("enableSTT",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (enableSTTField != null) enableSTTField.SetValue(config, false); // STT off by default

                    Debug.Log("[Dualis] Using default config: ws://localhost:8000/ws");
                }
            }

            // Initialize subsystems
            WebSocket = gameObject.AddComponent<WebSocketClient>();
            WebSocket.Initialize(config);

            // Subscribe to WebSocket events
            WebSocket.OnChatResponse += HandleChatResponse;
            WebSocket.OnEmotionUpdate += HandleEmotionUpdate;

            AudioManager = gameObject.AddComponent<AudioManager>();
            AudioManager.Initialize(config);

            AvatarManager = gameObject.AddComponent<AvatarManager>();
            AvatarManager.Initialize(config);

            STTClient = gameObject.AddComponent<STTClient>();
            STTClient.Initialize(config);

            WindowController = gameObject.AddComponent<WindowController>();
            WindowController.Initialize(config);

            // Add UI manager if exists in scene
            var uiManager = FindObjectOfType<DualisUIManager>();
            if (uiManager != null)
            {
                UI = uiManager;
            }

            IsInitialized = true;
            Debug.Log("[Dualis] Initialization complete.");
        }

        private void Update()
        {
            // Update subsystems
            WebSocket?.Update();
            AudioManager?.Update();
        }

        private void OnDestroy()
        {
            Debug.Log("[Dualis] Shutting down...");

            WebSocket?.Disconnect();
            AudioManager?.Cleanup();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // Re-apply window settings when resuming
                if (WindowController != null)
                {
                    // Window settings are re-applied in Update
                }
            }
        }

        private void OnApplicationQuit()
        {
            WebSocket?.Disconnect();
        }

        /// <summary>
        /// Send a chat message to the backend.
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Dualis] Cannot send message: Not connected to backend.");
                return;
            }

            WebSocket.SendChatMessage(message);
        }

        /// <summary>
        /// Switch AI mode (Companion/Assistant).
        /// </summary>
        public void SwitchMode(string mode)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Dualis] Cannot switch mode: Not connected to backend.");
                return;
            }

            WebSocket.SendModeSwitch(mode);
        }

        /// <summary>
        /// Handle chat response from backend.
        /// </summary>
        private void HandleChatResponse(Network.ChatResponse response)
        {
            Debug.Log($"[Dualis] Received: {response.message}");

            // Update avatar emotion
            if (response.emotion != null)
            {
                var avatarEmotion = Avatar.AvatarManager.ParseEmotion(response.emotion.primary);
                AvatarManager?.SetEmotion(avatarEmotion, response.emotion.intensity);
            }

            // Play TTS audio if available
            if (!string.IsNullOrEmpty(response.audio_base64))
            {
                AudioManager?.PlayTTSFromBase64(response.audio_base64);
            }
        }

        /// <summary>
        /// Handle emotion update from backend.
        /// </summary>
        private void HandleEmotionUpdate(Network.EmotionData emotion)
        {
            var avatarEmotion = Avatar.AvatarManager.ParseEmotion(emotion.primary);
            AvatarManager?.SetEmotion(avatarEmotion, emotion.intensity);
        }
    }
}
