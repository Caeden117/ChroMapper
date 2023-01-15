using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CustomColorButton : MonoBehaviour, IPointerClickHandler
{
    public Image image;

    public event Action onRightClick;
    public event Action onMiddleClick;

    public void OnPointerClick(PointerEventData data)
    {
        switch (data.button)
        {
            case PointerEventData.InputButton.Middle:
                onMiddleClick?.Invoke();
                break;
            case PointerEventData.InputButton.Right:
                onRightClick?.Invoke();
                break;
        }
    }
}