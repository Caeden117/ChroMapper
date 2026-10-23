using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

/// <summary>
///     Keeps the basic-event simulation synchronized with editor actions.
/// </summary>
/// <remarks>
///     A modified collection used to rebuild every basic-event state after a small selection move or mirror:
///     <c>O(M * P)</c>, where <c>M</c> is all map events and <c>P</c> is affected simulator state managers. Collection
///     replacement now removes originals and inserts final edits only: <c>O(C * P)</c>, where <c>C</c> is changed basic
///     events. Each state manager repairs the immediate state boundaries while removing and inserting an event, so
///     neighboring fades and transitions remain correct. This especially improves large lightshows where moving two
///     events previously replayed thousands of unrelated events.
/// </remarks>
public class BasicEventManager : BeatmapObjectManager<BaseEvent>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    [SerializeField] private LightshowController lightshowController;

    protected override void Awake()
    {
        base.Awake();
        BeatmapObjectContainerCollection.OnBpmTimingChanged += RefreshBpmTiming;
    }

    protected override void OnDestroy()
    {
        BeatmapObjectContainerCollection.OnBpmTimingChanged -= RefreshBpmTiming;
        base.OnDestroy();
    }

    private void RefreshBpmTiming(float jsonTime)
    {
        var descriptor = Context.Descriptor;
        // Map loading retimes objects before the environment establishes its state managers.
        if (descriptor == null || !AllowAction)
            return;

        var events = BeatSaberSongContainer.Instance.Map.Events.AsSpan();
        var start = events.LowerBoundBy(jsonTime, data => data.JsonTime);
        var affectedTypes = new HashSet<int>();
        for (var index = start; index < events.Length; index++)
            affectedTypes.Add(events[index].Type);

        var managers = descriptor.BasicEventEffectManager.EventTypeToEffects;
        var affectedManagers = new HashSet<StateManager<BaseEvent>>();
        foreach (var type in affectedTypes)
        {
            if (!managers.TryGetValue(type, out var effects))
                continue;

            foreach (var effect in effects)
            {
                if (affectedManagers.Add(effect))
                    effect.Initialize();

            }

        }

        // Each affected manager needs its earlier events to establish colors and transition boundaries.
        // Shared managers also need the other event types they consume, even when those types did not move.
        foreach (var data in events)
        {
            if (!managers.TryGetValue(data.Type, out var effects))
                continue;

            foreach (var effect in effects)
            {
                if (affectedManagers.Contains(effect))
                    effect.InsertData(data);

            }

        }

        UpdateTime();
    }

    public override void Refresh()
    {
        // Reserve the O(M * P) rebuild for explicit map/environment refreshes, not ordinary collection actions.
        // Unity song containers need explicit null checks before reading the current lightshow map.
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null ? songContainer.Map : null;
        if (map == null) return;
        Context.Descriptor.BasicEventEffectManager.Reinitialize();
        Context.Descriptor.BasicEventEffectManager.InsertData(map.Events);
    }

    public override void UpdateTime()
    {
        if (lightshowController.Mode != LightshowMode.Full) return;
        UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        // Combined pair effects are registered under several event types; use the
        // manager's unique list so one shared timeline is not evaluated repeatedly.
        var effects = Context.Descriptor.BasicEventEffectManager.Effects;
        for (var i = 0; i < effects.Count; i++)
            effects[i].UpdateTime(isPlaying, time);
    }

    // Delegate changed collections to the manager's allocation-free enumerable dispatcher.
    protected override bool AddData(IEnumerable<BaseEvent> data) =>
        Context.Descriptor.BasicEventEffectManager.InsertData(data);

    protected override bool RemoveData(IEnumerable<(BaseEvent reference, BaseEvent original)> data)
    {
        // Remove each original cache entry before its final replacement is inserted.
        var mark = false;
        foreach (var (reference, original) in data)
        {
            mark |= Context.Descriptor.BasicEventEffectManager.RemoveData(reference, original);
        }
        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEvent> data)
    {
        // Remove only the replaced events so neighboring state chunks repair their local boundaries.
        var mark = false;
        foreach (var evt in data)
        {
            mark |= Context.Descriptor.BasicEventEffectManager.RemoveData(evt, evt);
        }

        return mark;
    }

}
