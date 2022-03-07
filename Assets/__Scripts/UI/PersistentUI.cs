using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PersistentUI : MonoBehaviour
{
    public enum DisplayMessageType
    {
        Bottom,
        Center
    }

    public Slider LevelLoadSlider;
    public TextMeshProUGUI LevelLoadSliderLabel;

    [SerializeField] private Localization localization;
    [Header("Loading")] [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TMP_Text loadingTip;
    [SerializeField] private Image editorLoadingBackground;
    [SerializeField] private ImageList editorImageList;

    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private Text tooltipText;
    [SerializeField] private GameObject tooltipObject;
    [SerializeField] private RectTransform tooltipPanelRect;
    [SerializeField] private Vector3 tooltipOffset;
    [SerializeField] private HorizontalLayoutGroup tooltipLayout;

    [Header("Dialog Box")]
    // This isn't strictly required but I need the scriptable object to be loaded by Unity, and this can garauntee that.
    [SerializeField] private ComponentStoreSO componentStore;
    [SerializeField] private DialogBox newDialogBoxPrefab;
    [SerializeField] private CM_DialogBox dialogBox;
    [SerializeField] private TMP_FontAsset greenFont;
    [SerializeField] private TMP_FontAsset redFont;
    [SerializeField] private TMP_FontAsset goldFont;

    [Header("Input Box")] [SerializeField] private CM_InputBox inputBox;

    [FormerlySerializedAs("DialogBox_Loading")] public bool DialogBoxLoading;

    [Header("Center Message")]
    [SerializeField]
    private MessageDisplayer centerDisplay;

    [SerializeField] private MessageDisplayer bottomDisplay;

    [FormerlySerializedAs("enableTransitions")] public bool EnableTransitions = true;

    public UIDropdown DropdownPrefab;
    public UIButton ButtonPrefab;
    public UITextInput TextInputPrefab;
    public Sprites Sprites;
    private string currentTooltipAdvancedMessage;

    private string currentTooltipMessage;

    [Header("Tooltip")] private bool showTooltip;

    public static PersistentUI Instance { get; private set; }

    public bool DialogBoxIsEnabled => dialogBox.IsEnabled || DialogBoxLoading;

    public bool InputBoxIsEnabled => inputBox.IsEnabled;

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

    [Header("Color Input Box")]
    [SerializeField] private CM_ColorInputBox colorInputBox;

    public bool ColorInputBox_IsEnabled => colorInputBox.IsEnabled;

    private void Start()
    {
        CMInputCallbackInstaller.PersistentObject(transform);
        LocalizationSettings.SelectedLocale = Locale.CreateLocale(Settings.Instance.Language);

        UpdateDSPBufferSize();
        AudioListener.volume = Settings.Instance.Volume;

        RequirementCheck.Setup();

        centerDisplay.Host = this;
        bottomDisplay.Host = this;
    }

    private void LateUpdate()
    {
        if (showTooltip) UpdateTooltipPosition();
    }

    private void OnApplicationQuit()
    {
        ColourHistory.Save();
        Settings.Instance.Save();
    }

    private static void UpdateDSPBufferSize()
    {
        var config = AudioSettings.GetConfiguration();
        config.dspBufferSize = (int)Math.Pow(2, Settings.Instance.DSPBufferSize);
        AudioSettings.Reset(config);
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
        switch (message.Type)
        {
            case DisplayMessageType.Bottom:
                bottomDisplay.DisplayMessage(message);
                break;
            case DisplayMessageType.Center:
                centerDisplay.DisplayMessage(message);
                break;
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

    /*
     * Notification messages
     */

    #region notifications

    [Serializable]
    public class MessageDisplayer
    {
        [SerializeField] private TMP_Text messageText;

        [FormerlySerializedAs("host")] public MonoBehaviour Host;
        private readonly Queue<NotificationMessage> messagesQueue = new Queue<NotificationMessage>();
        private bool isShowingMessages;

        private IEnumerator MessageRoutine()
        {
            isShowingMessages = true;
            while (messagesQueue.Count > 0)
            {
                var message = messagesQueue.Dequeue();
                if (!message.Cancelled) yield return Host.StartCoroutine(MessageFadingRoutine(message));
            }

            isShowingMessages = false;
        }

        private IEnumerator MessageFadingRoutine(NotificationMessage message)
        {
            // Fade in
            float t = 0;
            messageText.alpha = 0;
            messageText.text = message.Message;
            while (t < 1 && !message.Cancelled && !message.SkipFade)
            {
                yield return null;
                t += Time.deltaTime;
                if (t > 1) t = 1;
                messageText.alpha = t;
            }

            // Wait for 2 seconds
            messageText.alpha = 1;
            yield return new WaitForFixedUpdate();
            while (t <= message.WaitTime && !message.Cancelled && !message.SkipDisplay)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Fade out
            t = 1;
            yield return new WaitForFixedUpdate();
            while (t > 0 && !message.Cancelled && !message.SkipFade)
            {
                yield return null;
                t -= Time.deltaTime;
                if (t < 0) t = 0;
                messageText.alpha = t;
            }

            messageText.alpha = 0;
        }

        public void DisplayMessage(NotificationMessage message)
        {
            messagesQueue.Enqueue(message);
            if (!isShowingMessages) Host.StartCoroutine(MessageRoutine());
        }

        public class NotificationMessage
        {
            public readonly DisplayMessageType Type;
            private AsyncOperationHandle<string>? localisable;
            public bool Cancelled = false;
            public string Message;
            public bool SkipDisplay = false;
            public bool SkipFade = false;

            public float WaitTime = 2.0f;

            public NotificationMessage(AsyncOperationHandle<string> localisable, DisplayMessageType type)
            {
                this.localisable = localisable;
                Type = type;
            }

            public NotificationMessage(string message, DisplayMessageType type)
            {
                Message = message;
                Type = type;
            }

            public IEnumerator LoadMessage()
            {
                if (!localisable.HasValue) yield break;

                yield return localisable.Value;
                Message = localisable.Value.Result;
            }
        }
    }

    #endregion

    #region loading

    public static void UpdateBackground(BeatSaberSong song)
    {
        if (Instance.editorLoadingBackground.gameObject.activeSelf == false)
            Instance.editorLoadingBackground.gameObject.SetActive(true);
        Instance.editorLoadingBackground.sprite = Instance.editorImageList.GetBgSprite(song);
    }

    public Coroutine FadeInLoadingScreen()
    {
        loadingTip.text = localization.GetRandomLoadingMessage();
        return StartCoroutine(FadeInLoadingScreen(2f));
    }

    private IEnumerator FadeInLoadingScreen(float rate)
    {
        loadingCanvasGroup.blocksRaycasts = true;
        loadingCanvasGroup.interactable = true;
        float t = 0;
        while (t < 1 && EnableTransitions)
        {
            loadingCanvasGroup.alpha = fadeInCurve.Evaluate(t);
            t += Time.deltaTime * rate;
            yield return null;
        }

        loadingCanvasGroup.alpha = 1;
    }

    public Coroutine FadeOutLoadingScreen() => StartCoroutine(FadeOutLoadingScreen(2f));

    private IEnumerator FadeOutLoadingScreen(float rate)
    {
        float t = 1;
        while (t > 0 && EnableTransitions)
        {
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

    public void ShowTooltip() => showTooltip = true;

    public void HideTooltip()
    {
        showTooltip = false;
        if (tooltipObject != null) tooltipObject.SetActive(false);
    }

    private void UpdateTooltipPosition()
    {
        if (Input.GetKey(KeyCode.LeftControl) && currentTooltipAdvancedMessage != null)
            tooltipText.text = currentTooltipAdvancedMessage;
        else tooltipText.text = currentTooltipMessage;
        tooltipText.color =
            Color.white; //idk if anyone else gets this but sometimes the text goes black and becomes unreadable

        if (!tooltipObject.activeSelf) tooltipObject.SetActive(true);

#if UNITY_EDITOR
        var gameSize = Handles.GetMainGameViewSize();
        var screenWidth = gameSize.x;
        var screenHeight = gameSize.y;
#else
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
#endif

        var rectWidth = tooltipPanelRect.rect.width;
        var rectHeight = tooltipPanelRect.rect.height;

        var clamped = new Vector2(
            Mathf.Clamp(Input.mousePosition.x, rectWidth, screenWidth - rectWidth),
            Mathf.Clamp(Input.mousePosition.y + (rectHeight - 4), rectHeight, screenHeight - rectHeight)
        );
        tooltipPanelRect.position = clamped;
    }

    #endregion

    #region Dialog and Input Box

    /// <summary>
    /// Creates a new Dialog Box powered by CMUI.
    /// </summary>
    /// <remarks>
    /// By default, this dialog box will automatically be destroyed when it is closed.
    /// To prevent this behavior, call <see cref="DialogBox.DontDestroyOnClose"/>.
    /// </remarks>
    /// <returns>The newly instantiated <see cref="DialogBox"/>.</returns>
    public DialogBox CreateNewDialogBox() => Instantiate(newDialogBoxPrefab, transform);

    /// <summary>
    ///     Show a dialog box created automatically with a preset selection of common uses.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="result">Result to invoke, based on the button ID pressed: Left (0), Middle (1), Right (2).</param>
    /// <param name="preset">Preset to automatically set up the rest of the dialog box for you.</param>
    public void ShowDialogBox(string message, Action<int> result, DialogBoxPresetType preset)
    {
        Debug.LogWarning($"Dialog box not localized '{message}'");
        DialogBoxLoading = true;
        DoShowDialogBox(message, result, preset);
    }

    public void ShowDialogBox(string table, string key, Action<int> result, DialogBoxPresetType preset,
        object[] args = null)
    {
        DialogBoxLoading = true;
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        DoShowDialogBox(message, result, preset);
    }

    private void DoShowDialogBox(string message, Action<int> result, DialogBoxPresetType preset)
    {
        switch (preset)
        {
            case DialogBoxPresetType.Ok:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "ok"), new[] { greenFont });
                break;
            case DialogBoxPresetType.OkCancel:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "ok", "cancel"),
                    new[] { greenFont, goldFont });
                break;
            case DialogBoxPresetType.YesNo:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "yes", "no"), new[] { greenFont, redFont });
                break;
            case DialogBoxPresetType.YesNoCancel:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "yes", "no", "cancel"),
                    new[] { greenFont, redFont, goldFont });
                break;
            case DialogBoxPresetType.OkIgnore:
                DoShowDialogBox(message, result, GetStrings("PersistentUI", "ok", "ignore"), new[] { greenFont, goldFont });
                break;
        }
    }

    private List<string> GetStrings(string table, params string[] keys) =>
        keys.Select(key =>
            LocalizationSettings.StringDatabase.GetLocalizedString(table, key)
        ).ToList();

    /// <summary>
    ///     Show a custom-made dialog box with up to 3 buttons to choose from, and up to 3 TMP Font Assets to spice up visuals.
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
        DialogBoxLoading = true;
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        DoShowDialogBox(message, result, buttonText, ba);
    }

    public void ShowDialogBox(string table, string key, Action<int> result, string[] buttonTexts,
        TMP_FontAsset[] ba = null) => ShowDialogBox(table, key, result, GetStrings(table, buttonTexts), ba);

    public void ShowDialogBox(string message, Action<int> result, string b0 = null, string b1 = null, string b2 = null,
        TMP_FontAsset b0A = null, TMP_FontAsset b1A = null, TMP_FontAsset b2A = null)
    {
        Debug.LogWarning($"Dialog box not localized '{message}'");
        DoShowDialogBox(message, result, new[] { b0, b1, b2 }, new[] { b0A, b1A, b2A });
    }

    private void DoShowDialogBox(string message, Action<int> result, IList<string> buttonText,
        TMP_FontAsset[] ba)
    {
        //dialogBox.SetParams(message, result, buttonText.ToArray(), ba);
        var dialogBox = CreateNewDialogBox().WithNoTitle();

        var title = dialogBox.AddComponent<TextComponent>().WithInitialValue(message);

        foreach (var text in buttonText)
        {
            // This may seem unnecessary but it actually fixes a bug with this retrofit
            // Using a standard "for" loop will cause the Action lambda (a few lines of code down) to always return
            //    the last value of i.
            var i = buttonText.IndexOf(text);

            var button = dialogBox.AddFooterButton(() => result?.Invoke(i), text);
        
            if (i < ba.Length && ba[i].material.shaderKeywords.Contains("GLOW_ON"))
            {
                var color = ba[i].material.GetColor("_GlowColor");
                button.WithBackgroundColor(color.Multiply(color.a).WithAlpha(1).WithSatuation(0.5f));
            }
        }

        dialogBox.Open();

        DialogBoxLoading = false;
    }


    public void ShowInputBox(string message, Action<string> result, string defaultText = "")
    {
        Debug.LogWarning($"Input box not localized '{message}'");
        DoShowInputBox(message, result, defaultText);
    }

    public void ShowInputBox(string table, string key, Action<string> result, string defaultTextKey = "",
        string defaultDefault = "")
    {
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        var defaultTextStr = defaultDefault;
        if (!string.IsNullOrEmpty(defaultTextKey))
        {
            var defaultText = LocalizationSettings.StringDatabase.GetLocalizedString(table, defaultTextKey);
            defaultTextStr = defaultText;
        }

        DoShowInputBox(message, result, defaultTextStr);
    }

    private void DoShowInputBox(string message, Action<string> result, string defaultText)
    {
        var dialogBox = CreateNewDialogBox().WithNoTitle();

        var title = dialogBox.AddComponent<TextComponent>().WithInitialValue(message);

        var textBox = dialogBox.AddComponent<TextBoxComponent>()
            .WithInitialValue(defaultText)
            .WithNoLabel();

        var cancelButton = dialogBox
            .AddFooterButton(() => result?.Invoke(null),
                LocalizationSettings.StringDatabase.GetLocalizedString(nameof(PersistentUI), "cancel"));

        var submitButton = dialogBox
            .AddFooterButton(() => result?.Invoke(textBox.Value),
                LocalizationSettings.StringDatabase.GetLocalizedString(nameof(PersistentUI), "submit"));

        dialogBox.Open();
    }


    public void ShowColorInputBox(string table, string key, Action<Color?> result, Color selctedColor, string defaultTextKey = "", string defaultDefault = "")
    {
        var message = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        var defaultTextStr = defaultDefault;
        if (!string.IsNullOrEmpty(defaultTextKey))
        {
            var defaultText = LocalizationSettings.StringDatabase.GetLocalizedString(table, defaultTextKey);
            defaultTextStr = defaultText;
        }

        DoShowColorInputBox(message, result, selctedColor);
    }

    public void ShowColorInputBox(string table, string key, Action<Color?> result, string defaultTextKey = "", string defaultDefault = "")
        => ShowColorInputBox(table, key, result, Color.red, defaultTextKey, defaultDefault);

    private void DoShowColorInputBox(string message, Action<Color?> result, Color defaultColor)
    {
        var dialogBox = CreateNewDialogBox().WithNoTitle();

        var title = dialogBox.AddComponent<TextComponent>().WithInitialValue(message);

        var colorPicker = dialogBox
            .AddComponent<ColorPickerComponent>()
            .WithInitialValue(defaultColor);

        var cancelButton = dialogBox
            .AddFooterButton(() => result?.Invoke(null),
                LocalizationSettings.StringDatabase.GetLocalizedString(nameof(PersistentUI), "cancel"));

        var submitButton = dialogBox
            .AddFooterButton(() => result?.Invoke(colorPicker.Value),
                LocalizationSettings.StringDatabase.GetLocalizedString(nameof(PersistentUI), "submit"));

        dialogBox.Open();
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
}
