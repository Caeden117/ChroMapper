using System;
using SimpleJSON;
using UnityEngine;

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
            // This sets it to the corresponding Value closest to the precise rotation
            // (-60, -45, -30, -15, 15, 30, 45, 60)
            if (value <= -53) Value = 0;
            else if (value <= -38) Value = 1;
            else if (value <= -23) Value = 2;
            else if (value <= 0) Value = 3;
            else if (value <= 22) Value = 4;
            else if (value <= 37) Value = 5;
            else if (value <= 52) Value = 6;
            else Value = 7;
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
            switch (m.Value)
            {
                case 0:
                    RotationAmount = -60;
                    break;
                case 1:
                    RotationAmount = -45;
                    break;
                case 2:
                    RotationAmount = -30;
                    break;
                case 3:
                    RotationAmount = -15;
                    break;
                case 4:
                    RotationAmount = 15;
                    break;
                case 5:
                    RotationAmount = 30;
                    break;
                case 6:
                    RotationAmount = 45;
                    break;
                case 7:
                    RotationAmount = 60;
                    break;
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
