using System;
using SimpleJSON;
using UnityEngine;

/// <summary>Parses scalar and color event values without changing their targets.</summary>
internal static class CustomEventValueParser
{
    public static Func<float, float> GetEasing(JSONNode data) =>
        data["easing"].IsString ? Easing.Named(data["easing"]) : Easing.Linear;

    public static bool TryReadFloat(JSONNode value, out float number, bool allowBoolean = false)
    {
        number = 0;
        if (value == null) return false;
        if (value.IsNumber) number = value.AsFloat;
        else if (allowBoolean && value.IsBoolean) number = value.AsBool ? 1f : 0f;
        else return false;
        return true;
    }

    public static Func<float, float> Float(JSONNode value, bool allowBoolean = false)
    {
        if (TryReadFloat(value, out var number, allowBoolean)) return _ => number;
        if (value == null || value.IsBoolean) return null;
        var points = PointDefinitionParser.Get(value);
        return points == null ? null : points.GetFloat;
    }

    public static Func<float, Color> Color(JSONNode value)
    {
        if (TryReadColor(value, out var color)) return _ => color;
        var points = PointDefinitionParser.Get(value);
        return points == null ? null : points.GetColor;
    }

    public static bool TryReadColor(JSONNode value, out Color color)
    {
        color = UnityEngine.Color.white;
        if (value == null || !value.IsArray || (value.Count != 3 && value.Count != 4)) return false;
        for (var i = 0; i < value.Count; i++)
        {
            if (!value[i].IsNumber) return false;
            color[i] = value[i].AsFloat;
        }
        return true;
    }
}
