using Beatmap.Base;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Helper
{
    public static class BeatmapFactory
    {
        public static IDifficulty GetDifficultyFromJson(JSONNode mainNode, string directoryAndFile)
        {
            var v = PeekMapVersionFromJson(mainNode);
            return v[0] == '3'
                ? V3Difficulty.GetFromJson(mainNode, directoryAndFile)
                : V2Difficulty.GetFromJson(mainNode, directoryAndFile) as IDifficulty;
        }

        private static string PeekMapVersionFromJson(JSONNode mainNode)
        {
            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;
                if (key == "version" || key == "_version") return node.Value;
            }

            Debug.LogError("no version detected, return default version 2.0.0.");
            return "2.0.0";
        }

        public static TConcrete Clone<TConcrete>(TConcrete cloneable) where TConcrete : IItem
        {
            return cloneable.Clone() as TConcrete;
        }
    }
}
