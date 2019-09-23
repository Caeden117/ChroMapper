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

    public bool PreviousActiveState { get; private set; } = true;

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    internal virtual void OnMouseOver()
    {
        if (!KeybindsController.ShiftHeld) return;
        if (Input.GetMouseButtonDown(0))
        { //Selects if it's not already selected, deselect if it is.
            if (SelectionController.IsObjectSelected(this)) SelectionController.Deselect(this);
            else SelectionController.Select(this, true);
        }
        else if (Input.GetMouseButtonDown(2)) FlaggedForDeletionEvent?.Invoke(this);
    }

    private void OnMouseEnter()
    {
        bool massSelect = KeybindsController.CtrlHeld && Input.GetMouseButton(0);
        if (massSelect) //Selects if its not already selected
            if (!SelectionController.IsObjectSelected(this)) SelectionController.Select(this, true);
    }

    public void SafeSetActive(bool active)
    {
        if (active != PreviousActiveState)
        {
            PreviousActiveState = active;
            gameObject.SetActive(active);
        }
    }
}
