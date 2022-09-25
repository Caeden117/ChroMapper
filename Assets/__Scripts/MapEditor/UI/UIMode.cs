using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// TODO rewrite
public class UIMode : MonoBehaviour, CMInput.IUIModeActions
{
    public static UIModeType SelectedMode;
    private Vector3 savedCamPosition = Vector3.zero;
    private Quaternion savedCamRotation = Quaternion.identity;

    public static Action<UIModeType> UIModeSwitched;

    [SerializeField] private GameObject modesGameObject;
    [SerializeField] private RectTransform selected;
    [FormerlySerializedAs("_cameraController")] [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject[] gameObjectsWithRenderersToToggle;
    [SerializeField] private Transform[] thingsThatRequireAMoveForPreview;
    [FormerlySerializedAs("_rotationCallbackController")] [SerializeField] private RotationCallbackController rotationCallbackController;
    [SerializeField] private AudioTimeSyncController atsc;

    public string Keybind = "CTRL+H";

    private readonly List<TextMeshProUGUI> modes = new List<TextMeshProUGUI>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Canvas> canvases = new List<Canvas>();
    private CanvasGroup canvasGroup;

    private static readonly List<Action<object>> actions = new List<Action<object>>();


    private MapEditorUI mapEditorUi;
    private Coroutine showUI;
    private Coroutine slideSelectionCoroutine;

    private void Awake()
    {
        mapEditorUi = transform.GetComponentInParent<MapEditorUI>();
        modes.AddRange(modesGameObject.transform.GetComponentsInChildren<TextMeshProUGUI>());
        canvasGroup = GetComponent<CanvasGroup>();
        UIModeSwitched = null;
        SelectedMode = UIModeType.Normal;
        savedCamPosition = Settings.Instance.SavedPositions[0]?.Position ?? savedCamPosition;
        savedCamRotation = Settings.Instance.SavedPositions[0]?.Rotation ?? savedCamRotation;
    }

    private void Start()
    {
        foreach (var go in gameObjectsWithRenderersToToggle)
        {
            var r = go.GetComponentsInChildren<Renderer>();
            if (r.Length != 0) renderers.AddRange(r);
            else canvases.AddRange(go.GetComponentsInChildren<Canvas>());
        }

        atsc.PlayToggle += OnPlayToggle;
        Shader.SetGlobalFloat("_EnableNoteSurfaceGridLine", 1f);
    }

    public void OnToggleUIMode(InputAction.CallbackContext context)
    {
        if (context.performed && !BPMTapperController.IsActive)
        {
            var currentOption = selected.parent.GetSiblingIndex();
            var nextOption = currentOption + 1;

            var disablePlayingMode = rotationCallbackController.IsActive;

            if (nextOption == (int)UIModeType.Playing && disablePlayingMode) nextOption++;

            if (nextOption < 0)
            {
                nextOption = modes.Count - 1;
                if (disablePlayingMode) nextOption--;
            }

            if (nextOption >= modes.Count) nextOption = 0;

            if (currentOption == (int)UIModeType.Playing && nextOption != currentOption)
            {
                // restore cam position/rotation
                cameraController.transform.SetPositionAndRotation(savedCamPosition, savedCamRotation);
            }
            else if (nextOption == (int)UIModeType.Playing)
            {
                // save cam position/rotation
                savedCamPosition = cameraController.transform.position;
                savedCamRotation = cameraController.transform.rotation;
            }

            SetUIMode(nextOption);
        }
    }

    private void OnPlayToggle(bool playing)
    {
        if (SelectedMode == UIModeType.Playing || SelectedMode == UIModeType.Preview)
        {
            foreach (var group in mapEditorUi.MainUIGroup)
            {
                if (group.name == "Song Timeline")
                {
                    mapEditorUi.ToggleUIVisible(!playing, group);
                }
            }
        }

        if (SelectedMode == UIModeType.Playing) cameraController.SetLockState(playing);
    }

    public void SetUIMode(UIModeType mode, bool showUIChange = true) => SetUIMode((int)mode, showUIChange);

    public void SetUIMode(int modeID, bool showUIChange = true)
    {
        SelectedMode = (UIModeType)modeID;
        UIModeSwitched?.Invoke(SelectedMode);
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
                OnPlayToggle(atsc.IsPlaying); // kinda jank but it works
                break;
        }

        foreach (var boy in actions) boy?.Invoke(SelectedMode);
    }

    private void HideStuff(bool showUI, bool showExtras, bool showMainGrid, bool showCanvases, bool showPlacement)
    {
        foreach (var group in mapEditorUi.MainUIGroup) mapEditorUi.ToggleUIVisible(showUI, group);
        foreach (var r in renderers) r.enabled = showExtras;
        foreach (var c in canvases) c.enabled = showCanvases;

        var fixTheCam =
            cameraController
                .LockedOntoNoteGrid; //If this is not used, then there is a chance the moved items may break.
        if (fixTheCam) cameraController.LockedOntoNoteGrid = false;

        if (showPlacement)
        {
            Shader.SetGlobalFloat("_EnableNoteSurfaceGridLine", 1f);
            foreach (var s in thingsThatRequireAMoveForPreview)
            {
                var t = s.transform;
                var p = t.localPosition;

                p.y = t.name switch
                {
                    "Rotating" => 0.05f,
                    _ => 0f,
                };
                t.localPosition = p;
            }
        }
        else
        {
            Shader.SetGlobalFloat("_EnableNoteSurfaceGridLine", 0f);
            foreach (var s in thingsThatRequireAMoveForPreview)
            {
                var t = s.transform;
                var p = t.localPosition;
                switch (s.name)
                {
                    case "Note Interface Scaling Offset":
                        if (showMainGrid) break;
                        p.y = 2000f;
                        break;
                    default:
                        p.y = 2000f;
                        break;
                }

                t.localPosition = p;
            }
        }

        if (fixTheCam) cameraController.LockedOntoNoteGrid = true;
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
        if (callback != null)
        {
            actions.Add(callback);
        }
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
