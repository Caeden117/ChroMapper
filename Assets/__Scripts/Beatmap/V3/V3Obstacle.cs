using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Obstacle
    {
        public const string CustomKeyAnimation = "animation";

        public const string CustomKeyTrack = "track";

        public const string CustomKeyColor = "color";

        public const string CustomKeyCoordinate = "coordinates";

        public const string CustomKeyWorldRotation = "worldRotation";

        public const string CustomKeyLocalRotation = "localRotation";

        public const string CustomKeySpawnEffect = "spawnEffect";

        public const string CustomKeyNoteJumpMovementSpeed = "noteJumpMovementSpeed";

        public const string CustomKeyNoteJumpStartBeatOffset = "noteJumpStartBeatOffset";

        public const string CustomKeySize = "size";

        public static BaseObstacle GetFromJson(JSONNode node, bool customFake = false)
        {
            var obstacle = new BaseObstacle();
            
            obstacle.JsonTime = node["b"].AsFloat;
            obstacle.PosX = node["x"].AsInt;
            obstacle.PosY = node["y"].AsInt;
            obstacle.Duration = node["d"].AsFloat;
            obstacle.Width = node["w"].AsInt;
            obstacle.Height = node["h"].AsInt;

            obstacle.CustomData = node["customData"];
            obstacle.RefreshCustom();
            
            obstacle.CustomFake = customFake;

            return obstacle;
        }

        public static JSONNode ToJson(BaseObstacle obstacle)
        {
            JSONNode node = new JSONObject();
            node["b"] = obstacle.JsonTime;
            node["x"] = obstacle.PosX;
            node["y"] = obstacle.PosY;
            node["d"] = obstacle.Duration; //Get rid of float precision errors
            node["w"] = obstacle.Width;
            node["h"] = obstacle.Height;
            obstacle.CustomData = obstacle.SaveCustom();
            if (!obstacle.CustomData.Children.Any()) return node;
            node["customData"] = obstacle.CustomData;
            return node;
        }
    }
}
