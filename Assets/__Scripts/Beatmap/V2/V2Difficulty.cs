using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Converters;
using Beatmap.Enums;
using Beatmap.V2.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V2
{
    public class V2Difficulty : BaseDifficulty
    {
        public override string Version => "2.6.0";

        public override string BookmarksUseOfficialBpmEventsKey => "_bookmarksUseOfficialBpmEvents";

        public override bool IsChroma() =>
            Notes.Any(x => x.IsChroma()) || Obstacles.Any(x => x.IsChroma()) || Events.Any(x => x.IsChroma()) ||
            EnvironmentEnhancements.Any();

        public override bool IsNoodleExtensions() =>
            Notes.Any(x => x.IsNoodleExtensions()) || Obstacles.Any(x => x.IsNoodleExtensions()) ||
            Events.Any(x => x.IsNoodleExtensions()) || CustomEvents.Any();

        public override bool IsMappingExtensions() =>
            Notes.Any(x => x.IsMappingExtensions()) || Obstacles.Any(x => x.IsMappingExtensions()) ||
            Events.Any(x => x.IsMappingExtensions());

        public override bool Save()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                MainNode ??= new JSONObject();
                MainNode["_version"] = Version;
                ParseV3ToV2();

                var events = new JSONArray();

                var allEvents = new List<BaseObject>();
                allEvents.AddRange(Events);
                allEvents.AddRange(BpmEvents);
                if (BpmEvents.Count > 0 && BpmEvents.First().JsonTime != 0)
                {
                    allEvents.Add(new V2BpmEvent()
                    {
                        JsonTime = 0,
                        Bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute
                    });
                }
                allEvents.Sort((lhs, rhs) => lhs.JsonTime.CompareTo(rhs.JsonTime));
                foreach (var e in allEvents) events.Add(e.ToJson());

                var notes = new JSONArray();
                foreach (var n in Notes) notes.Add(n.ToJson());

                var obstacles = new JSONArray();
                foreach (var o in Obstacles) obstacles.Add(o.ToJson());

                var waypoints = new JSONArray();
                foreach (var w in Waypoints) waypoints.Add(w.ToJson());

                MainNode["_notes"] = CleanupArray(notes);
                MainNode["_obstacles"] = CleanupArray(obstacles);
                MainNode["_events"] = CleanupArray(events);
                MainNode["_waypoints"] = CleanupArray(waypoints);
                MainNode["_sliders"] = new JSONArray();
                if (EventTypesWithKeywords?.Keywords.Length > 0)
                    MainNode["_specialEventsKeywordFilters"] = EventTypesWithKeywords.ToJson();

                SaveCustom();

                WriteDifficultyFile(this);
                WriteBPMInfoFile(this);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(
                    "This is bad. You are recommended to restart ChroMapper; progress made after this point is not guaranteed to be saved.");
                return false;
            }
        }

        public override bool SaveCustom()
        {
            var bookmarks = new JSONArray();
            foreach (var b in Bookmarks) bookmarks.Add(b.ToJson());

            var customEvents = new JSONArray();
            foreach (var c in CustomEvents) customEvents.Add(c.ToJson());

            var envEnhancements = new JSONArray();
            foreach (var e in EnvironmentEnhancements) envEnhancements.Add(e.ToJson());

            MainNode["_customData"] = CustomData ?? new JSONObject();
            MainNode["_customData"].Remove("_BPMChanges");

            if (Bookmarks.Any())
                MainNode["_customData"]["_bookmarks"] = CleanupArray(bookmarks);
            else
                MainNode["_customData"].Remove("_bookmarks");

            if (CustomEvents.Any())
                MainNode["_customData"]["_customEvents"] = CleanupArray(customEvents);
            else
                MainNode["_customData"].Remove("_customEvents");

            if (PointDefinitions.Any())
            {
                var pAry = new JSONArray();
                foreach (var p in PointDefinitions)
                {
                    var obj = new JSONObject { ["_name"] = p.Key };
                    var points = new JSONArray();
                    foreach (var ary in p.Value) points.Add(ary);
                    obj["_points"] = points;
                    pAry.Add(obj);
                }

                MainNode["_customData"]["_pointDefinitions"] = pAry;
            }
            else
            {
                MainNode["_customData"].Remove("_pointDefinitions");
            }

            if (EnvironmentEnhancements.Any())
                MainNode["_customData"]["_environment"] = envEnhancements;
            else
                MainNode["_customData"].Remove("_environment");

            if (Materials.Any())
            {
                MainNode["_customData"]["_materials"] = new JSONObject();
                foreach (var m in Materials) MainNode["_customData"]["_materials"][m.Key] = m.Value;
            }
            else
            {
                MainNode["_customData"].Remove("_materials");
            }

            if (Time > 0) MainNode["_customData"]["_time"] = Math.Round(Time, 3);
            if (Bookmarks.Count > 0) MainNode["_customData"][BookmarksUseOfficialBpmEventsKey] = true;

            BeatSaberSong.CleanObject(MainNode["_customData"]);
            if (!MainNode["_customData"].Children.Any()) MainNode.Remove("_customData");

            return true;
        }

        private void ParseV3ToV2()
        {
            var newNotes = new List<BaseNote>();
            foreach (var n in Notes)
                switch (n.Type)
                {
                    case (int)NoteType.Bomb:
                        newNotes.Add(V3ToV2.Note(n));
                        break;
                    case (int)NoteType.Red:
                    case (int)NoteType.Blue:
                        newNotes.Add(V3ToV2.Note(n));
                        break;
                    default:
                        Debug.LogError("Unsupported note type for Beatmap version 2.0.0");
                        break;
                }

            Notes = newNotes;

            Obstacles = Obstacles.Select(V3ToV2.Obstacle).Cast<BaseObstacle>().ToList();

            Arcs = new List<BaseArc>(); // we purge them anyway

            var newEvents = new List<BaseEvent>();
            foreach (var e in Events)
                switch (e.Type)
                {
                    case (int)EventTypeValue.ColorBoost:
                        newEvents.Add(V3ToV2.Event(e));
                        break;
                    case (int)EventTypeValue.EarlyLaneRotation:
                    case (int)EventTypeValue.LateLaneRotation:
                        newEvents.Add(V3ToV2.Event(e));
                        break;
                    case (int)EventTypeValue.BpmChange:
                        newEvents.Add(V3ToV2.Event(e));
                        break;
                    default:
                        newEvents.Add(V3ToV2.Event(e));
                        break;
                }

            Events = newEvents;
            BpmEvents = BpmEvents.Select(V3ToV2.BpmEvent).Cast<BaseBpmEvent>().ToList();

            Bookmarks = Bookmarks.Select(V3ToV2.Bookmark).Cast<BaseBookmark>().ToList();
            BpmChanges = BpmChanges.Select(V3ToV2.BpmChange).Cast<BaseBpmChange>().ToList();
            CustomEvents = CustomEvents.Select(V3ToV2.CustomEvent).Cast<BaseCustomEvent>().ToList();
            EnvironmentEnhancements = EnvironmentEnhancements.Select(V3ToV2.EnvironmentEnhancement)
                .Cast<BaseEnvironmentEnhancement>().ToList();
        }

        public override JSONNode ToJson() => throw new NotImplementedException();

        public override BaseItem Clone() => throw new NotSupportedException();

        public static V2Difficulty GetFromJson(JSONNode mainNode, string path)
        {
            try
            {
                var map = new V2Difficulty { MainNode = mainNode, DirectoryAndFile = path };

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
                                    map.BpmEvents.Add(new V2BpmEvent(n));
                                else
                                    map.Events.Add(new V2Event(n));
                            }
                            break;
                        case "_notes":
                            foreach (JSONNode n in node) map.Notes.Add(new V2Note(n));
                            break;
                        case "_obstacles":
                            foreach (JSONNode n in node) map.Obstacles.Add(new V2Obstacle(n));
                            break;
                        case "_waypoints":
                            foreach (JSONNode n in node) map.Waypoints.Add(new V2Waypoint(n));
                            break;
                        case "_specialEventsKeywordFilter":
                            map.EventTypesWithKeywords = new V2SpecialEventsKeywordFilters(node);
                            break;
                    }
                }

                LoadCustom(map, mainNode);

                return map;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static void LoadCustom(V2Difficulty map, JSONNode mainNode)
        {
            var bpmList = new List<BaseBpmChange>();
            var bookmarksList = new List<BaseBookmark>();
            var customEventsList = new List<BaseCustomEvent>();
            var pointDefinitions = new Dictionary<string, List<JSONNode>>();
            var envEnhancementsList = new List<BaseEnvironmentEnhancement>();
            var materials = new Dictionary<string, JSONObject>();

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
                                    foreach (JSONNode n in cNode) bpmList.Add(new V2BpmChange(n));
                                    break;
                                case "_bpmChanges":
                                    foreach (JSONNode n in cNode) bpmList.Add(new V2BpmChange(n));
                                    break;
                                case "_bookmarks":
                                    foreach (JSONNode n in cNode) bookmarksList.Add(new V2Bookmark(n));
                                    break;
                                case "_customEvents":
                                    foreach (JSONNode n in cNode) customEventsList.Add(new V2CustomEvent(n));
                                    break;
                                case "_pointDefinitions":
                                    foreach (JSONNode n in cNode)
                                    {
                                        var points = new List<JSONNode>();
                                        foreach (var p in n["_points"]) points.Add(p);
                                        if (!pointDefinitions.ContainsKey(n["_name"]))
                                            pointDefinitions.Add(n["_name"], points);
                                        else
                                            Debug.LogWarning($"Duplicate key {n["_name"]} found in point definitions");
                                    }
                                    break;
                                case "_environment":
                                    foreach (JSONNode n in cNode)
                                        envEnhancementsList.Add(new V2EnvironmentEnhancement(n));
                                    break;
                                case "_materials":
                                    if (cNode is JSONObject matObj)
                                    {
                                        foreach (var n in matObj)
                                            if (!materials.ContainsKey(n.Key))
                                                materials.Add(n.Key, n.Value.AsObject);
                                            else
                                                Debug.LogWarning($"Duplicate key {n.Key} found in materials");
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
                    case "_BPMChanges":
                        foreach (JSONNode n in mNode) bpmList.Add(new V2BpmChange(n));
                        break;
                    case "_bpmChanges":
                        foreach (JSONNode n in mNode) bpmList.Add(new V2BpmChange(n));
                        break;
                    case "_bookmarks":
                        foreach (JSONNode n in mNode) bookmarksList.Add(new V2Bookmark(n));
                        break;
                    case "_customEvents":
                        foreach (JSONNode n in mNode) customEventsList.Add(new V2CustomEvent(n));
                        break;
                }
            }

            if (mainNode.HasKey("_BPMChanges")) mainNode.Remove("_BPMChanges");
            if (mainNode.HasKey("_bpmChanges")) mainNode.Remove("_bpmChanges");
            if (mainNode.HasKey("_bookmarks")) mainNode.Remove("_bookmarks");
            if (mainNode.HasKey("_customEvents")) mainNode.Remove("_customEvents");

            map.BpmChanges = bpmList.DistinctBy(x => x.JsonTime).ToList();
            map.Bookmarks = bookmarksList;
            map.CustomEvents = customEventsList.DistinctBy(x => x.ToString()).ToList();
            map.PointDefinitions = pointDefinitions;
            map.EnvironmentEnhancements = envEnhancementsList;
        }
    }
}
