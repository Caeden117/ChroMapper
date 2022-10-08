using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Obstacle : IObstacle
    {
        public V2Obstacle()
        {
        }

        public V2Obstacle(IObstacle other) : base(other) => ParseCustom();

        public V2Obstacle(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "_time").AsFloat;
            PosX = RetrieveRequiredNode(node, "_lineIndex").AsInt;
            PosY = node["_lineLayer"] ?? 0;
            Type = RetrieveRequiredNode(node, "_type").AsInt;
            Duration = RetrieveRequiredNode(node, "_duration").AsFloat;
            Width = RetrieveRequiredNode(node, "_width").AsInt;
            Height = node["_height"] ?? 0;
            CustomData = node["_customData"];
            InferPosYHeight();
            ParseCustom();
        }

        public V2Obstacle(float time, int posX, int type, float duration, int width, JSONNode customData = null) : base(
            time, posX, type, duration, width, customData) =>
            ParseCustom();

        public sealed override void ParseCustom() => base.ParseCustom();

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeySize { get; } = "_scale";

        public override bool IsChroma() => CustomData?["_color"] != null && CustomData["_color"].IsArray;


        public override bool IsNoodleExtensions() =>
            (CustomData?["_animation"] != null && CustomData["_animation"].IsArray) ||
            (CustomData?["_fake"] != null && CustomData["_fake"].IsBoolean) ||
            (CustomData?["_interactable"] != null && CustomData["_interactable"].IsBoolean) ||
            (CustomData?["_localRotation"] != null && CustomData["_localRotation"].IsArray) ||
            (CustomData?["_noteJumpMovementSpeed"] != null && CustomData["_noteJumpMovementSpeed"].IsNumber) ||
            (CustomData?["_noteJumpStartBeatOffset"] != null &&
             CustomData["_noteJumpStartBeatOffset"].IsNumber) ||
            (CustomData?["_position"] != null && CustomData["_position"].IsArray) ||
            (CustomData?["_rotation"] != null &&
             (CustomData["_rotation"].IsArray || CustomData["_rotation"].IsNumber)) ||
            (CustomData?["_scale"] != null && CustomData["_scale"].IsArray) ||
            (CustomData?["_track"] != null && CustomData["_track"].IsString);

        public override bool IsMappingExtensions() =>
            (Width >= 1000 || Type >= 1000 || PosX < 0 || PosX > 3) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(Time, DecimalPrecision);
            node["_lineIndex"] = PosX;
            node["_lineLayer"] = PosY;
            node["_type"] = Type;
            node["_duration"] = Math.Round(Duration, DecimalPrecision); //Get rid of float precision errors
            node["_width"] = Width;
            node["_height"] = Height;
            if (CustomData != null) node["_customData"] = CustomData;
            return node;
        }

        public override IItem Clone() => new V2Obstacle(Time, PosX, Type, Duration, Width, CustomData?.Clone());
    }
}
