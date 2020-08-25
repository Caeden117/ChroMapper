using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RotatingLightsRandom : MonoBehaviour {

    [SerializeField] public float startRotationAngle = 0f;

    protected bool _useZPositionForAngleOffset = false;
    protected float _zPositionAngleOffsetScale = 1f;

    protected bool _overrideRandomValues;
    protected int _randomGenerationFrameNum = -1;
    internal float _rotationSpeed;
    internal float _randomStartRotation;
    internal float _randomDirection;

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
                _randomDirection = (leftEvent ? 1f : (-1f));
                _randomStartRotation = (leftEvent ? frameCount : (-frameCount));
                if (_useZPositionForAngleOffset)
                {
                    _randomStartRotation += transform.position.z * _zPositionAngleOffsetScale;
                }
            }
            else
            {
                _randomDirection = ((Random.value > 0.5f) ? 1f : (-1f));
                _randomStartRotation = Random.Range(0f, 360f);
            }
            _randomGenerationFrameNum = Time.frameCount;
        }
    }
}
