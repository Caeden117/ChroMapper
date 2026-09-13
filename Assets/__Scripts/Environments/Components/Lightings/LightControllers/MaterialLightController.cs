using UnityEngine;

public class MaterialLightController : LightController
{
    public Renderer Renderer;

    public bool SetAlphaOnly;
    public float AlphaIntensity;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public bool MultiplyColorWithAlpha;
    public bool MultiplyColor;
    public float ColorMultiplier;
    public float Alpha;
    public string Property;

    private int propId;

    private void Awake() => propId = Shader.PropertyToID(Property);

    public override bool IsPhysical => true;
    protected override bool Initialize() => Renderer != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;

        var adjustedColor = color;
        color.a *= AlphaIntensity;
        if (SetAlphaOnly)
            adjustedColor.a = color.a;
        else
            adjustedColor = AlphaIntoColor ? new Color(color.a, color.a, color.a) : color;

        if (SetColorOnly) adjustedColor.a = Alpha;

        var alpha = 1f;
        if (MultiplyColorWithAlpha) alpha *= color.a;

        if (MultiplyColor) alpha *= ColorMultiplier;

        if (MultiplyColorWithAlpha || MultiplyColor)
        {
            adjustedColor.r *= alpha;
            adjustedColor.g *= alpha;
            adjustedColor.b *= alpha;
        }

        Mpb.SetColor(propId, adjustedColor);
        Renderer.SetPropertyBlock(Mpb);
    }
}
