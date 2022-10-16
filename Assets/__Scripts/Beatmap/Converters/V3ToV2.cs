using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V2;
using Beatmap.V2.Customs;
using Beatmap.V3;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Converters
{
    public static class V3ToV2
    {
        public static V2Note Note(BaseNote other) =>
            other switch
            {
                V3ColorNote o => new V2Note(o) { CustomData = CustomDataObject(o.CustomData) },
                V3BombNote o => new V2Note(o) { CustomData = CustomDataObject(o.CustomData) },
                V2Note o => new V2Note(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 color note to v2 note")
            };

        public static V2Event Event(BaseEvent other) =>
            other switch
            {
                V3BasicEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V3ColorBoostEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V3RotationEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V3BpmEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V2Event o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 basic event to v2 event")
            };

        public static V2SpecialEventsKeywordFilters EventTypesWithKeywords(BaseEventTypesWithKeywords other) =>
            other switch
            {
                V3BasicEventTypesWithKeywords o => new V2SpecialEventsKeywordFilters(o),
                V2SpecialEventsKeywordFilters o => o,
                _ => throw new ArgumentException("Unexpected object to convert")
            };

        public static V2SpecialEventsKeywordFiltersKeywords EventTypesForKeywords(BaseEventTypesForKeywords other) =>
            other switch
            {
                V3BasicEventTypesForKeywords o => new V2SpecialEventsKeywordFiltersKeywords(o),
                V2SpecialEventsKeywordFiltersKeywords o => o,
                _ => throw new ArgumentException("Unexpected object to convert")
            };

        public static V2Obstacle Obstacle(BaseObstacle other) =>
            other switch
            {
                V3Obstacle o => new V2Obstacle(o) { CustomData = CustomDataObject(o.CustomData) },
                V2Obstacle o => new V2Obstacle(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 obstacle to v2 obstacle")
            };

        public static V2Arc Arc(BaseArc other) =>
            other switch
            {
                V3Arc o => new V2Arc(o) { CustomData = CustomDataObject(o.CustomData) },
                V2Arc o => new V2Arc(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 arc to v2 arc")
            };

        public static V2Waypoint Waypoint(BaseWaypoint other) =>
            other switch
            {
                V3Waypoint o => new V2Waypoint(o) { CustomData = CustomDataObject(o.CustomData) },
                V2Waypoint o => new V2Waypoint(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 waypoint to v2 waypoint")
            };

        public static V2Bookmark Bookmark(BaseBookmark other) =>
            other switch
            {
                V3Bookmark o => new V2Bookmark(o),
                V2Bookmark o => o,
                _ => throw new ArgumentException("Unexpected object to convert v3 bookmark to v2 bookmark")
            };

        public static V2BpmChange BpmChange(BaseBpmChange other) =>
            other switch
            {
                V3BpmChange o => new V2BpmChange(o),
                V2BpmChange o => o,
                _ => throw new ArgumentException("Unexpected object to convert v3 BPM change to v2 BPM change")
            };

        public static V2CustomEvent CustomEvent(BaseCustomEvent other) =>
            other switch
            {
                V3CustomEvent o => new V2CustomEvent(o),
                V2CustomEvent o => o,
                _ => throw new ArgumentException("Unexpected object to convert v3 BPM change to v2 BPM change")
            };

        public static V2EnvironmentEnhancement EnvironmentEnhancement(BaseEnvironmentEnhancement other) =>
            other switch
            {
                V3EnvironmentEnhancement o => new V2EnvironmentEnhancement(o) { Position = RescaleVector3(o.Position), LocalPosition = RescaleVector3(o.LocalPosition) },
                V2EnvironmentEnhancement o => o,
                _ => throw new ArgumentException("Unexpected object to convert v3 environment enhancement to v2 environment enhancement")
            };

        private static Vector3? RescaleVector3(Vector3? vec3) => vec3 is { } v ? new Vector3(v.x / 0.6f, v.y / 0.6f, v.z / 0.6f) as Vector3? : null;

        public static JSONNode CustomDataObject(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();

            if (n.HasKey("color")) n["_color"] = n.HasKey("_color") ? n["_color"] : n["color"];
            if (n.HasKey("coordinates")) n["_position"] = n.HasKey("_position") ? n["_position"] : n["coordinates"];
            if (n.HasKey("disableNoteGravity")) n["_disableNoteGravity"] = n.HasKey("_disableNoteGravity") ? n["_disableNoteGravity"] : n["disableNoteGravity"];
            if (n.HasKey("disableNoteLook")) n["_disableNoteLook"] = n.HasKey("_disableNoteLook") ? n["_disableNoteLook"] : n["disableNoteLook"];
            if (n.HasKey("flip")) n["_flip"] = n.HasKey("_flip") ? n["_flip"] : n["flip"];
            if (n.HasKey("localRotation")) n["_localRotation"] = n.HasKey("_localRotation") ? n["_localRotation"] : n["localRotation"];
            if (n.HasKey("noteJumpMovementSpeed")) n["_noteJumpMovementSpeed"] = n.HasKey("_noteJumpMovementSpeed") ? n["_noteJumpMovementSpeed"] : n["noteJumpMovementSpeed"];
            if (n.HasKey("noteJumpStartBeatOffset")) n["_noteJumpStartBeatOffset"] = n.HasKey("_noteJumpStartBeatOffset") ? n["_noteJumpStartBeatOffset"] : n["noteJumpStartBeatOffset"];
            if (n.HasKey("spawnEffect") && !n.HasKey("_disableSpawnEffect")) n["_disableSpawnEffect"] =  !n["spawnEffect"];
            if (n.HasKey("size")) n["_scale"] = n.HasKey("_scale") ? n["_scale"] : n["size"];
            if (n.HasKey("track")) n["_track"] = n.HasKey("_track") ? n["_track"] : n["track"];
            if (n.HasKey("uninteractable") && !n.HasKey("_interactable")) n["_interactable"] = !n["uninteractable"];
            if (n.HasKey("worldRotation")) n["_rotation"] = n.HasKey("_rotation") ? n["_rotation"] : n["worldRotation"];
            if (n.HasKey("animation") && !n.HasKey("_animation"))
                n["_animation"] = new JSONObject
                {
                    ["_color"] = n["animation"]["color"],
                    ["_definitePosition"] = n["animation"]["definitePosition"],
                    ["_dissolve"] = n["animation"]["dissolve"],
                    ["_dissolveArrow"] = n["animation"]["dissolveArrow"],
                    ["_interactable"] = n["animation"]["interactable"],
                    ["_localRotation"] = n["animation"]["localRotation"],
                    ["_position"] = n["animation"]["offsetPosition"],
                    ["_rotation"] = n["animation"]["offsetRotation"],
                    ["_scale"] = n["animation"]["scale"],
                    ["_time"] = n["animation"]["time"]
                };

            if (n.HasKey("color")) n.Remove("color");
            if (n.HasKey("coordinates")) n.Remove("coordinates");
            if (n.HasKey("disableNoteGravity")) n.Remove("disableNoteGravity");
            if (n.HasKey("disableNoteLook")) n.Remove("disableNoteLook");
            if (n.HasKey("flip")) n.Remove("flip");
            if (n.HasKey("localRotation")) n.Remove("localRotation");
            if (n.HasKey("noteJumpMovementSpeed")) n.Remove("noteJumpMovementSpeed");
            if (n.HasKey("noteJumpStartBeatOffset")) n.Remove("noteJumpStartBeatOffset");
            if (n.HasKey("spawnEffect")) n.Remove("spawnEffect");
            if (n.HasKey("size")) n.Remove("size");
            if (n.HasKey("track")) n.Remove("track");
            if (n.HasKey("uninteractable")) n.Remove("uninteractable");
            if (n.HasKey("worldRotation")) n.Remove("worldRotation");
            if (n.HasKey("animation")) n.Remove("animation");

            return n;
        }

        public static JSONNode CustomDataEvent(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();

            if (n.HasKey("color")) n["_color"] = n.HasKey("_color") ? n["_color"] : n["color"];
            if (n.HasKey("lightID")) n["_lightID"] = n.HasKey("_lightID") ? n["_lightID"] : n["lightID"];
            if (n.HasKey("easing")) n["_easing"] = n.HasKey("_easing") ? n["_easing"] : n["easing"];
            if (n.HasKey("lerpType")) n["_lerpType"] = n.HasKey("_lerpType") ? n["_lerpType"] : n["lerpType"];
            if (n.HasKey("nameFilter")) n["_nameFilter"] = n.HasKey("_nameFilter") ? n["_nameFilter"] : n["nameFilter"];
            if (n.HasKey("rotation")) n["_rotation"] = n.HasKey("_rotation") ? n["_rotation"] : n["rotation"];
            if (n.HasKey("step")) n["_step"] = n.HasKey("_step") ? n["_step"] : n["step"];
            if (n.HasKey("prop")) n["_prop"] = n.HasKey("_prop") ? n["_prop"] : n["prop"];
            if (n.HasKey("speed")) n["_speed"] = n.HasKey("_speed") ? n["_speed"] : n["speed"];
            if (n.HasKey("direction")) n["_direction"] = n.HasKey("_direction") ? n["_direction"] : n["direction"];
            if (n.HasKey("lockRotation")) n["_lockPosition"] = n.HasKey("_lockPosition") ? n["_lockPosition"] : n["lockRotation"];
            if (n.HasKey("speed")) n["_preciseSpeed"] = n.HasKey("_preciseSpeed") ? n["_preciseSpeed"] : n["speed"];

            if (n.HasKey("color")) n.Remove("color");
            if (n.HasKey("lightID")) n.Remove("lightID");
            if (n.HasKey("easing")) n.Remove("easing");
            if (n.HasKey("lerpType")) n.Remove("lerpType");
            if (n.HasKey("nameFilter")) n.Remove("nameFilter");
            if (n.HasKey("rotation")) n.Remove("rotation");
            if (n.HasKey("step")) n.Remove("step");
            if (n.HasKey("prop")) n.Remove("prop");
            if (n.HasKey("speed")) n.Remove("speed");
            if (n.HasKey("direction")) n.Remove("direction");
            if (n.HasKey("lockRotation")) n.Remove("lockRotation");

            return n;
        }

        public static V2Difficulty Difficulty(V3Difficulty other) =>
            new V2Difficulty
            {
                DirectoryAndFile = other.DirectoryAndFile,
                Time = other.Time,
                Events = other.Events,
                Notes = other.Notes,
                Obstacles = other.Obstacles,
                Waypoints = other.Waypoints,
                Arcs = other.Arcs,
                
                BpmChanges = other.BpmChanges,
                Bookmarks = other.Bookmarks,
                CustomEvents = other.CustomEvents,
                EnvironmentEnhancements = other.EnvironmentEnhancements,
                
                CustomData = other.CustomData?.Clone()
            };
    }
}
