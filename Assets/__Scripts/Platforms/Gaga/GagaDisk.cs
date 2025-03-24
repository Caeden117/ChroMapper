using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GagaDisk : MonoBehaviour
{
    // Based off of the TrackLaneRings system.
    public int HeightEventType = 0;
    private Vector3 basePosition;
    private float prevPosY;
    private float destPosY;
    private float posY;
    private float moveSpeed;
    
    public void Reset()
    {
        prevPosY = 0;
        destPosY = 0;
        posY = 0;
        moveSpeed = 0;
    }

    public void Init(float posY)
    {
        this.posY = posY;
        this.basePosition = gameObject.transform.position;
    }

    public void FixedUpdateDisk(float fixedDeltaTime)
    {
        prevPosY = posY;
        posY = Mathf.Lerp(posY, destPosY, fixedDeltaTime * moveSpeed);
    }

    public void LateUpdateDisk(float interpolationFactor)
    {
        transform.position = new Vector3(basePosition.x,
            prevPosY + (posY - prevPosY) * interpolationFactor,
            basePosition.z);
    }
    
    public void SetPosition(float destinationY, float moveSpeed)
    {
        destPosY = destinationY;
        this.moveSpeed = moveSpeed;
    }
    
    public int GetHeightEventType() => HeightEventType != 0 ? HeightEventType : -1;
    
    public float GetPosition() => posY;
    public float GetDestinationPosition() => destPosY;
}
