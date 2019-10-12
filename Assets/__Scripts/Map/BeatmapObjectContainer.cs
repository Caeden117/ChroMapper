using System;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour {

    public static readonly int BeatmapObjectLayer = 9;
    public static readonly int BeatmapObjectSelectedLayer = 10;

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

    [SerializeField] private BoxCollider boxCollider;

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    private void FixedUpdate()
    {
        if (boxCollider == null) return;
        boxCollider.enabled = KeybindsController.ShiftHeld || Input.GetMouseButton(2)
            || KeybindsController.AltHeld || KeybindsController.CtrlHeld;
    }

    internal virtual void OnMouseOver()
    {
        if (KeybindsController.CtrlHeld && Input.GetMouseButton(0))
            if (!SelectionController.IsObjectSelected(this)) SelectionController.Select(this, true);
        if (!KeybindsController.ShiftHeld) return;
        if (Input.GetMouseButtonDown(0))
        { //Selects if it's not already selected, deselect if it is.
            if (SelectionController.IsObjectSelected(this)) SelectionController.Deselect(this);
            else SelectionController.Select(this, true);
        }
        else if (Input.GetMouseButtonDown(2)) FlaggedForDeletionEvent?.Invoke(this);
    }

    internal virtual void SafeSetActive(bool active)
    {
        if (active != gameObject.activeSelf) gameObject.SetActive(active);
    }
}
