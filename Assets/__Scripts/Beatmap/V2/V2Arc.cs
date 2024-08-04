using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Arc
    {
        public const string CustomKeyAnimation = "_animation";

        public const string CustomKeyTrack = "_track";

        public const string CustomKeyColor = "_color";

        public const string CustomKeyCoordinate = "_position";

        public const string CustomKeyWorldRotation = "_rotation";

        public const string CustomKeyLocalRotation = "_localRotation";

        public const string CustomKeySpawnEffect = "_disableSpawnEffect";

        public const string CustomKeyNoteJumpMovementSpeed = "_noteJumpMovementSpeed";

        public const string CustomKeyNoteJumpStartBeatOffset = "_noteJumpStartBeatOffset";

        public const string CustomKeyTailCoordinate = "_tailPosition";

        public static BaseArc GetFromJson(JSONNode node)
        {
            var arc = new BaseArc();
            
            arc.Color = BaseItem.GetRequiredNode(node, "_colorType").AsInt;
            arc.JsonTime = BaseItem.GetRequiredNode(node, "_headTime").AsFloat;
            arc.PosX = BaseItem.GetRequiredNode(node, "_headLineIndex").AsInt;
            arc.PosY = BaseItem.GetRequiredNode(node, "_headLineLayer").AsInt;
            arc.CutDirection = BaseItem.GetRequiredNode(node, "_headCutDirection").AsInt;
            arc.HeadControlPointLengthMultiplier = BaseItem.GetRequiredNode(node, "_headControlPointLengthMultiplier").AsFloat;
            arc.TailJsonTime = BaseItem.GetRequiredNode(node, "_tailTime").AsFloat;
            arc.TailPosX = BaseItem.GetRequiredNode(node, "_tailLineIndex").AsInt;
            arc.TailPosY = BaseItem.GetRequiredNode(node, "_tailLineLayer").AsInt;
            arc.TailCutDirection = BaseItem.GetRequiredNode(node, "_tailCutDirection").AsInt;
            arc.TailControlPointLengthMultiplier = BaseItem.GetRequiredNode(node, "_tailControlPointLengthMultiplier").AsFloat;
            arc.MidAnchorMode = BaseItem.GetRequiredNode(node, "_sliderMidAnchorMode").AsInt;
            arc.CustomData = node["_customData"];
            arc.RefreshCustom();

            return arc;
        }
        
        public static JSONNode ToJson(BaseArc arc)
        {
            JSONNode node = new JSONObject();
            node["_colorType"] = arc.Color;
            node["_headTime"] = arc.JsonTime;
            node["_headLineIndex"] = arc.PosX;
            node["_headLineLayer"] = arc.PosY;
            node["_headCutDirection"] = arc.CutDirection;
            node["_headControlPointLengthMultiplier"] = arc.HeadControlPointLengthMultiplier;
            node["_tailTime"] = arc.TailJsonTime;
            node["_tailLineIndex"] = arc.TailPosX;
            node["_tailLineLayer"] = arc.TailPosY;
            node["_tailCutDirection"] = arc.TailCutDirection;
            node["_tailControlPointLengthMultiplier"] = arc.TailControlPointLengthMultiplier;
            node["_sliderMidAnchorMode"] = arc.MidAnchorMode;
            arc.CustomData = arc.SaveCustom();
            if (!arc.CustomData.Children.Any()) return node;
            node["_customData"] = arc.CustomData;
            return node;
        }
    }
}
