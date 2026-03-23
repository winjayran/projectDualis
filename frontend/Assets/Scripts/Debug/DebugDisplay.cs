using UnityEngine;
using ProjectDualis.Core;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.DebugUI
{
    /// <summary>
    /// Simple on-screen debug display for testing.
    /// Shows connection status, recent messages, and emotion.
    /// </summary>
    public class DebugDisplay : MonoBehaviour
    {
        private DualisGameManager gameManager;
        private string statusMessage = "Initializing...";
        private string lastChatMessage = "";
        private string currentEmotion = "Neutral";
        private string connectionStatus = "Disconnected";
        private Color statusColor = Color.red;

        private Vector2 scrollPosition;
        private readonly System.Collections.Generic.List<string> messageLog = new System.Collections.Generic.List<string>();

        private void Start()
        {
            gameManager = DualisGameManager.Instance;

            if (gameManager != null)
            {
                gameManager.WebSocket.OnConnected += (url) =>
                {
                    connectionStatus = "Connected";
                    statusColor = Color.green;
                    AddLog("Connected to " + url);
                };

                gameManager.WebSocket.OnDisconnected += (reason) =>
                {
                    connectionStatus = "Disconnected: " + reason;
                    statusColor = Color.red;
                    AddLog("Disconnected: " + reason);
                };

                gameManager.WebSocket.OnChatResponse += (response) =>
                {
                    lastChatMessage = response.message;
                    if (response.emotion != null)
                    {
                        currentEmotion = response.emotion.primary;
                    }
                    AddLog("AI: " + response.message);
                };

                gameManager.WebSocket.OnError += (error) =>
                {
                    AddLog("Error: " + error);
                };
            }

            AddLog("Debug Display initialized");
        }

        private void Update()
        {
            if (gameManager != null && gameManager.IsConnected)
            {
                statusMessage = "Connected - Ready to chat";
            }
            else
            {
                statusMessage = "Connecting...";
            }
        }

        private void AddLog(string message)
        {
            messageLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            if (messageLog.Count > 50)
                messageLog.RemoveAt(0);
        }

        private void OnGUI()
        {
            // Status box
            GUI.Box(new Rect(10, 10, 300, 150), "Project Dualis - Debug");

            // Connection status
            GUI.color = statusColor;
            GUI.Label(new Rect(20, 35, 280, 20), $"● {connectionStatus}");
            GUI.color = Color.white;

            // Status message
            GUI.Label(new Rect(20, 55, 280, 20), statusMessage);

            // Current emotion
            GUI.Label(new Rect(20, 75, 280, 20), $"Emotion: {currentEmotion}");

            // Last message
            GUI.Label(new Rect(20, 95, 280, 40), "Last: " + lastChatMessage);

            // Quick test button
            if (GUI.Button(new Rect(20, 115, 120, 30), "Send Test Hello"))
            {
                SendTestMessage();
            }

            // Message log
            GUI.Box(new Rect(10, 170, 400, 300), "Message Log");
            scrollPosition = GUI.BeginScrollView(new Rect(20, 195, 380, 270), scrollPosition,
                new Rect(0, 0, 360, messageLog.Count * 20));

            for (int i = 0; i < messageLog.Count; i++)
            {
                GUI.Label(new Rect(0, i * 20, 360, 20), messageLog[i]);
            }

            GUI.EndScrollView();
        }

        private void SendTestMessage()
        {
            if (gameManager != null && gameManager.IsConnected)
            {
                gameManager.SendChatMessage("Hello, this is a test message!");
                AddLog("You: Hello, this is a test message!");
            }
            else
            {
                AddLog("Cannot send: Not connected");
            }
        }
    }
}
