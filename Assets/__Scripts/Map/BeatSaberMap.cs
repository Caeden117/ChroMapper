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

    public string _version = "2.0.0";
    /// <summary>
    /// Time (in Minutes) that the user has worked on this map.
    /// </summary>
    public float _time = 0;
    public List<MapEvent> _events = new List<MapEvent>();
    public List<BeatmapNote> _notes = new List<BeatmapNote>();
    public List<BeatmapObstacle> _obstacles = new List<BeatmapObstacle>();
    public List<BeatmapBPMChange> _BPMChanges = new List<BeatmapBPMChange>();
    public List<BeatmapBookmark> _bookmarks = new List<BeatmapBookmark>();
    public List<BeatmapCustomEvent> _customEvents = new List<BeatmapCustomEvent>();

    public bool Save() {

        try {
            /*
             * LISTS
             */

            //Just in case, I'm moving this up here
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            _events = _events.OrderBy(x => x._time).ToList();
            _notes = _notes.OrderBy(x => x._time).ToList();
            _obstacles = _obstacles.OrderBy(x => x._time).ToList();
            _BPMChanges = _BPMChanges.OrderBy(x => x._time).ToList();
            _bookmarks = _bookmarks.OrderBy(x => x._time).ToList();
            _customEvents = _customEvents.OrderBy(x => x._time).ThenBy(x => x._type).ToList();

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

            mainNode["_notes"] = notes;
            mainNode["_obstacles"] = obstacles;
            mainNode["_events"] = events;
            /*
             * According to new the new BeatSaver schema, which will be enforced sometime soon™,
             * Bookmarks, Custom Events, and BPM Changes are now pushed to _customData instead of being on top level.
             * 
             * Private MM should already has this updated, however public MM will need a PR by someone, or maybe squeaksies if he
             * wants to go against his own words and go back to that.
             * 
             * Since these are editor only things, it's fine if I implement them now. Besides, CM reads both versions anyways.
             */ 
            if (_BPMChanges.Any()) mainNode["_customData"]["_BPMChanges"] = bpm;
            if (_bookmarks.Any()) mainNode["_customData"]["_bookmarks"] = bookmarks;
            if (_customEvents.Any()) mainNode["_customEvents"] = customEvents;
            if (_time > 0) mainNode["_customData"]["_time"] = Math.Round(_time, 3);

            using (StreamWriter writer = new StreamWriter(directoryAndFile, false))
            {
                //Advanced users might want human readable JSON to perform easy modifications and reload them on the fly.
                //Thus, ChroMapper "beautifies" the JSON if you are in advanced mode.
                if (Settings.Instance.AdvancedShit)
                    writer.Write(mainNode.ToString(2));
                else writer.Write(mainNode.ToString());
            }

            return true;
        } catch (Exception e) {
            Debug.LogException(e);
            return false;
        }

    }


    public static BeatSaberMap GetBeatSaberMapFromJSON(JSONNode mainNode, string directoryAndFile) {

        try {

            BeatSaberMap map = new BeatSaberMap();
            map.mainNode = mainNode;

            map.directoryAndFile = directoryAndFile;

            List<MapEvent> eventsList = new List<MapEvent>();
            List<BeatmapNote> notesList = new List<BeatmapNote>();
            List<BeatmapObstacle> obstaclesList = new List<BeatmapObstacle>();
            List<BeatmapBPMChange> bpmList = new List<BeatmapBPMChange>();
            List<BeatmapBookmark> bookmarksList = new List<BeatmapBookmark>();
            List<BeatmapCustomEvent> customEventsList = new List<BeatmapCustomEvent>();

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
            map._BPMChanges = bpmList.DistinctBy(x => x.ConvertToJSON().ToString()).ToList();
            map._bookmarks = bookmarksList;
            map._customEvents = customEventsList.DistinctBy(x => x.ConvertToJSON().ToString()).ToList();
            return map;

        } catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }

}
