using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MovingLightsRandom : MonoBehaviour {

    [SerializeField] public float startOffset = 0f;

    protected bool _useZPositionForAngleOffset = false;
    protected float _zPositionAngleOffsetScale = 1f;

    protected bool _overrideRandomValues;
    protected int _randomGenerationFrameNum = -1;
    internal float _movementSpeed;
    internal float _randomStartOffset;

    public Action onSwitchStyle;

    public void SwitchStyle()
    {
        _overrideRandomValues = !_overrideRandomValues;
        RandomUpdate(false);
        onSwitchStyle.Invoke();
    }

    public void RandomUpdate(bool leftEvent)
    {
        int frameCount = Time.frameCount;
        if (_randomGenerationFrameNum != frameCount)
        {
            if (_overrideRandomValues)
            {
                _randomStartOffset = 0f;
            }
            else
            {
                _randomStartOffset = Random.Range(0.0f, 2 * (float) Math.PI);
            }
            _randomGenerationFrameNum = Time.frameCount;
        }
    }
}
