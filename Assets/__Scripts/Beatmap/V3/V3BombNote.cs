using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BombNote : BaseBombNote
    {
        public V3BombNote()
        {
        }

        public V3BombNote(BaseGrid other) : base(other) => ParseCustom();

        public V3BombNote(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            PosY = RetrieveRequiredNode(node, "y").AsInt;
            CustomData = node["customData"];
            ParseCustom();
        }

        public V3BombNote(float time, int posX, int posY, JSONNode customData = null) : base(time, posX, posY,
            customData) =>
            ParseCustom();

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinate";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

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
            (PosX < 0 || PosX > 3 || PosY < 0 || PosY > 2) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["x"] = PosX;
            node["y"] = PosY;
            if (CustomData == null) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3BombNote(Time, PosX, PosY, CustomData?.Clone());
    }
}
