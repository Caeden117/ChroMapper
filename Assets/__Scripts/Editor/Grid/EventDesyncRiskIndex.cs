using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;

/// <summary>
///     Flags same-type/name-filter ring-rotation events that sit inside one 50 Hz fixed tick; in game
///     a second event's spawner can read the ring's old cumulative destination before the queued
///     fixed-tick wave writes the first event's contribution, so the expected angle is lost and the
///     pair desyncs randomly.
/// </summary>
/// <remarks>
///     EventDesyncRiskTest drives every entry point. The flag is editor warning state keyed by object
///     reference, so it lives in this index rather than on <see cref="BaseEvent"/>; the owning
///     <see cref="EventGridContainer"/> only forwards spawn/remove/relink calls and repaints flagged
///     nodes through the change callback.
/// </remarks>
public sealed class EventDesyncRiskIndex
{
    // Events closer than one 50 Hz fixed update can anchor on a stale ring destination in game.
    private const float DesyncRiskWindowSeconds = 0.02f;

    private const BasicEventComponent RiskComponents =
        BasicEventComponent.RingRotation;

    // Reuse the FIFO window and flag buffers so edit-boundary relinking stays allocation-free.
    private readonly Queue<BaseEvent> window = new();
    private readonly HashSet<BaseEvent> flagBuffer = new();
    private readonly HashSet<BaseEvent> flagged = new();

    private readonly Func<int, BasicEventComponent> componentsForType;
    private readonly Action<BaseEvent> onFlagChanged;

    public EventDesyncRiskIndex(
        Func<int, BasicEventComponent> componentsForType,
        Action<BaseEvent> onFlagChanged)
    {
        this.componentsForType = componentsForType;
        this.onFlagChanged = onFlagChanged;
    }

    public bool IsFlagged(BaseEvent evt) => flagged.Contains(evt);

    private static float ThresholdSongBpmTime() =>
        DesyncRiskWindowSeconds * BeatSaberSongContainer.Instance.Info.BeatsPerMinute / 60f;

    private float scanThreshold;

    private bool IsRiskEvent(BaseEvent e) =>
        (componentsForType(e.Type) & RiskComponents) != 0;

    private static bool NameFiltersOverlap(BaseEvent a, BaseEvent b) =>
        string.IsNullOrEmpty(a.CustomNameFilter)
        || string.IsNullOrEmpty(b.CustomNameFilter)
        || string.Equals(a.CustomNameFilter, b.CustomNameFilter, StringComparison.OrdinalIgnoreCase);

    private static bool IsPartner(BaseEvent candidate, BaseEvent e) =>
        !ReferenceEquals(candidate, e)
        && candidate.Type == e.Type
        && NameFiltersOverlap(candidate, e);

    public void BeginScan()
    {
        window.Clear();
        flagBuffer.Clear();
        scanThreshold = ThresholdSongBpmTime();
    }

    public void Observe(BaseEvent e)
    {
        if (!IsRiskEvent(e)) return;

        while (window.Count > 0 && e.SongBpmTime - window.Peek().SongBpmTime > scanThreshold)
            window.Dequeue();

        foreach (var prev in window)
        {
            if (!IsPartner(prev, e)) continue;
            flagBuffer.Add(prev);
            flagBuffer.Add(e);
        }

        window.Enqueue(e);
    }

    public void FinishScan()
    {
        // Unflag events whose fixed-tick window no longer contains a same-type/filter neighbor.
        flagged.RemoveWhere(evt =>
        {
            if (flagBuffer.Contains(evt)) return false;
            onFlagChanged(evt);
            return true;
        });

        foreach (var evt in flagBuffer) SetFlag(evt, true);
    }

    public void HandleSpawned(BaseEvent e, List<BaseEvent> mapObjects)
    {
        if (!IsRiskEvent(e)) return;

        var index = mapObjects.BinarySearch(e);
        if (index < 0) index = ~index;
        var threshold = ThresholdSongBpmTime();
        var hasPartner = false;

        for (var i = index - 1; i >= 0; i--)
        {
            var prev = mapObjects[i];
            if (e.SongBpmTime - prev.SongBpmTime > threshold) break;
            if (!IsPartner(prev, e)) continue;
            hasPartner = true;
            SetFlag(prev, true);
        }

        for (var i = index; i < mapObjects.Count; i++)
        {
            var next = mapObjects[i];
            if (next.SongBpmTime - e.SongBpmTime > threshold) break;
            if (!IsPartner(next, e)) continue;
            hasPartner = true;
            SetFlag(next, true);
        }

        SetFlag(e, hasPartner);
    }

    // Object removal runs after MapObjects removal so former partners may lose their last in-window neighbor
    public void HandleRemoved(BaseEvent e, List<BaseEvent> mapObjects)
    {
        if (!IsRiskEvent(e)) return;

        SetFlag(e, false);
        var index = mapObjects.BinarySearch(e);
        if (index < 0) index = ~index;
        var threshold = ThresholdSongBpmTime();

        for (var i = index - 1; i >= 0; i--)
        {
            var prev = mapObjects[i];
            if (e.SongBpmTime - prev.SongBpmTime > threshold) break;
            if (IsPartner(prev, e)) SetFlag(prev, HasPartner(prev, mapObjects, threshold));
        }

        for (var i = index; i < mapObjects.Count; i++)
        {
            var next = mapObjects[i];
            if (next.SongBpmTime - e.SongBpmTime > threshold) break;
            if (IsPartner(next, e)) SetFlag(next, HasPartner(next, mapObjects, threshold));
        }
    }

    private bool HasPartner(BaseEvent e, List<BaseEvent> mapObjects, float threshold)
    {
        var index = mapObjects.BinarySearch(e);
        if (index < 0) index = ~index;

        for (var i = index - 1; i >= 0; i--)
        {
            var prev = mapObjects[i];
            if (e.SongBpmTime - prev.SongBpmTime > threshold) break;
            if (IsPartner(prev, e)) return true;
        }

        for (var i = index; i < mapObjects.Count; i++)
        {
            var next = mapObjects[i];
            if (next.SongBpmTime - e.SongBpmTime > threshold) break;
            if (IsPartner(next, e)) return true;
        }

        return false;
    }

    private void SetFlag(BaseEvent evt, bool risk)
    {
        var changed = risk
            ? flagged.Add(evt)
            : flagged.Remove(evt);
        if (changed)
            onFlagChanged(evt);
    }
}
