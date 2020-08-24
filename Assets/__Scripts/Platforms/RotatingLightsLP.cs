using UnityEngine;
using System;
using SimpleJSON;

public class RotatingLightsLP : IRotatingLights
{

    [SerializeField]
    protected Vector3 _rotationVector = Vector3.up;

    [SerializeField] public float multiplier = 20;
    private float startRotationAngle = 0f;
    [SerializeField] public bool left = false;
    [SerializeField] protected RotatingLightsRandom rotatingLightsRandom;

    private float songSpeed = 1;

    private bool rotationEnabled = false;
    private float rotationSpeed = 0f;
    private float rotationAngle = 0f;
    private Quaternion startRotation;

    protected bool _useZPositionForAngleOffset = false;
    protected float _zPositionAngleOffsetScale = 1f;

    private void Start()
    {
        startRotation = gameObject.transform.localRotation;
        startRotationAngle = left ? -rotatingLightsRandom.startRotationAngle : rotatingLightsRandom.startRotationAngle;
        startRotationAngle *= 4;

        // TODO: Remove
        rotationAngle = startRotationAngle;

        transform.localRotation = startRotation * Quaternion.Euler(_rotationVector * startRotationAngle);

        rotatingLightsRandom.onSwitchStyle += SwitchStyle;
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void UpdateSongSpeed(object value)
    {
        float speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10;
    }

    private void Update()
    {
        if (!rotationEnabled) return;

        rotationAngle += Time.deltaTime * rotationSpeed * songSpeed;
        transform.localRotation = startRotation * Quaternion.Euler(_rotationVector * rotationAngle);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("SongSpeed");
    }

    public void SwitchStyle()
    {
        rotationAngle = rotatingLightsRandom._randomStartRotation + startRotationAngle;
        rotationSpeed = Mathf.Abs(rotationSpeed);

        if (!left)
        {
            rotationAngle = 0f - rotationAngle;
            rotationSpeed = 0f - rotationSpeed;
        }
    }

    public override void UpdateOffset(int Speed, float Rotation, bool RotateForwards, JSONNode customData = null)
    {
        rotatingLightsRandom.RandomUpdate(left);
        UpdateRotationData(Speed, rotatingLightsRandom._randomStartRotation, rotatingLightsRandom._randomDirection);
    }

    public void UpdateRotationData(int beatmapEventDataValue, float startRotationOffset, float direction)
    {
        Debug.Log($"UpdateRotationData {beatmapEventDataValue}");
        if (beatmapEventDataValue == 0)
        {
            rotationEnabled = false;
            transform.localRotation = startRotation * Quaternion.Euler(_rotationVector * startRotationAngle);
        }
        else if (beatmapEventDataValue > 0)
        {
            rotationEnabled = true;
            rotationAngle = startRotationOffset + startRotationAngle;
            transform.localRotation = startRotation * Quaternion.Euler(_rotationVector * rotationAngle);
            rotationSpeed = beatmapEventDataValue * 20f * direction;
        }
    }

    public override bool IsOverrideLightGroup()
    {
        return false;
    }
}
