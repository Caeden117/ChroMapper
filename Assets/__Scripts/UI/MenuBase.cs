using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public abstract class MenuBase : MonoBehaviour, CMInput.IMenusExtendedActions
{
    protected abstract GameObject GetDefault();

    public abstract void OnLeaveMenu(CallbackContext context);

    public virtual void OnTab(CallbackContext context)
    {
        if (!context.performed || this == null) return;

        var system = EventSystem.current;
        try
        {
            Selectable selected = system.currentSelectedGameObject.GetComponent<Selectable>();

            Selectable next;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                next = selected.FindSelectableOnUp();
            }
            else
            {
                next = selected.FindSelectableOnDown();
            }

            if (next != null)
            {
                TMP_InputField inputfield = next.GetComponent<TMP_InputField>();
                if (inputfield != null)
                    inputfield.MoveToEndOfLine(false, false);  //if it's an input field, also set the text caret

                system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
            }
        }
        catch (Exception)
        {
            // If there's an error select the default selectable
            system.SetSelectedGameObject(GetDefault(), new BaseEventData(system));
        }
    }
}