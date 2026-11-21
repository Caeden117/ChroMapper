using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Shared;
using UnityEngine;
using System.Collections.Generic;
using Beatmap.Containers;

public class LightGradientController : MonoBehaviour
{
    private static readonly int colorA = Shader.PropertyToID("_ColorA");
    private static readonly int colorB = Shader.PropertyToID("_ColorB");
    private static readonly int easingId = Shader.PropertyToID("_EasingID");
    private static readonly int strobeColorA = Shader.PropertyToID("_StrobeColorA");
    private static readonly int strobeColorB = Shader.PropertyToID("_StrobeColorB");
    private static readonly int strobeDurationId = Shader.PropertyToID("_StrobeDuration");
    private static readonly int strobeFadeId = Shader.PropertyToID("_StrobeFade");
    private static readonly int strobeFrequencyAId = Shader.PropertyToID("_StrobeFrequencyA");
    private static readonly int strobeFrequencyBId = Shader.PropertyToID("_StrobeFrequencyB");
    private static readonly int useStrobeColorsId = Shader.PropertyToID("_UseStrobeColors");
    private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");
    // Adjacent ribbons need the same lane coordinates so their anti-aliased edges meet on the same screen
    // pixels.
    private static readonly int ribbonPlaneOriginId = Shader.PropertyToID("_RibbonPlaneOrigin");
    private static readonly int ribbonPlaneTimeId = Shader.PropertyToID("_RibbonPlaneTime");
    private static readonly int ribbonPlaneWidthId = Shader.PropertyToID("_RibbonPlaneWidth");
    private static readonly int useRibbonPlaneId = Shader.PropertyToID("_UseRibbonPlane");
    private static readonly int ribbonEdgePaddingId = Shader.PropertyToID("_RibbonEdgePadding");

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private ObjectContainer interactionOwner;

    private MaterialPropertyBlock materialPropertyBlock;
    private GLSColorTransitionPreview colorTransitionPreview;
    private float ribbonLength;
    private IntersectionCollider interactionCollider;
    private GridLane ribbonLane;
    private Transform ribbonNode;
    private Transform ribbonScrollingTrack;

    public bool IsInteractiveTransitionRibbon => interactionCollider != null;
    public bool IsIncomingColorTransition { get; private set; }
    public bool AggregatesSameTimeBoxes { get; private set; }
    public float ColorTimelineStart { get; private set; }
    public float ColorTimelineDuration { get; private set; }

    public Vector2 GetHitUv(Vector3 worldPoint)
    {
        var point = meshRenderer.transform.InverseTransformPoint(worldPoint);
        // Ribbon prefabs use Unity's centered unit quad, whose UVs are local XY shifted by half a unit.
        return new Vector2(point.x + 0.5f, point.y + 0.5f);
    }

    public void UpdateColorTimeline(
        GLSColorTimeline timeline, BaseLightColorBase owner, bool incoming,
        EventAppearanceSO appearance, System.Func<float, bool> isBoostAt, bool aggregateSameTimeBoxes = false,
        ColorBoostEventIndex boostEvents = null)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();
        colorTransitionPreview ??= new GLSColorTransitionPreview();
        IsIncomingColorTransition = incoming;
        AggregatesSameTimeBoxes = aggregateSameTimeBoxes;
        var visible = colorTransitionPreview.UpdateTimeline(
            timeline, owner, incoming, appearance, isBoostAt, materialPropertyBlock, out var start, out var end,
            aggregateSameTimeBoxes, boostEvents);
        ColorTimelineStart = start;
        ColorTimelineDuration = visible ? end - start : 0f;
        SetVisible(visible);
        if (!visible)
            return;
        UpdateDuration(end - start);
        var position = transform.localPosition;
        position.z = (start - owner.SongBpmTime) * EditorScaleController.EditorScale * (4f / 3f);
        transform.localPosition = position;
        UpdateBoundRibbonPlane();
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    public void BindRibbonLane(GridLane lane, Transform node, Transform scrollingTrack)
    {
        ribbonNode = node;
        ribbonScrollingTrack = scrollingTrack;
        if (ribbonLane == lane)
            return;
        if (ribbonLane != null && isActiveAndEnabled)
            ribbonLane.Controller.OnGridViewUpdated -= HandleRibbonGridLayoutChanged;
        ribbonLane = lane;
        if (ribbonLane != null && isActiveAndEnabled)
            ribbonLane.Controller.OnGridViewUpdated += HandleRibbonGridLayoutChanged;
    }

    private void WriteRibbonPlane(Vector3 origin, Vector3 timeAxis, Vector3 widthAxis)
    {
        materialPropertyBlock.SetVector(ribbonPlaneOriginId, origin);
        materialPropertyBlock.SetVector(ribbonPlaneTimeId, timeAxis);
        materialPropertyBlock.SetVector(ribbonPlaneWidthId, widthAxis);
        materialPropertyBlock.SetFloat(useRibbonPlaneId, 1f);
    }

    // Remove the node's beat and length transforms and the track's scrolling Z. Preserve the grid scale,
    // offset, and ribbon orientation before the shader applies live rotation.
    private void UpdateBoundRibbonPlane()
    {
        if (ribbonLane == null)
        {
            materialPropertyBlock.SetFloat(useRibbonPlaneId, 0f);
            return;
        }
        var laneTransform = ribbonLane.transform;
        var trackPosition = ribbonScrollingTrack.localPosition;
        trackPosition.z = 0f;
        var nodePosition = ribbonNode.localPosition;
        nodePosition.z = 0f;
        var ribbonPosition = transform.localPosition;
        ribbonPosition.z = 0f;
        var sprite = meshRenderer.transform;
        var laneMatrix = Matrix4x4.TRS(laneTransform.localPosition,
            laneTransform.localRotation, laneTransform.localScale);
        var trackMatrix = Matrix4x4.TRS(trackPosition,
            ribbonScrollingTrack.localRotation, ribbonScrollingTrack.localScale);
        var nodeMatrix = Matrix4x4.TRS(nodePosition, ribbonNode.localRotation, ribbonNode.localScale);
        var ribbonMatrix = Matrix4x4.TRS(ribbonPosition, transform.localRotation, Vector3.one);
        var spriteMatrix = Matrix4x4.TRS(sprite.localPosition, sprite.localRotation, sprite.localScale);
        var plane = laneMatrix * trackMatrix * nodeMatrix * ribbonMatrix * spriteMatrix;
        WriteRibbonPlane(plane.MultiplyPoint3x4(new Vector3(-0.5f, -0.5f, 0f)),
            plane.MultiplyVector(Vector3.right) * (EditorScaleController.EditorScale * (4f / 3f)),
            plane.MultiplyVector(Vector3.up));
    }

    // Lane layout changes affect the shared projection, not the timeline texture.
    private void HandleRibbonGridLayoutChanged() => RefreshRibbonPlane();

    // Ghost nodes can move after appearance binding. Update the ribbon plane after that move.
    public void RefreshRibbonPlane()
    {
        if (ColorTimelineDuration <= 0f)
            return;
        UpdateBoundRibbonPlane();
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void OnEnable()
    {
        if (ribbonLane != null)
            ribbonLane.Controller.OnGridViewUpdated += HandleRibbonGridLayoutChanged;
    }

    private void OnDisable()
    {
        if (ribbonLane != null)
            ribbonLane.Controller.OnGridViewUpdated -= HandleRibbonGridLayoutChanged;
    }

    public void UpdateGradientData(
        ChromaLightGradient gradient,
        BasicEventColorLerpType colorLerpType = BasicEventColorLerpType.RGB,
        ChromaLightGradient strobeGradient = null,
        int? easeType = null,
        float strobeFrequencyA = 0f,
        float strobeFrequencyB = 0f,
        bool strobeFade = false)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();

        ColorTimelineDuration = 0f;
        // A renderer reused for a Basic Event gradient must discard the GLS lane frame and edge padding.
        materialPropertyBlock.SetFloat(useRibbonPlaneId, 0f);
        materialPropertyBlock.SetFloat(ribbonEdgePaddingId, 0f);
        materialPropertyBlock.SetVector(colorA, gradient.StartColor);
        materialPropertyBlock.SetVector(colorB, gradient.EndColor);
        materialPropertyBlock.SetInt(
            easingId,
            easeType.HasValue
                ? Easing.EasingShaderId(easeType.Value)
                : Easing.EasingShaderId(gradient.EasingType));
        var renderedStrobeGradient = strobeGradient ?? gradient;
        materialPropertyBlock.SetVector(strobeColorA, renderedStrobeGradient.StartColor);
        materialPropertyBlock.SetVector(strobeColorB, renderedStrobeGradient.EndColor);
        materialPropertyBlock.SetFloat(useStrobeColorsId, strobeGradient != null ? 1f : 0f);
        materialPropertyBlock.SetFloat(strobeDurationId, renderedStrobeGradient.Duration);
        materialPropertyBlock.SetFloat(strobeFadeId, strobeGradient != null && strobeFade ? 1f : 0f);
        materialPropertyBlock.SetFloat(strobeFrequencyAId, strobeFrequencyA);
        materialPropertyBlock.SetFloat(strobeFrequencyBId, strobeFrequencyB);
        // The shader uses the shared enum values to distinguish legacy scalar HSV from true angular HSV.
        materialPropertyBlock.SetInt(useHsvId, (int)colorLerpType);
        
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void OnDestroy() => colorTransitionPreview?.Dispose();

    // note: 4/3rds magic number comes from the fact that events are 0.75m in size
    public void UpdateDuration(float duration)
    {
        ribbonLength = duration * EditorScaleController.EditorScale * (4f / 3);
        transform.localPosition = new Vector3(
            0,
            -0.5f + 0.005f,
            0);
        transform.localScale = new Vector3(ribbonLength, 1, 1);
        // Keep the ribbon collider in the source event's current intersection chunk after event moves.
        SyncInteractionColliderGroup();
    }

    public void SetVisible(bool visible)
    {
        // Ribbon prefab children start inactive, so enabling only their renderer cannot make a GLS transition visible.
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
        meshRenderer.enabled = visible;
        // Create the ribbon collider lazily so hidden ribbons add no intersection work.
        if (visible)
            EnsureInteractionCollider();
    }

    /// <summary>
    /// Ensures the ribbon has an interaction collider for Basic Event and GLS ribbons.
    /// Creates the collider lazily so hidden ribbons add no intersection work.
    /// </summary>
    private void EnsureInteractionCollider()
    {
        if (interactionCollider != null)
        {
            SyncInteractionColliderGroup();
            return;
        }

        if (interactionOwner == null)
            return;

        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        // Configure before re-enabling so IntersectionCollider registers once with valid mesh and chunk data.
        interactionCollider = meshRenderer.gameObject.AddComponent<IntersectionCollider>();
        interactionCollider.enabled = false;
        interactionCollider.Mesh = meshFilter.sharedMesh;
        interactionCollider.CollisionGroups = new List<int> { interactionOwner.ChunkID };
        interactionCollider.enabled = true;
    }

    private void SyncInteractionColliderGroup()
    {
        if (interactionCollider == null || interactionOwner == null)
            return;

        var chunkId = interactionOwner.ChunkID;
        if (interactionCollider.CollisionGroups.Count == 1
            && interactionCollider.CollisionGroups[0] == chunkId)
        {
            return;
        }

        // Re-enable through IntersectionCollider's lifecycle so the custom raycaster receives the new chunk.
        interactionCollider.enabled = false;
        interactionCollider.CollisionGroups.Clear();
        interactionCollider.CollisionGroups.Add(chunkId);
        interactionCollider.enabled = true;
    }
}
