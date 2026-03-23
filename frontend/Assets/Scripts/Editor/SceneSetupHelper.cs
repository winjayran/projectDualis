using UnityEngine;
using UnityEditor;

namespace ProjectDualis.Editor
{
    /// <summary>
    /// Automatically adds debug components to the scene when playing.
    /// </summary>
    [InitializeOnLoad]
    public class SceneSetupHelper
    {
        static SceneSetupHelper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnsureDebugDisplay();
            }
        }

        private static void EnsureDebugDisplay()
        {
            // Check if DebugDisplay exists
            var existing = Object.FindObjectOfType<DebugDisplay>();
            if (existing != null) return;

            // Find GameManager
            var gameManager = Object.FindObjectOfType<DualisGameManager>();
            if (gameManager != null)
            {
                // Add DebugDisplay to GameManager
                gameManager.gameObject.AddComponent<DebugDisplay>();
                Debug.Log("[SceneSetup] Added DebugDisplay component");
            }
        }
    }
}
