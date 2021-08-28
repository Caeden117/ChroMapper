using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MovingLightsRandom : MonoBehaviour
{
    [FormerlySerializedAs("startOffset")] public float StartOffset;
    internal float MovementSpeed;

    protected bool OverrideRandomValues;
    protected int RandomGenerationFrameNum = -1;
    internal float RandomStartOffset;

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
                RandomStartOffset = 0f;
            else
                RandomStartOffset = Random.Range(0.0f, 2 * (float)Math.PI);
            RandomGenerationFrameNum = Time.frameCount;
        }
    }
}
