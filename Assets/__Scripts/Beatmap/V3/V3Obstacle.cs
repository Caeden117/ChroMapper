using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Obstacle : IObstacle
    {
        public V3Obstacle()
        {
        }

        public V3Obstacle(IObstacle other) : base(other) => ParseCustom();

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

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeySize { get; } = "_scale";

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
            if (CustomData == null) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override IItem Clone() => new V3Obstacle(Time, PosX, PosY, Duration, Width, Height, CustomData?.Clone());
    }
}
