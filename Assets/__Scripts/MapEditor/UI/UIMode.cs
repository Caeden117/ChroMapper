using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIMode : MonoBehaviour
{

    [SerializeField] private GameObject modesGameObject;
    [SerializeField] private RectTransform selected;
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private GameObject[] gameObjectsWithRenderersToToggle;
    [SerializeField] private SoftAttachToNoteGrid[] thingsThatRequireAMoveForPreviewSoftAttachToNoteGrid;
    [SerializeField] private Transform[] thingsThatRequireAMoveForPreview;
    [SerializeField] private RotationCallbackController _rotationCallbackController;

    private List<Renderer> _renderers = new List<Renderer>();
    private List<Canvas> _canvases = new List<Canvas>();

    private MapEditorUI _mapEditorUi;
    private CanvasGroup _canvasGroup;

    private List<TextMeshProUGUI> _modes = new List<TextMeshProUGUI>();
    private Coroutine _slideSelectionCoroutine;
    private Coroutine _showUI;

    [HideInInspector] public UIModeType selectedMode;
    
    private void Awake()
    {
        _mapEditorUi = transform.GetComponentInParent<MapEditorUI>();
        _modes.AddRange(modesGameObject.transform.GetComponentsInChildren<TextMeshProUGUI>());
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        foreach (GameObject go in gameObjectsWithRenderersToToggle)
        {
            Renderer[] r = go.GetComponentsInChildren<Renderer>();
            if(r.Length != 0) _renderers.AddRange(r);
            else _canvases.AddRange(go.GetComponentsInChildren<Canvas>());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl))
        {
            int selectedOption;
            bool shiftKey = Input.GetKey(KeyCode.LeftShift);
            if (shiftKey) selectedOption = selected.parent.GetSiblingIndex() - 1;
            else selectedOption = selected.parent.GetSiblingIndex() + 1;

            bool shouldIWorry = OptionsController.Find<NoteLanesController>()?.NoteLanes != 4f|| _rotationCallbackController.IsActive;
            
            if (selectedOption == (int) UIModeType.PLAYING && shouldIWorry) selectedOption++;
            
            if (selectedOption < 0)
            {
                selectedOption = _modes.Count - 1;
                if (shouldIWorry) selectedOption--;
            }
            
            if (selectedOption >= _modes.Count) selectedOption = (int) UIModeType.NORMAL;

            SetUIMode(selectedOption);
        }
    }

    public void SetUIMode(UIModeType mode, bool showUIChange = true)
    {
        SetUIMode((int) mode, showUIChange);
    }

    public void SetUIMode(int modeID, bool showUIChange = true)
    {
        selectedMode = (UIModeType) modeID;
        selected.SetParent(_modes[modeID].transform, true);
        _slideSelectionCoroutine = StartCoroutine(SlideSelection());
        if(showUIChange) _showUI = StartCoroutine(ShowUI());
        
        switch (selectedMode)
        {
            case UIModeType.NORMAL:
                HideStuff(true, true, true, true, true);
                break;
            case UIModeType.HIDE_UI:
                HideStuff(false, true, true, true, true);
                break;
            case UIModeType.HIDE_GRIDS:
                HideStuff(false, false, true, true, true);
                break;
            case UIModeType.PREVIEW:
                HideStuff(false, false,false,  false, false);
                break;
            case UIModeType.PLAYING:
                HideStuff(false, false, false, false, false);
                _cameraController.transform.position = new Vector3(0,1.8f,0);
                _cameraController.transform.rotation = Quaternion.Euler(Vector3.zero);
                _cameraController.SetLockState(true);
                break;
        }
    }

    private void HideStuff(bool showUI, bool showExtras, bool showMainGrid, bool showCanvases, bool showPlacement)
    {
        foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(showUI, group);
        foreach (Renderer r in _renderers) r.enabled = showExtras;
        foreach (Canvas c in _canvases) c.enabled = showCanvases;

        bool fixTheCam = _cameraController.LockedOntoNoteGrid; //If this is not used, then there is a chance the moved items may break.
        if (fixTheCam) _cameraController.LockedOntoNoteGrid = false;

        if (showPlacement)
        {
            foreach (SoftAttachToNoteGrid s in thingsThatRequireAMoveForPreviewSoftAttachToNoteGrid)
            {
                Transform t = s.transform;
                Vector3 p = t.localPosition;
                switch (t.name)
                {
                    case "Event Type Labels":
                        p.y = 0.1f;
                        break;
                    default:
                        p.y = 0f;
                        break;
                }
                t.localPosition = p;
                s.overridePos = false;
            }

            foreach (Transform s in thingsThatRequireAMoveForPreview)
            {
                Transform t = s.transform;
                Vector3 p = t.localPosition;
                switch (t.name)
                {
                    case "Note Interface Scaling Offset":
                        p.y = -0.05f;
                        break;
                    default:
                        p.y = 0f;
                        break;
                }

                t.localPosition = p;
            }
        }
        else
        {
            foreach (SoftAttachToNoteGrid s in thingsThatRequireAMoveForPreviewSoftAttachToNoteGrid)
            {
                s.overridePos = true;
                Transform t = s.transform;
                Vector3 p = t.localPosition;
                p.y = 2000f;
                t.localPosition = p;
            }

            foreach (Transform s in thingsThatRequireAMoveForPreview)
            {
                Transform t = s.transform;
                Vector3 p = t.localPosition;
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

        if (fixTheCam) _cameraController.LockedOntoNoteGrid = true;
        //foreach (Renderer r in _verticalGridRenderers) r.enabled = showMainGrid;
        //todo Move events grid and Note placement grid UP to stop clicks
    }

    private IEnumerator ShowUI()
    {
        if(_showUI != null) StopCoroutine(_showUI);
        
        float startTime = Time.time;
        while (true)
        {
            if (_canvasGroup.alpha >= 0.98f)
            {
                _canvasGroup.alpha = 1f;
                break;
            }
            
            float alpha = _canvasGroup.alpha;
            alpha = Mathf.Lerp(alpha,1, (Time.time / startTime) * 0.1f);
            _canvasGroup.alpha = alpha;
            yield return new WaitForFixedUpdate();
        }
        
        yield return new WaitForSeconds(3);
        
        while (true)
        {
            if (_canvasGroup.alpha <= 0.05f)
            {
                _canvasGroup.alpha = 0f;
                break;
            }
            
            float alpha = _canvasGroup.alpha;
            alpha = Mathf.Lerp(alpha,0, (Time.time / startTime) * 0.1f);
            _canvasGroup.alpha = alpha;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SlideSelection()
    {
        if(_slideSelectionCoroutine != null) StopCoroutine(_slideSelectionCoroutine);
        
        float startTime = Time.time;
        
        while (true)
        {
            Vector3 localPosition = selected.localPosition;
            localPosition = Vector3.Lerp(localPosition,Vector3.zero, (Time.time / startTime) * 0.15f);
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
}

/// <inheritdoc />
public enum UIModeType
{
    NORMAL = 0,
    HIDE_UI = 1,
    HIDE_GRIDS = 2,
    PREVIEW = 3,
    PLAYING = 4,
    
}
