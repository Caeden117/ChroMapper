using System;
using System.Linq;
using UnityEngine;

public class LightmapLightWithIdsData : EnvironmentComponentData<LightmapLightsController>
{
    public string BakeId;
    public float Intensity = 1f;
    public float ProbeIntensity = 1f;
    public LightIntensitiesWithIdData[] LightIntensityData;
    public string MixType;
    public float NormalizerWeight = 1f;

    public override void FillComponents(
        GameObject self,
        LightmapLightsController comp,
        CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var d = comp.gameObject.AddComponent<LightmapIntensityData>();
                d.Intensity = data.Intensity;
                d.ProbeHighlightsIntensityMultiplier = data.ProbeHighlightsIntensityMultiplier;
                return d;
            })
            .ToArray();

        comp.BakeId = Enum.Parse<LightConstants.BakeId>(BakeId);
        comp.Intensity = Intensity;
        comp.ProbeIntensity = ProbeIntensity;
        comp.MixType = Enum.Parse<ColorMixAndWeightingApproach>(MixType);
        comp.NormalizerWeight = NormalizerWeight;
    }

    public class LightIntensitiesWithIdData
    {
        public float Intensity;
        public float ProbeHighlightsIntensityMultiplier = 1f;
    }
}
