using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class BeatSaberMap {

    public JSONNode mainNode;
    public string directoryAndFile;

    public string _version = "2.0.0";
    public List<MapEvent> _events = new List<MapEvent>();
    public List<BeatmapNote> _notes = new List<BeatmapNote>();
    public List<BeatmapObstacle> _obstacles = new List<BeatmapObstacle>();
    public List<BeatmapBPMChange> _BPMChanges = new List<BeatmapBPMChange>();
    public List<BeatmapBookmark> _bookmarks = new List<BeatmapBookmark>();

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

            mainNode["_version"] = _version;

            JSONArray events = new JSONArray();
            foreach (MapEvent e in _events)
                events.Add(e.ConvertToJSON());

            JSONArray notes = new JSONArray();
            foreach (BeatmapNote n in _notes)notes.Add(n.ConvertToJSON());

            JSONArray obstacles = new JSONArray();
            foreach (BeatmapObstacle o in _obstacles) obstacles.Add(o.ConvertToJSON());

            JSONArray bpm = new JSONArray();
            foreach (BeatmapBPMChange b in _BPMChanges) bpm.Add(b.ConvertToJSON());

            JSONArray bookmarks = new JSONArray();
            foreach (BeatmapBookmark b in _bookmarks) bookmarks.Add(b.ConvertToJSON());

            mainNode["_notes"] = notes;
            mainNode["_obstacles"] = obstacles;
            mainNode["_events"] = events;
            mainNode["_BPMChanges"] = bpm;
            mainNode["_bookmarks"] = bookmarks;

            using (StreamWriter writer = new StreamWriter(directoryAndFile, false))
                writer.Write(mainNode.ToString());

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
                    case "_BPMChanges":
                        foreach (JSONNode n in node) bpmList.Add(new BeatmapBPMChange(n));
                        break;
                    case "_bookmarks":
                        foreach (JSONNode n in node) bookmarksList.Add(new BeatmapBookmark(n));
                        break;
                }
            }

            map._events = eventsList;
            map._notes = notesList;
            map._obstacles = obstaclesList;
            map._BPMChanges = bpmList;
            map._bookmarks = bookmarksList;
            return map;

        } catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }

}
