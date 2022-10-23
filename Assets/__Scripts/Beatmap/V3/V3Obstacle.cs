using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Obstacle : BaseObstacle
    {
        private int height;
        private int width;

        public V3Obstacle()
        {
        }

        public V3Obstacle(BaseObstacle other) : base(other) => ParseCustom();

        public V3Obstacle(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            PosY = RetrieveRequiredNode(node, "y").AsInt;
            Duration = RetrieveRequiredNode(node, "d").AsFloat;
            Width = RetrieveRequiredNode(node, "w").AsInt;
            Height = RetrieveRequiredNode(node, "h").AsInt;
            CustomData = node["customData"];
            InferType();
            ParseCustom();
        }

        public V3Obstacle(float time, int posX, int posY, float duration, int width, int height,
            JSONNode customData = null) : base(time, posX, posY, duration, width, height, customData) =>
            ParseCustom();

        public override int Width
        {
            get => width;
            set
            {
                width = value;
                InferType();
            }
        }

        public override int Height
        {
            get => height;
            set
            {
                height = value;
                InferType();
            }
        }

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "position";

        public override string CustomKeyWorldRotation { get; } = "rotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeySize { get; } = "size";

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override bool IsChroma() => CustomData.HasKey("color") && CustomData["color"].IsArray;

        public override bool IsNoodleExtensions() =>
            (CustomData.HasKey("animation") && CustomData["animation"].IsArray) ||
            (CustomData.HasKey("uninteractable") && CustomData["uninteractable"].IsBoolean) ||
            (CustomData.HasKey("localRotation") && CustomData["localRotation"].IsArray) ||
            (CustomData.HasKey("noteJumpMovementSpeed") && CustomData["noteJumpMovementSpeed"].IsNumber) ||
            (CustomData.HasKey("noteJumpStartBeatOffset") &&
             CustomData["noteJumpStartBeatOffset"].IsNumber) ||
            (CustomData.HasKey("coordinates") && CustomData["coordinates"].IsArray) ||
            (CustomData.HasKey("worldRotation") &&
             (CustomData["worldRotation"].IsArray || CustomData["worldRotation"].IsNumber)) ||
            (CustomData.HasKey("size") && CustomData["size"].IsArray) ||
            (CustomData.HasKey("track") && CustomData["track"].IsString);

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 || Width <= -1000 || Width >= 1000 ||
             Height < 0 || Height > 5) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
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
            new V3Obstacle(Time, PosX, PosY, Duration, Width, Height, CustomData?.Clone());
    }
}
