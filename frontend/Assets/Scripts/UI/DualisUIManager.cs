using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectDualis.Core;

namespace ProjectDualis.UI
{
    /// <summary>
    /// Main UI manager for Project Dualis Unity client.
    /// Handles chat input, status display, and user interactions.
    /// </summary>
    public class DualisUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private InputField chatInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private TextMeshProUGUI chatDisplay; // Requires TextMesh Pro
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Toggle modeToggle;
        [SerializeField] private Button recordButton;

        [Header("Status Indicators")]
        [SerializeField] private Image connectionStatusImage;
        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color disconnectedColor = Color.red;
        [SerializeField] private Image emotionIndicator;

        private DualisGameManager gameManager;
        private bool isCompanionMode = true;

        private void Awake()
        {
            gameManager = DualisGameManager.Instance;

            // Setup UI event handlers
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendButtonClicked);

            if (chatInputField != null)
                chatInputField.onSubmit.AddListener(OnChatSubmitted);

            if (modeToggle != null)
                modeToggle.onValueChanged.AddListener(OnModeToggleChanged);

            if (recordButton != null)
                recordButton.onClick.AddListener(OnRecordButtonClicked);

            // Subscribe to game manager events
            if (gameManager.WebSocket != null)
            {
                gameManager.WebSocket.OnConnected += HandleConnected;
                gameManager.WebSocket.OnDisconnected += HandleDisconnected;
                gameManager.WebSocket.OnChatResponse += HandleChatResponse;
                gameManager.WebSocket.OnEmotionUpdate += HandleEmotionUpdate;
            }

            // Subscribe to STT events
            if (gameManager.STTClient != null)
            {
                gameManager.STTClient.OnTranscriptionComplete += HandleTranscriptionComplete;
                gameManager.STTClient.OnTranscriptionError += HandleTranscriptionError;
            }
        }

        private void Start()
        {
            UpdateConnectionStatus();
            UpdateModeDisplay();
        }

        private void OnDestroy()
        {
            if (gameManager.WebSocket != null)
            {
                gameManager.WebSocket.OnConnected -= HandleConnected;
                gameManager.WebSocket.OnDisconnected -= HandleDisconnected;
                gameManager.WebSocket.OnChatResponse -= HandleChatResponse;
                gameManager.WebSocket.OnEmotionUpdate -= HandleEmotionUpdate;
            }

            if (gameManager.STTClient != null)
            {
                gameManager.STTClient.OnTranscriptionComplete -= HandleTranscriptionComplete;
                gameManager.STTClient.OnTranscriptionError -= HandleTranscriptionError;
            }
        }

        private void OnSendButtonClicked()
        {
            SendChatMessage();
        }

        private void OnChatSubmitted(string text)
        {
            SendChatMessage();
        }

        private void SendChatMessage()
        {
            if (chatInputField == null) return;

            string message = chatInputField.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            gameManager.SendChatMessage(message);
            AddMessageToDisplay("You", message);
            chatInputField.text = "";
        }

        private void OnModeToggleChanged(bool isCompanion)
        {
            isCompanionMode = isCompanion;
            string mode = isCompanion ? "companion" : "assistant";
            gameManager.SwitchMode(mode);
            UpdateModeDisplay();
        }

        private void OnRecordButtonClicked()
        {
            var audioManager = gameManager.AudioManager;
            if (audioManager == null) return;

            if (audioManager.IsRecording)
            {
                byte[] audioData = audioManager.StopRecording();
                SetRecordButtonState(false);

                // Send to STT endpoint
                if (audioData != null && audioData.Length > 0 && gameManager.STTClient != null)
                {
                    gameManager.STTClient.Transcribe(audioData);
                }
            }
            else
            {
                audioManager.StartRecording(30);
                SetRecordButtonState(true);
            }
        }

        private void SetRecordButtonState(bool isRecording)
        {
            if (recordButton == null) return;

            var text = recordButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = isRecording ? "Stop Recording" : "Voice Input";
            }

            var image = recordButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = isRecording ? Color.red : Color.white;
            }
        }

        private void HandleConnected(string url)
        {
            UpdateConnectionStatus();
            SetStatusText($"Connected to {url}");
        }

        private void HandleDisconnected(string reason)
        {
            UpdateConnectionStatus();
            SetStatusText($"Disconnected: {reason}");
        }

        private void HandleChatResponse(Network.ChatResponse response)
        {
            AddMessageToDisplay("Dualis", response.message);
            UpdateEmotionIndicator(response.emotion);
        }

        private void HandleEmotionUpdate(Network.EmotionData emotion)
        {
            UpdateEmotionIndicator(emotion);
        }

        private void AddMessageToDisplay(string sender, string message)
        {
            if (chatDisplay == null) return;

            string formattedMessage = $"<b>{sender}:</b> {message}\n";
            chatDisplay.text += formattedMessage;
        }

        private void UpdateConnectionStatus()
        {
            if (connectionStatusImage == null) return;

            bool isConnected = gameManager.IsConnected;
            connectionStatusImage.color = isConnected ? connectedColor : disconnectedColor;
        }

        private void UpdateModeDisplay()
        {
            if (modeToggle != null)
            {
                modeToggle.isOn = isCompanionMode;
            }

            SetStatusText($"Mode: {(isCompanionMode ? "Companion" : "Assistant")}");
        }

        private void UpdateEmotionIndicator(Network.EmotionData emotion)
        {
            if (emotionIndicator == null) return;

            // Update avatar emotion
            var avatarManager = gameManager.AvatarManager;
            if (avatarManager != null)
            {
                var avatarEmotion = Avatar.AvatarManager.ParseEmotion(emotion.primary);
                avatarManager.SetEmotion(avatarEmotion, emotion.intensity);
            }

            // Update UI indicator color based on emotion
            emotionIndicator.color = GetEmotionColor(emotion.primary);
        }

        private Color GetEmotionColor(string emotion)
        {
            switch (emotion?.ToLower())
            {
                case "joy":
                case "excitement":
                    return Color.yellow;
                case "sadness":
                    return Color.blue;
                case "anger":
                    return Color.red;
                case "love":
                    return Color.magenta;
                case "neutral":
                default:
                    return Color.white;
            }
        }

        private void SetStatusText(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
        }

        private void HandleTranscriptionComplete(string transcription)
        {
            // Fill chat input with transcription
            if (chatInputField != null)
            {
                chatInputField.text = transcription;
            }

            SetStatusText($"Transcribed: {transcription}");
        }

        private void HandleTranscriptionError(string error)
        {
            SetStatusText($"STT Error: {error}");
            Debug.LogError($"[UI] Transcription error: {error}");
        }
    }
}
