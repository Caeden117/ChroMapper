using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapEventV3 : MapEvent
{
    //public float Time { get => base.Time; set => base.Time = value; }
    public int EventType { get => Type; set => Type = value; }
    //public int Value { get => base.Value; set => base.Value = value; }
    public float FloatValue = 1.0f;

    public MapEventV3(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        EventType = RetrieveRequiredNode(node, "et").AsInt;
        Value = RetrieveRequiredNode(node, "i").AsInt;
        FloatValue = RetrieveRequiredNode(node, "f").AsFloat;
        CustomData = node[BeatmapObjectV3CustomDataKey];
        if (node[BeatmapObjectV3CustomDataKey]["_lightGradient"] != null)
            LightGradient = new ChromaGradient(node[BeatmapObjectV3CustomDataKey]["_lightGradient"]);
    }

    public MapEventV3(MapEvent m) :
        base(m.Time, m.Type, m.Value, m.CustomData)
    {
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["et"] = EventType;
        node["i"] = Value;
        node["f"] = FloatValue;
        if (CustomData != null)
        {
            node[BeatmapObjectV3CustomDataKey] = CustomData;
            if (LightGradient != null)
            {
                var lightGradient = LightGradient.ToJsonNode();
                if (lightGradient != null && lightGradient.Children.Count() > 0)
                    node[BeatmapObjectV3CustomDataKey]["_lightGradient"] = lightGradient;
            }
        }
        return node;
    }
}
