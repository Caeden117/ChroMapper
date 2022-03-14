using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatSaberMapFactory
{
    public static BeatSaberMap GetBeatSaberMapFromJson(JSONNode mainNode, string directoryAndFile)
    {
        var v = PeekMapVersionFromJson(mainNode);
        if (v[0] == '3')
        {
            return BeatSaberMapV3.GetBeatSaberMapFromJson(mainNode, directoryAndFile);
        }
        else
        {
            return BeatSaberMap.GetBeatSaberMapFromJson(mainNode, directoryAndFile);
        }
    }

    public static string PeekMapVersionFromJson(JSONNode mainNode)
    {
        var nodeEnum = mainNode.GetEnumerator();
        while (nodeEnum.MoveNext())
        {
            var key = nodeEnum.Current.Key;
            var node = nodeEnum.Current.Value;
            if (key == "version" ||key == "_version")
            {
                return node.Value;
            }
        }
        Debug.LogError("no version detected, return default version 2.0.0.");
        return "2.0.0";
    }
}
