using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour
{
    public static Action<BeatmapObjectContainer, bool, string> FlaggedForDeletionEvent;

    private static readonly int Outline = Shader.PropertyToID("_Outline");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

    public bool OutlineVisible
    { 
        get => SelectionMaterials.FirstOrDefault()?.GetFloat(Outline) != 0;
        set {
            foreach (Material SelectionMaterial in SelectionMaterials)
            {
                SelectionMaterial.SetFloat(Outline, value ? 0.05f : 0);
            }
        }
    }

    public Track AssignedTrack { get; private set; } = null;

    [SerializeField]
    public abstract BeatmapObject objectData { get; set; }

    public abstract void UpdateGridPosition();

    protected int chunkID;
    public int ChunkID { get => chunkID; }
    public List<Material> ModelMaterials = new List<Material>() { };
    public List<Material> SelectionMaterials = new List<Material>() { };

    [SerializeField] protected BoxCollider boxCollider;
    internal bool SelectionStateChanged;

    public virtual void Setup()
    {
        ModelMaterials.Clear();
        SelectionMaterials.Clear();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SpriteRenderer) continue;

            ModelMaterials.Add(renderer.materials.First());
            SelectionMaterials.Add(renderer.materials.Last());
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

    public void SetOutlineColor(Color color, bool automaticallyShowOutline = true)
    {
        if (automaticallyShowOutline) OutlineVisible = true;
        foreach (Material SelectionMaterial in SelectionMaterials)
        {
            SelectionMaterial.SetColor(OutlineColor, color);
        }
    }

    public virtual void AssignTrack(Track track)
    {
        AssignedTrack = track;
        chunkID = (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
                 MidpointRounding.AwayFromZero);
    }
}
