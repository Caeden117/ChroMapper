using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    public class BaseInfo
    {
        // Editor Properties
        private string directory;

        public string Directory
        {
            get => directory;
            set
            {
                // Even though we set LastWriteTime here, it is indeed supposed to be the write time of Info.dat
                // The field is of the BaseInfo class and refers to the write time of the Info.dat file
                // The reason it's set here is because we only get the path information at this point
                LastWriteTime = File.GetLastWriteTime(Path.Combine(value, "Info.dat"));

                isFavourite = File.Exists(Path.Combine(value, ".favourite"));
                directory = value;
            }
        }
        
        // Used for sorting maps by last modified
        // The Info.dat file is written any time the map is saved, so it is a reliable way to see when the map was last modified
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
        public string Version { get; set; } = "4.0.1";
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
            .Aggregate(SongName, (res, el) => res.Replace(el.ToString(), string.Empty))
            .Trim('.'); // Windows disallows trailing periods and macOS treats leading period as hidden folder

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
        
        public string EnvironmentName => EnvironmentNames.FirstOrDefault() ?? "DefaultEnvironment";
        public string AllDirectionsEnvironmentName => "GlassDesertEnvironment";
        
        public List<string> EnvironmentNames { get; set; } = new() { "DefaultEnvironment" };

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

                
                var lastEditedByKey = BeatSaberSongContainer.Instance?.Info.MajorVersion switch
                {
                    2 => "_lastEditedBy",
                    4 => "lastEditedBy",
                    _ => "lastEditedBy"
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
}
