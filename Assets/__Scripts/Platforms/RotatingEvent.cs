using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Although there are <see cref="TrackLaneRing"/> and <see cref="RotatingLights"/>, they could not perfectly meet v3 rotation needs.
/// </summary>
public class RotatingEvent : MonoBehaviour
{
    private static readonly Func<float, float>[] easingFunctions =
    {
        Easing.Linear,
        Easing.Quadratic.In,
        Easing.Quadratic.Out,
        Easing.Quadratic.InOut,
    };

    private Func<float, float> currentEasingFn = easingFunctions[0];

    private float rotatingTime;
    private float timeToTransitionX;
    private float timeToTransitionY;
    private float currentXDegree;
    private float currentYDegree;
    private float targetXDegree;
    private float targetYDegree;

    internal LightsManagerV3 lightsManager;
    public int RotationIdx;

    protected void Update()
    {
        rotatingTime += Time.deltaTime;
        transform.rotation = Quaternion.Euler(
            Mathf.LerpAngle(currentYDegree, targetYDegree, currentEasingFn(rotatingTime / timeToTransitionY)),
            0,
            Mathf.LerpAngle(currentXDegree, targetXDegree, currentEasingFn(rotatingTime / timeToTransitionX))
            );
    }


    private Func<float, float> GetEasingFunction(int i)
    {
        if (i < 0 || i >= easingFunctions.Length) i = 0;
        return easingFunctions[i];
    }

    public void UpdateXRotation(float rotation, float timeToTransition)
    {
        if (lightsManager.XFlip) rotation = -rotation;
        rotatingTime = 0;
        targetXDegree = rotation;
        timeToTransitionX = timeToTransition;
        if (timeToTransition == 0) currentXDegree = rotation;
    }

    public void UpdateYRotation(float rotation, float timeToTransition)
    {
        if (lightsManager.YFlip) rotation = -rotation;
        rotatingTime = 0;
        targetYDegree = rotation;
        timeToTransitionY = timeToTransition;
        if (timeToTransition == 0) currentYDegree = rotation;
    }
}

