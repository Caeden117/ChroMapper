using SimpleJSON;
using System.Collections.Generic;
using System.Globalization;

[System.Serializable]
public class MapEvent : BeatmapObject {

    /*
     * Event Type constants
     */

    public const int EVENT_TYPE_BACK_LASERS = 0;
    public const int EVENT_TYPE_RING_LIGHTS = 1;
    public const int EVENT_TYPE_LEFT_LASERS = 2;
    public const int EVENT_TYPE_RIGHT_LASERS = 3;
    public const int EVENT_TYPE_ROAD_LIGHTS = 4;
    //5
    //6
    //7
    public const int EVENT_TYPE_RINGS_ROTATE = 8;
    public const int EVENT_TYPE_RINGS_ZOOM = 9;
    //10
    //11
    public const int EVENT_TYPE_LEFT_LASERS_SPEED = 12;
    public const int EVENT_TYPE_RIGHT_LASERS_SPEED = 13;
    public const int MEDIOCRE_MAPPER_BPM_ADJUST = 14;

    /*
     * Light value constants
     */

    public const int LIGHT_VALUE_OFF = 0;

    public const int LIGHT_VALUE_BLUE_ON = 1;
    public const int LIGHT_VALUE_BLUE_FLASH = 2;
    public const int LIGHT_VALUE_BLUE_FADE = 3;

    public const int LIGHT_VALUE_RED_ON = 5;
    public const int LIGHT_VALUE_RED_FLASH = 6;
    public const int LIGHT_VALUE_RED_FADE = 7;



    /*
     * MapEvent logic
     */

    public MapEvent(JSONNode node) {
        _time = node["_time"].AsFloat; //KIIIIWIIIIII
        _type = node["_type"].AsInt;
        _value = node["_value"].AsInt;
        _customData = node["_customData"];
    }

    public MapEvent(float time, int type, int value) {
        _time = time;
        _type = type;
        _value = value;
    }

    public bool IsUtilityEvent()
    {
        List<int> UtilityIDS = new List<int>() { EVENT_TYPE_LEFT_LASERS_SPEED, EVENT_TYPE_RIGHT_LASERS_SPEED,
        EVENT_TYPE_RINGS_ROTATE, EVENT_TYPE_RINGS_ZOOM};
        if (UtilityIDS.Contains(_type)) return true;
        return false;
    }

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = _time.ToString("G", CultureInfo.InvariantCulture);
        node["_type"] = _type;
        node["_value"] = _value;
        node["_customData"] = _customData;
        return node;
    }

    public override float _time { get; set; }
    public override Type beatmapType { get; set; } = Type.EVENT;
    public override JSONNode _customData { get; set; }
    public int _type = 0;
    public int _value = 0;
}
