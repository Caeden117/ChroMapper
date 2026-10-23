using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialPropertyBlockController : MonoBehaviour
{
    public List<Renderer> Renderers = new();
    private readonly Dictionary<Renderer, int> indexMap = new();
    private readonly HashSet<Renderer> invertedCutoutRenderers = new();
    private static readonly int cutoutId = Shader.PropertyToID("_Cutout");

    public MaterialPropertyBlock Mpb => mpb ??= new MaterialPropertyBlock();
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        mpb ??= new MaterialPropertyBlock();
        for (var i = 0; i < Renderers.Count; i++) indexMap[Renderers[i]] = i;
    }

    public void ApplyChanges()
    {
        var len = Renderers.Count;
        if (invertedCutoutRenderers.Count == 0 || mpb == null || !mpb.HasFloat(cutoutId))
        {
            for (var i = 0; i < len; i++) Renderers[i].SetPropertyBlock(mpb);
            return;
        }

        // Some custom shaders use _Cutout as visibility; native/editor shaders use the hidden amount.
        var cutout = mpb.GetFloat(cutoutId);
        try
        {
            for (var i = 0; i < len; i++)
            {
                var renderer = Renderers[i];
                mpb.SetFloat(cutoutId, invertedCutoutRenderers.Contains(renderer) ? 1f - cutout : cutout);
                renderer.SetPropertyBlock(mpb);
            }
        }
        finally
        {
            mpb.SetFloat(cutoutId, cutout);
        }
    }

    public void ShowRenderer(bool active)
    {
        var len = Renderers.Count;
        for (var i = 0; i < len; i++) Renderers[i].enabled = active;
    }

    /// <summary>Registers a renderer receiving this controller's property block.</summary>
    /// <param name="r">Renderer to register.</param>
    /// <param name="invertCutout">Write visibility to _Cutout for a confirmed visibility-based shader.</param>
    public void Add(Renderer r, bool invertCutout = false)
    {
        Renderers.Add(r);
        indexMap[r] = Renderers.Count - 1;
        if (invertCutout) invertedCutoutRenderers.Add(r);
    }

    /// <summary>Registers renderers, optionally converting dissolve values for visibility-based custom shaders.</summary>
    /// <param name="list">Renderers receiving this controller's property block.</param>
    /// <param name="invertCutout">Write visibility to _Cutout instead of the editor's hidden amount.</param>
    public void Add(IEnumerable<Renderer> list, bool invertCutout = false)
    {
        foreach (var r in list)
            Add(r, invertCutout);
    }

    public void Remove(Renderer r)
    {
        invertedCutoutRenderers.Remove(r);
        if (indexMap.TryGetValue(r, out var v))
        {
            var lastRenderer = Renderers[^1];
            Renderers.RemoveAtSwapBack(v);
            if (lastRenderer != r) indexMap[lastRenderer] = indexMap[r];
            indexMap.Remove(r);
        }
        else
            Renderers.Remove(r);
    }

    public void Remove(IEnumerable<Renderer> r)
    {
        foreach (var u in r) Remove(u);
    }
}
