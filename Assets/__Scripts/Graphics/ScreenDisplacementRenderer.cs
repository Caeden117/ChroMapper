using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class ScreenDisplacementRenderer : MonoBehaviour
{
    private const string displacementShaderName = "ChroMapper/Object/Obstacle Distortion";

    private static readonly List<ScreenDisplacementRenderer> renderers = new();
    private static bool displacementEnabled = true;

    [SerializeField, Range(0, 31)] private int displacementLayer = 31;

    private Renderer targetRenderer;
    private int originalLayer;
    private bool usesDisplacementLayer;

    public Renderer TargetRenderer => targetRenderer;
    public bool IsReady =>
        displacementEnabled
        && isActiveAndEnabled
        && targetRenderer != null
        && targetRenderer.enabled
        && usesDisplacementLayer;

    public static IReadOnlyList<ScreenDisplacementRenderer> Renderers => renderers;

    public static void SetEnabled(bool enabled)
    {
        displacementEnabled = enabled;
        foreach (var renderer in renderers)
        {
            if (renderer != null) renderer.RefreshLayer();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        renderers.Clear();
        displacementEnabled = true;
    }

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        originalLayer = gameObject.layer;
    }

    private void OnEnable()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (!renderers.Contains(this)) renderers.Add(this);
        RefreshLayer();
    }

    private void LateUpdate() => RefreshLayer();

    private void OnDisable()
    {
        renderers.Remove(this);
        RestoreLayer();
    }

    private void OnDestroy()
    {
        renderers.Remove(this);
        RestoreLayer();
    }

    private void RefreshLayer()
    {
        if (!displacementEnabled)
        {
            RestoreLayer();
            return;
        }

        var material = targetRenderer == null ? null : targetRenderer.sharedMaterial;
        var shouldUseDisplacementLayer =
            material != null && material.shader != null && material.shader.name == displacementShaderName;
        if (shouldUseDisplacementLayer == usesDisplacementLayer) return;

        usesDisplacementLayer = shouldUseDisplacementLayer;
        gameObject.layer = usesDisplacementLayer ? displacementLayer : originalLayer;
    }

    private void RestoreLayer()
    {
        if (!usesDisplacementLayer) return;
        gameObject.layer = originalLayer;
        usesDisplacementLayer = false;
    }
}
