using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ProjectDualis.Core;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Avatar
{
    /// <summary>
    /// Emotion types for avatar expressions.
    /// </summary>
    [Serializable]
    public enum AvatarEmotion
    {
        Neutral,
        Joy,
        Sadness,
        Anger,
        Fear,
        Surprise,
        Love,
        Excitement
    }

    /// <summary>
    /// BlendShape proxy for VRM avatar control.
    /// </summary>
    [Serializable]
    public class BlendShapeKey
    {
        public string name;
        public float value;

        public BlendShapeKey(string name, float value = 0f)
        {
            this.name = name;
            this.value = value;
        }
    }

    /// <summary>
    /// Manages VRM avatar loading, animation, and expression control.
    /// Requires UniVRM package: https://github.com/vrm-c/UniVRM
    /// </summary>
    public class AvatarManager : MonoBehaviour
    {
        private DualisConfig config;
        private VRMLoader vrmLoader;
        private GameObject currentAvatar;
        private SkinnedMeshRenderer faceRenderer;
        private Animator avatarAnimator;

        // BlendShape proxy (UniVRM)
        private object blendShapeProxy;

        // BlendShape dictionaries
        private Dictionary<string, int> blendShapeIndices = new Dictionary<string, int>();
        private Dictionary<AvatarEmotion, BlendShapeKey[]> emotionBlendShapes = new Dictionary<AvatarEmotion, BlendShapeKey[]>();

        // Current state
        private AvatarEmotion currentEmotion = AvatarEmotion.Neutral;
        private float lipSyncValue = 0f;
        private bool isInitialized = false;

        public bool IsAvatarLoaded => currentAvatar != null;
        public AvatarEmotion CurrentEmotion => currentEmotion;

        public void Initialize(DualisConfig config)
        {
            this.config = config;

            // Create VRM loader
            vrmLoader = gameObject.AddComponent<VRMLoader>();
            vrmLoader.Initialize(config);

            InitializeEmotionMappings();

            // Try to load VRM model, or use placeholder
            bool vrmLoaded = false;
            if (!string.IsNullOrEmpty(config.defaultVRMModel))
            {
                _ = LoadAvatarAsync(config.defaultVRMModel);
                vrmLoaded = true;
            }

            // If no VRM model specified, create placeholder
            if (!vrmLoaded && currentAvatar == null)
            {
                Debug.Log("[Avatar] No VRM model found, creating placeholder avatar.");
                CreatePlaceholderAvatar();
            }

            isInitialized = true;
        }

        private void CreatePlaceholderAvatar()
        {
            currentAvatar = PlaceholderAvatar.CreatePlaceholder("DualisPlaceholder");
            currentAvatar.transform.position = Vector3.zero;
            currentAvatar.transform.rotation = Quaternion.Euler(0, 180, 0);

            // Setup for emotion control
            var placeholder = currentAvatar.GetComponent<PlaceholderAvatar>();

            Debug.Log("[Avatar] Placeholder avatar created.");
        }

        private void InitializeEmotionMappings()
        {
            // Standard VRM blend shape names for emotions
            emotionBlendShapes[AvatarEmotion.Neutral] = new[] { new BlendShapeKey("Neutral", 1f) };
            emotionBlendShapes[AvatarEmotion.Joy] = new[]
            {
                new BlendShapeKey("Fun", 1f),
                new BlendShapeKey("A", 0.3f)
            };
            emotionBlendShapes[AvatarEmotion.Sadness] = new[]
            {
                new BlendShapeKey("Sorrow", 1f),
                new BlendShapeKey("Blink", 0.5f)
            };
            emotionBlendShapes[AvatarEmotion.Anger] = new[]
            {
                new BlendShapeKey("Angry", 1f),
                new BlendShapeKey("BrowL", 0.5f),
                new BlendShapeKey("BrowR", 0.5f)
            };
            emotionBlendShapes[AvatarEmotion.Surprise] = new[]
            {
                new BlendShapeKey("Surprise", 1f),
                new BlendShapeKey("E", 0.3f)
            };
            emotionBlendShapes[AvatarEmotion.Love] = new[]
            {
                new BlendShapeKey("Joy", 0.8f),
                new BlendShapeKey("Blink_L", 0.3f),
                new BlendShapeKey("Blink_R", 0.3f)
            };
            emotionBlendShapes[AvatarEmotion.Excitement] = new[]
            {
                new BlendShapeKey("Fun", 1f),
                new BlendShapeKey("I", 0.5f),
                new BlendShapeKey("U", 0.3f)
            };
        }

        public async Task LoadAvatarAsync(string resourcePath)
        {
            Debug.Log($"[Avatar] Loading VRM model from: {resourcePath}");

            try
            {
                // Unload existing avatar
                if (currentAvatar != null)
                {
                    Destroy(currentAvatar);
                }

                // Try loading from resources first
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset != null)
                {
                    currentAvatar = await vrmLoader.LoadVRMFromBytesAsync(textAsset.bytes);
                    Resources.UnloadAsset(textAsset);
                }
                else
                {
                    // Try loading as file path
                    currentAvatar = await vrmLoader.LoadVRMAsync(resourcePath);
                }

                if (currentAvatar != null)
                {
                    SetupAvatar(currentAvatar);
                }
                else
                {
                    Debug.LogWarning("[Avatar] Failed to load VRM model, using placeholder.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Avatar] Failed to load VRM: {e.Message}");
            }
        }

        private void SetupAvatar(GameObject model)
        {
            currentAvatar = model;

            // Find SkinnedMeshRenderer for face expressions
            faceRenderer = currentAvatar.GetComponentInChildren<SkinnedMeshRenderer>();
            avatarAnimator = currentAvatar.GetComponentInChildren<Animator>();

            // Setup blend shape proxy if UniVRM is available
            SetupBlendShapeProxy();

            if (faceRenderer != null)
            {
                BuildBlendShapeIndex();
            }

            // Position avatar
            currentAvatar.transform.position = Vector3.zero;
            currentAvatar.transform.rotation = Quaternion.Euler(0, 180, 0);

            Debug.Log("[Avatar] VRM model loaded successfully.");
        }

        private void SetupBlendShapeProxy()
        {
            // Try to get UniVRM BlendShapeProxy
            var proxyType = Type.GetType("VRM.BlendShapeProxy, VRM");
            if (proxyType != null)
            {
                blendShapeProxy = currentAvatar.GetComponent(proxyType);
                if (blendShapeProxy == null)
                {
                    blendShapeProxy = currentAvatar.AddComponent(proxyType);
                }
            }
        }

        private void BuildBlendShapeIndex()
        {
            if (faceRenderer == null) return;

            blendShapeIndices.Clear();

            for (int i = 0; i < faceRenderer.sharedMesh.blendShapeCount; i++)
            {
                var name = faceRenderer.sharedMesh.GetBlendShapeName(i);
                blendShapeIndices[name] = i;
            }

            Debug.Log($"[Avatar] Found {blendShapeIndices.Count} blend shapes.");
        }

        public void SetEmotion(AvatarEmotion emotion, float intensity = 1f)
        {
            currentEmotion = emotion;

            // Check if using placeholder avatar
            var placeholder = currentAvatar?.GetComponent<PlaceholderAvatar>();
            if (placeholder != null)
            {
                placeholder.SetEmotion(emotion, intensity);
                return;
            }

            // Original VRM logic
            if (!IsAvatarLoaded || faceRenderer == null) return;

            if (emotionBlendShapes.TryGetValue(emotion, out var shapes))
            {
                foreach (var shape in shapes)
                {
                    SetBlendShape(shape.name, shape.value * intensity);
                }
            }
        }

        public void SetBlendShape(string name, float value)
        {
            if (!IsAvatarLoaded) return;

            // Try UniVRM BlendShapeProxy first
            if (blendShapeProxy != null)
            {
                try
                {
                    var proxyType = blendShapeProxy.GetType();
                    var setValueMethod = proxyType.GetMethod("SetValue");

                    if (setValueMethod != null)
                    {
                        // Try to create BlendShapeKey
                        var keyType = Type.GetType("VRM.BlendShapeKey, VRM");
                        if (keyType != null)
                        {
                            var key = Activator.CreateInstance(keyType);
                            var nameProp = keyType.GetProperty("Name");
                            var presetProp = keyType.GetProperty("Preset");

                            nameProp?.SetValue(key, name, null);

                            // Call SetValue
                            setValueMethod.Invoke(blendShapeProxy, new object[] { key, value });
                            return;
                        }
                    }
                }
                catch
                {
                    // Fall through to direct SkinnedMeshRenderer access
                }
            }

            // Direct access to SkinnedMeshRenderer
            if (faceRenderer != null && blendShapeIndices.TryGetValue(name, out int index))
            {
                faceRenderer.SetBlendShapeWeight(index, value);
            }
        }

        public void SetLipSyncValue(float value)
        {
            lipSyncValue = Mathf.Clamp01(value * config.lipSyncSensitivity);

            if (!config.enableLipSync) return;

            // Apply to mouth blend shapes (A, I, U, E, O in VRM)
            SetBlendShape("A", Mathf.Max(0, lipSyncValue - 0.5f) * 2f);
            SetBlendShape("I", Mathf.Max(0, 0.5f - Mathf.Abs(lipSyncValue - 0.5f)) * 2f);
            SetBlendShape("U", Mathf.Max(0, lipSyncValue - 0.7f) * 3f);
        }

        public void PlayAnimation(string animationName)
        {
            if (avatarAnimator == null) return;

            avatarAnimator.Play(animationName, 0, 0f);
        }

        public void LookAt(Vector3 target)
        {
            if (!IsAvatarLoaded) return;

            // Use VRMLookAtHead component from UniVRM
            // This is a placeholder
            currentAvatar.transform.LookAt(target);
        }

        /// <summary>
        /// Convert string emotion to AvatarEmotion enum.
        /// </summary>
        public static AvatarEmotion ParseEmotion(string emotion)
        {
            if (string.IsNullOrEmpty(emotion)) return AvatarEmotion.Neutral;

            emotion = emotion.ToLower().Replace("_", "");

            switch (emotion)
            {
                case "joy": return AvatarEmotion.Joy;
                case "sadness": return AvatarEmotion.Sadness;
                case "anger": return AvatarEmotion.Anger;
                case "fear": return AvatarEmotion.Fear;
                case "surprise": return AvatarEmotion.Surprise;
                case "love": return AvatarEmotion.Love;
                case "excitement": return AvatarEmotion.Excitement;
                default: return AvatarEmotion.Neutral;
            }
        }

        private void OnDestroy()
        {
            if (currentAvatar != null)
            {
                Destroy(currentAvatar);
            }
        }
    }
}
