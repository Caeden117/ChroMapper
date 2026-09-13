using UnityEngine;

public class LightTranslationGroupData : ILightTransformGroup
{
    public Vector2 xTranslationLimits;
    public Vector2 yTranslationLimits;
    public Vector2 zTranslationLimits;
    public Vector2 xDistributionLimits;
    public Vector2 yDistributionLimits;
    public Vector2 zDistributionLimits;

    public bool MirrorX { get; set; }
    public bool MirrorY { get; set; }
    public bool MirrorZ { get; set; }
    public string[] XTransforms { get; set; }
    public string[] YTransforms { get; set; }
    public string[] ZTransforms { get; set; }
    public int GroupId { get; set; }
    public int Count { get; set; }
}
