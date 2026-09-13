using System.Linq;
using UnityEngine;

public class TubeBloomPrePassLightReflectionData : EnvironmentComponentData<LightReflection>
{
    public TubeBloomPrePassLightWithHitPoint MainTubeBloomPrePassLight;
    public TubeBloomPrePassLightWithHitPoint[] TubeBloomPrePassLightBounces;
    public string[] EnvironmentLayerMask;

    public override void FillComponents(
        GameObject self,
        LightReflection comp,
        CreateContainer container)
    {
        comp.Repository = container.Descriptor.FloatFxGroupEffectManager.gameObject
            .GetOrAddComponent<ColliderRepository>();

        comp.MainParametricLight = RegisterReflection(MainTubeBloomPrePassLight);
        comp.ParametricLightReflection = TubeBloomPrePassLightBounces.Select(RegisterReflection).ToArray();
        comp.EnvironmentLayerMask = container.Library.LayerMaskLookup[EnvironmentLayerMask[0]];
        return;

        LightReflection.ParametricLightWithHitPoint RegisterReflection(TubeBloomPrePassLightWithHitPoint c)
        {
            var parametricLight = container.GetComponentOrNull<ParametricBloomFogLightController>(c.Light);
            if (parametricLight != null)
            {
                parametricLight.GetComponent<ChromaIDMarker>().MarkUse = true;
                parametricLight.GetComponent<ChromaIDMarker>().MarkActivator = true;
            }

            var hitPointLightWithId =
                container.GetComponentOrNull<InstancedMaterialLightController>(c.HitPointLightWithId);
            if (hitPointLightWithId != null)
            {
                hitPointLightWithId.GetComponent<ChromaIDMarker>().MarkUse = true;
                hitPointLightWithId.GetComponent<ChromaIDMarker>().MarkActivator = true;
            }

            return new LightReflection.ParametricLightWithHitPoint
            {
                Light = parametricLight,
                HitPointLightWithId = hitPointLightWithId,
                HitPointGameObject = container.ChromaIdObjects[c.HitPointGameObject],
                HitPointTransform = container.GetComponentOrNull<Transform>(c.HitPointTransform),
                HitPointDistanceToAlphaCurve = c.HitPointDistanceToAlphaCurve.Create(),
                ShowHitPoint = c.ShowHitPoint
            };
        }
    }

    public class TubeBloomPrePassLightWithHitPoint
    {
        public int Light;
        public int HitPointLightWithId;
        public AnimationCurveData HitPointDistanceToAlphaCurve;
        public string HitPointGameObject;
        public int HitPointTransform;
        public bool ShowHitPoint;
    }
}
