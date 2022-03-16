using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// <see cref="ColorBoostEvent"/> is seperated from basic <see cref="MapEvent"/> in map v3. Boost color could be overwritten by `SongCore`
/// </summary>
public class ColorBoostEvent : MapEvent
{
    //public float Time { get => base.Time; set => base.Time = value; }
    public bool Boost { get => Value == 1; set => Value = value ? 1 : 0; }
    public ColorBoostEvent(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        Boost = RetrieveRequiredNode(node, "o").AsBool;
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
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["o"] = Boost;
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
