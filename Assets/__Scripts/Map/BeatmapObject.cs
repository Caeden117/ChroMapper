using SimpleJSON;
using System;

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

    public float _time;
    public abstract Type beatmapType { get; set; }
    public virtual JSONNode _customData { get; set; }

    public abstract JSONNode ConvertToJSON();

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

    public override string ToString() => ConvertToJSON().ToString();
}
