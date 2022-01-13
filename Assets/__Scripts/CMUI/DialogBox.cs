using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogBox : MonoBehaviour
{
    private readonly List<DialogBoxComponent> components = new List<DialogBoxComponent>();

    public DialogBox Add<T>(Action<T> builderOptions) where T : DialogBoxComponent
    {
        var component = gameObject.AddComponent<T>();

        component.AssignDialogBox(this);

        builderOptions?.Invoke(component);

        components.Add(component);

        return this;
    }

    public void Show()
    {

    }

    public void Close()
    {

    }
}
