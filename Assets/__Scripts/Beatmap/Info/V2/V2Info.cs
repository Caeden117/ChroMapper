using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace Beatmap.Info
{
    public static class V2Info
    {
        public static BaseInfo GetFromJson(JSONNode node)
        {
            var info = new BaseInfo();

            info.Version = node["_version"].Value;
            info.SongName = node["_songName"].Value;
            info.SongSubName = node["_songSubName"].Value;
            info.SongAuthorName = node["_songAuthorName"].Value;
            
            info.LevelAuthorName = node["_levelAuthorName"].Value;
            
            info.BeatsPerMinute = node["_beatsPerMinute"].AsFloat;
            
            // Deprecated fields
            info.SongTimeOffset = node["_songTimeOffset"].AsFloat;
            info.Shuffle = node["_shuffle"].AsFloat;
            info.ShufflePeriod = node["_shufflePeriod"].AsFloat;

            info.PreviewStartTime = node["_previewStartTime"].AsFloat;
            info.PreviewDuration = node["_previewDuration"].AsFloat;

            info.SongFilename = node["_songFilename"].Value;
            info.CoverImageFilename = node["_coverImageFilename"].Value;

            info.EnvironmentName = node["_environmentName"].Value;
            info.EnvironmentNames = node["_environmentNames"].AsArray.Children.Select(x => x.Value).ToList();
            info.AllDirectionsEnvironmentName = node["_allDirectionsEnvironmentName"].Value;

            var colorSchemes = new List<InfoColorScheme>();
            var colorSchemeNodes = node["_colorSchemes"].AsArray.Children.Select(x => x.AsObject);
            foreach (var colorSchemeNode in colorSchemeNodes)
            {
                var colorScheme = new InfoColorScheme();
                colorScheme.UseOverride = colorSchemeNode["useOverride"].AsBool;
                colorScheme.ColorSchemeName = colorSchemeNode["colorScheme"]["colorSchemeId"];
                colorScheme.SaberAColor = colorSchemeNode["colorScheme"]["saberAColor"].ReadColor();
                colorScheme.SaberBColor = colorSchemeNode["colorScheme"]["saberBColor"].ReadColor();
                colorScheme.ObstaclesColor = colorSchemeNode["colorScheme"]["obstaclesColor"].ReadColor();
                colorScheme.EnvironmentColor0 = colorSchemeNode["colorScheme"]["environmentColor0"].ReadColor();
                colorScheme.EnvironmentColor1 = colorSchemeNode["colorScheme"]["environmentColor1"].ReadColor();
                colorScheme.EnvironmentColor0Boost = colorSchemeNode["colorScheme"]["environmentColor0Boost"].ReadColor();
                colorScheme.EnvironmentColor1Boost = colorSchemeNode["colorScheme"]["environmentColor1Boost"].ReadColor();
                colorSchemes.Add(colorScheme);
            }
            info.ColorSchemes = colorSchemes;

            var beatmapSets = new List<InfoDifficultySet>();
            var beatmapSetsNode = node["_difficultyBeatmapSets"].AsArray;
            foreach (var beatmapSetNode in beatmapSetsNode.Children)
            {
                var beatmapSet = new InfoDifficultySet();
                beatmapSet.Characteristic = beatmapSetNode["_beatmapCharacteristicName"].Value;
                
                var difficulties = new List<InfoDifficulty>();

                var beatmapsNode = beatmapSetNode["_difficultyBeatmaps"].AsArray;
                foreach (var beatmapNode in beatmapsNode.Children)
                {
                    var infoDifficulty = new InfoDifficulty(beatmapSet);
                    infoDifficulty.BeatmapFileName = beatmapNode["_beatmapFilename"].Value;
                    infoDifficulty.Difficulty = beatmapNode["_difficulty"].Value;
                    infoDifficulty.EnvironmentNameIndex = beatmapNode["_environmentNameIdx"].AsInt;
                    infoDifficulty.ColorSchemeIndex = beatmapNode["_beatmapColorSchemeIdx"].AsInt;
                    infoDifficulty.NoteJumpSpeed = beatmapNode["_noteJumpMovementSpeed"].AsFloat;
                    infoDifficulty.NoteStartBeatOffset = beatmapNode["_noteJumpStartBeatOffset"].AsFloat;

                    var customData = beatmapNode["_customData"].AsObject;
                    
                    ParseDifficultyCustomData(customData, infoDifficulty);

                    infoDifficulty.CustomData = customData;
                    
                    difficulties.Add(infoDifficulty);
                }

                beatmapSet.Difficulties = difficulties;

                var setCustomData = beatmapSetNode["_customData"].AsObject;

                ParseDifficultySetCustomData(setCustomData, beatmapSet);
                
                beatmapSet.CustomData = setCustomData;

                beatmapSets.Add(beatmapSet);
            }
            info.DifficultySets = beatmapSets;

            // CustomData Parsing
            if (node["_customData"].IsObject)
            {
                var customData = node["_customData"];

                if (customData["_contributors"].IsArray)
                {
                    info.CustomContributors = customData["_contributors"].AsArray.Children.Select(V2Contributor.GetFromJson).ToList();
                }

                if (customData["_editors"].IsObject)
                {
                    info.CustomEditorsData = new BaseInfo.CustomEditorsMetadata(customData["_editors"]);
                }
                
                info.CustomData = customData;
            }
            
            return info;
        }

        public static JSONNode GetOutputJson(BaseInfo info)
        {
            var json = new JSONObject();

            json["_version"] = "2.1.0";

            json["_songName"] = info.SongName;
            json["_songSubName"] = info.SongSubName;
            json["_songSubName"] = info.SongSubName;
            json["_songAuthorName"] = info.SongAuthorName;
            json["_levelAuthorName"] = info.LevelAuthorName;
            json["_beatsPerMinute"] = info.BeatsPerMinute;
            json["_songTimeOffset"] = info.SongTimeOffset;
            json["_shuffle"] = info.Shuffle;
            json["_shufflePeriod"] = info.ShufflePeriod;
            json["_previewStartTime"] = info.PreviewStartTime;
            json["_previewDuration"] = info.PreviewDuration;
            json["_songFilename"] = info.SongFilename;
            json["_coverImageFilename"] = info.CoverImageFilename;
            json["_environmentName"] = info.EnvironmentName;
            json["_allDirectionsEnvironmentName"] = info.AllDirectionsEnvironmentName;

            var environmentNames = new JSONArray();
            foreach (var environmentName in info.EnvironmentNames)
            {
                environmentNames.Add(environmentName);
            }
            json["_environmentNames"] = environmentNames;

            var colorSchemes = new JSONArray();
            foreach (var colorScheme in info.ColorSchemes)
            {
                var node = new JSONObject();
                node["useOverride"] = colorScheme.UseOverride;
                node["colorScheme"]["colorSchemeId"] = colorScheme.ColorSchemeName;
                node["colorScheme"]["saberAColor"] = colorScheme.SaberAColor;
                node["colorScheme"]["saberBColor"] = colorScheme.SaberBColor;
                node["colorScheme"]["obstaclesColor"] = colorScheme.ObstaclesColor;
                node["colorScheme"]["environmentColor0"] = colorScheme.EnvironmentColor0;
                node["colorScheme"]["environmentColor1"] = colorScheme.EnvironmentColor1;
                node["colorScheme"]["environmentColor0Boost"] = colorScheme.EnvironmentColor0Boost;
                node["colorScheme"]["environmentColor1Boost"] = colorScheme.EnvironmentColor1Boost;
                
                colorSchemes.Add(node);
            }
            json["_colorSchemes"] = colorSchemes;

            var beatmapSetArray = new JSONArray();
            foreach (var beatmapSet in info.DifficultySets)
            {
                var setNode = new JSONObject { ["_beatmapCharacteristicName"] = beatmapSet.Characteristic };
                var difficultyBeatmapsArray = new JSONArray();

                foreach (var difficulty in beatmapSet.Difficulties)
                {
                    var node = new JSONObject();
                    node["_difficulty"] = difficulty.Difficulty;
                    node["_difficultyRank"] = difficulty.DifficultyRank;
                    node["_beatmapFilename"] = difficulty.BeatmapFileName;
                    node["_noteJumpMovementSpeed"] = difficulty.NoteJumpSpeed;
                    node["_noteJumpStartBeatOffset"] = difficulty.NoteStartBeatOffset;
                    node["_beatmapColorSchemeIdx"] = difficulty.ColorSchemeIndex;
                    node["_environmentNameIdx"] = difficulty.EnvironmentNameIndex;

                    PopulateDifficultyCustomData(difficulty);
                    
                    node["_customData"] = difficulty.CustomData;
                    difficultyBeatmapsArray.Add(node);
                }

                setNode["_difficultyBeatmaps"] = difficultyBeatmapsArray;

                PopulateDifficultySetCustomData(beatmapSet);
                
                setNode["_customData"] = beatmapSet.CustomData;
                beatmapSetArray.Add(setNode);
            }

            json["_difficultyBeatmapSets"] = beatmapSetArray;

            // CustomData writing
            if (info.CustomContributors.Any())
            {
                var customContributors = new JSONArray();
                foreach (var customContributor in info.CustomContributors)
                {
                    customContributors.Add(V2Contributor.ToJson(customContributor));
                }
                
                info.CustomData["_contributors"] = customContributors;
            }
            
            info.CustomData["_editors"] = info.CustomEditorsData.ToJson();
            
            json["_customData"] = info.CustomData;
            
            return json;
        }
        
        private static void ParseDifficultySetCustomData(JSONNode customData, InfoDifficultySet difficultySet)
        {
            if (customData["_characteristicLabel"].IsString)
            {
                difficultySet.CustomCharacteristicLabel = customData["_characteristicLabel"].Value;
            }

            if (customData["_characteristicIconImageFilename"].IsString)
            {
                difficultySet.CustomCharacteristicIconImageFileName = customData["_characteristicIconImageFilename"].Value;
            }
        }

        private static void ParseDifficultyCustomData(JSONNode customData, InfoDifficulty difficulty)
        {
            if (customData["_oneSaber"].IsBoolean)
            {
                difficulty.CustomOneSaberFlag = customData["_oneSaber"].AsBool;
            }

            if (customData["_showRotationNoteSpawnLines"].IsBoolean)
            {
                difficulty.CustomShowRotationNoteSpawnLinesFlag = customData["_showRotationNoteSpawnLines"].AsBool;
            }

            if (customData["_difficultyLabel"].IsString)
            {
                difficulty.CustomLabel = customData["_difficultyLabel"].Value;
            }
            
            if (customData["_information"].IsArray)
            {
                difficulty.CustomInformation =
                    customData["_information"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["_warnings"].IsArray)
            {
                difficulty.CustomWarnings =
                    customData["_warnings"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["_suggestions"].IsArray)
            {
                difficulty.CustomSuggestions =
                    customData["_suggestions"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["_requirements"].IsArray)
            {
                difficulty.CustomRequirements =
                    customData["_requirements"].AsArray.Children.Select(x => x.Value).ToList();
            }

            if (customData["_colorLeft"].IsObject)
            {
                difficulty.CustomColorLeft = customData["_colorLeft"].ReadColor();
            }
            
            if (customData["_colorRight"].IsObject)
            {
                difficulty.CustomColorRight = customData["_colorRight"].ReadColor();
            }
            
            if (customData["_obstacleColor"].IsObject)
            {
                difficulty.CustomColorObstacle = customData["_obstacleColor"].ReadColor();
            }

            if (customData["_envColorLeft"].IsObject)
            {
                difficulty.CustomEnvColorLeft = customData["_envColorLeft"].ReadColor();
            }
            
            if (customData["_envColorRight"].IsObject)
            {
                difficulty.CustomEnvColorRight = customData["_envColorRight"].ReadColor();
            }
            
            if (customData["_envColorWhite"].IsObject)
            {
                difficulty.CustomEnvColorWhite = customData["_envColorWhite"].ReadColor();
            }
            
            if (customData["_envColorLeftBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostLeft = customData["_envColorLeftBoost"].ReadColor();
            }
            
            if (customData["_envColorRightBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostRight = customData["_envColorRightBoost"].ReadColor();
            }
            
            if (customData["_envColorWhiteBoost"].IsObject)
            {
                difficulty.CustomEnvColorBoostWhite = customData["_envColorWhiteBoost"].ReadColor();
            }
        }

        private static void PopulateDifficultySetCustomData(InfoDifficultySet difficultySet)
        {
            if (!string.IsNullOrWhiteSpace(difficultySet.CustomCharacteristicLabel))
            {
                difficultySet.CustomData["_characteristicLabel"] = difficultySet.CustomCharacteristicLabel;
            }
            
            if (!string.IsNullOrWhiteSpace(difficultySet.CustomCharacteristicIconImageFileName))
            {
                difficultySet.CustomData["_characteristicIconImageFilename"] = difficultySet.CustomCharacteristicIconImageFileName;
            }
        }

        private static void PopulateDifficultyCustomData(InfoDifficulty difficulty)
        {
            if (difficulty.CustomOneSaberFlag != null)
            {
                difficulty.CustomData["_oneSaber"] = difficulty.CustomOneSaberFlag.Value;
            }

            if (difficulty.CustomShowRotationNoteSpawnLinesFlag != null)
            {
                difficulty.CustomData["_showRotationNoteSpawnLines"] = difficulty.CustomShowRotationNoteSpawnLinesFlag.Value;
            }

            if (!string.IsNullOrWhiteSpace(difficulty.CustomLabel))
            {
                difficulty.CustomData["_difficultyLabel"] = difficulty.CustomLabel;
            }

            if (difficulty.CustomInformation.Any())
            {
                difficulty.CustomData["_information"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomInformation, s => s);
            }
            
            if (difficulty.CustomWarnings.Any())
            {
                difficulty.CustomData["_warnings"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomWarnings, s => s);
            }
            
            if (difficulty.CustomSuggestions.Any())
            {
                difficulty.CustomData["_suggestions"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomSuggestions, s => s);
            }
            
            if (difficulty.CustomRequirements.Any())
            {
                difficulty.CustomData["_requirements"] =
                    SimpleJSONHelper.MapSequenceToJSONArray(difficulty.CustomRequirements, s => s);
            }
            
            // SongCore saves colors in Object format so temporarily change container type
            JSONNode.ColorContainerType = JSONContainerType.Object;

            if (difficulty.CustomColorLeft != null)
            {
                difficulty.CustomData["_colorLeft"] = difficulty.CustomColorLeft.Value;
            }

            if (difficulty.CustomColorRight != null)
            {
                difficulty.CustomData["_colorRight"] = difficulty.CustomColorRight.Value;
            }

            if (difficulty.CustomColorObstacle != null)
            {
                difficulty.CustomData["_obstacleColor"] = difficulty.CustomColorObstacle.Value;
            }

            if (difficulty.CustomEnvColorLeft != null)
            {
                difficulty.CustomData["_envColorLeft"] = difficulty.CustomEnvColorLeft.Value;
            }

            if (difficulty.CustomEnvColorRight != null)
            {
                difficulty.CustomData["_envColorRight"] = difficulty.CustomEnvColorRight.Value;
            }

            if (difficulty.CustomEnvColorWhite != null)
            {
                difficulty.CustomData["_envColorWhite"] = difficulty.CustomEnvColorWhite.Value;
            }

            if (difficulty.CustomEnvColorBoostLeft != null)
            {
                difficulty.CustomData["_envColorLeftBoost"] = difficulty.CustomEnvColorBoostLeft.Value;
            }

            if (difficulty.CustomEnvColorBoostRight != null)
            {
                difficulty.CustomData["_envColorRightBoost"] = difficulty.CustomEnvColorBoostRight.Value;
            }

            if (difficulty.CustomEnvColorBoostWhite != null)
            {
                difficulty.CustomData["_envColorWhiteBoost"] = difficulty.CustomEnvColorBoostWhite.Value;
            }
            
            JSONNode.ColorContainerType = JSONContainerType.Array;
        }
    }
}
