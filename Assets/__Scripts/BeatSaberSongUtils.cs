using System;
using System.IO;
using Beatmap.Base;
using Beatmap.Helper;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

public static class BeatSaberSongUtils
{

    public static BaseInfo GetInfoFromFolder(string directory)
    {
        try
        {
            var mainNode = GetNodeFromFile(directory + "/Info.dat");
            if (mainNode == null)
            {
                //Virgin "info.dat" VS chad "Info.dat"
                mainNode = GetNodeFromFile(directory + "/info.dat");
                if (mainNode == null) return null;
                File.Move(directory + "/info.dat", directory + "/Info.dat");
            }

            var version = -1;

            if (mainNode.HasKey("_version"))
            {
                version = 2;
            }
            else if (mainNode.HasKey("version"))
            {
                version = mainNode["version"].Value[0] == '4' ? 4 : -1;
            }

            var info = version switch
            {
                2 => V2Info.GetFromJson(mainNode),
                4 => V4Info.GetFromJson(mainNode),
                _ => null
            };

            if (info != null)
            {
                info.Directory = directory;
            }
            else
            {
                Debug.LogWarning($"Could not parse Info.dat in {directory}");
            }

            return info;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }    
    
    
    public static BaseDifficulty GetMapFromInfoFiles(BaseInfo info, InfoDifficulty difficultyData)
    {
        if (!System.IO.Directory.Exists(info.Directory))
        {
            Debug.LogWarning("Failed to get difficulty json file.");
            return null;
        }
        var fullPath = Path.Combine(info.Directory, difficultyData.BeatmapFileName);

        var mainNode = GetNodeFromFile(fullPath);
        if (mainNode == null)
        {
            Debug.LogWarning("Failed to get difficulty json file " + fullPath);
            return null;
        }

        return BeatmapFactory.GetDifficultyFromJson(mainNode, fullPath);
        
    }

    public static JSONNode GetNodeFromFile(string file)
    {
        if (!File.Exists(file)) return null;
        try
        {
            using (var reader = new StreamReader(file))
            {
                var node = JSON.Parse(reader.ReadToEnd());
                return node;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error trying to read from file {file}\n{e}");
        }

        return null;
    }
}

