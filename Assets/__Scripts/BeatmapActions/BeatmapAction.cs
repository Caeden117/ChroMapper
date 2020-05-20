using System.Collections.Generic;
using System;

/// <summary>
/// A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
/// 
/// An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
/// or using Strobe Generator
/// </summary>
public abstract class BeatmapAction : IEquatable<BeatmapAction>
{
    public bool Active = true;
    public string Comment { get; private set; } = "No comment.";
    public IEnumerable<BeatmapObject> Data;

    public BeatmapAction (IEnumerable<BeatmapObject> data, string comment = "No comment."){
        Data = data;
        Comment = comment;
    }

    protected void RefreshPools(IEnumerable<BeatmapObject> data)
    {
        foreach (BeatmapObject unique in data.DistinctBy(x => x.beatmapType))
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType);
            collection.RefreshPool(collection.AudioTimeSyncController.CurrentBeat + collection.DespawnCallbackController.offset,
                collection.AudioTimeSyncController.CurrentBeat + collection.SpawnCallbackController.offset);
        }
    }

    public static bool operator ==(BeatmapAction a, BeatmapAction b)
    {
        if (a is null || b is null) return false;
        if (a is null && b is null) return true;
        return a.Data == b.Data && a.Active == b.Active;
    }

    public static bool operator !=(BeatmapAction a, BeatmapAction b)
    {
        if (a is null || b is null) return false;
        if (a is null && b is null) return true;
        return a.Data != b.Data || a.Active != b.Active;
    }

    public bool Equals(BeatmapAction other)
    {
        if (this is null || other is null) return false;
        if (this is null && other is null) return true;
        return this == other;
    }

    public override bool Equals(object obj)
    {
        if (this is null || obj is null) return false;
        if (this is null && obj is null) return true;
        return GetType() == obj.GetType() && Equals((BeatmapAction)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Active.GetHashCode();
            hashCode = (hashCode * 400) ^ Data.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Steps that should be taken to Undo an Action.
    /// </summary>
    /// <param name="param">Collection of useful stuff.</param>
    public abstract void Undo(BeatmapActionContainer.BeatmapActionParams param);

    /// <summary>
    /// Steps that should be taken to Redo an Action.
    /// </summary>
    /// <param name="param">Collection of useful stuff.</param>
    public abstract void Redo(BeatmapActionContainer.BeatmapActionParams param);

}