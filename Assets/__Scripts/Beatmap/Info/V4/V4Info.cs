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
            info.EnvironmentName = info.EnvironmentNames.FirstOrDefault() ?? "DefaultEnvironment";
            info.AllDirectionsEnvironmentName = node["allDirectionsEnvironmentName"].Value;
            
            var colorSchemes = new List<InfoColorScheme>();
            var colorSchemeNodes = node["colorSchemes"].AsArray.Children.Select(x => x.AsObject);
            foreach (var colorSchemeNode in colorSchemeNodes)
            {
                var colorScheme = new InfoColorScheme();
                colorScheme.UseOverride = colorSchemeNode["useOverride"].AsBool;
                colorScheme.ColorSchemeName = colorSchemeNode["colorSchemeName"].Value;
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
                    
                    var customData = beatmap["customData"].AsObject;
                    
                    ParseDifficultyCustomData(customData, infoDifficulty);

                    infoDifficulty.CustomData = customData;
                    
                    infoDifficultys.Add(infoDifficulty);
                }

                beatmapSet.Difficulties = infoDifficultys;
                difficultySets.Add(beatmapSet);
            }
            
            info.DifficultySets = difficultySets;

            // CustomData Parsing
            if (node["customData"].IsObject)
            {
                var customData = node["customData"];

                if (customData["contributors"].IsArray)
                {
                    info.CustomContributors = customData["contributors"].AsArray.Children.Select(V4Contributor.GetFromJson).ToList();
                }
                
                // v4 no longer has difficulty sets so the customData for custom characteristics is done here
                if (customData["characteristicData"].IsArray)
                {
                    foreach (var characteristicNode in customData["characteristicData"].AsArray.Children)
                    {
                        var characteristic = characteristicNode["characteristic"].Value;
                        var difficultySet = info.DifficultySets.FirstOrDefault(x => x.Characteristic == characteristic);
                        if (difficultySet != null)
                        {
                            ParseDifficultySetCustomData(characteristicNode, difficultySet);
                        }
                    }
                }

                if (customData["editors"].IsObject)
                {
                    info.CustomEditorsData = new BaseInfo.CustomEditorsMetadata(customData["editors"]);
                }

                if (customData["customEnvironment"].IsString)
                {
                    info.CustomEnvironmentMetadata.Name = customData["customEnvironment"].Value;
                    // Don't need save retrieve since it's calculated on save
                }
                
                info.CustomData = customData;
            }
            
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
                node["saberAColor"] = ColorUtility.ToHtmlStringRGBA(colorScheme.SaberAColor);
                node["saberBColor"] = ColorUtility.ToHtmlStringRGBA(colorScheme.SaberBColor);
                node["obstaclesColor"] = ColorUtility.ToHtmlStringRGBA(colorScheme.ObstaclesColor);
                node["environmentColor0"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor0);
                node["environmentColor1"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor1);
                node["environmentColor0Boost"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor0Boost);
                node["environmentColor1Boost"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor1Boost);
                
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
                
                PopulateDifficultyCustomData(difficulty);
                if (difficulty.CustomData.Count > 0)
                {
                    node["customData"] = difficulty.CustomData;
                }
                
                difficultyBeatmaps.Add(node);
            }

            json["difficultyBeatmaps"] = difficultyBeatmaps;

            // CustomData writing
            if (info.CustomContributors.Any())
            {
                var customContributors = new JSONArray();
                foreach (var customContributor in info.CustomContributors)
                {
                    customContributors.Add(V4Contributor.ToJson(customContributor));
                }
                info.CustomData["contributors"] = customContributors;
            }

            var customCharacteristics = new JSONArray();
            foreach (var difficultySet in info.DifficultySets)
            {
                var customCharacteristic = new JSONObject { ["characteristic"] = difficultySet.Characteristic };
                if (!string.IsNullOrWhiteSpace(difficultySet.CustomCharacteristicLabel))
                {
                    customCharacteristic["label"] = difficultySet.CustomCharacteristicLabel;
                }

                if (!string.IsNullOrWhiteSpace(difficultySet.CustomCharacteristicIconImageFileName))
                {
                    customCharacteristic["iconPath"] = difficultySet.CustomCharacteristicIconImageFileName;
                }

                if (customCharacteristic.Count > 1)
                {
                    customCharacteristics.Add(customCharacteristic);
                }
            }

            if (customCharacteristics.Count > 0)
            {
                info.CustomData["characteristicData"] = customCharacteristics;
            }
            
            // I'm not sure if custom platforms exists for v4 yet. This seems like a safe guess.
            if (!string.IsNullOrEmpty(info.CustomEnvironmentMetadata.Name))
            {
                info.CustomData["customEnvironment"] = info.CustomEnvironmentMetadata.Name;
                info.CustomData["customEnvironmentHash"] = info.CustomEnvironmentMetadata.Hash;
            }
            
            info.CustomData["editors"] = info.CustomEditorsData.ToJson();

            json["customData"] = info.CustomData;
            
            return json;
        }

        private static void ParseDifficultySetCustomData(JSONNode customData, InfoDifficultySet difficultySet)
        {
            if (customData["label"].IsString)
            {
                difficultySet.CustomCharacteristicLabel = customData["label"].Value;
            }

            if (customData["iconPath"].IsString)
            {
                difficultySet.CustomCharacteristicIconImageFileName = customData["iconPath"].Value;
            }
        }
        
        private static void ParseDifficultyCustomData(JSONNode customData, InfoDifficulty difficulty)
        {
            if (customData["oneSaber"].IsBoolean)
            {
                difficulty.CustomOneSaberFlag = customData["oneSaber"].AsBool;
            }

            if (customData["showRotationNoteSpawnLines"].IsBoolean)
            {
                difficulty.CustomShowRotationNoteSpawnLinesFlag = customData["showRotationNoteSpawnLines"].AsBool;
            }

            if (customData["difficultyLabel"].IsString)
            {
                difficulty.CustomLabel = customData["difficultyLabel"].Value;
            }

            if (customData["information"].IsArray)
            {
                difficulty.CustomInformation =
                    customData["information"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["warnings"].IsArray)
            {
                difficulty.CustomWarnings =
                    customData["warnings"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["suggestions"].IsArray)
            {
                difficulty.CustomSuggestions =
                    customData["suggestions"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["requirements"].IsArray)
            {
                difficulty.CustomRequirements =
                    customData["requirements"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["colorLeft"].IsObject)
            {
                difficulty.CustomColorLeft = customData["colorLeft"].ReadColor();
            }
            
            if (customData["colorRight"].IsObject)
            {
                difficulty.CustomColorRight = customData["colorRight"].ReadColor();
            }
            
            if (customData["obstacleColor"].IsObject)
            {
                difficulty.CustomColorObstacle = customData["obstacleColor"].ReadColor();
            }

            if (customData["envColorLeft"].IsObject)
            {
                difficulty.CustomEnvColorLeft = customData["envColorLeft"].ReadColor();
            }
            
            if (customData["envColorRight"].IsObject)
            {
                difficulty.CustomEnvColorRight = customData["envColorRight"].ReadColor();
            }
            
            if (customData["envColorWhite"].IsObject)
            {
                difficulty.CustomEnvColorWhite = customData["envColorWhite"].ReadColor();
            }
            
            if (customData["envColorLeftBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostLeft = customData["envColorLeftBoost"].ReadColor();
            }
            
            if (customData["envColorRightBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostRight = customData["envColorRightBoost"].ReadColor();
            }
            
            if (customData["envColorWhiteBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostWhite = customData["envColorWhiteBoost"].ReadColor();
            }
        }

        private static void PopulateDifficultyCustomData(InfoDifficulty difficulty)
        {
            if (difficulty.CustomOneSaberFlag != null)
            {
                difficulty.CustomData["oneSaber"] = difficulty.CustomOneSaberFlag.Value;
            }

            if (difficulty.CustomShowRotationNoteSpawnLinesFlag != null)
            {
                difficulty.CustomData["showRotationNoteSpawnLines"] = difficulty.CustomShowRotationNoteSpawnLinesFlag.Value;
            }

            if (!string.IsNullOrWhiteSpace(difficulty.CustomLabel))
            {
                difficulty.CustomData["difficultyLabel"] = difficulty.CustomLabel;
            }

            if (difficulty.CustomInformation.Any())
            {
                difficulty.CustomData["information"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomInformation, s => s);
            }
            
            if (difficulty.CustomWarnings.Any())
            {
                difficulty.CustomData["warnings"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomWarnings, s => s);
            }
            
            if (difficulty.CustomSuggestions.Any())
            {
                difficulty.CustomData["suggestions"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomSuggestions, s => s);
            }
            
            if (difficulty.CustomRequirements.Any())
            {
                difficulty.CustomData["requirements"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomRequirements, s => s);
            }
            
            // SongCore saves colors in Object format so temporarily change container type
            JSONNode.ColorContainerType = JSONContainerType.Object;

            if (difficulty.CustomColorLeft != null)
            {
                difficulty.CustomData["colorLeft"] = difficulty.CustomColorLeft.Value;
            }

            if (difficulty.CustomColorRight != null)
            {
                difficulty.CustomData["colorRight"] = difficulty.CustomColorRight.Value;
            }

            if (difficulty.CustomColorObstacle != null)
            {
                difficulty.CustomData["obstacleColor"] = difficulty.CustomColorObstacle.Value;
            }

            if (difficulty.CustomEnvColorLeft != null)
            {
                difficulty.CustomData["envColorLeft"] = difficulty.CustomEnvColorLeft.Value;
            }

            if (difficulty.CustomEnvColorRight != null)
            {
                difficulty.CustomData["envColorRight"] = difficulty.CustomEnvColorRight.Value;
            }

            if (difficulty.CustomEnvColorWhite != null)
            {
                difficulty.CustomData["envColorWhite"] = difficulty.CustomEnvColorWhite.Value;
            }

            if (difficulty.CustomEnvColorBoostLeft != null)
            {
                difficulty.CustomData["envColorLeftBoost"] = difficulty.CustomEnvColorBoostLeft.Value;
            }

            if (difficulty.CustomEnvColorBoostRight != null)
            {
                difficulty.CustomData["envColorRightBoost"] = difficulty.CustomEnvColorBoostRight.Value;
            }

            if (difficulty.CustomEnvColorBoostWhite != null)
            {
                difficulty.CustomData["envColorWhiteBoost"] = difficulty.CustomEnvColorBoostWhite.Value;
            }

            JSONNode.ColorContainerType = JSONContainerType.Array;
        }
    }
}
