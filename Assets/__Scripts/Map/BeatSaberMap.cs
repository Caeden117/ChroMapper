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
public class BeatSaberMap
{
    [FormerlySerializedAs("directoryAndFile")] public string DirectoryAndFile;

    [FormerlySerializedAs("_version")] public string Version = "2.2.0";

    /// <summary>
    ///     Time (in Minutes) that the user has worked on this map.
    /// </summary>
    [FormerlySerializedAs("_time")] public float Time;

    [FormerlySerializedAs("_events")] public List<MapEvent> Events = new List<MapEvent>();
    [FormerlySerializedAs("_notes")] public List<BeatmapNote> Notes = new List<BeatmapNote>();
    [FormerlySerializedAs("_obstacles")] public List<BeatmapObstacle> Obstacles = new List<BeatmapObstacle>();
    [FormerlySerializedAs("_waypoints")] public List<JSONNode> Waypoints = new List<JSONNode>(); // TODO: Add formal support
    [FormerlySerializedAs("_BPMChanges")] public List<BeatmapBPMChange> BpmChanges = new List<BeatmapBPMChange>();
    [FormerlySerializedAs("_bookmarks")] public List<BeatmapBookmark> Bookmarks = new List<BeatmapBookmark>();
    [FormerlySerializedAs("_customEvents")] public List<BeatmapCustomEvent> CustomEvents = new List<BeatmapCustomEvent>();
    [FormerlySerializedAs("_envEnhancements")] public List<EnvEnhancement> EnvEnhancements = new List<EnvEnhancement>();
    public JSONNode CustomData = new JSONObject();

    public JSONNode MainNode;

    public bool Save()
    {
        try
        {
            /*
             * LISTS
             */

            //Just in case, I'm moving this up here
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (MainNode is null) MainNode = new JSONObject();

            MainNode["_version"] = Version;

            var events = new JSONArray();
            foreach (var e in Events) events.Add(e.ConvertToJson());

            var notes = new JSONArray();
            foreach (var n in Notes) notes.Add(n.ConvertToJson());

            var obstacles = new JSONArray();
            foreach (var o in Obstacles) obstacles.Add(o.ConvertToJson());

            var bpm = new JSONArray();
            foreach (var b in BpmChanges) bpm.Add(b.ConvertToJson());

            var bookmarks = new JSONArray();
            foreach (var b in Bookmarks) bookmarks.Add(b.ConvertToJson());

            var customEvents = new JSONArray();
            foreach (var c in CustomEvents) customEvents.Add(c.ConvertToJson());

            var waypoints = new JSONArray(); // TODO: Add formal support
            foreach (var w in Waypoints) waypoints.Add(w);

            var envEnhancements = new JSONArray();
            foreach (var e in EnvEnhancements) envEnhancements.Add(e.ConvertToJson());

            MainNode["_notes"] = CleanupArray(notes);
            MainNode["_obstacles"] = CleanupArray(obstacles);
            MainNode["_events"] = CleanupArray(events);
            MainNode["_waypoints"] = waypoints; // TODO: Add formal support
            /*
             * According to new the new BeatSaver schema, which will be enforced sometime soon™,
             * Bookmarks, Custom Events, and BPM Changes are now pushed to _customData instead of being on top level.
             * 
             * Private MM should already has this updated, however public MM will need a PR by someone, or maybe squeaksies if he
             * wants to go against his own words and go back to that.
             * 
             * Since these are editor only things, it's fine if I implement them now. Besides, CM reads both versions anyways.
             */
            if (!MainNode.HasKey("_customData") || MainNode["_customData"] is null ||
                !MainNode["_customData"].Children.Any())
            {
                MainNode["_customData"] = CustomData;
            }

            if (BpmChanges.Any())
                MainNode["_customData"]["_BPMChanges"] = CleanupArray(bpm);
            else
                MainNode["_customData"].Remove("_BPMChanges");

            if (Bookmarks.Any())
                MainNode["_customData"]["_bookmarks"] = CleanupArray(bookmarks);
            else
                MainNode["_customData"].Remove("_bookmarks");

            if (CustomEvents.Any())
                MainNode["_customData"]["_customEvents"] = CleanupArray(customEvents);
            else
                MainNode["_customData"].Remove("_customEvents");

            if (EnvEnhancements.Any())
                MainNode["_customData"]["_environment"] = envEnhancements;
            else
                MainNode["_customData"].Remove("_environment");
            if (Time > 0) MainNode["_customData"]["_time"] = Math.Round(Time, 3);
            BeatSaberSong.CleanObject(MainNode["_customData"]);
            if (!MainNode["_customData"].Children.Any()) MainNode.Remove("_customData");

            // I *believe* this automatically creates the file if it doesn't exist. Needs more experiementation
            if (Settings.Instance.AdvancedShit)
                File.WriteAllText(DirectoryAndFile, MainNode.ToString(2));
            else
                File.WriteAllText(DirectoryAndFile, MainNode.ToString());
            /*using (StreamWriter writer = new StreamWriter(directoryAndFile, false))
            {
                //Advanced users might want human readable JSON to perform easy modifications and reload them on the fly.
                //Thus, ChroMapper "beautifies" the JSON if you are in advanced mode.
                if (Settings.Instance.AdvancedShit)
                    writer.Write(mainNode.ToString(2));
                else writer.Write(mainNode.ToString());
            }*/

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError(
                "This is bad. You are recommendend to restart ChroMapper; progress made after this point is not garaunteed to be saved.");
            return false;
        }
    }

    // Cleans an array by filtering out null elements, or objects with invalid time.
    // Could definitely be optimized a little bit, but since saving is done on a separate thread, I'm not too worried about it.
    private static JSONArray CleanupArray(JSONArray original)
    {
        var array = original.Clone().AsArray;
        foreach (JSONNode node in original)
        {
            if (node is null || node["_time"].IsNull || float.IsNaN(node["_time"]))
                array.Remove(node);
        }

        return array;
    }

    public static BeatSaberMap GetBeatSaberMapFromJson(JSONNode mainNode, string directoryAndFile)
    {
        try
        {
            var map = new BeatSaberMap { MainNode = mainNode, DirectoryAndFile = directoryAndFile };

            var eventsList = new List<MapEvent>();
            var notesList = new List<BeatmapNote>();
            var obstaclesList = new List<BeatmapObstacle>();
            var waypointsList = new List<JSONNode>(); // TODO: Add formal support
            var bpmList = new List<BeatmapBPMChange>();
            var bookmarksList = new List<BeatmapBookmark>();
            var customEventsList = new List<BeatmapCustomEvent>();
            var envEnhancementsList = new List<EnvEnhancement>();

            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "_version":
                        map.Version = node.Value;
                        break;

                    case "_events":
                        foreach (JSONNode n in node) eventsList.Add(new MapEvent(n));
                        break;
                    case "_notes":
                        foreach (JSONNode n in node) notesList.Add(new BeatmapNote(n));
                        break;
                    case "_obstacles":
                        foreach (JSONNode n in node) obstaclesList.Add(new BeatmapObstacle(n));
                        break;
                    case "_waypoints":
                        foreach (JSONNode n in node) waypointsList.Add(n); // TODO: Add formal support
                        break;
                    case "_customData":
                        map.CustomData = node;

                        var dataNodeEnum = node.GetEnumerator();
                        while (dataNodeEnum.MoveNext())
                        {
                            var dataKey = dataNodeEnum.Current.Key;
                            var dataNode = dataNodeEnum.Current.Value;
                            switch (dataKey)
                            {
                                case "_BPMChanges":
                                    foreach (JSONNode n in dataNode) bpmList.Add(new BeatmapBPMChange(n));
                                    break;
                                case "_bpmChanges":
                                    foreach (JSONNode n in dataNode) bpmList.Add(new BeatmapBPMChange(n));
                                    break;
                                case "_bookmarks":
                                    foreach (JSONNode n in dataNode) bookmarksList.Add(new BeatmapBookmark(n));
                                    break;
                                case "_customEvents":
                                    foreach (JSONNode n in dataNode) customEventsList.Add(new BeatmapCustomEvent(n));
                                    break;
                                case "_time":
                                    map.Time = dataNode.AsFloat;
                                    break;
                                case "_environment":
                                    foreach (JSONNode n in dataNode) envEnhancementsList.Add(new EnvEnhancement(n));
                                    break;
                            }
                        }

                        break;
                    case "_BPMChanges":
                        foreach (JSONNode n in node) bpmList.Add(new BeatmapBPMChange(n));
                        break;
                    case "_bpmChanges":
                        foreach (JSONNode n in node) bpmList.Add(new BeatmapBPMChange(n));
                        break;
                    case "_bookmarks":
                        foreach (JSONNode n in node) bookmarksList.Add(new BeatmapBookmark(n));
                        break;
                    case "_customEvents":
                        foreach (JSONNode n in node) customEventsList.Add(new BeatmapCustomEvent(n));
                        break;
                }
            }

            map.Events = eventsList;
            map.Notes = notesList;
            map.Obstacles = obstaclesList;
            map.Waypoints = waypointsList; // TODO: Add formal support
            map.BpmChanges = bpmList.DistinctBy(x => x.Time).ToList();
            map.Bookmarks = bookmarksList;
            map.CustomEvents = customEventsList.DistinctBy(x => x.ConvertToJson().ToString()).ToList();
            map.EnvEnhancements = envEnhancementsList;
            return map;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }
}
