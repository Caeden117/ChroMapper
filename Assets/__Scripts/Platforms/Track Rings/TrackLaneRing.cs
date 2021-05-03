using UnityEngine;

public class TrackLaneRing : MonoBehaviour
{
    private Vector3 positionOffset;

    private float prevRotZ;
    private float rotZ;
    private float destRotZ;

    private float rotateSpeed;
    private float moveSpeed;

    private float prevPosZ;
    private float posZ;
    private float destPosZ;

    public void Init(Vector3 pos, Vector3 posOffset)
    {
        positionOffset = posOffset;
        transform.localPosition = pos + positionOffset;
        prevPosZ = posZ = pos.z + positionOffset.z;
        rotZ = destRotZ = transform.localPosition.z;
    }

    public void FixedUpdateRing(float fixedDeltaTime)
    {
        prevRotZ = rotZ;
        rotZ = Mathf.Lerp(rotZ, destRotZ, fixedDeltaTime * rotateSpeed);
        prevPosZ = posZ;
        posZ = Mathf.Lerp(posZ, positionOffset.z + destPosZ, fixedDeltaTime * moveSpeed);
    }

    public void LateUpdateRing(float interpolationFactor)
    {
        transform.localEulerAngles = new Vector3(0, 0, prevRotZ + (rotZ - prevRotZ) * interpolationFactor);
        transform.localPosition = new Vector3(positionOffset.x, positionOffset.y, prevPosZ + (posZ - prevPosZ) * interpolationFactor);
    }

    public void SetRotation(float destinationZ, float rotateSpeed)
    {
        destRotZ = destinationZ;
        this.rotateSpeed = rotateSpeed;
    }

    public float GetRotation() => rotZ;
    public float GetDestinationRotation() => destRotZ;

    public void SetPosition(float destinationZ, float moveSpeed)
    {
        destPosZ = destinationZ;
        this.moveSpeed = moveSpeed;
    }

    public void Reset()
    {
        rotZ = 0;
        prevRotZ = 0;
        destRotZ = 0;
        rotateSpeed = 0;
    }
}
