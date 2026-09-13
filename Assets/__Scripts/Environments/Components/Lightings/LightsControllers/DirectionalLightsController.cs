using UnityEngine;

public class DirectionalLightsController : CombinedLightsController
{
    public DirectionalLight Light;

    public bool SetIntensityOnly;
    public Color DefaultColor = Color.black;

    protected override bool Initialize() => Light != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;
        if (SetIntensityOnly) color = DefaultColor.WithValue(color.a);
        Light.Color = color;
    }
}
