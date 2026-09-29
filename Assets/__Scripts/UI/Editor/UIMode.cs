using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// TODO rewrite
public class UIMode : MonoBehaviour, CMInput.IUIModeActions
{
    public static UIModeType SelectedMode;
    public static bool PreviewMode { get; private set; }
    public static bool AnimationMode { get; private set; }
    private Vector3 savedCamPosition = Vector3.zero;
    private Quaternion savedCamRotation = Quaternion.identity;

    public static event Action<UIModeType> OnUIModeSwitched;
    public static event Action OnPreviewModeSwitched;

    [SerializeField] private GameObject modesGameObject;
    [SerializeField] private RectTransform selected;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private GameObject[] gameObjectsWithRenderersToToggle;
    [SerializeField] private Transform[] thingsThatRequireAMoveForPreview;
    [SerializeField] private AudioTimeSyncController atsc;

    private readonly List<TextMeshProUGUI> modes = new();
    private readonly List<Renderer> renderers = new();
    private readonly List<Canvas> canvases = new();
    private CanvasGroup canvasGroup;

    private static readonly List<Action<object>> actions = new();


    private MapEditorUI mapEditorUi;
    private Coroutine showUI;
    private Coroutine slideSelectionCoroutine;

    private void Awake()
    {
        mapEditorUi = transform.GetComponentInParent<MapEditorUI>();
        modes.AddRange(modesGameObject.transform.GetComponentsInChildren<TextMeshProUGUI>());
        canvasGroup = GetComponent<CanvasGroup>();
        OnUIModeSwitched = null;
        OnPreviewModeSwitched = null;
        SelectedMode = UIModeType.Normal;
        savedCamPosition = Settings.Instance.SavedPositions[0]?.Position ?? savedCamPosition;
        savedCamRotation = Settings.Instance.SavedPositions[0]?.Rotation ?? savedCamRotation;
    }

    private void Start()
    {
        foreach (var go in gameObjectsWithRenderersToToggle)
        {
            renderers.AddRange(go.GetComponentsInChildren<Renderer>());
            canvases.AddRange(go.GetComponentsInChildren<Canvas>());
        }

        atsc.OnPlayToggled += OnPlayToggle;
        Shader.DisableKeyword("CM_PREVIEW_MODE");
    }

    private void OnDestroy()
    {
        SelectedMode = UIModeType.Normal;
        PreviewMode = false;
        AnimationMode = false;
        OnUIModeSwitched = null;
        OnPreviewModeSwitched = null;
        Shader.DisableKeyword("CM_PREVIEW_MODE");
        atsc.OnPlayToggled -= OnPlayToggle;
    }

    public void OnToggleUIMode(InputAction.CallbackContext context)
    {
        if (context.performed) ToggleUIMode(true);
    }

    public void OnToggleUIModeReverse(InputAction.CallbackContext context)
    {
        if (context.performed) ToggleUIMode(false);
    }

    void CMInput.IUIModeActions.OnToggleUIModeNormal(InputAction.CallbackContext context)
    {
        if (context.performed && SelectedMode != UIModeType.Normal)
        {
            UpdateCameraOnUIModeToggle(UIModeType.Normal);
            SetUIMode(UIModeType.Normal, true);
        }
    }

    void CMInput.IUIModeActions.OnToggleUIModeHideUI(InputAction.CallbackContext context)
    {
        if (context.performed && SelectedMode != UIModeType.HideUI)
        {
            UpdateCameraOnUIModeToggle(UIModeType.HideUI);
            SetUIMode(UIModeType.HideUI, true);
        }
    }

    void CMInput.IUIModeActions.OnToggleUIModeHideGrids(InputAction.CallbackContext context)
    {
        if (context.performed && SelectedMode != UIModeType.HideGrids)
        {
            UpdateCameraOnUIModeToggle(UIModeType.HideGrids);
            SetUIMode(UIModeType.HideGrids, true);
        }
    }

    void CMInput.IUIModeActions.OnToggleUIModePreview(InputAction.CallbackContext context)
    {
        if (context.performed && SelectedMode != UIModeType.Preview)
        {
            UpdateCameraOnUIModeToggle(UIModeType.Preview);
            SetUIMode(UIModeType.Preview, true);
        }
    }

    void CMInput.IUIModeActions.OnToggleUIModePlaying(InputAction.CallbackContext context)
    {
        if (context.performed && SelectedMode != UIModeType.Playing)
        {
            UpdateCameraOnUIModeToggle(UIModeType.Playing);
            SetUIMode(UIModeType.Playing, true);
        }
    }

    private void ToggleUIMode(bool forward)
    {
        var currentOption = selected.parent.GetSiblingIndex();
        var nextOption = currentOption + (forward ? 1 : -1);

        if (nextOption < 0) nextOption = modes.Count - 1;

        if (nextOption >= modes.Count) nextOption = 0;

        UpdateCameraOnUIModeToggle((UIModeType)nextOption);

        SetUIMode(nextOption);
    }

    private void UpdateCameraOnUIModeToggle(UIModeType mode)
    {
        var currentOption = selected.parent.GetSiblingIndex();

        if (currentOption == (int)UIModeType.Playing && ((int)mode) != currentOption)
        {
            // restore cam position/rotation
            cameraManager.SelectCamera(CameraType.Editing);
            cameraManager.SelectedCameraController.transform.SetPositionAndRotation(
                savedCamPosition,
                savedCamRotation);
        }
        else if (mode == UIModeType.Playing)
        {
            // save cam position/rotation
            var cameraTransform = cameraManager.SelectedCameraController.transform;
            savedCamPosition = cameraTransform.position;
            savedCamRotation = cameraTransform.rotation;
            cameraManager.SelectCamera(CameraType.Playing);
        }
    }

    private void OnPlayToggle(bool playing)
    {
        if (PreviewMode)
        {
            foreach (var group in mapEditorUi.MainUIGroup)
                if (group.name == "Song Timeline")
                    mapEditorUi.ToggleUIVisible(!playing, group);
        }

        if (SelectedMode == UIModeType.Playing) cameraManager.SelectedCameraController.SetLockState(playing);
    }

    public void SetUIMode(UIModeType mode, bool showUIChange = true) => SetUIMode((int)mode, showUIChange);

    public void SetUIMode(int modeID, bool showUIChange = true)
    {
        var previousPreviewMode = PreviewMode;

        SelectedMode = (UIModeType)modeID;
        PreviewMode = SelectedMode is UIModeType.Playing or UIModeType.Preview;
        AnimationMode = PreviewMode && Settings.Instance.Animations;

        if (previousPreviewMode != PreviewMode) OnPreviewModeSwitched?.Invoke();

        OnUIModeSwitched?.Invoke(SelectedMode);

        selected.SetParent(modes[modeID].transform, true);
        slideSelectionCoroutine = StartCoroutine(SlideSelection());
        if (showUIChange) showUI = StartCoroutine(ShowUI());

        switch (SelectedMode)
        {
            case UIModeType.Normal:
                HideStuff(true, true, true, true, true);
                break;
            case UIModeType.HideUI:
                HideStuff(false, true, true, true, true);
                break;
            case UIModeType.HideGrids:
                HideStuff(false, false, true, true, true);
                break;
            case UIModeType.Preview:
            case UIModeType.Playing:
                HideStuff(false, false, false, false, false);
                break;
        }

        foreach (var boy in actions) boy?.Invoke(SelectedMode);
    }

    private void HideStuff(bool showUI, bool showExtras, bool showMainGrid, bool showCanvases, bool showPlacement)
    {
        foreach (var group in mapEditorUi.MainUIGroup) mapEditorUi.ToggleUIVisible(showUI, group);
        foreach (var r in renderers) r.enabled = showExtras;
        foreach (var c in canvases) c.enabled = showCanvases;
        foreach (var t in thingsThatRequireAMoveForPreview) t.gameObject.SetActive(showPlacement);

        if (PreviewMode)
            Shader.EnableKeyword("CM_PREVIEW_MODE");
        else
            Shader.DisableKeyword("CM_PREVIEW_MODE");

        //foreach (Renderer r in _verticalGridRenderers) r.enabled = showMainGrid;
        atsc.RefreshGridSnapping();
    }

    private IEnumerator ShowUI()
    {
        if (showUI != null) StopCoroutine(showUI);

        const float transitionTime = 0.2f;
        const float delayTime = 1f;

        var startTime = Time.time;
        var startAlpha = canvasGroup.alpha;
        while (canvasGroup.alpha != 1f)
        {
            var t = Mathf.Clamp01((Time.time - startTime) / transitionTime);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1, t);
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(delayTime);

        startTime = Time.time;
        startAlpha = canvasGroup.alpha;
        while (canvasGroup.alpha != 0)
        {
            var t = Mathf.Clamp01((Time.time - startTime) / transitionTime);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SlideSelection()
    {
        if (slideSelectionCoroutine != null) StopCoroutine(slideSelectionCoroutine);

        const float transitionTime = 0.5f;

        var startTime = Time.time;
        var startLocalPosition = selected.localPosition;
        while (selected.localPosition.x != 0)
        {
            var x = Mathf.Clamp01((Time.time - startTime) / transitionTime);
            var t = 1 - Mathf.Pow(1 - x, 3); // cubic interpolation because linear looks bad
            selected.localPosition = Vector3.Lerp(startLocalPosition, Vector3.zero, t);
            yield return new WaitForFixedUpdate();
        }
    }

    /// <summary>
    /// Attach an <see cref="Action"/> that will be triggered when the UI mode has been changed.
    /// </summary>
    public static void NotifyOnUIModeChange(Action<object> callback)
    {
        if (callback != null) actions.Add(callback);
    }

    /// <summary>
    /// Clear all <see cref="Action"/>s associated with a UI mode change
    /// </summary>
    public static void ClearUIModeNotifications() => actions.Clear();
}


/// <inheritdoc />
public enum UIModeType
{
    Normal = 0,
    HideUI = 1,
    HideGrids = 2,
    Preview = 3,
    Playing = 4
}
