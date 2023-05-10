using System;
using System.Linq;
using Beatmap.Base;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Arc : BaseArc, V3Object
    {
        public V3Arc()
        {
        }

        public V3Arc(BaseArc other) : base(other) => ParseCustom();

        public V3Arc(BaseNote start, BaseNote end) : base(start, end) => ParseCustom();

        public V3Arc(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "b").AsFloat;
            Color = RetrieveRequiredNode(node, "c").AsInt;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            PosY = RetrieveRequiredNode(node, "y").AsInt;
            CutDirection = RetrieveRequiredNode(node, "d").AsInt;
            HeadControlPointLengthMultiplier = RetrieveRequiredNode(node, "mu").AsFloat;
            TailJsonTime = RetrieveRequiredNode(node, "tb").AsFloat;
            TailPosX = RetrieveRequiredNode(node, "tx").AsInt;
            TailPosY = RetrieveRequiredNode(node, "ty").AsInt;
            TailCutDirection = RetrieveRequiredNode(node, "tc").AsInt;
            TailControlPointLengthMultiplier = RetrieveRequiredNode(node, "tmu").AsFloat;
            MidAnchorMode = RetrieveRequiredNode(node, "m").AsInt;
            CustomData = node["customData"];
            ParseCustom();
        }

        public V3Arc(float time, int posX, int posY, int color, int cutDirection, float mult,
            float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult, int midAnchorMode,
            JSONNode customData = null) : base(time, posX, posY, color, cutDirection, 0, mult,
            tailTime, tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData) =>
            ParseCustom();

        public V3Arc(float time, int posX, int posY, int color, int cutDirection, int angleOffset, float mult,
            float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult, int midAnchorMode,
            JSONNode customData = null) : base(time, posX, posY, color, cutDirection, angleOffset, mult,
            tailTime, tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData) =>
            ParseCustom();

        public V3Arc(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset, float mult,
            float tailJsonTime, float tailSongBpmTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult, int midAnchorMode,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, color, cutDirection, angleOffset, mult,
            tailJsonTime, tailSongBpmTime, tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinates";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeyTailCoordinate { get; } = "tailCoordinates";
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override bool IsChroma() =>
            CustomData != null &&
            ((CustomData.HasKey("color") && CustomData["color"].IsArray) ||
             (CustomData.HasKey("spawnEffect") && CustomData["spawnEffect"].IsBoolean) ||
             (CustomData.HasKey("disableDebris") && CustomData["disableDebris"].IsBoolean));

        public override bool IsNoodleExtensions() =>
            CustomData != null &&
            ((CustomData.HasKey("animation") && CustomData["animation"].IsArray) ||
             (CustomData.HasKey("disableNoteGravity") && CustomData["disableNoteGravity"].IsBoolean) ||
             (CustomData.HasKey("disableNoteLook") && CustomData["disableNoteLook"].IsBoolean) ||
             (CustomData.HasKey("flip") && CustomData["flip"].IsArray) ||
             (CustomData.HasKey("uninteractable") && CustomData["uninteractable"].IsBoolean) ||
             (CustomData.HasKey("localRotation") && CustomData["localRotation"].IsArray) ||
             (CustomData.HasKey("noteJumpMovementSpeed") && CustomData["noteJumpMovementSpeed"].IsNumber) ||
             (CustomData.HasKey("noteJumpStartBeatOffset") && CustomData["noteJumpStartBeatOffset"].IsNumber) ||
             (CustomData.HasKey("coordinates") && CustomData["coordinates"].IsArray) ||
             (CustomData.HasKey("worldRotation") &&
              (CustomData["worldRotation"].IsArray || CustomData["worldRotation"].IsNumber)) ||
             (CustomData.HasKey("track") && CustomData["track"].IsString));

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 ||
             (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360) ||
             (TailCutDirection >= 1000 && TailCutDirection <= 1360)) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(JsonTime, DecimalPrecision);
            node["c"] = Color;
            node["x"] = PosX;
            node["y"] = PosY;
            node["d"] = CutDirection;
            node["mu"] = HeadControlPointLengthMultiplier;
            node["tb"] = TailJsonTime;
            node["tx"] = TailPosX;
            node["ty"] = TailPosY;
            node["tc"] = TailCutDirection;
            node["tmu"] = TailControlPointLengthMultiplier;
            node["m"] = MidAnchorMode;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3Arc(JsonTime, SongBpmTime, PosX, PosY, Color, CutDirection, AngleOffset,
                HeadControlPointLengthMultiplier, TailJsonTime, TailSongBpmTime, TailPosX, TailPosY,
                TailCutDirection, TailControlPointLengthMultiplier,
                MidAnchorMode, SaveCustom().Clone());
    }
}
