using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Note : BaseNote, V2Object
    {
        public V2Note() => CustomData = new JSONObject();

        public V2Note(BaseNote other) : base(other) => ParseCustom();

        public V2Note(BaseBombNote baseBomb) : base(baseBomb) => ParseCustom();

        public V2Note(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "_time").AsFloat;
            PosX = RetrieveRequiredNode(node, "_lineIndex").AsInt;
            PosY = RetrieveRequiredNode(node, "_lineLayer").AsInt;
            Type = RetrieveRequiredNode(node, "_type").AsInt;
            CutDirection = RetrieveRequiredNode(node, "_cutDirection").AsInt;
            CustomData = node["_customData"];
            InferColor();
            ParseCustom();
        }

        public V2Note(float time, int posX, int posY, int type, int cutDirection, JSONNode customData = null) : base(
            time, posX, posY, type, cutDirection, customData) =>
            ParseCustom();

        public V2Note(float jsonTime, float songBpmTime, int posX, int posY, int type, int cutDirection,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, type, cutDirection, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeyDirection { get; } = "_cutDirection";

        protected sealed override void ParseCustom()
        {
            base.ParseCustom();

            CustomDirection = (CustomData?.HasKey(CustomKeyDirection) ?? false) ? CustomData?[CustomKeyDirection].AsInt : null;
        }

        protected internal sealed override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomDirection != null) CustomData[CustomKeyDirection] = CustomDirection; else CustomData.Remove(CustomKeyDirection);
            return CustomData;
        }

        public override bool IsChroma() =>
            CustomData != null &&
            ((CustomData.HasKey("_color") && CustomData["_color"].IsArray) ||
             (CustomData.HasKey("_disableSpawnEffect") && CustomData["_disableSpawnEffect"].IsBoolean));

        public override bool IsNoodleExtensions() =>
            CustomData != null &&
            ((CustomData.HasKey("_animation") && CustomData["_animation"].IsArray) ||
             (CustomData.HasKey("_cutDirection") && CustomData["_cutDirection"].IsNumber) ||
             (CustomData.HasKey("_disableNoteGravity") && CustomData["_disableNoteGravity"].IsBoolean) ||
             (CustomData.HasKey("_disableNoteLook") && CustomData["_disableNoteLook"].IsBoolean) ||
             (CustomData.HasKey("_flip") && CustomData["_flip"].IsArray) ||
             (CustomData.HasKey("_fake") && CustomData["_fake"].IsBoolean) ||
             (CustomData.HasKey("_interactable") && CustomData["_interactable"].IsBoolean) ||
             (CustomData.HasKey("_localRotation") && CustomData["_localRotation"].IsArray) ||
             (CustomData.HasKey("_noteJumpMovementSpeed") && CustomData["_noteJumpMovementSpeed"].IsNumber) ||
             (CustomData.HasKey("_noteJumpStartBeatOffset") &&
              CustomData["_noteJumpStartBeatOffset"].IsNumber) ||
             (CustomData.HasKey("_position") && CustomData["_position"].IsArray) ||
             (CustomData.HasKey("_rotation") &&
              (CustomData["_rotation"].IsArray || CustomData["_rotation"].IsNumber)) ||
             (CustomData.HasKey("_track") && CustomData["_track"].IsString));

        public override bool IsMappingExtensions() =>
            (PosX < 0 || PosX > 3 || PosY < 0 || PosY > 2 || (CutDirection >= 1000 && CutDirection <= 1360)) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(JsonTime, DecimalPrecision);
            node["_lineIndex"] = PosX;
            node["_lineLayer"] = PosY;
            node["_type"] = Type;
            node["_cutDirection"] = CutDirection;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V2Note(JsonTime, SongBpmTime, PosX, PosY, Type, CutDirection, SaveCustom().Clone());
    }
}
