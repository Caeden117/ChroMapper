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
    [SerializeField] private GameObject verticalGrid;

    private List<Renderer> _verticalGridRenderers = new List<Renderer>();
    private List<Renderer> _renderers = new List<Renderer>();
    private List<Canvas> _canvases = new List<Canvas>();

    private MapEditorUI _mapEditorUi;
    private CanvasGroup _canvasGroup;

    private List<TextMeshProUGUI> _modes = new List<TextMeshProUGUI>();
    private Coroutine _slideSelectionCoroutine;
    private Coroutine _showUI;

    [HideInInspector] public int selectedOption;
    
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
        _verticalGridRenderers.AddRange(verticalGrid.GetComponentsInChildren<Renderer>());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl))
        {
            bool shiftKey = Input.GetKey(KeyCode.LeftShift);
            if (shiftKey) selectedOption = selected.parent.GetSiblingIndex() - 1;
            else selectedOption = selected.parent.GetSiblingIndex() + 1; 
            
            bool shouldIWorry = OptionsController.Find<NoteLanesController>()?.NoteLanes != 4f;
            
            if (selectedOption == 3 && shouldIWorry) selectedOption++;
            
            if (selectedOption < 0)
            {
                selectedOption = _modes.Count - 1;
                if (shouldIWorry) selectedOption--;
            }
            
            if (selectedOption >= _modes.Count) selectedOption = 0;

            SetUIMode(selectedOption);
        }
    }

    public void SetUIMode(int modeID)
    {
        selectedOption = modeID;
        selected.SetParent(_modes[modeID].transform, true);
        _slideSelectionCoroutine = StartCoroutine(SlideSelection());
        _showUI = StartCoroutine(ShowUI());
        
        switch (modeID)
        {
            case (int) UIModeType.NORMAL:
                HideStuff(true, true, true, true, true);
                break;
            case (int) UIModeType.HIDE_UI:
                HideStuff(false, true, true, true, true);
                break;
            case (int) UIModeType.HIDE_GRIDS:
                HideStuff(false, false, true, true, true);
                break;
            case (int) UIModeType.PREVIEW:
                HideStuff(false, false,false,  false, false);
                break;
            case (int) UIModeType.PLAYING:
                HideStuff(false, false, false, false, false);
                _cameraController.transform.position = new Vector3(0,1.8f,0); //todo test with 360 maps
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
        foreach (Renderer r in _verticalGridRenderers) r.enabled = showMainGrid;

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
