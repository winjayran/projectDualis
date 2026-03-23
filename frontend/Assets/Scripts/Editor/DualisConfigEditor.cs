using UnityEngine;
using UnityEditor;

namespace ProjectDualis.Core
{
    /// <summary>
    /// Editor window for creating and managing DualisConfig assets.
    /// </summary>
    public class DualisConfigEditor : EditorWindow
    {
        private string configName = "DualisConfig";

        [MenuItem("ProjectDualis/Create Config Asset")]
        public static void ShowWindow()
        {
            GetWindow<DualisConfigEditor>("Dualis Config");
        }

        private void OnGUI()
        {
            GUILayout.Label("Create DualisConfig Asset", EditorStyles.boldLabel);

            configName = EditorGUILayout.TextField("Config Name:", configName);

            if (GUILayout.Button("Create Config"))
            {
                CreateConfigAsset();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Load Existing Config"))
            {
                Selection.activeObject = Resources.Load<DualisConfig>(configName);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Config assets are stored in Resources folder.\n" +
                "The GameManager will automatically load 'DualisConfig' on startup.",
                MessageType.Info
            );
        }

        private void CreateConfigAsset()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Create the config asset
            DualisConfig config = ScriptableObject.CreateInstance<DualisConfig>();

            // Set default values
            config.backendUrl = "ws://localhost:8000/ws";
            config.apiUrl = "http://localhost:8000/api/v1";
            config.connectionTimeout = 10f;
            config.reconnectInterval = 3f;
            config.enableTTS = true;
            config.enableSTT = true;
            config.sampleRate = 24000;
            config.microphoneDevice = "";
            config.defaultVRMModel = "Models/Avatar";
            config.enableLipSync = true;
            config.lipSyncSensitivity = 1f;
            config.transparentBackground = true;
            config.alwaysOnTop = true;
            config.windowScale = 1f;

            // Save the asset
            string assetPath = $"Assets/Resources/{configName}.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the newly created asset
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"[Dualis] Created config asset at: {assetPath}");
        }
    }

    /// <summary>
    /// Custom editor for DualisConfig Inspector.
    /// </summary>
    [CustomEditor(typeof(DualisConfig))]
    public class DualisConfigInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DualisConfig config = (DualisConfig)target;

            EditorGUILayout.HelpBox(
                "Project Dualis Configuration\n\n" +
                "Edit settings below and remember to save your project.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();

            // Test connection section
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Test Backend Connection"))
            {
                Debug.Log($"[Dualis] Testing connection to {config.backendUrl}");
                // Connection test would be done at runtime
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Config", "Reset all values to defaults?", "Yes", "No"))
                {
                    config.backendUrl = "ws://localhost:8000/ws";
                    config.apiUrl = "http://localhost:8000/api/v1";
                    config.connectionTimeout = 10f;
                    config.reconnectInterval = 3f;
                    config.enableTTS = true;
                    config.enableSTT = true;
                    config.sampleRate = 24000;
                    config.enableLipSync = true;
                    config.lipSyncSensitivity = 1f;
                    config.transparentBackground = true;
                    config.alwaysOnTop = true;
                    config.windowScale = 1f;
                    EditorUtility.SetDirty(config);
                }
            }

            // Apply changes
            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }
    }
}
