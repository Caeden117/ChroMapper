﻿using UnityEngine;
using SimpleJSON;

public class RotatingLights : MonoBehaviour {

    private float speed;
    private Vector3 rotationVector = Vector3.up;

    [SerializeField] public float multiplier = 20;
    [SerializeField] private float rotationSpeed = 0;
    [SerializeField] private float zPositionModifier = 1;
    private Quaternion startRotation;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;
    public bool UseZPositionForAngleOffset = false;

    private void Start()
    {
        startRotation = transform.localRotation;
        if (OverrideLightGroup)
        {
            PlatformDescriptor descriptor = GetComponentInParent<PlatformDescriptor>();
            descriptor?.LightingManagers[OverrideLightGroupID].RotatingLights.Add(this);
        }
    }

    private void Update()
    {
        transform.Rotate(rotationVector, Time.deltaTime * rotationSpeed, Space.Self);
    }

    // If you have any complaints about CM's inaccurate lasers, please look through this and tell me what the hell is wrong.
    public void UpdateOffset(int Speed, float Rotation, bool RotateForwards, JSONNode customData = null)
    {
        speed = Speed;
        bool lockRotation = false;
        if (customData != null) //We have custom data in this event
        {
            //Apply some chroma precision values
            if (customData.HasKey("_lockPosition")) lockRotation = customData["_lockPosition"];
            if (customData.HasKey("_preciseSpeed") && Speed > 0) speed = customData["_preciseSpeed"];
            if (customData.HasKey("_direction")) RotateForwards = customData["_direction"].AsInt.Equals(0);
        }
        if (!lockRotation) //If we are not locking rotation, reset it to its default.
        {
            transform.localRotation = startRotation;
        }
        if (UseZPositionForAngleOffset && !lockRotation) //Timbaland has laser speeds offset by their Z position
        {
            Rotation = Time.frameCount + (transform.position.z * zPositionModifier);
        }
        //Rotate by Rotation variable
        //In most cases, it is randomized, except in Timbaland environment (see above)
        if (!lockRotation && (speed > 0 || (customData?.HasKey("_preciseSpeed") ?? false && customData["_preciseSpeed"] >= 0)))
        {
            transform.Rotate(rotationVector, Rotation, Space.Self);
        }
        rotationSpeed = speed * multiplier * (RotateForwards ? 1 : -1); //Set rotation speed
    }
}
