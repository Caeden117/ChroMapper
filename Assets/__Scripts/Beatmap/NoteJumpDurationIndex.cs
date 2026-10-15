using System.Collections.Generic;
using Beatmap.Base;

// Paused seeks use the longest jump to bound their hit-time query. Track duration changes from NJS
// updates too, so seeking backward cannot exclude notes still holding a longer jump duration.
internal sealed class NoteJumpDurationIndex
{
    private readonly HashSet<BaseNote> notes = new();
    private readonly Dictionary<float, int> counts = new();
    private readonly SortedSet<float> durations = new();

    internal float Maximum => durations.Count == 0 ? 0f : durations.Max;

    internal void Reset(IReadOnlyList<BaseNote> mapNotes)
    {
        Clear();
        foreach (var note in mapNotes) Add(note);
    }

    internal void Clear()
    {
        foreach (var note in notes) note.HalfJumpDurationChanged -= OnDurationChanged;
        notes.Clear();
        counts.Clear();
        durations.Clear();
    }

    internal void Add(BaseNote note)
    {
        if (!notes.Add(note)) return;
        AddDuration(note.HalfJumpDuration);
        note.HalfJumpDurationChanged += OnDurationChanged;
    }

    internal void Remove(BaseNote note)
    {
        if (!notes.Remove(note)) return;
        note.HalfJumpDurationChanged -= OnDurationChanged;
        RemoveDuration(note.HalfJumpDuration);
    }

    private void OnDurationChanged(BaseGrid note, float previous, float current)
    {
        RemoveDuration(previous);
        AddDuration(current);
    }

    private void AddDuration(float duration)
    {
        counts.TryGetValue(duration, out var count);
        counts[duration] = count + 1;
        if (count == 0) durations.Add(duration);
    }

    private void RemoveDuration(float duration)
    {
        var count = counts[duration] - 1;
        if (count == 0)
        {
            counts.Remove(duration);
            durations.Remove(duration);
        }
        else counts[duration] = count;
    }
}
