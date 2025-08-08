using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    public static class V4Info
    {
        public const string Version = "4.0.1";
        
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
            
            var colorSchemes = new List<InfoColorScheme>();
            var colorSchemeNodes = node["colorSchemes"].AsArray.Children.Select(x => x.AsObject);
            foreach (var colorSchemeNode in colorSchemeNodes)
            {
                var colorScheme = new InfoColorScheme();

                colorScheme.ColorSchemeName = colorSchemeNode["colorSchemeName"].Value;

                if (colorSchemeNode.HasKey("useOverride")) // 4.0.0
                {
                    colorScheme.UseOverride = colorSchemeNode["useOverride"].AsBool;
                    colorScheme.OverrideNotes = colorScheme.UseOverride;
                    colorScheme.OverrideLights = colorScheme.UseOverride;
                }
                else // 4.0.1
                {
                    colorScheme.OverrideNotes = colorSchemeNode["overrideNotes"].AsBool;
                    colorScheme.OverrideLights = colorSchemeNode["overrideLights"].AsBool;
                    colorScheme.UseOverride = colorScheme.OverrideNotes || colorScheme.OverrideLights;
                }

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
                    customData.Remove("contributors");
                }
                
                // v4 no longer has difficulty sets so the customData for custom characteristics is done here
                if (customData["characteristicData"].IsArray)
                {
                    foreach (var characteristicNode in customData["characteristicData"].AsArray.Children)
                    {
                        var characteristic = characteristicNode["characteristic"].Value;
                        var difficultySet = info.DifficultySets.FirstOrDefault(x => x.Characteristic == characteristic);
                        if (difficultySet == null)
                        {
                            difficultySet = new InfoDifficultySet { Characteristic = characteristic };
                            info.DifficultySets.Add(difficultySet);
                        }

                        ParseDifficultySetCustomData(characteristicNode, difficultySet);
                    }

                    // recreated on save so we can just remove the entire array
                    // downside is data for characteristics that don't have any corresponding diffs will be lost
                    customData.Remove("characteristicData");
                }

                if (customData["editors"].IsObject)
                {
                    info.CustomEditorsData = new BaseInfo.CustomEditorsMetadata(customData["editors"]);
                    customData.Remove("editors");
                }

                if (customData["customEnvironment"].IsString)
                {
                    info.CustomEnvironmentMetadata.Name = customData["customEnvironment"].Value;
                    info.CustomEnvironmentMetadata.Hash = customData["customEnvironmentHash"].Value;
                    customData.Remove("customEnvironment");
                    customData.Remove("customEnvironmentHash");
                }
                
                info.CustomData = customData;
            }
            
            return info;
        }

        public static JSONNode GetOutputJson(BaseInfo info)
        {
            var json = new JSONObject();

            json["version"] = Version;

            var songNode = new JSONObject();
            songNode["title"] = info.SongName;
            songNode["subTitle"] = info.SongSubName;
            songNode["author"] = info.SongAuthorName;
            json["song"] = songNode;

            var audioNode = new JSONObject();
            audioNode["songFilename"] = info.SongFilename;
            audioNode["songDuration"] = BeatSaberSongContainer.Instance?.LoadedSongLength;
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
                node["colorSchemeName"] = colorScheme.ColorSchemeName;

                node["overrideNotes"] = colorScheme.OverrideNotes;
                node["saberAColor"] = ColorUtility.ToHtmlStringRGBA(colorScheme.SaberAColor);
                node["saberBColor"] = ColorUtility.ToHtmlStringRGBA(colorScheme.SaberBColor);
                node["obstaclesColor"] = ColorUtility.ToHtmlStringRGBA(colorScheme.ObstaclesColor);
                
                node["overrideLights"] = colorScheme.OverrideLights;
                node["environmentColor0"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor0);
                node["environmentColor1"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor1);
                node["environmentColor0Boost"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor0Boost);
                node["environmentColor1Boost"] = ColorUtility.ToHtmlStringRGBA(colorScheme.EnvironmentColor1Boost);
                
                colorSchemes.Add(node);
            }
            json["colorSchemes"] = colorSchemes;

            var difficultyBeatmaps = new JSONArray();
            foreach (var difficulty in info.DifficultySets.SelectMany(x => x.Difficulties)
                         .OrderBy(x => x.Characteristic).ThenBy(x => x.DifficultyRank))
            {
                // The orderings aren't strictly necessary, but it's nice to have them grouped in order for json editing
                
                var node = new JSONObject();

                node["characteristic"] = difficulty.Characteristic;
                node["difficulty"] = difficulty.Difficulty;

                var authorsNode = new JSONObject();
                
                var mappers = new JSONArray();
                foreach (var mapper in difficulty.Mappers.Where(mapper => !string.IsNullOrEmpty(mapper)))
                {
                    mappers.Add(mapper);
                }
                authorsNode["mappers"] = mappers;

                var lighters = new JSONArray();
                foreach (var lighter in difficulty.Lighters.Where(lighter => !string.IsNullOrEmpty(lighter)))
                {
                    lighters.Add(lighter);
                }
                authorsNode["lighters"] = lighters;

                node["beatmapAuthors"] = authorsNode;

                node["environmentNameIdx"] = difficulty.EnvironmentNameIndex;
                node["beatmapColorSchemeIdx"] = difficulty.ColorSchemeIndex;
                node["noteJumpMovementSpeed"] = difficulty.NoteJumpSpeed;
                node["noteJumpStartBeatOffset"] = difficulty.NoteStartBeatOffset;
                node["beatmapDataFilename"] = difficulty.BeatmapFileName;
                node["lightshowDataFilename"] = difficulty.LightshowFileName;
                
                var diffCustomData = GetOutputDifficultyCustomData(difficulty);
                if (diffCustomData.Count > 0)
                {
                    node["customData"] = diffCustomData;
                }
                
                difficultyBeatmaps.Add(node);
            }

            json["difficultyBeatmaps"] = difficultyBeatmaps;

            // CustomData writing
            var customData = info.CustomData.Clone();
            if (info.CustomContributors.Any())
            {
                var customContributors = new JSONArray();
                foreach (var customContributor in info.CustomContributors)
                {
                    customContributors.Add(V4Contributor.ToJson(customContributor));
                }
                customData["contributors"] = customContributors;
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
                customData["characteristicData"] = customCharacteristics;
            }
            
            // I'm not sure if custom platforms exists for v4 yet. This seems like a safe guess.
            if (!string.IsNullOrEmpty(info.CustomEnvironmentMetadata.Name))
            {
                customData["customEnvironment"] = info.CustomEnvironmentMetadata.Name;
                customData["customEnvironmentHash"] = info.CustomEnvironmentMetadata.Hash;
            }
            
            customData["editors"] = info.CustomEditorsData.ToJson();

            json["customData"] = customData;
            
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
                customData.Remove("oneSaber");
            }

            if (customData["showRotationNoteSpawnLines"].IsBoolean)
            {
                difficulty.CustomShowRotationNoteSpawnLinesFlag = customData["showRotationNoteSpawnLines"].AsBool;
                customData.Remove("showRotationNoteSpawnLines");
            }

            if (customData["difficultyLabel"].IsString)
            {
                difficulty.CustomLabel = customData["difficultyLabel"].Value;
                customData.Remove("difficultyLabel");
            }

            if (customData["information"].IsArray)
            {
                difficulty.CustomInformation =
                    customData["information"].AsArray.Children.Select(x => x.Value).ToList();
                customData.Remove("information");
            }

            if (customData["warnings"].IsArray)
            {
                difficulty.CustomWarnings =
                    customData["warnings"].AsArray.Children.Select(x => x.Value).ToList();
                customData.Remove("warnings");
            }

            if (customData["suggestions"].IsArray)
            {
                difficulty.CustomSuggestions =
                    customData["suggestions"].AsArray.Children.Select(x => x.Value).ToList();
                customData.Remove("suggestions");
            }

            if (customData["requirements"].IsArray)
            {
                difficulty.CustomRequirements =
                    customData["requirements"].AsArray.Children.Select(x => x.Value).ToList();
                customData.Remove("requirements");
            }

            if (customData["colorLeft"].IsObject)
            {
                difficulty.CustomColorLeft = customData["colorLeft"].ReadColor();
                customData.Remove("colorLeft");
            }
            
            if (customData["colorRight"].IsObject)
            {
                difficulty.CustomColorRight = customData["colorRight"].ReadColor();
                customData.Remove("colorRight");
            }
            
            if (customData["obstacleColor"].IsObject)
            {
                difficulty.CustomColorObstacle = customData["obstacleColor"].ReadColor();
                customData.Remove("obstacleColor");
            }

            if (customData["envColorLeft"].IsObject)
            {
                difficulty.CustomEnvColorLeft = customData["envColorLeft"].ReadColor();
                customData.Remove("envColorLeft");
            }
            
            if (customData["envColorRight"].IsObject)
            {
                difficulty.CustomEnvColorRight = customData["envColorRight"].ReadColor();
                customData.Remove("envColorRight");
            }
            
            if (customData["envColorWhite"].IsObject)
            {
                difficulty.CustomEnvColorWhite = customData["envColorWhite"].ReadColor();
                customData.Remove("envColorWhite");
            }
            
            if (customData["envColorLeftBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostLeft = customData["envColorLeftBoost"].ReadColor();
                customData.Remove("envColorLeftBoost");
            }
            
            if (customData["envColorRightBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostRight = customData["envColorRightBoost"].ReadColor();
                customData.Remove("envColorRightBoost");
            }
            
            if (customData["envColorWhiteBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostWhite = customData["envColorWhiteBoost"].ReadColor();
                customData.Remove("envColorWhiteBoost");
            }
        }

        private static JSONNode GetOutputDifficultyCustomData(InfoDifficulty difficulty)
        {
            var customData = difficulty.CustomData.Clone();

            if (difficulty.CustomOneSaberFlag != null)
            {
                customData["oneSaber"] = difficulty.CustomOneSaberFlag.Value;
            }

            if (difficulty.CustomShowRotationNoteSpawnLinesFlag != null)
            {
                customData["showRotationNoteSpawnLines"] = difficulty.CustomShowRotationNoteSpawnLinesFlag.Value;
            }

            if (!string.IsNullOrWhiteSpace(difficulty.CustomLabel))
            {
                customData["difficultyLabel"] = difficulty.CustomLabel;
            }

            if (difficulty.CustomInformation.Any())
            {
                customData["information"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomInformation, s => s);
            }
            
            if (difficulty.CustomWarnings.Any())
            {
                customData["warnings"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomWarnings, s => s);
            }
            
            if (difficulty.CustomSuggestions.Any())
            {
                customData["suggestions"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomSuggestions, s => s);
            }
            
            if (difficulty.CustomRequirements.Any())
            {
                customData["requirements"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomRequirements, s => s);
            }
            
            // SongCore saves colors in Object format so temporarily change container type
            JSONNode.ColorContainerType = JSONContainerType.Object;

            if (difficulty.CustomColorLeft != null)
            {
                customData["colorLeft"] = difficulty.CustomColorLeft.Value;
            }

            if (difficulty.CustomColorRight != null)
            {
                customData["colorRight"] = difficulty.CustomColorRight.Value;
            }

            if (difficulty.CustomColorObstacle != null)
            {
                customData["obstacleColor"] = difficulty.CustomColorObstacle.Value;
            }

            if (difficulty.CustomEnvColorLeft != null)
            {
                customData["envColorLeft"] = difficulty.CustomEnvColorLeft.Value;
            }

            if (difficulty.CustomEnvColorRight != null)
            {
                customData["envColorRight"] = difficulty.CustomEnvColorRight.Value;
            }

            if (difficulty.CustomEnvColorWhite != null)
            {
                customData["envColorWhite"] = difficulty.CustomEnvColorWhite.Value;
            }

            if (difficulty.CustomEnvColorBoostLeft != null)
            {
                customData["envColorLeftBoost"] = difficulty.CustomEnvColorBoostLeft.Value;
            }

            if (difficulty.CustomEnvColorBoostRight != null)
            {
                customData["envColorRightBoost"] = difficulty.CustomEnvColorBoostRight.Value;
            }

            if (difficulty.CustomEnvColorBoostWhite != null)
            {
                customData["envColorWhiteBoost"] = difficulty.CustomEnvColorBoostWhite.Value;
            }

            JSONNode.ColorContainerType = JSONContainerType.Array;

            return customData;
        }
    }
}
