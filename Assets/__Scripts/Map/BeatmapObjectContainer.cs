using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BeatmapObjectContainer : MonoBehaviour
{
    public static Action<BeatmapObjectContainer, bool, string> FlaggedForDeletionEvent;

    internal static readonly int Color = Shader.PropertyToID("_Color");
    internal static readonly int Rotation = Shader.PropertyToID("_Rotation");
    internal static readonly int Outline = Shader.PropertyToID("_Outline");
    internal static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    internal static readonly int HandleScale = Shader.PropertyToID("_HandleScale");

    public bool OutlineVisible
    { 
        get => MaterialPropertyBlock.GetFloat(Outline) != 0;
        set
        {
            selectionRenderers.ForEach(r => r.enabled = value);
            MaterialPropertyBlock.SetFloat(Outline, value ? 0.05f : 0);
            UpdateMaterials();
        }
    }

    public Track AssignedTrack { get; private set; } = null;

    public MaterialPropertyBlock MaterialPropertyBlock { get; private set; } = null;

    [SerializeField]
    public abstract BeatmapObject objectData { get; set; }
    public bool dragging;

    public abstract void UpdateGridPosition();

    [SerializeField] protected List<IntersectionCollider> colliders;

    public int ChunkID => (int)(objectData._time / Intersections.ChunkSize);

    [SerializeField] protected List<Renderer> selectionRenderers = new List<Renderer>();
    private List<Renderer> modelRenderers = new List<Renderer>();

    [SerializeField] protected BoxCollider boxCollider;
    internal bool SelectionStateChanged;

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
        if (active != gameObject.activeSelf)
        {
            gameObject.SetActive(active);
        }
    }

    internal void SafeSetBoxCollider(bool con)
    {
        if (boxCollider == null) return;
        if (con != boxCollider.isTrigger) boxCollider.isTrigger = con;
    }

    internal virtual void UpdateMaterials()
    {
        foreach (var renderer in modelRenderers)
        {
            renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
    }

    public void SetRotation(float rotation)
    {
        MaterialPropertyBlock.SetFloat(Rotation, rotation);
        UpdateMaterials();
    }

    public void SetOutlineColor(Color color, bool automaticallyShowOutline = true)
    {
        if (automaticallyShowOutline) OutlineVisible = true;
        MaterialPropertyBlock.SetColor(OutlineColor, color);
        UpdateMaterials();
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
            var unregistered = Intersections.UnregisterColliderFromGroups(collider);
            collider.CollisionGroups.Clear();
            collider.CollisionGroups.Add(chunkId);
            if (unregistered) Intersections.RegisterColliderToGroups(collider);
        }
    }
}
