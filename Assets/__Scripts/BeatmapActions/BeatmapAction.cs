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
    public List<BeatmapObjectContainer> containers = new List<BeatmapObjectContainer>();
    public List<BeatmapObject> data = new List<BeatmapObject>();

    public BeatmapAction (IEnumerable<BeatmapObjectContainer> containers, string comment = "No comment."){
        if (containers is null) return;
        foreach(BeatmapObjectContainer con in containers)
        {
            if (con is null) continue;
            this.containers.Add(con);
            data.Add(con.objectData);
        }
        Comment = comment;
    }

    public static bool operator ==(BeatmapAction a, BeatmapAction b)
    {
        if (a is null || b is null) return false;
        if (a is null && b is null) return true;
        return a.data == b.data && a.containers == b.containers && a.Active == b.Active;
    }

    public static bool operator !=(BeatmapAction a, BeatmapAction b)
    {
        if (a is null || b is null) return false;
        if (a is null && b is null) return true;
        return a.data != b.data || a.containers != b.containers || a.Active != b.Active;
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
            hashCode = (hashCode * 400) ^ data.GetHashCode();
            hashCode = (hashCode * 400) ^ containers.GetHashCode();
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