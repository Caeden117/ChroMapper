using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Obstacle : BaseObstacle, V2Object
    {
        public V2Obstacle()
        {
        }

        public V2Obstacle(BaseObstacle other) : base(other) => ParseCustom();

        public V2Obstacle(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "_time").AsFloat;
            PosX = RetrieveRequiredNode(node, "_lineIndex").AsInt;
            InternalType = RetrieveRequiredNode(node, "_type").AsInt;
            Duration = RetrieveRequiredNode(node, "_duration").AsFloat;
            Width = RetrieveRequiredNode(node, "_width").AsInt;
            CustomData = node["_customData"];
            InferPosYHeight();
            ParseCustom();
        }

        public V2Obstacle(float time, int posX, int type, float duration, int width, JSONNode customData = null) : base(
            time, posX, type, duration, width, customData) =>
            ParseCustom();

        public V2Obstacle(float jsonTime, float songBpmTime, int posX, int type, float duration, int width, JSONNode customData = null) :
            base(jsonTime, songBpmTime, posX, type, duration, width, customData) =>
            ParseCustom();

        // i fear plugins or anything may mess this up for v2 wall, so i had to make sure
        public override int PosY
        {
            get => InternalPosY;
            set
            {
                if (value != (int)GridY.Base && value != (int)GridY.Top)
                {
                    Type = 0;
                    return;
                }

                InternalPosY = value;
                InternalHeight = InternalPosY switch
                {
                    (int)GridY.Base => (int)ObstacleHeight.Full,
                    (int)GridY.Top => (int)ObstacleHeight.Crouch,
                    _ => 5
                };
                InferType();
            }
        }

        public override int Height
        {
            get => InternalHeight;
            set
            {
                if (value != (int)ObstacleHeight.Full && value != (int)ObstacleHeight.Crouch)
                {
                    Type = 0;
                    return;
                }

                InternalHeight = value;
                InternalPosY = InternalHeight switch
                {
                    (int)ObstacleHeight.Full => (int)GridY.Base,
                    (int)ObstacleHeight.Crouch => (int)GridY.Top,
                    _ => 0
                };
                InferType();
            }
        }

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeySize { get; } = "_scale";

        protected override void ParseCustom() => base.ParseCustom();

        public override bool IsChroma() =>
            CustomData != null && CustomData.HasKey("_color") && CustomData["_color"].IsArray;


        public override bool IsNoodleExtensions() =>
            CustomData != null &&
            ((CustomData.HasKey("_animation") && CustomData["_animation"].IsArray) ||
             (CustomData.HasKey("_fake") && CustomData["_fake"].IsBoolean) ||
             (CustomData.HasKey("_interactable") && CustomData["_interactable"].IsBoolean) ||
             (CustomData.HasKey("_localRotation") && CustomData["_localRotation"].IsArray) ||
             (CustomData.HasKey("_noteJumpMovementSpeed") && CustomData["_noteJumpMovementSpeed"].IsNumber) ||
             (CustomData.HasKey("_noteJumpStartBeatOffset") &&
              CustomData["_noteJumpStartBeatOffset"].IsNumber) ||
             (CustomData.HasKey("_position") && CustomData["_position"].IsArray) ||
             (CustomData.HasKey("_rotation") &&
              (CustomData["_rotation"].IsArray || CustomData["_rotation"].IsNumber)) ||
             (CustomData.HasKey("_scale") && CustomData["_scale"].IsArray) ||
             (CustomData.HasKey("_track") && CustomData["_track"].IsString));

        public override bool IsMappingExtensions() =>
            (Width >= 1000 || Type >= 1000 || PosX < 0 || PosX > 3) &&
            !IsNoodleExtensions();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(JsonTime, DecimalPrecision);
            node["_lineIndex"] = PosX;
            node["_type"] = Type;
            node["_duration"] = Math.Round(Duration, DecimalPrecision);
            node["_width"] = Width;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V2Obstacle(JsonTime, SongBpmTime, PosX, Type, Duration, Width, SaveCustom().Clone());
    }
}
