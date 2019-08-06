using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PersistentUI : MonoBehaviour {

    public enum DisplayMessageType {
        BOTTOM,
        CENTER
    }

    private static PersistentUI _instance;
    public static PersistentUI Instance {
        get { return _instance; }
    }

    private void Awake() {
        if (_instance != null) {
            Destroy(this.gameObject);
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
    private Text loadingTip;
    [SerializeField]
    private Image editorLoadingBackground;
    [SerializeField]
    private ImageList editorImageList;

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

    [Header("Center Message")]

    [SerializeField]
    private MessageDisplayer centerDisplay;

    [SerializeField]
    private MessageDisplayer bottomDisplay;

    private void Start() {
        centerDisplay.host = this;
        bottomDisplay.host = this;
    }

    private void LateUpdate() {
        if (showTooltip) UpdateTooltipPosition();
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
    public void SetTooltip(string message, string advancedMessage = null) {
        currentTooltipMessage = message;
        if (advancedMessage != null && advancedMessage != "") currentTooltipAdvancedMessage = advancedMessage;
        else currentTooltipAdvancedMessage = null;
    }

    public void ShowTooltip() {
        showTooltip = true;
    }

    public void HideTooltip() {
        showTooltip = false;
        if (tooltipObject != null) tooltipObject.SetActive(false);
    }

    private void UpdateTooltipPosition() {
        if (Input.GetKey(KeyCode.LeftControl) && currentTooltipAdvancedMessage != null) tooltipText.text = currentTooltipAdvancedMessage;
        else tooltipText.text = currentTooltipMessage;
        if (!tooltipObject.activeSelf) tooltipObject.SetActive(true);
        tooltipObject.transform.position = Input.mousePosition + new Vector3(0, tooltipPanelRect.sizeDelta.y, 0);
    }
    #endregion



    /*
     * Notification messages
     */

    #region notifications

    [System.Serializable]
    public class MessageDisplayer {

        [SerializeField]
        TMP_Text messageText;

        public MonoBehaviour host;

        bool isShowingMessages = false;
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
