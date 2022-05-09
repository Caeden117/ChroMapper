using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatSaberMapV3 : BeatSaberMap
{
    /// <summary>
    /// All Lists of type JSONNode are unsupported
    /// </summary>
    public List<JSONNode> BpmEvents = new List<JSONNode>(); // disable supprot for bpm change
    public List<RotationEvent> RotationEvents = new List<RotationEvent>();
    public List<BeatmapColorNote> ColorNotes = new List<BeatmapColorNote>();
    public List<BeatmapBombNote> BombNotes = new List<BeatmapBombNote>();
    public List<BeatmapObstacleV3> ObstaclesV3 = new List<BeatmapObstacleV3>();
    public List<BeatmapArc> Arcs = new List<BeatmapArc>();
    public List<BeatmapChain> Chains = new List<BeatmapChain>();
    public new List<JSONNode> Waypoints = new List<JSONNode>();
    public List<MapEventV3> BasicBeatmapEvents = new List<MapEventV3>();
    public List<ColorBoostEvent> ColorBoostBeatmapEvents = new List<ColorBoostEvent>();
    public List<JSONNode> LightColorEventBoxGroups = new List<JSONNode>();
    public List<JSONNode> LightRotationEventBoxGroups = new List<JSONNode>();
    public Dictionary<string, JSONNode> BasicEventTypesWithKeywords = new Dictionary<string, JSONNode>(); // although idk what it is used for, save as a dict first
    public bool UseNormalEventsAsCompatibleEvents = false;

    public const string BeatSaberMapV3CustomDatakey = "customData";
    public BeatSaberMapV3() { }
    public BeatSaberMapV3(BeatSaberMap other)
    {
        DirectoryAndFile = other.DirectoryAndFile;
        Time = other.Time;
        Events = other.Events;
        Notes = other.Notes;
        Obstacles = other.Obstacles;

        base.Waypoints = other.Waypoints;
        BpmChanges = other.BpmChanges;
        Bookmarks = other.Bookmarks;
        CustomEvents = other.CustomEvents;
        EnvEnhancements = other.EnvEnhancements;
        CustomData = other.CustomData;
        // do not ref mainnode, or it will exist duplicate data.
    }

    public override bool Save()
    {
        if (!Settings.Instance.Load_MapV3)
        {
            return base.Save();
        }
        try
        {
            /*
             * LISTS
             */

            //Just in case, I'm moving this up here
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (MainNode is null) MainNode = new JSONObject();

            MainNode["version"] = Version;
            ParseBaseNoteToV3();
            /// official nodes
            var bpmEvents = new JSONArray();
            foreach (var b in BpmEvents) bpmEvents.Add(b);

            var rotationEvents = new JSONArray();
            foreach (var r in RotationEvents) rotationEvents.Add(r.ConvertToJson());

            var colorNotes = new JSONArray();
            foreach (var n in ColorNotes) colorNotes.Add(n.ConvertToJson());

            var bombNotes = new JSONArray();
            foreach (var b in BombNotes) bombNotes.Add(b.ConvertToJson());

            var obstacles = new JSONArray();
            foreach (var o in ObstaclesV3) obstacles.Add(o.ConvertToJson());

            var arcs = new JSONArray();
            foreach (var s in Arcs) arcs.Add(s.ConvertToJson());

            var chains = new JSONArray();
            foreach (var c in Chains) chains.Add(c.ConvertToJson());

            var waypoints = new JSONArray(); // TODO: Add formal support
            foreach (var w in Waypoints) waypoints.Add(w);

            var basicBeatmapEvents = new JSONArray();
            foreach (var b in BasicBeatmapEvents) basicBeatmapEvents.Add(b.ConvertToJson());

            var colorBoostBeatmapEvents = new JSONArray();
            foreach (var c in ColorBoostBeatmapEvents) colorBoostBeatmapEvents.Add(c.ConvertToJson());

            var lightColorEventBoxGroups = new JSONArray();
            foreach (var l in LightColorEventBoxGroups) lightColorEventBoxGroups.Add(l);

            var lightRotationEventBoxGroups = new JSONArray();
            foreach (var l in lightRotationEventBoxGroups) lightRotationEventBoxGroups.Add(l);

            var basicEventTypesWithKeywords = new JSONObject();
            foreach (var k in BasicEventTypesWithKeywords.Keys) basicEventTypesWithKeywords[k] = BasicEventTypesWithKeywords[k];

            MainNode["bpmEvents"] = bpmEvents;
            MainNode["rotationEvents"] = rotationEvents;
            MainNode["colorNotes"] = CleanupArray(colorNotes, "b");
            MainNode["bombNotes"] = CleanupArray(bombNotes, "b");
            MainNode["obstacles"] = CleanupArray(obstacles, "b");
            MainNode["sliders"] = CleanupArray(arcs, "b");
            MainNode["burstSliders"] = CleanupArray(chains, "b");
            MainNode["waypoints"] = waypoints;
            MainNode["basicBeatmapEvents"] = CleanupArray(basicBeatmapEvents, "b");
            MainNode["colorBoostBeatmapEvents"] = CleanupArray(colorBoostBeatmapEvents, "b");
            MainNode["lightColorEventBoxGroups"] = lightColorEventBoxGroups;
            MainNode["lightRotationEventBoxGroups"] = lightRotationEventBoxGroups;
            MainNode["basicEventTypesWithKeywords"] = basicEventTypesWithKeywords;
            MainNode["useNormalEventsAsCompatibleEvents"] = UseNormalEventsAsCompatibleEvents;

            SaveCustomDataNode(BeatSaberMapV3CustomDatakey);

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


    public static new BeatSaberMapV3 GetBeatSaberMapFromJson(JSONNode mainNode, string directoryAndFile)
    {
        try
        {
            var mapV3 = new BeatSaberMapV3 { MainNode = mainNode, DirectoryAndFile = directoryAndFile };

            var eventsList = new List<MapEvent>();
            var bpmEventsList = new List<JSONNode>();
            var rotationEventsList = new List<RotationEvent>();
            var colorNotesList = new List<BeatmapColorNote>();
            var bombNotesList = new List<BeatmapBombNote>();
            var arcsList = new List<BeatmapArc>();
            var obstaclesList = new List<BeatmapObstacleV3>();
            var chainsList = new List<BeatmapChain>();
            var waypointsList = new List<JSONNode>();
            var basicBeatmapEventsList = new List<MapEventV3>();
            var colorBoostBeatmapEventsList = new List<ColorBoostEvent>();
            var lightColorEventBoxGroupsList = new List<JSONNode>();
            var lightRotationEventBoxGroupsList = new List<JSONNode>();
            var basicEventTypesWithKeywordsDict = new Dictionary<string, JSONNode>();


            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "version":
                        mapV3.Version = node.Value;
                        break;
                    case "bpmEvents":
                        foreach (JSONNode n in node) bpmEventsList.Add(n);
                        break;
                    case "rotationEvents":
                        foreach (JSONNode n in node) rotationEventsList.Add(new RotationEvent(n));
                        break;
                    case "colorNotes":
                        foreach (JSONNode n in node) colorNotesList.Add(new BeatmapColorNote(n));
                        break;
                    case "bombNotes":
                        foreach (JSONNode n in node) bombNotesList.Add(new BeatmapBombNote(n));
                        break;
                    case "obstacles":
                        foreach (JSONNode n in node) obstaclesList.Add(new BeatmapObstacleV3(n));
                        break;
                    case "sliders":
                        foreach (JSONNode n in node) arcsList.Add(new BeatmapArc(n));
                        break;
                    case "burstSliders":
                        foreach (JSONNode n in node) chainsList.Add(new BeatmapChain(n));
                        break;
                    case "waypoints":
                        foreach (JSONNode n in node) waypointsList.Add(n); // TODO: Add formal support
                        break;
                    case "basicBeatmapEvents":
                        foreach (JSONNode n in node) basicBeatmapEventsList.Add(new MapEventV3(n));
                        break;
                    case "colorBoostBeatmapEvents":
                        foreach (JSONNode n in node) colorBoostBeatmapEventsList.Add(new ColorBoostEvent(n));
                        break;
                    case "lightColorEventBoxGroups":
                        foreach (JSONNode n in node) lightColorEventBoxGroupsList.Add(n);
                        break;
                    case "lightRotationEventBoxGroups":
                        foreach (JSONNode n in node) lightRotationEventBoxGroupsList.Add(n);
                        break;
                    case "basicEventTypesWithKeywords":
                        foreach (var k in node.Keys)
                        {
                            basicEventTypesWithKeywordsDict[k] = node[k];
                        }
                        break;
                    case "useNormalEventsAsCompatibleEvents":
                        mapV3.UseNormalEventsAsCompatibleEvents = node.AsBool;
                        break;
                    default:
                        break;
                }
            }


            mapV3.BpmEvents = bpmEventsList;
            mapV3.RotationEvents = rotationEventsList;
            mapV3.ColorNotes = colorNotesList;
            mapV3.BombNotes = bombNotesList;
            mapV3.ObstaclesV3 = obstaclesList;
            mapV3.Waypoints = waypointsList; // TODO: Add formal support
            mapV3.Arcs = arcsList;
            mapV3.Chains = chainsList;
            mapV3.BasicBeatmapEvents = basicBeatmapEventsList;
            mapV3.ColorBoostBeatmapEvents = colorBoostBeatmapEventsList;
            mapV3.LightColorEventBoxGroups = lightColorEventBoxGroupsList;
            mapV3.LightRotationEventBoxGroups = lightRotationEventBoxGroupsList;
            mapV3.BasicEventTypesWithKeywords = basicEventTypesWithKeywordsDict;

            var mapV2 = mapV3 as BeatSaberMap;
            LoadCustomDataNode(ref mapV2, ref mainNode);
            mapV3.ParseNoteV3ToBase();
            return mapV3;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }

    /// <summary>
    /// Parse all compatible <see cref="BeatmapObject"/> to v3 children classes. It is only used when saving.
    /// </summary>
    public void ParseBaseNoteToV3()
    {
        ColorNotes.Clear();
        BombNotes.Clear();
        foreach (var note in Notes)
        {
            if (note is BeatmapColorNote colorNote) ColorNotes.Add(colorNote);
            else if (note is BeatmapBombNote bombNote) BombNotes.Add(bombNote);
            else
            {
                switch (note.Type)
                {
                    case BeatmapNote.NoteTypeBomb:
                        BombNotes.Add(new BeatmapBombNote(note));
                        break;
                    case BeatmapNote.NoteTypeA:
                    case BeatmapNote.NoteTypeB:
                        ColorNotes.Add(new BeatmapColorNote(note));
                        break;
                    default:
                        Debug.LogError("Unsupported note type for Beatmap version 3.0.0");
                        break;
                }
            }
        }

        ObstaclesV3.Clear();
        foreach (var o in Obstacles) ObstaclesV3.Add(new BeatmapObstacleV3(o));

        BasicBeatmapEvents.Clear();
        ColorBoostBeatmapEvents.Clear();
        RotationEvents.Clear();
        foreach (var e in Events)
        {
            switch (e.Type)
            {
                case MapEvent.EventTypeBoostLights:
                    ColorBoostBeatmapEvents.Add(new ColorBoostEvent(e));
                    break;
                case MapEvent.EventTypeEarlyRotation:
                case MapEvent.EventTypeLateRotation:
                    RotationEvents.Add(new RotationEvent(e));
                    break;
                default:
                    BasicBeatmapEvents.Add(new MapEventV3(e));
                    break;
            }

        }
    }

    /// <summary>
    /// Parse all compatible <see cref="BeatmapObject"/> back to map v2 format.
    /// Since all the previous fucntion calls are on v2 objects, reusing them would be more conveinient.
    /// </summary>
    public void ParseNoteV3ToBase()
    {
        Notes = ColorNotes.OfType<BeatmapNote>().ToList();
        Notes.AddRange(BombNotes.OfType<BeatmapNote>().ToList());

        Obstacles = ObstaclesV3.OfType<BeatmapObstacle>().ToList();
        Events = BasicBeatmapEvents.OfType<MapEvent>().ToList();
        Events.AddRange(ColorBoostBeatmapEvents.OfType<MapEvent>().ToList());
        Events.AddRange(RotationEvents.OfType<MapEvent>().ToList());
        Events.Sort((lhs, rhs) => { return lhs.Time.CompareTo(rhs.Time); });
    }
}
