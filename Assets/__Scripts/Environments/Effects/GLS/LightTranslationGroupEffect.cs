using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    LightTranslationGroupEffect : EventGroupEffect<
    LightTranslationGroupStateData,
    LightTranslationEventStateData,
    BaseLightTranslationEventBoxGroup,
    BaseLightTranslationEventBox,
    BaseLightTranslationBase>
{
    [SerializeField] private List<TransformEntry> transformEntries = new();

    [SerializeField] public Vector2[] TranslationLimits;
    [SerializeField] public Vector2[] DistributionLimits;

    private readonly Dictionary<(Axis axis, int index), LightTranslationGroupContainer>
        idToContainer = new();

    private LightTranslationGroupContainer[] activeContainers = Array.Empty<LightTranslationGroupContainer>();

    public void Register(int id, Axis axis, bool mirrored, Transform tr)
    {
        if (transformEntries.Exists(x => x.ID == id && x.Axis == axis))
            transformEntries.First(x => x.ID == id && x.Axis == axis).Transforms.Add(tr);
        else
            transformEntries.Add(
                new TransformEntry
                {
                    ID = id, Transforms = new List<Transform> { tr }, Axis = axis, Mirrored = mirrored
                });
    }

    public void Unregister(int id, Axis axis) => transformEntries.RemoveAll(e => e.ID == id && e.Axis == axis);

    // public void Unregister(Transform tr) => transformEntries.RemoveAll(e => e.Transforms == tr);

    public override void Initialize()
    {
        idToContainer.Clear();
        foreach (var entry in transformEntries)
        {
            if (idToContainer.ContainsKey((entry.Axis, entry.ID))) continue;

            idToContainer[(entry.Axis, entry.ID)] = new LightTranslationGroupContainer(
                entry.Transforms.ToArray(),
                entry.Axis,
                entry.Mirrored);
            var container = idToContainer[(entry.Axis, entry.ID)];

            var startEvent = new LightTranslationEventStateData(new BaseLightTranslationBase(), short.MinValue);
            var endEvent = new LightTranslationEventStateData(
                new BaseLightTranslationBase { UsePrevious = 1 },
                float.MaxValue);
            container.EventContainer.Resize(Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

            startEvent.EndTime = endEvent.StartTime;
            startEvent.Next = endEvent;
            endEvent.Previous = startEvent;

            container.EventContainer.AddState(startEvent);
            container.EventContainer.AddState(endEvent);

            var start = CreateState(
                new BaseLightTranslationEventBoxGroup { songBpmTime = short.MinValue, JsonTime = short.MinValue });
            start.Box = new BaseLightTranslationEventBox
            {
                Axis = (int)entry.Axis,
                IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                Events = Array.Empty<BaseLightTranslationBase>()
            };
            start.LocalJsonTime = start.StartTime;

            var end = CreateState(
                new BaseLightTranslationEventBoxGroup { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
            end.Box = new BaseLightTranslationEventBox
            {
                Axis = (int)entry.Axis,
                IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                Events = Array.Empty<BaseLightTranslationBase>()
            };
            end.LocalJsonTime = end.StartTime = end.EndTime;

            RegenerateEvents(start, float.MaxValue);
            RegenerateEvents(end, float.MaxValue);

            InitializeStates(container.GroupContainer, start, end);

            container.GroupContainer.SetStateAt(0);
            container.EventContainer.SetStateAt(0);
        }

        activeContainers = idToContainer.Values.ToArray();
    }

    public override void Refresh()
    {
        foreach (var container in idToContainer.Values)
        {
            container.EventContainer.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(container);
            container.Tween.UpdateTime(Atsc.CurrentSongBpmTime);
            SetTranslation(container);
        }
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        for (var i = 0; i < activeContainers.Length; i++)
        {
            var container = activeContainers[i];
            if (!container.EventContainer.IsCurrentOrFindState(time, isPlaying)) UpdateObject(container);
            if (!container.Tween.UpdateTime(time)) continue;
            SetTranslation(container);
        }
    }

    private void UpdateObject(LightTranslationGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var tween = container.Tween;

        tween.StartTime = state.StartTime;
        var startState = (LightTranslationEventStateData)(state.UsePrevious ? state.Previous : state);

        tween.EndTime = state.EndTime;
        var endState = (LightTranslationEventStateData)(state.Next.UsePrevious ? startState : state.Next);

        var translationLimits = container.Axis switch
        {
            Axis.X => TranslationLimits[0],
            Axis.Y => TranslationLimits[1],
            Axis.Z => TranslationLimits[2],
            _ => throw new ArgumentOutOfRangeException()
        };

        var distributionLimits = container.Axis switch
        {
            Axis.X => DistributionLimits[0],
            Axis.Y => DistributionLimits[1],
            Axis.Z => DistributionLimits[2],
            _ => throw new ArgumentOutOfRangeException()
        };

        tween.StartValue = ComputeTranslation(
            startState.Translation,
            translationLimits,
            startState.Distribution,
            distributionLimits,
            container.Mirrored);
        tween.EndValue = ComputeTranslation(
            endState.Translation,
            translationLimits,
            endState.Distribution,
            distributionLimits,
            container.Mirrored);

        tween.Easing = Easing.FromID((int)endState.EaseType);
    }

    private void SetTranslation(LightTranslationGroupContainer container)
    {
        var t = container.Tween.Current;
        for (var i = 0; i < container.Transforms.Length; i++)
        {
            var containerTransform = container.Transforms[i];
            var tr = containerTransform;
            var local = tr.localPosition;
            switch (container.Axis)
            {
                case Axis.X:
                    local.x = t;
                    break;
                case Axis.Y:
                    local.y = t;
                    break;
                case Axis.Z:
                    local.z = t;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            tr.localPosition = local;
        }
    }

    private float ComputeTranslation(
        float translation,
        Vector2 translationLimits,
        float distribution,
        Vector2 distributionLimits,
        bool mirrored)
    {
        var tTrans = ((mirrored ? 0f - translation : translation) + 1f) * 0.5f;
        var tDist = ((mirrored ? 0f - distribution : distribution) + 1f) * 0.5f;
        return Mathf.LerpUnclamped(translationLimits.x, translationLimits.y, tTrans)
            + Mathf.LerpUnclamped(distributionLimits.x, distributionLimits.y, tDist);
    }

    protected override LightTranslationGroupStateData CreateState(
        BaseLightTranslationEventBoxGroup data) =>
        new(data);

    protected override
        StateChunksContainer<LightTranslationGroupStateData,
            BaseLightTranslationEventBoxGroup>
        GetGroupContainer((Axis axis, int element) key)
    {
        return idToContainer.TryGetValue(key, out var value)
            ? value?.GroupContainer
            : null;
    }

    protected override StateChunksContainer<LightTranslationEventStateData, BaseLightTranslationBase> GetEventContainer(
        (Axis axis, int element) key)
    {
        return idToContainer.TryGetValue(key, out var value)
            ? value?.EventContainer
            : null;
    }

    protected override
        IEnumerable<(
            StateChunksContainer<LightTranslationGroupStateData,
                BaseLightTranslationEventBoxGroup> groupContainer,
            StateChunksContainer<LightTranslationEventStateData, BaseLightTranslationBase> eventContainer)>
        GetContainers() =>
        idToContainer.Values.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseLightTranslationEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseLightTranslationEventBox box) => box.Events[^1].RelativeJsonTime;

    protected override float GetDistribution(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseLightTranslationEventBox box,
        int order) =>
        DistributionHelper.GetValueStep(
            order,
            DistributionHelper.GetDistributionCount(indexFilter),
            (DistributionType)box.TranslationDistributionType,
            box.TranslationDistribution,
            (EaseType)box.Easing);

    protected override LightTranslationEventStateData[] GenerateEvents(
        LightTranslationGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime) =>
        state
            .Box
            .Events
            .Select((x, i) =>
                {
                    var distribution = state.Box.TranslationAffectFirst != 1 && i == 0 ? 0f : distributionOffset;
                    var d = new LightTranslationEventStateData(
                        x,
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                            state.Base.JsonTime + x.RelativeJsonTime + (state.DurationOrder * state.BeatStep)),
                        state.Box.Flip == 1 ? -1 : 1,
                        distribution);
                    return d;
                }
            )
            .Where(x => state.Base.JsonTime + x.Base.RelativeJsonTime + (state.DurationOrder * state.BeatStep)
                <= maxRelativeJsonTime)
            .ToArray();
}

public class LightTranslationGroupStateData : EventGroupStateData<
    BaseLightTranslationEventBoxGroup,
    BaseLightTranslationEventBox,
    BaseLightTranslationBase>
{
    public LightTranslationGroupStateData(BaseLightTranslationEventBoxGroup data) : base(
        data)
    {
    }
}

[Serializable]
public class LightTranslationEventStateData : EventGroupEventStateData<BaseLightTranslationBase>
{
    public readonly float Translation;
    public readonly float Distribution;

    public LightTranslationEventStateData(
        BaseLightTranslationBase data,
        float startTime,
        int direction = 1,
        float offset = 0f) : base(data, startTime, data.EaseType, data.UsePrevious)
    {
        Translation = data.Translation * direction;
        Distribution = offset * direction;
    }
}

public record LightTranslationGroupContainer : EventGroupContainer<
    LightTranslationGroupStateData,
    LightTranslationEventStateData,
    BaseLightTranslationEventBoxGroup,
    BaseLightTranslationEventBox,
    BaseLightTranslationBase>
{
    public readonly Transform[] Transforms;
    public readonly Axis Axis;
    public readonly bool Mirrored;

    public readonly FloatTween Tween = new();

    public LightTranslationGroupContainer(Transform[] transforms, Axis axis, bool mirrored)
    {
        Transforms = transforms;
        Axis = axis;
        Mirrored = mirrored;
    }
}
