using System;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class MapEvent : BeatmapObject
{
    /*
     * Event Type constants
     */

    public const int EventTypeBackLasers = 0;
    public const int EventTypeRingLights = 1;
    public const int EventTypeLeftLasers = 2;
    public const int EventTypeRightLasers = 3;
    public const int EventTypeRoadLights = 4;
    public const int EventTypeBoostLights = 5;
    public const int EventTypeCustomLight2 = 6;
    public const int EventTypeCustomLight3 = 7;
    public const int EventTypeRingsRotate = 8;
    public const int EventTypeRingsZoom = 9;
    public const int EventTypeCustomLight4 = 10;
    public const int EventTypeCustomLight5 = 11;
    public const int EventTypeLeftLasersSpeed = 12;
    public const int EventTypeRightLasersSpeed = 13;
    public const int EventTypeEarlyRotation = 14;
    public const int EventTypeLateRotation = 15;
    public const int EventTypeCustomEvent1 = 16;
    public const int EventTypeCustomEvent2 = 17;

    /*
     * Light value constants
     */

    public const int LightValueOff = 0;

    public const int LightValueBlueON = 1;
    public const int LightValueBlueFlash = 2;
    public const int LightValueBlueFade = 3;

    public const int LightValueRedON = 5;
    public const int LightValueRedFlash = 6;
    public const int LightValueRedFade = 7;

    public static readonly int[] LightValueToRotationDegrees = { -60, -45, -30, -15, 15, 30, 45, 60 };
    public int PropId = -1;
    [FormerlySerializedAs("_type")] public int Type;
    [FormerlySerializedAs("_value")] public int Value;
    [FormerlySerializedAs("_lightGradient")] public ChromaGradient LightGradient;

    /*
     * MapEvent logic
     */
    public MapEvent() { }

    public MapEvent(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "_time").AsFloat; //KIIIIWIIIIII
        Type = RetrieveRequiredNode(node, "_type").AsInt;
        Value = RetrieveRequiredNode(node, "_value").AsInt;
        CustomData = node["_customData"];
        if (node["_customData"]["_lightGradient"] != null)
            LightGradient = new ChromaGradient(node["_customData"]["_lightGradient"]);
    }

    public MapEvent(float time, int type, int value, JSONNode customData = null)
    {
        Time = time;
        Type = type;
        Value = value;
        CustomData = customData;

        if (CustomData != null && CustomData.HasKey("_lightGradient"))
            LightGradient = new ChromaGradient(CustomData["_lightGradient"]);
    }

    public bool IsRotationEvent => Type == EventTypeEarlyRotation || Type == EventTypeLateRotation;
    public bool IsRingEvent => Type == EventTypeRingsRotate || Type == EventTypeRingsZoom;
    public bool IsLaserSpeedEvent => Type == EventTypeLeftLasersSpeed || Type == EventTypeRightLasersSpeed;

    public bool IsUtilityEvent => IsRotationEvent || IsRingEvent || IsLaserSpeedEvent ||
                                  Type == EventTypeBoostLights || IsInterscopeEvent;

    public bool IsInterscopeEvent => Type == EventTypeCustomEvent1 || Type == EventTypeCustomEvent2;
    public bool IsLegacyChromaEvent => Value >= ColourManager.RgbintOffset;
    public bool IsChromaEvent => CustomData?.HasKey("_color") ?? false;
    public bool IsPropogationEvent => PropId > -1; //_customData["_lightID"].IsArray
    public bool IsLightIdEvent => CustomData?.HasKey("_lightID") ?? false;

    public int[] LightId => !CustomData["_lightID"].IsArray
        ? new[] { CustomData["_lightID"].AsInt }
        : CustomData["_lightID"].AsArray.Linq.Select(x => x.Value.AsInt).ToArray();

    public override ObjectType BeatmapType { get; set; } = ObjectType.Event;

    public static bool IsBlueEventFromValue(int value) => value == LightValueBlueON ||
                                                           value == LightValueBlueFlash ||
                                                           value == LightValueBlueFade;

    public int? GetRotationDegreeFromValue()
    {
        //Mapping Extensions precision rotation from 1000 to 1720: 1000 = -360 degrees, 1360 = 0 degrees, 1720 = 360 degrees
        var val = CustomData != null && CustomData.HasKey("_queuedRotation")
            ? CustomData["_queuedRotation"].AsInt
            : Value;
        if (val >= 0 && val < LightValueToRotationDegrees.Length) return LightValueToRotationDegrees[val];
        if (val >= 1000 && val <= 1720) return val - 1360;
        return null;
    }

    public Vector2? GetPosition(CreateEventTypeLabels labels, EventsContainer.PropMode mode, int prop)
    {
        if (IsLightIdEvent) PropId = labels.LightIdsToPropId(Type, LightId) ?? -1;

        if (mode == EventsContainer.PropMode.Off)
        {
            return new Vector2(
                labels.EventTypeToLaneId(Type) + 0.5f,
                0.5f
            );
        }

        if (Type != prop) return null;

        if (IsLightIdEvent)
        {
            var x = mode == EventsContainer.PropMode.Prop ? PropId : -1;

            if (x < 0)
                x = LightId.Length > 0 ? labels.LightIDToEditor(Type, LightId[0]) : -1;

            return new Vector2(
                x + 1.5f,
                0.5f
            );
        }

        return new Vector2(
            0.5f,
            0.5f
        );
    }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(Time, DecimalPrecision);
        node["_type"] = Type;
        node["_value"] = Value;
        if (CustomData != null)
        {
            node["_customData"] = CustomData;
            if (LightGradient != null)
            {
                var lightGradient = LightGradient.ToJsonNode();
                if (lightGradient != null && lightGradient.Children.Count() > 0)
                    node["_customData"]["_lightGradient"] = lightGradient;
            }
        }

        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (other is MapEvent @event)
        {
            var lightId = IsLightIdEvent ? LightId : null;
            var otherLightId = @event.IsLightIdEvent ? @event.LightId : null;
            var lightIdEquals = lightId?.Length == otherLightId?.Length &&
                                (lightId == null || lightId.All(x => otherLightId.Contains(x)));

            return Type == @event.Type && lightIdEquals;
        }

        return false;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is MapEvent obs)
        {
            Type = obs.Type;
            Value = obs.Value;
            LightGradient = obs.LightGradient?.Clone();
        }
    }

    [Serializable]
    public class ChromaGradient
    {
        public float Duration;
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
                if (!Easing.ByName.ContainsKey(gradientObject["_easing"]))
                    throw new ArgumentException("Gradient object contains invalid easing type.");
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

        public JSONNode ToJsonNode()
        {
            var obj = new JSONObject();
            obj["_duration"] = Duration;
            obj["_startColor"] = StartColor;
            obj["_endColor"] = EndColor;
            obj["_easing"] = EasingType;
            return obj;
        }
    }
}
