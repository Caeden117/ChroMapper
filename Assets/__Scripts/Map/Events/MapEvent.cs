using SimpleJSON;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class MapEvent : BeatmapObject {

    /*
     * Event Type constants
     */

    public const int EVENT_TYPE_BACK_LASERS = 0;
    public const int EVENT_TYPE_RING_LIGHTS = 1;
    public const int EVENT_TYPE_LEFT_LASERS = 2;
    public const int EVENT_TYPE_RIGHT_LASERS = 3;
    public const int EVENT_TYPE_ROAD_LIGHTS = 4;
    public const int EVENT_TYPE_BOOST_LIGHTS = 5;
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

    public static bool IsBlueEventFromValue(int _value)
    {
        return _value == LIGHT_VALUE_BLUE_ON || _value == LIGHT_VALUE_BLUE_FLASH || _value == LIGHT_VALUE_BLUE_FADE;
    }

    /*
     * MapEvent logic
     */

    public MapEvent(JSONNode node) {
        _time = RetrieveRequiredNode(node, "_time").AsFloat; //KIIIIWIIIIII
        _type = RetrieveRequiredNode(node, "_type").AsInt;
        _value = RetrieveRequiredNode(node, "_value").AsInt;
        _customData = node["_customData"];
        if (node["_customData"]["_lightGradient"] != null)
        {
            _lightGradient = new ChromaGradient(node["_customData"]["_lightGradient"]);
        }
    }

    public MapEvent(float time, int type, int value, JSONNode customData = null) {
        _time = time;
        _type = type;
        _value = value;
        _customData = customData;

        if (_customData != null && _customData.HasKey("_lightGradient"))
        {
            _lightGradient = new ChromaGradient(_customData["_lightGradient"]);
        }
    }

    public int? GetRotationDegreeFromValue()
    {   //Mapping Extensions precision rotation from 1000 to 1720: 1000 = -360 degrees, 1360 = 0 degrees, 1720 = 360 degrees
        int val = _customData != null && _customData.HasKey("_queuedRotation") ? _customData["_queuedRotation"].AsInt : _value;
        if (val >= 0 && val < LIGHT_VALUE_TO_ROTATION_DEGREES.Length) return LIGHT_VALUE_TO_ROTATION_DEGREES[val];
        else if (val >= 1000 && val <= 1720) return val - 1360;
        return null;
    }

    public Vector2? GetPosition(CreateEventTypeLabels labels, EventsContainer.PropMode mode, int prop)
    {
        if (mode == EventsContainer.PropMode.Off)
        {
            return new Vector2(
                labels.EventTypeToLaneId(_type) + 0.5f,
                0.5f
            );
        }

        if (_type != prop)
        {
            return null;
        }
        
        var key = EventsContainer.GetKeyForProp(mode);
        if (_customData != null &&
            _customData.Count > 0 &&
            _customData.HasKey(key)
            && _customData[key].IsNumber)
        {
            var p = _customData[key].AsInt;
            var x = mode == EventsContainer.PropMode.Prop ? labels.GameToEditorPropID(_type, p) : labels.GameToEditorLightID(_type, p);

            return new Vector2(
                (x < 0 ? p : x) + 1.5f,
                0.5f
            );
        }

        return new Vector2(
            0.5f,
            0.5f
        );
    }

    public bool IsRotationEvent => _type == EVENT_TYPE_EARLY_ROTATION || _type == EVENT_TYPE_LATE_ROTATION;
    public bool IsRingEvent => _type == EVENT_TYPE_RINGS_ROTATE || _type == EVENT_TYPE_RINGS_ZOOM;
    public bool IsLaserSpeedEvent => _type == EVENT_TYPE_LEFT_LASERS_SPEED || _type == EVENT_TYPE_RIGHT_LASERS_SPEED;
    public bool IsUtilityEvent => IsRotationEvent || IsRingEvent || IsLaserSpeedEvent || _type == EVENT_TYPE_BOOST_LIGHTS;
    public bool IsLegacyChromaEvent => _value >= ColourManager.RGB_INT_OFFSET;
    public bool IsChromaEvent => (_customData?.HasKey("_color") ?? false);
    public bool IsPropogationEvent => _customData?.HasKey("_propID") ?? false;
    public int PropId => _customData["_propID"].AsInt;
    public bool IsLightIdEvent => _customData?.HasKey("_lightID") ?? false;
    public int LightId => _customData["_lightID"].AsInt;

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_type"] = _type;
        node["_value"] = _value;
        if (_customData != null)
        {
            node["_customData"] = _customData;
            if (_lightGradient != null)
            {
                JSONNode lightGradient = _lightGradient.ToJSONNode();
                if (lightGradient != null && lightGradient.Children.Count() > 0)
                {
                    node["_customData"]["_lightGradient"] = lightGradient;
                }
            }
        }
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (other is MapEvent @event)
        {
            var propId = IsPropogationEvent ? (int?)PropId : null;
            var otherPropId = @event.IsPropogationEvent ? (int?)@event.PropId : null;
            
            var lightId = IsLightIdEvent ? (int?)LightId : null;
            var otherLightId = @event.IsLightIdEvent ? (int?)@event.LightId : null;

            return _type == @event._type && propId == otherPropId && lightId == otherLightId;
        }
        return false;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is MapEvent obs)
        {
            _type = obs._type;
            _value = obs._value;
            _lightGradient = obs._lightGradient?.Clone();
        }
    }

    public override Type beatmapType { get; set; } = Type.EVENT;
    public int _type;
    public int _value;
    public ChromaGradient _lightGradient = null;

    [Serializable]
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
                throw new ArgumentException("Gradient object must have a end color named \"_endColor\"");
            Duration = gradientObject?["_duration"] ?? 0;
            StartColor = gradientObject["_startColor"];
            EndColor = gradientObject["_endColor"];
            if (gradientObject.HasKey("_easing"))
            {
                if (!Easing.byName.ContainsKey(gradientObject["_easing"]))
                {
                    throw new ArgumentException("Gradient object contains invalid easing type.");
                }
                EasingType = gradientObject["_easing"];
            }
            else
            {
                EasingType = "easeLinear";
            }
        }

        public ChromaGradient(Color start, Color end, float duration = 1, string easing = "easeLinear")
        {
            StartColor = start;
            EndColor = end;
            Duration = duration;
            EasingType = easing;
        }

        public ChromaGradient Clone() => new ChromaGradient(StartColor, EndColor, Duration, EasingType);

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
