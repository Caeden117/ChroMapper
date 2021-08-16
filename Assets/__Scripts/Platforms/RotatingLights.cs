using UnityEngine;
using SimpleJSON;
using System;

public class RotatingLights : RotatingLightsBase
{

    private float speed;
    private Vector3 rotationVector = Vector3.up;

    [SerializeField] public float multiplier = 20;
    [SerializeField] private float rotationSpeed = 0;
    [SerializeField] private float zPositionModifier = 0;
    [SerializeField] private float zRotationModifier = 1;
    private Quaternion startRotation;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;
    public bool UseZPositionForAngleOffset = false;
    private float zPositionOffset = 0;

    private float songSpeed = 1;

    private void Start()
    {
        startRotation = transform.localRotation;
        if (OverrideLightGroup)
        {
            PlatformDescriptor descriptor = GetComponentInParent<PlatformDescriptor>();
            descriptor?.LightingManagers[OverrideLightGroupID].RotatingLights.Add(this);
        }
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void UpdateSongSpeed(object value)
    {
        float speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10;
    }

    private void Update()
    {
        transform.Rotate(rotationVector, Time.deltaTime * rotationSpeed * songSpeed, Space.Self);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("SongSpeed");
    }

    // If you have any complaints about CM's inaccurate lasers, please look through this and tell me what the hell is wrong.
    public override void UpdateOffset(bool isLeftEvent, int Speed, float Rotation, bool RotateForwards, JSONNode customData = null)
    {
        speed = Speed;
        bool lockRotation = false;
        if (customData != null) //We have custom data in this event
        {
            //Apply some chroma precision values
            if (customData.HasKey("_lockPosition")) lockRotation = customData["_lockPosition"];

            if (Speed > 0)
            {
                if (customData.HasKey("_preciseSpeed"))
                {
                    speed = customData["_preciseSpeed"];
                }
                else if (customData.HasKey("_speed"))
                {
                    speed = customData["_speed"];
                }
            }
            if (customData.HasKey("_direction"))
            {
                RotateForwards = customData["_direction"].AsInt.Equals(0) ^ isLeftEvent;
            }
        }
        if (!lockRotation) //If we are not locking rotation, reset it to its default.
        {
            transform.localRotation = startRotation;
        }
        if (UseZPositionForAngleOffset && !lockRotation) //BTS, FitBeat, and Timbaland has laser speeds offset by their Z position
        {
            Rotation = (Time.frameCount + (transform.position.z * zPositionOffset)) * zRotationModifier;
        }
        //Rotate by Rotation variable
        //In most cases, it is randomized, except in certain environments (see above)
        if (!lockRotation && (speed > 0 || (customData?.HasKey("_preciseSpeed") ?? false && customData["_preciseSpeed"] >= 0)))
        {
            transform.Rotate(rotationVector, Rotation, Space.Self);
        }
        rotationSpeed = speed * multiplier * (RotateForwards ? -1 : 1); //Set rotation speed
    }

    public override void UpdateZPosition()
    {
        zPositionOffset += zPositionModifier;
    }

    public override bool IsOverrideLightGroup()
    {
        return OverrideLightGroup;
    }
}
