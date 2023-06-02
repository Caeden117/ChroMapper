using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BombNote : BaseBombNote, V3Object
    {
        public V3BombNote()
        {
        }

        public V3BombNote(BaseGrid other) : base(other) => ParseCustom();

        public V3BombNote(JSONNode node)
        {
            JsonTime = node["b"].AsFloat;
            PosX = node["x"].AsInt;
            PosY = node["y"].AsInt;
            Type = (int)NoteType.Bomb;
            CustomData = node["customData"];
            ParseCustom();
        }

        public V3BombNote(JSONNode node, bool fake = false)
            : this(node)
        {
            CustomFake = fake;
        }

        public V3BombNote(float time, int posX, int posY, JSONNode customData = null) : base(time, posX, posY,
            customData) =>
            ParseCustom();

        public V3BombNote(float jsonTime, float songBpmTime, int posX, int posY, JSONNode customData = null)
            : base(jsonTime, songBpmTime, posX, posY, customData) =>
            ParseCustom();

        public override string CustomKeyAnimation { get; } = "animation";

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinates";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeyNoteJumpMovementSpeed { get; } = "noteJumpMovementSpeed";

        public override string CustomKeyNoteJumpStartBeatOffset { get; } = "noteJumpStartBeatOffset";

        public override string CustomKeyDirection { get; } = "direction";

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
             (CustomData.HasKey("noteJumpStartBeatOffset") &&
              CustomData["noteJumpStartBeatOffset"].IsNumber) ||
             (CustomData.HasKey("coordinates") && CustomData["coordinates"].IsArray) ||
             (CustomData.HasKey("worldRotation") &&
              (CustomData["worldRotation"].IsArray || CustomData["worldRotation"].IsNumber)) ||
             (CustomData.HasKey("track") && CustomData["track"].IsString));

        public override bool IsMappingExtensions() =>
            (PosX < 0 || PosX > 3 || PosY < 0 || PosY > 2) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = JsonTime;
            node["x"] = PosX;
            node["y"] = PosY;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3BombNote(JsonTime, SongBpmTime, PosX, PosY, SaveCustom().Clone());
    }
}
