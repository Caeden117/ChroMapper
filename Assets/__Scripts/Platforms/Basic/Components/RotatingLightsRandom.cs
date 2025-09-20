using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class RotatingLightsRandom : MonoBehaviour
{
    [FormerlySerializedAs("startRotationAngle")] public float StartRotationAngle;

    protected bool OverrideRandomValues;
    internal float randomDirection;
    protected int RandomGenerationFrameNum = -1;
    internal float randomStartRotation;
    internal float rotationSpeed;

    protected bool UseZPositionForAngleOffset = false;
    protected float ZPositionAngleOffsetScale = 1f;

    public Action OnSwitchStyle;

    public void SwitchStyle(bool b)
    {
        OverrideRandomValues = b;
        RandomUpdate(false);
        OnSwitchStyle.Invoke();
    }

    public void RandomUpdate(bool leftEvent)
    {
        var frameCount = Time.frameCount;
        if (RandomGenerationFrameNum != frameCount)
        {
            if (OverrideRandomValues)
            {
                randomDirection = leftEvent ? 1f : -1f;
                randomStartRotation = leftEvent ? frameCount : -frameCount;
                if (UseZPositionForAngleOffset)
                    randomStartRotation += transform.position.z * ZPositionAngleOffsetScale;
            }
            else
            {
                randomDirection = Random.value > 0.5f ? 1f : -1f;
                randomStartRotation = Random.Range(0f, 360f);
            }

            RandomGenerationFrameNum = Time.frameCount;
        }
    }
}
