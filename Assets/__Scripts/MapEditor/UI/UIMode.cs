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
    }

    private void Start()
    {
        foreach (var go in gameObjectsWithRenderersToToggle)
        {
            var r = go.GetComponentsInChildren<Renderer>();
            if (r.Length != 0) renderers.AddRange(r);
            else canvases.AddRange(go.GetComponentsInChildren<Canvas>());
        }
    }

    public void OnToggleUIMode(InputAction.CallbackContext context)
    {
        if (context.performed && !BPMTapperController.IsActive)
        {
            var selectedOption = selected.parent.GetSiblingIndex() + 1;

            var shouldIWorry = rotationCallbackController.IsActive;

            if (selectedOption == (int)UIModeType.Playing && shouldIWorry) selectedOption++;

            if (selectedOption < 0)
            {
                selectedOption = modes.Count - 1;
                if (shouldIWorry) selectedOption--;
            }

            if (selectedOption >= modes.Count) selectedOption = (int)UIModeType.Normal;

            SetUIMode(selectedOption);
        }
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
                HideStuff(false, false, false, false, false);
                break;
            case UIModeType.Playing:
                HideStuff(false, false, false, false, false);
                cameraController.transform.position = new Vector3(0, 1.8f, 0);
                cameraController.transform.rotation = Quaternion.Euler(Vector3.zero);
                cameraController.SetLockState(true);
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

        var startTime = Time.time;
        while (true)
        {
            if (canvasGroup.alpha >= 0.98f)
            {
                canvasGroup.alpha = 1f;
                break;
            }

            var alpha = canvasGroup.alpha;
            alpha = Mathf.Lerp(alpha, 1, Time.time / startTime * 0.1f);
            canvasGroup.alpha = alpha;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(3);

        while (true)
        {
            if (canvasGroup.alpha <= 0.05f)
            {
                canvasGroup.alpha = 0f;
                break;
            }

            var alpha = canvasGroup.alpha;
            alpha = Mathf.Lerp(alpha, 0, Time.time / startTime * 0.1f);
            canvasGroup.alpha = alpha;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SlideSelection()
    {
        if (slideSelectionCoroutine != null) StopCoroutine(slideSelectionCoroutine);

        var startTime = Time.time;

        while (true)
        {
            var localPosition = selected.localPosition;
            localPosition = Vector3.Lerp(localPosition, Vector3.zero, Time.time / startTime * 0.15f);
            selected.localPosition = localPosition;
            if (Math.Abs(selected.localPosition.x) < 0.001f)
            {
                localPosition.x = 0;
                selected.localPosition = localPosition;
                break;
            }

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
