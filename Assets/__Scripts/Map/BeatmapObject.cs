using SimpleJSON;

public abstract class BeatmapObject {

    public enum Type {
        NOTE,
        EVENT,
        OBSTACLE,
        CUSTOM_NOTE,
        CUSTOM_EVENT,
        BPM_CHANGE,
    }

    public abstract float _time { get; set; }
    public abstract Type beatmapType { get; set; }
    public abstract JSONNode _customData { get; set; }

    public abstract JSONNode ConvertToJSON();

    public static T GenerateCopy<T>(T originalData) where T : BeatmapObject
    {
        T objectData = null;
        switch (originalData.beatmapType)
        {
            case Type.NOTE:
                objectData = new BeatmapNote(originalData.ConvertToJSON()) as T;
                break;
            case Type.CUSTOM_NOTE:
                objectData = new BeatmapNote(originalData.ConvertToJSON()) as T;
                break;
            case Type.OBSTACLE:
                objectData = new BeatmapObstacle(originalData.ConvertToJSON()) as T;
                break;
            case Type.EVENT:
                objectData = new MapEvent(originalData.ConvertToJSON()) as T;
                break;
            case Type.CUSTOM_EVENT:
                objectData = new BeatmapCustomEvent(originalData.ConvertToJSON()) as T;
                break;
        }
        //The JSONObject somehow stays behind even after this, so we're going to have to parse a new one from the original
        if (originalData._customData != null)
            objectData._customData = JSON.Parse(originalData._customData.ToString());
        return objectData;
    }
}
