using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Helper;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable][Obsolete]
public class BeatSaberSong
{
    // These values piggy back off of Application.productName and Application.version here.
    // It's so that anyone maintaining a ChroMapper fork, but wants its identity to be separate, can easily just change
    // product name and the version from Project Settings, and have it automatically apply to the metadata.
    // But it's in their own fields because Unity cries like a little blyat when you access them directly from another thread.
    private static string editorName;
    private static string editorVersion;

    public DateTime LastWriteTime;
    [FormerlySerializedAs("songName")] public string SongName = "New Song";
    [FormerlySerializedAs("directory")] public string Directory;

    [FormerlySerializedAs("version")] public string Version = "2.1.0";
    [FormerlySerializedAs("songSubName")] public string SongSubName = "";
    [FormerlySerializedAs("songAuthorName")] public string SongAuthorName = "";
    [FormerlySerializedAs("levelAuthorName")] public string LevelAuthorName = "";
    [FormerlySerializedAs("beatsPerMinute")] public float BeatsPerMinute = 100;
    [FormerlySerializedAs("songTimeOffset")] public float SongTimeOffset;
    [FormerlySerializedAs("previewStartTime")] public float PreviewStartTime = 12;
    [FormerlySerializedAs("previewDuration")] public float PreviewDuration = 10;
    [FormerlySerializedAs("shuffle")] public float Shuffle;
    [FormerlySerializedAs("shufflePeriod")] public float ShufflePeriod = 0.5f;

    [FormerlySerializedAs("songFilename")]
    public string SongFilename =
            "song.ogg"; // .egg file extension is a problem solely beat saver deals with, work with .ogg for the mapper

    [FormerlySerializedAs("coverImageFilename")] public string CoverImageFilename = "cover.png";
    [FormerlySerializedAs("environmentName")] public string EnvironmentName = "DefaultEnvironment";
    [FormerlySerializedAs("allDirectionsEnvironmentName")] public string AllDirectionsEnvironmentName = "GlassDesertEnvironment";
    public List<string> EnvironmentNames = new(); // TODO: Support editing
    public JSONNode ColorSchemes = new JSONArray(); // TODO: Support editing
    [FormerlySerializedAs("editors")] public EditorsObject Editors = new(null);

    [FormerlySerializedAs("difficultyBeatmapSets")] public List<DifficultyBeatmapSet> DifficultyBeatmapSets = new();

    [FormerlySerializedAs("warnings")] public List<string> Warnings = new();
    [FormerlySerializedAs("suggestions")] public List<string> Suggestions = new();
    [FormerlySerializedAs("requirements")] public List<string> Requirements = new();
    [FormerlySerializedAs("contributors")] public List<BaseContributor> Contributors = new();

    private bool isFavourite;
    public JSONNode CustomData;
    public JSONNode Json;

    public BeatSaberSong(string directory, JSONNode json)
    {
        Directory = directory;
        Json = json;
        LastWriteTime = System.IO.Directory.GetLastWriteTime(Directory);
        isFavourite = File.Exists(Path.Combine(directory, ".favourite"));
        TouchEditorValues();
    }

    public BeatSaberSong(string directory, string songName)
    {
        if (!(string.IsNullOrEmpty(songName) || string.IsNullOrWhiteSpace(songName))) SongName = songName;
        Directory = Path.Combine(directory, CleanSongName);
        Json = null;
        TouchEditorValues();
        Editors = new EditorsObject(null);
    }

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

    public string CleanSongName => Path.GetInvalidFileNameChars()
        .Aggregate(SongName, (res, el) => res.Replace(el.ToString(), string.Empty));

    // As crazy as this may seem, we do actually need to define them separately so that Unity doesn't
    // whine like a baby when we access Application.productName or Application.version on another thread.
    private void TouchEditorValues()
    {
        if (string.IsNullOrEmpty(editorName)) editorName = Application.productName;
        if (string.IsNullOrEmpty(editorVersion)) editorVersion = Application.version;
    }

    public void SaveSong()
    {
        try
        {
            // Create map folder
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);

            if (Json == null) Json = new JSONObject();
            if (CustomData == null) CustomData = new JSONObject();

            //Just in case, i'm moving them up here
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            Json["_version"] = Version;
            Json["_songName"] = SongName;
            Json["_songSubName"] = SongSubName;
            Json["_songAuthorName"] = SongAuthorName;
            Json["_levelAuthorName"] = LevelAuthorName;

            Json["_beatsPerMinute"] = BeatsPerMinute;
            Json["_previewStartTime"] = PreviewStartTime;
            Json["_previewDuration"] = PreviewDuration;
            Json["_songTimeOffset"] = SongTimeOffset;

            Json["_shuffle"] = Shuffle;
            Json["_shufflePeriod"] = ShufflePeriod;

            Json["_coverImageFilename"] = CoverImageFilename;
            Json["_songFilename"] = SongFilename;

            Json["_environmentName"] = EnvironmentName;
            Json["_allDirectionsEnvironmentName"] = AllDirectionsEnvironmentName;

            var infoEnvironmentNames = new JSONArray();
            foreach (var environmentName in EnvironmentNames) { infoEnvironmentNames.Add(environmentName); }
            Json["_environmentNames"] = infoEnvironmentNames;

            Json["_colorSchemes"] = ColorSchemes;

            Json["_customData"] = CustomData;
            Json["_customData"]["_editors"] = Editors.ToJsonNode();

            // Remove old "_editor" string with the new "_editors" object
            if (Json["_customData"].HasKey("_editor")) Json["_customData"].Remove("_editor");

            if (Contributors.Any())
            {
                var contributorArray = new JSONArray();
                Contributors.ForEach(x => contributorArray.Add(V2Contributor.ToJson(x)));
                if (Contributors.Any()) Json["_customData"]["_contributors"] = contributorArray;
            }

            //BeatSaver schema changes, CleanObject function
            Json["_customData"] = SimpleJSONHelper.CleanObject(Json["_customData"]);
            if (Json["_customData"] is null || Json["_customData"].Count <= 0) Json.Remove("_customData");

            var sets = new JSONArray();
            foreach (var set in DifficultyBeatmapSets)
            {
                if (!set.DifficultyBeatmaps.Any()) continue;
                JSONNode setNode = new JSONObject();
                setNode["_beatmapCharacteristicName"] = set.BeatmapCharacteristicName;
                if (set.CustomData != null && set.CustomData.Count > 0)
                {
                    setNode["_customData"] = set.CustomData;
                }

                var diffs = new JSONArray();
                IEnumerable<DifficultyBeatmap> sortedBeatmaps = set.DifficultyBeatmaps.OrderBy(x => x.DifficultyRank);
                foreach (var diff in sortedBeatmaps)
                {
                    var map = GetMapFromDifficultyBeatmap(diff);
                    if (map != null)
                        diff.RefreshRequirementsAndWarnings(map);

                    JSONNode subNode = new JSONObject();

                    subNode["_difficulty"] = diff.Difficulty;
                    subNode["_difficultyRank"] = diff.DifficultyRank;
                    subNode["_beatmapFilename"] = diff.BeatmapFilename;
                    subNode["_noteJumpMovementSpeed"] = diff.NoteJumpMovementSpeed;
                    subNode["_noteJumpStartBeatOffset"] = diff.NoteJumpStartBeatOffset;
                    subNode["_beatmapColorSchemeIdx"] = diff.BeatmapColorSchemeIndex;
                    subNode["_environmentNameIdx"] = diff.EnvironmentNameIndex;
                    subNode["_customData"] = diff.CustomData ?? new JSONObject();

                    /*
                     * Chroma saves colors in the Array format:
                     * [r, g, b]
                     * 
                     * ...while SongCore saves in the Object format:
                     * { "r": r, "g": g, "b": b }
                     * 
                     * Well, fuck. This is why we can't have nice things.
                     * 
                     * So my totally not hacky fix is to assign what container type they'll save in, and revert it back when I'm done.
                     */
                    JSONNode.ColorContainerType = JSONContainerType.Object;

                    if (diff.ColorLeft != null)
                        subNode["_customData"]["_colorLeft"] = diff.ColorLeft;
                    else subNode["_customData"].Remove("_colorLeft");

                    if (diff.ColorRight != null)
                        subNode["_customData"]["_colorRight"] = diff.ColorRight;
                    else subNode["_customData"].Remove("_colorRight");

                    if (diff.EnvColorLeft != null)
                        subNode["_customData"]["_envColorLeft"] = diff.EnvColorLeft;
                    else subNode["_customData"].Remove("_envColorLeft");

                    if (diff.EnvColorRight != null)
                        subNode["_customData"]["_envColorRight"] = diff.EnvColorRight;
                    else subNode["_customData"].Remove("_envColorRight");

                    if (diff.EnvColorWhite != null)
                        subNode["_customData"]["_envColorWhite"] = diff.EnvColorWhite;
                    else subNode["_customData"].Remove("_envColorWhite");

                    if (diff.BoostColorLeft != null)
                        subNode["_customData"]["_envColorLeftBoost"] = diff.BoostColorLeft;
                    else subNode["_customData"].Remove("_envColorLeftBoost");

                    if (diff.BoostColorRight != null)
                        subNode["_customData"]["_envColorRightBoost"] = diff.BoostColorRight;
                    else subNode["_customData"].Remove("_envColorRightBoost");

                    if (diff.BoostColorWhite != null)
                        subNode["_customData"]["_envColorWhiteBoost"] = diff.BoostColorWhite;
                    else subNode["_customData"].Remove("_envColorWhiteBoost");

                    if (diff.ObstacleColor != null)
                        subNode["_customData"]["_obstacleColor"] = diff.ObstacleColor;
                    else subNode["_customData"].Remove("_obstacleColor");

                    JSONNode.ColorContainerType = JSONContainerType.Array;

                    /*
                     * More BeatSaver Schema changes, yayyyyy! (fuck)
                     * If any additional non-required fields are present, they cannot be empty.
                     * 
                     * So ChroMapper is just gonna yeet anything that is null or empty, then keep going down the list.
                     * If customData is empty, then we just yeet that.
                     */
                    if (subNode["_customData"] != null)
                    {
                        subNode["_customData"] = SimpleJSONHelper.CleanObject(subNode["_customData"]);
                        if (subNode["_customData"].Count <= 0) subNode.Remove("_customData");
                    }
                    else
                    {
                        subNode.Remove("_customData"); //Just remove it if it's null lmao
                    }

                    diffs.Add(subNode);
                }

                setNode["_difficultyBeatmaps"] = diffs;
                sets.Add(setNode);
            }

            Json["_difficultyBeatmapSets"] = sets;

            var info = new FileInfo(Directory + "/Info.dat");
            //No, patrick, not existing does not mean it is read only.
            if (!info.IsReadOnly || !info.Exists)
            {
                using (var writer = new StreamWriter(Directory + "/Info.dat", false))
                {
                    writer.Write(Json.ToString(2));
                }
            }
            else
            {
                PersistentUI.Instance.ShowDialogBox("PersistentUI", "readonly",
                    null, PersistentUI.DialogBoxPresetType.Ok);
                Debug.LogError(":hyperPepega: :mega: DONT MAKE YOUR MAP FILES READONLY");
            }

            Debug.Log("Saved song Info.dat for " + SongName);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError(
                "This is bad. You are recommended to restart ChroMapper; progress made after this point is not guaranteed to be saved.");
        }
    }

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
    
    [Obsolete("Use GetInfoFromFolder", true)]
    public static BeatSaberSong GetSongFromFolder(string directory)
    {
        try
        {
            //"excuse me this is not a schema change" ~lolPants
            //...after saying that beatsaver will stop accepting "info.dat" for uploading in the near future monkaHMMMMMMM
            var mainNode = GetNodeFromFile(directory + "/Info.dat");
            if (mainNode == null)
            {
                //Virgin "info.dat" VS chad "Info.dat"
                mainNode = GetNodeFromFile(directory + "/info.dat");
                if (mainNode == null) return null;
                File.Move(directory + "/info.dat", directory + "/Info.dat");
            }

            var song = new BeatSaberSong(directory, mainNode);
            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "_songName":
                        song.SongName = node.Value;
                        break;
                    case "_songSubName":
                        song.SongSubName = node.Value;
                        break;
                    case "_songAuthorName":
                        song.SongAuthorName = node.Value;
                        break;
                    case "_levelAuthorName":
                        song.LevelAuthorName = node.Value;
                        break;

                    case "_beatsPerMinute":
                        song.BeatsPerMinute = node.AsFloat;
                        break;
                    case "_songTimeOffset":
                        song.SongTimeOffset = node.AsFloat;
                        break;
                    case "_previewStartTime":
                        song.PreviewStartTime = node.AsFloat;
                        break;
                    case "_previewDuration":
                        song.PreviewDuration = node.AsFloat;
                        break;

                    case "_shuffle":
                        song.Shuffle = node.AsFloat;
                        break;
                    case "_shufflePeriod":
                        song.ShufflePeriod = node.AsFloat;
                        break;

                    case "_coverImageFilename":
                        song.CoverImageFilename = node.Value;
                        break;
                    case "_songFilename":
                        song.SongFilename = node.Value;
                        break;
                    case "_environmentName":
                        song.EnvironmentName = node.Value;
                        break;
                    //Because there is only one option, I wont load from file.
                    //case "_allDirectionsEnvironmentName": song.allDirectionsEnvironmentName = node.Value; break;

                    case "_environmentNames":
                        song.EnvironmentNames = node.AsArray.Children.Select(x => x.Value).ToList();
                        break;

                    case "_colorSchemes":
                        song.ColorSchemes = node;
                        break;

                    case "_customData":
                        song.CustomData = node;
                        if (node.HasKey("_contributors"))
                        {
                            foreach (JSONNode contributor in song.CustomData["_contributors"])
                                song.Contributors.Add(V2Contributor.GetFromJson(contributor));
                        }

                        song.Editors = new EditorsObject(node["_editors"]);
                        break;

                    case "_difficultyBeatmapSets":
                        foreach (JSONNode n in node)
                        {
                            var set = new DifficultyBeatmapSet
                            {
                                BeatmapCharacteristicName = n["_beatmapCharacteristicName"],
                                CustomData = n["_customData"]
                            };
                            foreach (JSONNode d in n["_difficultyBeatmaps"])
                            {
                                var beatmap = new DifficultyBeatmap(set)
                                {
                                    Difficulty = d["_difficulty"].Value,
                                    DifficultyRank = d["_difficultyRank"].AsInt,
                                    NoteJumpMovementSpeed = d["_noteJumpMovementSpeed"].AsFloat,
                                    NoteJumpStartBeatOffset = d["_noteJumpStartBeatOffset"].AsFloat,
                                    BeatmapColorSchemeIndex = d.HasKey("_beatmapColorSchemeIdx") ? d["_beatmapColorSchemeIdx"].AsInt : 0,
                                    EnvironmentNameIndex = d.HasKey("_environmentNameIdx") ? d["_environmentNameIdx"].AsInt : 0,
                                    CustomData = d["_customData"]
                                };
                                if (d["_customData"]["_colorLeft"] != null)
                                    beatmap.ColorLeft = d["_customData"]["_colorLeft"].ReadColor();
                                if (d["_customData"]["_colorRight"] != null)
                                    beatmap.ColorRight = d["_customData"]["_colorRight"].ReadColor();

                                if (d["_customData"]["_envColorLeft"] != null)
                                    beatmap.EnvColorLeft = d["_customData"]["_envColorLeft"].ReadColor();
                                if (d["_customData"]["_envColorRight"] != null)
                                    beatmap.EnvColorRight = d["_customData"]["_envColorRight"].ReadColor();
                                if (d["_customData"]["_envColorWhite"] != null)
                                    beatmap.EnvColorWhite = d["_customData"]["_envColorWhite"].ReadColor();

                                if (d["_customData"]["_envColorLeftBoost"] != null)
                                    beatmap.BoostColorLeft = d["_customData"]["_envColorLeftBoost"].ReadColor();
                                if (d["_customData"]["_envColorRightBoost"] != null)
                                    beatmap.BoostColorRight = d["_customData"]["_envColorRightBoost"].ReadColor();
                                if (d["_customData"]["_envColorWhiteBoost"] != null)
                                    beatmap.BoostColorWhite = d["_customData"]["_envColorWhiteBoost"].ReadColor();

                                if (d["_customData"]["_obstacleColor"] != null)
                                    beatmap.ObstacleColor = d["_customData"]["_obstacleColor"].ReadColor();
                                beatmap.UpdateName(d["_beatmapFilename"]);
                                set.DifficultyBeatmaps.Add(beatmap);
                            }

                            // If there are already difficulties ignore duplicates of the same difficulty
                            set.DifficultyBeatmaps = set.DifficultyBeatmaps.DistinctBy(it => it.DifficultyRank)
                                .OrderBy(x => x.DifficultyRank).ToList();
                            song.DifficultyBeatmapSets.Add(set);
                        }

                        song.DifficultyBeatmapSets = song.DifficultyBeatmapSets
                            .GroupBy(it => it.BeatmapCharacteristicName)
                            .Select(it =>
                            {
                                var container = it.First();
                                container.DifficultyBeatmaps = it.SelectMany(a => a.DifficultyBeatmaps).ToList();

                                return container;
                            })
                            .OrderBy(x =>
                                SongInfoEditUI.CharacteristicDropdownToBeatmapName.IndexOf(x.BeatmapCharacteristicName))
                            .ToList();
                        break;
                }
            }

            return song;
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

    public BaseDifficulty GetMapFromDifficultyBeatmap(DifficultyBeatmap data)
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            Debug.LogWarning("Failed to get difficulty json file.");
            return null;
        }
        var fullPath = Path.Combine(Directory, data.BeatmapFilename);

        var mainNode = GetNodeFromFile(fullPath);
        if (mainNode == null)
        {
            Debug.LogWarning("Failed to get difficulty json file " + fullPath);
            return null;
        }

        return BeatmapFactory.GetDifficultyFromJson(mainNode, fullPath);
    }

    private static JSONNode GetNodeFromFile(string file)
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

    [Serializable][Obsolete("Use InfoDifficulty", true)]
    public class DifficultyBeatmap
    {
        [FormerlySerializedAs("difficulty")] public string Difficulty = "Easy";
        [FormerlySerializedAs("difficultyRank")] public int DifficultyRank = 1;
        [FormerlySerializedAs("beatmapFilename")] public string BeatmapFilename = "Easy.dat";
        [FormerlySerializedAs("noteJumpMovementSpeed")] public float NoteJumpMovementSpeed = 16;
        [FormerlySerializedAs("noteJumpStartBeatOffset")] public float NoteJumpStartBeatOffset;
        public int BeatmapColorSchemeIndex;
        public int EnvironmentNameIndex;
        [FormerlySerializedAs("colorLeft")] public Color? ColorLeft;
        [FormerlySerializedAs("colorRight")] public Color? ColorRight;
        [FormerlySerializedAs("envColorLeft")] public Color? EnvColorLeft;
        [FormerlySerializedAs("envColorRight")] public Color? EnvColorRight;
        public Color? EnvColorWhite;
        [FormerlySerializedAs("boostColorLeft")] public Color? BoostColorLeft;
        [FormerlySerializedAs("boostColorRight")] public Color? BoostColorRight;
        public Color? BoostColorWhite;
        [FormerlySerializedAs("obstacleColor")] public Color? ObstacleColor;
        public JSONNode CustomData;
        [NonSerialized] public DifficultyBeatmapSet ParentBeatmapSet;

        public DifficultyBeatmap(DifficultyBeatmapSet beatmapSet)
        {
            ParentBeatmapSet = beatmapSet;
            UpdateName();
        }

        public void UpdateParent(DifficultyBeatmapSet newParentSet) => ParentBeatmapSet = newParentSet;

        public void UpdateName(string fileName = null)
        {
            if (fileName is null) BeatmapFilename = $"{Difficulty}{ParentBeatmapSet.BeatmapCharacteristicName}.dat";
            else BeatmapFilename = fileName;
        }

        public void RefreshRequirementsAndWarnings(BaseDifficulty map)
        {
            if (!Settings.Instance.AutomaticModRequirements) return;

            //Saving Map Requirement Info
            var requiredArray = new JSONArray(); //Generate suggestions and requirements array
            var suggestedArray = new JSONArray();

            foreach (var req in RequirementCheck.requirementsAndSuggestions)
            {
                // switch (req.IsRequiredOrSuggested(this, map))
                // {
                //     case RequirementCheck.RequirementType.Requirement:
                //         requiredArray.Add(req.Name);
                //         break;
                //     case RequirementCheck.RequirementType.Suggestion:
                //         suggestedArray.Add(req.Name);
                //         break;
                // }
            }

            if (requiredArray.Count > 0)
            {
                if (CustomData == null) CustomData = new JSONObject();
                CustomData["_requirements"] = requiredArray;
            }
            else if (CustomData != null)
            {
                if (CustomData.HasKey("_requirements")) CustomData.Remove("_requirements");
            }

            if (suggestedArray.Count > 0)
            {
                if (CustomData == null) CustomData = new JSONObject();
                CustomData["_suggestions"] = suggestedArray;
            }
            else if (CustomData != null)
            {
                if (CustomData.HasKey("_suggestions")) CustomData.Remove("_suggestions");
            }

            if (CustomData != null && CustomData.Count == 0) CustomData = null;
        }

        public JSONNode GetOrCreateCustomData()
        {
            if (CustomData == null)
                CustomData = new JSONObject();

            return CustomData;
        }
    }

    [Serializable][Obsolete("Use InfoDifficultySet")]
    public class DifficultyBeatmapSet
    {
        [FormerlySerializedAs("beatmapCharacteristicName")] public string BeatmapCharacteristicName = "Standard";
        [FormerlySerializedAs("difficultyBeatmaps")] public List<DifficultyBeatmap> DifficultyBeatmaps = new();
        public JSONNode CustomData = new JSONObject();

        public DifficultyBeatmapSet() => BeatmapCharacteristicName = "Standard";
        public DifficultyBeatmapSet(string characteristicName) => BeatmapCharacteristicName = characteristicName;
    }

    /// <summary>
    ///     Special object that represents "_customData._editors", and contains special metadata pertaining to each editor.
    /// </summary>
    [Serializable]
    public class EditorsObject
    {
        private readonly JSONNode editorsObject;

        /// <summary>
        ///     Editor Metadata for this editor.
        /// </summary>
        public JSONNode EditorMetadata = new JSONObject();

        public EditorsObject(JSONNode obj)
        {
            if (obj is null || !obj.Children.Any())
            {
                editorsObject = new JSONObject();
            }
            else
            {
                editorsObject = obj;
                if (editorsObject.HasKey(editorName)) EditorMetadata = editorsObject[editorName];
            }
        }

        public JSONNode ToJsonNode()
        {
            EditorMetadata["version"] = editorVersion;

            editorsObject["_lastEditedBy"] = editorName;
            editorsObject[editorName] = EditorMetadata;

            return editorsObject;
        }
    }
}
