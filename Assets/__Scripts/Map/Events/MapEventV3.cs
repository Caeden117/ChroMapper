using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapEventV3 : MapEvent
{
    public float B { get => Time; set => Time = value; }
    public int Et { get => Type; set => Type = value; }
    public int I { get => Value; set => Value = value; }
    public float F = 1.0f;

    public MapEventV3(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        Et = RetrieveRequiredNode(node, "et").AsInt;
        I = RetrieveRequiredNode(node, "i").AsInt;
        F = RetrieveRequiredNode(node, "f").AsFloat;
        CustomData = node["_customData"];
        if (node["_customData"]["_lightGradient"] != null)
            LightGradient = new ChromaGradient(node["_customData"]["_lightGradient"]);
    }

    public MapEventV3(MapEvent m) :
        base(m.Time, m.Type, m.Value, m.CustomData)
    {
    }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(B, DecimalPrecision);
        node["et"] = Et;
        node["i"] = I;
        node["f"] = F;
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
