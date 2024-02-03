using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Converters;
using Beatmap.Enums;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3Difficulty : BaseDifficulty
    {
        public override string Version { get; } = "3.3.0";

        public override string BookmarksUseOfficialBpmEventsKey { get; } = "bookmarksUseOfficialBpmEvents";

        public override bool IsChroma() =>
            Notes.Any(x => x.IsChroma()) || Bombs.Any(x => x.IsChroma()) || Arcs.Any(x => x.IsChroma()) ||
            Chains.Any(x => x.IsChroma()) || Obstacles.Any(x => x.IsChroma()) ||
            Events.Any(x => x.IsChroma()) || EnvironmentEnhancements.Any();


        public override bool IsNoodleExtensions() =>
            Notes.Any(x => x.IsNoodleExtensions()) || Bombs.Any(x => x.IsNoodleExtensions()) ||
            Arcs.Any(x => x.IsNoodleExtensions()) || Chains.Any(x => x.IsNoodleExtensions()) ||
            Obstacles.Any(x => x.IsNoodleExtensions());


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

                MainNode ??= new JSONObject();
                MainNode["version"] = Version;
                ParseV2ToV3();

                var rotationEvents = new JSONArray();
                foreach (var r in RotationEvents) rotationEvents.Add(r.ToJson());

                var bpmEvents = new JSONArray();
                if (BpmEvents.Count > 0 && BpmEvents.First().JsonTime != 0)
                {
                    BpmEvents.Insert(0, new V3BpmEvent()
                    {
                        JsonTime = 0,
                        Bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute
                    });
                }
                foreach (var b in BpmEvents) bpmEvents.Add(b.ToJson());

                var colorNotes = new JSONArray();
                foreach (var n in Notes) if (!n.CustomFake) colorNotes.Add(n.ToJson());

                var bombNotes = new JSONArray();
                foreach (var b in Bombs) if (!b.CustomFake)  bombNotes.Add(b.ToJson());

                var obstacles = new JSONArray();
                foreach (var o in Obstacles) if (!o.CustomFake) obstacles.Add(o.ToJson());

                var arcs = new JSONArray();
                foreach (var a in Arcs) arcs.Add(a.ToJson());

                var chains = new JSONArray();
                foreach (var c in Chains) if (!c.CustomFake) chains.Add(c.ToJson());

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

                var lightTranslationEventBoxGroups = new JSONArray();
                foreach (var e in LightTranslationEventBoxGroups) lightTranslationEventBoxGroups.Add(e.ToJson());

                var vfxEventBoxGroups = new JSONArray();
                foreach (var e in VfxEventBoxGroups) vfxEventBoxGroups.Add(e.ToJson());

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
                MainNode["lightTranslationEventBoxGroups"] = CleanupArray(lightTranslationEventBoxGroups, "b");
                MainNode["vfxEventBoxGroups"] = CleanupArray(vfxEventBoxGroups, "b");
                MainNode["_fxEventsCollection"] = FxEventsCollection?.ToJson() ?? new V3FxEventsCollection().ToJson();
                MainNode["basicEventTypesWithKeywords"] =
                    EventTypesWithKeywords?.ToJson() ?? new V3BasicEventTypesWithKeywords().ToJson();
                MainNode["useNormalEventsAsCompatibleEvents"] = UseNormalEventsAsCompatibleEvents;

                // Do this before adding customData
                if (Settings.Instance.SaveWithoutDefaultValues)
                {
                    SimpleJSONHelper.RemovePropertiesWithDefaultValues(MainNode);
                }

                SaveCustom();

                WriteDifficultyFile(this);
                WriteBPMInfoFile(this);

                // TODO: temporary fix, there is possibility better solution but this is quick band aid
                // we need to put them back into the map
                ParseV3ToV2(this);

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

            MainNode["customData"] = CustomData ?? new JSONObject();
            MainNode["customData"].Remove("BPMChanges");

            var fakeColorNotes = new JSONArray();
            foreach (var n in Notes) if (n.CustomFake) fakeColorNotes.Add(n.ToJson());
            if (fakeColorNotes.Count > 0)
                MainNode["customData"]["fakeColorNotes"] = CleanupArray(fakeColorNotes, "b");
            else
                MainNode["customData"].Remove("fakeColorNotes");

            var fakeBombNotes = new JSONArray();
            foreach (var b in Bombs) if (b.CustomFake)  fakeBombNotes.Add(b.ToJson());
            if (fakeBombNotes.Count > 0)
                MainNode["customData"]["fakeBombNotes"] = CleanupArray(fakeBombNotes, "b");
            else
                MainNode["customData"].Remove("fakeBombNotes");

            var fakeObstacles = new JSONArray();
            foreach (var o in Obstacles) if (o.CustomFake) fakeObstacles.Add(o.ToJson());
            if (fakeObstacles.Count > 0)
                MainNode["customData"]["fakeObstacles"] = CleanupArray(fakeObstacles, "b");
            else
                MainNode["customData"].Remove("fakeObstacles");

            var fakeChains = new JSONArray();
            foreach (var c in Chains) if (c.CustomFake) fakeChains.Add(c.ToJson());
            if (fakeChains.Count > 0)
                MainNode["customData"]["fakeChains"] = CleanupArray(fakeChains, "b");
            else
                MainNode["customData"].Remove("fakeChains");

            if (Bookmarks.Any())
                MainNode["customData"]["bookmarks"] = CleanupArray(bookmarks, "b");
            else
                MainNode["customData"].Remove("bookmarks");

            if (CustomEvents.Any())
                MainNode["customData"]["customEvents"] = CleanupArray(customEvents, "b");
            else
                MainNode["customData"].Remove("customEvents");

            if (PointDefinitions.Any())
            {
                MainNode["customData"]["pointDefinitions"] = new JSONObject();
                foreach (var p in PointDefinitions)
                {
                    MainNode["customData"]["pointDefinitions"][p.Key] = p.Value;
                }
            }
            else
            {
                MainNode["customData"].Remove("pointDefinitions");
            }

            if (EnvironmentEnhancements.Any())
                MainNode["customData"]["environment"] = envEnhancements;
            else
                MainNode["customData"].Remove("environment");

            if (Materials.Any())
            {
                MainNode["customData"]["materials"] = new JSONObject();
                foreach (var m in Materials) MainNode["customData"]["materials"][m.Key] = m.Value.ToJson();
            }
            else
            {
                MainNode["customData"].Remove("materials");
            }

            if (Time > 0) MainNode["customData"]["time"] = Math.Round(Time, 3);
            if (Bookmarks.Count > 0) MainNode["customData"][BookmarksUseOfficialBpmEventsKey] = true;

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
                var map = new V3Difficulty { MainNode = mainNode, DirectoryAndFile = path };

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
                        case "lightTranslationEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.LightTranslationEventBoxGroups.Add(new V3LightTranslationEventBoxGroup(n));
                            break;
                        case "vfxEventBoxGroups":
                            foreach (JSONNode n in node)
                                map.VfxEventBoxGroups.Add(new V3VfxEventEventBoxGroup(n));
                            break;
                        case "_fxEventsCollection":
                            map.FxEventsCollection = new V3FxEventsCollection(node);
                            break;
                        case "basicEventTypesWithKeywords":
                            map.EventTypesWithKeywords = new V3BasicEventTypesWithKeywords(node);
                            break;
                        case "useNormalEventsAsCompatibleEvents":
                            map.UseNormalEventsAsCompatibleEvents = node.AsBool;
                            break;
                    }
                }

                LoadCustom(map, mainNode);
                ParseV3ToV2(map);

                return map;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private void ParseV2ToV3()
        {
            // Note conversion
            for (var i = Notes.Count - 1; i >= 0; i--)
            {
                var note = Notes[i];

                // simple color note conversion
                if (note.Type != (int)NoteType.Bomb)
                {
                    Notes[i] = V2ToV3.ColorNote(note);
                    continue;
                }

                // Special bombs case
                Notes.RemoveAt(i);
                var bomb = V2ToV3.BombNote(note);
                Bombs.Add(bomb);
            }
            Bombs.Sort();

            // Obstacle conversion
            for (var i = 0; i < Obstacles.Count; i++)
            {
                Obstacles[i] = V2ToV3.Obstacle(Obstacles[i]);
            }

            // Event conversion
            for (var i = Events.Count - 1; i >= 0; i--)
            {
                var @event = Events[i];

                switch (@event.Type)
                {
                    case (int)EventTypeValue.ColorBoost:
                        Events.RemoveAt(i);
                        ColorBoostEvents.Add(V2ToV3.ColorBoostEvent(@event));
                        break;
                    case (int)EventTypeValue.EarlyLaneRotation:
                    case (int)EventTypeValue.LateLaneRotation:
                        Events.RemoveAt(i);
                        RotationEvents.Add(V2ToV3.RotationEvent(@event));
                        break;
                    default:
                        Events[i] = V2ToV3.BasicEvent(@event);
                        break;
                }
            }
            ColorBoostEvents.Sort();
            RotationEvents.Sort();

            // Bpm event conversion
            for (var i = 0; i < BpmEvents.Count; i++)
            {
                BpmEvents[i] = V2ToV3.BpmEvent(BpmEvents[i]);
            }

            // Bookmark conversion
            for (var i = 0; i < Bookmarks.Count; i++)
            {
                Bookmarks[i] = V2ToV3.Bookmark(Bookmarks[i]);
            }

            // BPM change conversion (?)
            for (var i = 0; i < BpmChanges.Count; i++)
            {
                BpmChanges[i] = V2ToV3.BpmChange(BpmChanges[i]);
            }

            // Custom event conversion
            for (var i = 0; i < CustomEvents.Count; i++)
            {
                CustomEvents[i] = V2ToV3.CustomEvent(CustomEvents[i]);
            }

            // Environment Enhancement conversion
            for (var i = 0; i < EnvironmentEnhancements.Count; i++)
            {
                EnvironmentEnhancements[i] = V2ToV3.EnvironmentEnhancement(EnvironmentEnhancements[i]);
            }
        }

        private static void ParseV3ToV2(V3Difficulty map)
        {
            map.Notes.AddRange(map.Bombs);
            map.Notes.Sort();

            map.Events.AddRange(map.ColorBoostEvents);
            map.Events.AddRange(map.RotationEvents);
            map.Events.Sort();
        }

        private static void LoadCustom(V3Difficulty map, JSONNode mainNode)
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
                        foreach (JSONNode n in node) bpmList.Add(new V3BpmChange(n));
                        break;
                    case "bookmarks":
                        foreach (JSONNode n in node) bookmarksList.Add(new V3Bookmark(n));
                        break;
                    case "customEvents":
                        foreach (JSONNode n in node) customEventsList.Add(new V3CustomEvent(n));
                        break;
                    case "fakeColorNotes":
                        foreach (JSONNode n in node) map.Notes.Add(new V3ColorNote(n, true));
                        break;
                    case "fakeBombNotes":
                        foreach (JSONNode n in node) map.Bombs.Add(new V3BombNote(n, true));
                        break;
                    case "fakeObstacles":
                        foreach (JSONNode n in node) map.Obstacles.Add(new V3Obstacle(n, true));
                        break;
                    case "fakeBurstSliders":
                        foreach (JSONNode n in node) map.Chains.Add(new V3Chain(n, true));
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
                        foreach (JSONNode n in node) envEnhancementsList.Add(new V3EnvironmentEnhancement(n));
                        break;
                    case "materials":
                        if (node is JSONObject matObj)
                        {
                            foreach (var n in matObj)
                                if (!materials.ContainsKey(n.Key))
                                    materials.Add(n.Key, new V3Material(n.Value.AsObject));
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
