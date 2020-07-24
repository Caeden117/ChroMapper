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

    public bool HasAttachedContainer = false;

    public float _time;
    public abstract Type beatmapType { get; set; }
    public virtual JSONNode _customData { get; set; }

    public abstract JSONNode ConvertToJSON();

    protected abstract bool IsConflictingWithObjectAtSameTime(BeatmapObject other);

    public static T GenerateCopy<T>(T originalData) where T : BeatmapObject
    {
        T objectData = Activator.CreateInstance(originalData.GetType(), new object[] { originalData.ConvertToJSON() }) as T;
        //The JSONObject somehow stays behind even after this, so we're going to have to parse a new one from the original
        if (originalData._customData != null) objectData._customData = JSON.Parse(originalData._customData.ToString());
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

    public override bool Equals(object obj)
    {
        if (obj is BeatmapObject other)
        {
            return ConvertToJSON().ToString() == other.ConvertToJSON().ToString();
        }
        return false;
    }
}
