using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ProjectDualis.Core;

namespace ProjectDualis.Audio
{
    /// <summary>
    /// Manages audio input (STT) and output (TTS) for the Dualis avatar.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private DualisConfig config;

        // Audio output
        private AudioSource audioSource;
        private AudioClip currentClip;
        private bool isPlaying = false;
        private int sampleRate;

        // Audio input (microphone)
        private AudioClip microphoneClip;
        private bool isRecording = false;
        private string selectedMicrophone;

        // Lip sync data
        private float[] audioSamples;
        private int sampleIndex = 0;

        // Events
        public event Action<string> OnSpeechRecognized;
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackComplete;

        public bool IsPlaying => isPlaying;
        public bool IsRecording => isRecording;

        public void Initialize(DualisConfig config)
        {
            this.config = config;
            this.sampleRate = config.sampleRate;

            // Setup audio output
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound

            // Setup microphone
            if (config.enableSTT)
            {
                InitializeMicrophone();
            }
        }

        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[Audio] No microphone devices found.");
                return;
            }

            // Select microphone
            if (!string.IsNullOrEmpty(config.microphoneDevice))
            {
                foreach (var device in Microphone.devices)
                {
                    if (device.Contains(config.microphoneDevice))
                    {
                        selectedMicrophone = device;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(selectedMicrophone) && Microphone.devices.Length > 0)
            {
                selectedMicrophone = Microphone.devices[0];
            }

            Debug.Log($"[Audio] Using microphone: {selectedMicrophone}");
        }

        public void Update()
        {
            // Update lip sync during playback
            if (isPlaying && audioSource.isPlaying && audioSamples != null)
            {
                int position = audioSource.timeSamples;
                if (position < audioSamples.Length)
                {
                    // Get current audio level for lip sync
                    float level = Mathf.Abs(audioSamples[position]);
                    DualisGameManager.Instance?.AvatarManager?.SetLipSyncValue(level);
                }
            }

            // Check if playback completed
            if (isPlaying && !audioSource.isPlaying)
            {
                isPlaying = false;
                OnPlaybackComplete?.Invoke();
            }
        }

        /// <summary>
        /// Play audio data from backend TTS.
        /// </summary>
        public void PlayTTS(byte[] audioData)
        {
            if (!config.enableTTS)
            {
                Debug.LogWarning("[Audio] TTS is disabled in config.");
                return;
            }

            StartCoroutine(PlayAudioFromBytes(audioData));
        }

        /// <summary>
        /// Play audio from base64 encoded string (from WebSocket).
        /// </summary>
        public void PlayTTSFromBase64(string base64Audio)
        {
            if (string.IsNullOrEmpty(base64Audio))
            {
                Debug.LogWarning("[Audio] No audio data provided.");
                return;
            }

            try
            {
                byte[] audioData = Convert.FromBase64String(base64Audio);
                PlayTTS(audioData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Audio] Failed to decode base64 audio: {e.Message}");
            }
        }

        private IEnumerator PlayAudioFromBytes(byte[] audioData)
        {
            // Convert bytes to audio clip
            // This depends on the format from TTS (WAV, MP3, etc.)
            float[] samples = ConvertBytesToSamples(audioData);
            audioSamples = samples; // Store for lip sync

            currentClip = AudioClip.Create("TTS_Audio", samples.Length, 1, sampleRate, false);
            currentClip.SetData(samples, 0);

            audioSource.clip = currentClip;
            audioSource.Play();

            isPlaying = true;
            OnPlaybackStarted?.Invoke();

            yield return new WaitForSecondsRealtime(currentClip.length);

            isPlaying = false;
            OnPlaybackComplete?.Invoke();
        }

        private float[] ConvertBytesToSamples(byte[] audioData)
        {
            // Placeholder conversion - implement based on actual TTS format
            // For 16-bit PCM:
            int sampleCount = audioData.Length / 2;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)((audioData[i * 2 + 1] << 8) | audioData[i * 2]);
                samples[i] = sample / 32768f;
            }

            return samples;
        }

        /// <summary>
        /// Start recording from microphone.
        /// </summary>
        public void StartRecording(int maxDurationSeconds = 30)
        {
            if (!config.enableSTT || string.IsNullOrEmpty(selectedMicrophone))
            {
                Debug.LogWarning("[Audio] Cannot record: STT disabled or no microphone.");
                return;
            }

            if (isRecording)
            {
                Debug.LogWarning("[Audio] Already recording.");
                return;
            }

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedMicrophone, out minFreq, out maxFreq);

            int frequency = sampleRate;
            if (frequency < minFreq) frequency = minFreq;
            if (frequency > maxFreq && maxFreq > 0) frequency = maxFreq;

            microphoneClip = Microphone.Start(selectedMicrophone, false, maxDurationSeconds, frequency);
            isRecording = true;

            Debug.Log($"[Audio] Started recording at {frequency}Hz");
        }

        /// <summary>
        /// Stop recording and return the audio data.
        /// </summary>
        public byte[] StopRecording()
        {
            if (!isRecording)
            {
                return null;
            }

            int position = Microphone.GetPosition(selectedMicrophone);
            Microphone.End(selectedMicrophone);
            isRecording = false;

            if (position <= 0)
            {
                return null;
            }

            // Get samples
            float[] samples = new float[position];
            microphoneClip.GetData(samples, 0);

            // Convert to bytes (16-bit PCM)
            byte[] audioData = new byte[position * 2];
            for (int i = 0; i < position; i++)
            {
                short sample = (short)(samples[i] * 32767f);
                audioData[i * 2] = (byte)(sample & 0xFF);
                audioData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            Debug.Log($"[Audio] Recording stopped: {position} samples");
            return audioData;
        }

        /// <summary>
        /// Start voice activity detection and automatic STT.
        /// </summary>
        public void StartContinuousListening()
        {
            StartCoroutine(ContinuousListeningCoroutine());
        }

        private IEnumerator ContinuousListeningCoroutine()
        {
            while (config.enableSTT)
            {
                // Wait for voice activity
                yield return StartCoroutine(DetectVoiceActivity());

                // Start recording when voice detected
                StartRecording(10);

                // Wait for silence or timeout
                yield return StartCoroutine(DetectSilenceOrTimeout(3f));

                // Stop and send for recognition
                byte[] audioData = StopRecording();
                if (audioData != null && audioData.Length > 1000)
                {
                    // In production, send to Whisper STT endpoint
                    // OnSpeechRecognized?.Invoke(transcript);
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator DetectVoiceActivity()
        {
            // Simple VAD: wait for audio level above threshold
            const float threshold = 0.01f;
            const int checkInterval = 100; // samples

            if (string.IsNullOrEmpty(selectedMicrophone)) yield break;

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedMicrophone, out minFreq, out maxFreq);

            AudioClip vadClip = Microphone.Start(selectedMicrophone, true, 1, sampleRate > 0 ? sampleRate : minFreq);
            yield return new WaitForSeconds(0.1f);

            float[] samples = new float[checkInterval];
            bool voiceDetected = false;

            while (!voiceDetected)
            {
                int position = Microphone.GetPosition(selectedMicrophone);
                if (position >= checkInterval)
                {
                    vadClip.GetData(samples, position - checkInterval);
                    float level = 0f;
                    foreach (var s in samples)
                    {
                        level += Mathf.Abs(s);
                    }
                    level /= checkInterval;

                    if (level > threshold)
                    {
                        voiceDetected = true;
                    }
                }
                yield return null;
            }

            Microphone.End(selectedMicrophone);
        }

        private IEnumerator DetectSilenceOrTimeout(float timeoutSeconds)
        {
            float silenceTimer = 0f;
            const float silenceThreshold = 0.005f;

            while (isRecording && silenceTimer < timeoutSeconds)
            {
                int position = Microphone.GetPosition(selectedMicrophone);
                if (position > 100)
                {
                    float[] samples = new float[100];
                    microphoneClip.GetData(samples, position - 100);

                    float level = 0f;
                    foreach (var s in samples)
                    {
                        level += Mathf.Abs(s);
                    }
                    level /= 100;

                    if (level < silenceThreshold)
                    {
                        silenceTimer += Time.deltaTime;
                    }
                    else
                    {
                        silenceTimer = 0f;
                    }
                }

                yield return null;
            }
        }

        public void StopPlayback()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                isPlaying = false;
                OnPlaybackComplete?.Invoke();
            }
        }

        public void Cleanup()
        {
            StopPlayback();
            if (isRecording)
            {
                Microphone.End(selectedMicrophone);
            }
        }
    }
}
