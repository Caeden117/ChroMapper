using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class BeatmapObjectContainer : MonoBehaviour
{
    public static Action<BeatmapObjectContainer, bool, string> FlaggedForDeletionEvent;

    internal static readonly int color = Shader.PropertyToID("_Color");
    internal static readonly int rotation = Shader.PropertyToID("_Rotation");
    internal static readonly int outline = Shader.PropertyToID("_Outline");
    internal static readonly int outlineColor = Shader.PropertyToID("_OutlineColor");
    [FormerlySerializedAs("dragging")] public bool Dragging;

    [FormerlySerializedAs("colliders")] [SerializeField] protected List<IntersectionCollider> Colliders;

    [FormerlySerializedAs("selectionRenderers")] [SerializeField] protected List<Renderer> SelectionRenderers = new List<Renderer>();

    [FormerlySerializedAs("boxCollider")] [SerializeField] protected BoxCollider BoxCollider;
    private readonly List<Renderer> modelRenderers = new List<Renderer>();
    internal bool selectionStateChanged;

    public bool OutlineVisible
    {
        get => MaterialPropertyBlock.GetFloat(outline) != 0;
        set
        {
            SelectionRenderers.ForEach(r => r.enabled = value);
            MaterialPropertyBlock.SetFloat(outline, value ? 0.05f : 0);
            UpdateMaterials();
        }
    }

    public Track AssignedTrack { get; private set; }

    public MaterialPropertyBlock MaterialPropertyBlock { get; private set; }

    public abstract BeatmapObject ObjectData { get; set; }

    public int ChunkID => (int)(ObjectData.Time / Intersections.ChunkSize);

    public abstract void UpdateGridPosition();

    public virtual void Setup()
    {
        if (MaterialPropertyBlock == null)
        {
            MaterialPropertyBlock = new MaterialPropertyBlock();
            modelRenderers.AddRange(GetComponentsInChildren<Renderer>(true).Where(x => !(x is SpriteRenderer)));
        }
    }

    internal virtual void SafeSetActive(bool active)
    {
        if (active != gameObject.activeSelf) gameObject.SetActive(active);
    }

    internal void SafeSetBoxCollider(bool con)
    {
        if (BoxCollider == null) return;
        if (con != BoxCollider.isTrigger) BoxCollider.isTrigger = con;
    }

    internal virtual void UpdateMaterials()
    {
        foreach (var renderer in modelRenderers) renderer.SetPropertyBlock(MaterialPropertyBlock);
    }

    public void SetRotation(float rotation)
    {
        MaterialPropertyBlock.SetFloat(BeatmapObjectContainer.rotation, rotation);
        UpdateMaterials();
    }

    public void SetOutlineColor(Color color, bool automaticallyShowOutline = true)
    {
        if (automaticallyShowOutline) OutlineVisible = true;
        MaterialPropertyBlock.SetColor(outlineColor, color);
        UpdateMaterials();
    }

    public virtual void AssignTrack(Track track) => AssignedTrack = track;

    protected virtual void UpdateCollisionGroups()
    {
        var chunkId = ChunkID;

        foreach (var collider in Colliders)
        {
            var unregistered = Intersections.UnregisterColliderFromGroups(collider);
            collider.CollisionGroups.Clear();
            collider.CollisionGroups.Add(chunkId);
            if (unregistered) Intersections.RegisterColliderToGroups(collider);
        }
    }
}
