using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    // TODO: Parse CustomData for supported properties *and* preserve unknown fields
    public class BaseInfo
    {
        // Editor Properties
        private string directory;

        public string Directory
        {
            get => directory;
            set
            {
                LastWriteTime = System.IO.Directory.GetLastWriteTime(value);
                isFavourite = File.Exists(Path.Combine(value, ".favourite"));
                directory = value;
            }
        }
        
        public DateTime LastWriteTime { get; private set; }
        
        // These values piggy back off of Application.productName and Application.version here.
        // It's so that anyone maintaining a ChroMapper fork, but wants its identity to be separate, can easily just change
        // product name and the version from Project Settings, and have it automatically apply to the metadata.
        // But it's in their own fields because Unity cries like a little blyat when you access them directly from another thread.
        private static string editorName;
        private static string editorVersion;

        private bool isFavourite;

        public bool IsFavourite
        {
            get => isFavourite;
            set
            {
                var path = Path.Combine(Directory, ".favourite");
                lock (this)
                {
                    if (value)
                    {
                        File.Create(path).Dispose();
                        File.SetAttributes(path, FileAttributes.Hidden);
                    }
                    else
                    {
                        File.Delete(path);
                    }
                }

                isFavourite = value;
            }
        }

        public BaseInfo()
        {
            if (string.IsNullOrEmpty(editorName)) editorName = Application.productName;
            if (string.IsNullOrEmpty(editorVersion)) editorVersion = Application.version;
        }

        // Vanilla Properties
        public string Version { get; set; } = "4.0.0";
        public int MajorVersion
        {
            get
            {
                if (string.IsNullOrEmpty(Version))
                {
                    return -1;
                }

                return (int)char.GetNumericValue(Version[0]);
            }
        }

        public string SongName { get; set; } = "New Song";
        public string CleanSongName => Path.GetInvalidFileNameChars()
            .Aggregate(SongName, (res, el) => res.Replace(el.ToString(), string.Empty));
        public string SongSubName { get; set; } = "";
        public string SongAuthorName { get; set; } = "";

        public string LevelAuthorName { get; set; } = "";
        
        public float SongTimeOffset { get; set; }
        public float Shuffle { get; set; }
        public float ShufflePeriod { get; set; }

        public float BeatsPerMinute { get; set; } = 100;
        public float PreviewStartTime { get; set; } = 12;
        public float PreviewDuration { get; set; } = 10;
        public string SongFilename { get; set; } =
            "song.ogg"; // .egg file extension is a problem solely beat saver deals with, work with .ogg for the mapper
        public string SongPreviewFilename { get; set; } = "song.ogg"; // Default same as SongFilename
        public string AudioDataFilename { get; set; } = "AudioData.dat";
        public float SongDurationMetadata { get; set; }
        public float Lufs { get; set; } // Some normalisation thing 
        
        public string CoverImageFilename { get; set; } = "cover.png";

        // TODO: Could probably convert this to EnvironmentNames.FirstOrDefault() ?? "DefaultEnvironment";
        public string EnvironmentName { get; set; } = "DefaultEnvironment";
        
        // TODO: This might be better as just a constant. No other all directions environment exists in vanilla.
        public string AllDirectionsEnvironmentName { get; set; } = "GlassDesertEnvironment";
        
        public List<string> EnvironmentNames { get; set; } = new();

        public List<InfoColorScheme> ColorSchemes { get; set; } = new();

        public List<InfoDifficultySet> DifficultySets { get; set; } = new();
        
        
        // CustomData Properties
        public JSONNode CustomData = new JSONObject();

        public List<BaseContributor> CustomContributors = new();

        public CustomEditorsMetadata CustomEditorsData = new(null);
        public CustomEnvironmentMetadata CustomEnvironmentMetadata = new();
        
        public class CustomEditorsMetadata
        {
            // This is so we preserve existing data that might be located in this object
            private readonly JSONNode editorsNode;

            /// <summary>
            ///     Editor Metadata for this editor.
            /// </summary>
            public JSONNode MetadataNode = new JSONObject();
            
            public CustomEditorsMetadata(JSONNode obj)
            {
                if (obj is null || !obj.Children.Any())
                {
                    editorsNode = new JSONObject();
                }
                else
                {
                    editorsNode = obj;
                    if (editorsNode.HasKey(editorName)) MetadataNode = editorsNode[editorName];
                }
            }

            public JSONNode ToJson()
            {
                MetadataNode["version"] = editorVersion;

                var lastEditedByKey = BeatSaberSongContainer.Instance.Info.Version[0] switch
                {
                    '2' => "_lastEditedByKey",
                    '4' => "lastEditedByKey",
                    _ => "lastEditedByKey"
                };
                
                editorsNode[lastEditedByKey] = editorName;
                editorsNode[editorName] = MetadataNode;

                return editorsNode;
            }
        }
        
        
        public bool Save()
        {
            // Create map folder
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            
            var outputJson = Version[0] switch
            {
                '2' => V2Info.GetOutputJson(this),
                '4' => V4Info.GetOutputJson(this),
                _ => null
            };

            if (outputJson == null)
                return false;

            // Write info file - Previous behaviour always indented file
            File.WriteAllText(Path.Combine(Directory, "Info.dat"), outputJson.ToString(2));

            return true;
        }
    }

    public struct CustomEnvironmentMetadata
    {
        public string Name { get; set; }
        public string Hash { get; set; }
    }

    public class InfoDifficultySet
    {
        public string Characteristic { get; set; }
        public List<InfoDifficulty> Difficulties { get; set; } = new();

        public JSONObject CustomData { get; set; } = new(); // Pretty much just for v2 SongCore parsing
        
        public string CustomCharacteristicLabel { get; set; }
        public string CustomCharacteristicIconImageFileName { get; set; }
    }

    public class InfoDifficulty
    {
        public InfoDifficulty(InfoDifficultySet parentSet) => ParentSet = parentSet;
        public InfoDifficultySet ParentSet { get; }
        public string Characteristic => ParentSet.Characteristic;
        public string CustomCharacteristicLabel => ParentSet.CustomCharacteristicLabel;
        public string CustomCharacteristicIconImageFileName => ParentSet.CustomCharacteristicIconImageFileName;

        public string BeatmapFileName { get; set; }

        private string lightshowFileName = "Lightshow.dat";

        public string LightshowFileName
        {
            get => string.IsNullOrEmpty(lightshowFileName) ? BeatmapFileName : lightshowFileName;
            set => lightshowFileName = value;
        }

        public string Difficulty { get; set; }

        public int DifficultyRank => Difficulty switch
        {
            "ExpertPlus" => 9,
            "Expert+" => 9, // Yes this is valid
            "Expert" => 7,
            "Hard" => 5,
            "Normal" => 3,
            "Easy" => 1,
            _ => -1
        };

        public int EnvironmentNameIndex { get; set; }
        public int ColorSchemeIndex { get; set; }

        public float NoteJumpSpeed { get; set; }
        public float NoteStartBeatOffset { get; set; }

        public List<string> Mappers { get; set; } = new();
        public List<string> Lighters { get; set; } = new();

        // CustomData Properties
        public JSONObject CustomData { get; set; } = new();

        public string CustomLabel { get; set; }
        public bool? CustomOneSaberFlag { get; set; }
        public bool? CustomShowRotationNoteSpawnLinesFlag { get; set; }
        
        public List<string> CustomInformation { get; set; } = new();
        public List<string> CustomWarnings { get; set; } = new();
        public List<string> CustomSuggestions { get; set; } = new();
        public List<string> CustomRequirements { get; set; } = new();
        
        public Color? CustomColorLeft { get; set; }
        public Color? CustomColorRight { get; set; }
        public Color? CustomColorObstacle { get; set; }
        public Color? CustomEnvColorLeft { get; set; }
        public Color? CustomEnvColorRight { get; set; }
        public Color? CustomEnvColorWhite { get; set; }
        public Color? CustomEnvColorBoostLeft { get; set; }
        public Color? CustomEnvColorBoostRight { get; set; }
        public Color? CustomEnvColorBoostWhite { get; set; }

        public void SetBeatmapFileNameToDefault() => BeatmapFileName = $"{Difficulty}{Characteristic}.dat";
        
        public void RefreshRequirementsAndWarnings(BaseInfo info, BaseDifficulty map)
        {
            if (!Settings.Instance.AutomaticModRequirements) return;

            //Saving Map Requirement Info
            var requiredArray = new JSONArray(); //Generate suggestions and requirements array
            var suggestedArray = new JSONArray();

            foreach (var req in RequirementCheck.requirementsAndSuggestions)
            {
                switch (req.IsRequiredOrSuggested(this, map))
                {
                    case RequirementCheck.RequirementType.Requirement:
                        requiredArray.Add(req.Name);
                        break;
                    case RequirementCheck.RequirementType.Suggestion:
                        suggestedArray.Add(req.Name);
                        break;
                }
            }

            if (requiredArray.Count > 0)
            {
                CustomData ??= new JSONObject();
                CustomData[info.MajorVersion == 4 ? "requirements" : "_requirements"] = requiredArray;
            }
            else if (CustomData != null)
            {
                CustomData.Remove("requirements");
                CustomData.Remove("_requirements");
            }

            if (suggestedArray.Count > 0)
            {
                CustomData ??= new JSONObject();
                CustomData[info.MajorVersion == 4 ? "suggestions" : "_suggestions"] = suggestedArray;
            }
            else if (CustomData != null)
            {
                CustomData.Remove("suggestions");
                CustomData.Remove("_suggestions");
            }
        }
    }

    public class InfoColorScheme
    {
        public bool UseOverride { get; set; }
        public string ColorSchemeName { get; set; }
        public Color SaberAColor { get; set; }
        public Color SaberBColor { get; set; }
        public Color ObstaclesColor { get; set; }
        public Color EnvironmentColor0 { get; set; }
        public Color EnvironmentColor1 { get; set; }
        public Color EnvironmentColor0Boost { get; set; }
        public Color EnvironmentColor1Boost { get; set; }
    }
}
