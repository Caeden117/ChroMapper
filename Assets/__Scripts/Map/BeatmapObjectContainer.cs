using System;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour {

    public static Action<BeatmapObjectContainer> FlaggedForDeletionEvent;

    [SerializeField]
    public abstract BeatmapObject objectData { get; }
    
    public abstract void UpdateGridPosition();

    public int ChunkID
    {
        get {
            return (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
                MidpointRounding.AwayFromZero);
        }
    }

    public bool PreviousActiveState = true;

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    private void OnMouseOver()
    {
        if (!KeybindsController.ShiftHeld) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (SelectionController.IsObjectSelected(this)) //Shift Right-Click on a selected object will deselect.
                SelectionController.Deselect(this);
            else //Else it will try to select again.
                SelectionController.Select(this, true);
        }
        else if (Input.GetMouseButtonDown(2)) FlaggedForDeletionEvent?.Invoke(this);
    }
}
