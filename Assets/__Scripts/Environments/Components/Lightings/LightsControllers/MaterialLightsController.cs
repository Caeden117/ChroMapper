using UnityEngine;

public class MaterialLightsController : CombinedLightsController
{
    public MeshRenderer MeshRenderer;
    public bool SetAlphaOnly;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public string ColorProperty = "_Color";

    private MaterialPropertyBlock mpb;
    private int propertyId;
    private float alpha;

    protected override bool Initialize()
    {
        mpb = new MaterialPropertyBlock();
        propertyId = Shader.PropertyToID(ColorProperty);
        if (SetColorOnly) alpha = MeshRenderer.sharedMaterial.GetColor(propertyId).a;
        if (SetAlphaOnly) Color = MeshRenderer.sharedMaterial.GetColor(propertyId);
        return MeshRenderer != null;
    }

    public override void SetColor(Color col)
    {
        Color = col;
        if (!HasInitialized) return;

        mpb ??= new MaterialPropertyBlock();
        if (SetAlphaOnly)
            Color.a = Color.a;
        else
            Color = AlphaIntoColor ? new Color(Color.a, Color.a, Color.a) : Color;
        if (SetColorOnly) Color.a = alpha;
        mpb.Clear();
        mpb.SetColor(propertyId, Color);
        MeshRenderer.SetPropertyBlock(mpb);
    }
}
