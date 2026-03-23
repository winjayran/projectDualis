using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Core
{
    /// <summary>
    /// Runtime initializer that creates default config if missing.
    /// Attach this to a GameObject in the scene to auto-set up.
    /// </summary>
    public class DualisRuntimeInitializer : MonoBehaviour
    {
        [Header("Auto-Create Settings")]
        [SerializeField] private bool createDefaultConfig = true;
        [SerializeField] private bool showSetupUI = true;

        void Awake()
        {
            // Find the game manager
            var gameManager = FindObjectOfType<DualisGameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[DualisInit] DualisGameManager not found in scene. Creating one...");
                var go = new GameObject("DualisGameManager");
                gameManager = go.AddComponent<DualisGameManager>();
            }

            // Check if config exists via reflection
            var configField = typeof(DualisGameManager).GetField("config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (configField != null)
            {
                var config = configField.GetValue(gameManager) as DualisConfig;
                if (config == null)
                {
                    Debug.LogWarning("[DualisInit] No config assigned. Creating default config...");

                    // Try to load from Resources
                    config = Resources.Load<DualisConfig>("DualisConfig");

                    if (config == null && createDefaultConfig)
                    {
                        config = ScriptableObject.CreateInstance<DualisConfig>();
                        config.name = "DualisConfig (Runtime)";

                        // Set default values
                        SetDefaultConfigValues(config);

                        Debug.Log("[DualisInit] Created default config with default values.");
                        Debug.Log("[DualisInit] NOTE: Config is not saved. Use 'ProjectDualis > Create Config Asset' in editor.");
                    }

                    // Assign to game manager
                    configField.SetValue(gameManager, config);
                }
            }

            // Show setup UI if needed
            if (showSetupUI && Application.isEditor)
            {
                Debug.Log("[DualisInit] Scene is ready! Press Play to start.");
            }
        }

        private void SetDefaultConfigValues(DualisConfig config)
        {
            // Use reflection to set private fields
            var backendUrlField = typeof(DualisConfig).GetField("backendUrl",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (backendUrlField != null)
            {
                backendUrlField.SetValue(config, "ws://localhost:8000/ws");
            }

            // Set other default values via reflection or public properties
            var apiUrlField = typeof(DualisConfig).GetField("apiUrl",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (apiUrlField != null)
            {
                apiUrlField.SetValue(config, "http://localhost:8000/api/v1");
            }
        }
    }
}
