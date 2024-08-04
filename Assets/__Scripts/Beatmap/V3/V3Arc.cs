using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Arc
    {
        public const string CustomKeyAnimation = "animation";

        public const string CustomKeyTrack = "track";

        public const string CustomKeyColor = "color";

        public const string CustomKeyCoordinate = "coordinates";

        public const string CustomKeyWorldRotation = "worldRotation";

        public const string CustomKeyLocalRotation = "localRotation";

        public const string CustomKeySpawnEffect = "spawnEffect";

        public const string CustomKeyNoteJumpMovementSpeed = "noteJumpMovementSpeed";

        public const string CustomKeyNoteJumpStartBeatOffset = "noteJumpStartBeatOffset";

        public const string CustomKeyTailCoordinate = "tailCoordinates";

        public static BaseArc GetFromJson(JSONNode node)
        {
            var arc = new BaseArc();
            
            arc.JsonTime = node["b"].AsFloat;
            arc.Color = node["c"].AsInt;
            arc.PosX = node["x"].AsInt;
            arc.PosY = node["y"].AsInt;
            arc.CutDirection = node["d"].AsInt;
            arc.HeadControlPointLengthMultiplier = node["mu"].AsFloat;
            arc.TailJsonTime = node["tb"].AsFloat;
            arc.TailPosX = node["tx"].AsInt;
            arc.TailPosY = node["ty"].AsInt;
            arc.TailCutDirection = node["tc"].AsInt;
            arc.TailControlPointLengthMultiplier = node["tmu"].AsFloat;
            arc.MidAnchorMode = node["m"].AsInt;
            arc.CustomData = node["customData"];
            arc.RefreshCustom();

            return arc;
        }
        
        public static JSONNode ToJson(BaseArc arc)
        {
            JSONNode node = new JSONObject();
            node["b"] = arc.JsonTime;
            node["c"] = arc.Color;
            node["x"] = arc.PosX;
            node["y"] = arc.PosY;
            node["d"] = arc.CutDirection;
            node["mu"] = arc.HeadControlPointLengthMultiplier;
            node["tb"] = arc.TailJsonTime;
            node["tx"] = arc.TailPosX;
            node["ty"] = arc.TailPosY;
            node["tc"] = arc.TailCutDirection;
            node["tmu"] = arc.TailControlPointLengthMultiplier;
            node["m"] = arc.MidAnchorMode;
            arc.CustomData = arc.SaveCustom();
            if (!arc.CustomData.Children.Any()) return node;
            node["customData"] = arc.CustomData;
            return node;
        }
    }
}
