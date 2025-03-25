using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Converters;
using Beatmap.Helper;
using Beatmap.Info;
using Beatmap.V2;
using Beatmap.V3;
using Beatmap.V4;
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

        public string Version { get; set; } = "3.3.0";

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

        public List<BaseBpmEvent> BpmEvents { get; set; } = new();
        public List<BaseNote> Notes { get; set; } = new();
        public List<BaseObstacle> Obstacles { get; set; } = new();
        public List<BaseArc> Arcs { get; set; } = new();
        public List<BaseChain> Chains { get; set; } = new();
        public List<BaseWaypoint> Waypoints { get; set; } = new();
        public List<BaseEvent> Events { get; set; } = new();
        public List<BaseNJSEvent> NJSEvents { get; set; } = new();

        public List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> LightColorEventBoxGroups { get; set; } = new();

        public List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
            LightRotationEventBoxGroups { get; set; } = new();

        public List<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>>
            LightTranslationEventBoxGroups { get; set; } = new();

        public List<BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>> VfxEventBoxGroups { get; set; } = new();
        public BaseFxEventsCollection FxEventsCollection { get; set; } = new();

        public BaseEventTypesWithKeywords EventTypesWithKeywords { get; set; }
        public bool UseNormalEventsAsCompatibleEvents { get; set; } = true;

        public float Time { get; set; }
        public List<BaseBpmChange> BpmChanges { get; set; } = new();
        public List<BaseBookmark> Bookmarks { get; set; } = new();
        public string BookmarksUseOfficialBpmEventsKey => Settings.Instance.MapVersion switch
        {
            2 => "_bookmarksUseOfficialBpmEvents",
            3 => "bookmarksUseOfficialBpmEvents",
            4 => "bookmarksUseOfficialBpmEvents",
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

        #region BPM Time Conversion Logic

        private float? songBpm;

        public void BootstrapBpmEvents(float songBpm)
        {
            // remove invalid bpm events
            BpmEvents.RemoveAll(x => x.JsonTime < 0);
            BpmEvents.RemoveAll(x => x.Bpm < 0);

            if (!BpmEvents.Any()) return;

            BpmEvents.Sort();

            // insert beat 0 bpm event if needed
            if (BpmEvents.First().JsonTime > 0)
            {
                var newBpmEvent = new BaseBpmEvent(0, songBpm);
                BpmEvents.Insert(0, newBpmEvent);
            }

            BaseBpmEvent lastBpmEvent = null;
            foreach (var bpmEvent in BpmEvents)
            {
                if (lastBpmEvent is null)
                {
                    bpmEvent.songBpmTime = bpmEvent.JsonTime;
                }
                else
                {
                    bpmEvent.songBpmTime = lastBpmEvent.songBpmTime + (bpmEvent.JsonTime - lastBpmEvent.JsonTime) * (songBpm / lastBpmEvent.Bpm);
                }

                lastBpmEvent = bpmEvent;
            }

            this.songBpm = songBpm;
        }

        public float? JsonTimeToSongBpmTime(float jsonTime)
        {
            if (songBpm is null) return null;
            var lastBpmEvent = FindLastBpmEventByJsonTime(jsonTime);
            if (lastBpmEvent is null)
            {
                return jsonTime;
            }
            return lastBpmEvent.SongBpmTime + (jsonTime - lastBpmEvent.JsonTime) * (songBpm / lastBpmEvent.Bpm);
        }

        public float? SongBpmTimeToJsonTime(float songBpmTime)
        {
            if (songBpm is null) return null;
            var lastBpmEvent = FindLastBpmEventBySongBpmTime(songBpmTime);
            if (lastBpmEvent is null)
            {
                return songBpmTime;
            }
            return lastBpmEvent.JsonTime + (songBpmTime - lastBpmEvent.SongBpmTime) * (lastBpmEvent.Bpm / songBpm);
        }

        public BaseBpmEvent FindLastBpmEventByJsonTime(float jsonTime)
        {
            return BpmEvents.LastOrDefault(x => x.JsonTime <= jsonTime);
        }

        public BaseBpmEvent FindLastBpmEventBySongBpmTime(float songBpmTime)
        {
            if (songBpm is null) return null;
            return BpmEvents.LastOrDefault(x => x.SongBpmTime <= songBpmTime);
        }

        public float? BpmAtJsonTime(float jsonTime)
        {
            return FindLastBpmEventByJsonTime(jsonTime)?.Bpm ?? songBpm;
        }

        public float? BpmAtSongBpmTime(float songBpmTime)
        {
            return FindLastBpmEventBySongBpmTime(songBpmTime)?.Bpm ?? songBpm;
        }

        public void RecomputeAllObjectSongBpmTimes()
        {
            foreach (var objList in AllBaseObjectProperties())
            {
                if (objList is null) continue;
                foreach (var obj in objList)
                {
                    obj.RecomputeSongBpmTime();
                }
            }
        }

        #endregion

        public void ConvertCustomBpmToOfficial()
        {
            var songBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var customData = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData;
            
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

            CustomData[BookmarksUseOfficialBpmEventsKey] = true;

            BpmChanges.Clear();
        }

        public void ConvertCustomDataVersion(int fromVersion, int toVersion)
        {
            if (fromVersion == 2 && toVersion is 3 or 4)
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

                foreach (var evt in CustomEvents) evt.SetData(V2ToV3.CustomEventData(evt.SaveCustom()));
                
                CustomData = V2ToV3.CustomDataRoot(CustomData, this);
            }

            if (fromVersion is 3 or 4 && toVersion == 2)
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

                foreach (var evt in CustomEvents) evt.SetData(V3ToV2.CustomEventData(evt.SaveCustom()));

                CustomData = V3ToV2.CustomDataRoot(CustomData, this);
            }
        }

        public bool Save()
        {
            var outputJson = Settings.Instance.MapVersion switch
            {
                2 => V2Difficulty.GetOutputJson(this),
                3 => V3Difficulty.GetOutputJson(this),
                4 => V4Difficulty.GetOutputJson(this)
            };
            
            if (outputJson == null)
                return false;

            // Write difficulty file
            File.WriteAllText(DirectoryAndFile, Settings.Instance.FormatJson
                ? outputJson.ToString(2)
                : outputJson.ToString());

            // Write lightshow file if in v4
            var songContainer = BeatSaberSongContainer.Instance;
            var mapDifficultyInfo = songContainer.MapDifficultyInfo;
            if (Settings.Instance.MapVersion == 4)
            {
                // Either user error or an non-v4 map has been converted to v4.
                // In this case, write to a different file
                if (mapDifficultyInfo.BeatmapFileName == mapDifficultyInfo.LightshowFileName)
                {
                    mapDifficultyInfo.LightshowFileName = $"LightsFor-{mapDifficultyInfo.LightshowFileName}";
                }

                var lightshowJson = V4Difficulty.GetLightshowOutputJson(this);
                File.WriteAllText(Path.Combine(songContainer.Info.Directory, mapDifficultyInfo.LightshowFileName),
                    Settings.Instance.FormatJson
                        ? lightshowJson.ToString(2)
                        : lightshowJson.ToString());
                
                // Write bookmarks for official editor compability
                var bookmarksJson = GetOfficialBookmarkOutputJson(mapDifficultyInfo.Characteristic, mapDifficultyInfo.Difficulty);
                var bookmarksFolder = Path.Combine(songContainer.Info.Directory, "Bookmarks");
                if (!Directory.Exists(bookmarksFolder)) Directory.CreateDirectory(bookmarksFolder);
                File.WriteAllText(Path.Combine(bookmarksFolder, mapDifficultyInfo.BookmarkFileName), 
                    bookmarksJson.ToString(2));
            }
            else
            {
                // Separate lightshows are not loaded in non-v4, so we'll change the lightshow to refer to the same file
                mapDifficultyInfo.LightshowFileName = mapDifficultyInfo.BeatmapFileName;
            }

            // Write Bpm file
            var bpmRegions = BaseBpmInfo.GetBpmInfoRegions(BpmEvents, songContainer.Info.BeatsPerMinute,
                songContainer.LoadedSongSamples, songContainer.LoadedSongFrequency);
            var bpmInfo = new BaseBpmInfo { BpmRegions = bpmRegions }.InitWithSongContainerInstance();

            // Don't write if created difficulty before supplying audio file
            if (bpmInfo.AudioSamples != default)
            {
                var bpmOutputJson = songContainer.Info.MajorVersion switch
                {
                    2 => V2BpmInfo.GetOutputJson(bpmInfo),
                    4 => V4AudioData.GetOutputJson(bpmInfo),
                };

                if (bpmOutputJson == null) return true;
                
                var bpmOutputFileName = BaseBpmInfo.GetOutputFileName(songContainer.Info.MajorVersion, songContainer.Info);
                
                File.WriteAllText(Path.Combine(songContainer.Info.Directory, bpmOutputFileName),
                    bpmOutputJson.ToString(2));
            }

            return true;
        }
        
        private JSONNode GetOfficialBookmarkOutputJson(string characteristic, string difficulty)
        {
            var json = new JSONObject();
            json["name"] = "ChroMapper";
            json["characteristic"] = characteristic;
            json["difficulty"] = difficulty;
            json["color"] = "00FFFF"; // Cyan

            var bookmarksNode = new JSONArray();
            foreach (var bookmark in Bookmarks)
            {
                var bookmarkObject = new JSONObject();
                bookmarkObject["beat"] = bookmark.JsonTime;
                bookmarkObject["label"] = bookmark.Name;
                bookmarkObject["text"] = bookmark.Name;
                bookmarksNode.Add(bookmarkObject);
            }
            
            json["bookmarks"] = bookmarksNode;
            
            return json;
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
