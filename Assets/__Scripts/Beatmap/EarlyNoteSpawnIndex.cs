using System;
using System.Collections.Generic;
using Beatmap.Base;

/// <summary>
/// Finds notes that need to start jumping before the hit-time callback walk reaches them.
/// </summary>
/// <remarks>
/// MapObjects stays ordered by hit beat, but custom HalfJumpDuration values can change spawn order.
/// With Offset 2, a beat-12 note with HalfJumpDuration 8 spawns at beat 4 minus jumpTime,
/// before a beat-10 note with HalfJumpDuration 2 spawns at beat 8 minus jumpTime. The hit-time
/// walk stops at the beat-10 note and would miss the beat-12 note until it was already late.
/// Keep a separate spawn-time order and the original map indices required by callbacks.
/// </remarks>
internal sealed class EarlyNoteSpawnIndex
{
    private readonly List<(float DueTime, int MapIndex, BaseObject Note)> pending = new();
    private readonly HashSet<BaseObject> emitted = new();
    private readonly float jumpTime;
    private float indexedOffset = float.NaN;
    // Advance through the sorted entries without removing or shifting them during playback.
    private int nextPendingIndex;

    public EarlyNoteSpawnIndex(float jumpTime) => this.jumpTime = jumpTime;

    // Starting playback establishes a new hit-time cursor, so previous emissions no longer apply.
    public void Reset(IReadOnlyList<BaseNote> notes, int nextNoteIndex, float offset)
    {
        emitted.Clear();
        Rebuild(notes, nextNoteIndex, offset);
    }

    public void RebuildIfOffsetChanged(IReadOnlyList<BaseNote> notes, int nextNoteIndex, float offset)
    {
        if (offset != indexedOffset) Rebuild(notes, nextNoteIndex, offset);
    }

    // Only notes with HalfJumpDuration above offset need this index, but that can include the whole
    // remaining map. Rebuilding scans that range and sorts the candidates once, keeping frame queries
    // proportional to due entries. Note edits also require a rebuild because stored map indices change.
    // Preserve emission markers across rebuilds so an offset change or edit cannot emit a note twice.
    public void Rebuild(IReadOnlyList<BaseNote> notes, int nextNoteIndex, float offset)
    {
        indexedOffset = offset;
        pending.Clear();
        nextPendingIndex = 0;
        for (var i = Math.Max(0, nextNoteIndex); i < notes.Count; ++i)
        {
            var note = notes[i];
            if (note.HalfJumpDuration > offset && !emitted.Contains(note))
            {
                pending.Add((note.SongBpmTime - Math.Max(note.HalfJumpDuration, offset) - jumpTime, i, note));
            }
        }

        pending.Sort((a, b) =>
        {
            var byDueTime = a.DueTime.CompareTo(b.DueTime);
            return byDueTime != 0 ? byDueTime : a.MapIndex.CompareTo(b.MapIndex);
        });
    }

    // Taking a candidate does not mark it emitted: the caller may filter it out without firing a callback.
    public bool TryTakeDueNote(float songBpmTime, int nextNoteIndex, out int mapIndex, out BaseObject note)
    {
        while (nextPendingIndex < pending.Count && pending[nextPendingIndex].DueTime <= songBpmTime)
        {
            var candidate = pending[nextPendingIndex++];
            if (emitted.Contains(candidate.Note) || candidate.MapIndex < nextNoteIndex) continue;

            mapIndex = candidate.MapIndex;
            note = candidate.Note;
            return true;
        }

        mapIndex = -1;
        note = null;
        return false;
    }

    public void RecordEmission(BaseObject note) => emitted.Add(note);

    // Release the marker when the hit-time walk reaches the note or the note is deleted.
    // A true result tells the walk to skip the callback already fired through this index.
    public bool ForgetEmission(BaseObject note) => emitted.Remove(note);
}
