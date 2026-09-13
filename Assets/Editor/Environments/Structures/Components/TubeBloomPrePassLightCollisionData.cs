using UnityEngine;

public class TubeBloomPrePassLightCollisionData : EnvironmentComponentData<LightCollision>
{
    public int TubeBloomPrePassLight;
    public int HitPointLightWithId;
    public AnimationCurveData HitPointDistanceToAlphaCurve;
    public bool UseScale;
    public int ScaleTransform;
    public string HitPointGameObject;
    public int HitPointTransform;
    public string[] EnvironmentLayerMask;
    public bool ShowHitPoint;

    public override void FillComponents(
        GameObject self,
        LightCollision comp,
        CreateContainer container)
    {
        comp.ParametricLight = container
            .GetComponentOrNull<ParametricBloomFogLightController>(TubeBloomPrePassLight);
        if (comp.ParametricLight != null)
        {
            comp.ParametricLight.GetComponent<ChromaIDMarker>().MarkUse = true;
            comp.ParametricLight.GetComponent<ChromaIDMarker>().MarkActivator = true;
        }

        comp.HitPointLightWithId = container.GetComponentOrNull<InstancedMaterialLightController>(HitPointLightWithId);
        if (comp.HitPointLightWithId != null)
        {
            comp.HitPointLightWithId.GetComponent<ChromaIDMarker>().MarkUse = true;
            comp.HitPointLightWithId.GetComponent<ChromaIDMarker>().MarkActivator = true;
        }

        comp.HitPointGameObject = container.GetGameObjectOrNull(HitPointGameObject, self);
        comp.HitPointTransform = container.GetComponentOrNull<Transform>(HitPointTransform);
        if (comp.HitPointGameObject != null && comp.HitPointTransform == null)
            comp.HitPointTransform = comp.HitPointGameObject.transform;
        var t = container.GetComponentOrNull<Transform>(ScaleTransform);
        comp.ScaleTransform = t != null ? t : self.transform;
        comp.EnvironmentLayerMask = container.Library.LayerMaskLookup[EnvironmentLayerMask[0]];
        comp.HitPointDistanceToAlphaCurve = HitPointDistanceToAlphaCurve.Create();
        comp.UseScale = UseScale;
        comp.ShowHitPoint = ShowHitPoint;
    }
}
