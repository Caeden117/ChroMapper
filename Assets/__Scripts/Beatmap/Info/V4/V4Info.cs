using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    public static class V4Info
    {
        public static BaseInfo GetFromJson(JSONNode node)
        {
            var info = new BaseInfo();

            info.Version = node["version"].Value;

            var songNode = node["song"].AsObject;
            info.SongName = songNode["title"].Value;
            info.SongSubName = songNode["subTitle"].Value;
            info.SongAuthorName = songNode["author"].Value;

            var audioNode = node["audio"].AsObject;
            info.SongFilename = audioNode["songFilename"].Value;
            info.SongDurationMetadata = audioNode["songDuration"].AsFloat;
            info.AudioDataFilename = audioNode["audioDataFilename"].Value;
            info.BeatsPerMinute = audioNode["bpm"].AsFloat;
            info.PreviewStartTime = audioNode["previewStartTime"].AsFloat;
            info.PreviewDuration = audioNode["previewDuration"].AsFloat;
            info.Lufs = audioNode["lufs"].AsFloat;

            info.SongPreviewFilename = node["songPreviewFilename"].Value;
            info.CoverImageFilename = node["coverImageFilename"].Value;

            info.EnvironmentNames = node["environmentNames"].AsArray.Children.Select(x => x.Value).ToList();
            info.EnvironmentName = info.EnvironmentNames.First();
            info.AllDirectionsEnvironmentName = node["allDirectionsEnvironmentName"].Value;
            
            var colorSchemes = new List<InfoColorScheme>();
            var colorSchemeNodes = node["colorSchemes"].AsArray.Children.Select(x => x.AsObject);
            foreach (var colorSchemeNode in colorSchemeNodes)
            {
                var colorScheme = new InfoColorScheme();
                colorScheme.UseOverride = colorSchemeNode["useOverride"].AsBool;
                colorScheme.ColorSchemeName = colorSchemeNode["colorSchemeName"];
                colorScheme.SaberAColor = colorSchemeNode["saberAColor"].ReadHtmlStringColor();
                colorScheme.SaberBColor = colorSchemeNode["saberBColor"].ReadHtmlStringColor();
                colorScheme.ObstaclesColor = colorSchemeNode["obstaclesColor"].ReadHtmlStringColor();
                colorScheme.EnvironmentColor0 = colorSchemeNode["environmentColor0"].ReadHtmlStringColor();
                colorScheme.EnvironmentColor1 = colorSchemeNode["environmentColor1"].ReadHtmlStringColor();
                colorScheme.EnvironmentColor0Boost = colorSchemeNode["environmentColor0Boost"].ReadHtmlStringColor();
                colorScheme.EnvironmentColor1Boost = colorSchemeNode["environmentColor1Boost"].ReadHtmlStringColor();
                colorSchemes.Add(colorScheme);
            }
            info.ColorSchemes = colorSchemes;
            
            var beatmapsNode = node["difficultyBeatmaps"].AsArray;
            var difficultySets = new List<InfoDifficultySet>();
            foreach (var beatmapCharacteristicSet in beatmapsNode.Children.GroupBy(x => x["characteristic"].Value))
            {
                var beatmapSet = new InfoDifficultySet { Characteristic = beatmapCharacteristicSet.Key };
                var infoDifficultys = new List<InfoDifficulty>();

                foreach (var beatmap in beatmapCharacteristicSet)
                {
                    var infoDifficulty = new InfoDifficulty(beatmapSet);
                    infoDifficulty.Difficulty = beatmap["difficulty"].Value;
                    infoDifficulty.EnvironmentNameIndex = beatmap["environmentNameIdx"].AsInt;
                    infoDifficulty.ColorSchemeIndex = beatmap["beatmapColorSchemeIdx"].AsInt;
                    infoDifficulty.NoteJumpSpeed = beatmap["noteJumpMovementSpeed"].AsFloat;
                    infoDifficulty.NoteStartBeatOffset = beatmap["noteJumpStartBeatOffset"].AsFloat;

                    infoDifficulty.BeatmapFileName = beatmap["beatmapDataFilename"].Value;
                    infoDifficulty.LightshowFileName = beatmap["lightshowDataFilename"].Value;
                    
                    var authorsNode = beatmap["beatmapAuthors"].AsObject;
                    infoDifficulty.Mappers = authorsNode["mappers"].AsArray.Children.Select(x => x.Value).ToList();
                    infoDifficulty.Lighters = authorsNode["lighters"].AsArray.Children.Select(x => x.Value).ToList();
                    
                    infoDifficultys.Add(infoDifficulty);
                }

                beatmapSet.Difficulties = infoDifficultys;
                difficultySets.Add(beatmapSet);
            }
            
            info.DifficultySets = difficultySets;

            info.CustomData = node["customData"];
            
            return info;
        }

        public static JSONNode GetOutputJson(BaseInfo info)
        {
            var json = new JSONObject();

            json["version"] = "4.0.0";

            var songNode = new JSONObject();
            songNode["title"] = info.SongName;
            songNode["subTitle"] = info.SongSubName;
            songNode["author"] = info.SongAuthorName;
            json["song"] = songNode;

            var audioNode = new JSONObject();
            audioNode["songFilename"] = info.SongFilename;
            audioNode["songDuration"] = info.SongDurationMetadata; // Probably insert loaded audio duration here
            audioNode["audioDataFilename"] = info.AudioDataFilename;
            audioNode["bpm"] = info.BeatsPerMinute;
            audioNode["lufs"] = info.Lufs;
            audioNode["previewStartTime"] = info.PreviewStartTime;
            audioNode["previewDuration"] = info.PreviewDuration;
            json["audio"] = audioNode;

            json["songPreviewFilename"] = info.SongPreviewFilename; // Why isn't this part of the audio node???
            json["coverImageFilename"] = info.CoverImageFilename;

            var environmentNames = new JSONArray();
            foreach (var environmentName in info.EnvironmentNames)
            {
                environmentNames.Add(environmentName);
            }
            json["environmentNames"] = environmentNames;

            var colorSchemes = new JSONArray();
            foreach (var colorScheme in info.ColorSchemes)
            {
                var node = new JSONObject();
                
                node["useOverride"] = colorScheme.UseOverride;
                node["colorSchemeName"] = colorScheme.ColorSchemeName;
                node["saberAColor"] = ToHashPrefixedHtmlStringRGBA(colorScheme.SaberAColor);
                node["saberBColor"] = ToHashPrefixedHtmlStringRGBA(colorScheme.SaberBColor);
                node["obstaclesColor"] = ToHashPrefixedHtmlStringRGBA(colorScheme.ObstaclesColor);
                node["environmentColor0"] = ToHashPrefixedHtmlStringRGBA(colorScheme.EnvironmentColor0);
                node["environmentColor1"] = ToHashPrefixedHtmlStringRGBA(colorScheme.EnvironmentColor1);
                node["environmentColor0Boost"] = ToHashPrefixedHtmlStringRGBA(colorScheme.EnvironmentColor0Boost);
                node["environmentColor1Boost"] = ToHashPrefixedHtmlStringRGBA(colorScheme.EnvironmentColor1Boost);
                
                colorSchemes.Add(node);
            }
            json["colorSchemes"] = colorSchemes;

            var difficultyBeatmaps = new JSONArray();
            foreach (var difficulty in info.DifficultySets.SelectMany(x => x.Difficulties))
            {
                var node = new JSONObject();

                node["characteristic"] = difficulty.Characteristic;
                node["difficulty"] = difficulty.Difficulty;

                var authorsNode = new JSONObject();
                
                var mappers = new JSONArray();
                foreach (var mapper in difficulty.Mappers) mappers.Add(mapper);
                authorsNode["mappers"] = mappers;

                var lighters = new JSONArray();
                foreach (var lighter in difficulty.Lighters) lighters.Add(lighter);
                authorsNode["lighters"] = lighters;

                node["beatmapAuthors"] = authorsNode;

                node["environmentNameIdx"] = difficulty.EnvironmentNameIndex;
                node["beatmapColorSchemeIdx"] = difficulty.ColorSchemeIndex;
                node["noteJumpMovementSpeed"] = difficulty.NoteJumpSpeed;
                node["noteJumpStartBeatOffset"] = difficulty.NoteStartBeatOffset;
                node["beatmapDataFilename"] = difficulty.BeatmapFileName;
                node["lightshowDataFilename"] = difficulty.LightshowFileName;

                node["customData"] = difficulty.CustomData;
                
                difficultyBeatmaps.Add(node);
            }

            json["difficultyBeatmaps"] = difficultyBeatmaps;

            json["customData"] = info.CustomData;
            
            return json;
        }

        private static string ToHashPrefixedHtmlStringRGBA(Color color) => $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }
}
