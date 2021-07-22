using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class PersistentUI : MonoBehaviour {

    public enum DisplayMessageType {
        BOTTOM,
        CENTER
    }

    public static PersistentUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
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

    public bool DialogBox_IsEnabled => dialogBox.IsEnabled || DialogBox_Loading;
    public bool DialogBox_Loading = false;

    public bool InputBox_IsEnabled => inputBox.IsEnabled;

    [Header("Center Message")]

    [SerializeField]
    private MessageDisplayer centerDisplay;

    [SerializeField]
    private MessageDisplayer bottomDisplay;

    public bool enableTransitions = true;

    public UIDropdown DropdownPrefab;
    public UIButton ButtonPrefab;
    public UITextInput TextInputPrefab;
    public Sprites Sprites;

    private void Start() {
        CMInputCallbackInstaller.PersistentObject(transform);
        LocalizationSettings.SelectedLocale = Locale.CreateLocale(Settings.Instance.Language);

        UpdateDSPBufferSize();
        AudioListener.volume = Settings.Instance.Volume;

        RequirementCheck.Setup();

        centerDisplay.host = this;
        bottomDisplay.host = this;
    }

    private static void UpdateDSPBufferSize()
    {
        var config = AudioSettings.GetConfiguration();
        config.dspBufferSize = (int) Math.Pow(2, Settings.Instance.DSPBufferSize);
        AudioSettings.Reset(config);
    }

    private void LateUpdate() {
        if (showTooltip) UpdateTooltipPosition();
    }

    private void OnApplicationQuit()
    {
        ColourHistory.Save();
        Settings.Instance.Save();
    }

    public MessageDisplayer.NotificationMessage DisplayMessage(string message, DisplayMessageType type)
    {
        Debug.LogWarning($"Message not localized '{message}'");
        var notification = new MessageDisplayer.NotificationMessage(message, type);
        DoDisplayMessage(notification);
        return notification;
    }

    private void DoDisplayMessage(MessageDisplayer.NotificationMessage message)
    {
        switch (message.type)
        {
            case DisplayMessageType.BOTTOM: bottomDisplay.DisplayMessage(message); break;
            case DisplayMessageType.CENTER: centerDisplay.DisplayMessage(message); break;
        }
    }

    public MessageDisplayer.NotificationMessage DisplayMessage(string table, string key, DisplayMessageType type)
    {
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        var notification = new MessageDisplayer.NotificationMessage(message, type);
        StartCoroutine(DisplayMessage(notification));
        return notification;
    }

    public IEnumerator DisplayMessage(MessageDisplayer.NotificationMessage notifiation)
    {
        yield return notifiation.LoadMessage();
        DoDisplayMessage(notifiation);
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
        while (t < 1 && enableTransitions) {
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
        while (t > 0 && enableTransitions) {
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
        Debug.LogWarning($"Dialog box not localized '{message}'");
        DialogBox_Loading = true;
        DoShowDialogBox(message, result, preset);
    }

    public void ShowDialogBox(string table, string key, Action<int> result, DialogBoxPresetType preset, object[] args = null)
    {
        DialogBox_Loading = true;
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        DoShowDialogBox(message, result, preset);
    }

    private void DoShowDialogBox(string message, Action<int> result, DialogBoxPresetType preset)
    {
        switch (preset)
        {
            case DialogBoxPresetType.Ok:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "ok"), new TMP_FontAsset[] { greenFont });
                break;
            case DialogBoxPresetType.OkCancel:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "ok", "cancel"), new TMP_FontAsset[] { greenFont, goldFont });
                break;
            case DialogBoxPresetType.YesNo:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "yes", "no"), new TMP_FontAsset[] { greenFont, redFont });
                break;
            case DialogBoxPresetType.YesNoCancel:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "yes", "no", "cancel"), new TMP_FontAsset[] { greenFont, redFont, goldFont });
                break;
            case DialogBoxPresetType.OkIgnore:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "ok", "ignore"), null);
                break;
        }
    }

    private List<string> GetStrings(string table, params string[] keys)
    {
        return keys.Select(key => 
            LocalizationSettings.StringDatabase.GetLocalizedString(table, key)
        ).ToList();
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
    public void ShowDialogBox(string table, string key, Action<int> result, List<string> buttonText,
        TMP_FontAsset[] ba)
    {
        DialogBox_Loading = true;
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        DoShowDialogBox(message, result, buttonText, ba);
    }

    public void ShowDialogBox(string table, string key, Action<int> result, string[] buttonTexts,
        TMP_FontAsset[] ba = null)
    {
        ShowDialogBox(table, key, result, GetStrings(table, buttonTexts), ba);
    }

    public void ShowDialogBox(string message, Action<int> result, string b0 = null, string b1 = null, string b2 = null,
           TMP_FontAsset b0a = null, TMP_FontAsset b1a = null, TMP_FontAsset b2a = null)
    {
        Debug.LogWarning($"Dialog box not localized '{message}'");
        dialogBox.SetParams(message, result, new string[] { b0, b1, b2 }, new TMP_FontAsset[] { b0a, b1a, b2a });
    }

    private void DoShowDialogBox(string message, Action<int> result, List<string> buttonText,
        TMP_FontAsset[] ba)
    {
        dialogBox.SetParams(message, result, buttonText.ToArray(), ba);
        DialogBox_Loading = false;
    }


    public void ShowInputBox(string message, Action<string> result, string defaultText = "")
    {
        Debug.LogWarning($"Input box not localized '{message}'");
        inputBox.SetParams(message, result, defaultText);
    }

    public void ShowInputBox(string table, string key, Action<string> result, string defaultTextKey = "", string defaultDefault = "")
    {
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        var defaultTextStr = defaultDefault;
        if (!string.IsNullOrEmpty(defaultTextKey))
        {
            var defaultText = LocalizationSettings.StringDatabase.GetLocalizedString(table, defaultTextKey);
            defaultTextStr = defaultText;
        }

        inputBox.SetParams(message, result, defaultTextStr);
    }

    public enum DialogBoxPresetType
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel,
        OkIgnore
    }
    #endregion

    /*
     * Notification messages
     */

    #region notifications

    [Serializable]
    public class MessageDisplayer {

        public class NotificationMessage
        {
            private AsyncOperationHandle<string>? _localisable;

            public float waitTime = 2.0f;
            public string message;
            public bool cancelled = false;
            public bool skipDisplay = false;
            public bool skipFade = false;
            public readonly DisplayMessageType type;

            public NotificationMessage(AsyncOperationHandle<string> localisable, DisplayMessageType type)
            {
                _localisable = localisable;
                this.type = type;
            }

            public NotificationMessage(string message, DisplayMessageType type)
            {
                this.message = message;
                this.type = type;
            }

            public IEnumerator LoadMessage()
            {
                if (!_localisable.HasValue) yield break;

                yield return _localisable.Value;
                message = _localisable.Value.Result;
            }
        }

        [SerializeField]
        TMP_Text messageText;

        public MonoBehaviour host;

        bool isShowingMessages;
        private Queue<NotificationMessage> messagesQueue = new Queue<NotificationMessage>();

        IEnumerator MessageRoutine() {
            isShowingMessages = true;
            while (messagesQueue.Count > 0)
            {
                var message = messagesQueue.Dequeue();
                if (!message.cancelled)
                {
                    yield return host.StartCoroutine(MessageFadingRoutine(message));
                }
            }
            isShowingMessages = false;
        }

        IEnumerator MessageFadingRoutine(NotificationMessage message)
        {
            // Fade in
            float t = 0;
            messageText.alpha = 0;
            messageText.text = message.message;
            while (t < 1 && !message.cancelled && !message.skipFade) {
                yield return null;
                t += Time.deltaTime;
                if (t > 1) t = 1;
                messageText.alpha = t;
            }

            // Wait for 2 seconds
            messageText.alpha = 1;
            yield return new WaitForFixedUpdate();
            while (t <= message.waitTime && !message.cancelled && !message.skipDisplay) {
                t += Time.deltaTime;
                yield return null;
            }

            // Fade out
            t = 1;
            yield return new WaitForFixedUpdate();
            while (t > 0 && !message.cancelled && !message.skipFade) {
                yield return null;
                t -= Time.deltaTime;
                if (t < 0) t = 0;
                messageText.alpha = t;
            }
            messageText.alpha = 0;
        }

        public void DisplayMessage(NotificationMessage message) {
            messagesQueue.Enqueue(message);
            if (!isShowingMessages) host.StartCoroutine(MessageRoutine());
        }

    }
    #endregion

}
