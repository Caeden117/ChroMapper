﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class PersistentUI : MonoBehaviour {

    public enum DisplayMessageType {
        BOTTOM,
        CENTER
    }

    private static PersistentUI _instance;
    public static PersistentUI Instance => _instance;

    private void Awake() {
        if (_instance != null) {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        _instance = this;
    }

    [SerializeField]
    private Localization localization;

    [Header("Loading")]
    [SerializeField]
    private CanvasGroup loadingCanvasGroup;
    [SerializeField]
    private TMP_Text loadingTip;
    [SerializeField]
    private Image editorLoadingBackground;
    [SerializeField]
    private ImageList editorImageList;
    public Slider LevelLoadSlider;
    public TextMeshProUGUI LevelLoadSliderLabel;

    [SerializeField]
    private AnimationCurve fadeInCurve;
    [SerializeField]
    private AnimationCurve fadeOutCurve;

    [Header("Tooltip")]
    private bool showTooltip;
    [SerializeField]
    private Text tooltipText;
    [SerializeField]
    GameObject tooltipObject;
    [SerializeField]
    RectTransform tooltipPanelRect;
    [SerializeField]
    Vector3 tooltipOffset;
    [SerializeField]
    HorizontalLayoutGroup tooltipLayout;

    private string currentTooltipMessage;
    private string currentTooltipAdvancedMessage;

    [Header("Dialog Box")]
    [SerializeField] private CM_DialogBox dialogBox;
    [SerializeField] private TMP_FontAsset greenFont;
    [SerializeField] private TMP_FontAsset redFont;
    [SerializeField] private TMP_FontAsset goldFont;

    [Header("Input Box")]
    [SerializeField] private CM_InputBox inputBox;

    public bool DialogBox_IsEnabled => dialogBox.IsEnabled;

    public bool InputBox_IsEnabled => inputBox.IsEnabled;

    [Header("Center Message")]

    [SerializeField]
    private MessageDisplayer centerDisplay;

    [SerializeField]
    private MessageDisplayer bottomDisplay;

    private void Start() {
        LocalizationSettings.SelectedLocale = Locale.CreateLocale(Settings.Instance.Language);
        AudioListener.volume = Settings.Instance.Volume;
        centerDisplay.host = this;
        bottomDisplay.host = this;
    }

    private void LateUpdate() {
        if (showTooltip) UpdateTooltipPosition();
    }

    private void OnApplicationQuit()
    {
        ColourHistory.Save();
        Settings.Instance.Save();
    }

    public void DisplayMessage(string message, DisplayMessageType type) {
        switch (type) {
            case DisplayMessageType.BOTTOM: bottomDisplay.DisplayMessage(message); break;
            case DisplayMessageType.CENTER: centerDisplay.DisplayMessage(message); break;
        }
    }
    
    #region loading
    public static void UpdateBackground(BeatSaberSong song)
    {
        if (Instance.editorLoadingBackground.gameObject.activeSelf == false) Instance.editorLoadingBackground.gameObject.SetActive(true);
        Instance.editorLoadingBackground.sprite = Instance.editorImageList.GetBGSprite(song);
    }

    public Coroutine FadeInLoadingScreen() {
        loadingTip.text = localization.GetRandomLoadingMessage();
        return StartCoroutine(FadeInLoadingScreen(2f));
    }

    IEnumerator FadeInLoadingScreen(float rate) {
        loadingCanvasGroup.blocksRaycasts = true;
        loadingCanvasGroup.interactable = true;
        float t = 0;
        while (t < 1) {
            loadingCanvasGroup.alpha = fadeInCurve.Evaluate(t);
            t += Time.deltaTime * rate;
            yield return null;
        }
        loadingCanvasGroup.alpha = 1;
    }

    public Coroutine FadeOutLoadingScreen() {
        return StartCoroutine(FadeOutLoadingScreen(2f));
    }

    IEnumerator FadeOutLoadingScreen(float rate) {
        float t = 1;
        while (t > 0) {
            loadingCanvasGroup.alpha = fadeOutCurve.Evaluate(t);
            t -= Time.deltaTime * rate;
            yield return null;
        }
        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.blocksRaycasts = false;
        loadingCanvasGroup.interactable = false;
        Instance.editorLoadingBackground.gameObject.SetActive(false);
    }
    #endregion

    #region tooltip
    public void SetTooltip(string message, string advancedMessage = null)
    {
        currentTooltipMessage = message;
        currentTooltipAdvancedMessage = !string.IsNullOrEmpty(advancedMessage) ? advancedMessage : null;
    }

    public void ShowTooltip() {
        showTooltip = true;
    }

    public void HideTooltip() {
        showTooltip = false;
        if (tooltipObject != null) tooltipObject?.SetActive(false);
    }

    private void UpdateTooltipPosition() {

        if (Input.GetKey(KeyCode.LeftControl) && currentTooltipAdvancedMessage != null) tooltipText.text = currentTooltipAdvancedMessage;
        else tooltipText.text = currentTooltipMessage;
        tooltipText.color = Color.white; //idk if anyone else gets this but sometimes the text goes black and becomes unreadable

        if (!tooltipObject.activeSelf) tooltipObject.SetActive(true);

#if UNITY_EDITOR
        Vector2 gameSize = UnityEditor.Handles.GetMainGameViewSize();
        float screenWidth = gameSize.x;
        float screenHeight = gameSize.y;
#else
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
#endif

        float rectWidth = tooltipPanelRect.rect.width;
        float rectHeight = tooltipPanelRect.rect.height;

        Vector2 clamped = new Vector2(
            Mathf.Clamp(Input.mousePosition.x, rectWidth, screenWidth - rectWidth),
            Mathf.Clamp(Input.mousePosition.y + (rectHeight - 4), rectHeight, screenHeight - rectHeight)
            );
        tooltipPanelRect.position = clamped;
    }
    #endregion

    #region Dialog and Input Box
    /// <summary>
    /// Show a dialog box created automatically with a preset selection of common uses.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="result">Result to invoke, based on the button ID pressed: Left (0), Middle (1), Right (2).</param>
    /// <param name="preset">Preset to automatically set up the rest of the dialog box for you.</param>
    public void ShowDialogBox(string message, Action<int> result, DialogBoxPresetType preset)
    {
        switch (preset)
        {
            case DialogBoxPresetType.Ok:
                dialogBox.SetParams(message, result, "OK", null, null, greenFont);
                break;
            case DialogBoxPresetType.OkCancel:
                dialogBox.SetParams(message, result, "OK", "Cancel", null, greenFont, goldFont);
                break;
            case DialogBoxPresetType.YesNo:
                dialogBox.SetParams(message, result, "Yes", "No", null, greenFont, redFont);
                break;
            case DialogBoxPresetType.YesNoCancel:
                dialogBox.SetParams(message, result, "Yes", "No", "Cancel", greenFont, redFont, goldFont);
                break;
        }
    }

    /// <summary>
    /// Show a custom-made dialog box with up to 3 buttons to choose from, and up to 3 TMP Font Assets to spice up visuals.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="result">Result to invoke, based on the button ID pressed: Left (0), Middle (1), Right (2).</param>
    /// <param name="b0">Custom Button 0 text.</param>
    /// <param name="b1">Custom Button 1 text.</param>
    /// <param name="b2">Custom Button 2 text.</param>
    /// <param name="b0a">Custom Button 0 TMP Font Asset.</param>
    /// <param name="b1a">Custom Button 1 TMP Font Asset.</param>
    /// <param name="b2a">Custom Button 2 TMP Font Asset.</param>
    public void ShowDialogBox(string message, Action<int> result, string b0 = null, string b1 = null, string b2 = null,
        TMP_FontAsset b0a = null, TMP_FontAsset b1a = null, TMP_FontAsset b2a = null)
    {
        dialogBox.SetParams(message, result, b0, b1, b2, b0a, b1a, b2a);
    }

    public void ShowInputBox(string message, Action<string> result, string defaultText = "")
    {
        inputBox.SetParams(message, result, defaultText);
    }

    public enum DialogBoxPresetType
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel
    }
    #endregion

    /*
     * Notification messages
     */

    #region notifications

    [Serializable]
    public class MessageDisplayer {

        [SerializeField]
        TMP_Text messageText;

        public MonoBehaviour host;

        bool isShowingMessages;
        private Queue<string> messagesQueue = new Queue<string>();

        IEnumerator MessageRoutine() {
            isShowingMessages = true;
            while (messagesQueue.Count > 0) {
                yield return host.StartCoroutine(MessageFadingRoutine(messagesQueue.Dequeue()));
            }
            isShowingMessages = false;
        }

        IEnumerator MessageFadingRoutine(string message) {
            float t = 0;
            messageText.alpha = 0;
            messageText.text = message;
            while (t < 1) {
                yield return null;
                t += Time.deltaTime;
                if (t > 1) t = 1;
                messageText.alpha = t;
            }
            yield return new WaitForSeconds(2f);
            while (t > 0) {
                yield return null;
                t -= Time.deltaTime;
                if (t < 0) t = 0;
                messageText.alpha = t;
            }
        }

        public void DisplayMessage(string message) {
            messagesQueue.Enqueue(message);
            if (!isShowingMessages) host.StartCoroutine(MessageRoutine());
        }

    }
    #endregion

}
