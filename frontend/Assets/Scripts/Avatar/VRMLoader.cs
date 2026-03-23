using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using ProjectDualis.Core;

namespace ProjectDualis.Avatar
{
    /// <summary>
    /// VRM model loader using UniVRM.
    /// This component handles loading and setting up VRM models for the avatar.
    /// </summary>
    public class VRMLoader : MonoBehaviour
    {
        private DualisConfig config;
        private GameObject currentModel;

        public bool IsModelLoaded => currentModel != null;
        public GameObject CurrentModel => currentModel;

        public void Initialize(DualisConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Load a VRM model from a file path (Runtime).
        /// </summary>
        public async Task<GameObject> LoadVRMAsync(string filePath)
        {
            Debug.Log($"[VRM] Loading model from: {filePath}");

            try
            {
                // Check if UniVRM is available
                var vrmlibType = Type.GetType("VRM.VRMLib, VRM");
                if (vrmlibType == null)
                {
                    Debug.LogWarning("[VRM] UniVRM not found. Please install UniVRM package.");
                    return CreatePlaceholderModel();
                }

                // Load file bytes
                #if UNITY_WEBGL && !UNITY_EDITOR
                // For WebGL, use UnityWebRequest
                var bytes = await LoadBytesWebGL(filePath);
                #else
                var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
                #endif

                // Load VRM using UniVRM
                return await LoadVRMFromBytesAsync(bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VRM] Failed to load model: {e.Message}");
                return CreatePlaceholderModel();
            }
        }

        /// <summary>
        /// Load VRM from byte array.
        /// </summary>
        public async Task<GameObject> LoadVRMFromBytesAsync(byte[] vrmBytes)
        {
            try
            {
                // Try using UniVRM's VRMImporter
                var contextType = Type.GetType("VRM.VRMImporterContext, VRM");
                if (contextType != null)
                {
                    var context = Activator.CreateInstance(contextType);
                    var parseMethod = contextType.GetMethod("ParseGlb");
                    var loadMethod = contextType.GetMethod("LoadAsync");

                    if (parseMethod != null && loadMethod != null)
                    {
                        // Parse GLB
                        parseMethod.Invoke(context, new object[] { vrmBytes });

                        // Load async
                        var task = loadMethod.Invoke(context, null) as Task;
                        await task;

                        // Get root GameObject
                        var rootProp = contextType.GetProperty("Root");
                        var model = rootProp.GetValue(context) as GameObject;

                        if (model != null)
                        {
                            SetupModel(model);
                            currentModel = model;
                            Debug.Log("[VRM] Model loaded successfully with UniVRM.");
                            return model;
                        }
                    }
                }

                // Fallback: Try VRM10Loader (UniVRM 1.x)
                var loaderType = Type.GetType("VRM.VRM10.VRM10Loader, VRM");
                if (loaderType != null)
                {
                    var loader = Activator.CreateInstance(loaderType);
                    var loadMethod = loaderType.GetMethod("LoadAsync", new Type[] { typeof(byte[]) });

                    if (loadMethod != null)
                    {
                        var task = loadMethod.Invoke(loader, new object[] { vrmBytes }) as Task;
                        await task;

                        var loadedProp = loaderType.GetProperty("Loaded");
                        var model = loadedProp.GetValue(loader) as GameObject;

                        if (model != null)
                        {
                            SetupModel(model);
                            currentModel = model;
                            Debug.Log("[VRM] Model loaded successfully with VRM10Loader.");
                            return model;
                        }
                    }
                }

                Debug.LogWarning("[VRM] Failed to load with UniVRM, creating placeholder.");
                return CreatePlaceholderModel();
            }
            catch (Exception e)
            {
                Debug.LogError($"[VRM] Exception loading model: {e.Message}");
                return CreatePlaceholderModel();
            }
        }

        /// <summary>
        /// Load VRM from Resources folder.
        /// </summary>
        public async Task<GameObject> LoadVRMFromResourcesAsync(string resourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"[VRM] Resource not found: {resourcePath}");
                return CreatePlaceholderModel();
            }

            var model = await LoadVRMFromBytesAsync(textAsset.bytes);
            Resources.UnloadAsset(textAsset);
            return model;
        }

        /// <summary>
        /// Create a placeholder model for testing when VRM is unavailable.
        /// </summary>
        private GameObject CreatePlaceholderModel()
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
            }

            var model = new GameObject("Dualis_Placeholder");

            // Add basic renderer for visibility
            var rendererObj = new GameObject("Body");
            rendererObj.transform.SetParent(model.transform, false);

            // Create a simple capsule renderer
            var meshFilter = rendererObj.AddComponent<MeshFilter>();
            var meshRenderer = rendererObj.AddComponent<MeshRenderer>();

            // Create capsule mesh
            meshFilter.mesh = CreateCapsuleMesh();

            // Create a simple material
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.8f, 0.6f, 0.9f); // Light purple
            meshRenderer.material = material;

            // Add animator for blend shapes
            var animator = model.AddComponent<Animator>();
            var controller = new AnimatorRuntimeAnimatorController();
            animator.runtimeAnimatorController = controller;

            // Add look at component placeholder
            var lookAtObj = new GameObject("LookAt");
            lookAtObj.transform.SetParent(model.transform, false);

            Debug.Log("[VRM] Created placeholder model.");
            currentModel = model;
            return model;
        }

        private Mesh CreateCapsuleMesh()
        {
            var mesh = new Mesh();
            // Simple capsule using Unity's primitive
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var capsuleMesh = capsule.GetComponent<MeshFilter>().mesh;
            Destroy(capsule);
            return capsuleMesh;
        }

        private void SetupModel(GameObject model)
        {
            // Position model
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.Euler(0, 180, 0); // Face forward

            // Ensure animator is present
            if (model.GetComponent<Animator>() == null)
            {
                model.AddComponent<Animator>();
            }

            // Setup blend shape proxy if UniVRM is available
            var proxyType = Type.GetType("VRM.BlendShapeProxy, VRM");
            if (proxyType != null)
            {
                var proxy = model.GetComponent(proxyType);
                if (proxy == null)
                {
                    proxy = model.AddComponent(proxyType);
                }
            }

            // Setup look at component if UniVRM is available
            var lookAtType = Type.GetType("VRM.VRMLookAtHead, VRM");
            if (lookAtType != null && model.GetComponent(lookAtType) == null)
            {
                model.AddComponent(lookAtType);
            }
        }

        public void UnloadModel()
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
                currentModel = null;
            }
        }

        private void OnDestroy()
        {
            UnloadModel();
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private async Task<byte[]> LoadBytesWebGL(string path)
        {
            using (var webRequest = UnityEngine.Networking.UnityWebRequest.Get(path))
            {
                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    throw new Exception(webRequest.error);
                }

                return webRequest.downloadHandler.data;
            }
        }
        #endif

        /// <summary>
        /// Simple runtime animator controller for placeholder.
        /// </summary>
        private class AnimatorRuntimeAnimatorController : RuntimeAnimatorController
        {
            public override int animationClipsCount => 0;

            public override AnimationClip GetAnimationClip(int index)
            {
                return null;
            }
        }
    }
}
