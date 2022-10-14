using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Chain : BaseChain
    {
        public V3Chain()
        {
        }

        public V3Chain(BaseChain other) : base(other) => ParseCustom();

        public V3Chain(BaseNote start, BaseNote end) : base(start, end) => ParseCustom();

        public V3Chain(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            Color = RetrieveRequiredNode(node, "c").AsInt;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            PosY = RetrieveRequiredNode(node, "y").AsInt;
            CutDirection = RetrieveRequiredNode(node, "d").AsInt;
            TailTime = RetrieveRequiredNode(node, "tb").AsFloat;
            TailPosX = RetrieveRequiredNode(node, "tx").AsInt;
            TailPosY = RetrieveRequiredNode(node, "ty").AsInt;
            SliceCount = RetrieveRequiredNode(node, "sc").AsInt;
            Squish = RetrieveRequiredNode(node, "s").AsFloat;
            CustomData = node["customData"];
            ParseCustom();
        }

        public V3Chain(float time, int color, int posX, int posY, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, int sliceCount, float squish,
            JSONNode customData = null) : base(time, color, posX, posY, cutDirection, angleOffset, tailTime,
            tailPosX, tailPosY, sliceCount, squish, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinate";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeyTailCoordinate { get; } = "tailCoordinate";

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override bool IsChroma() =>
            (CustomData?["color"] != null && CustomData["color"].IsArray) ||
            (CustomData?["spawnEffect"] != null && CustomData["spawnEffect"].IsBoolean) ||
            (CustomData?["disableDebris"] != null && CustomData["disableDebris"].IsBoolean);

        public override bool IsNoodleExtensions() =>
            (CustomData?["animation"] != null && CustomData["animation"].IsArray) ||
            (CustomData?["disableNoteGravity"] != null && CustomData["disableNoteGravity"].IsBoolean) ||
            (CustomData?["disableNoteLook"] != null && CustomData["disableNoteLook"].IsBoolean) ||
            (CustomData?["flip"] != null && CustomData["flip"].IsArray) ||
            (CustomData?["uninteractable"] != null && CustomData["uninteractable"].IsBoolean) ||
            (CustomData?["localRotation"] != null && CustomData["localRotation"].IsArray) ||
            (CustomData?["noteJumpMovementSpeed"] != null && CustomData["noteJumpMovementSpeed"].IsNumber) ||
            (CustomData?["noteJumpStartBeatOffset"] != null &&
             CustomData["noteJumpStartBeatOffset"].IsNumber) ||
            (CustomData?["coordinates"] != null && CustomData["coordinates"].IsArray) ||
            (CustomData?["worldRotation"] != null &&
             (CustomData["worldRotation"].IsArray || CustomData["worldRotation"].IsNumber)) ||
            (CustomData?["track"] != null && CustomData["track"].IsString);

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 ||
             (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360)) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["c"] = Color;
            node["x"] = PosX;
            node["y"] = PosY;
            node["d"] = CutDirection;
            node["tb"] = TailTime;
            node["tx"] = TailPosX;
            node["ty"] = TailPosY;
            node["sc"] = SliceCount;
            node["s"] = Squish;
            CustomData = SaveCustom();
            if (CustomData.Count == 0) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3Chain(Time, Color, PosX, PosY, CutDirection, AngleOffset,
                TailTime, TailPosX, TailPosY, SliceCount, Squish, CustomData?.Clone());
    }
}
