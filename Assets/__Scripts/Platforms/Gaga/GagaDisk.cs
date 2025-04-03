using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GagaDisk : MonoBehaviour
{
    // Based off of the TrackLaneRings system.
    public int HeightEventType = 0;
    private GagaArc arc;
    private Vector3 basePosition;
    private float prevPosY;
    private float destPosY;
    private float posY;
    private float startTime;
    private float destTime;
    
    public void Reset()
    {
        prevPosY = 0;
        destPosY = 0;
        posY = 0;
        destTime = 0;
        startTime = 0;
    }

    public void Init(float positionY)
    {
        arc = gameObject.transform.GetComponentInChildren<GagaArc>();
        
        posY = destPosY = prevPosY = positionY;
        basePosition = gameObject.transform.position;
    }

    public void LateUpdateDisk(float jsonTime)
    {
        var easedFactor = Easing.Cubic.InOut(LerpTime(startTime, destTime, jsonTime));
        transform.position = new Vector3(basePosition.x,
            Mathf.Lerp(prevPosY, destPosY, easedFactor),
            basePosition.z);
    }

    private float LerpTime(float timeStart, float targetTime, float x)
    {
        var e = Mathf.Clamp01((x - timeStart) / (targetTime - timeStart));
        return !float.IsNaN(e) ? e : 0;
    }
    
    public void SetPosition(int startValue, int destinationValue, float timeStart, float timeDest)
    { 
        prevPosY = GetPositionForValue(startValue);
        destPosY = GetPositionForValue(destinationValue);
        startTime = timeStart;
        destTime = timeDest;
    }
    
    private int GetPositionForValue(int value) => (value * 6) - 4;
}
