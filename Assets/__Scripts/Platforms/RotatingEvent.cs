using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RotatingEventData
{
    private static readonly Func<float, float>[] easingFunctions =
    {
        NoneTransition,
        Easing.Linear,
        Easing.Quadratic.In,
        Easing.Quadratic.Out,
        Easing.Quadratic.InOut,
    };
    private float rotatingTime;
    private float timeToTransition;
    private float currentDegree = 0;
    private float targetDegree = 0;
    private int loop;
    private int direction;
    internal bool flip;
    private Func<float, float> currentEasingFn = easingFunctions[0];
    private int currentNoteIdx = -1;

    private Func<float, float> GetEasingFunction(int i)
    {
        if (i < 0 || i >= easingFunctions.Length) i = 0;
        return easingFunctions[i];
    }

    public void SetEaseFunction(int type)
    {
        currentEasingFn = GetEasingFunction(type + 1);
    }

    public void UpdateRotation(float rotation, float timeToTransition)
    {
        rotatingTime = 0;
        targetDegree = rotation;
        this.timeToTransition = timeToTransition;
        if (timeToTransition == 0) currentDegree = rotation;
    }

    public void SetLoop(int loop) => this.loop = loop;

    public void SetDirection(int direction) => this.direction = direction;

    private static float NoneTransition(float _) => 0;

    public void SetCurrentTimeRatio(float t) => rotatingTime = t * timeToTransition;
    public bool SetNoteIndex(int noteIdx, bool force = false)
    {
        if (!force && noteIdx < currentNoteIdx)
        {
            return false;
        }
        currentNoteIdx = noteIdx;
        return true;
    }

    private float LerpAngleWithDirection(float a, float b, float t)
    {
        t = Mathf.Clamp01(t);
        if (direction == 0) return Mathf.LerpAngle(a, b + 360 * loop, t);
        if (direction == 1) // CW
        {
            if (a > b)
            {
                int diff = Mathf.CeilToInt((a - b) / 360);
                a -= diff * 360;
            }
            b += loop * 360;
        }
        else // CCW
        {
            if (a < b)
            {
                int diff = Mathf.CeilToInt((b - a) / 360); 
                a += diff * 360;
            }
            b -= loop * 360;
        }
        return (1 - t) * a + t * b;
    }

    public float LerpAngle(float deltaTime)
    {
        rotatingTime += deltaTime;
        var angle = LerpAngleWithDirection(currentDegree, targetDegree, currentEasingFn(rotatingTime / timeToTransition));
        if (flip)
        {
            angle = -angle;
        }
        return angle;
    }
}


/// <summary>
/// Although there are <see cref="TrackLaneRing"/> and <see cref="RotatingLights"/>, they could not perfectly meet v3 rotation needs.
/// </summary>
public class RotatingEvent : MonoBehaviour
{

    internal LightsManagerV3 lightsManager;
    public int RotationIdx;
    public RotatingEventData XData = new RotatingEventData();
    public RotatingEventData YData = new RotatingEventData();

    protected void Update()
    {
        if (lightsManager == null) return;
        var dt = Time.deltaTime;
        if (lightsManager.ZRotatable) // for rings
        {
            transform.localEulerAngles = new Vector3(
                0,
                0,
                XData.LerpAngle(dt)
                );
        }
        else // for light beams
        {
            transform.localEulerAngles = new Vector3(
                XData.LerpAngle(dt),
                YData.LerpAngle(dt),
                0
                );
        }

    }

    public RotatingEventData GetAxisData(int axis)
    {
        return axis == 0 ? XData : YData;
    }
    public void ResetNoteIndex()
    {
        XData.SetNoteIndex(-1, true);
        YData.SetNoteIndex(-1, true);
    }
}

