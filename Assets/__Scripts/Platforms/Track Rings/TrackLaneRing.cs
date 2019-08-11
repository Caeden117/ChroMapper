using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackLaneRing : MonoBehaviour
{
    private Vector3 positionOffset;
    private float rotZ;
    private float destRotZ;
    private float rotateSpeed;
    private float moveSpeed;
    private float destPosZ;

    public void Init(Vector3 pos, Vector3 posOffset)
    {
        positionOffset = posOffset;
        transform.localPosition = pos + positionOffset;
        rotZ = destRotZ = transform.localPosition.z;
    }

    public void UpdateRing(float deltaTime)
    {
        rotZ = Mathf.Lerp(rotZ, destRotZ, deltaTime * rotateSpeed);
        float z = Mathf.Lerp(transform.localPosition.z, positionOffset.z + destPosZ, deltaTime * moveSpeed);
        transform.localEulerAngles = new Vector3(0, 0, rotZ);
        transform.localPosition = new Vector3(positionOffset.x, positionOffset.y, z);
    }

    public void SetRotation(float destinationZ, float rotateSpeed)
    {
        destRotZ = destinationZ;
        this.rotateSpeed = rotateSpeed;
    }

    public float GetRotation() { return rotZ; }
    public float GetDestinationRotation() { return destRotZ; }

    public void SetPosition(float destinationZ, float moveSpeed)
    {
        destPosZ = destinationZ;
        this.moveSpeed = moveSpeed;
    }
}
