using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BeatSaberSong
{
    public static readonly Color DefaultLeftColor = Color.red;
    public static readonly Color DefaultRightColor = new Color(0, 0.282353f, 1, 1);
    public static readonly Color DefaultLeftNote = new Color(0.7352942f, 0, 0);
    public static readonly Color DefaultRightNote = new Color(0, 0.3701827f, 0.7352942f);

    // These values piggy back off of Application.productName and Application.version here.
    // It's so that anyone maintaining a ChroMapper fork, but wants its identity to be separate, can easily just change
    // product name and the version from Project Settings, and have it automatically apply to the metadata.
    // But it's in their own fields because Unity cries like a little blyat when you access them directly from another thread.
    private static string editorName;
    private static string editorVersion;

    public DateTime LastWriteTime;
    [FormerlySerializedAs("songName")] public string SongName = "New Song";
    [FormerlySerializedAs("directory")] public string Directory;

    [FormerlySerializedAs("version")] public string Version = "2.0.0";
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
    [FormerlySerializedAs("editors")] public EditorsObject Editors = new EditorsObject(null);

    [FormerlySerializedAs("difficultyBeatmapSets")] public List<DifficultyBeatmapSet> DifficultyBeatmapSets = new List<DifficultyBeatmapSet>();

    [FormerlySerializedAs("warnings")] public List<string> Warnings = new List<string>();
    [FormerlySerializedAs("suggestions")] public List<string> Suggestions = new List<string>();
    [FormerlySerializedAs("requirements")] public List<string> Requirements = new List<string>();
    [FormerlySerializedAs("contributors")] public List<MapContributor> Contributors = new List<MapContributor>();

    private readonly bool isWipMap;

    private readonly string stagedDirectory;
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

    public BeatSaberSong(bool wipmap, string name = "")
    {
        Directory = null;
        Json = null;
        if (!(string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))) SongName = name;
        stagedDirectory = CleanSongName;
        isWipMap = wipmap;
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
            if (string.IsNullOrEmpty(Directory))
            {
                Directory = Path.Combine(
                    isWipMap ? Settings.Instance.CustomWIPSongsFolder : Settings.Instance.CustomSongsFolder,
                    stagedDirectory ?? CleanSongName);
            }

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
            Json["_customData"] = CustomData;
            Json["_customData"]["_editors"] = Editors.ToJsonNode();

            // Remove old "_editor" string with the new "_editors" object
            if (Json["_customData"].HasKey("_editor")) Json["_customData"].Remove("_editor");

            if (Contributors.Any())
            {
                var contributorArrayFuckyougit = new JSONArray();
                Contributors.ForEach(x => contributorArrayFuckyougit.Add(x.ToJsonNode()));
                if (Contributors.Any()) Json["_customData"]["_contributors"] = contributorArrayFuckyougit;
            }

            //BeatSaver schema changes, CleanObject function
            Json["_customData"] = CleanObject(Json["_customData"]);
            if (Json["_customData"] is null || Json["_customData"].Count <= 0) Json.Remove("_customData");

            var sets = new JSONArray();
            foreach (var set in DifficultyBeatmapSets)
            {
                if (!set.DifficultyBeatmaps.Any()) continue;
                JSONNode setNode = new JSONObject();
                setNode["_beatmapCharacteristicName"] = set.BeatmapCharacteristicName;
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

                    if (diff.EnvColorLeft != null && diff.EnvColorLeft != diff.ColorLeft)
                        subNode["_customData"]["_envColorLeft"] = diff.EnvColorLeft;
                    else subNode["_customData"].Remove("_envColorLeft");

                    if (diff.EnvColorRight != null && diff.EnvColorRight != diff.ColorRight)
                        subNode["_customData"]["_envColorRight"] = diff.EnvColorRight;
                    else subNode["_customData"].Remove("_envColorRight");

                    if (diff.BoostColorLeft != null && diff.BoostColorLeft != (diff.EnvColorLeft ?? diff.ColorLeft))
                        subNode["_customData"]["_envColorLeftBoost"] = diff.BoostColorLeft;
                    else subNode["_customData"].Remove("_envColorLeftBoost");

                    if (diff.BoostColorRight != null && diff.BoostColorRight != (diff.EnvColorRight ?? diff.ColorRight))
                        subNode["_customData"]["_envColorRightBoost"] = diff.BoostColorRight;
                    else subNode["_customData"].Remove("_envColorRightBoost");

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
                        subNode["_customData"] = CleanObject(subNode["_customData"]);
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

            Debug.Log("Saved song info.dat for " + SongName);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError(
                "This is bad. You are recommendend to restart ChroMapper; progress made after this point is not garaunteed to be saved.");
        }
    }

    /// <summary>
    ///     Loops through all children of a JSON object, and remove any that are null or empty.
    ///     This help makes _customData objects compliant with BeatSaver schema in a reusable and smart way.
    /// </summary>
    /// <param name="obj">Object of which to loop through and remove all empty children from.</param>
    public static JSONNode CleanObject(JSONNode obj)
    {
        if (obj is null) return null;
        var clone = obj.Clone();
        foreach (var key in clone.Keys)
        {
            if (obj.HasKey(key) && (obj[key].IsNull || obj[key].AsArray?.Count <= 0 ||
                                    (!obj.IsArray && !obj.IsObject && string.IsNullOrEmpty(obj[key].Value))))
            {
                obj.Remove(key);
            }
        }

        return obj;
    }

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

                    case "_customData":
                        song.CustomData = node;
                        if (node.HasKey("_contributors"))
                        {
                            foreach (JSONNode contributor in song.CustomData["_contributors"])
                                song.Contributors.Add(new MapContributor(contributor));
                        }

                        song.Editors = new EditorsObject(node["_editors"]);
                        break;

                    case "_difficultyBeatmapSets":
                        foreach (JSONNode n in node)
                        {
                            var set = new DifficultyBeatmapSet
                            {
                                BeatmapCharacteristicName = n["_beatmapCharacteristicName"]
                            };
                            foreach (JSONNode d in n["_difficultyBeatmaps"])
                            {
                                var beatmap = new DifficultyBeatmap(set)
                                {
                                    Difficulty = d["_difficulty"].Value,
                                    DifficultyRank = d["_difficultyRank"].AsInt,
                                    NoteJumpMovementSpeed = d["_noteJumpMovementSpeed"].AsFloat,
                                    NoteJumpStartBeatOffset = d["_noteJumpStartBeatOffset"].AsFloat,
                                    CustomData = d["_customData"]
                                };
                                if (d["_customData"]["_colorLeft"] != null)
                                    beatmap.ColorLeft = d["_customData"]["_colorLeft"].ReadColor();
                                if (d["_customData"]["_colorRight"] != null)
                                    beatmap.ColorRight = d["_customData"]["_colorRight"].ReadColor();

                                if (d["_customData"]["_envColorLeft"] != null)
                                    beatmap.EnvColorLeft = d["_customData"]["_envColorLeft"].ReadColor();
                                else beatmap.EnvColorLeft = beatmap.ColorLeft;
                                if (d["_customData"]["_envColorRight"] != null)
                                    beatmap.EnvColorRight = d["_customData"]["_envColorRight"].ReadColor();
                                else beatmap.EnvColorRight = beatmap.ColorRight;

                                if (d["_customData"]["_envColorLeftBoost"] != null)
                                    beatmap.BoostColorLeft = d["_customData"]["_envColorLeftBoost"].ReadColor();
                                else beatmap.BoostColorLeft = beatmap.EnvColorLeft;

                                if (d["_customData"]["_envColorRightBoost"] != null)
                                    beatmap.BoostColorRight = d["_customData"]["_envColorRightBoost"].ReadColor();
                                else beatmap.BoostColorRight = beatmap.EnvColorRight;

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

    public BeatSaberMap GetMapFromDifficultyBeatmap(DifficultyBeatmap data)
    {
        var fullPath = Path.Combine(Directory, data.BeatmapFilename);

        var mainNode = GetNodeFromFile(fullPath);
        if (mainNode == null)
        {
            Debug.LogWarning("Failed to get difficulty json file " + fullPath);
            return null;
        }

        return BeatSaberMapFactory.GetBeatSaberMapFromJson(mainNode, fullPath);
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

    [Serializable]
    public class DifficultyBeatmap
    {
        [FormerlySerializedAs("difficulty")] public string Difficulty = "Easy";
        [FormerlySerializedAs("difficultyRank")] public int DifficultyRank = 1;
        [FormerlySerializedAs("beatmapFilename")] public string BeatmapFilename = "Easy.dat";
        [FormerlySerializedAs("noteJumpMovementSpeed")] public float NoteJumpMovementSpeed = 16;
        [FormerlySerializedAs("noteJumpStartBeatOffset")] public float NoteJumpStartBeatOffset;
        [FormerlySerializedAs("colorLeft")] public Color? ColorLeft;
        [FormerlySerializedAs("colorRight")] public Color? ColorRight;
        [FormerlySerializedAs("envColorLeft")] public Color? EnvColorLeft;
        [FormerlySerializedAs("envColorRight")] public Color? EnvColorRight;
        [FormerlySerializedAs("boostColorLeft")] public Color? BoostColorLeft;
        [FormerlySerializedAs("boostColorRight")] public Color? BoostColorRight;
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

        public void RefreshRequirementsAndWarnings(BeatSaberMap map)
        {
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

    [Serializable]
    public class DifficultyBeatmapSet
    {
        [FormerlySerializedAs("beatmapCharacteristicName")] public string BeatmapCharacteristicName = "Standard";
        [FormerlySerializedAs("difficultyBeatmaps")] public List<DifficultyBeatmap> DifficultyBeatmaps = new List<DifficultyBeatmap>();

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
            if (obj is null || obj.Children.Count() <= 0)
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
