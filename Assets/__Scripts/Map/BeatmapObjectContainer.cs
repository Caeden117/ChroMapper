using System;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour {

    public static readonly int BeatmapObjectLayer = 9;
    public static readonly int BeatmapObjectSelectedLayer = 10;

    public static Action<BeatmapObjectContainer> FlaggedForDeletionEvent;

    [SerializeField]
    public abstract BeatmapObject objectData { get; }

    private MeshCollider meshCollider;

    public abstract void UpdateGridPosition();

    public int ChunkID
    {
        get {
            return (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
                MidpointRounding.AwayFromZero);
        }
    }

    public bool PreviousActiveState { get; private set; } = true;

    private void Start()
    {
        meshCollider = GetComponentInChildren<MeshCollider>();
    }

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    private void LateUpdate()
    {
        meshCollider.isTrigger = gameObject.layer != BeatmapObjectSelectedLayer;
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

    public void SafeSetActive(bool active)
    {
        if (active != PreviousActiveState)
        {
            PreviousActiveState = active;
            gameObject.SetActive(active);
        }
    }
}
