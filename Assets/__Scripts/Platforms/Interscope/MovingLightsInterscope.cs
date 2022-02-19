using System;
using SimpleJSON;
using UnityEngine;

public class MovingLightsInterscope : RotatingLightsBase
{
    public bool Left;
    [SerializeField] protected MovingLightsRandom MovingLightsRandom;

    [SerializeField] protected Vector3 StartPositionOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField] protected Vector3 EndPositionOffset = new Vector3(0.0f, 2f, 0.0f);

    private bool movementEnabled;
    private float movementSpeed;
    private float movementValue;

    private float songSpeed = 1;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.localPosition;
        movementValue = MovingLightsRandom.StartOffset;

        var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset,
            (float)((Mathf.Sin(MovingLightsRandom.StartOffset) * 0.5) + 0.5));
        vector3.x *= Left ? 1f : -1f;
        transform.localPosition = startPosition + vector3;

        MovingLightsRandom.ONSwitchStyle += SwitchStyle;
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void Update()
    {
        if (!movementEnabled) return;

        movementValue += Time.deltaTime * movementSpeed * songSpeed;
        var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset,
            (float)((Mathf.Sin(movementValue) * 0.5) + 0.5));
        vector3.x *= Left ? 1f : -1f;
        transform.localPosition = startPosition + vector3;
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("SongSpeed");

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10;
    }

    public void SwitchStyle()
    {
        movementValue = MovingLightsRandom.randomStartOffset;
        movementSpeed = Mathf.Abs(MovingLightsRandom.movementSpeed);

        movementValue += MovingLightsRandom.StartOffset;
    }

    public override void UpdateOffset(bool isLeft, int speed, float rotation, bool rotateForwards,
        JSONNode customData = null)
    {
        MovingLightsRandom.RandomUpdate(Left);
        UpdateRotationData(speed, MovingLightsRandom.randomStartOffset);
    }

    private void UpdateRotationData(int beatmapEventDataValue, float startRotationOffset)
    {
        if (beatmapEventDataValue == 0)
        {
            movementEnabled = false;

            var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset,
                (float)((Mathf.Sin(MovingLightsRandom.StartOffset) * 0.5) + 0.5));
            vector3.x *= Left ? 1f : -1f;
            transform.localPosition = startPosition + vector3;
        }
        else if (beatmapEventDataValue > 0)
        {
            movementEnabled = true;
            movementValue = startRotationOffset + MovingLightsRandom.StartOffset;

            var vector3 = Vector3.LerpUnclamped(StartPositionOffset, EndPositionOffset,
                (float)((Mathf.Sin(movementValue) * 0.5) + 0.5));
            vector3.x *= Left ? 1f : -1f;
            transform.localPosition = startPosition + vector3;

            movementSpeed = beatmapEventDataValue;
            if (Left) MovingLightsRandom.movementSpeed = movementSpeed;
        }
    }

    public override void UpdateZPosition()
    {
    }

    public override bool IsOverrideLightGroup() => false;
}
