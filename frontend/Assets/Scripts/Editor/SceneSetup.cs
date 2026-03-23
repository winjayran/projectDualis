using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace ProjectDualis.Editor
{
    /// <summary>
    /// Scene setup utility for Project Dualis.
    /// Creates the complete UI hierarchy in the Main scene.
    /// </summary>
    public class SceneSetup
    {
        [MenuItem("ProjectDualis/Setup Main Scene")]
        public static void SetupMainScene()
        {
            // Get or create the Main scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!scene.name.Equals("Main"))
            {
                Debug.LogWarning("Please open the Main scene first.");
                return;
            }

            // Clear existing UI if any
            var existingCanvas = GameObject.Find("DualisCanvas");
            if (existingCanvas != null)
            {
                if (!EditorUtility.DisplayDialog("Setup Main Scene",
                    "UI Canvas already exists. Replace it?", "Replace", "Cancel"))
                {
                    return;
                }
                DestroyImmediate(existingCanvas);
            }

            // Create Canvas
            GameObject canvasGO = new GameObject("DualisCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create EventSystem if not exists
            if (EventSystem.current == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            // Create Panels
            CreateChatPanel(canvasGO.transform);
            CreateStatusBar(canvasGO.transform);
            CreateSettingsPanel(canvasGO.transform);

            // Mark scene as dirty
            EditorUtility.SetDirty(canvasGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("[SceneSetup] Main scene setup complete!");
        }

        private static void CreateChatPanel(Transform parent)
        {
            // Main Chat Panel
            GameObject chatPanel = new GameObject("ChatPanel");
            RectTransform chatRect = chatPanel.AddComponent<RectTransform>();
            chatPanel.transform.SetParent(parent, false);
            chatRect.anchorMin = new Vector2(0, 0.3f);
            chatRect.anchorMax = new Vector2(1, 1);
            chatRect.offsetMin = Vector2.zero;
            chatRect.offsetMax = Vector2.zero;

            Image chatImage = chatPanel.AddComponent<Image>();
            chatImage.color = new Color(0, 0, 0, 0.5f);

            // Chat Display (Scroll View)
            GameObject scrollGO = new GameObject("ChatDisplay");
            RectTransform scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollGO.transform.SetParent(chatPanel.transform, false);
            scrollRect.anchorMin = new Vector2(0, 0.15f);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(20, 10);
            scrollRect.offsetMax = new Vector2(-20, -10);

            ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.scrollSensitivity = 30;

            // Viewport
            GameObject viewportGO = new GameObject("Viewport");
            RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportGO.transform.SetParent(scrollGO.transform, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = Vector2.zero;

            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.3f);
            Mask viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            scroll.viewport = viewportRect;

            // Content
            GameObject contentGO = new GameObject("Content");
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentGO.transform.SetParent(viewportGO.transform, false);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 100);

            VerticalLayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            // Chat Messages Container (TextMeshPro)
            GameObject messagesGO = new GameObject("Messages");
            RectTransform messagesRect = messagesGO.AddComponent<RectTransform>();
            messagesGO.transform.SetParent(contentGO.transform, false);
            messagesRect.anchorMin = Vector2.zero;
            messagesRect.anchorMax = Vector2.one;
            messagesRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI messagesText = messagesGO.AddComponent<TextMeshProUGUI>();
            messagesText.fontSize = 18;
            messagesText.color = Color.white;
            messagesText.alignment = TextAlignmentOptions.TopLeft;
            messagesText.enableWordWrapping = true;

            // Scrollbar
            GameObject scrollbarGO = new GameObject("Scrollbar");
            RectTransform scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
            scrollbarGO.transform.SetParent(scrollGO.transform, false);
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.pivot = new Vector2(1, 1);

            Image scrollbarBg = scrollbarGO.AddComponent<Image>();
            scrollbarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Handle
            GameObject handleGO = new GameObject("Handle");
            RectTransform handleRect = handleGO.AddComponent<RectTransform>();
            handleGO.transform.SetParent(scrollbarGO.transform, false);
            handleRect.sizeDelta = new Vector2(20, 20);

            Image handleImage = handleGO.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);

            scrollbar.handleRect = handleRect;

            // Input Panel
            GameObject inputPanelGO = new GameObject("InputPanel");
            RectTransform inputRect = inputPanelGO.AddComponent<RectTransform>();
            inputPanelGO.transform.SetParent(chatPanel.transform, false);
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.15f);
            inputRect.offsetMin = new Vector2(20, 10);
            inputRect.offsetMax = new Vector2(-20, -10));

            Image inputImage = inputPanelGO.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            // Input Field
            GameObject inputFieldGO = new GameObject("ChatInputField");
            RectTransform inputFieldRect = inputFieldGO.AddComponent<RectTransform>();
            inputFieldGO.transform.SetParent(inputPanelGO.transform, false);
            inputFieldRect.anchorMin = new Vector2(0, 0);
            inputFieldRect.anchorMax = new Vector2(0.7f, 1);
            inputFieldRect.offsetMin = new Vector2(10, 5);
            inputFieldRect.offsetMax = new Vector2(-5, -5);

            Image inputFieldImage = inputFieldGO.AddComponent<Image>();
            inputFieldImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            TMP_InputField inputField = inputFieldGO.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            inputField.contentType = TMP_InputField.ContentType.Standard;

            // Text Area
            GameObject textAreaGO = new GameObject("TextArea");
            RectTransform textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaGO.transform.SetParent(inputFieldGO.transform, false);
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(5, 5);
            textAreaRect.offsetMax = new Vector2(-5, -5);

            // Placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.fontSize = 16;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1);
            placeholderText.text = "Type a message...";

            inputField.textViewport = textAreaRect;
            inputField.textComponent = null; // Will be set below
            inputField.placeholder = placeholderText;

            // Text
            GameObject textGO = new GameObject("Text");
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textGO.transform.SetParent(textAreaGO.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI fieldText = textGO.AddComponent<TextMeshProUGUI>();
            fieldText.fontSize = 16;
            fieldText.color = Color.white;

            inputField.textComponent = fieldText;

            // Send Button
            GameObject sendButtonGO = new GameObject("SendButton");
            RectTransform sendButtonRect = sendButtonGO.AddComponent<RectTransform>();
            sendButtonGO.transform.SetParent(inputPanelGO.transform, false);
            sendButtonRect.anchorMin = new Vector2(0.7f, 0);
            sendButtonRect.anchorMax = new Vector2(0.85f, 1);
            sendButtonRect.offsetMin = new Vector2(5, 5);
            sendButtonRect.offsetMax = new Vector2(-5, -5);

            Image sendButtonImage = sendButtonGO.AddComponent<Image>();
            sendButtonImage.color = new Color(0.2f, 0.6f, 1f, 1);

            Button sendButton = sendButtonGO.AddComponent<Button>();

            GameObject sendTextGO = new GameObject("Text");
            sendTextGO.transform.SetParent(sendButtonGO.transform, false);
            RectTransform sendTextRect = sendTextGO.AddComponent<RectTransform>();
            sendTextRect.anchorMin = Vector2.zero;
            sendTextRect.anchorMax = Vector2.one;

            TextMeshProUGUI sendText = sendTextGO.AddComponent<TextMeshProUGUI>();
            sendText.text = "Send";
            sendText.fontSize = 18;
            sendText.color = Color.white;
            sendText.alignment = TextAlignmentOptions.Center;

            // Voice Button
            GameObject voiceButtonGO = new GameObject("VoiceButton");
            RectTransform voiceButtonRect = voiceButtonGO.AddComponent<RectTransform>();
            voiceButtonGO.transform.SetParent(inputPanelGO.transform, false);
            voiceButtonRect.anchorMin = new Vector2(0.85f, 0);
            voiceButtonRect.anchorMax = new Vector2(1, 1);
            voiceButtonRect.offsetMin = new Vector2(5, 5);
            voiceButtonRect.offsetMax = new Vector2(-5, -5);

            Image voiceButtonImage = voiceButtonGO.AddComponent<Image>();
            voiceButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1);

            Button voiceButton = voiceButtonGO.AddComponent<Button>();

            GameObject voiceTextGO = new GameObject("Text");
            voiceTextGO.transform.SetParent(voiceButtonGO.transform, false);
            RectTransform voiceTextRect = voiceTextGO.AddComponent<RectTransform>();
            voiceTextRect.anchorMin = Vector2.zero;
            voiceTextRect.anchorMax = Vector2.one;

            TextMeshProUGUI voiceText = voiceTextGO.AddComponent<TextMeshProUGUI>();
            voiceText.text = "🎤";
            voiceText.fontSize = 24;
            voiceText.color = Color.white;
            voiceText.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateStatusBar(Transform parent)
        {
            // Status Bar Panel
            GameObject statusPanelGO = new GameObject("StatusBar");
            RectTransform statusRect = statusPanelGO.AddComponent<RectTransform>();
            statusPanelGO.transform.SetParent(parent, false);
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.1f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            Image statusImage = statusPanelGO.AddComponent<Image>();
            statusImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            HorizontalLayoutGroup layout = statusPanelGO.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 5, 5);
            layout.spacing = 20;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            // Connection Status Indicator
            GameObject connIndicatorGO = new GameObject("ConnectionIndicator");
            connIndicatorGO.transform.SetParent(statusPanelGO.transform, false);
            RectTransform connRect = connIndicatorGO.AddComponent<RectTransform>();
            connRect.sizeDelta = new Vector2(20, 20);

            Image connImage = connIndicatorGO.AddComponent<Image>();
            connImage.color = Color.red;

            // Status Text
            GameObject statusTextGO = new GameObject("StatusText");
            statusTextGO.transform.SetParent(statusPanelGO.transform, false);
            RectTransform statusTextRect = statusTextGO.AddComponent<RectTransform>();
            statusTextRect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI statusText = statusTextGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Disconnected";
            statusText.fontSize = 16;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.Left;

            // Mode Toggle
            GameObject modeToggleGO = new GameObject("ModeToggle");
            modeToggleGO.transform.SetParent(statusPanelGO.transform, false);
            RectTransform modeToggleRect = modeToggleGO.AddComponent<RectTransform>();
            modeToggleRect.sizeDelta = new Vector2(200, 30);

            Toggle modeToggle = modeToggleGO.AddComponent<Toggle>();

            GameObject modeBgGO = new GameObject("Background");
            modeBgGO.transform.SetParent(modeToggleGO.transform, false);
            RectTransform modeBgRect = modeBgGO.AddComponent<RectTransform>();
            modeBgRect.anchorMin = Vector2.zero;
            modeBgRect.anchorMax = Vector2.one;
            modeBgRect.sizeDelta = new Vector2(-30, 0);

            Image modeBgImage = modeBgGO.AddComponent<Image>();
            modeBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1);

            GameObject modeCheckGO = new GameObject("Checkmark");
            modeCheckGO.transform.SetParent(modeToggleGO.transform, false);
            RectTransform modeCheckRect = modeCheckGO.AddComponent<RectTransform>();
            modeCheckRect.anchorMin = new Vector2(0.5f, 0);
            modeCheckRect.anchorMax = new Vector2(1, 1);
            modeCheckRect.offsetMin = new Vector2(5, 0);
            modeCheckRect.sizeDelta = new Vector2(20, 20);

            Image modeCheckImage = modeCheckGO.AddComponent<Image>();
            modeCheckImage.color = Color.green;

            modeToggle.targetGraphic = modeBgImage;
            modeToggle.graphic = modeCheckImage;
            modeToggle.isOn = true;

            GameObject modeLabelGO = new GameObject("Label");
            modeLabelGO.transform.SetParent(modeToggleGO.transform, false);
            RectTransform modeLabelRect = modeLabelGO.AddComponent<RectTransform>();
            modeLabelRect.anchorMin = new Vector2(0, 0);
            modeLabelRect.anchorMax = new Vector2(0.5f, 1);
            modeLabelRect.offsetMin = new Vector2(25, 0);
            modeLabelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI modeLabel = modeLabelGO.AddComponent<TextMeshProUGUI>();
            modeLabel.text = "Companion Mode";
            modeLabel.fontSize = 14;
            modeLabel.color = Color.white;
            modeLabel.alignment = TextAlignmentOptions.Left;

            // Emotion Indicator
            GameObject emotionIndicatorGO = new GameObject("EmotionIndicator");
            emotionIndicatorGO.transform.SetParent(statusPanelGO.transform, false);
            RectTransform emotionRect = emotionIndicatorGO.AddComponent<RectTransform>();
            emotionRect.sizeDelta = new Vector2(20, 20);

            Image emotionImage = emotionIndicatorGO.AddComponent<Image>();
            emotionImage.color = Color.white;
        }

        private static void CreateSettingsPanel(Transform parent)
        {
            // Settings Panel (hidden by default)
            GameObject settingsPanelGO = new GameObject("SettingsPanel");
            RectTransform settingsRect = settingsPanelGO.AddComponent<RectTransform>();
            settingsPanelGO.transform.SetParent(parent, false);
            settingsRect.anchorMin = new Vector2(0.3f, 0.2f);
            settingsRect.anchorMax = new Vector2(0.7f, 0.8f);
            settingsRect.offsetMin = Vector2.zero;
            settingsRect.offsetMax = Vector2.zero;

            settingsPanelGO.SetActive(false);

            Image settingsImage = settingsPanelGO.AddComponent<Image>();
            settingsImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            VerticalLayoutGroup settingsLayout = settingsPanelGO.AddComponent<VerticalLayoutGroup>();
            settingsLayout.padding = new RectOffset(20, 20, 20, 20);
            settingsLayout.spacing = 10;
            settingsLayout.childControlHeight = true;
            settingsLayout.childControlWidth = true;
            settingsLayout.childForceExpandHeight = false;
            settingsLayout.childForceExpandWidth = true;

            // Settings Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(settingsPanelGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 50);

            TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Settings";
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;

            // Close Button
            GameObject closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(settingsPanelGO.transform, false);
            RectTransform closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
            closeBtnRect.sizeDelta = new Vector2(200, 40);

            Image closeBtnImage = closeBtnGO.AddComponent<Image>();
            closeBtnImage.color = new Color(0.6f, 0.2f, 0.2f, 1);

            Button closeBtn = closeBtnGO.AddComponent<Button>();

            GameObject closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
            RectTransform closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "Close";
            closeText.fontSize = 18;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;

            closeBtn.targetGraphic = closeBtnImage;
        }
    }
}
