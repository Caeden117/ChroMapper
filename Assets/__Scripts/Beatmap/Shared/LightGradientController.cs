using Beatmap.Shared;
using UnityEngine;
using System.Collections.Generic;
using Beatmap.Containers;

public class LightGradientController : MonoBehaviour
{
    private static readonly int colorA = Shader.PropertyToID("_ColorA");
    private static readonly int colorB = Shader.PropertyToID("_ColorB");
    private static readonly int easingId = Shader.PropertyToID("_EasingID");
    private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialPropertyBlock;
    private float ribbonLength;
    private EventContainer interactionOwner;
    private IntersectionCollider interactionCollider;

    // Only Basic Event transition ribbons create a collider; GLS ribbons retain their existing input behavior.
    public bool IsInteractiveBasicEventRibbon => interactionCollider != null;

    public void UpdateGradientData(
        ChromaLightGradient gradient,
        BasicEventColorLerpType colorLerpType = BasicEventColorLerpType.RGB)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();

        materialPropertyBlock.SetColor(colorA, gradient.StartColor);
        materialPropertyBlock.SetColor(colorB, gradient.EndColor);
        materialPropertyBlock.SetInt(easingId, Easing.EasingShaderId(gradient.EasingType));
        // The shader uses the shared enum values to distinguish legacy scalar HSV from true angular HSV.
        materialPropertyBlock.SetInt(useHsvId, (int)colorLerpType);
        
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

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
        // Create the Basic Event ribbon collider lazily so hidden and GLS ribbons add no intersection work.
        if (visible)
            EnsureInteractionCollider();
    }

    /// <summary>
    /// Ensures the ribbon has an interaction collider for Basic Event ribbons.
    /// Creates the collider lazily so hidden and GLS ribbons add no intersection work.
    /// </summary>
    private void EnsureInteractionCollider()
    {
        if (interactionCollider != null)
        {
            SyncInteractionColliderGroup();
            return;
        }

        interactionOwner = GetComponentInParent<EventContainer>();
        if (interactionOwner == null)
            return;

        var meshFilter = meshRenderer.GetComponent<MeshFilter>();
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
