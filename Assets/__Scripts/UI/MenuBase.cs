using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public abstract class MenuBase : MonoBehaviour, CMInput.IMenusExtendedActions
{
    public abstract void OnLeaveMenu(CallbackContext context);

    public virtual void OnTab(CallbackContext context)
    {
        if (!context.performed || this == null) return;

        var system = EventSystem.current;
        try
        {
            var selected = system.currentSelectedGameObject.GetComponent<Selectable>();

            var next = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                ? selected.FindSelectableOnUp()
                : selected.FindSelectableOnDown();

            if (next != null)
            {
                var inputfield = next.GetComponent<TMP_InputField>();
                if (inputfield != null)
                    inputfield.MoveToEndOfLine(false, false); //if it's an input field, also set the text caret

                system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
            }
        }
        catch (Exception)
        {
            // If there's an error select the default selectable
            system.SetSelectedGameObject(GetDefault(), new BaseEventData(system));
        }
    }

    protected abstract GameObject GetDefault();
}
