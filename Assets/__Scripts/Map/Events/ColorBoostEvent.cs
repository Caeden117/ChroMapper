using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class ColorBoostEvent : MapEvent
{
    public float B { get => Time; set => Time = value; }
    public bool O { get => Value == 1; set => Value = value ? 1 : 0; }
    public ColorBoostEvent(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        O = RetrieveRequiredNode(node, "o").AsBool;
        Type = EventTypeBoostLights;
        CustomData = node["_customData"];
        if (node["_customData"]["_lightGradient"] != null)
            LightGradient = new ChromaGradient(node["_customData"]["_lightGradient"]);
    }
    public ColorBoostEvent(MapEvent m) :
        base(m.Time, m.Type, m.Value, m.CustomData)
    {
        Type = EventTypeBoostLights;
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(B, DecimalPrecision);
        node["o"] = O;
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

}
