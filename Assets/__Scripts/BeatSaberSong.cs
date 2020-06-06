using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class BeatSaberSong
{

    public static readonly Color DEFAULT_LEFTCOLOR = Color.red;
    public static readonly Color DEFAULT_RIGHTCOLOR = new Color(0, 0.282353f, 1, 1);
    public static readonly Color DEFAULT_LEFTNOTE = new Color(0.7352942f, 0, 0);
    public static readonly Color DEFAULT_RIGHTNOTE = new Color(0, 0.3701827f, 0.7352942f);

    [Serializable]
    public class DifficultyBeatmap
    {
        public string difficulty = "Easy";
        public int difficultyRank = 1;
        public string beatmapFilename = "Easy.dat";
        public float noteJumpMovementSpeed = 16;
        public float noteJumpStartBeatOffset = 0;
        public Color colorLeft = DEFAULT_LEFTNOTE;
        public Color colorRight = DEFAULT_RIGHTNOTE;
        public Color envColorLeft = DEFAULT_LEFTCOLOR;
        public Color envColorRight = DEFAULT_RIGHTCOLOR;
        public Color obstacleColor = DEFAULT_LEFTCOLOR;
        public JSONNode customData;
        [NonSerialized] public DifficultyBeatmapSet parentBeatmapSet;

        public DifficultyBeatmap(DifficultyBeatmapSet beatmapSet)
        {
            parentBeatmapSet = beatmapSet;
            UpdateName();
        }

        public void UpdateParent(DifficultyBeatmapSet newParentSet)
        {
            parentBeatmapSet = newParentSet;
        }

        public void UpdateName(string fileName = null)
        {
            if (fileName is null) beatmapFilename = $"{difficulty}{parentBeatmapSet.beatmapCharacteristicName}.dat";
            else beatmapFilename = fileName;
        }

        public void RefreshRequirementsAndWarnings(BeatSaberMap map)
        {
            //Saving Map Requirement Info
            JSONArray requiredArray = new JSONArray(); //Generate suggestions and requirements array
            JSONArray suggestedArray = new JSONArray();
            if (HasChromaEvents(map)) suggestedArray.Add("Chroma");
            if (HasLegacyChromaEvents(map)) suggestedArray.Add("Chroma Lighting Events");
            if (HasNoodleExtensions(map)) requiredArray.Add("Noodle Extensions");
            if (HasMappingExtensions(map)) requiredArray.Add("Mapping Extensions");
            if (requiredArray.Count > 0 || suggestedArray.Count > 0)
            {
                if (customData == null) customData = new JSONObject();
                customData["_suggestions"] = suggestedArray;
                customData["_requirements"] = requiredArray;
            }
        }

        private bool HasNoodleExtensions(BeatSaberMap map)
        {
            if (map is null) return false;
            return map._obstacles.Any(ob => ob._customData?["_position"] != null || ob._customData?["_scale"] != null ||
                        ob._customData?["_rotation"] != null || ob._customData?["_localRotation"] != null) ||
                   map._notes.Any(ob => ob._customData?["_position"] != null || ob._customData?["_cutDirection"] != null);
        }

        private bool HasChromaEvents(BeatSaberMap map)
        {
            if (map is null) return false;
            return map._notes.Any(note => note._customData?["_color"] != null) ||
                    map._obstacles.Any(ob => ob._customData?["_color"] != null) ||
                    map._events.Any(ob => ob._customData?["_color"] != null || ob._customData?["_lightGradient"] != null || ob._customData?["_propID"] != null);
        }

        private bool HasLegacyChromaEvents(BeatSaberMap map)
        {
            if (map is null) return false;
            return map?._events?.Any(mapevent => mapevent._value > ColourManager.RGB_INT_OFFSET) ?? false;
        }

        private bool HasMappingExtensions(BeatSaberMap map)
        {
            if (map is null) return false;
            return map._notes.Any(note => note._lineIndex < 0 || note._lineIndex > 3 || note._lineLayer < 0 || note._lineLayer > 2) ||
                   map._obstacles.Any(ob => ob._lineIndex < 0 || ob._lineIndex > 3 || ob._type >= 2 || ob._width >= 1000) ||
                   map._events.Any(ob => ob.IsRotationEvent && ob._value >= 1000 && ob._value <= 1720);
        }
    }

    [Serializable]
    public class DifficultyBeatmapSet
    {
        public string beatmapCharacteristicName = "Standard";
        public List<DifficultyBeatmap> difficultyBeatmaps = new List<DifficultyBeatmap>();

        public DifficultyBeatmapSet()
        {
            beatmapCharacteristicName = "Standard";
        }
        public DifficultyBeatmapSet(string CharacteristicName)
        {
            beatmapCharacteristicName = CharacteristicName;
        }
    }

    public string songName = "New Song";

    public string directory;
    public JSONNode json;

    public string version = "2.0.0";
    public string songSubName = "";
    public string songAuthorName = "";
    public string levelAuthorName = "";
    public float beatsPerMinute = 100;
    public float songTimeOffset = 0;
    public float previewStartTime = 12;
    public float previewDuration = 10;
    public float shuffle = 0;
    public float shufflePeriod = 0.5f;
    public string songFilename = "song.ogg"; // .egg file extension is a problem solely beat saver deals with, work with .ogg for the mapper
    public string coverImageFilename = "cover.png";
    public string environmentName = "DefaultEnvironment";
    public string allDirectionsEnvironmentName = "GlassDesertEnvironment";
    public JSONNode customData;

    //Credits: BeatMapper for the idea, Beat Sage for the Name/Version format
    public string editor = "ChroMapper";

    private bool isWIPMap = false;

    public List<DifficultyBeatmapSet> difficultyBeatmapSets = new List<DifficultyBeatmapSet>();

    public List<string> warnings = new List<string>();
    public List<string> suggestions = new List<string>();
    public List<string> requirements = new List<string>();
    public List<MapContributor> contributors = new List<MapContributor>();

    public BeatSaberSong(string directory, JSONNode json)
    {
        this.directory = directory;
        this.json = json;
    }

    public BeatSaberSong(bool wipmap, string name = "")
    {
        directory = null;
        json = null;
        if (!(string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))) songName = name;
        isWIPMap = wipmap;
    }

    public void SaveSong()
    {
        try
        {
            if (string.IsNullOrEmpty(directory))
                directory = $"{(isWIPMap ? Settings.Instance.CustomWIPSongsFolder : Settings.Instance.CustomSongsFolder)}/{songName}";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (json == null) json = new JSONObject();
            if (customData == null) customData = new JSONObject();

            //Just in case, i'm moving them up here
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            json["_version"] = version;
            json["_songName"] = songName;
            json["_songSubName"] = songSubName;
            json["_songAuthorName"] = songAuthorName;
            json["_levelAuthorName"] = levelAuthorName;

            json["_beatsPerMinute"] = beatsPerMinute;
            json["_previewStartTime"] = previewStartTime;
            json["_previewDuration"] = previewDuration;
            json["_songTimeOffset"] = songTimeOffset;

            json["_shuffle"] = shuffle;
            json["_shufflePeriod"] = shufflePeriod;

            json["_coverImageFilename"] = coverImageFilename;
            json["_songFilename"] = songFilename;

            json["_environmentName"] = environmentName;
            json["_allDirectionsEnvironmentName"] = allDirectionsEnvironmentName;
            json["_customData"] = customData;
            json["_customData"]["_editor"] = editor;

            if (contributors.Any())
            {
                JSONArray contributorArrayFUCKYOUGIT = new JSONArray();
                contributors.ForEach(x => contributorArrayFUCKYOUGIT.Add(x.ToJSONNode()));
                if (contributors.Any())
                {
                    json["_customData"]["_contributors"] = contributorArrayFUCKYOUGIT;
                }
            }

            //BeatSaver schema changes, CleanObject function
            json["_customData"] = CleanObject(json["_customData"]);
            if (json["_customData"] is null || json["_customData"].Count <= 0) json.Remove("_customData");

            JSONArray sets = new JSONArray();
            foreach (DifficultyBeatmapSet set in difficultyBeatmapSets)
            {
                if (!set.difficultyBeatmaps.Any()) continue;
                JSONNode setNode = new JSONObject();
                setNode["_beatmapCharacteristicName"] = set.beatmapCharacteristicName;
                JSONArray diffs = new JSONArray();
                IEnumerable<DifficultyBeatmap> sortedBeatmaps = set.difficultyBeatmaps.OrderBy(x => x.difficultyRank);
                foreach (DifficultyBeatmap diff in sortedBeatmaps)
                {
                    diff.RefreshRequirementsAndWarnings(GetMapFromDifficultyBeatmap(diff));

                    JSONNode subNode = new JSONObject();

                    subNode["_difficulty"] = diff.difficulty;
                    subNode["_difficultyRank"] = diff.difficultyRank;
                    subNode["_beatmapFilename"] = diff.beatmapFilename;
                    subNode["_noteJumpMovementSpeed"] = diff.noteJumpMovementSpeed;
                    subNode["_noteJumpStartBeatOffset"] = diff.noteJumpStartBeatOffset;
                    subNode["_customData"] = diff.customData;

                    if (diff.colorLeft != DEFAULT_LEFTNOTE)
                        subNode["_customData"]["_colorLeft"] = diff.colorLeft;
                    if (diff.colorRight != DEFAULT_RIGHTNOTE)
                        subNode["_customData"]["_colorRight"] = diff.colorRight;
                    if (diff.envColorLeft != DEFAULT_LEFTCOLOR && diff.envColorLeft != diff.colorLeft)
                        subNode["_customData"]["_envColorLeft"] = diff.envColorLeft;
                    if (diff.envColorRight != DEFAULT_RIGHTCOLOR && diff.envColorRight != diff.colorRight)
                        subNode["_customData"]["_envColorRight"] = diff.envColorRight;
                    if (diff.obstacleColor != DEFAULT_LEFTCOLOR)
                        subNode["_customData"]["_obstacleColor"] = diff.obstacleColor;

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
                    else subNode.Remove("_customData"); //Just remove it if it's null lmao

                    diffs.Add(subNode);
                }
                setNode["_difficultyBeatmaps"] = diffs;
                sets.Add(setNode);
            }

            json["_difficultyBeatmapSets"] = sets;

            FileInfo info = new FileInfo(directory + "/Info.dat");
            //No, patrick, not existing does not mean it is read only.
            if (!info.IsReadOnly || !info.Exists)
            {
                using (StreamWriter writer = new StreamWriter(directory + "/Info.dat", false))
                    writer.Write(json.ToString(2));
            }
            else
            {
                PersistentUI.Instance.ShowDialogBox("ChroMapper has detected that your info file has been marked as read-only.\n\n" +
                    "Please remove the read-only marker and try again, or keep using the latest editor from the EditSaber family.\n\n" +
                    "Marking your info file read-only to prevent MM or CM from overwriting it is extremely dumb, so undo it or take your map somewhere else.",
                    null, PersistentUI.DialogBoxPresetType.Ok);
                Debug.LogError($":hyperPepega: :mega: DONT MAKE YOUR MAP FILES READONLY");
            }

            Debug.Log("Saved song info.dat for " + songName);

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Loops through all children of a JSON object, and remove any that are null or empty. 
    /// This help makes _customData objects compliant with BeatSaver schema in a reusable and smart way.
    /// </summary>
    /// <param name="obj">Object of which to loop through and remove all empty children from.</param>
    public static JSONNode CleanObject(JSONNode obj)
    {
        if (obj is null) return null;
        JSONNode clone = obj.Clone();
        foreach (string key in clone.Keys)
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
            JSONNode mainNode = GetNodeFromFile(directory + "/Info.dat");
            if (mainNode == null) 
            {
                //Virgin "info.dat" VS chad "Info.dat"
                mainNode = GetNodeFromFile(directory + "/info.dat");
                if (mainNode == null) return null;
            }

            BeatSaberSong song = new BeatSaberSong(directory, mainNode);
            song.editor = $"{Application.productName}/{Application.version}";
            JSONNode.Enumerator nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                string key = nodeEnum.Current.Key;
                JSONNode node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "_songName": song.songName = node.Value; break;
                    case "_songSubName": song.songSubName = node.Value; break;
                    case "_songAuthorName": song.songAuthorName = node.Value; break;
                    case "_levelAuthorName": song.levelAuthorName = node.Value; break;

                    case "_beatsPerMinute": song.beatsPerMinute = node.AsFloat; break;
                    case "_songTimeOffset": song.songTimeOffset = node.AsFloat; break;
                    case "_previewStartTime": song.previewStartTime = node.AsFloat; break;
                    case "_previewDuration": song.previewDuration = node.AsFloat; break;

                    case "_shuffle": song.shuffle = node.AsFloat; break;
                    case "_shufflePeriod": song.shufflePeriod = node.AsFloat; break;

                    case "_coverImageFilename": song.coverImageFilename = node.Value; break;
                    case "_songFilename": song.songFilename = node.Value; break;
                    case "_environmentName": song.environmentName = node.Value; break;
                    //Because there is only one option, I wont load from file.
                    //case "_allDirectionsEnvironmentName": song.allDirectionsEnvironmentName = node.Value; break;

                    case "_customData":
                        song.customData = node;
                        if (!song.customData["_contributors"].IsNull)
                        {
                            foreach (JSONNode contributor in song.customData["_contributors"])
                                song.contributors.Add(new MapContributor(contributor));
                        }
                        break;

                    case "_difficultyBeatmapSets":
                        foreach (JSONNode n in node)
                        {
                            DifficultyBeatmapSet set = new DifficultyBeatmapSet();
                            set.beatmapCharacteristicName = n["_beatmapCharacteristicName"];
                            foreach (JSONNode d in n["_difficultyBeatmaps"])
                            {
                                DifficultyBeatmap beatmap = new DifficultyBeatmap(set)
                                {
                                    difficulty = d["_difficulty"].Value,
                                    difficultyRank = d["_difficultyRank"].AsInt,
                                    noteJumpMovementSpeed = d["_noteJumpMovementSpeed"].AsFloat,
                                    noteJumpStartBeatOffset = d["_noteJumpStartBeatOffset"].AsFloat,
                                    customData = d["_customData"],
                                };
                                if (d["_customData"]["_colorLeft"] != null)
                                    beatmap.colorLeft = d["_customData"]["_colorLeft"].AsObject.ReadColor();
                                if (d["_customData"]["_colorRight"] != null)
                                    beatmap.colorRight = d["_customData"]["_colorRight"].AsObject.ReadColor();
                                if (d["_customData"]["_envColorLeft"] != null)
                                    beatmap.envColorLeft = d["_customData"]["_envColorLeft"].AsObject.ReadColor();
                                else if (d["_customData"]["_colorLeft"] != null) beatmap.envColorLeft = beatmap.colorLeft;
                                if (d["_customData"]["_envColorRight"] != null)
                                    beatmap.envColorRight = d["_customData"]["_envColorRight"].AsObject.ReadColor();
                                else if (d["_customData"]["_colorRight"] != null) beatmap.envColorRight = beatmap.colorRight;
                                if (d["_customData"]["_obstacleColor"] != null)
                                    beatmap.obstacleColor = d["_customData"]["_obstacleColor"].AsObject.ReadColor();
                                beatmap.UpdateName(d["_beatmapFilename"]);
                                set.difficultyBeatmaps.Add(beatmap);
                            }
                            set.difficultyBeatmaps = set.difficultyBeatmaps.OrderBy(x => x.difficultyRank).ToList();
                            song.difficultyBeatmapSets.Add(set);
                        }
                        song.difficultyBeatmapSets = song.difficultyBeatmapSets.OrderBy(x =>
                        SongInfoEditUI.CharacteristicDropdownToBeatmapName.IndexOf(x.beatmapCharacteristicName)).ToList();
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
        string fullPath = Path.Combine(directory, data.beatmapFilename);

        JSONNode mainNode = GetNodeFromFile(fullPath);
        if (mainNode == null)
        {
            Debug.LogWarning("Failed to get difficulty json file " + fullPath);
            return null;
        }

        return BeatSaberMap.GetBeatSaberMapFromJSON(mainNode, fullPath);
    }

    private static JSONNode GetNodeFromFile(string file)
    {
        if (!File.Exists(file)) return null;
        try
        {
            using (StreamReader reader = new StreamReader(file))
            {
                JSONNode node = JSON.Parse(reader.ReadToEnd());
                return node;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return null;
    }

}
