using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3ColorNote : BaseNote
    {
        public V3ColorNote()
        {
        }

        public V3ColorNote(BaseNote other) : base(other) => ParseCustom();

        public V3ColorNote(BaseSlider slider) : base(slider) => ParseCustom();

        public V3ColorNote(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            PosY = RetrieveRequiredNode(node, "y").AsInt;
            AngleOffset = RetrieveRequiredNode(node, "a").AsInt;
            Color = RetrieveRequiredNode(node, "c").AsInt;
            CutDirection = RetrieveRequiredNode(node, "d").AsInt;
            CustomData = node["customData"];
            InferType();
            ParseCustom();
        }

        public V3ColorNote(float time, int posX, int posY, int type, int cutDirection,
            JSONNode customData = null) : base(
            time, posX, posY, type, cutDirection, customData) =>
            ParseCustom();

        public V3ColorNote(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            JSONNode customData = null) : base(
            time, posX, posY, color, cutDirection, angleOffset, customData) =>
            ParseCustom();

        // TODO: deal with custom direction to angle offset
        public override int? CustomDirection
        {
            get => null;
            set { }
        }

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "position";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeyDirection { get; } = "cutDirection";

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
            (PosX < 0 || PosX > 3 || PosY < 0 || PosY > 2 || (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360)) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["x"] = PosX;
            node["y"] = PosY;
            node["a"] = AngleOffset;
            node["c"] = Color;
            node["d"] = CutDirection;
            SaveCustom();
            if (CustomData.Count == 0) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3ColorNote(Time, PosX, PosY, Color, CutDirection, AngleOffset, CustomData?.Clone());
    }
}
