using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIMode : MonoBehaviour
{

    [SerializeField] private GameObject modesGameObject;
    [SerializeField] private RectTransform selected;
    [SerializeField] private SoftAttachToNoteGrid[] thingsToMove;
    [SerializeField] private Transform[] thingsToMoveTransform;
    [SerializeField] private CameraController _cameraController;
    
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
            case 0: //Normal
                foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(true, group);
                MoveThings(false);
                break;
            case 1: //Hide UI
                foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(false, group);
                MoveThings(false);
                break;
            case 2: //Preview Mode
                foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(false, group);
                MoveThings(true);
                break;
            case 3: //Playing Mode
                foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(false, group);
                MoveThings(true);
                _cameraController.transform.position = new Vector3(0,1.8f,0);
                _cameraController.transform.rotation = Quaternion.Euler(Vector3.zero);
                _cameraController.SetLockState(true);
                break;
        }
    }
    
    private void MoveThings(bool up)
    {
        bool fixTheCam = _cameraController.LockedOntoNoteGrid; //If this is not used, then there is a chance the moved items may break.
        
        if(fixTheCam) _cameraController.LockedOntoNoteGrid = false;
        if (up)
        {
            foreach (SoftAttachToNoteGrid s in thingsToMove)
            {
                s.overridePos = true;
                Transform t = s.transform;
                Vector3 p = t.localPosition;
                p.y = 2000f;
                t.localPosition = p;
            }
            foreach (Transform s in thingsToMoveTransform)
            {
                Transform t = s.transform;
                Vector3 p = t.localPosition;
                p.y = 2000f;
                t.localPosition = p;
            }
        }
        else
        {
            foreach (SoftAttachToNoteGrid s in thingsToMove)
            {
                Transform t = s.transform;
                Vector3 p = t.localPosition;
                switch (t.name)
                {
                    case "Event Type Labels":
                        p.y = 0.1f;
                        break;
                    case "Note Interface Scaling Offset":
                        p.y = -0.05f;
                        break;
                    default:
                        p.y = 0f;
                        break;
                }
                t.localPosition = p;
                s.overridePos = false;
            }
            foreach (Transform s in thingsToMoveTransform)
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
        if(fixTheCam) _cameraController.LockedOntoNoteGrid = true;
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
