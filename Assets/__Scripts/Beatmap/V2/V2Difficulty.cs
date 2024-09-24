using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V2.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V2
{
    public class V2Difficulty
    {
        private const string version = "2.6.0";
        
        public static JSONNode GetOutputJson(BaseDifficulty difficulty)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                var json = new JSONObject { ["_version"] = version };

                var events = new JSONArray();

                var allEvents = new List<BaseObject>();
                allEvents.AddRange(difficulty.Events);
                allEvents.AddRange(difficulty.BpmEvents);
                if (difficulty.BpmEvents.Count > 0 && difficulty.BpmEvents.First().JsonTime != 0)
                {
                    var insertedBpm = (BeatSaberSongContainer.Instance != null)
                        ? BeatSaberSongContainer.Instance.Song.BeatsPerMinute
                        : 100; // This path only appears in tests
                    allEvents.Add(new BaseBpmEvent
                    {
                        JsonTime = 0,
                        Bpm = insertedBpm
                    });
                }
                allEvents.Sort((lhs, rhs) => lhs.JsonTime.CompareTo(rhs.JsonTime));
                foreach (var e in allEvents) events.Add(e.ToJson());
                json["_events"] = events;

                var notes = new JSONArray();
                foreach (var n in difficulty.Notes) notes.Add(n.ToJson());
                json["_notes"] = notes;

                var obstacles = new JSONArray();
                foreach (var o in difficulty.Obstacles) obstacles.Add(o.ToJson());
                json["_obstacles"] = obstacles;

                var waypoints = new JSONArray();
                foreach (var w in difficulty.Waypoints) waypoints.Add(w.ToJson());
                json["_waypoints"] = waypoints;

                json["_sliders"] = new JSONArray();

                json["_specialEventsKeywordFilters"] = difficulty.EventTypesWithKeywords?.Keywords.Length > 0
                    ? difficulty.EventTypesWithKeywords.ToJson()
                    : new JSONObject();

                var customDataJson = GetOutputCustomJsonData(difficulty);
                if (customDataJson.Children.Any())
                    json["_customData"] = customDataJson;

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
                customData["_bookmarks"] = bookmarks;
                customData[difficulty.BookmarksUseOfficialBpmEventsKey] = true;
            }
            
            if (difficulty.CustomEvents.Any())
            {
                var customEvents = new JSONArray();
                foreach (var c in difficulty.CustomEvents) customEvents.Add(c.ToJson());
                customData["_customEvents"] = customEvents;
            }

            if (difficulty.EnvironmentEnhancements.Any())
            {
                var envEnhancements = new JSONArray();
                foreach (var e in difficulty.EnvironmentEnhancements) envEnhancements.Add(e.ToJson());
                customData["_environment"] = envEnhancements;
            }

            if (difficulty.PointDefinitions.Any())
            {
                var pAry = new JSONArray();
                foreach (var p in difficulty.PointDefinitions)
                {
                    var obj = new JSONObject
                    {
                        ["_name"] = p.Key,
                        ["_points"] = p.Value
                    };
                    pAry.Add(obj);
                }

                customData["_pointDefinitions"] = pAry;
            }

            if (difficulty.Materials.Any())
            {
                customData["_materials"] = new JSONObject();
                foreach (var m in difficulty.Materials) 
                    customData["_materials"][m.Key] = m.Value.ToJson();
            }
            

            if (difficulty.Time > 0) customData["_time"] = Math.Round(difficulty.Time, 3);

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
                        case "_events":
                            foreach (JSONNode n in node)
                            {
                                if (n["_type"] != null && n["_type"] == 100)
                                    map.BpmEvents.Add(V2BpmEvent.GetFromJson(n));
                                else
                                    map.Events.Add(V2Event.GetFromJson(n));
                            }
                            break;
                        case "_notes":
                            foreach (JSONNode n in node) map.Notes.Add(V2Note.GetFromJson(n));
                            break;
                        case "_obstacles":
                            foreach (JSONNode n in node) map.Obstacles.Add(V2Obstacle.GetFromJson(n));
                            break;
                        case "_waypoints":
                            foreach (JSONNode n in node) map.Waypoints.Add(V2Waypoint.GetFromJson(n));
                            break;
                        case "_specialEventsKeywordFilter":
                            map.EventTypesWithKeywords = V2SpecialEventsKeywordFilters.GetFromJson(node);
                            break;
                    }
                }

                // Do not assume map is sorted
                map.BpmEvents.Sort();
                map.Events.Sort();
                map.Notes.Sort();
                map.Obstacles.Sort();
                map.Waypoints.Sort();
                
                LoadCustom(map, mainNode);

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
            var bpmList = new List<BaseBpmChange>();
            var bookmarksList = new List<BaseBookmark>();
            var customEventsList = new List<BaseCustomEvent>();
            var pointDefinitions = new Dictionary<string, JSONArray>();
            var envEnhancementsList = new List<BaseEnvironmentEnhancement>();
            var materials = new Dictionary<string, BaseMaterial>();

            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var mKey = nodeEnum.Current.Key;
                var mNode = nodeEnum.Current.Value;

                switch (mKey)
                {
                    case "_customData":
                        map.CustomData = mNode;

                        var dataNodeEnum = mNode.GetEnumerator();
                        while (dataNodeEnum.MoveNext())
                        {
                            var cKey = dataNodeEnum.Current.Key;
                            var cNode = dataNodeEnum.Current.Value;
                            switch (cKey)
                            {
                                case "_BPMChanges":
                                    foreach (JSONNode n in cNode) bpmList.Add(V2BpmChange.GetFromJson(n));
                                    break;
                                case "_bpmChanges":
                                    foreach (JSONNode n in cNode) bpmList.Add(V2BpmChange.GetFromJson(n));
                                    break;
                                case "_bookmarks":
                                    foreach (JSONNode n in cNode) bookmarksList.Add(V2Bookmark.GetFromJson(n));
                                    break;
                                case "_customEvents":
                                    foreach (JSONNode n in cNode) customEventsList.Add(V2CustomEvent.GetFromJson(n));
                                    break;
                                case "_pointDefinitions":
                                    foreach (JSONNode n in cNode)
                                    {
                                        var points = n["_points"].AsArray;
                                        if (!pointDefinitions.ContainsKey(n["_name"]))
                                            pointDefinitions.Add(n["_name"], points);
                                        else
                                            Debug.LogWarning($"Duplicate key {n["_name"]} found in point definitions");
                                    }
                                    break;
                                case "_environment":
                                    foreach (JSONNode n in cNode)
                                        envEnhancementsList.Add(V2EnvironmentEnhancement.GetFromJson(n));
                                    break;
                                case "_materials":
                                    if (cNode is JSONObject matObj)
                                    {
                                        foreach (var n in matObj)
                                            if (!materials.ContainsKey(n.Key))
                                                materials.Add(n.Key, V2Material.GetFromJson(n.Value.AsObject));
                                            else
                                                Debug.LogWarning($"Duplicate key \"{n.Key}\" found in materials");
                                        break;
                                    }
                                    Debug.LogWarning("Could not read materials");
                                    break;
                                case "_time":
                                    map.Time = cNode.AsFloat;
                                    break;
                            }
                        }

                        break;
                    
                    // Keys can be present outside of root CustomData from legacy community editor
                    case "_BPMChanges":
                        foreach (JSONNode n in mNode) bpmList.Add(V2BpmChange.GetFromJson(n));
                        break;
                    case "_bpmChanges":
                        foreach (JSONNode n in mNode) bpmList.Add(V2BpmChange.GetFromJson(n));
                        break;
                    case "_bookmarks":
                        foreach (JSONNode n in mNode) bookmarksList.Add(V2Bookmark.GetFromJson(n));
                        break;
                    case "_customEvents":
                        foreach (JSONNode n in mNode) customEventsList.Add(V2CustomEvent.GetFromJson(n));
                        break;
                }
            }

            // Removing customData keys outside of root customData
            if (mainNode.HasKey("_BPMChanges")) mainNode.Remove("_BPMChanges");
            if (mainNode.HasKey("_bpmChanges")) mainNode.Remove("_bpmChanges");
            if (mainNode.HasKey("_bookmarks")) mainNode.Remove("_bookmarks");
            if (mainNode.HasKey("_customEvents")) mainNode.Remove("_customEvents");

            // Sort and assign
            map.BpmChanges = bpmList.DistinctBy(x => x.JsonTime).ToList();
            map.Bookmarks = bookmarksList;
            map.CustomEvents = customEventsList.DistinctBy(x => x.ToString()).ToList();
            map.PointDefinitions = pointDefinitions;
            map.EnvironmentEnhancements = envEnhancementsList;
            map.Materials = materials;
        }
    }
}
