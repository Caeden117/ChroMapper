using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIMode : MonoBehaviour
{

    [SerializeField] private GameObject modesGameObject;
    [SerializeField] private RectTransform selected;
    private MapEditorUI _mapEditorUi;
    
    
    private List<TextMeshProUGUI> _modes;

    private void Awake()
    {
        _mapEditorUi = transform.GetComponentInParent<MapEditorUI>();
        _modes.AddRange(modesGameObject.transform.GetComponentsInChildren<TextMeshProUGUI>());
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl)) {
            //if (mainUIGroup.First().alpha == 1) PersistentUI.Instance.DisplayMessage("CTRL+H to toggle UI", PersistentUI.DisplayMessageType.BOTTOM);

            selected.SetParent(_modes[selected.parent.GetSiblingIndex() + 1].transform, false); //this returns null?
            
            //Possibly make worldPositionStays to true and then lerp the x pos to 0; That could be nice!

            //foreach (CanvasGroup group in _mapEditorUi.mainUIGroup) _mapEditorUi.ToggleUIVisible(group.alpha != 1, group);
        }
    }
}
