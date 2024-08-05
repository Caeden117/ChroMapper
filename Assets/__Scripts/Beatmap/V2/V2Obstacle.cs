using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Obstacle
    {
        public const string CustomKeyAnimation = "_animation";

        public const string CustomKeyTrack = "_track";

        public const string CustomKeyColor = "_color";

        public const string CustomKeyCoordinate = "_position";

        public const string CustomKeyWorldRotation = "_rotation";

        public const string CustomKeyLocalRotation = "_localRotation";

        public const string CustomKeySpawnEffect = "_disableSpawnEffect";

        public const string CustomKeyNoteJumpMovementSpeed = "_noteJumpMovementSpeed";

        public const string CustomKeyNoteJumpStartBeatOffset = "_noteJumpStartBeatOffset";

        public const string CustomKeySize = "_scale";

        public static BaseObstacle GetFromJson(JSONNode node)
        {
            var obstacle = new BaseObstacle();

            obstacle.JsonTime = BaseItem.GetRequiredNode(node, "_time").AsFloat;
            obstacle.PosX = BaseItem.GetRequiredNode(node, "_lineIndex").AsInt;
            obstacle.Type = BaseItem.GetRequiredNode(node, "_type").AsInt;
            obstacle.Duration = BaseItem.GetRequiredNode(node, "_duration").AsFloat;
            obstacle.Width = BaseItem.GetRequiredNode(node, "_width").AsInt;
            obstacle.CustomData = node["_customData"];
            obstacle.RefreshCustom();

            return obstacle;
        }

        public static JSONNode ToJson(BaseObstacle obstacle)
        {
            JSONNode node = new JSONObject();
            node["_time"] = obstacle.JsonTime;
            node["_lineIndex"] = obstacle.PosX;
            node["_type"] = obstacle.Type;
            node["_duration"] = obstacle.Duration;
            node["_width"] = obstacle.Width;
            obstacle.CustomData = obstacle.SaveCustom().Clone();
            if (!obstacle.CustomData.Children.Any()) return node;
            node["_customData"] = obstacle.CustomData;
            return node;
        }
    }
}
