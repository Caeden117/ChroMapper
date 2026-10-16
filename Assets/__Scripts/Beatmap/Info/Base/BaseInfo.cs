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
                LastWriteTime = File.GetLastWriteTime(PathUtils.Combine(value, "Info.dat"));

                isFavourite = File.Exists(PathUtils.Combine(value, ".favourite"));
                CustomEditorsData.SetStateDirectory(value);
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
                var path = PathUtils.Combine(Directory, ".favourite");
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

        // Windows has reserved names that should not be used by folders or files
        private static readonly HashSet<string> ReservedNamesWindows = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        public string CleanSongName
        {
            get
            {   
                var cleaned = Path.GetInvalidFileNameChars()
                    .Aggregate(SongName, (res, el) => res.Replace(el.ToString(), string.Empty))
                    .Trim('.'); // Windows disallows trailing periods and macOS treats leading period as hidden folder

                if (ReservedNamesWindows.Contains(cleaned))
                {
                    return string.Empty;
                }

                return cleaned;
            }
        }

        public string SongSubName { get; set; } = "";
        public string SongAuthorName { get; set; } = "";

        public string LevelAuthorName { get; set; } = "";

        public float SongTimeOffset { get; set; }
        public float Shuffle { get; set; }
        public float ShufflePeriod { get; set; }

        public float BeatsPerMinute { get; set; } = 100;
        public float PreviewStartTime { get; set; } = 12;
        public float PreviewDuration { get; set; } = 10;

        private string songFilename = "song.ogg";
        private string songPreviewFilename = "song.ogg";
        private string audioDataFilename = "AudioData.dat";
        private string coverImageFilename = "cover.png";

        public string SongFilename
        {
            get => songFilename;
            set => songFilename = PathUtils.RequireFileName(value);
        }

        public string SongPreviewFilename
        {
            get => songPreviewFilename;
            set => songPreviewFilename = PathUtils.RequireFileName(value);
        }

        public string AudioDataFilename
        {
            get => audioDataFilename;
            set => audioDataFilename = PathUtils.RequireFileName(value);
        }

        public float SongDurationMetadata { get; set; }
        public float Lufs { get; set; } // Some normalisation thing 

        public string CoverImageFilename
        {
            get => coverImageFilename;
            set => coverImageFilename = PathUtils.RequireFileName(value);
        }

        public string EnvironmentName { get; set; } = "DefaultEnvironment";
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
            private const string VersionKey = "_version";
            private const string StateFileKey = "_state";
            private const string LegacyVersionKey = "version";
            private const string EditorStateKey = "editorState";
            private static readonly char[] invalidStateFileNameCharacters = "<>\"|?*".ToCharArray();

            // This is so we preserve existing data that might be located in this object
            private readonly JSONNode editorsNode;
            private JSONNode metadataNode = new JSONObject();
            private string sourceDirectory;
            private bool stateLoaded = true;

            public string StateFileName { get; private set; } = "CmState.dat";

            /// <summary>
            ///     Editor Metadata for this editor.
            /// </summary>
            public JSONNode MetadataNode
            {
                get
                {
                    LoadState();
                    return metadataNode;
                }
                set
                {
                    metadataNode = value;
                    stateLoaded = true;
                }
            }

            public CustomEditorsMetadata(JSONNode obj)
            {
                editorsNode = obj ?? new JSONObject();
                if (editorsNode.Count == 0 || !editorsNode.HasKey(editorName))
                    return;

                metadataNode = editorsNode[editorName].Clone();
                metadataNode.Remove(LegacyVersionKey);
                metadataNode.Remove(VersionKey);
                if (!metadataNode.HasKey(StateFileKey))
                    return;

                StateFileName = PathUtils.RequireFileName(metadataNode[StateFileKey].Value);
                if (string.IsNullOrWhiteSpace(StateFileName)
                    || StateFileName.IndexOfAny(invalidStateFileNameCharacters) >= 0
                    || StateFileName.Any(char.IsControl)
                    || StateFileName.EndsWith(" ", StringComparison.Ordinal)
                    || StateFileName.EndsWith(".", StringComparison.Ordinal)
                    || ReservedNamesWindows.Contains(Path.GetFileNameWithoutExtension(StateFileName))
                    || string.Equals(StateFileName, "Info.dat", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Editor state requires a filename in the song folder, not a path: " + StateFileName);
                }

                metadataNode.Remove(StateFileKey);
                metadataNode.Remove(EditorStateKey);
                stateLoaded = false;
            }

            // Keep the original folder for a deferred read if the map is moved before its state is opened.
            internal void SetStateDirectory(string value) => sourceDirectory ??= value;

            private void LoadState()
            {
                if (stateLoaded || sourceDirectory == null)
                    return;

                var data = BeatSaberSongUtils.GetNodeFromFile(PathUtils.Combine(sourceDirectory, StateFileName));
                metadataNode[EditorStateKey] = data?.AsObject ?? new JSONObject();
                stateLoaded = true;
            }

            // Keep editor data available through the same API for embedded legacy maps and separate state files.
            public JSONNode GetEditorData(string key) => MetadataNode[key];

            public void SetEditorData(string key, JSONNode data) => MetadataNode[key] = data;

            internal void Save(string directory)
            {
                LoadState();
                var editorState = metadataNode[EditorStateKey].AsObject ?? new JSONObject();
                File.WriteAllText(PathUtils.Combine(directory, StateFileName), editorState.ToString(2));
            }

            public JSONNode ToJson()
            {
                // Unity song containers need explicit null checks before selecting the metadata key version.
                var songContainer = BeatSaberSongContainer.Instance;
                var info = songContainer != null ? songContainer.Info : null;
                var lastEditedByKey = info != null 
                    ? info.MajorVersion switch
                    {
                        2 => "_lastEditedBy",
                        4 => "lastEditedBy",
                        _ => "lastEditedBy"
                    } 
                    : "lastEditedBy";

                editorsNode[lastEditedByKey] = editorName;
                // Only editorState moves to the state file. Keep any other editor metadata in Info.dat.
                var editorMetadata = metadataNode.Clone();
                editorMetadata.Remove(EditorStateKey);
                editorMetadata[VersionKey] = editorVersion;
                editorMetadata[StateFileKey] = StateFileName;
                editorsNode[editorName] = editorMetadata;

                return editorsNode;
            }
        }

        public bool Save()
        {
            // Create map folder
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);

            // Sync the legacy top-level environment name to the first entry in the per-difficulty
            // environment names list (which corresponds to the first/expert+ difficulty environment).
            if (EnvironmentNames != null && EnvironmentNames.Count > 0)
            {
                EnvironmentName = EnvironmentNames[0];
            }

            var outputJson = Version[0] switch
            {
                '2' => V2Info.GetOutputJson(this),
                '4' => V4Info.GetOutputJson(this),
                _ => null
            };

            if (outputJson == null) return false;

            CustomEditorsData.Save(Directory);

            // Write info file - Previous behaviour always indented file
            File.WriteAllText(PathUtils.Combine(Directory, "Info.dat"), outputJson.ToString(2));

            return true;
        }
    }
}
