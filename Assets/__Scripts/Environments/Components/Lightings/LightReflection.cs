using System;
using UnityEngine;

public class LightReflection : MonoBehaviour
{
    public ParametricLightWithHitPoint MainParametricLight;
    public ParametricLightWithHitPoint[] ParametricLightReflection;

    public LayerMask EnvironmentLayerMask;
    public ColliderRepository Repository;

    private Transform tr;

    private void Awake()
    {
        var parametricLightBounces = ParametricLightReflection;
        for (var i = 0; i < parametricLightBounces.Length; i++) parametricLightBounces[i].Light.UseCollision = true;
        tr = transform;
    }

    private void LateUpdate()
    {
        if (!MainParametricLight.Light.EnabledRenderers) return;
        var len = MainParametricLight.Light.Length;
        var flag = RaycastAndSetLightLength(
            MainParametricLight,
            tr.position,
            tr.up,
            out var hitWorldPosition,
            out var hitReflection,
            out var length,
            out var endAlpha);
        var lightBounces = ParametricLightReflection;
        foreach (var lightWithHitPoint in lightBounces)
        {
            lightWithHitPoint.SetActive(flag, MainParametricLight.Light.Color);
            if (!flag) continue;
            len -= length;
            lightWithHitPoint.SetData(len, endAlpha, hitWorldPosition, hitReflection);
            flag = RaycastAndSetLightLength(
                lightWithHitPoint,
                hitWorldPosition,
                hitReflection,
                out hitWorldPosition,
                out hitReflection,
                out length,
                out endAlpha);
        }
    }

    private bool RaycastAndSetLightLength(
        ParametricLightWithHitPoint bounce,
        Vector3 rayWorldOrigin,
        Vector3 rayDirection,
        out Vector3 hitWorldPosition,
        out Vector3 hitReflection,
        out float length,
        out float endAlpha)
    {
        var flag = Physics.Raycast(
            new Ray(rayWorldOrigin, rayDirection),
            out var hitInfo,
            bounce.Light.Length,
            EnvironmentLayerMask);
        bounce.SetCollisionLength(flag, hitInfo);
        hitWorldPosition = hitInfo.point;
        hitReflection = Vector3.Reflect(rayDirection, hitInfo.normal);
        length = hitInfo.distance;
        endAlpha = bounce.Light.CollisionEndAlpha;
        if (flag && Repository.TryGet(hitInfo.collider, out var effect)) return effect.Value > 0f;

        return false;
    }

    [Serializable]
    public class ParametricLightWithHitPoint
    {
        public ParametricBloomFogLightController Light;

        public bool ShowHitPoint;
        public GameObject HitPointGameObject;
        public Transform HitPointTransform;
        public InstancedMaterialLightController HitPointLightWithId;
        public AnimationCurve HitPointDistanceToAlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private bool hasHit;

        public void SetCollisionLength(bool flag, RaycastHit hit)
        {
            if (ShowHitPoint && hasHit != flag)
            {
                hasHit = flag;
                HitPointGameObject.SetActive(hasHit);
            }

            Light.UseCollision = flag;
            if (!flag)
            {
                Light.CollisionLength = float.MaxValue;
                return;
            }

            Light.CollisionLength = hit.distance;
            if (!ShowHitPoint) return;
            HitPointTransform.SetPositionAndRotation(
                hit.point,
                Quaternion.FromToRotation(HitPointTransform.forward, hit.normal) * HitPointTransform.rotation);
            HitPointLightWithId.Intensity =
                HitPointDistanceToAlphaCurve.Evaluate(Mathf.InverseLerp(0f, Light.Length, hit.distance));
        }

        public void SetActive(bool enabled, Color color)
        {
            if (enabled == Light.gameObject.activeSelf) return;
            Light.Color = color;
            Light.gameObject.SetActive(enabled);
        }

        public void SetData(float remainingLength, float startAlpha, Vector3 hitWorldPosition, Vector3 hitReflection)
        {
            Light.Length = remainingLength;
            Light.StartAlpha = startAlpha;
            Light.transform.SetPositionAndRotation(
                hitWorldPosition,
                Quaternion.FromToRotation(Vector3.up, hitReflection));
        }
    }
}
