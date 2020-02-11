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
    private MapEditorUI _mapEditorUi;
    private CanvasGroup _canvasGroup;

    private List<TextMeshProUGUI> _modes = new List<TextMeshProUGUI>();
    private Coroutine _slideSelectionCoroutine;
    private Coroutine _showUI;

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
            int selectedOption;
            if (Input.GetKey(KeyCode.LeftShift)) selectedOption = selected.parent.GetSiblingIndex() - 1;
            else selectedOption = selected.parent.GetSiblingIndex() + 1;
            
            //if (mainUIGroup.First().alpha == 1) PersistentUI.Instance.DisplayMessage("CTRL+H to toggle UI", PersistentUI.DisplayMessageType.BOTTOM);
            
            if (selectedOption < 0) selectedOption = _modes.Count - 1;
            if (selectedOption >= _modes.Count) selectedOption = 0;
            
            selected.SetParent(_modes[selectedOption].transform, true);
            
            _slideSelectionCoroutine = StartCoroutine(SlideSelection());
            _showUI = StartCoroutine(ShowUI());

            switch (selectedOption)
            {
                case 0:
                    foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(true, group);
                    moveThings(false);
                    break;
                case 1:
                    foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(false, group);
                    moveThings(false);
                    break;
                case 2:
                    foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(false, group);
                    moveThings(true);
                    break;
                case 3:
                    foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(false, group);
                    moveThings(true);
                    break;
            }
            
            //foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(group.alpha != 1, group);
        }
    }

    private void moveThings(bool up)
    {
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
            if (selected.localPosition == Vector3.zero) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
