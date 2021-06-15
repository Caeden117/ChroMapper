using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class BeatSaberMap {

    public JSONNode mainNode;
    public string directoryAndFile;

    public string _version = "2.2.0";
    /// <summary>
    /// Time (in Minutes) that the user has worked on this map.
    /// </summary>
    public float _time = 0;
    public List<MapEvent> _events = new List<MapEvent>();
    public List<BeatmapNote> _notes = new List<BeatmapNote>();
    public List<BeatmapObstacle> _obstacles = new List<BeatmapObstacle>();
    public List<JSONNode> _waypoints = new List<JSONNode>(); // TODO: Add formal support
    public List<BeatmapBPMChange> _BPMChanges = new List<BeatmapBPMChange>();
    public List<BeatmapBookmark> _bookmarks = new List<BeatmapBookmark>();
    public List<BeatmapCustomEvent> _customEvents = new List<BeatmapCustomEvent>();
    public List<EnvEnhancement> _envEnhancements = new List<EnvEnhancement>();

    public bool Save() {

        try {
            /*
             * LISTS
             */

            //Just in case, I'm moving this up here
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (mainNode is null) mainNode = new JSONObject();

            mainNode["_version"] = _version;

            JSONArray events = new JSONArray();
            foreach (MapEvent e in _events) events.Add(e.ConvertToJSON());

            JSONArray notes = new JSONArray();
            foreach (BeatmapNote n in _notes) notes.Add(n.ConvertToJSON());

            JSONArray obstacles = new JSONArray();
            foreach (BeatmapObstacle o in _obstacles) obstacles.Add(o.ConvertToJSON());

            JSONArray bpm = new JSONArray();
            foreach (BeatmapBPMChange b in _BPMChanges) bpm.Add(b.ConvertToJSON());

            JSONArray bookmarks = new JSONArray();
            foreach (BeatmapBookmark b in _bookmarks) bookmarks.Add(b.ConvertToJSON());

            JSONArray customEvents = new JSONArray();
            foreach (BeatmapCustomEvent c in _customEvents) customEvents.Add(c.ConvertToJSON());

            JSONArray waypoints = new JSONArray(); // TODO: Add formal support
            foreach (JSONNode w in _waypoints) waypoints.Add(w);

            JSONArray envEnhancements = new JSONArray();
            foreach (EnvEnhancement e in _envEnhancements) envEnhancements.Add(e.ConvertToJson());

            mainNode["_notes"] = CleanupArray(notes);
            mainNode["_obstacles"] = CleanupArray(obstacles);
            mainNode["_events"] = CleanupArray(events);
            mainNode["_waypoints"] = waypoints; // TODO: Add formal support
            /*
             * According to new the new BeatSaver schema, which will be enforced sometime soon™,
             * Bookmarks, Custom Events, and BPM Changes are now pushed to _customData instead of being on top level.
             * 
             * Private MM should already has this updated, however public MM will need a PR by someone, or maybe squeaksies if he
             * wants to go against his own words and go back to that.
             * 
             * Since these are editor only things, it's fine if I implement them now. Besides, CM reads both versions anyways.
             */
            if (!mainNode.HasKey("_customData") || mainNode["_customData"] is null || !mainNode["_customData"].Children.Any()) mainNode["_customData"] = new JSONObject();
            if (_BPMChanges.Any())
            {
                mainNode["_customData"]["_BPMChanges"] = CleanupArray(bpm);
            }
            else
            {
                mainNode["_customData"].Remove("_BPMChanges");
            }

            if (_bookmarks.Any())
            {
                mainNode["_customData"]["_bookmarks"] = CleanupArray(bookmarks);
            }
            else
            {
                mainNode["_customData"].Remove("_bookmarks");
            }

            if (_customEvents.Any())
            {
                mainNode["_customData"]["_customEvents"] = CleanupArray(customEvents);
            }
            else
            {
                mainNode["_customData"].Remove("_customEvents");
            }

            if (_envEnhancements.Any())
            {
                mainNode["_customData"]["_environment"] = envEnhancements;
            }
            else
            {
                mainNode["_customData"].Remove("_environment");
            }
            if (_time > 0) mainNode["_customData"]["_time"] = Math.Round(_time, 3);
            BeatSaberSong.CleanObject(mainNode["_customData"]);
            if (!mainNode["_customData"].Children.Any())
            {
                mainNode.Remove("_customData");
            }

            // I *believe* this automatically creates the file if it doesn't exist. Needs more experiementation
            if (Settings.Instance.AdvancedShit)
            {
                File.WriteAllText(directoryAndFile, mainNode.ToString(2));
            }
            else
            { 
                File.WriteAllText(directoryAndFile, mainNode.ToString());
            }
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
            Debug.LogError("This is bad. You are recommendend to restart ChroMapper; progress made after this point is not garaunteed to be saved.");
            return false;
        }

    }

    // Cleans an array by filtering out null elements, or objects with invalid time.
    // Could definitely be optimized a little bit, but since saving is done on a separate thread, I'm not too worried about it.
    private static JSONArray CleanupArray(JSONArray original) 
    {
        JSONArray array = original.Clone().AsArray;
        foreach (JSONNode node in original)
        {
            if (node is null || node["_time"].IsNull || float.IsNaN(node["_time"])) array.Remove(node);
        }
        return array;
    }

    public static BeatSaberMap GetBeatSaberMapFromJSON(JSONNode mainNode, string directoryAndFile) {

        try {

            BeatSaberMap map = new BeatSaberMap();
            map.mainNode = mainNode;

            map.directoryAndFile = directoryAndFile;

            List<MapEvent> eventsList = new List<MapEvent>();
            List<BeatmapNote> notesList = new List<BeatmapNote>();
            List<BeatmapObstacle> obstaclesList = new List<BeatmapObstacle>();
            List<JSONNode> waypointsList = new List<JSONNode>(); // TODO: Add formal support
            List<BeatmapBPMChange> bpmList = new List<BeatmapBPMChange>();
            List<BeatmapBookmark> bookmarksList = new List<BeatmapBookmark>();
            List<BeatmapCustomEvent> customEventsList = new List<BeatmapCustomEvent>();
            List<EnvEnhancement> envEnhancementsList = new List<EnvEnhancement>();

            JSONNode.Enumerator nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext()) {
                string key = nodeEnum.Current.Key;
                JSONNode node = nodeEnum.Current.Value;

                switch (key) {
                    case "_version": map._version = node.Value; break;

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
                        JSONNode.Enumerator dataNodeEnum = node.GetEnumerator();
                        while (dataNodeEnum.MoveNext())
                        {
                            string dataKey = dataNodeEnum.Current.Key;
                            JSONNode dataNode = dataNodeEnum.Current.Value;
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
                                    map._time = dataNode.AsFloat;
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

            map._events = eventsList;
            map._notes = notesList;
            map._obstacles = obstaclesList;
            map._waypoints = waypointsList; // TODO: Add formal support
            map._BPMChanges = bpmList.DistinctBy(x => x._time).ToList();
            map._bookmarks = bookmarksList;
            map._customEvents = customEventsList.DistinctBy(x => x.ConvertToJSON().ToString()).ToList();
            map._envEnhancements = envEnhancementsList;
            return map;

        } catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }

}
