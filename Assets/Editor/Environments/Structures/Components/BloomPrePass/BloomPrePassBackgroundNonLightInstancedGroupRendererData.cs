using System.Linq;
using UnityEngine;

public class
    BloomPrePassBackgroundNonLightInstancedGroupRendererData : EnvironmentComponentData<
    BloomPrePassBackgroundNonLightInstancedGroupRenderer>
{
    public int ExecutionTimeType;

    public int[] Renderers;
    public SupportedPropertyComponent[] SupportedProperties;

    public class SupportedPropertyComponent
    {
        public int PropertyType;
        public string PropertyName;
    }

    public override void FillComponents(
        GameObject self,
        BloomPrePassBackgroundNonLightInstancedGroupRenderer comp,
        CreateContainer container)
    {
        comp.Renderers =
            Renderers
                .Select(container.GetComponentOrNull<BloomPrePassBackgroundNonLightRenderer>)
                .ToArray();
        comp.ExecutionTimeType = (BloomPrePassNonLightPass.ExecutionTime)ExecutionTimeType;
        comp.SupportedProperties = SupportedProperties
            .Select(x => new BloomPrePassBackgroundNonLightInstancedGroupRenderer.SupportedProperty
            {
                PropertyType = (BloomPrePassBackgroundNonLightInstancedGroupRenderer.PropertyType)x.PropertyType,
                PropertyName = x.PropertyName
            })
            .ToArray();
    }
}
