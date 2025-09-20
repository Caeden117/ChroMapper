using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class RotatingLightsManager : RotatingLightsManagerBase
{
    [FormerlySerializedAs("multiplier")] public float Multiplier = 20;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float zPositionModifier;

    public bool OverrideLightGroup;
    public int OverrideLightGroupID;
    public bool UseZPositionForAngleOffset;
    private readonly Vector3 rotationVector = Vector3.up;

    private float songSpeed = 1;

    private float speed;
    private Quaternion startRotation;
    private float zPositionOffset;

    private void Start()
    {
        startRotation = transform.localRotation;
        if (OverrideLightGroup)
        {
            var descriptor = GetComponentInParent<PlatformDescriptor>();

            if (descriptor != null) descriptor.LightingManagers[OverrideLightGroupID].RotatingLights.Add(this);
        }

        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void Update() => transform.Rotate(rotationVector, Time.deltaTime * rotationSpeed * songSpeed, Space.Self);

    private void OnDestroy() => Settings.ClearSettingNotifications("SongSpeed");

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10;
    }

    // If you have any complaints about CM's inaccurate lasers, please look through this and tell me what the hell is wrong.
    public override void UpdateOffset(bool isLeftEvent, BaseEvent evt)
    {
        var rotation = UnityEngine.Random.Range(0f, 180f);
        var rotateForwards = UnityEngine.Random.Range(0, 1) == 1;

        this.speed = evt.Value;
        var lockRotation = false;
        if (evt.CustomData != null) //We have custom data in this event
        {
            //Apply some chroma precision values

            if (evt.CustomLockRotation.HasValue) lockRotation = evt.CustomLockRotation.Value;

            if (speed > 0)
            {
                if (evt.CustomPreciseSpeed.HasValue)
                    this.speed = evt.CustomPreciseSpeed.Value;
                else if (evt.CustomSpeed.HasValue) this.speed = evt.CustomSpeed.Value;
            }

            if (evt.CustomDirection.HasValue) rotateForwards = evt.CustomDirection.Value.Equals(0) ^ isLeftEvent;
        }

        if (!lockRotation) //If we are not locking rotation, reset it to its default.
            transform.localRotation = startRotation;
        if (UseZPositionForAngleOffset
            && !lockRotation) //BTS, FitBeat, and Timbaland has laser speeds offset by their Z position
        {
            rotation = (Time.frameCount + (transform.position.z * zPositionModifier));
        }

        //Rotate by Rotation variable
        //In most cases, it is randomized, except in certain environments (see above)
        if (!lockRotation && (this.speed > 0 || (evt.CustomPreciseSpeed.HasValue) && evt.CustomPreciseSpeed.Value >= 0))
        {
            transform.Rotate(rotationVector, rotation, Space.Self);
        }

        rotationSpeed =
            this.speed * Multiplier * (rotateForwards ? -1 : 1) * Mathf.Sign(Multiplier); //Set rotation speed
    }

    public override bool IsOverrideLightGroup() => OverrideLightGroup;
}
