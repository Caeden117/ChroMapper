using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[Serializable]
public class BeatSaberSong {

    private readonly Color DEFAULT_LEFTCOLOR = Color.red;
    private readonly Color DEFAULT_RIGHTCOLOR = new Color(0, 0.282353f, 1, 1);

    [Serializable]
    public class DifficultyBeatmap
    {
        public string difficulty = "Easy";
        public int difficultyRank = 1;
        public string beatmapFilename = "Easy.dat";
        public float offset = 0;
        public float oldOffset = 0;
        public float noteJumpMovementSpeed = 16;
        public float noteJumpStartBeatOffset = 0;
        public Color colorLeft = Color.red;
        public Color colorRight = new Color(0, 0.282353f, 1, 1);
        public JSONNode customData;
        [NonSerialized] private DifficultyBeatmapSet parentBeatmapSet;

        public DifficultyBeatmap (DifficultyBeatmapSet beatmapSet)
        {
            parentBeatmapSet = beatmapSet;
        }

        public void UpdateName(string fileName = null)
        {
            if (fileName is null) beatmapFilename = $"{difficulty}{parentBeatmapSet.beatmapCharacteristicName}.dat";
            else beatmapFilename = fileName;
        }
    }

    [Serializable]
    public class DifficultyBeatmapSet
    {
        public string beatmapCharacteristicName = "Standard";
        public List<DifficultyBeatmap> difficultyBeatmaps = new List<DifficultyBeatmap>();

        public DifficultyBeatmapSet() {
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
    public string songFilename = "song.egg";
    public string coverImageFilename = "cover.png";
    public string environmentName = "DefaultEnvironment";
    public JSONNode customData;

    private bool isWIPMap = false;

    public List<DifficultyBeatmapSet> difficultyBeatmapSets = new List<DifficultyBeatmapSet>();

    public List<string> warnings = new List<string>();
    public List<string> suggestions = new List<string>();
    public List<string> requirements = new List<string>();

    public BeatSaberSong(string directory, JSONNode json) {
        this.directory = directory;
        this.json = json;
    }

    public BeatSaberSong(bool wipmap)
    {
        directory = null;
        json = null;
        isWIPMap = wipmap;
    }

    public void SaveSong() {
        try {
            if (directory == null || directory == "") directory = (isWIPMap ? Settings.CustomWIPSongsFolder : Settings.CustomSongsFolder) + "/" + songName;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (json == null) json = new JSONObject();
            if (customData == null) customData = new JSONObject();

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
            json["_customData"] = customData;

            JSONArray sets = new JSONArray();
            foreach (DifficultyBeatmapSet set in difficultyBeatmapSets)
            {
                JSONNode setNode = new JSONObject();
                setNode["_beatmapCharacteristicName"] = set.beatmapCharacteristicName;
                JSONArray diffs = new JSONArray();
                foreach(DifficultyBeatmap diff in set.difficultyBeatmaps)
                {
                    diff.customData["_editorOffset"] = diff.offset;
                    JSONNode subNode = new JSONObject();
                    subNode["_difficulty"] = diff.difficulty;
                    subNode["_difficultyRank"] = diff.difficultyRank;
                    subNode["_beatmapFilename"] = diff.beatmapFilename;
                    subNode["_noteJumpMovementSpeed"] = diff.noteJumpMovementSpeed;
                    subNode["_noteJumpStartBeatOffset"] = diff.noteJumpStartBeatOffset;
                    subNode["_customData"] = diff.customData;
                    if (diff.colorLeft != DEFAULT_LEFTCOLOR)
                        subNode["_customData"]["_colorLeft"] = GetJSONNodeFromColor(diff.colorLeft);
                    if (diff.colorRight != DEFAULT_RIGHTCOLOR)
                        subNode["_customData"]["_colorRight"] = GetJSONNodeFromColor(diff.colorRight);
                    diffs.Add(subNode);
                }
                setNode["_difficultyBeatmaps"] = diffs;
                sets.Add(setNode);
            }

            json["_difficultyBeatmapSets"] = sets;

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            using (StreamWriter writer = new StreamWriter(directory + "/info.dat", false)) {
                writer.Write(json.ToString(2));
            }

            Debug.Log("Saved song info.dat for " + songName);

        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    public static BeatSaberSong GetSongFromFolder(string directory) {

        try {

            JSONNode mainNode = GetNodeFromFile(directory + "/info.dat");
            if (mainNode == null) return null;

            BeatSaberSong song = new BeatSaberSong(directory, mainNode);

            List<DifficultyBeatmapSet> difficultyDataList = new List<DifficultyBeatmapSet>();

            JSONNode.Enumerator nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext()) {
                string key = nodeEnum.Current.Key;
                JSONNode node = nodeEnum.Current.Value;

                switch (key) {
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

                    case "_customData": song.customData = node; break;

                    case "_difficultyBeatmapSets":
                        foreach (JSONNode n in node) {
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
                                    offset = d["_customData"]["_editorOffset"].AsFloat,
                                };
                                if (d["_customData"]["_colorLeft"] != null)
                                    beatmap.colorLeft = GetColorFromJSONNode(d["_customData"]["_colorLeft"]);
                                if (d["_customData"]["_colorRight"] != null)
                                    beatmap.colorRight = GetColorFromJSONNode(d["_customData"]["_colorRight"]);
                                beatmap.UpdateName(d["_beatmapFilename"]);
                                set.difficultyBeatmaps.Add(beatmap);
                            }
                            //Debug.Log("Found difficulty data for " + difficultyData.jsonPath);
                            difficultyDataList.Add(set);
                        }

                        break;
                }
            }

            song.difficultyBeatmapSets = difficultyDataList;

            return song;

        } catch (Exception e) {
            Debug.LogError(e);
            return null;
        }

    }

    public BeatSaberMap GetMapFromDifficultyBeatmap(DifficultyBeatmap data) {

        JSONNode mainNode = GetNodeFromFile(directory + "/" + data.beatmapFilename);
        if (mainNode == null) {
            Debug.LogWarning("Failed to get difficulty json file "+(directory + "/" + data.beatmapFilename));
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

    private static JSONNode GetNodeFromFile(string file) {
        if (!File.Exists(file)) return null;
        try {
            using (StreamReader reader = new StreamReader(file)) {
                JSONNode node = JSON.Parse(reader.ReadToEnd());
                return node;
            }
        } catch (Exception e) {
            Debug.LogError(e);
        }
        return null;
    }

}
