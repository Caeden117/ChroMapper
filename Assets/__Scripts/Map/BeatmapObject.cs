using SimpleJSON;
using System;
using System.Security.Cryptography.X509Certificates;

public abstract class BeatmapObject {

    protected static int decimalPrecision
    {
        get
        {
#if UNITY_EDITOR
            return 3;
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

    protected abstract bool IsConflictingWithObjectAtSameTime(BeatmapObject other);

    /// <summary>
    /// Create an identical, yet not exact, copy of a given <see cref="BeatmapObject"/>.
    /// </summary>
    /// <typeparam name="T">Specific type of BeatmapObject (Note, event, etc.)</typeparam>
    /// <param name="originalData">Original object to clone.</param>
    /// <returns>A clone of <paramref name="originalData"/>.</returns>
    public static T GenerateCopy<T>(T originalData) where T : BeatmapObject
    {
        if (originalData is null) throw new ArgumentException("originalData is null.");
        T objectData = Activator.CreateInstance(originalData.GetType(), new object[] { originalData.ConvertToJSON() }) as T;
        if (originalData._customData != null) objectData._customData = originalData._customData.Clone();
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
    public virtual bool IsConflictingWith(BeatmapObject other)
    {
        if (_time == other._time)
        {
            return IsConflictingWithObjectAtSameTime(other);
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
}
