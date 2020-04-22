using SimpleJSON;
using UnityEngine;
using System;

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
    public const int EVENT_TYPE_CUSTOM_LIGHT_1 = 5;
    public const int EVENT_TYPE_CUSTOM_LIGHT_2 = 6;
    public const int EVENT_TYPE_CUSTOM_LIGHT_3 = 7;
    public const int EVENT_TYPE_RINGS_ROTATE = 8;
    public const int EVENT_TYPE_RINGS_ZOOM = 9;
    public const int EVENT_TYPE_CUSTOM_LIGHT_4 = 10;
    public const int EVENT_TYPE_CUSTOM_LIGHT_5 = 11;
    public const int EVENT_TYPE_LEFT_LASERS_SPEED = 12;
    public const int EVENT_TYPE_RIGHT_LASERS_SPEED = 13;
    public const int EVENT_TYPE_EARLY_ROTATION = 14;
    public const int EVENT_TYPE_LATE_ROTATION = 15;

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

    public static readonly int[] LIGHT_VALUE_TO_ROTATION_DEGREES = { -60, -45, -30, -15, 15, 30, 45, 60 };

    /*
     * MapEvent logic
     */

    public MapEvent(JSONNode node) {
        _time = node["_time"].AsFloat; //KIIIIWIIIIII
        _type = node["_type"].AsInt;
        _value = node["_value"].AsInt;
        _customData = node["_customData"];
        if (node["_customData"]["_lightGradient"] != null)
        {
            _lightGradient = new ChromaGradient(node["_customData"]["_lightGradient"]);
        }
    }

    public MapEvent(float time, int type, int value) {
        _time = time;
        _type = type;
        _value = value;
    }

    public int? GetRotationDegreeFromValue()
    {   //Mapping Extensions precision rotation from 1000 to 1720: 1000 = -360 degrees, 1360 = 0 degrees, 1720 = 360 degrees
        if (_value >= 0 && _value < LIGHT_VALUE_TO_ROTATION_DEGREES.Length) return LIGHT_VALUE_TO_ROTATION_DEGREES[_value];
        else if (_value >= 1000 && _value <= 1720)  return _value - 1360;
        return null;
    }

    public bool IsRotationEvent => _type == EVENT_TYPE_EARLY_ROTATION || _type == EVENT_TYPE_LATE_ROTATION;
    public bool IsRingEvent => _type == EVENT_TYPE_RINGS_ROTATE || _type == EVENT_TYPE_RINGS_ZOOM;
    public bool IsLaserSpeedEvent => _type == EVENT_TYPE_LEFT_LASERS_SPEED || _type == EVENT_TYPE_RIGHT_LASERS_SPEED;
    public bool IsUtilityEvent => IsRotationEvent || IsRingEvent || IsLaserSpeedEvent;
    public bool IsChromaEvent => _value >= ColourManager.RGB_INT_OFFSET || _customData?["_color"] != null;

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, 3);
        node["_type"] = _type;
        node["_value"] = _value;
        if (_customData != null)
        {
            node["_customData"] = _customData;
            if (_lightGradient != null)
            {
                node["_customData"]["_lightGradient"] = _lightGradient.ToJSONNode();
            }
        }
        return node;
    }

    public override Type beatmapType { get; set; } = Type.EVENT;
    public int _type;
    public int _value;
    public ChromaGradient _lightGradient = null;

    public class ChromaGradient
    {
        public float Duration = 0;
        public Color StartColor;
        public Color EndColor;
        public string EasingType;

        public ChromaGradient(JSONNode gradientObject)
        {
            if (gradientObject["_startColor"] == null)
                throw new ArgumentException("Gradient object must have a start color named \"_startColor\"");
            if (gradientObject["_endColor"] == null)
                throw new ArgumentException("Gradient object must have a start color named \"_endColor\"");
            if (gradientObject["_easing"] == null)
                throw new ArgumentException("Gradient object must have a valid easing type named \"_easing\"");
            Duration = gradientObject?["_duration"] ?? 0;
            StartColor = gradientObject["_startColor"];
            EndColor = gradientObject["_endColor"];
            EasingType = gradientObject["_easing"];
        }

        public JSONNode ToJSONNode()
        {
            JSONObject obj = new JSONObject();
            obj["_duration"] = Duration;
            obj["_startColor"] = StartColor;
            obj["_endColor"] = EndColor;
            obj["_easing"] = EasingType;
            return obj;
        }
    }
}
