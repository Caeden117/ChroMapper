using UnityEngine;

public class PointLightsController : CombinedLightsController
{
    public PointLight Light;

    protected override bool Initialize() => Light != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (HasInitialized) Light.Color = color;
    }
}
