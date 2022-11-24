using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightEventDistributionEnumerator
{
    public float Value;
    private int stepCount;
    private int currentStep;
    private int easeIdx;
    private Func<float, float> easeFn;

    public LightEventDistributionEnumerator()
    {

    }

    public LightEventDistributionEnumerator(int stop, float stopValue, int easeType = 0)
    {
        Reset(stop, stopValue, easeType);
    }

    public void Reset<T>(IEnumerable<T> filteredLightChunks, int distributionType, float value, int easeType = 0)
    {
        var cnt = BeatmapLightEventFilter.Intervals(filteredLightChunks);
        if (distributionType == 2)
        {
            cnt += 1; // it's really strange to have an additional light as end..
            value *= cnt;
        }
        Reset(cnt, value, easeType);
    }

    public void Reset(int stop, float stopValue, int easeType = 0)
    {
        easeIdx = easeType + 1;
        easeFn = RotatingEventData.easingFunctions[easeIdx];
        stepCount = stop;
        Value = stopValue;
        currentStep = 0;
    }

    public float Next()
    {
        if (currentStep > stepCount)
        {
            Debug.LogError($"Distribution enumerator is set incorrectly. Accessing {currentStep} while max is {stepCount}.");
        }
        float ret = Mathf.Lerp(0, Value, easeFn((float)currentStep / stepCount));
        currentStep++;
        return ret;
    }

    public LightEventDistributionEnumerator Copy()
    {
        return new LightEventDistributionEnumerator(stepCount, Value, easeIdx - 1);
    }

    public void ResetStep() => currentStep = 0;
}
