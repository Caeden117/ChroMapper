using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Obstacle : BaseObstacle, V3Object
    {
        public V3Obstacle()
        {
        }

        public V3Obstacle(BaseObstacle other) : base(other) => ParseCustom();

        public V3Obstacle(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "b").AsFloat;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            InternalPosY = RetrieveRequiredNode(node, "y").AsInt;
            Duration = RetrieveRequiredNode(node, "d").AsFloat;
            Width = RetrieveRequiredNode(node, "w").AsInt;
            InternalHeight = RetrieveRequiredNode(node, "h").AsInt;
            CustomData = node["customData"];
            InferType();
            ParseCustom();
        }

        public V3Obstacle(float time, int posX, int posY, float duration, int width, int height,
            JSONNode customData = null) : base(time, posX, posY, duration, width, height, customData) =>
            ParseCustom();

        public V3Obstacle(float jsonTime, float songBpmTime, int posX, int posY, float duration, int width, int height,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, duration, width, height, customData) =>
            ParseCustom();

        // srsly, u dont need to set this on v3 wall
        public override int Type
        {
            get => InternalType;
            set => base.Type = value > 1 ? 0 : value;
        }

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinates";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeySize { get; } = "size";

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override bool IsChroma() =>
            CustomData != null && CustomData.HasKey("color") && CustomData["color"].IsArray;

        public override bool IsNoodleExtensions() =>
            CustomData != null &&
            ((CustomData.HasKey("animation") && CustomData["animation"].IsArray) ||
             (CustomData.HasKey("uninteractable") && CustomData["uninteractable"].IsBoolean) ||
             (CustomData.HasKey("localRotation") && CustomData["localRotation"].IsArray) ||
             (CustomData.HasKey("noteJumpMovementSpeed") && CustomData["noteJumpMovementSpeed"].IsNumber) ||
             (CustomData.HasKey("noteJumpStartBeatOffset") &&
              CustomData["noteJumpStartBeatOffset"].IsNumber) ||
             (CustomData.HasKey("coordinates") && CustomData["coordinates"].IsArray) ||
             (CustomData.HasKey("worldRotation") &&
              (CustomData["worldRotation"].IsArray || CustomData["worldRotation"].IsNumber)) ||
             (CustomData.HasKey("size") && CustomData["size"].IsArray) ||
             (CustomData.HasKey("track") && CustomData["track"].IsString));

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 || Width <= -1000 || Width >= 1000 ||
             Height <= -1000 || Height > 5) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(JsonTime, DecimalPrecision);
            node["x"] = PosX;
            node["y"] = PosY;
            node["d"] = Math.Round(Duration, DecimalPrecision); //Get rid of float precision errors
            node["w"] = Width;
            node["h"] = Height;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3Obstacle(JsonTime, SongBpmTime, PosX, PosY, Duration, Width, Height, SaveCustom().Clone());
    }
}
