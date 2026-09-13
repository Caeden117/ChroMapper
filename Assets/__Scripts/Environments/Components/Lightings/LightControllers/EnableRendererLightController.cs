using UnityEngine;

public class EnableRendererLightController : LightController
{
    [SerializeField] public Renderer Renderer;
    [SerializeField] public float HideAlphaRangeMin = 0.001f;
    [SerializeField] public float HideAlphaRangeMax = 1f;

    public override bool IsPhysical => false;
    protected override bool Initialize() => Renderer != null;

    public override void SetColor(Color color) =>
        Renderer.enabled = color.a >= HideAlphaRangeMin && color.a <= HideAlphaRangeMax;
}
