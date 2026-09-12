using UnityEngine;

public class TrackLaneRing : MonoBehaviour
{
    [SerializeField] public Vector3 PositionOffset;
    [SerializeField] public TrackLaneRingsManager ParentManager;

    private float previousPosZ;
    public float PositionZ;
    private float destinationPosZ;
    private float moveSpeed;

    public Transform CachedTransform;

    public void Start() => CachedTransform = transform;

    public void Init(Vector3 pos, Vector3 posOffset)
    {
        CachedTransform = transform; // don't ask why twice
        PositionOffset = posOffset;
        CachedTransform.localPosition = pos + PositionOffset;
        previousPosZ = PositionZ = pos.z + PositionOffset.z;
    }

    public void FixedUpdateRing(float fixedDeltaTime)
    {
        previousPosZ = PositionZ;
        PositionZ = Mathf.Lerp(PositionZ, PositionOffset.z + destinationPosZ, fixedDeltaTime * moveSpeed);
    }

    public void LateUpdateRing(float interpolationFactor)
    {
        CachedTransform.localPosition = new Vector3(
            PositionOffset.x,
            PositionOffset.y,
            previousPosZ + ((PositionZ - previousPosZ) * interpolationFactor));
    }

    // EnvironmentEnhancementWithZeroPositionAnimateTrackKeepsDefaultEnvironmentBigRingsVisibleAtWorldOrigin
    // moves the ring's base while preserving both its current visible wave and subsequent native Z interpolation.
    public void RebasePositionOffset(Vector3 positionOffset)
    {
        var waveOffset = CachedTransform.localPosition - PositionOffset;
        var positionZDelta = positionOffset.z - PositionOffset.z;
        PositionOffset = positionOffset;
        previousPosZ += positionZDelta;
        PositionZ += positionZDelta;
        CachedTransform.localPosition = PositionOffset + waveOffset;
    }

    // Beat Saber initializes ring rotation state to zero; rotation snapshots own all
    // later rotation state, so no duplicate live destination fields are retained here.
    public float GetRotation() => 0f;

    public void SetPosition(float destinationZ, float moveSpeed)
    {
        destinationPosZ = destinationZ;
        this.moveSpeed = moveSpeed;
    }
}
