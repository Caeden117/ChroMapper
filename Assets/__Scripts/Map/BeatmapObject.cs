using SimpleJSON;
using System;
using UnityEngine;

public abstract class BeatmapObject {

    protected static int decimalPrecision
    {
        get
        {
#if UNITY_EDITOR
            return 6;
#else
            return Settings.Instance.TimeValueDecimalPrecision;
#endif
        }
    }

    public enum Type {
        NOTE,
        EVENT,
        OBSTACLE,
        CUSTOM_NOTE,
        CUSTOM_EVENT,
        BPM_CHANGE,
    }

    public abstract Type beatmapType { get; set; }

    /// <summary>
    /// Whether or not there exists a <see cref="BeatmapObjectContainer"/> that contains this data.
    /// </summary>
    public bool HasAttachedContainer = false;
    /// <summary>
    /// Time, in beats, where this object is located.
    /// </summary>
    public float _time;
    /// <summary>
    /// An expandable <see cref="JSONNode"/> that stores data for Beat Saber mods to use.
    /// </summary>
    public JSONNode _customData;

    public abstract JSONNode ConvertToJSON();

    protected abstract bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false);

    /// <summary>
    /// Create an identical, yet not exact, copy of a given <see cref="BeatmapObject"/>.
    /// </summary>
    /// <typeparam name="T">Specific type of BeatmapObject (Note, event, etc.)</typeparam>
    /// <param name="originalData">Original object to clone.</param>
    /// <returns>A clone of <paramref name="originalData"/>.</returns>
    public static T GenerateCopy<T>(T originalData) where T : BeatmapObject
    {
        if (originalData is null) throw new ArgumentException("originalData is null.");
        T objectData;
        switch (originalData)
        {
            case MapEvent evt:
                var ev = new MapEvent(evt._time, evt._type, evt._value, originalData._customData?.Clone());
                ev._lightGradient = evt._lightGradient?.Clone();
                objectData = ev as T;
                break;
            case BeatmapNote note:
                objectData = new BeatmapNote(note._time, note._lineIndex, note._lineLayer, note._type, note._cutDirection, originalData._customData?.Clone()) as T;
                break;
            default:
                objectData = Activator.CreateInstance(originalData.GetType(), new object[] { originalData.ConvertToJSON() }) as T;
                objectData._customData = originalData._customData?.Clone();
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
    /// Determines if this object is found to be conflicting with <paramref name="other"/>.
    /// </summary>
    /// <param name="other">Other object to check if they're conflicting.</param>
    /// <returns>Whether or not they are conflicting with each other.</returns>
    public virtual bool IsConflictingWith(BeatmapObject other, bool deletion = false)
    {
        if (Mathf.Abs(_time - other._time) < BeatmapObjectContainerCollection.Epsilon)
        {
            return IsConflictingWithObjectAtSameTime(other, deletion);
        }
        return false;
    }

    public override string ToString() => ConvertToJSON().ToString();

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
        _time = originalData._time;
        _customData = originalData._customData?.Clone();
    }

    public JSONNode GetOrCreateCustomData()
    {
        if (_customData == null)
            _customData = new JSONObject();

        return _customData;
    }
}
