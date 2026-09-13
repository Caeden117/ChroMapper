using UnityEngine;

public class DirectionalLightsGroupController : CombinedLightsGroupController
{
    public DirectionalLight Light;

    protected override bool Initialize() => Light != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;
        Light.Color = color;
    }
}
