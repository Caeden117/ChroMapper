using UnityEngine;

public class LightCollision : MonoBehaviour
{
    public ParametricBloomFogLightController ParametricLight;

    public LayerMask EnvironmentLayerMask;
    public bool UseScale;
    public Transform ScaleTransform;
    public bool ShowHitPoint;

    public GameObject HitPointGameObject;
    public Transform HitPointTransform;
    public InstancedMaterialLightController HitPointLightWithId;
    public AnimationCurve HitPointDistanceToAlphaCurve;

    private Transform tr;
    private bool hasHit;

    private void Start()
    {
        ParametricLight.UseCollision = true;
        tr = transform;
    }

    private void Update()
    {
        if (!ParametricLight.EnabledRenderers) return;
        var scale = 1f;
        var len = ParametricLight.Length;
        var maxDistance = len;
        if (UseScale)
        {
            var localScale = ScaleTransform.localScale;
            scale = 1f / localScale.y;
            maxDistance = len * localScale.y;
        }

        var flag = Physics.Raycast(
            new Ray(tr.position, tr.up),
            out var hitInfo,
            maxDistance,
            EnvironmentLayerMask);
        if (ShowHitPoint && hasHit != flag)
        {
            hasHit = flag;
            HitPointGameObject.SetActive(hasHit);
        }

        if (flag)
        {
            ParametricLight.CollisionLength = hitInfo.distance * scale;
            if (!ShowHitPoint) return;
            HitPointTransform.position = tr.position + (tr.up * hitInfo.distance);
            HitPointTransform.rotation = Quaternion.FromToRotation(HitPointTransform.forward, hitInfo.normal)
                * HitPointTransform.rotation;
            HitPointLightWithId.Intensity = HitPointDistanceToAlphaCurve.Evaluate(
                Mathf.InverseLerp(0f, len, hitInfo.distance * scale));
        }
        else
            ParametricLight.CollisionLength = float.MaxValue;
    }
}
