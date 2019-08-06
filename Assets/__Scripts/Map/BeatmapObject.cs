using SimpleJSON;

public abstract class BeatmapObject {

    public enum Type {
        NOTE,
        EVENT,
        OBSTACLE,
        BOMB,
        CUSTOM_NOTE,
        CUSTOM_EVENT,
        BPM_CHANGE,
    }

    public abstract float _time { get; set; }
    public abstract Type beatmapType { get; set; }
    public abstract JSONNode _customData { get; set; }
}
