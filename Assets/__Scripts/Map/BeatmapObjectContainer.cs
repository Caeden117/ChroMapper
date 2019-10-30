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

    [SerializeField] protected BoxCollider boxCollider;

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    internal virtual void OnMouseOver()
    {
        if (KeybindsController.AltHeld && Input.GetMouseButton(0) && !SelectionController.IsObjectSelected(this))
            SelectionController.Select(this, true);
        if (!KeybindsController.ShiftHeld) return;
        if (Input.GetMouseButtonDown(0))
        { //Selects if it's not already selected, deselect if it is.
            if (SelectionController.IsObjectSelected(this)) SelectionController.Deselect(this);
            else SelectionController.Select(this, true);
        }
        else if (Input.GetMouseButtonDown(2))
        {
            if (SelectionController.HasSelectedObjects())
            {
                SelectionController.Select(this, true, false);
                return;
            }else FlaggedForDeletionEvent?.Invoke(this);
        }
    }

    internal virtual void SafeSetActive(bool active)
    {
        if (active != gameObject.activeSelf)
        {
            gameObject.SetActive(active);
            if (boxCollider != null) boxCollider.enabled = active;
        }
    }

    internal void SafeSetBoxCollider(bool con)
    {
        if (boxCollider == null) return;
        if (con != boxCollider.isTrigger) boxCollider.isTrigger = con;
    }
}
