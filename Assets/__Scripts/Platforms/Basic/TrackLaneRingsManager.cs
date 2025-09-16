using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class TrackLaneRingsManager : TrackLaneRingsManagerBase
{
    [FormerlySerializedAs("ringCount")] public int RingCount = 10;
    [FormerlySerializedAs("prefab")] public TrackLaneRing Prefab;

    [FormerlySerializedAs("moveFirstRing")]
    public bool MoveFirstRing;

    [FormerlySerializedAs("minPositionStep")]
    public float MINPositionStep = 1;

    [FormerlySerializedAs("maxPositionStep")]
    public float MAXPositionStep = 2;

    [FormerlySerializedAs("moveSpeed")] public float MoveSpeed = 1;

    [FormerlySerializedAs("rotationStep")] [Header("Rotation")]
    public float RotationStep = 5;

    [FormerlySerializedAs("propagationSpeed")]
    public float PropagationSpeed = 1;

    [FormerlySerializedAs("flexySpeed")] public float FlexySpeed = 1;

    [FormerlySerializedAs("rotationEffect")]
    public TrackLaneRingsRotationEffect RotationEffect;

    private bool zoomed;
    public TrackLaneRing[] Rings { get; private set; }

    public void Awake()
    {
        Prefab.gameObject.SetActive(false);
        Rings = new TrackLaneRing[RingCount];
        for (var i = 0; i < Rings.Length; i++)
        {
            Rings[i] = Instantiate(Prefab, transform);
            Rings[i].gameObject.SetActive(true);
            Rings[i].gameObject.name = $"Ring {i}";
            var pos = new Vector3(0, 0, i * MAXPositionStep);
            Rings[i].Init(pos, Vector3.zero);

            if (RingCount <= 1) continue;

            var lights = Rings[i]
                .GetComponentsInChildren<LightingObject>()
                .GroupBy(x => x.OverrideLightGroup ? x.OverrideLightGroupID : -1);
            foreach (var group in lights)
            {
                foreach (var lightingEvent in @group)
                {
                    lightingEvent.PropGroup = i;
                    lightingEvent.LightID += i * @group.Count();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (var ring in Rings) ring.FixedUpdateRing(TimeHelper.FixedDeltaTime);
    }

    private void LateUpdate()
    {
        foreach (var ring in Rings) ring.LateUpdateRing(TimeHelper.InterpolationFactor);
    }

    private void OnDrawGizmosSelected()
    {
        var forward = transform.forward;
        var position = transform.position;
        var d = 0.5f;
        var num = 45f;
        Gizmos.DrawRay(position, forward);
        var a = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 180f + num, 0f) * new Vector3(0f, 0f, 1f);
        var a2 = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 180f - num, 0f) * new Vector3(0f, 0f, 1f);
        Gizmos.DrawRay(position + forward, a * d);
        Gizmos.DrawRay(position + forward, a2 * d);
    }

    protected virtual bool IsAffectedByZoom() => !Mathf.Approximately(MAXPositionStep, MINPositionStep);

    public override void HandlePositionEvent(BaseEvent evt)
    {
        var step = zoomed ? MAXPositionStep : MINPositionStep;

        if (IsAffectedByZoom() && (evt.CustomStep != null)) step = evt.CustomStep.Value;

        // Multiplying MoveSpeed by 5 since I don't want to edit 20+ environment prefabs
        var speed = evt.CustomSpeed ?? (MoveSpeed * 5);

        zoomed = !zoomed;
        for (var i = 0; i < Rings.Length; i++)
        {
            var destPosZ = (i + (MoveFirstRing ? 1 : 0)) * step;
            Rings[i].SetPosition(destPosZ, speed);
        }
    }

    public override void HandleRotationEvent(BaseEvent evt)
    {
        if (RotationEffect != null)
        {
            RotationEffect.AddRingRotationEvent(
                Rings[0].GetDestinationRotation(),
                Random.Range(0, RotationStep),
                PropagationSpeed,
                FlexySpeed,
                evt);
        }
    }

    public override Object[] GetToDestroy() => new Object[] { this, RotationEffect };
}
