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

    // public bool IsControlLight => Type >= 0 && Type <= 4;
    public bool IsControlLight => !IsUtilityEvent; // assume there is no other event type...
    public override bool IsChromaEvent => CustomData?.HasKey("color") ?? false;
    public override bool IsLightIdEvent => CustomData?.HasKey("lightID") ?? false;
    public override int[] LightId => !CustomData["lightID"].IsArray
        ? new[] { CustomData["lightID"].AsInt }
        : CustomData["lightID"].AsArray.Linq.Select(x => x.Value.AsInt).ToArray();

    public MapEventV3 Next = null;

    public MapEventV3(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        EventType = RetrieveRequiredNode(node, "et").AsInt;
        Value = RetrieveRequiredNode(node, "i").AsInt;
        FloatValue = RetrieveRequiredNode(node, "f").AsFloat;
        CustomData = node[BeatmapObjectV3CustomDataKey];
    }

    public MapEventV3(MapEvent m) :
        base(m.Time, m.Type, m.Value, m.CustomData, m.FloatValue)
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
        }
        return node;
    }

    public override JSONNode CustomColor
    {
        get => CustomData?["color"];
        set => CustomData["color"] = value;
    }
    public override JSONNode CustomLightID
    {
        get => CustomData?["lightID"];
        set => CustomData["lightID"] = value;
    }
    public override JSONNode CustomSpeed
    {
        get => CustomData?["speed"];
        set => CustomData["speed"] = value;
    }
    public override JSONNode CustomLockRotation
    {
        get => CustomData?["lockRotation"];
        set => CustomData["lockRotation"] = value;
    }
    public override JSONNode CustomDirection
    {
        get => CustomData?["direction"];
        set => CustomData["direction"] = value;
    }
}
