using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class RotatingLightsRandom : MonoBehaviour
{
    [FormerlySerializedAs("startRotationAngle")] public float StartRotationAngle;

    protected bool OverrideRandomValues;
    internal float RandomDirection;
    protected int RandomGenerationFrameNum = -1;
    internal float RandomStartRotation;
    internal float RotationSpeed;

    protected bool UseZPositionForAngleOffset = false;
    protected float ZPositionAngleOffsetScale = 1f;

    public Action ONSwitchStyle;

    public void SwitchStyle()
    {
        OverrideRandomValues = !OverrideRandomValues;
        RandomUpdate(false);
        ONSwitchStyle.Invoke();
    }

    public void RandomUpdate(bool leftEvent)
    {
        var frameCount = Time.frameCount;
        if (RandomGenerationFrameNum != frameCount)
        {
            if (OverrideRandomValues)
            {
                RandomDirection = leftEvent ? 1f : -1f;
                RandomStartRotation = leftEvent ? frameCount : -frameCount;
                if (UseZPositionForAngleOffset)
                    RandomStartRotation += transform.position.z * ZPositionAngleOffsetScale;
            }
            else
            {
                RandomDirection = Random.value > 0.5f ? 1f : -1f;
                RandomStartRotation = Random.Range(0f, 360f);
            }

            RandomGenerationFrameNum = Time.frameCount;
        }
    }
}
