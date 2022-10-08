using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Note : INote
    {
        public V2Note()
        {
        }

        public V2Note(INote other) : base(other) => ParseCustom();

        public V2Note(IBombNote bomb) : base(bomb)
        {
        }

        public V2Note(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "_time").AsFloat;
            PosX = RetrieveRequiredNode(node, "_lineIndex").AsInt;
            PosY = RetrieveRequiredNode(node, "_lineLayer").AsInt;
            Type = RetrieveRequiredNode(node, "_type").AsInt;
            CutDirection = RetrieveRequiredNode(node, "_cutDirection").AsInt;
            CustomData = node["_customData"];
            InferColor();
            ParseCustom();
        }

        public V2Note(float time, int posX, int posY, int type, int cutDirection, JSONNode customData = null) : base(
            time,
            posX, posY, type, cutDirection, customData) =>
            ParseCustom();

        public sealed override void ParseCustom()
        {
            base.ParseCustom();
            
            if (CustomData == null) return;
            if (CustomData[CustomKeyDirection] != null) CustomDirection = CustomData[CustomKeyDirection].AsInt;
        }

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeyDirection { get; } = "_cutDirection";

        public override bool IsChroma() =>
            (CustomData?["_color"] != null && CustomData["_color"].IsArray) ||
            (CustomData?["_disableSpawnEffect"] != null && CustomData["_disableSpawnEffect"].IsBoolean);

        public override bool IsNoodleExtensions() =>
            (CustomData?["_animation"] != null && CustomData["_animation"].IsArray) ||
            (CustomData?["_cutDirection"] != null && CustomData["_cutDirection"].IsNumber) ||
            (CustomData?["_disableNoteGravity"] != null && CustomData["_disableNoteGravity"].IsBoolean) ||
            (CustomData?["_disableNoteLook"] != null && CustomData["_disableNoteLook"].IsBoolean) ||
            (CustomData?["_flip"] != null && CustomData["_flip"].IsArray) ||
            (CustomData?["_fake"] != null && CustomData["_fake"].IsBoolean) ||
            (CustomData?["_interactable"] != null && CustomData["_interactable"].IsBoolean) ||
            (CustomData?["_localRotation"] != null && CustomData["_localRotation"].IsArray) ||
            (CustomData?["_noteJumpMovementSpeed"] != null && CustomData["_noteJumpMovementSpeed"].IsNumber) ||
            (CustomData?["_noteJumpStartBeatOffset"] != null &&
             CustomData["_noteJumpStartBeatOffset"].IsNumber) ||
            (CustomData?["_position"] != null && CustomData["_position"].IsArray) ||
            (CustomData?["_rotation"] != null &&
             (CustomData["_rotation"].IsArray || CustomData["_rotation"].IsNumber)) ||
            (CustomData?["_track"] != null && CustomData["_track"].IsString);

        public override bool IsMappingExtensions() =>
            (PosX < 0 || PosX > 3 || PosY < 0 || PosY > 2 || (CutDirection >= 1000 && CutDirection <= 1360)) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(Time, DecimalPrecision);
            node["_lineIndex"] = PosX;
            node["_lineLayer"] = PosY;
            node["_type"] = Type;
            node["_cutDirection"] = CutDirection;
            if (CustomData != null) node["_customData"] = CustomData;
            return node;
        }

        public override IItem Clone() => new V2Note(Time, PosX, PosY, Type, CutDirection, CustomData?.Clone());
    }
}
