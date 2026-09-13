using UnityEngine;

public class LightmapsIntensityData : LightController
{
    public float Intensity;
    public LightConstants.BakeId BakeId;
    public float Weight;

    private int lightmapLightIdColorId;
    private int lightProbeLightIdColorId;

    protected override bool Initialize()
    {
        lightmapLightIdColorId = LightConstants.GetLightmapLightBakeIdPropertyId(BakeId);
        lightProbeLightIdColorId = LightConstants.GetLightProbeLightBakeIdPropertyId(BakeId);
        return true;
    }

    public override void SetColor(Color color) => Color = color;

    public void SetDataToShaders(Color lightmapColor, Color probeColor)
    {
        if (!HasInitialized) return;
        Shader.SetGlobalColor(lightmapLightIdColorId, lightmapColor);
        Shader.SetGlobalColor(lightProbeLightIdColorId, probeColor);
    }
}
