using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

namespace ProjectDualis.Editor
{
    /// <summary>
    /// Automatically connects UI elements to DualisUIManager.
    /// Run this after scene setup to wire up all references.
    /// </summary>
    public class AutoSetupUI
    {
        [MenuItem("ProjectDualis/Auto-Connect UI Elements")]
        public static void ConnectUIElements()
        {
            // Find the UI Manager
            var uiManager = Object.FindObjectOfType<DualisUIManager>();
            if (uiManager == null)
            {
                var go = new GameObject("DualisUIManager");
                uiManager = go.AddComponent<DualisUIManager>();
                Debug.Log("[AutoSetup] Created DualisUIManager");
            }

            // SerializedObject to modify private fields
            var serialized = new SerializedObject(uiManager);

            // Find and connect ChatInputField
            var inputField = Object.FindObjectOfType<TMP_InputField>();
            if (inputField != null)
            {
                serialized.FindProperty("chatInputField").objectReferenceValue = inputField;
                Debug.Log($"[AutoSetup] Connected chatInputField: {inputField.name}");
            }

            // Find and connect send button
            var buttons = Object.FindObjectsOfType<Button>(true);
            Button sendButton = null;
            Button recordButton = null;
            Toggle modeToggle = Object.FindObjectOfType<Toggle>(true);

            foreach (var btn in buttons)
            {
                if (btn.name.ToLower().Contains("send"))
                {
                    sendButton = btn;
                }
                else if (btn.name.ToLower().Contains("voice") || btn.name.ToLower().Contains("record"))
                {
                    recordButton = btn;
                }
            }

            if (sendButton != null)
            {
                serialized.FindProperty("sendButton").objectReferenceValue = sendButton;
                Debug.Log($"[AutoSetup] Connected sendButton: {sendButton.name}");
            }

            if (recordButton != null)
            {
                serialized.FindProperty("recordButton").objectReferenceValue = recordButton;
                Debug.Log($"[AutoSetup] Connected recordButton: {recordButton.name}");
            }

            if (modeToggle != null)
            {
                serialized.FindProperty("modeToggle").objectReferenceValue = modeToggle;
                Debug.Log($"[AutoSetup] Connected modeToggle: {modeToggle.name}");
            }

            // Find chat display
            var chatTexts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var text in chatTexts)
            {
                if (text.name.ToLower().Contains("message") || text.transform.parent?.name.ToLower().Contains("chat"))
                {
                    serialized.FindProperty("chatDisplay").objectReferenceValue = text;
                    Debug.Log($"[AutoSetup] Connected chatDisplay: {text.name}");
                    break;
                }
            }

            // Find status text
            foreach (var text in chatTexts)
            {
                if (text.name.ToLower().Contains("status") || text.text.ToLower().Contains("disconnected"))
                {
                    serialized.FindProperty("statusText").objectReferenceValue = text;
                    Debug.Log($"[AutoSetup] Connected statusText: {text.name}");
                    break;
                }
            }

            // Find status indicators
            var images = Object.FindObjectsOfType<Image>(true);
            foreach (var img in images)
            {
                if (img.name.ToLower().Contains("connection") || img.name.ToLower().Contains("indicator"))
                {
                    serialized.FindProperty("connectionStatusImage").objectReferenceValue = img;
                    Debug.Log($"[AutoSetup] Connected connectionStatusImage: {img.name}");
                }
                else if (img.name.ToLower().Contains("emotion"))
                {
                    serialized.FindProperty("emotionIndicator").objectReferenceValue = img;
                    Debug.Log($"[AutoSetup] Connected emotionIndicator: {img.name}");
                }
            }

            // Apply changes
            serialized.ApplyModifiedProperties();

            // Save scene
            EditorUtility.SetDirty(uiManager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[AutoSetup] ✅ UI elements connected successfully!");
        }
    }
}
