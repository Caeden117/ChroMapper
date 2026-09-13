using UnityEngine;

public class ParametricBoxControllerData : EnvironmentComponentData<ParametricBoxLight>
{
    public float AlphaStart;
    public float AlphaEnd;
    public float AlphaMultiplier;
    public float Width;
    public float WidthStart;
    public float WidthEnd;
    public float Center;
    public float Height;
    public float Length;
    public float MinAlpha;

    public override void FillComponents(GameObject self, ParametricBoxLight comp, CreateContainer container)
    {
        comp.Renderer = self.GetComponent<Renderer>();
        if (comp.Renderer == null)
            Debug.LogWarning($"[EnvironmentTools] ParametricBoxLight on '{self.name}' has no Renderer on the same GameObject. SetColor will NRE at runtime.");
        comp.AlphaStart = AlphaStart;
        comp.AlphaEnd = AlphaEnd;
        comp.AlphaMultiplier = AlphaMultiplier;
        comp.Width = Width;
        comp.WidthStart = WidthStart;
        comp.WidthEnd = WidthEnd;
        comp.Center = Center;
        comp.Height = Height;
        comp.Length = Length;
        comp.MinAlpha = MinAlpha;
    }
}
