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
            if (HasChromaEvents(map)) suggestedArray.Add(new JSONString("Chroma Lighting Events"));
            if (HasMappingExtensions(map)) requiredArray.Add(new JSONString("Mapping Extensions"));
            if (HasChromaToggle(map)) requiredArray.Add(new JSONString("ChromaToggle"));
            customData["_warnings"] = suggestedArray;
            customData["_requirements"] = requiredArray;
        }

        private bool HasChromaEvents(BeatSaberMap map)
        {
            return map._events.Any(mapevent => mapevent._value > ColourManager.RGB_INT_OFFSET);
        }

        private bool HasMappingExtensions(BeatSaberMap map)
        {
            return map._notes.Any(note => note._lineIndex < 0 || note._lineIndex > 3) ||
                   map._obstacles.Any(ob => ob._lineIndex < 0 || ob._lineIndex > 3 || ob._type >= 2 || ob._width >= 1000) ||
                   map._events.Any(ob => ob.IsRotationEvent && ob._value >= 1000 && ob._value <= 1720);
        }

        private bool HasChromaToggle(BeatSaberMap map)
        {
            //TODO when CustomJSONData CT notes exist
            return false;
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
    public string editor = "chromapper"; //BeatMapper started doing this so might as well do it for CM too
    public JSONNode customData;

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

            JSONArray contributorArrayFUCKYOUGIT = new JSONArray();
            contributors.DistinctBy(x => x.ToJSONNode().ToString()).ToList().ForEach(x => contributorArrayFUCKYOUGIT.Add(x.ToJSONNode()));
            json["_customData"]["_contributors"] = contributorArrayFUCKYOUGIT;

            //BeatSaver schema changes, see below comment.
            if (string.IsNullOrEmpty(customData["_editor"])) json["_customData"]["_editor"] = "chromapper";
            if (string.IsNullOrEmpty(customData["_contributors"])) json["_customData"].Remove("_contributors");
            if (string.IsNullOrEmpty(customData["_customEnvironment"])) json["_customData"].Remove("_customEnvironment");
            if (string.IsNullOrEmpty(customData["_customEnvironmentHash"])) json["_customData"].Remove("_customEnvironmentHash");
            if (json["_customData"].Linq.Count() <= 0) json.Remove("_customData");

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
                        subNode["_customData"]["_colorLeft"] = GetJSONNodeFromColor(diff.colorLeft);
                    if (diff.colorRight != DEFAULT_RIGHTNOTE)
                        subNode["_customData"]["_colorRight"] = GetJSONNodeFromColor(diff.colorRight);
                    if (diff.envColorLeft != DEFAULT_LEFTCOLOR && diff.envColorLeft != diff.colorLeft)
                        subNode["_customData"]["_envColorLeft"] = GetJSONNodeFromColor(diff.envColorLeft);
                    if (diff.envColorRight != DEFAULT_RIGHTCOLOR && diff.envColorRight != diff.colorRight)
                        subNode["_customData"]["_envColorRight"] = GetJSONNodeFromColor(diff.envColorRight);
                    if (diff.obstacleColor != DEFAULT_LEFTCOLOR)
                        subNode["_customData"]["_obstacleColor"] = GetJSONNodeFromColor(diff.obstacleColor);

                    /*
                     * More BeatSaver Schema changes, yayyyyy! (fuck)
                     * If any additional non-required fields are present, they cannot be empty.
                     * 
                     * So ChroMapper is just gonna yeet anything that is null or empty, then keep going down the list.
                     * If customData is empty, then we just yeet that.
                     */
                    if (subNode["_customData"] != null)
                    {
                        if (string.IsNullOrEmpty(diff.customData["_difficultyLabel"])) subNode["_customData"].Remove("_difficultyLabel");
                        if (diff.customData["_editorOldOffset"] != null && diff.customData["_editorOldOffset"].AsFloat <= 0)
                            subNode["_customData"].Remove("_editorOldOffset"); //For some reason these are used by MM but not by CM
                        if (diff.customData["_editorOffset"] != null && diff.customData["_editorOffset"].AsFloat <= 0)
                            subNode["_customData"].Remove("_editorOffset"); //So we're just gonna yeet them. Sorry squanksers.
                        if (diff.customData["_warnings"] != null && diff.customData["_warnings"].AsArray.Count <= 0)
                            subNode["_customData"].Remove("_warnings");
                        if (diff.customData["_information"] != null && diff.customData["_information"].AsArray.Count <= 0)
                            subNode["_customData"].Remove("_information");
                        if (diff.customData["_suggestions"] != null && diff.customData["_suggestions"].AsArray.Count <= 0)
                            subNode["_customData"].Remove("_suggestions");
                        if (diff.customData["_requirements"] != null && diff.customData["_requirements"].AsArray.Count <= 0)
                            subNode["_customData"].Remove("_requirements");
                        if (subNode["_customData"].Linq.Count() <= 0) subNode.Remove("_customData");
                    }
                    else subNode.Remove("_customData"); //Just remove it if it's null lmao

                    diffs.Add(subNode);
                }
                setNode["_difficultyBeatmaps"] = diffs;
                sets.Add(setNode);
            }

            json["_difficultyBeatmapSets"] = sets;

            using (StreamWriter writer = new StreamWriter(directory + "/info.dat", false))
                writer.Write(json.ToString(2));

            Debug.Log("Saved song info.dat for " + songName);

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public static BeatSaberSong GetSongFromFolder(string directory)
    {

        try
        {

            JSONNode mainNode = GetNodeFromFile(directory + "/info.dat");
            if (mainNode == null) return null;

            BeatSaberSong song = new BeatSaberSong(directory, mainNode);

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
                        foreach (JSONNode n in node)
                        {
                            if (n["_contributors"]?.AsArray != null)
                            {
                                foreach (JSONNode contributor in n["_contributors"].AsArray)
                                    song.contributors.Add(new MapContributor(contributor));
                            }
                            if (n["_editor"]?.Value != null) song.editor = n["_editor"].Value;
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
                                    beatmap.colorLeft = GetColorFromJSONNode(d["_customData"]["_colorLeft"]);
                                if (d["_customData"]["_colorRight"] != null)
                                    beatmap.colorRight = GetColorFromJSONNode(d["_customData"]["_colorRight"]);
                                if (d["_customData"]["_envColorLeft"] != null)
                                    beatmap.envColorLeft = GetColorFromJSONNode(d["_customData"]["_envColorLeft"]);
                                else if (d["_customData"]["_colorLeft"] != null) beatmap.envColorLeft = beatmap.colorLeft;
                                if (d["_customData"]["_envColorRight"] != null)
                                    beatmap.envColorRight = GetColorFromJSONNode(d["_customData"]["_envColorRight"]);
                                else if (d["_customData"]["_colorRight"] != null) beatmap.envColorRight = beatmap.colorRight;
                                if (d["_customData"]["_obstacleColor"] != null)
                                    beatmap.obstacleColor = GetColorFromJSONNode(d["_customData"]["_obstacleColor"]);
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

        JSONNode mainNode = GetNodeFromFile(directory + "/" + data.beatmapFilename);
        if (mainNode == null)
        {
            Debug.LogWarning("Failed to get difficulty json file " + (directory + "/" + data.beatmapFilename));
            return null;
        }

        return BeatSaberMap.GetBeatSaberMapFromJSON(mainNode, directory + "/" + data.beatmapFilename);
    }

    private static Color GetColorFromJSONNode(JSONNode node)
    {
        return new Color(node["r"].AsFloat, node["g"].AsFloat, node["b"].AsFloat);
    }

    private JSONNode GetJSONNodeFromColor(Color color)
    {
        JSONObject obj = new JSONObject();
        obj["r"] = color.r;
        obj["g"] = color.g;
        obj["b"] = color.b;
        return obj;
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
