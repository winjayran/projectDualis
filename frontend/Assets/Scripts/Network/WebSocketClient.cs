using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using ProjectDualis.Core;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Network
{
    /// <summary>
    /// WebSocket message types for communication with Python backend.
    /// </summary>
    public enum WebSocketMessageType
    {
        Chat,
        Response,
        ModeSwitch,
        StateUpdate,
        Emotion,
        Audio,
        Ping,
        Pong
    }

    /// <summary>
    /// WebSocket message envelope.
    /// </summary>
    [Serializable]
    public class WebSocketMessage
    {
        public string type;
        public string data;
        public long timestamp;

        public WebSocketMessage(string type, string data)
        {
            this.type = type;
            this.data = data;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Chat request message.
    /// </summary>
    [Serializable]
    public class ChatRequest
    {
        public string message;
        public string mode;
        public string session_id;
        public bool use_memory = true;
    }

    /// <summary>
    /// Chat response from backend.
    /// </summary>
    [Serializable]
    public class ChatResponse
    {
        public string message;
        public string mode;
        public EmotionData emotion;
        public string[] memories_used;
        public Dictionary<string, int> tokens_used;
        public string audio_base64;  // Base64 encoded MP3 audio
    }

    /// <summary>
    /// Emotion data from backend.
    /// </summary>
    [Serializable]
    public class EmotionData
    {
        public string primary;
        public string secondary;
        public float intensity;
    }

    /// <summary>
    /// WebSocket client for communication with Python backend.
    /// Uses native WebSocket with fallback for different platforms.
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        private DualisConfig config;
        private WebSocket websocket;
        private Queue<string> messageQueue = new Queue<string>();
        private bool isConnecting = false;
        private float reconnectTimer = 0f;

        // Events
        public event Action<string> OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnMessageReceived;
        public event Action<ChatResponse> OnChatResponse;
        public event Action<EmotionData> OnEmotionUpdate;
        public event Action<string> OnError;

        public bool IsConnected => websocket != null && websocket.ReadyState == WebSocketState.Open;

        public void Initialize(DualisConfig config)
        {
            this.config = config;
            Connect();
        }

        public void Connect()
        {
            if (isConnecting || IsConnected) return;

            isConnecting = true;
            Debug.Log($"[WebSocket] Connecting to {config.backendUrl}...");

            try
            {
                websocket = new WebSocket(config.backendUrl);

                websocket.OnOpen += () =>
                {
                    isConnecting = false;
                    Debug.Log("[WebSocket] Connected to backend.");
                    OnConnected?.Invoke(config.backendUrl);
                };

                websocket.OnMessage += (data) =>
                {
                    string message = Encoding.UTF8.GetString(data);
                    ProcessMessage(message);
                };

                websocket.OnError += (error) =>
                {
                    Debug.LogError($"[WebSocket] Error: {error}");
                    OnError?.Invoke(error);
                };

                websocket.OnClose += (code) =>
                {
                    isConnecting = false;
                    Debug.Log($"[WebSocket] Disconnected: {code}");
                    OnDisconnected?.Invoke(code.ToString());
                };

                websocket.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocket] Connection failed: {e.Message}");
                isConnecting = false;
                ScheduleReconnect();
            }
        }

        public void Disconnect()
        {
            if (websocket != null)
            {
                websocket.Close();
                websocket = null;
            }
        }

        public void Update()
        {
            // Handle reconnection
            if (!IsConnected && !isConnecting && config.reconnectInterval > 0)
            {
                reconnectTimer += Time.deltaTime;
                if (reconnectTimer >= config.reconnectInterval)
                {
                    reconnectTimer = 0f;
                    Connect();
                }
            }

            // Process queued messages
            lock (messageQueue)
            {
                while (messageQueue.Count > 0)
                {
                    var message = messageQueue.Dequeue();
                    OnMessageReceived?.Invoke(message);
                }
            }
        }

        private void ProcessMessage(string data)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<WebSocketMessage>(data);
                if (msg == null) return;

                switch (msg.type.ToLower())
                {
                    case "chat_response":
                        var response = JsonConvert.DeserializeObject<ChatResponse>(msg.data);
                        OnChatResponse?.Invoke(response);
                        break;

                    case "emotion":
                        var emotion = JsonConvert.DeserializeObject<EmotionData>(msg.data);
                        OnEmotionUpdate?.Invoke(emotion);
                        break;

                    case "state":
                        // Handle state updates
                        break;

                    case "ping":
                        SendPong();
                        break;

                    default:
                        OnMessageReceived?.Invoke(data);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocket] Failed to process message: {e.Message}");
            }
        }

        public void SendChatMessage(string message)
        {
            var request = new ChatRequest
            {
                message = message,
                session_id = SystemInfo.deviceUniqueIdentifier,
                use_memory = true
            };

            var msg = new WebSocketMessage("chat", JsonConvert.SerializeObject(request));
            SendMessage(JsonConvert.SerializeObject(msg));
        }

        public void SendModeSwitch(string mode)
        {
            var data = new { mode = mode };
            var msg = new WebSocketMessage("mode_switch", JsonConvert.SerializeObject(data));
            SendMessage(JsonConvert.SerializeObject(msg));
        }

        private void SendPong()
        {
            var msg = new WebSocketMessage("pong", "");
            SendMessage(JsonConvert.SerializeObject(msg));
        }

        private new void SendMessage(string message)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebSocket] Cannot send message: Not connected.");
                return;
            }

            try
            {
                websocket.Send(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocket] Failed to send message: {e.Message}");
            }
        }

        private void ScheduleReconnect()
        {
            reconnectTimer = 0f;
        }

        private class WebSocket
        {
            private string url;
            private UnityWebSocket impl;

            public WebSocketState ReadyState => impl?.ReadyState ?? WebSocketState.Closed;

            public event Action OnOpen;
            public event Action<byte[]> OnMessage;
            public event Action<string> OnError;
            public event Action<int> OnClose;

            public WebSocket(string url)
            {
                this.url = url;
                impl = new UnityWebSocket(url);

                impl.OnOpen += () => OnOpen?.Invoke();
                impl.OnMessage += (data) => OnMessage?.Invoke(data);
                impl.OnError += (error) => OnError?.Invoke(error);
                impl.OnClose += (code) => OnClose?.Invoke(code);
            }

            public void Connect()
            {
                impl.Connect();
            }

            public void Send(string data)
            {
                impl.Send(data);
            }

            public void Close()
            {
                impl.Close();
            }
        }
    }
}
