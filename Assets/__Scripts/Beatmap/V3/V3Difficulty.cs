using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3Difficulty : BaseDifficulty
    {
        public V3Difficulty()
        {
        }

        public override string Version { get; } = "3.1.0";

        public override bool IsChroma() =>
            Notes.Any(x => x.IsChroma()) || Bombs.Any(x => x.IsChroma()) || Arcs.Any(x => x.IsChroma()) ||
            Chains.Any(x => x.IsChroma()) || Obstacles.Any(x => x.IsChroma()) ||
            Events.Any(x => x.IsChroma()) || EnvironmentEnhancements.Any();


        public override bool IsNoodleExtensions() =>
            Notes.Any(x => x.IsNoodleExtensions()) || Bombs.Any(x => x.IsNoodleExtensions()) ||
            Arcs.Any(x => x.IsNoodleExtensions()) || Chains.Any(x => x.IsNoodleExtensions()) ||
            Obstacles.Any(x => x.IsNoodleExtensions()) || CustomEvents.Any();


        public override bool IsMappingExtensions() =>
            Notes.Any(x => x.IsMappingExtensions()) || Bombs.Any(x => x.IsMappingExtensions()) ||
            Arcs.Any(x => x.IsMappingExtensions()) || Chains.Any(x => x.IsMappingExtensions()) ||
            Obstacles.Any(x => x.IsMappingExtensions());

        public override bool Save()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                if (MainNode == null) MainNode = new JSONObject();

                MainNode["version"] = Version;

                var rotationEvents = new JSONArray();
                foreach (var r in RotationEvents) rotationEvents.Add(r.ToJson());

                var bpmEvents = new JSONArray();
                foreach (var b in BpmEvents) bpmEvents.Add(b.ToJson());

                var colorNotes = new JSONArray();
                foreach (var n in Notes) colorNotes.Add(n.ToJson());

                var bombNotes = new JSONArray();
                foreach (var b in Bombs) bombNotes.Add(b.ToJson());

                var obstacles = new JSONArray();
                foreach (var o in Obstacles) obstacles.Add(o.ToJson());

                var arcs = new JSONArray();
                foreach (var a in Arcs) arcs.Add(a.ToJson());

                var chains = new JSONArray();
                foreach (var c in Chains) chains.Add(c.ToJson());

                var waypoints = new JSONArray();
                foreach (var w in Waypoints) waypoints.Add(w.ToJson());

                var events = new JSONArray();
                foreach (var e in Events) events.Add(e.ToJson());

                var colorBoostEvents = new JSONArray();
                foreach (var e in ColorBoostEvents) colorBoostEvents.Add(e.ToJson());

                var lightColorEventBoxGroups = new JSONArray();
                foreach (var e in LightColorEventBoxGroups) lightColorEventBoxGroups.Add(e.ToJson());

                var lightRotationEventBoxGroups = new JSONArray();
                foreach (var e in LightRotationEventBoxGroups) lightRotationEventBoxGroups.Add(e.ToJson());

                MainNode["bpmEvents"] = CleanupArray(bpmEvents, "b");
                MainNode["rotationEvents"] = CleanupArray(rotationEvents, "b");
                MainNode["colorNotes"] = CleanupArray(colorNotes, "b");
                MainNode["bombNotes"] = CleanupArray(bombNotes, "b");
                MainNode["obstacles"] = CleanupArray(obstacles, "b");
                MainNode["sliders"] = CleanupArray(arcs, "b");
                MainNode["burstSliders"] = CleanupArray(chains, "b");
                MainNode["waypoints"] = CleanupArray(waypoints, "b");
                MainNode["basicBeatmapEvents"] = CleanupArray(events, "b");
                MainNode["colorBoostBeatmapEvents"] = CleanupArray(colorBoostEvents, "b");
                MainNode["lightColorEventBoxGroups"] = CleanupArray(lightColorEventBoxGroups, "b");
                MainNode["lightRotationEventBoxGroups"] = CleanupArray(lightRotationEventBoxGroups, "b");
                MainNode["basicEventTypesWithKeywords"] = EventTypesWithKeywords.ToJson();
                MainNode["useNormalEventsAsCompatibleEvents"] = UseNormalEventsAsCompatibleEvents;

                SaveCustomDataNode();

                // I *believe* this automatically creates the file if it doesn't exist. Needs more experimentation
                File.WriteAllText(DirectoryAndFile,
                    Settings.Instance.FormatJson ? MainNode.ToString(2) : MainNode.ToString());
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
                    "This is bad. You are recommended to restart ChroMapper; progress made after this point is not guaranteed to be saved.");
                return false;
            }
        }

        public override int GetVersion() => 3;

        protected override bool SaveCustomDataNode()
        {
            var bpm = new JSONArray();
            foreach (var b in BpmChanges) bpm.Add(b.ToJson());

            var bookmarks = new JSONArray();
            foreach (var b in Bookmarks) bookmarks.Add(b.ToJson());

            var customEvents = new JSONArray();
            foreach (var c in CustomEvents) customEvents.Add(c.ToJson());

            var envEnhancements = new JSONArray();
            foreach (var e in EnvironmentEnhancements) envEnhancements.Add(e.ToJson());

            if (!MainNode.HasKey("customData") || MainNode["customData"] is null ||
                !MainNode["customData"].Children.Any())
                MainNode["customData"] = CustomData;

            if (BpmChanges.Any())
                MainNode["customData"]["BPMChanges"] = CleanupArray(bpm, "b");
            else
                MainNode["customData"].Remove("BPMChanges");

            if (Bookmarks.Any())
                MainNode["customData"]["bookmarks"] = CleanupArray(bookmarks, "b");
            else
                MainNode["customData"].Remove("bookmarks");

            if (CustomEvents.Any())
                MainNode["customData"]["customEvents"] = CleanupArray(customEvents, "b");
            else
                MainNode["customData"].Remove("customEvents");

            if (EnvironmentEnhancements.Any())
                MainNode["customData"]["environment"] = envEnhancements;
            else
                MainNode["customData"].Remove("environment");
            if (Time > 0) MainNode["customData"]["time"] = Math.Round(Time, 3);
            BeatSaberSong.CleanObject(MainNode["customData"]);
            if (!MainNode["customData"].Children.Any()) MainNode.Remove("customData");

            return true;
        }

        public override JSONNode ToJson() => throw new NotImplementedException();

        public override BaseItem Clone() => throw new NotSupportedException();

        public static V3Difficulty GetFromJson(JSONNode mainNode, string path)
        {
            try
            {
                var map = new V3Difficulty
                {
                    MainNode = mainNode,
                    DirectoryAndFile = path,
                    EventTypesWithKeywords = new V3BasicEventTypesWithKeywords() // apparently this is required
                };

                var nodeEnum = mainNode.GetEnumerator();
                while (nodeEnum.MoveNext())
                {
                    var key = nodeEnum.Current.Key;
                    var node = nodeEnum.Current.Value;

                    switch (key)
                    {
                        case "bpmEvents":
                            foreach (JSONNode n in node) map.BpmEvents.Add(new V3BpmEvent(n));
                            break;
                        case "rotationEvents":
                            foreach (JSONNode n in node) map.RotationEvents.Add(new V3RotationEvent(n));
                            break;
                        case "colorNotes":
                            foreach (JSONNode n in node) map.Notes.Add(new V3ColorNote(n));
                            break;
                        case "bombNotes":
                            foreach (JSONNode n in node) map.Bombs.Add(new V3BombNote(n));
                            break;
                        case "obstacles":
                            foreach (JSONNode n in node) map.Obstacles.Add(new V3Obstacle(n));
                            break;
                        case "sliders":
                            foreach (JSONNode n in node) map.Arcs.Add(new V3Arc(n));
                            break;
                        case "burstSliders":
                            foreach (JSONNode n in node) map.Chains.Add(new V3Chain(n));
                            break;
                        case "waypoints":
                            foreach (JSONNode n in node) map.Waypoints.Add(new V3Waypoint(n));
                            break;
                        case "basicBeatmapEvents":
                            foreach (JSONNode n in node) map.Events.Add(new V3BasicEvent(n));
                            break;
                        case "colorBoostBeatmapEvents":
                            foreach (JSONNode n in node) map.ColorBoostEvents.Add(new V3ColorBoostEvent(n));
                            break;
                        case "lightColorEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.LightColorEventBoxGroups.Add(new V3LightColorEventBoxGroup(n));
                            break;
                        case "lightRotationEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.LightRotationEventBoxGroups.Add(new V3LightRotationEventBoxGroup(n));
                            break;
                        case "basicEventTypesWithKeywords":
                            map.EventTypesWithKeywords = new V3BasicEventTypesWithKeywords(node);
                            break;
                        case "useNormalEventsAsCompatibleEvents":
                            map.UseNormalEventsAsCompatibleEvents = node.AsBool;
                            break;
                    }
                }

                LoadCustomDataNode(ref map, ref mainNode);

                return map;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static void LoadCustomDataNode(ref V3Difficulty map, ref JSONNode mainNode)
        {
            if (mainNode["customData"] == null) return;

            var bpmList = new List<BaseBpmChange>();
            var bookmarksList = new List<BaseBookmark>();
            var customEventsList = new List<BaseCustomEvent>();
            var envEnhancementsList = new List<BaseEnvironmentEnhancement>();

            var nodeEnum = mainNode["customData"].GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "BPMChanges":
                        foreach (JSONNode n in node) bpmList.Add(new V3BpmChange(n));
                        break;
                    case "bookmarks":
                        foreach (JSONNode n in node) bookmarksList.Add(new V3Bookmark(n));
                        break;
                    case "customEvents":
                        foreach (JSONNode n in node) customEventsList.Add(new V3CustomEvent(n));
                        break;
                    case "environment":
                        foreach (JSONNode n in node) envEnhancementsList.Add(new V3EnvironmentEnhancement(n));
                        break;
                    case "time":
                        map.Time = node.AsFloat;
                        break;
                }
            }

            map.BpmChanges = bpmList.DistinctBy(x => x.Time).ToList();
            map.Bookmarks = bookmarksList;
            map.CustomEvents = customEventsList.DistinctBy(x => x.ToString()).ToList();
            map.EnvironmentEnhancements = envEnhancementsList;
        }
    }
}
