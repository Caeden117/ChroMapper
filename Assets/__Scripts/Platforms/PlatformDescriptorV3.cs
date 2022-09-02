﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlatformDescriptorV3 : PlatformDescriptor 
{
    [Header("V3 Configurations")]
    [Tooltip("V3 LightsMangaers, which supports lightColorEvent/LightRotationEvent")]
    public LightsManagerV3[] LightsManagersV3;

    private Dictionary<int, int> groupIdToLaneIdx = new Dictionary<int, int>();

    private LightColorEventCallbackController lightColorEventCallback;
    private LightRotationEventCallbackController lightRotationEventCallback;

    private LightColorEventsContainer lightColorEventsContainer;
    private LightRotationEventsContainer lightRotationEventsContainer;

    protected new void Start()
    {
        base.Start();
        for (int i = 0; i < LightsManagersV3.Length; ++i)
        {
            groupIdToLaneIdx[LightsManagersV3[i].GroupId] = i;
        }

        lightColorEventCallback = GameObject.Find("Vertical Grid Callback").GetComponent<LightColorEventCallbackController>();
        if (lightColorEventCallback == null)
        {
            Debug.LogError("Unable to find callback, maybe prerequisite is not met?");
        }
        lightColorEventCallback.ObjectPassedThreshold += LightColorEventPassed;

        lightRotationEventCallback = GameObject.Find("Vertical Grid Callback").GetComponent<LightRotationEventCallbackController>();
        if (lightColorEventCallback == null)
        {
            Debug.LogError("Unable to find callback, maybe prerequisite is not met?");
        }
        lightRotationEventCallback.ObjectPassedThreshold += LightRotationEventPassed;

        lightColorEventsContainer = FindObjectOfType<LightColorEventsContainer>();
        if (lightColorEventsContainer == null)
        {
            Debug.LogError("Unable to find lightColorEventsContainer");
        }

        lightRotationEventsContainer = FindObjectOfType<LightRotationEventsContainer>();
        if (lightRotationEventsContainer == null)
        {
            Debug.LogError("Unable to find lightRotationEventsContainer");
        }

        foreach (var lighColorPlacement in FindObjectsOfType<LightColorEventPlacement>()) lighColorPlacement.platformDescriptor = this;
        foreach (var lighRotationPlacement in FindObjectsOfType<LightRotationEventPlacement>()) lighRotationPlacement.platformDescriptor = this;
    }

    protected new void OnDestroy()
    {
        lightColorEventCallback.ObjectPassedThreshold -= LightColorEventPassed;
        groupIdToLaneIdx.Clear();
        base.OnDestroy();
    }

    /// <summary>
    /// return lane index of given groupId, return -1 if not found
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public int GroupIdToLaneIndex(int groupId)
    {
        if (groupIdToLaneIdx.TryGetValue(groupId, out var idx))
        {
            return idx;
        }
        return -1;
    }

    /// <summary>
    /// return groupId of given lane index, return -1 if not found
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public int LaneIndexToGroupId(int idx)
    {
        if (idx < 0 || idx >= LightsManagersV3.Length) return -1;
        return LightsManagersV3[idx].GroupId;
    }

    public static IEnumerable<T> Partition<T>(IEnumerable<T> list, int section, int partition, bool reverse = false)
    {
        if (reverse) list = list.Reverse();
        var binSize = list.Count() / partition;
        return list.Where((x, i) => i / binSize == section);
    }

    public static IEnumerable<T> Range<T>(IEnumerable<T> list, int start, int step, bool reverse = false)
    {
        if (reverse) list = list.Reverse();
        return list.Where((x, i) => i >= start && (i - start)  % step == 0);
    }

    public static int Intervals<T>(IEnumerable<T> list)
    {
        return Mathf.Max(list.Count() - 1, 1);
    }

    public Color InferColor(int c)
    {
        var color = Color.white;
        if (c == 1) color = ColorBoost ? Colors.BlueBoostColor : Colors.BlueColor;
        else if (c == 0) color = ColorBoost ? Colors.RedBoostColor : Colors.RedColor;
        return color;
    }

    public void LightColorEventPassed(bool natural, int idx, BeatmapLightColorEvent e)
    {
        if (GroupIdToLaneIndex(e.Group) == -1) return;
        var allLights = LightsManagersV3[GroupIdToLaneIndex(e.Group)].ControllingLights;
        var eb = e.EventBoxes[0];

        var filteredLights = eb.Filter.FilterType == 1 
            ? Partition(allLights, eb.Filter.Section, eb.Filter.Partition, eb.Filter.Reverse == 1)
            : Range(allLights, eb.Filter.Partition, eb.Filter.Section, eb.Filter.Reverse == 1);
        if (filteredLights.Count() == 0) return;

        float deltaAlpha = eb.BrightnessDistribution;
        if (eb.BrightnessDistributionType == 1) deltaAlpha /= Intervals(filteredLights);
        float deltaTime = eb.Distribution;
        if (eb.DistributionType == 1) deltaTime /= Intervals(filteredLights);
        for (int i = 0; i < eb.EventDatas.Count; ++i)
        {
            var ebd = eb.EventDatas[i];
            if (i == 0 && eb.BrightnessAffectFirst == 0)
                StartCoroutine(LightColorRoutine(filteredLights, deltaTime, 0, e.Group, e.Time, idx, ebd));
            else
                StartCoroutine(LightColorRoutine(filteredLights, deltaTime, deltaAlpha, e.Group, e.Time, idx, ebd));
        }

    }

    public override void KillLights()
    {
        base.KillLights();
        StopAllCoroutines();
        foreach (var manager in LightsManagersV3)
        {
            if (manager != null)
                manager.ChangeAlpha(0, 1, manager.ControllingLights);
        }
        ResetNoteIndex();
    }

    private void ResetNoteIndex()
    {
        foreach (var lightManager in LightsManagersV3)
        {
            if (lightManager != null)
                lightManager.ResetNoteIndex();
        }
    }


    private IEnumerator LightColorRoutine(IEnumerable<LightingEvent> lights, float deltaTime, float deltaAlpha, 
        int group, float baseTime, int noteIdx, BeatmapLightColorEventData data)
    {
        var deltaSecond = Atsc.GetSecondsFromBeat(deltaTime);
        float afterSeconds = Atsc.GetSecondsFromBeat(data.AddedBeat);
        if (afterSeconds != 0.0f) yield return new WaitForSeconds(afterSeconds);
        var color = InferColor(data.Color);
        color = color.Multiply(LightsManager.HDRIntensity);
        var brightness = data.Brightness;
        float extraTime = 0;
        foreach (var light in lights)
        {
            if (!light.SetNoteIndex(noteIdx)) continue;
            if (data.TransitionType != 2)
            {
                light.UpdateTargetColor(color, 0);
                light.UpdateTargetAlpha(brightness, 0);
            }
            if (lightColorEventsContainer.TryGetNextLightColorEventData(group, light.LightIdx, baseTime + extraTime + data.Time, out var nextData))
            {
                if (nextData.TransitionType == 1)
                {
                    var nextColor = InferColor(nextData.Color);
                    var nextAlpha = nextData.Brightness;
                    var timeToTransition = Atsc.GetSecondsFromBeat(nextData.Time - data.Time - baseTime - extraTime);
                    light.UpdateTargetColor(nextColor.Multiply(LightsManager.HDRIntensity), timeToTransition);
                    light.UpdateTargetAlpha(nextAlpha, timeToTransition);
                }
            }
            if (deltaTime != 0.0f)
                yield return new WaitForSeconds(deltaSecond);
            brightness += deltaAlpha;
            extraTime += deltaTime;
        }
        yield return null;

    }

    public void LightRotationEventPassed(bool natural, int idx, BeatmapLightRotationEvent e)
    {
        if (GroupIdToLaneIndex(e.Group) == -1) return;
        var allLights = LightsManagersV3[GroupIdToLaneIndex(e.Group)].ControllingRotations;
        var eb = e.EventBoxes[0];

        if (eb.Axis == 0 && !LightsManagersV3[GroupIdToLaneIndex(e.Group)].XRotatable) return;
        if (eb.Axis == 1 && !LightsManagersV3[GroupIdToLaneIndex(e.Group)].YRotatable) return;

        var filteredLights = eb.Filter.FilterType == 1
            ? Partition(allLights, eb.Filter.Section, eb.Filter.Partition, eb.Filter.Reverse == 1)
            : Range(allLights, eb.Filter.Partition, eb.Filter.Section, eb.Filter.Reverse == 1);
        if (filteredLights.Count() == 0) return;
        float deltaRotation = eb.RotationDistribution;
        if (eb.RotationDistributionType == 1) deltaRotation /= Intervals(filteredLights);
        float deltaTime = eb.Distribution;
        if (eb.DistributionType == 1) deltaTime /= Intervals(filteredLights);
        for (int i = 0; i < eb.EventDatas.Count; ++i)
        {
            var ebd = eb.EventDatas[i];
            if (i == 0 && eb.RotationAffectFirst == 0)
                StartCoroutine(LightRotationRoutine(filteredLights, deltaTime, 0, eb.Axis, eb.ReverseRotation == 1, e.Group, e.Time, idx, ebd));
            else
                StartCoroutine(LightRotationRoutine(filteredLights, deltaTime, deltaRotation, eb.Axis, eb.ReverseRotation == 1, e.Group, e.Time, idx, ebd));
        }
    }

    private IEnumerator LightRotationRoutine(IEnumerable<RotatingEvent> lights, float deltaTime, float deltaRotation, int axis, bool reverse,
        int group, float baseTime, int noteIdx, BeatmapLightRotationEventData data)
    {
        var deltaSecond = Atsc.GetSecondsFromBeat(deltaTime);
        float afterSeconds = Atsc.GetSecondsFromBeat(data.AddedBeat);
        if (afterSeconds != 0.0f) yield return new WaitForSeconds(afterSeconds);
        float rotation = data.RotationValue;
        float extraTime = 0;
        foreach (var light in lights)
        {
            var axisData = axis == 0 ? light.XData : light.YData;
            if (!axisData.SetNoteIndex(noteIdx)) continue;
            if (data.Transition != 1)
            {
                axisData.UpdateRotation(rotation, 0);
                axisData.SetReverse(reverse);
            }
            if (lightRotationEventsContainer.TryGetNextLightRotationEventData(group, light.RotationIdx, 
                baseTime + extraTime + data.Time, out var nextData))
            {
                if (nextData.Transition == 0)
                {
                    var timeToTransition = Atsc.GetSecondsFromBeat(nextData.Time - baseTime - extraTime - data.Time);
                    axisData.UpdateRotation(nextData.RotationValue, timeToTransition);
                    axisData.SetEaseFunction(nextData.EaseType);
                    axisData.SetLoop(nextData.AdditionalLoop);
                    axisData.SetDirection(nextData.RotationDirection);
                }
            }
            if (deltaTime != 0)
                yield return new WaitForSeconds(deltaSecond);
            rotation += deltaRotation;
            extraTime += deltaTime;
        }
        yield return null;
    }
}