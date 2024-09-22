using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Converters;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    /// <summary>
    /// ChroMapper's internal format.
    /// - Notes contains both color notes and bombs
    /// - Events contains basic events, rotation events, and boost events
    /// </summary>
    public class BaseDifficulty : BaseItem//, ICustomDataDifficulty
    {
        public string DirectoryAndFile { get; set; }

        public string Version => Settings.Instance.MapVersion switch
        {
            2 => "2.6.0",
            3 => "3.3.0",
            _ => throw new NotImplementedException("Unknown version")
        };
        public List<BaseBpmEvent> BpmEvents { get; set; } = new();
        public List<BaseNote> Notes { get; set; } = new();
        public List<BaseObstacle> Obstacles { get; set; } = new();
        public List<BaseArc> Arcs { get; set; } = new();
        public List<BaseChain> Chains { get; set; } = new();
        public List<BaseWaypoint> Waypoints { get; set; } = new();
        public List<BaseEvent> Events { get; set; } = new();

        public List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> LightColorEventBoxGroups { get; set; } = new();

        public List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
            LightRotationEventBoxGroups { get; set; } = new();

        public List<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>>
            LightTranslationEventBoxGroups { get; set; } = new();

        public List<BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>> VfxEventBoxGroups { get; set; } = new();
        public BaseFxEventsCollection FxEventsCollection { get; set; }

        public BaseEventTypesWithKeywords EventTypesWithKeywords { get; set; }
        public bool UseNormalEventsAsCompatibleEvents { get; set; } = true;

        public float Time { get; set; }
        public List<BaseBpmChange> BpmChanges { get; set; } = new();
        public List<BaseBookmark> Bookmarks { get; set; } = new();
        public string BookmarksUseOfficialBpmEventsKey => Settings.Instance.MapVersion switch
        {
            2 => "_bookmarksUseOfficialBpmEvents",
            3 => "bookmarksUseOfficialBpmEvents",
            _ => null
        };
        public List<BaseCustomEvent> CustomEvents { get; set; } = new();

        public Dictionary<string, BaseMaterial> Materials = new();

        public Dictionary<string, JSONArray> PointDefinitions = new();

        public List<BaseEnvironmentEnhancement> EnvironmentEnhancements { get; set; } = new();

        public JSONNode CustomData { get; set; } = new JSONObject();

        private List<List<BaseObject>> AllBaseObjectProperties() => new()
        {
            new List<BaseObject>(Notes),
            new List<BaseObject>(Obstacles),
            new List<BaseObject>(Arcs),
            new List<BaseObject>(Chains),
            new List<BaseObject>(Waypoints),
            new List<BaseObject>(Events),
            new List<BaseObject>(Bookmarks),
            new List<BaseObject>(CustomEvents),
        };

        public void ConvertCustomBpmToOfficial()
        {
            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var customData = BeatSaberSongContainer.Instance.DifficultyData.CustomData;
            
            // Replace editor offset with equivalent bpm change if it exists
            if ((customData?.HasKey("_editorOffset") ?? false) && customData["_editorOffset"] > 0f)
            {
                float offset = customData["_editorOffset"];
                customData.Remove("_editorOffset");
                customData.Remove("_editorOldOffset");
                BpmChanges.Insert(0, new BaseBpmChange{ JsonTime = songBpm / 60 * (offset / 1000f), Bpm = songBpm });
                Debug.Log($"Editor offset detected: {songBpm / 60 * (offset / 1000f)}s");
            }

            if (BpmChanges.Count == 0)
            {
                return;
            }
            
            PersistentUI.Instance.ShowDialogBox("Mapper", "custom.bpm.convert",
                null, PersistentUI.DialogBoxPresetType.Ok);
            BpmEvents.Clear(); // If for some reason we have both official and old custom bpm changes, we'll overwrite official bpm events

            BaseBpmEvent nextBpmChange;
            for (var i = 0; i < BpmChanges.Count; i++)
            {
                var bpmChange = BpmChanges[i];
                var prevBpmChange = (i > 0)
                    ? BpmChanges[i - 1]
                    : new BaseBpmChange{ JsonTime = 0, Bpm = songBpm };

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

                    // Place a very fast bpm event slighty behind the original event to account for drift
                    var aVeryLargeBpm = 100000f;
                    var offsetRequiredInBeats = jsonTimeOffset * prevBpmChange.Bpm / (aVeryLargeBpm - prevBpmChange.Bpm);
                    BpmEvents.Add(new BaseBpmEvent(oldTime - offsetRequiredInBeats, aVeryLargeBpm));
                }

                BpmEvents.Add(new BaseBpmEvent(bpmChange.JsonTime, bpmChange.Bpm));

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

        public void ConvertCustomDataVersion(int fromVersion, int toVersion)
        {
            if (fromVersion == 2 && toVersion == 3)
            {
                foreach (var note in Notes) note.SetCustomData(V2ToV3.CustomDataObject(note.SaveCustom()));
                foreach (var obstacle in Obstacles) obstacle.SetCustomData(V2ToV3.CustomDataObject(obstacle.SaveCustom()));
                foreach (var arc in Arcs) arc.SetCustomData(V2ToV3.CustomDataObject(arc.SaveCustom()));
                foreach (var chain in Chains) chain.SetCustomData(V2ToV3.CustomDataObject(chain.SaveCustom()));
                
                foreach (var evt in Events) evt.SetCustomData(V2ToV3.CustomDataEvent(evt.SaveCustom()));

                foreach (var env in EnvironmentEnhancements)
                {
                    env.Position = V2ToV3.RescaleVector3(env.Position);
                    env.LocalPosition = V2ToV3.RescaleVector3(env.LocalPosition);
                    env.Geometry = V2ToV3.Geometry(env.Geometry?.AsObject);
                }
            }

            if (fromVersion == 3 && toVersion == 2)
            {
                foreach (var note in Notes) note.SetCustomData(V3ToV2.CustomDataObject(note.SaveCustom()));
                foreach (var obstacle in Obstacles) obstacle.SetCustomData(V3ToV2.CustomDataObject(obstacle.SaveCustom()));
                foreach (var arc in Arcs) arc.SetCustomData(V3ToV2.CustomDataObject(arc.SaveCustom()));
                foreach (var chain in Chains) chain.SetCustomData(V3ToV2.CustomDataObject(chain.SaveCustom()));
                
                foreach (var evt in Events) evt.SetCustomData(V3ToV2.CustomDataEvent(evt.SaveCustom()));

                foreach (var env in EnvironmentEnhancements)
                {
                    env.Position = V3ToV2.RescaleVector3(env.Position);
                    env.LocalPosition = V3ToV2.RescaleVector3(env.LocalPosition);
                    env.Geometry = V3ToV2.Geometry(env.Geometry?.AsObject);
                }
            }
        }
        
        // TODO: Bullet - I don't think this is needed anymore. Can remove code completely if no issues come up.
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

        public bool Save()
        {
            var outputJson = Settings.Instance.MapVersion switch
            {
                2 => V2Difficulty.GetOutputJson(this),
                3 => V3Difficulty.GetOutputJson(this),
            };

            if (outputJson == null)
                return false;
            
            // Write difficulty file
            File.WriteAllText(DirectoryAndFile, Settings.Instance.FormatJson 
                ? outputJson.ToString(2)
                : outputJson.ToString());
            
            // Write Bpm file
            WriteBPMInfoFile(this);

            return true;
        }

        // Write BPMInfo file for official editor compatibility
        private void WriteBPMInfoFile(BaseDifficulty map)
        {
            JSONNode bpmInfo = new JSONObject();

            var precision = Settings.Instance.BpmTimeValueDecimalPrecision;

            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var audioLength = BeatSaberSongContainer.Instance.LoadedSongLength;
            var audioSamples = BeatSaberSongContainer.Instance.LoadedSongSamples - 1; // Match official editor
            var audioFrequency = BeatSaberSongContainer.Instance.LoadedSongFrequency;

            // If for some reason we have bpm events outside the map and the mapper decided not to remove them,
            // it'll cause weird behaviour in official editor so we'll exclude those
            var maxSongBpmTime = audioLength * songBpm / 60f;
            
            var index = map.BpmEvents.Count - 1;
            while (index >= 0 && map.BpmEvents[index].SongBpmTime > maxSongBpmTime) index--;
            var bpmEvents = (index >= 0)
                ? map.BpmEvents.AsSpan()[..(index + 1)]
                : Span<BaseBpmEvent>.Empty;
            
            bpmInfo["_version"] = "2.0.0";
            bpmInfo["_songSampleCount"] = audioSamples;
            bpmInfo["_songFrequency"] = audioFrequency;

            var regions = new JSONArray();
            if (bpmEvents.Length == 0)
            {
                regions.Add(new JSONObject
                {
                    ["_startSampleIndex"] = 0,
                    ["_endSampleIndex"] = audioSamples,
                    ["_startBeat"] = 0f,
                    ["_endBeat"] = new JSONNumberWithOverridenRounding((songBpm / 60f) * audioLength, precision),
                });
            }
            else
            {
                for (var i = 0; i < bpmEvents.Length - 1; i++)
                {
                    var currentBpmEvent = bpmEvents[i];
                    var nextBpmEvent = bpmEvents[i + 1];

                    regions.Add(new JSONObject
                    {
                        ["_startSampleIndex"] = (int)(currentBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency),
                        ["_endSampleIndex"] = (int)(nextBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency),
                        ["_startBeat"] = new JSONNumberWithOverridenRounding(currentBpmEvent.JsonTime, precision),
                        ["_endBeat"] = new JSONNumberWithOverridenRounding(nextBpmEvent.JsonTime, precision),
                    });
                }

                var lastBpmEvent = bpmEvents[^1];
                var lastStartSampleIndex = (lastBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency);
                var secondsDiff = (audioSamples - lastStartSampleIndex) / audioFrequency;
                var jsonBeatsDiff = secondsDiff * (lastBpmEvent.Bpm / 60f);

                regions.Add(new JSONObject
                {
                    ["_startSampleIndex"] = (int)lastStartSampleIndex,
                    ["_endSampleIndex"] = audioSamples,
                    ["_startBeat"] = new JSONNumberWithOverridenRounding(lastBpmEvent.JsonTime, precision),
                    ["_endBeat"] = new JSONNumberWithOverridenRounding(lastBpmEvent.JsonTime + jsonBeatsDiff, precision),
                });
            }

            bpmInfo["_regions"] = regions;

            File.WriteAllText(Path.Combine(BeatSaberSongContainer.Instance.Song.Directory, "BPMInfo.dat"),
                bpmInfo.ToString(2));
        }
        
        public bool IsChroma() =>
            Notes.Any(x => x.IsChroma())  || Arcs.Any(x => x.IsChroma()) ||
            Chains.Any(x => x.IsChroma()) || Obstacles.Any(x => x.IsChroma()) ||
            Events.Any(x => x.IsChroma()) || EnvironmentEnhancements.Any();

        public bool IsNoodleExtensions() =>
            Notes.Any(x => x.IsNoodleExtensions()) ||
            Arcs.Any(x => x.IsNoodleExtensions()) || Chains.Any(x => x.IsNoodleExtensions()) ||
            Obstacles.Any(x => x.IsNoodleExtensions());
        
        public bool IsMappingExtensions() =>
            Notes.Any(x => x.IsMappingExtensions()) ||
            Arcs.Any(x => x.IsMappingExtensions()) || Chains.Any(x => x.IsMappingExtensions()) ||
            Obstacles.Any(x => x.IsMappingExtensions());
        
        public override JSONNode ToJson() => throw new NotImplementedException();

        public override BaseItem Clone() => throw new NotImplementedException();
    }
}
