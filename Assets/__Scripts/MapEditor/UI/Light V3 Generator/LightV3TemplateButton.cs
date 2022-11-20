using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LightV3TemplateButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private LightV3Buttons v3Buttons;

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                v3Buttons.RestoreTemplate(GetComponent<Button>());
                break;
            case PointerEventData.InputButton.Right:
                v3Buttons.RenameTemplate(GetComponent<Button>());
                break;
            case PointerEventData.InputButton.Middle:
                v3Buttons.DeleteTemplate(GetComponent<Button>());
                break;
        }
    }

    public void SetText(string text)
    {
        label.text = text;
    }
}
