using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4Obstacle
    {
        public static BaseObstacle GetFromJson(JSONNode node, IList<V4CommonData.Obstacle> obstaclesCommonData)
        {
            var obstacle = new BaseObstacle();
            
            obstacle.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var obstacleData = obstaclesCommonData[index];

            obstacle.PosX = obstacleData.PosX;
            obstacle.PosY = obstacleData.PosY;
            obstacle.Duration = obstacleData.Duration;
            obstacle.Width = obstacleData.Width;
            obstacle.Height = obstacleData.Height;
            
            return obstacle;
        }

        public static JSONNode ToJson(BaseObstacle obstacle, IList<V4CommonData.Obstacle> obstaclesCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = obstacle.JsonTime;
            node["r"] = 0;

            var data = V4CommonData.Obstacle.FromBaseObstacle(obstacle);
            node["i"] = obstaclesCommonData.IndexOf(data);
            
            return node;
        }
    }
}
