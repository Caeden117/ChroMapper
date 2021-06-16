using UnityEngine;
using System;
using SimpleJSON;

public class MovingLightsIS : RotatingLightsBase
{
    [SerializeField] public bool Left = false;
    [SerializeField] protected MovingLightsRandom MovingLightsRandom;

    [SerializeField] protected Vector3 StartPositionOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField] protected Vector3 EndPositionOffset = new Vector3(0.0f, 2f, 0.0f);

    private float _songSpeed = 1;

    private bool _movementEnabled = false;
    private float _movementSpeed = 0f;
    private Vector3 _startPosition;
    private float _movementValue = 0f;

    private void Start()
    {
        _startPosition = transform.localPosition;
        _movementValue = MovingLightsRandom.startOffset;

        var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset, (float) (Mathf.Sin(MovingLightsRandom.startOffset) * 0.5 + 0.5));
        vector3.x *= Left ? 1f : -1f;
        transform.localPosition = _startPosition + vector3;

        MovingLightsRandom.onSwitchStyle += SwitchStyle;
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void UpdateSongSpeed(object value)
    {
        float speedValue = (float)Convert.ChangeType(value, typeof(float));
        _songSpeed = speedValue / 10;
    }

    private void Update()
    {
        if (!_movementEnabled) return;

        _movementValue += Time.deltaTime * _movementSpeed * _songSpeed;
        var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset, (float) (Mathf.Sin(_movementValue) * 0.5 + 0.5));
        vector3.x *= Left ? 1f : -1f;
        transform.localPosition = _startPosition + vector3;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("SongSpeed");
    }

    public void SwitchStyle()
    {
        _movementValue = MovingLightsRandom._randomStartOffset;
        _movementSpeed = Mathf.Abs(MovingLightsRandom._movementSpeed);

        _movementValue += MovingLightsRandom.startOffset;
    }

    public override void UpdateOffset(bool isLeft, int Speed, float Rotation, bool RotateForwards, JSONNode customData = null)
    {
        MovingLightsRandom.RandomUpdate(Left);
        UpdateRotationData(Speed, MovingLightsRandom._randomStartOffset);
    }

    private void UpdateRotationData(int beatmapEventDataValue, float startRotationOffset)
    {
        if (beatmapEventDataValue == 0)
        {
            _movementEnabled = false;

            var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset, (float) (Mathf.Sin(MovingLightsRandom.startOffset) * 0.5 + 0.5));
            vector3.x *= Left ? 1f : -1f;
            transform.localPosition = _startPosition + vector3;
        }
        else if (beatmapEventDataValue > 0)
        {
            _movementEnabled = true;
            _movementValue = startRotationOffset + MovingLightsRandom.startOffset;

            var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset, (float) (Mathf.Sin(_movementValue) * 0.5 + 0.5));
            vector3.x *= Left ? 1f : -1f;
            transform.localPosition = _startPosition + vector3;

            _movementSpeed = beatmapEventDataValue;
            if (Left)
            {
                MovingLightsRandom._movementSpeed = _movementSpeed;
            }
        }
    }

    public override bool IsOverrideLightGroup()
    {
        return false;
    }
}
