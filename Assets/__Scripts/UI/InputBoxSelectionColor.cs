using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputBoxSelectionColor : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Color normal;
    [SerializeField] private Color selected;

    public void OnDeselect(BaseEventData eventData)
    {
        gameObject.GetComponent<Image>().color = normal;
    }

    public void OnSelect(BaseEventData eventData)
    {
        gameObject.GetComponent<Image>().color = selected;
    }
}
