using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour {

    [SerializeField]
    public abstract BeatmapObject objectData { get; }

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    private void OnMouseDown()
    {
        if (!KeybindsController.ShiftHeld) return;
        if (SelectionController.IsObjectSelected(this)) //Shift Right-Click on a selected object will deselect.
            SelectionController.Deselect(this);
        else //Else it will try to select again.
            SelectionController.Select(this, true);
    }

    public abstract void UpdateGridPosition();

}
