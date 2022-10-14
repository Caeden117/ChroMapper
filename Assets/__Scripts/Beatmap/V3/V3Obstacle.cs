using System;
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

        public override bool IsChroma() => CustomData?["color"] != null && CustomData["color"].IsArray;

        public override bool IsNoodleExtensions() =>
            (CustomData?["animation"] != null && CustomData["animation"].IsArray) ||
            (CustomData?["uninteractable"] != null && CustomData["uninteractable"].IsBoolean) ||
            (CustomData?["localRotation"] != null && CustomData["localRotation"].IsArray) ||
            (CustomData?["noteJumpMovementSpeed"] != null && CustomData["noteJumpMovementSpeed"].IsNumber) ||
            (CustomData?["noteJumpStartBeatOffset"] != null &&
             CustomData["noteJumpStartBeatOffset"].IsNumber) ||
            (CustomData?["coordinates"] != null && CustomData["coordinates"].IsArray) ||
            (CustomData?["worldRotation"] != null &&
             (CustomData["worldRotation"].IsArray || CustomData["worldRotation"].IsNumber)) ||
            (CustomData?["size"] != null && CustomData["size"].IsArray) ||
            (CustomData?["track"] != null && CustomData["track"].IsString);

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 || Width <= -1000 || Width >= 1000 ||
             Height < 0 || Height >= 5) &&
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
            SaveCustom();
            if (CustomData.Count == 0) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3Obstacle(Time, PosX, PosY, Duration, Width, Height, CustomData?.Clone());
    }
}
