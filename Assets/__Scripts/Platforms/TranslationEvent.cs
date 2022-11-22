using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationEventData
{
    internal bool flip;
    private Func<float, float> currentEasingFn = RotatingEventData.easingFunctions[0];
    private int currentNoteIdx = -1;

    private float translationTime;
    private float timeToTransition;
    private float currentPosition = 0;
    private float targetPosition = 0;

    private Func<float, float> GetEasingFunction(int i)
    {
        if (i < 0 || i >= RotatingEventData.easingFunctions.Length) i = 0;
        return RotatingEventData.easingFunctions[i];
    }

    public void SetEaseFunction(int type)
    {
        currentEasingFn = GetEasingFunction(type + 1);
    }

    public void UpdateTranslation(float position, float timeToTransition)
    {
        translationTime = 0;
        targetPosition = position;
        this.timeToTransition = timeToTransition;
        if (timeToTransition == 0) currentPosition = position;
    }

    public void SetCurrentTimeRatio(float t) => translationTime = t * timeToTransition;

    private float LerpPosition(float a, float b, float t)
    {
        t = Mathf.Clamp01(t);
        return (1 - t) * a + t * b;
    }

    public float LerpPosition(float deltaTime)
    {
        translationTime += deltaTime;
        var position = LerpPosition(currentPosition, targetPosition, currentEasingFn(translationTime / timeToTransition));
        if (flip)
        {
            position = -position;
        }
        return position;
    }

    public bool SetNoteIndex(int noteIdx, bool force = false)
    {
        if (!force && noteIdx < currentNoteIdx)
        {
            return false;
        }
        currentNoteIdx = noteIdx;
        return true;
    }
}


public class TranslationEvent : MonoBehaviour, ILightEventV3
{
    internal LightsManagerV3 lightsManager;
    public int TranslationIdx;
    [SerializeField] internal float translationMultiplierX = 1;
    [SerializeField] internal float translationMultiplierY = 1;
    [SerializeField] internal float translationMultiplierZ = 1;
    public TranslationEventData XData = new TranslationEventData();
    public TranslationEventData YData = new TranslationEventData();
    public TranslationEventData ZData = new TranslationEventData();

    protected void Update()
    {
        if (lightsManager == null) return;
        var dt = Time.deltaTime;
        transform.localPosition = new Vector3(
            XData.LerpPosition(dt) * translationMultiplierX,
            YData.LerpPosition(dt) * translationMultiplierY,
            ZData.LerpPosition(dt) * translationMultiplierZ
            );

    }

    public TranslationEventData GetAxisData(int axis)
    {
        return axis == 0 ? XData :
            axis == 1 ? YData : ZData;
    }
    public void ResetNoteIndex()
    {
        XData.SetNoteIndex(-1, true);
        YData.SetNoteIndex(-1, true);
        ZData.SetNoteIndex(-1, true);
    }

    public int GetIndex() => TranslationIdx;
}
