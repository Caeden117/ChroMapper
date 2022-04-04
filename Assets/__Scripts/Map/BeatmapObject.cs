using System;
using SimpleJSON;
using UnityEngine;

public abstract class BeatmapObject
{
    protected static int DecimalPrecision =>
#if UNITY_EDITOR
        6;
#else
        Settings.Instance.TimeValueDecimalPrecision;
#endif


    public enum ObjectType
    {
        Note,
        Event,
        Obstacle,
        CustomNote,
        CustomEvent,
        BpmChange,
        Arc, // introduced in v3
        Chain // introduced in v3
    }

    public const string BeatmapObjectV3CustomDataKey = "cd";

    public abstract ObjectType BeatmapType { get; set; }

    /// <summary>
    ///     Whether or not there exists a <see cref="BeatmapObjectContainer" /> that contains this data.
    /// </summary>
    public bool HasAttachedContainer = false;

    /// <summary>
    ///     Time, in beats, where this object is located.
    /// </summary>
    public float Time;

    /// <summary>
    ///     An expandable <see cref="JSONNode" /> that stores data for Beat Saber mods to use.
    /// </summary>
    public JSONNode CustomData;

    public abstract JSONNode ConvertToJson();

    protected abstract bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false);

    /// <summary>
    ///     Create an identical, yet not exact, copy of a given <see cref="BeatmapObject" />.
    /// </summary>
    /// <typeparam name="T">Specific type of BeatmapObject (Note, event, etc.)</typeparam>
    /// <param name="originalData">Original object to clone.</param>
    /// <returns>A clone of <paramref name="originalData" />.</returns>
    public static T GenerateCopy<T>(T originalData) where T : BeatmapObject
    {
        if (originalData is null) throw new ArgumentException("originalData is null.");
        T objectData;
        switch (originalData)
        {
            case MapEvent evt:
                var ev = new MapEvent(evt.Time, evt.Type, evt.Value, originalData.CustomData?.Clone())
                {
                    LightGradient = evt.LightGradient?.Clone()
                };
                objectData = ev as T;
                break;
            case BeatmapNote note:
                objectData = new BeatmapNote(note.Time, note.LineIndex, note.LineLayer, note.Type,
                    note.CutDirection, originalData.CustomData?.Clone()) as T;
                break;
            case BeatmapArc arc:
                objectData = new BeatmapArc(arc) as T;
                break;
            default:
                objectData =
                    Activator.CreateInstance(originalData.GetType(), new object[] { originalData.ConvertToJson() }) as T;
                objectData.CustomData = originalData.CustomData?.Clone();
                break;
        }

        return objectData;
    }

    protected JSONNode RetrieveRequiredNode(JSONNode node, string key)
    {
        if (!node.HasKey(key)) throw new ArgumentException($"{GetType().Name} missing required node \"{key}\".");
        return node[key];
    }

    /// <summary>
    ///     Determines if this object is found to be conflicting with <paramref name="other" />.
    /// </summary>
    /// <param name="other">Other object to check if they're conflicting.</param>
    /// <returns>Whether or not they are conflicting with each other.</returns>
    public virtual bool IsConflictingWith(BeatmapObject other, bool deletion = false)
    {
        if (Mathf.Abs(Time - other.Time) < BeatmapObjectContainerCollection.Epsilon)
            return IsConflictingWithObjectAtSameTime(other, deletion);
        return false;
    }

    public override string ToString() => ConvertToJson().ToString();

    /*public override bool Equals(object obj) // We do not need Equals anymore since IsConflictingWith exists
    {
        if (obj is BeatmapObject other)
        {
            return ConvertToJSON().ToString() == other.ConvertToJSON().ToString();
        }
        return false;
    }*/
    public virtual void Apply(BeatmapObject originalData)
    {
        Time = originalData.Time;
        CustomData = originalData.CustomData?.Clone();
    }

    public JSONNode GetOrCreateCustomData()
    {
        if (CustomData == null)
            CustomData = new JSONObject();

        return CustomData;
    }
}
