using System.Collections.Generic;
using System.IO;
using Beatmap.Base.Customs;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseDifficulty : BaseItem, ICustomDataDifficulty
    {
        // TODO: concrete class for these bad boys
        public Dictionary<string, JSONObject> Materials = new Dictionary<string, JSONObject>();

        public Dictionary<string, List<JSONNode>> PointDefinitions = new Dictionary<string, List<JSONNode>>();
        public JSONNode MainNode { get; set; }
        public string DirectoryAndFile { get; set; }
        public abstract string Version { get; }
        public List<BaseBpmEvent> BpmEvents { get; set; } = new List<BaseBpmEvent>();
        public List<BaseRotationEvent> RotationEvents { get; set; } = new List<BaseRotationEvent>();
        public List<BaseNote> Notes { get; set; } = new List<BaseNote>();
        public List<BaseBombNote> Bombs { get; set; } = new List<BaseBombNote>();
        public List<BaseObstacle> Obstacles { get; set; } = new List<BaseObstacle>();
        public List<BaseArc> Arcs { get; set; } = new List<BaseArc>();
        public List<BaseChain> Chains { get; set; } = new List<BaseChain>();
        public List<BaseWaypoint> Waypoints { get; set; } = new List<BaseWaypoint>();
        public List<BaseEvent> Events { get; set; } = new List<BaseEvent>();
        public List<BaseColorBoostEvent> ColorBoostEvents { get; set; } = new List<BaseColorBoostEvent>();

        public List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> LightColorEventBoxGroups { get; set; } =
            new List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>>();

        public List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
            LightRotationEventBoxGroups
        { get; set; } =
            new List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>();

        public List<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>>
            LightTranslationEventBoxGroups
        { get; set; } =
            new List<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>>();

        public BaseEventTypesWithKeywords EventTypesWithKeywords { get; set; }
        public bool UseNormalEventsAsCompatibleEvents { get; set; } = true;
        public float Time { get; set; } = 0f;
        public List<BaseBpmChange> BpmChanges { get; set; } = new List<BaseBpmChange>();
        public List<BaseBookmark> Bookmarks { get; set; } = new List<BaseBookmark>();
        public abstract string BookmarksUseOfficialBpmEventsKey { get; }
        public List<BaseCustomEvent> CustomEvents { get; set; } = new List<BaseCustomEvent>();

        public List<BaseEnvironmentEnhancement> EnvironmentEnhancements { get; set; } =
            new List<BaseEnvironmentEnhancement>();

        public JSONNode CustomData { get; set; } = new JSONObject();

        private List<List<BaseObject>> AllBaseObjectProperties() => new List<List<BaseObject>>
        {
            new List<BaseObject>(RotationEvents),
            new List<BaseObject>(Notes),
            new List<BaseObject>(Bombs),
            new List<BaseObject>(Obstacles),
            new List<BaseObject>(Arcs),
            new List<BaseObject>(Chains),
            new List<BaseObject>(Waypoints),
            new List<BaseObject>(Events),
            new List<BaseObject>(ColorBoostEvents),
            new List<BaseObject>(Bookmarks),
        };

        public void ConvertCustomBpmToOfficial()
        {
            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var customData = BeatSaberSongContainer.Instance.DifficultyData.CustomData;
            if ((customData?.HasKey("_editorOffset") ?? false) && customData["_editorOffset"] > 0f)
            {
                float offset = customData["_editorOffset"];
                customData.Remove("_editorOffset");
                customData.Remove("_editorOldOffset");
                BpmChanges.Insert(0, BeatmapFactory.BpmChange(songBpm / 60 * (offset / 1000f), songBpm));
                Debug.Log($"Editor offset detected: {songBpm / 60 * (offset / 1000f)}s");
            }

            if (BpmChanges.Count != 0)
            {
                PersistentUI.Instance.ShowDialogBox("Mapper", "custom.bpm.convert",
                    null, PersistentUI.DialogBoxPresetType.Ok);
                BpmEvents.Clear();
            }

            BaseBpmEvent nextBpmChange;
            for (var i = 0; i < BpmChanges.Count; i++)
            {
                var bpmChange = BpmChanges[i];

                // Account for custom bpm change original grid behaviour
                var distanceToNearestInt = Mathf.Abs(bpmChange.JsonTime - Mathf.Round(bpmChange.JsonTime));
                if (distanceToNearestInt > 0.01f)
                {
                    var oldTime = bpmChange.JsonTime;
                    var jsonTimeOffset = 1 - bpmChange.JsonTime % 1;

                    foreach (var objList in AllBaseObjectProperties())
                    {
                        if (objList == null) continue;

                        foreach (BaseObject obj in objList)
                        {
                            if (obj.JsonTime >= oldTime)
                            {
                                obj.JsonTime += jsonTimeOffset;
                            }
                            // assuming there's mapper who messed up with slider tail time
                            // or beat games non truncate float:tm: is lower than head time
                            if (obj is BaseSlider slider && slider.TailJsonTime >= oldTime)
                            {
                                slider.TailJsonTime += jsonTimeOffset;
                            }
                        }
                    }

                    for (var j = i; j < BpmChanges.Count; j++)
                    {
                        BpmChanges[j].JsonTime += jsonTimeOffset;
                    }

                    BpmEvents.Add(BeatmapFactory.BpmEvent(oldTime, 100000));
                }

                BpmEvents.Add(BeatmapFactory.BpmEvent(bpmChange.JsonTime, bpmChange.Bpm));

                // Adjust all the objects after the bpm change accordingly
                if (i + 1 < BpmChanges.Count)
                    nextBpmChange = BpmChanges[i + 1];
                else
                    nextBpmChange = null;

                var bpmSectionJsonTimeDiff = (nextBpmChange != null) ? nextBpmChange.JsonTime - bpmChange.JsonTime : 0;
                var scale = (bpmChange.Bpm / songBpm) - 1;

                foreach (var objList in AllBaseObjectProperties())
                {
                    if (objList == null) continue;

                    foreach (BaseObject obj in objList)
                    {
                        if (bpmChange.JsonTime < obj.JsonTime)
                        {
                            var jsonTimeBeforeScale = obj.JsonTime;
                            if (nextBpmChange == null || obj.JsonTime < nextBpmChange.JsonTime)
                                obj.JsonTime += (obj.JsonTime - bpmChange.JsonTime) * scale;
                            else
                                obj.JsonTime += bpmSectionJsonTimeDiff * scale;

                            if (obj is BaseObstacle obstacle)
                            {
                                var endJsonTime = jsonTimeBeforeScale + obstacle.Duration;
                                if (nextBpmChange == null || endJsonTime < nextBpmChange.JsonTime)
                                    endJsonTime += (endJsonTime - bpmChange.JsonTime) * scale;
                                else
                                    endJsonTime += bpmSectionJsonTimeDiff * scale;
                                obstacle.Duration = endJsonTime - obj.JsonTime;
                            }
                        }
                        if (obj is BaseSlider slider && bpmChange.JsonTime < slider.TailJsonTime)
                        {
                            if (nextBpmChange == null || slider.TailJsonTime < nextBpmChange.JsonTime)
                                slider.TailJsonTime += (slider.TailJsonTime - bpmChange.JsonTime) * scale;
                            else
                                slider.TailJsonTime += bpmSectionJsonTimeDiff * scale;
                        }
                    }
                }

                for (var j = i + 1; j < BpmChanges.Count; j++)
                {
                    BpmChanges[j].JsonTime += bpmSectionJsonTimeDiff * scale;
                }
            }

            BpmChanges.Clear();
        }

        public abstract bool IsChroma();
        public abstract bool IsNoodleExtensions();
        public abstract bool IsMappingExtensions();

        public abstract bool Save();
        public abstract bool SaveCustom();

        // Cleans an array by filtering out null elements, or objects with invalid time.
        // Could definitely be optimized a little bit, but since saving is done on a separate thread, I'm not too worried about it.
        protected static JSONArray CleanupArray(JSONArray original, string timeKey = "_time")
        {
            var array = original.Clone().AsArray;
            foreach (JSONNode node in original)
                if (node is null || node[timeKey].IsNull || float.IsNaN(node[timeKey]))
                    array.Remove(node);

            return array;
        }

        // fuick
        // public static abstract IDifficulty GetFromJson(JSONNode node, string path);

        protected void WriteDifficultyFile(BaseDifficulty map) =>
            // I *believe* this automatically creates the file if it doesn't exist. Needs more experimentation
            File.WriteAllText(map.DirectoryAndFile,
                Settings.Instance.FormatJson ? map.MainNode.ToString(2) : map.MainNode.ToString());

        // Write BPMInfo file for official editor compatibility
        protected void WriteBPMInfoFile(BaseDifficulty map)
        {
            JSONNode bpmInfo = new JSONObject();

            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var audioLength = BeatSaberSongContainer.Instance.LoadedSongLength;
            var audioSamples = BeatSaberSongContainer.Instance.LoadedSongSamples;
            var audioFrequency = BeatSaberSongContainer.Instance.LoadedSongFrequency;

            bpmInfo["_version"] = "2.0.0";
            bpmInfo["_songSampleCount"] = audioSamples;
            bpmInfo["_songFrequency"] = audioFrequency;

            var regions = new JSONArray();
            if (map.BpmEvents.Count == 0)
            {
                regions.Add(new JSONObject
                {
                    ["_startSampleIndex"] = 0,
                    ["_endSampleIndex"] = audioSamples,
                    ["_startBeat"] = 0f,
                    ["_endBeat"] = (songBpm / 60f) * audioLength,
                });
            }
            else
            {
                for (var i = 0; i < map.BpmEvents.Count - 1; i++)
                {
                    var currentBpmEvent = map.BpmEvents[i];
                    var nextBpmEvent = map.BpmEvents[i + 1];

                    regions.Add(new JSONObject
                    {
                        ["_startSampleIndex"] = (int)(currentBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency),
                        ["_endSampleIndex"] = (int)(nextBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency),
                        ["_startBeat"] = currentBpmEvent.JsonTime,
                        ["_endBeat"] = nextBpmEvent.JsonTime,
                    });
                }

                var lastBpmEvent = map.BpmEvents[map.BpmEvents.Count - 1];
                var lastStartSampleIndex = (lastBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency);
                var secondsDiff = (audioSamples - lastStartSampleIndex) / audioFrequency;
                var jsonBeatsDiff = secondsDiff * (lastBpmEvent.Bpm / 60f);

                regions.Add(new JSONObject
                {
                    ["_startSampleIndex"] = (int)lastStartSampleIndex,
                    ["_endSampleIndex"] = audioSamples,
                    ["_startBeat"] = lastBpmEvent.JsonTime,
                    ["_endBeat"] = lastBpmEvent.JsonTime + jsonBeatsDiff,
                });
            }

            bpmInfo["_regions"] = regions;

            File.WriteAllText(Path.Combine(BeatSaberSongContainer.Instance.Song.Directory, "BPMInfo.dat"),
                bpmInfo.ToString(2));
        }
    }
}
