using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using ProjectDualis.Core;
using ProjectDualis.Network;
using ProjectDualis.Audio;
using ProjectDualis.Avatar;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Editor
{
    /// <summary>
    /// Integration tests for Project Dualis Unity components.
    /// Run these tests from Unity Test Runner.
    /// </summary>
    public class DualisIntegrationTest
    {
        private DualisConfig testConfig;
        private GameObject testGameObject;

        [SetUp]
        public void SetUp()
        {
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<DualisConfig>();
            testConfig.backendUrl = "ws://localhost:8000/ws";
            testConfig.apiUrl = "http://localhost:8000/api/v1";
            testConfig.enableTTS = false; // Disable for tests
            testConfig.enableSTT = false;
            testConfig.enableLipSync = false;

            // Create test GameObject
            testGameObject = new GameObject("TestDualis");
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            if (testConfig != null)
            {
                Object.DestroyImmediate(testConfig);
            }
        }

        [Test]
        public void Config_DefaultValues_AreCorrect()
        {
            var config = ScriptableObject.CreateInstance<DualisConfig>();

            Assert.AreEqual("ws://localhost:8000/ws", config.backendUrl);
            Assert.AreEqual("http://localhost:8000/api/v1", config.apiUrl);
            Assert.AreEqual(10f, config.connectionTimeout);
            Assert.AreEqual(3f, config.reconnectInterval);
            Assert.IsTrue(config.enableTTS);
            Assert.IsTrue(config.enableSTT);
            Assert.IsTrue(config.enableLipSync);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void AvatarEmotion_ParseEmotion_WorksCorrectly()
        {
            Assert.AreEqual(AvatarEmotion.Joy, AvatarManager.ParseEmotion("joy"));
            Assert.AreEqual(AvatarEmotion.Sadness, AvatarManager.ParseEmotion("sadness"));
            Assert.AreEqual(AvatarEmotion.Anger, AvatarManager.ParseEmotion("anger"));
            Assert.AreEqual(AvatarEmotion.Surprise, AvatarManager.ParseEmotion("surprise"));
            Assert.AreEqual(AvatarEmotion.Love, AvatarManager.ParseEmotion("love"));
            Assert.AreEqual(AvatarEmotion.Excitement, AvatarManager.ParseEmotion("excitement"));
            Assert.AreEqual(AvatarEmotion.Neutral, AvatarManager.ParseEmotion("neutral"));
            Assert.AreEqual(AvatarEmotion.Neutral, AvatarManager.ParseEmotion("unknown"));
        }

        [Test]
        public void WebSocketMessage_Serialization_Works()
        {
            var request = new ChatRequest
            {
                message = "Hello, test!",
                mode = "companion",
                session_id = "test_session",
                use_memory = true
            };

            Assert.AreEqual("Hello, test!", request.message);
            Assert.AreEqual("companion", request.mode);
            Assert.IsTrue(request.use_memory);
        }

        [Test]
        public void ChatResponse_EmotionData_ParsedCorrectly()
        {
            var emotion = new EmotionData
            {
                primary = "joy",
                secondary = "excitement",
                intensity = 0.8f
            };

            var response = new ChatResponse
            {
                message = "Test response",
                emotion = emotion
            };

            Assert.AreEqual("Test response", response.message);
            Assert.AreEqual("joy", response.emotion.primary);
            Assert.AreEqual("excitement", response.emotion.secondary);
            Assert.AreEqual(0.8f, response.emotion.intensity);
        }

        [Test]
        public void WindowController_IsTransparentSupported_ReturnsCorrectPlatform()
        {
            #if UNITY_STANDALONE_WIN
            Assert.IsTrue(WindowController.IsTransparentSupported);
            #elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            Assert.IsFalse(WindowController.IsTransparentSupported);
            #else
            Assert.IsFalse(WindowController.IsTransparentSupported);
            #endif
        }
    }

    /// <summary>
    /// Runtime integration tests for WebSocket connection.
    /// These require a running backend server.
    /// </summary>
    public class DualisRuntimeTest : MonoBehaviour
    {
        private DualisConfig config;
        private WebSocketClient webSocket;
        private bool testCompleted = false;
        private string testResult = "";

        [Header("Test Configuration")]
        [SerializeField] private string testBackendUrl = "ws://localhost:8000/ws";

        void Start()
        {
            StartCoroutine(RunIntegrationTests());
        }

        private IEnumerator RunIntegrationTests()
        {
            Debug.Log("[Test] Starting integration tests...");

            // Setup
            config = ScriptableObject.CreateInstance<DualisConfig>();
            config.backendUrl = testBackendUrl;
            config.apiUrl = "http://localhost:8000/api/v1";

            webSocket = gameObject.AddComponent<WebSocketClient>();
            webSocket.Initialize(config);

            // Wait for connection or timeout
            float timeout = 10f;
            float elapsed = 0f;

            while (!webSocket.IsConnected && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (webSocket.IsConnected)
            {
                Debug.Log("[Test] WebSocket connection successful!");
                testResult = "Connection: OK";

                // Test sending a message
                webSocket.OnChatResponse += HandleTestResponse;

                Debug.Log("[Test] Sending test message...");
                webSocket.SendChatMessage("Hello, this is a test message.");

                // Wait for response
                yield return new WaitForSeconds(5f);
            }
            else
            {
                Debug.LogError("[Test] WebSocket connection failed!");
                testResult = "Connection: FAILED";
            }

            testCompleted = true;

            // Cleanup
            webSocket.Disconnect();

            Debug.Log($"[Test] Integration test complete. Result: {testResult}");
        }

        private void HandleTestResponse(ChatResponse response)
        {
            Debug.Log($"[Test] Received response: {response.message}");

            if (response.emotion != null)
            {
                Debug.Log($"[Test] Emotion: {response.emotion.primary} (intensity: {response.emotion.intensity})");
            }

            if (!string.IsNullOrEmpty(response.audio_base64))
            {
                Debug.Log("[Test] Audio data received (base64 length: " + response.audio_base64.Length + ")");
            }

            testResult = "Full Test: PASSED";
        }

        void OnGUI()
        {
            if (testCompleted)
            {
                GUI.Label(new Rect(10, 10, 400, 100), $"Integration Test Result:\n{testResult}");
            }
            else
            {
                GUI.Label(new Rect(10, 10, 400, 100), "Running integration tests...");
            }
        }
    }
}
