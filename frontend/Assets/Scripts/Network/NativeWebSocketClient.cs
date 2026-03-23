using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectDualis.Network
{
    /// <summary>
    /// Native WebSocket implementation using Unity's built-in WebSocket.
    /// Falls back to this implementation if NativeWebSocket package is not available.
    /// </summary>
    public class UnityWebSocket : IDisposable
    {
        private Uri uri;
        private Dictionary<string, string> headers;
        private WebSocketState state = WebSocketState.Closed;
        private System.Net.WebSockets.ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private Queue<byte[]> messageQueue = new Queue<byte[]>();
        private const int receiveChunkSize = 1024 * 8;

        public event Action OnOpen;
        public event Action<byte[]> OnMessage;
        public event Action<string> OnError;
        public event Action<int> OnClose;

        public WebSocketState ReadyState => state;

        public UnityWebSocket(string url, Dictionary<string, string> headers = null)
        {
            this.uri = new Uri(url);
            this.headers = headers ?? new Dictionary<string, string>();
        }

        public async void Connect()
        {
            if (state == WebSocketState.Open || state == WebSocketState.Connecting)
                return;

            try
            {
                state = WebSocketState.Connecting;
                webSocket = new System.Net.WebSockets.ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();

                // Add headers
                foreach (var header in headers)
                {
                    webSocket.Options.SetRequestHeader(header.Key, header.Value);
                }

                await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);

                state = WebSocketState.Open;
                OnOpen?.Invoke();

                // Start receive loop
                _ReceiveLoop();
            }
            catch (Exception e)
            {
                state = WebSocketState.Closed;
                OnError?.Invoke(e.Message);
            }
        }

        private async void _ReceiveLoop()
        {
            var buffer = new byte[receiveChunkSize];
            var cancellationToken = cancellationTokenSource.Token;

            while (webSocket.State == System.Net.WebSockets.WebSocketState.Open &&
                   state == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken
                    );

                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        state = WebSocketState.Closed;
                        OnClose?.Invoke((int)System.Net.WebSockets.WebSocketCloseStatus.NormalClosure);
                        break;
                    }

                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                    {
                        var message = new byte[result.Count];
                        Array.Copy(buffer, message, result.Count);
                        OnMessage?.Invoke(message);
                    }
                    else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                    {
                        var message = new byte[result.Count];
                        Array.Copy(buffer, message, result.Count);
                        OnMessage?.Invoke(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    OnError?.Invoke(e.Message);
                    break;
                }
            }

            Close();
        }

        public async void Send(byte[] data)
        {
            if (state != WebSocketState.Open)
                return;

            try
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    System.Net.WebSockets.WebSocketMessageType.Binary,
                    true,
                    cancellationTokenSource.Token
                );
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        public async void Send(string text)
        {
            if (state != WebSocketState.Open)
                return;

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    System.Net.WebSockets.WebSocketMessageType.Text,
                    true,
                    cancellationTokenSource.Token
                );
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        public void Close()
        {
            if (state == WebSocketState.Closed)
                return;

            state = WebSocketState.Closed;

            try
            {
                cancellationTokenSource?.Cancel();
                webSocket?.CloseAsync(
                    System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                );
            }
            catch { }

            webSocket?.Dispose();
            cancellationTokenSource?.Dispose();

            OnClose?.Invoke(1000);
        }

        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// WebSocket state enum matching NativeWebSocket.WebSocketState
    /// </summary>
    public enum WebSocketState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }
}
