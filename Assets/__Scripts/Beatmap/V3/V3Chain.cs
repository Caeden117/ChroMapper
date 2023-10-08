using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Chain : BaseChain, V3Object
    {
        public V3Chain()
        {
        }

        public V3Chain(BaseChain other) : base(other) => ParseCustom();

        public V3Chain(BaseNote start, BaseNote end) : base(start, end) => ParseCustom();

        public V3Chain(JSONNode node)
        {
            JsonTime = node["b"].AsFloat;
            Color = node["c"].AsInt;
            PosX = node["x"].AsInt;
            PosY = node["y"].AsInt;
            CutDirection = node["d"].AsInt;
            TailJsonTime = node["tb"].AsFloat;
            TailPosX = node["tx"].AsInt;
            TailPosY = node["ty"].AsInt;
            SliceCount = node["sc"].AsInt;
            Squish = node["s"].AsFloat;
            CustomData = node["customData"];
            ParseCustom();
        }

        public V3Chain(float time, int posX, int posY, int color, int cutDirection,
            float tailTime, int tailPosX, int tailPosY, int sliceCount, float squish,
            JSONNode customData = null) : base(time, posX, posY, color, cutDirection, 0,
            tailTime, tailPosX, tailPosY, sliceCount, squish, customData) =>
            ParseCustom();

        public V3Chain(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, int sliceCount, float squish,
            JSONNode customData = null) : base(time, posX, posY, color, cutDirection, angleOffset,
            tailTime, tailPosX, tailPosY, sliceCount, squish, customData) =>
            ParseCustom();

        public V3Chain(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset,
            float tailJsonTime, float tailSongBpmTime, int tailPosX, int tailPosY, int sliceCount, float squish,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, color, cutDirection, angleOffset,
            tailJsonTime, tailSongBpmTime, tailPosX, tailPosY, sliceCount, squish, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinates";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeyTailCoordinate { get; } = "tailCoordinates";

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
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 ||
             (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360)) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = JsonTime;
            node["c"] = Color;
            node["x"] = PosX;
            node["y"] = PosY;
            node["d"] = CutDirection;
            node["tb"] = TailJsonTime;
            node["tx"] = TailPosX;
            node["ty"] = TailPosY;
            node["sc"] = SliceCount;
            node["s"] = Squish;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3Chain(JsonTime, SongBpmTime, PosX, PosY, Color, CutDirection,
                AngleOffset, TailJsonTime, TailSongBpmTime, TailPosX, TailPosY, SliceCount, Squish, SaveCustom().Clone());
    }
}
