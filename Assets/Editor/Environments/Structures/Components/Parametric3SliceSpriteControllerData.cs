using System.Linq;
using UnityEngine;

public class Parametric3SliceSpriteControllerData : EnvironmentComponentData<ParametricSpriteLight>
{
    public float WidthMultiplier;
    public float AlphaStart;
    public float AlphaEnd;
    public float AlphaMultiplier;
    public float Width;
    public float WidthStart;
    public float WidthEnd;
    public float Center;
    public float Length;
    public float MinAlpha;

    public override void FillComponents(GameObject self, ParametricSpriteLight comp, CreateContainer container)
    {
        comp.Renderer = self.GetComponent<Renderer>();

        // Good chance env data doesnt have this and it's fine
        var meshFilter = comp.GetComponent<MeshFilter>();
        if (comp.Renderer == null || meshFilter == null || meshFilter.sharedMesh == null)
        {
            meshFilter = self.GetOrAddComponent<MeshFilter>();
            meshFilter.sharedMesh = container.Library.SliceSprite;
            var renderer = self.GetOrAddComponent<MeshRenderer>();

            var chromaId = self.GetComponent<ChromaIDMarker>().ChromaID;
            var envObject = container.Data.Objects.First(x => x.ChromaID == chromaId);
            if (envObject.Components.MeshRenderer?.First().Materials.Any() ?? false)
            {
                if (container.TryGetMaterial(envObject.Components.MeshRenderer[0].Materials[0], out var mat)
                    && mat != null)
                    renderer.sharedMaterial = mat;
                else
                {
                    Debug.LogWarning(
                        $"{envObject.ChromaID} material not found for:\n{envObject.Components.MeshRenderer[0].Materials[0]}");
                }
            }

            comp.Renderer = renderer;
        }

        comp.WidthMultiplier = WidthMultiplier;
        comp.AlphaStart = AlphaStart;
        comp.AlphaEnd = AlphaEnd;
        comp.AlphaMultiplier = AlphaMultiplier;
        comp.Width = Width;
        comp.WidthStart = WidthStart;
        comp.WidthEnd = WidthEnd;
        comp.Center = Center;
        comp.Length = Length;
        comp.MinAlpha = MinAlpha;
    }
}
