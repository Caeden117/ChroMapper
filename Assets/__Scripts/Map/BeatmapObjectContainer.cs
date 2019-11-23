using System;
using System.Linq;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour {

    public static readonly int BeatmapObjectLayer = 9;
    public static readonly int BeatmapObjectSelectedLayer = 10;

    public static Action<BeatmapObjectContainer> FlaggedForDeletionEvent;

    [SerializeField] protected Material SelectionMaterial;
    public bool OutlineVisible { get => SelectionMaterial.GetFloat("_Outline") != 0;
        set {
            if (!SelectionMaterial.HasProperty("_OutlineColor")) return;
            SelectionMaterial.SetFloat("_Outline", value ? 0.03f : 0);
            Color c = SelectionMaterial.GetColor("_OutlineColor");
            SelectionMaterial.SetColor("_OutlineColor", new Color(c.r, c.g, c.b, value ? 1 : 0));
        }
    }

    [SerializeField]
    public abstract BeatmapObject objectData { get; set; }

    public abstract void UpdateGridPosition();

    public int ChunkID
    {
        get {
            return (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
                MidpointRounding.AwayFromZero);
        }
    }

    [SerializeField] protected BoxCollider boxCollider;

    private void Awake()
    {
        SelectionMaterial = GetComponentInChildren<MeshRenderer>().materials.Last();
        OutlineVisible = false;
    }

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    internal virtual void OnMouseOver()
    {
        if (KeybindsController.AltHeld && Input.GetMouseButton(0) && !SelectionController.IsObjectSelected(this)
            && !Settings.Instance.BoxSelect)
            SelectionController.Select(this, true);
        if (!KeybindsController.ShiftHeld) {
            if (Input.GetMouseButtonDown(0) && NotePlacementUI.delete) FlaggedForDeletionEvent?.Invoke(this);
            return;
        }
        if (Input.GetMouseButtonDown(0))
        { //Selects if it's not already selected, deselect if it is.
            if (SelectionController.IsObjectSelected(this)) SelectionController.Deselect(this);
            else SelectionController.Select(this, true);
        }
        else if (Input.GetMouseButtonDown(2))
        {
            /*if (SelectionController.HasSelectedObjects())
            {
                SelectionController.Select(this, true, false);
                return;
            }else*/ FlaggedForDeletionEvent?.Invoke(this);
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

    internal void SetOutlineColor(Color color, bool automaticallyShowOutline = true)
    {
        if (automaticallyShowOutline) OutlineVisible = true;
        SelectionMaterial.SetColor("_OutlineColor", color);
    }
}
