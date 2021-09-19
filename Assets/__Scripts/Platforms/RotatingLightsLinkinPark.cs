using System;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class RotatingLightsLinkinPark : RotatingLightsBase
{
    [FormerlySerializedAs("_rotationVector")] [SerializeField] protected Vector3 RotationVector = Vector3.up;

    [FormerlySerializedAs("multiplier")] public float Multiplier = 20;
    public bool Left;
    [FormerlySerializedAs("rotatingLightsRandom")] [SerializeField] protected RotatingLightsRandom RotatingLightsRandom;

    protected bool UseZPositionForAngleOffset = false;
    protected float ZPositionAngleOffsetScale = 1f;
    private float rotationAngle;

    private bool rotationEnabled;
    private float rotationSpeed;

    private float songSpeed = 1;
    private Quaternion startRotation;
    private float startRotationAngle;

    private void Start()
    {
        startRotation = gameObject.transform.localRotation;
        startRotationAngle = Left ? -RotatingLightsRandom.StartRotationAngle : RotatingLightsRandom.StartRotationAngle;
        startRotationAngle *= 4;

        // TODO: Remove
        rotationAngle = startRotationAngle;

        transform.localRotation = startRotation * Quaternion.Euler(RotationVector * startRotationAngle);

        RotatingLightsRandom.ONSwitchStyle += SwitchStyle;
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void Update()
    {
        if (!rotationEnabled) return;

        rotationAngle += Time.deltaTime * rotationSpeed * songSpeed;
        transform.localRotation = startRotation * Quaternion.Euler(RotationVector * rotationAngle);
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("SongSpeed");

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10;
    }

    public void SwitchStyle()
    {
        rotationAngle = RotatingLightsRandom.randomStartRotation;
        rotationSpeed = Mathf.Abs(RotatingLightsRandom.rotationSpeed);

        if (!Left)
        {
            rotationAngle = 0f - rotationAngle;
            rotationSpeed = 0f - rotationSpeed;
        }

        rotationAngle += startRotationAngle;
    }

    public override void UpdateOffset(bool isLeftEvent, int speed, float rotation, bool rotateForwards,
        JSONNode customData = null)
    {
        RotatingLightsRandom.RandomUpdate(Left);
        if (Left)
            UpdateRotationData(speed, RotatingLightsRandom.randomStartRotation, RotatingLightsRandom.randomDirection);
        else
        {
            UpdateRotationData(speed, 0f - RotatingLightsRandom.randomStartRotation,
                0f - RotatingLightsRandom.randomDirection);
        }
    }

    public void UpdateRotationData(int beatmapEventDataValue, float startRotationOffset, float direction)
    {
        if (beatmapEventDataValue == 0)
        {
            rotationEnabled = false;
            transform.localRotation = startRotation * Quaternion.Euler(RotationVector * startRotationAngle);
        }
        else if (beatmapEventDataValue > 0)
        {
            rotationEnabled = true;
            rotationAngle = startRotationOffset + startRotationAngle;
            transform.localRotation = startRotation * Quaternion.Euler(RotationVector * rotationAngle);
            rotationSpeed = beatmapEventDataValue * 20f * direction;
            if (Left) RotatingLightsRandom.rotationSpeed = rotationSpeed;
        }
    }

    public override void UpdateZPosition()
    {
    }

    public override bool IsOverrideLightGroup() => false;
}
