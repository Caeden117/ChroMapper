using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3Chain
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

        public static BaseChain GetFromJson(JSONNode node, bool customFake = false)
        {
            var baseChain = new BaseChain();
            
            baseChain.JsonTime = node["b"].AsFloat;
            baseChain.Color = node["c"].AsInt;
            baseChain.PosX = node["x"].AsInt;
            baseChain.PosY = node["y"].AsInt;
            baseChain.CutDirection = node["d"].AsInt;
            baseChain.TailJsonTime = node["tb"].AsFloat;
            baseChain.TailPosX = node["tx"].AsInt;
            baseChain.TailPosY = node["ty"].AsInt;
            baseChain.SliceCount = node["sc"].AsInt;
            baseChain.Squish = node["s"].AsFloat;

            baseChain.CustomFake = customFake;
            baseChain.CustomData = node["customData"];

            return baseChain;
        }

        public static JSONNode ToJson(BaseChain chain)
        {
            JSONNode node = new JSONObject();
            node["b"] = chain.JsonTime;
            node["c"] = chain.Color;
            node["x"] = chain.PosX;
            node["y"] = chain.PosY;
            node["d"] = chain.CutDirection;
            node["tb"] = chain.TailJsonTime;
            node["tx"] = chain.TailPosX;
            node["ty"] = chain.TailPosY;
            node["sc"] = chain.SliceCount;
            node["s"] = chain.Squish;
            chain.CustomData = chain.SaveCustom();
            if (!chain.CustomData.Children.Any()) return node;
            node["customData"] = chain.CustomData;
            return node;
        }
    }
}
