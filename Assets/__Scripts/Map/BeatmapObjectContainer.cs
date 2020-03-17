using System;
using System.Linq;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour {

    public static readonly int BeatmapObjectLayer = 9; //todo: is this needed
    public static readonly int BeatmapObjectSelectedLayer = 10; //todo: is this needed

    public static Action<BeatmapObjectContainer, bool, string> FlaggedForDeletionEvent;

    [SerializeField] protected Material SelectionMaterial;
    public bool OutlineVisible { get => SelectionMaterial.GetFloat(Outline) != 0;
        set {
            if (!SelectionMaterial.HasProperty(OutlineColor)) return;
            SelectionMaterial.SetFloat(Outline, value ? 0.03f : 0);
            Color c = SelectionMaterial.GetColor(OutlineColor);
            SelectionMaterial.SetColor(OutlineColor, new Color(c.r, c.g, c.b, value ? 1 : 0));
        }
    }

    public Track AssignedTrack { get; private set; } = null;

    [SerializeField]
    public abstract BeatmapObject objectData { get; set; }

    public abstract void UpdateGridPosition();

    protected int chunkID;
    public int ChunkID { get => chunkID; }

    [SerializeField] protected BoxCollider boxCollider;
    private bool selectionStateChanged;
    private GameObject containerGameObject;

    private static readonly int Outline = Shader.PropertyToID("_Outline");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

    protected virtual void Awake()
    {
        SelectionMaterial = GetComponentInChildren<MeshRenderer>().materials.Last();
        OutlineVisible = false;
        containerGameObject = gameObject;
    }

    private void OnDestroy()
    {
        if (SelectionController.IsObjectSelected(this))
            SelectionController.Deselect(this);
    }

    internal virtual void OnMouseOver()
    {
        if (!KeybindsController.ShiftHeld) {
            if (Input.GetMouseButtonDown(0) && NotePlacementUI.delete)
                FlaggedForDeletionEvent?.Invoke(this, true, "Deleted with the Delete Tool.");
            return;
        }
        if (Input.GetMouseButton(0) && !selectionStateChanged)
        { //Selects if it's not already selected, deselect if it is and the user just clicked down.
            if (!SelectionController.IsObjectSelected(this)) SelectionController.Select(this, true);
            else if (Input.GetMouseButtonDown(0)) SelectionController.Deselect(this);
            selectionStateChanged = true;
        }
        else if (Input.GetMouseButtonDown(2))
            FlaggedForDeletionEvent?.Invoke(this, true, "Deleted by a Middle Mouse event.");
    }

    public void OnMouseUp()
    {
        selectionStateChanged = false;
    }

    internal virtual void SafeSetActive(bool active)
    {
        if (active != containerGameObject.activeSelf)
        {
            containerGameObject.SetActive(active);
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
        SelectionMaterial.SetColor(OutlineColor, color);
    }

    public void AssignTrack(Track track)
    {
        AssignedTrack = track;
        chunkID = (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
                 MidpointRounding.AwayFromZero);
    }
}
