using UnityEngine;
using System;
using SimpleJSON;

public class RotatingLightsLP : RotatingLightsBase
{

    [SerializeField]
    protected Vector3 _rotationVector = Vector3.up;

    [SerializeField] public float multiplier = 20;
    private float startRotationAngle = 0f;
    [SerializeField] public bool Left = false;
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
        startRotationAngle = Left ? -rotatingLightsRandom.startRotationAngle : rotatingLightsRandom.startRotationAngle;
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
        rotationAngle = rotatingLightsRandom._randomStartRotation;
        rotationSpeed = Mathf.Abs(rotatingLightsRandom._rotationSpeed);

        if (!Left)
        {
            rotationAngle = 0f - rotationAngle;
            rotationSpeed = 0f - rotationSpeed;
        }

        rotationAngle += startRotationAngle;
    }

    public override void UpdateOffset(bool isLeftEvent, int Speed, float Rotation, bool RotateForwards, JSONNode customData = null)
    {
        rotatingLightsRandom.RandomUpdate(Left);
        if (Left)
        {
            UpdateRotationData(Speed, rotatingLightsRandom._randomStartRotation, rotatingLightsRandom._randomDirection);
        }
        else
        {
            UpdateRotationData(Speed, 0f - rotatingLightsRandom._randomStartRotation, 0f - rotatingLightsRandom._randomDirection);
        }
    }

    public void UpdateRotationData(int beatmapEventDataValue, float startRotationOffset, float direction)
    {
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
            if (Left)
            {
                rotatingLightsRandom._rotationSpeed = rotationSpeed;
            }
        }
    }

    public override bool IsOverrideLightGroup()
    {
        return false;
    }
}
