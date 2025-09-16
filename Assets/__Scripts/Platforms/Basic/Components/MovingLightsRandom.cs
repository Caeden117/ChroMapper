using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MovingLightsRandom : MonoBehaviour
{
    [FormerlySerializedAs("startOffset")] public float StartOffset;
    internal float movementSpeed;

    protected bool OverrideRandomValues;
    protected int RandomGenerationFrameNum = -1;
    internal float randomStartOffset;

    protected bool UseZPositionForAngleOffset = false;
    protected float ZPositionAngleOffsetScale = 1f;

    public Action OnSwitchStyle;

    public void SwitchStyle()
    {
        OverrideRandomValues = !OverrideRandomValues;
        RandomUpdate(false);
        OnSwitchStyle.Invoke();
    }

    public void RandomUpdate(bool leftEvent)
    {
        var frameCount = Time.frameCount;
        if (RandomGenerationFrameNum != frameCount)
        {
            if (OverrideRandomValues)
                randomStartOffset = 0f;
            else
                randomStartOffset = Random.Range(0.0f, 2 * (float)Math.PI);
            RandomGenerationFrameNum = Time.frameCount;
        }
    }
}
