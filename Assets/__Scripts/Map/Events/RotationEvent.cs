using System;
using SimpleJSON;
using UnityEngine;
using System.Linq;

/// <summary>
/// <see cref="RotationEvent"/> is seperated from basic <see cref="MapEvent"/> in map v3.
/// </summary>
public class RotationEvent : MapEvent
{
    public int RotationType
    {
        get => (Type == EventTypeEarlyRotation) ? 0 : 1;
        set => Type = (value == 0) ? EventTypeEarlyRotation : EventTypeLateRotation;
    }

    private int degrees;
    public int RotationAmount
    {
        get => degrees;
        set {
            degrees = value;
            var index = Array.IndexOf(LightValueToRotationDegrees, value);
            Value = (index != -1) 
                ? index 
                : value + 1360; // ME value
        }
    }

    public override int? GetRotationDegreeFromValue()
    {
        //Mapping Extensions should have no effect on V3 rotation event
        return (CustomData != null && CustomData.HasKey("_queuedRotation"))
            ? CustomData["_queuedRotation"].AsInt
            : RotationAmount;
    }

    public RotationEvent(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        RotationAmount = RetrieveRequiredNode(node, "r").AsInt;
        RotationType = RetrieveRequiredNode(node, "e").AsInt;
        FloatValue = 1f;
        CustomData = node[BeatmapObjectV3CustomDataKey];
        if (node[BeatmapObjectV3CustomDataKey]["_lightGradient"] != null)
            LightGradient = new ChromaGradient(node[BeatmapObjectV3CustomDataKey]["_lightGradient"]);
    }

    public RotationEvent(MapEvent m) :
        base(m.Time, m.Type, m.Value, m.CustomData)
    {
        if (m is RotationEvent rotEvent)
            RotationAmount = rotEvent.RotationAmount;
        else
        {
            var val = m.Value;
            if (val >= 0 && val < LightValueToRotationDegrees.Length)
                RotationAmount = LightValueToRotationDegrees[val];
            else if (val >= 1000 && val <= 1720)
                RotationAmount = val - 1360;
            else
                RotationAmount = 0;
        }
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["r"] = RotationAmount;
        node["e"] = RotationType;
        if (CustomData != null) node[BeatmapObjectV3CustomDataKey] = CustomData;

        return node;
    }

}
