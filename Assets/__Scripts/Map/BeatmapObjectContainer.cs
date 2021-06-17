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
    public bool dragging;

    public abstract void UpdateGridPosition();

    [SerializeField] protected List<IntersectionCollider> colliders;

    public int ChunkID => (int)(objectData._time / BeatmapObjectContainerCollection.ChunkSize);

    public List<Material> ModelMaterials = new List<Material>() { };
    public List<Material> SelectionMaterials = new List<Material>() { };
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
        }
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
    }

    protected virtual void UpdateCollisionGroups()
    {
        var chunkId = ChunkID;

        foreach (var collider in colliders)
        {
            if (collider.CollisionGroups.Count == 0)
            {
                collider.CollisionGroups.Add(chunkId);
                Intersections.RegisterColliderToGroups(collider, collider.CollisionGroups);
                continue;
            }

            collider.CollisionGroups.Clear();
            collider.CollisionGroups.Add(chunkId);

            if (Intersections.UnregisterColliderFromGroups(collider))
            {
                Intersections.RegisterColliderToGroups(collider, collider.CollisionGroups);
            }
        }
    }
}
