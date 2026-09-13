using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    FloatFxGroupEffect : EventGroupEffect<
    FloatFxGroupStateData,
    FloatFxEventStateData,
    BaseVfxEventEventBoxGroup,
    BaseVfxEventEventBox,
    BaseFxEventFloat>
{
    [SerializeField] public bool Trigger;

    [SerializeField] private List<FxEntry> fxEntries = new();

    /// <summary>
    /// Root GameObject to search for FxTarget components when auto-populating fxEntries.
    /// Assign in the Inspector, then click "Populate Fx Entries from Auto Register Root".
    /// </summary>
    [SerializeField] public GameObject autoRegisterRoot;

    private FloatFxGroupContainer[] idToContainer = Array.Empty<FloatFxGroupContainer>();
    private FloatFxGroupContainer[] activeContainers = Array.Empty<FloatFxGroupContainer>();

    public void Register(int id, FxTarget target)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (fxEntries.Exists(x => x.ID == id))
            fxEntries.First(x => x.ID == id).Targets.Add(target);
        else
            fxEntries.Add(new FxEntry { ID = id, Targets = new List<FxTarget> { target } });
    }

    public void Unregister(int id) => fxEntries.Remove(fxEntries.Find(e => e.ID == id));

    /// <summary>
    /// Clears fxEntries and rebuilds them from all FxTarget components found under
    /// autoRegisterRoot, assigning IDs 0, 1, 2... in depth-first (hierarchy) order.
    /// </summary>
    public void PopulateFxEntriesFromRoot()
    {
        if (autoRegisterRoot == null)
        {
            Debug.LogWarning($"[FloatFxGroupEffect] autoRegisterRoot is not set on {gameObject.name}.");
            return;
        }

        var targets = autoRegisterRoot.GetComponentsInChildren<FxTarget>(true);

        fxEntries.Clear();
        for (var i = 0; i < targets.Length; i++)
        {
            fxEntries.Add(new FxEntry { ID = i, Targets = new List<FxTarget> { targets[i] } });
        }

        Debug.Log($"[FloatFxGroupEffect] Populated {targets.Length} FxTargets on {gameObject.name}.");
    }

    public override void Initialize()
    {
        idToContainer = new FloatFxGroupContainer[Count];
        foreach (var entry in fxEntries)
        {
            if (entry.ID >= Count)
            {
                Debug.LogError(
                    $"{entry}:{entry.ID} ID is larger than supported by group {ID}:{Count}, was the controller modified?");
                continue;
            }

            if (idToContainer[entry.ID] is null)
            {
                idToContainer[entry.ID] = new FloatFxGroupContainer(entry.ID, entry.Targets.ToArray());
                var container = idToContainer[entry.ID];

                var startEvent = new FloatFxEventStateData(new BaseFxEventFloat(), short.MinValue);
                var endEvent = new FloatFxEventStateData(
                    new BaseFxEventFloat { UsePrevious = 1 },
                    float.MaxValue);
                container.EventContainer.Resize(Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

                startEvent.EndTime = endEvent.StartTime;
                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;

                container.EventContainer.AddState(startEvent);
                container.EventContainer.AddState(endEvent);

                var start = CreateState(
                    new BaseVfxEventEventBoxGroup { songBpmTime = short.MinValue, JsonTime = short.MinValue });
                start.Box = new BaseVfxEventEventBox
                {
                    IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseFxEventFloat>()
                };
                start.LocalJsonTime = start.StartTime;

                var end = CreateState(
                    new BaseVfxEventEventBoxGroup { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
                end.Box = new BaseVfxEventEventBox
                {
                    IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseFxEventFloat>()
                };
                end.LocalJsonTime = end.StartTime = end.EndTime;

                RegenerateEvents(start, float.MaxValue);
                RegenerateEvents(end, float.MaxValue);

                container.EventContainer.SetStateAt(0);

                InitializeStates(container.GroupContainer, start, end);
            }
        }

        activeContainers = idToContainer.Where(x => x is not null).ToArray();
    }

    public override void Refresh()
    {
        foreach (var container in activeContainers)
        {
            container.EventContainer.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(container);
            container.Tween.UpdateTime(Atsc.CurrentSongBpmTime);

            if (Trigger)
            {
                for (var i = 0; i < container.Targets.Length; i++)
                    container.Targets[i].TriggerValue(ID, container.Id, container.EventContainer.CurrentState.Value);
            }
            else
            {
                for (var i = 0; i < container.Targets.Length; i++)
                    container.Targets[i].SetValue(ID, container.Id, container.Tween.Current);
            }
        }
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        for (var j = 0; j < activeContainers.Length; j++)
        {
            var container = activeContainers[j];
            if (!container.EventContainer.IsCurrentOrFindState(time, isPlaying))
            {
                UpdateObject(container);
                if (Trigger)
                {
                    for (var i = 0; i < container.Targets.Length; i++)
                    {
                        container
                            .Targets[i]
                            .TriggerValue(ID, container.Id, container.EventContainer.CurrentState.Value);
                    }
                }
            }

            if (!Trigger && container.Tween.UpdateTime(time))
            {
                for (var i = 0; i < container.Targets.Length; i++)
                    container.Targets[i].SetValue(ID, container.Id, container.Tween.Current);
            }
        }
    }

    private void UpdateObject(FloatFxGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var tween = container.Tween;

        tween.StartTime = state.StartTime;
        var startState = (FloatFxEventStateData)(state.UsePrevious ? state.Previous : state);
        tween.StartValue = startState.Value;

        tween.EndTime = state.EndTime;
        var endState = (FloatFxEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        tween.EndValue = endState.Value;

        tween.Easing = Easing.FromID(endState.Base.Easing);
    }

    protected override FloatFxGroupStateData CreateState(BaseVfxEventEventBoxGroup data) => new(data);

    protected override
        StateChunksContainer<FloatFxGroupStateData, BaseVfxEventEventBoxGroup>
        GetGroupContainer((Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.GroupContainer
            : null;
    }

    protected override StateChunksContainer<FloatFxEventStateData, BaseFxEventFloat> GetEventContainer(
        (Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.EventContainer
            : null;
    }

    protected override
        IEnumerable<(StateChunksContainer<FloatFxGroupStateData, BaseVfxEventEventBoxGroup>
            groupContainer, StateChunksContainer<FloatFxEventStateData, BaseFxEventFloat> eventContainer)>
        GetContainers() =>
        idToContainer.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseVfxEventEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseVfxEventEventBox box) => box.Events[^1].RelativeJsonTime;

    protected override float GetDistribution(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseVfxEventEventBox box,
        int order) =>
        DistributionHelper.GetValueStep(
            order,
            DistributionHelper.GetDistributionCount(indexFilter),
            (DistributionType)box.VfxDistributionType,
            box.VfxDistribution,
            (EaseType)box.Easing);

    protected override FloatFxEventStateData[] GenerateEvents(
        FloatFxGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime) =>
        state
            .Box
            .Events
            .Select((x, i) =>
                {
                    var affected = !(i == 0 && state.Box.VfxAffectFirst != 1);
                    var d = new FloatFxEventStateData(
                        x,
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                            state.Base.JsonTime + x.RelativeJsonTime + (state.DurationOrder * state.BeatStep)),
                        affected ? distributionOffset : 0f);
                    return d;
                }
            )
            .Where(x => state.Base.JsonTime + x.Base.RelativeJsonTime + (state.DurationOrder * state.BeatStep)
                <= maxRelativeJsonTime)
            .ToArray();
}

public class FloatFxGroupStateData : EventGroupStateData<
    BaseVfxEventEventBoxGroup,
    BaseVfxEventEventBox,
    BaseFxEventFloat>
{
    public FloatFxGroupStateData(BaseVfxEventEventBoxGroup data) : base(data)
    {
    }
}

[Serializable]
public class FloatFxEventStateData : EventGroupEventStateData<BaseFxEventFloat>
{
    public readonly float Value;

    public FloatFxEventStateData(BaseFxEventFloat data, float startTime, float offset = 0f) : base(
        data,
        startTime,
        data.Easing,
        data.UsePrevious) =>
        Value = data.Value + offset;
}

public record FloatFxGroupContainer : EventGroupContainer<
    FloatFxGroupStateData,
    FloatFxEventStateData,
    BaseVfxEventEventBoxGroup,
    BaseVfxEventEventBox,
    BaseFxEventFloat>
{
    public readonly FloatTween Tween = new();
    public readonly int Id;
    public readonly FxTarget[] Targets;

    public FloatFxGroupContainer(int id, FxTarget[] targets)
    {
        Id = id;
        Targets = targets;
    }
}

[Serializable]
public class FxEntry
{
    public int ID;
    public List<FxTarget> Targets;
}

#if UNITY_EDITOR
namespace FloatFxGroupEffectEditor_NS
{
    using UnityEditor;

    [CustomEditor(typeof(FloatFxGroupEffect))]
    public class FloatFxGroupEffectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto-Register", EditorStyles.boldLabel);

            var effect = (FloatFxGroupEffect)target;

            EditorGUI.BeginDisabledGroup(effect.autoRegisterRoot == null);
            if (GUILayout.Button("Populate Fx Entries from Auto Register Root"))
            {
                Undo.RecordObject(effect, "Populate Fx Entries");
                effect.PopulateFxEntriesFromRoot();
                EditorUtility.SetDirty(effect);
            }

            EditorGUI.EndDisabledGroup();

            if (effect.autoRegisterRoot == null)
                EditorGUILayout.HelpBox(
                    "Assign a GameObject to 'Auto Register Root' to enable population.",
                    MessageType.Info);
        }
    }
}
#endif
