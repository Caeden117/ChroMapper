using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3Difficulty
    {
        private const string version = "3.3.0";

        public static JSONNode GetOutputJson(BaseDifficulty difficulty)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                var json = new JSONObject { ["version"] = version };

                var bpmEvents = new JSONArray();
                if (difficulty.BpmEvents.Count > 0 && difficulty.BpmEvents.First().JsonTime != 0)
                {                    
                    var insertedBpm = (BeatSaberSongContainer.Instance != null)
                        ? BeatSaberSongContainer.Instance.Song.BeatsPerMinute
                        : 100; // This path only appears in tests
                    difficulty.BpmEvents.Insert(0, new BaseBpmEvent
                    {
                        JsonTime = 0,
                        Bpm = insertedBpm
                    });
                }
                foreach (var b in difficulty.BpmEvents) bpmEvents.Add(b.ToJson());
                json["bpmEvents"] = bpmEvents;

                // Events
                var basicBeatmapEvents = new JSONArray();
                var colorBoostBeatmapEvents = new JSONArray();
                var rotationEvents = new JSONArray();

                foreach (var evt in difficulty.Events)
                {
                    switch (evt.Type)
                    {
                        case (int)EventTypeValue.ColorBoost:
                            colorBoostBeatmapEvents.Add(evt.ToJson());
                            break;
                        
                        case (int)EventTypeValue.EarlyLaneRotation:
                        case (int)EventTypeValue.LateLaneRotation:
                            rotationEvents.Add(evt.ToJson());
                            break;
                        
                        default:
                            basicBeatmapEvents.Add(evt.ToJson());
                            break;
                    }
                }
                
                json["rotationEvents"] = rotationEvents;
                json["basicBeatmapEvents"] = basicBeatmapEvents;
                json["colorBoostBeatmapEvents"] = colorBoostBeatmapEvents;
                
                // Notes
                var colorNotes = new JSONArray();
                var bombNotes = new JSONArray();

                foreach (var note in difficulty.Notes)
                {
                    switch (note.Type)
                    {
                        case (int)NoteType.Red:
                        case (int)NoteType.Blue:
                            if (!note.CustomFake) colorNotes.Add(note.ToJson());
                            break;
                        case (int)NoteType.Bomb:
                            if (!note.CustomFake) bombNotes.Add(note.ToJson());
                            break;
                    }
                }
                
                json["colorNotes"] = colorNotes;
                json["bombNotes"] = bombNotes;
                
                var obstacles = new JSONArray();
                foreach (var o in difficulty.Obstacles.Where(o => !o.CustomFake)) obstacles.Add(o.ToJson());
                json["obstacles"] = obstacles;
                
                var arcs = new JSONArray();
                foreach (var a in difficulty.Arcs) arcs.Add(a.ToJson());
                json["sliders"] = arcs;
                
                var chains = new JSONArray();
                foreach (var c in difficulty.Chains.Where(c => !c.CustomFake)) chains.Add(c.ToJson());
                json["burstSliders"] = chains;

                var waypoints = new JSONArray();
                foreach (var w in difficulty.Waypoints) waypoints.Add(w.ToJson());
                json["waypoints"] = waypoints;
                
                var lightColorEventBoxGroups = new JSONArray();
                foreach (var e in difficulty.LightColorEventBoxGroups) lightColorEventBoxGroups.Add(e.ToJson());
                json["lightColorEventBoxGroups"] = lightColorEventBoxGroups;

                var lightRotationEventBoxGroups = new JSONArray();
                foreach (var e in difficulty.LightRotationEventBoxGroups) lightRotationEventBoxGroups.Add(e.ToJson());
                json["lightRotationEventBoxGroups"] = lightRotationEventBoxGroups;

                var lightTranslationEventBoxGroups = new JSONArray();
                foreach (var e in difficulty.LightTranslationEventBoxGroups) lightTranslationEventBoxGroups.Add(e.ToJson());
                json["lightTranslationEventBoxGroups"] = lightTranslationEventBoxGroups;

                var vfxEventBoxGroups = new JSONArray();
                foreach (var e in difficulty.VfxEventBoxGroups) vfxEventBoxGroups.Add(e.ToJson());
                json["vfxEventBoxGroups"] = vfxEventBoxGroups;
                
                json["_fxEventsCollection"] = difficulty.FxEventsCollection?.ToJson() ?? new BaseFxEventsCollection().ToJson();
                json["basicEventTypesWithKeywords"] = difficulty.EventTypesWithKeywords?.ToJson() ?? new BaseEventTypesWithKeywords().ToJson();
                json["useNormalEventsAsCompatibleEvents"] = difficulty.UseNormalEventsAsCompatibleEvents;
                
                // Do this before adding customData
                if (Settings.Instance.SaveWithoutDefaultValues)
                {
                    SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);
                }

                var customDataJson = GetOutputCustomJsonData(difficulty);
                if (customDataJson.Children.Any())
                    json["customData"] = customDataJson;

                return json;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(
                    "This is bad. You are recommended to restart ChroMapper; progress made after this point is not guaranteed to be saved.");
                return null;
            }
        }

        private static JSONNode GetOutputCustomJsonData(BaseDifficulty difficulty)
        {
            var customData = new JSONObject();

            if (difficulty.Bookmarks.Any())
            {
                var bookmarks = new JSONArray();
                foreach (var b in difficulty.Bookmarks) bookmarks.Add(b.ToJson());
                customData["bookmarks"] = bookmarks;
                customData[difficulty.BookmarksUseOfficialBpmEventsKey] = true;
            }
            
            if (difficulty.CustomEvents.Any())
            {
                var customEvents = new JSONArray();
                foreach (var c in difficulty.CustomEvents) customEvents.Add(c.ToJson());
                customData["customEvents"] = customEvents;
            }

            if (difficulty.EnvironmentEnhancements.Any())
            {
                var envEnhancements = new JSONArray();
                foreach (var e in difficulty.EnvironmentEnhancements) envEnhancements.Add(e.ToJson());
                customData["environment"] = envEnhancements;
            }
            
            if (difficulty.PointDefinitions.Any())
            {
                customData["pointDefinitions"] = new JSONObject();
                foreach (var p in difficulty.PointDefinitions)
                {
                    customData["pointDefinitions"][p.Key] = p.Value;
                }
            }

            if (difficulty.Materials.Any())
            {
                customData["materials"] = new JSONObject();
                foreach (var m in difficulty.Materials) 
                    customData["materials"][m.Key] = m.Value.ToJson();
            }
            
            if (difficulty.Time > 0) customData["time"] = Math.Round(difficulty.Time, 3);

            // All the fake stuff here :3
            var fakeColorNotes = new JSONArray();
            var fakeBombNotes = new JSONArray();
            foreach (var note in difficulty.Notes)
            {
                switch (note.Type)
                {
                    case (int)NoteType.Red:
                    case (int)NoteType.Blue:
                        if (note.CustomFake) fakeColorNotes.Add(note.ToJson());
                        break;
                    case (int)NoteType.Bomb:
                        if (note.CustomFake) fakeBombNotes.Add(note.ToJson());
                        break;
                }
            }
            customData["fakeColorNotes"] = fakeColorNotes;
            customData["fakeBombNotes"] = fakeBombNotes;
            

            var fakeObstacles = new JSONArray();
            foreach (var o in difficulty.Obstacles.Where(o => o.CustomFake)) fakeObstacles.Add(o.ToJson());
                customData["fakeObstacles"] = fakeObstacles;

            var fakeBurstSliders = new JSONArray();
            foreach (var c in difficulty.Chains.Where(c => c.CustomFake)) fakeBurstSliders.Add(c.ToJson());
            customData["fakeBurstSliders"] = fakeBurstSliders;
            
            BeatSaberSong.CleanObject(customData);

            return customData;
        }

        public static BaseDifficulty GetFromJson(JSONNode mainNode, string path)
        {
            try
            {
                var map = new BaseDifficulty { DirectoryAndFile = path, Version = version };

                var nodeEnum = mainNode.GetEnumerator();
                while (nodeEnum.MoveNext())
                {
                    var key = nodeEnum.Current.Key;
                    var node = nodeEnum.Current.Value;

                    switch (key)
                    {
                        // Notes and bombs are in same array
                        case "colorNotes":
                            foreach (JSONNode n in node) map.Notes.Add(V3ColorNote.GetFromJson(n));
                            break;
                        case "bombNotes":
                            foreach (JSONNode n in node) map.Notes.Add(V3BombNote.GetFromJson(n));
                            break;
                        
                        // Basic, boost, and rotation events are in same array
                        case "basicBeatmapEvents":
                            foreach (JSONNode n in node) map.Events.Add(V3BasicEvent.GetFromJson(n));
                            break;
                        case "colorBoostBeatmapEvents":
                            foreach (JSONNode n in node) map.Events.Add(V3ColorBoostEvent.GetFromJson(n));
                            break;
                        case "rotationEvents":
                            foreach (JSONNode n in node) map.Events.Add(V3RotationEvent.GetFromJson(n));
                            break;
                        
                        case "bpmEvents":
                            foreach (JSONNode n in node) map.BpmEvents.Add(V3BpmEvent.GetFromJson(n));
                            break;
                        case "obstacles":
                            foreach (JSONNode n in node) map.Obstacles.Add(V3Obstacle.GetFromJson(n));
                            break;
                        case "sliders":
                            foreach (JSONNode n in node) map.Arcs.Add(V3Arc.GetFromJson(n));
                            break;
                        case "burstSliders":
                            foreach (JSONNode n in node) map.Chains.Add(V3Chain.GetFromJson(n));
                            break;
                        case "waypoints":
                            foreach (JSONNode n in node) map.Waypoints.Add(V3Waypoint.GetFromJson(n));
                            break;
                        case "lightColorEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.LightColorEventBoxGroups.Add(V3LightColorEventBoxGroup.GetFromJson(n));
                            break;
                        case "lightRotationEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.LightRotationEventBoxGroups.Add(V3LightRotationEventBoxGroup.GetFromJson(n));
                            break;
                        case "lightTranslationEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.LightTranslationEventBoxGroups.Add(V3LightTranslationEventBoxGroup.GetFromJson(n));
                            break;
                        case "vfxEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.VfxEventBoxGroups.Add(V3VfxEventEventBoxGroup.GetFromJson(n));
                            break;
                        case "_fxEventsCollection":
                            map.FxEventsCollection = V3FxEventsCollection.GetFromJson(node);
                            break;
                        case "basicEventTypesWithKeywords":
                            map.EventTypesWithKeywords = V3BasicEventTypesWithKeywords.GetFromJson(node);
                            break;
                        case "useNormalEventsAsCompatibleEvents":
                            map.UseNormalEventsAsCompatibleEvents = node.AsBool;
                            break;
                    }
                }

                LoadCustom(map, mainNode);
                
                // Important!
                map.Notes.Sort();
                map.Events.Sort();
                
                // Do not assume map is sorted for other things anyway
                map.BpmEvents.Sort();
                map.Obstacles.Sort();
                map.Waypoints.Sort();
                map.Chains.Sort();
                map.Arcs.Sort();

                return map;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static void LoadCustom(BaseDifficulty map, JSONNode mainNode)
        {
            if (mainNode["customData"] == null) return;
            map.CustomData = mainNode["customData"];

            var bpmList = new List<BaseBpmChange>();
            var bookmarksList = new List<BaseBookmark>();
            var customEventsList = new List<BaseCustomEvent>();
            var pointDefinitions = new Dictionary<string, JSONArray>();
            var envEnhancementsList = new List<BaseEnvironmentEnhancement>();
            var materials = new Dictionary<string, BaseMaterial>();

            var nodeEnum = mainNode["customData"].GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "BPMChanges":
                        foreach (JSONNode n in node) bpmList.Add(V3BpmChange.GetFromJson(n));
                        break;
                    case "bookmarks":
                        foreach (JSONNode n in node) bookmarksList.Add(V3Bookmark.GetFromJson(n));
                        break;
                    case "customEvents":
                        foreach (JSONNode n in node) customEventsList.Add(V3CustomEvent.GetFromJson(n));
                        break;
                    case "fakeColorNotes":
                        foreach (JSONNode n in node) map.Notes.Add(V3ColorNote.GetFromJson(n, true));
                        break;
                    case "fakeBombNotes":
                        foreach (JSONNode n in node) map.Notes.Add(V3BombNote.GetFromJson(n, true));
                        break;
                    case "fakeObstacles":
                        foreach (JSONNode n in node) map.Obstacles.Add(V3Obstacle.GetFromJson(n, true));
                        break;
                    case "fakeBurstSliders":
                        foreach (JSONNode n in node) map.Chains.Add(V3Chain.GetFromJson(n, true));
                        break;
                    case "pointDefinitions":
                        // TODO: array is incorrect, but some old v3 NE/Chroma map uses them, temporarily this needs to be here
                        if (node is JSONArray nodeAry)
                        {
                            foreach (var n in nodeAry)
                            {
                                if (!(n.Value is JSONObject obj)) continue;

                                if (!pointDefinitions.ContainsKey(n.Key))
                                    pointDefinitions.Add(obj["name"], obj["points"].AsArray);
                                else
                                    Debug.LogWarning($"Duplicate key {n.Key} found in point definitions");
                            }

                            break;
                        }

                        if (node is JSONObject nodeObj)
                        {
                            foreach (var n in nodeObj)
                            {
                                if (!pointDefinitions.ContainsKey(n.Key))
                                    pointDefinitions.Add(n.Key, n.Value.AsArray);
                                else
                                    Debug.LogWarning($"Duplicate key {n.Key} found in point definitions");
                            }

                            break;
                        }

                        Debug.LogWarning("Could not read point definitions");
                        break;
                    case "environment":
                        foreach (JSONNode n in node) envEnhancementsList.Add(V3EnvironmentEnhancement.GetFromJson(n));
                        break;
                    case "materials":
                        if (node is JSONObject matObj)
                        {
                            foreach (var n in matObj)
                                if (!materials.ContainsKey(n.Key))
                                    materials.Add(n.Key, V3Material.GetFromJson(n.Value.AsObject));
                                else
                                    Debug.LogWarning($"Duplicate key {n.Key} found in materials");
                            break;
                        }

                        Debug.LogWarning("Could not read materials");
                        break;
                    case "time":
                        map.Time = node.AsFloat;
                        break;
                }
            }

            map.BpmChanges = bpmList.DistinctBy(x => x.JsonTime).ToList();
            map.Bookmarks = bookmarksList;
            map.CustomEvents = customEventsList.DistinctBy(x => x.ToString()).ToList();
            map.PointDefinitions = pointDefinitions;
            map.EnvironmentEnhancements = envEnhancementsList;
            map.Materials = materials;
        }
    }
}
