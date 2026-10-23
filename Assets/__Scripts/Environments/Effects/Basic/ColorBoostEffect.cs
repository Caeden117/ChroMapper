using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Comparers;

public class ColorBoostEffect : BasicEventEffect<ColorBoostStateData>, IEffectStateSignal<bool>
{
    private readonly BasicEventStateChunksContainer<ColorBoostStateData> container = new();
    private readonly List<BoostEntry> events = new();
    private readonly List<BoostEntry> changes = new();
    private readonly Dictionary<BaseEvent, BoostEntry> eventEntries = new();
    private int nextOrder;
    public ColorSchemeProvider ColorSchemeProvider;
    public bool Boost;

    public event Action<bool> OnStateChanged;

    public override void Initialize()
    {
        InitializeStates(container);
        events.Clear();
        changes.Clear();
        eventEntries.Clear();
        nextOrder = 0;
    }

    // Repeated values do not call native BoostCallback's refresh. Cache only actual changes so
    // light color queries can reconstruct a seek without scanning or copying boost events per frame.
    public (float Time, int FileOrder) GetLastChange(float songBpmTime)
    {
        var lower = 0;
        var upper = changes.Count;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) / 2);
            if (changes[middle].Time <= songBpmTime)
                lower = middle + 1;

            else
                upper = middle;

        }

        return lower > 0
            ? (changes[lower - 1].Time, changes[lower - 1].FileOrder)
            : (float.NegativeInfinity, int.MinValue);
    }

    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(ColorBoostStateData stateData)
    {
        if (stateData.Boost == Boost) return;
        Boost = stateData.Boost;
        ColorSchemeProvider.ColorScheme.SwapEnvironmentColors(Boost);
        OnStateChanged?.Invoke(Boost);
    }

    public bool GetCurrentState() => Boost;
    
    protected override ColorBoostStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.Boost = data.Value == 1;

        HandleInsertState(container, state);

        var entry = new BoostEntry(data, state.Boost, nextOrder++);
        var index = FindInsertionIndex(events, entry);
        events.Insert(index, entry);
        eventEntries.Add(data, entry);
        RebuildChanges(index, entry);

        // Inserting a boost before the paused preview time must replace the cached current state immediately.
        if (state.StartTime <= Atsc.CurrentSongBpmTime)
        {
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(container.CurrentState);
        }
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var entry = eventEntries[reference];
        var index = events.IndexOf(entry);
        events.RemoveAt(index);
        eventEntries.Remove(reference);
        RebuildChanges(index, entry);
        var state = HandleRemoveState(container, reference, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(reference.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    private void RebuildChanges(int index, BoostEntry firstChanged)
    {
        var changeIndex = FindInsertionIndex(changes, firstChanged, inclusive: true);
        changes.RemoveRange(changeIndex, changes.Count - changeIndex);
        var previousBoost = index > 0 && events[index - 1].Boost;
        for (var i = index; i < events.Count; i++)
        {
            var entry = events[i];
            if (entry.Boost != previousBoost)
                changes.Add(entry);

            previousBoost = entry.Boost;
        }
    }

    private static int FindInsertionIndex(List<BoostEntry> entries, BoostEntry entry, bool inclusive = false)
    {
        var lower = 0;
        var upper = entries.Count;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) / 2);
            var comparison = entries[middle].CompareTo(entry);
            if (comparison < 0 || (!inclusive && comparison == 0))
                lower = middle + 1;

            else
                upper = middle;

        }

        return lower;
    }

    private sealed class BoostEntry
    {
        public readonly float Time;
        public readonly bool Boost;
        private readonly float jsonTime;
        public readonly int FileOrder;
        private readonly int insertionOrder;

        public BoostEntry(BaseEvent source, bool boost, int order)
        {
            Time = source.SongBpmTime;
            jsonTime = source.JsonTime;
            Boost = boost;
            FileOrder = source.FileOrder;
            insertionOrder = order;
        }

        public int CompareTo(BoostEntry other)
        {
            var comparison = EventOrderComparer.CompareKeys(jsonTime, FileOrder, other.jsonTime, other.FileOrder);
            return comparison != 0
                ? comparison
                : insertionOrder.CompareTo(other.insertionOrder);
        }
    }
}

public class ColorBoostStateData : BasicEventStateData
{
    public bool Boost;

    public ColorBoostStateData(BaseEvent data) : base(data)
    {
    }
}
