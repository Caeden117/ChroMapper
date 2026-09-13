using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    LightColorGroupEffect : EventGroupEffect<
    LightColorGroupStateData,
    LightColorEventStateData,
    BaseLightColorEventBoxGroup,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    [SerializeField] public ColorBoostEffect ColorBoostEffect;
    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;

    [SerializeField] private List<LightController> lightEntries = new();
    private LightColorGroupContainer[] idToContainer = Array.Empty<LightColorGroupContainer>();
    private LightColorGroupContainer[] activeContainers = Array.Empty<LightColorGroupContainer>();

    public void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChange;
    public void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChange;

    public void Register(LightController controller) => lightEntries.Add(controller);

    public void Unregister(LightController controller) => lightEntries.Remove(controller);

    private void HandleBoostChange(bool boost)
    {
        var time = Atsc.CurrentSongBpmTime;
        for (var i = 0; i < activeContainers.Length; i++)
        {
            var container = activeContainers[i];
            var state = container.EventContainer.CurrentState;
            var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
            var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);

            // Resolve default GLS colors through the color scheme injected by the dev effect manager.
            var startColor = startState.Base.CustomColor
                ?? ColorSchemeProvider.ColorScheme.GetColorFrom((LightColor)startState.Base.Color, false);
            var endColor = endState.Base.CustomColor
                ?? ColorSchemeProvider.ColorScheme.GetColorFrom((LightColor)endState.Base.Color, false);

            container.Tween.StartColor = startColor;
            container.Tween.EndColor = endColor;
            container.Tween.StartStrobeColor = startState.Base.StrobeColor ?? startColor;
            container.Tween.EndStrobeColor = endState.Base.StrobeColor ?? endColor;

            // A paused preview has no subsequent time tick to apply the retinted GLS tween to its controllers. Apply the color change immediately.
            container.Tween.UpdateTime(time);
            foreach (var controller in container.Lights)
                controller.SetColor(container.Tween.Color, container.EventContainer.CurrentState, time);
        }
    }

    public override void Initialize()
    {
        idToContainer = new LightColorGroupContainer[Count];
        foreach (var elementId in lightEntries.Select(x => x.ID).Distinct())
        {
            if (elementId < 0 || elementId >= Count)
            {
                Debug.LogError($"Element {elementId} is outside the supported range for group {ID}:{Count}.");
                continue;
            }

            if (idToContainer[elementId] is null)
            {
                idToContainer[elementId] = new LightColorGroupContainer { ElementId = elementId };
                var container = idToContainer[elementId];

                var startEvent = new LightColorEventStateData(new BaseLightColorBase(), short.MinValue);
                var endEvent = new LightColorEventStateData(
                    new BaseLightColorBase { UsePrevious = 1 },
                    float.MaxValue);
                container.EventContainer.Resize(Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

                startEvent.EndTime = endEvent.StartTime;
                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;

                container.EventContainer.AddState(startEvent);
                container.EventContainer.AddState(endEvent);

                var start = CreateState(
                    new BaseLightColorEventBoxGroup { songBpmTime = short.MinValue, JsonTime = short.MinValue });
                start.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                start.LocalJsonTime = start.StartTime;

                var end = CreateState(
                    new BaseLightColorEventBoxGroup { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
                end.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                end.LocalJsonTime = end.StartTime = end.EndTime;

                RegenerateEvents(start, float.MaxValue);
                RegenerateEvents(end, float.MaxValue);

                container.EventContainer.SetStateAt(0);

                InitializeStates(container.GroupContainer, start, end);
            }
        }

        foreach (var entry in lightEntries.Where(x => 0 <= x.ID && x.ID < Count))
            idToContainer[entry.ID]?.Lights.Add(entry);

        activeContainers = idToContainer.Where(x => x is not null).ToArray();
    }

    public override void Refresh()
    {
        var time = Atsc.CurrentSongBpmTime;
        foreach (var container in activeContainers)
        {
            container.EventContainer.SetStateAt(time);
            UpdateObject(container);
            container.Tween.UpdateTime(time);
            foreach (var controller in container.Lights)
                controller.SetColor(container.Tween.Color, container.EventContainer.CurrentState, time);
        }
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        foreach (var container in activeContainers)
        {
            if (!container.EventContainer.IsCurrentOrFindState(time, isPlaying)) UpdateObject(container);
            if (!container.Tween.UpdateTime(time)) continue;
            foreach (var controller in container.Lights)
                controller.SetColor(container.Tween.Color, container.EventContainer.CurrentState, time);
        }
    }

    private void UpdateObject(LightColorGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var tween = container.Tween;

        tween.StartTimeAlpha = tween.StartTimeColor = state.StartTime;
        var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        tween.StartAlpha = startState.Brightness;
        tween.StartColor = startState.Base.CustomColor
            ?? ColorSchemeProvider.ColorScheme.GetColorFrom((LightColor)startState.Base.Color, false);
        tween.StartStrobeFrequency = StrobeFrequencyFor(startState.Base);
        tween.StartStrobeBrightness = startState.Base.StrobeBrightness;
        tween.StartStrobeColor = startState.Base.StrobeColor ?? tween.StartColor;

        tween.EndTimeAlpha = tween.EndTimeColor = state.EndTime;
        var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        tween.EndAlpha = endState.Brightness;
        tween.EndColor = endState.Base.CustomColor
            ?? ColorSchemeProvider.ColorScheme.GetColorFrom((LightColor)endState.Base.Color, false);

        if (endState.Base.Easing == (int)EaseType.None)
        {
            tween.EndStrobeFrequency = StrobeFrequencyFor(startState.Base);
            tween.EndStrobeBrightness = startState.Base.StrobeBrightness;
            tween.EndStrobeColor = tween.StartStrobeColor;
            tween.StrobeFade = startState.Base.StrobeFade == 1;
        }
        else
        {
            tween.EndStrobeFrequency = StrobeFrequencyFor(endState.Base);
            tween.EndStrobeBrightness = endState.Base.StrobeBrightness;
            tween.EndStrobeColor = endState.Base.StrobeColor ?? tween.EndColor;
            // shouldn't we fade between no strobe fade and strobe fade...? What does the game even do?
            tween.StrobeFade = endState.Base.StrobeFade == 1;
        }

        tween.Easing = Easing.FromID(endState.Base.Easing);
    }

    private static float StrobeFrequencyFor(BaseLightColorBase lightColorBase)
    {
        // A 0-light-level node with no strobe flash is not a strobe, regardless of strobeInterval or strobeColor.
        if (lightColorBase.Brightness <= 0f && lightColorBase.StrobeBrightness <= 0f) return 0f;

        // customData.strobeInterval is the period in beats per strobe cycle; the in-editor tween expects cycles per beat.
        return lightColorBase.ChromaStrobeInterval is { } interval && interval > 0f
            ? 1f / interval
            : lightColorBase.Frequency;
    }

    protected override LightColorGroupStateData CreateState(BaseLightColorEventBoxGroup data) => new(data);

    protected override
        StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>
        GetGroupContainer((Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.GroupContainer
            : null;
    }

    protected override StateChunksContainer<LightColorEventStateData, BaseLightColorBase> GetEventContainer(
        (Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.EventContainer
            : null;
    }

    protected override
        IEnumerable<(StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>
            groupContainer, StateChunksContainer<LightColorEventStateData, BaseLightColorBase> eventContainer)>
        GetContainers() =>
        idToContainer.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseLightColorEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseLightColorEventBox box) => box.Events[^1].RelativeJsonTime;

    protected override float GetDistribution(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseLightColorEventBox box,
        int order) =>
        DistributionHelper.GetValueStep(
            order,
            DistributionHelper.GetDistributionCount(indexFilter),
            (DistributionType)box.BrightnessDistributionType,
            box.BrightnessDistribution,
            (EaseType)box.Easing);

    protected override LightColorEventStateData[] GenerateEvents(
        LightColorGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime) =>
        state
            .Box
            .Events
            .Select((x, i) =>
            {
                var affected = !(i == 0 && state.Box.BrightnessAffectFirst != 1);
                var d = new LightColorEventStateData(
                    x,
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                        state.Base.JsonTime + x.RelativeJsonTime + (state.DurationOrder * state.BeatStep)),
                    affected ? distributionOffset : 0f);
                return d;
            })
            .Where(x => state.Base.JsonTime + x.Base.RelativeJsonTime + (state.DurationOrder * state.BeatStep)
                <= maxRelativeJsonTime)
            .ToArray();
}

public class LightColorGroupStateData : EventGroupStateData<
    BaseLightColorEventBoxGroup,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    public LightColorGroupStateData(BaseLightColorEventBoxGroup data) : base(data)
    {
    }
}

[Serializable]
public class LightColorEventStateData : EventGroupEventStateData<BaseLightColorBase>
{
    public readonly float Brightness;

    public LightColorEventStateData(BaseLightColorBase data, float startTime, float offset = 0f) : base(
        data,
        startTime,
        data.Easing,
        data.UsePrevious) =>
        Brightness = data.Brightness + offset;
}

public record LightColorGroupContainer : EventGroupContainer<
    LightColorGroupStateData,
    LightColorEventStateData,
    BaseLightColorEventBoxGroup,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    public int ElementId;
    public readonly LightColorTween Tween = new();
    public readonly List<LightController> Lights = new();
}
