using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ProjectDualis.Core;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Network
{
    /// <summary>
    /// Speech-to-Text client for Whisper API integration.
    /// Handles audio file upload and transcription response.
    /// </summary>
    public class STTClient : MonoBehaviour
    {
        private DualisConfig config;
        private string sttEndpoint;

        // Events
        public event Action<string> OnTranscriptionComplete;
        public event Action<string> OnTranscriptionError;

        public bool IsTranscribing { get; private set; }

        public void Initialize(DualisConfig config)
        {
            this.config = config;
            this.sttEndpoint = $"{config.apiUrl}/stt/transcribe";
        }

        /// <summary>
        /// Transcribe audio data using the Whisper API.
        /// </summary>
        /// <param name="audioData">Raw audio bytes (WAV/MP3 format)</param>
        /// <param name="model">Whisper model size (tiny, base, small, medium, large)</param>
        public void Transcribe(byte[] audioData, string model = "base")
        {
            if (!config.enableSTT)
            {
                Debug.LogWarning("[STT] STT is disabled in config.");
                return;
            }

            if (audioData == null || audioData.Length == 0)
            {
                OnTranscriptionError?.Invoke("No audio data provided.");
                return;
            }

            StartCoroutine(TranscribeCoroutine(audioData, model));
        }

        /// <summary>
        /// Transcribe audio file from path.
        /// </summary>
        public void TranscribeFile(string filePath, string model = "base")
        {
            if (!config.enableSTT)
            {
                Debug.LogWarning("[STT] STT is disabled in config.");
                return;
            }

            StartCoroutine(TranscribeFileCoroutine(filePath, model));
        }

        private IEnumerator TranscribeCoroutine(byte[] audioData, string model)
        {
            IsTranscribing = true;

            // Create multipart form data
            string boundary = "----Boundary" + DateTime.Now.Ticks.ToString("x");
            byte[] formData = BuildMultipartFormData(audioData, model, boundary);

            using (UnityWebRequest request = new UnityWebRequest(sttEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(formData);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

                Debug.Log($"[STT] Sending {audioData.Length} bytes for transcription...");

                yield return request.SendWebRequest();

                IsTranscribing = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    HandleTranscriptionResponse(request.downloadHandler.text);
                }
                else
                {
                    string error = $"[STT] Request failed: {request.error}";
                    Debug.LogError(error);
                    OnTranscriptionError?.Invoke(error);
                }
            }
        }

        private IEnumerator TranscribeFileCoroutine(string filePath, string model)
        {
            IsTranscribing = true;

            // Load audio file
            string audioPath = "file://" + filePath;
            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.WAV))
            {
                yield return audioRequest.SendWebRequest();

                if (audioRequest.result != UnityWebRequest.Result.Success)
                {
                    IsTranscribing = false;
                    OnTranscriptionError?.Invoke("Failed to load audio file.");
                    yield break;
                }
            }

            // Read file bytes
            byte[] audioData = System.IO.File.ReadAllBytes(filePath);
            yield return TranscribeCoroutine(audioData, model);
        }

        private byte[] BuildMultipartFormData(byte[] audioData, string model, string boundary)
        {
            MemoryStream formData = new MemoryStream();
            Encoding encoding = Encoding.UTF8;

            // Add model field
            WriteLine(formData, encoding, "--" + boundary);
            WriteLine(formData, encoding, "Content-Disposition: form-data; name=\"model\"");
            WriteLine(formData, encoding);
            WriteLine(formData, encoding, model);

            // Add audio file
            WriteLine(formData, encoding, "--" + boundary);
            WriteLine(formData, encoding, "Content-Disposition: form-data; name=\"audio\"; filename=\"audio.wav\"");
            WriteLine(formData, encoding, "Content-Type: audio/wav");
            WriteLine(formData, encoding);
            formData.Write(audioData, 0, audioData.Length);
            WriteLine(formData, encoding);

            // Add closing boundary
            WriteLine(formData, encoding, "--" + boundary + "--");

            return formData.ToArray();
        }

        private void WriteLine(MemoryStream stream, Encoding encoding, string line = "")
        {
            byte[] bytes = encoding.GetBytes(line + "\r\n");
            stream.Write(bytes, 0, bytes.Length);
        }

        private void HandleTranscriptionResponse(string jsonResponse)
        {
            try
            {
                // Parse JSON response
                // Expected format: {"text": "transcription here", "language": "en"}
                var response = JsonUtility.FromJson<STTResponse>(jsonResponse);

                if (response != null && !string.IsNullOrEmpty(response.text))
                {
                    Debug.Log($"[STT] Transcription: {response.text}");
                    OnTranscriptionComplete?.Invoke(response.text);
                }
                else
                {
                    OnTranscriptionError?.Invoke("Empty transcription response.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[STT] Failed to parse response: {e.Message}");
                OnTranscriptionError?.Invoke("Failed to parse transcription response.");
            }
        }

        /// <summary>
        /// Get available Whisper models.
        /// </summary>
        public IEnumerator GetAvailableModels(Action<string[]> onModelsReceived)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{config.apiUrl}/stt/models"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ModelsResponse>(request.downloadHandler.text);
                        onModelsReceived?.Invoke(response?.models ?? new string[0]);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[STT] Failed to parse models response: {e.Message}");
                        onModelsReceived?.Invoke(new string[0]);
                    }
                }
                else
                {
                    Debug.LogError($"[STT] Failed to get models: {request.error}");
                    onModelsReceived?.Invoke(new string[0]);
                }
            }
        }

        [Serializable]
        private class STTResponse
        {
            public string text;
            public string language;
        }

        [Serializable]
        private class ModelsResponse
        {
            public string[] models;
        }
    }
}
