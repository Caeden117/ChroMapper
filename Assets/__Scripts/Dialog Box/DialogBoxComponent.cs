using System;
using UnityEngine;

public abstract class DialogBoxComponent : MonoBehaviour
{
    public DialogBox DialogBox { get; private set; }

    internal void AssignDialogBox(DialogBox parent)
    {
        if (DialogBox != null)
        {
            throw new InvalidOperationException("Parent has already been assigned.");
        }

        DialogBox = parent;
    }

}
