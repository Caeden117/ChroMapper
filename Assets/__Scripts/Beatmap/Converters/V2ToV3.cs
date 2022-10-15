using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V2.Customs;
using Beatmap.V3;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Core.Parsing;

namespace Beatmap.Converters
{
    public static class V2ToV3
    {
        public static V3BombNote BombNote(BaseNote other) =>
            other switch
            {
                V2Note o => new V3BombNote(o) { CustomData = CustomDataObject(o.CustomData) },
                V3BombNote o => new V3BombNote(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v2 note to v3 color note")
            };

        public static V3BpmEvent BpmEvent(BaseEvent other) =>
            other switch
            {
                V2Event o => new V3BpmEvent(o),
                V3BpmEvent o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 event to v3 BPM event")
            };

        public static V3ColorBoostEvent ColorBoostEvent(BaseEvent other) =>
            other switch
            {
                V2Event o => new V3ColorBoostEvent(o),
                V3ColorBoostEvent o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 event to v3 color boost event")
            };

        public static V3ColorNote ColorNote(BaseNote other) =>
            other switch
            {
                V2Note o => new V3ColorNote(o) { CustomData = CustomDataObject(o.CustomData) },
                V3ColorNote o => new V3ColorNote(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ParsingErrors()
            };

        public static V3BasicEvent BasicEvent(BaseEvent other) =>
            other switch
            {
                V2Event o => new V3BasicEvent(o) { CustomData = CustomDataEvent(o.CustomData) },
                V3BasicEvent o => new V3BasicEvent(o) { CustomData = CustomDataEvent(o.CustomData) },
                V3ColorBoostEvent o => new V3BasicEvent(o), // TODO: this might be bad
                V3BpmEvent o => new V3BasicEvent(o),
                V3RotationEvent o => new V3BasicEvent(o),
                _ => throw new ArgumentException("Unexpected object to convert v2 event to v3 basic event")
            };

        public static V3BasicEventTypesWithKeywords EventTypesWithKeywords(BaseEventTypesWithKeywords other) =>
            other switch
            {
                V2SpecialEventsKeywordFilters o => new V3BasicEventTypesWithKeywords(o),
                V3BasicEventTypesWithKeywords o => o,
                _ => throw new ArgumentException("Unexpected object to convert")
            };

        public static V3BasicEventTypesForKeywords
            EventTypesForKeywords(BaseEventTypesForKeywords other) =>
            other switch
            {
                V2SpecialEventsKeywordFiltersKeywords o => new V3BasicEventTypesForKeywords(o),
                V3BasicEventTypesForKeywords o => o,
                _ => throw new ArgumentException("Unexpected object to convert")
            };

        public static V3Obstacle Obstacle(BaseObstacle other) =>
            other switch
            {
                V2Obstacle o => new V3Obstacle(o) { CustomData = CustomDataObject(o.CustomData) },
                V3Obstacle o => new V3Obstacle(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v2 obstacle to v3 obstacle")
            };

        public static V3RotationEvent RotationEvent(BaseEvent other) =>
            other switch
            {
                V2Event o => new V3RotationEvent(o),
                V3RotationEvent o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 event to v3 rotation event")
            };

        public static V3Arc Arc(BaseArc other) =>
            other switch
            {
                V2Arc o => new V3Arc(o) { CustomData = CustomDataObject(o.CustomData) },
                V3Arc o => new V3Arc(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v2 arc to v3 arc")
            };

        public static V3Waypoint Waypoint(BaseWaypoint other) =>
            other switch
            {
                V2Waypoint o => new V3Waypoint(o) { CustomData = CustomDataObject(o.CustomData) },
                V3Waypoint o => new V3Waypoint(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v2 waypoint to v3 waypoint")
            };

        public static V3Bookmark Bookmark(BaseBookmark other) =>
            other switch
            {
                V2Bookmark o => new V3Bookmark(o),
                V3Bookmark o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 bookmark to v3 bookmark")
            };

        public static V3BpmChange BpmChange(BaseBpmChange other) =>
            other switch
            {
                V2BpmChange o => new V3BpmChange(o),
                V3BpmChange o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 BPM change to v3 BPM change")
            };

        public static V3CustomEvent CustomEvent(BaseCustomEvent other) =>
            other switch
            {
                V2CustomEvent o => new V3CustomEvent(o),
                V3CustomEvent o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 custom event to v3 custom event")
            };

        public static V3EnvironmentEnhancement EnvironmentEnhancement(BaseEnvironmentEnhancement other) =>
            other switch
            {
                V2EnvironmentEnhancement o => new V3EnvironmentEnhancement(o),
                V3EnvironmentEnhancement o => o,
                _ => throw new ArgumentException("Unexpected object to convert v2 environment enhancement to v3 environment enhancement")
            };

        public static JSONNode CustomDataObject(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();

            if (n.HasKey("_color")) n["color"] = n.HasKey("color") ? n["color"] : n["_color"];
            if (n.HasKey("_position")) n["coordinates"] = n.HasKey("coordinates") ? n["coordinates"] : n["_position"];
            if (n.HasKey("_disableNoteGravity")) n["disableNoteGravity"] = n.HasKey("disableNoteGravity") ? n["disableNoteGravity"] : n["_disableNoteGravity"];
            if (n.HasKey("_disableNoteLook")) n["disableNoteLook"] = n.HasKey("disableNoteLook") ? n["disableNoteLook"] : n["_disableNoteLook"];
            if (n.HasKey("_flip")) n["flip"] = n.HasKey("flip") ? n["flip"] : n["_flip"];
            if (n.HasKey("_localRotation")) n["localRotation"] = n.HasKey("localRotation") ? n["localRotation"] : n["_localRotation"];
            if (n.HasKey("_noteJumpMovementSpeed")) n["noteJumpMovementSpeed"] = n.HasKey("noteJumpMovementSpeed") ? n["noteJumpMovementSpeed"] : n["_noteJumpMovementSpeed"];
            if (n.HasKey("_noteJumpStartBeatOffset")) n["noteJumpStartBeatOffset"] = n.HasKey("noteJumpStartBeatOffset") ? n["noteJumpStartBeatOffset"] : n["_noteJumpStartBeatOffset"];
            if (n.HasKey("_disableSpawnEffect") && !n.HasKey("spawnEffect")) n["spawnEffect"] = !n["_disableSpawnEffect"];
            if (n.HasKey("_scale")) n["size"] = n.HasKey("size") ? n["size"] : n["_scale"];
            if (n.HasKey("_track")) n["track"] = n.HasKey("track") ? n["track"] : n["_track"];
            if (n.HasKey("_interactable") && !n.HasKey("uninteractable")) n["uninteractable"] = !n["_interactable"];
            if (n.HasKey("_rotation")) n["worldRotation"] = n.HasKey("worldRotation") ? n["worldRotation"] : n["_rotation"];
            if (n.HasKey("_animation") && !n.HasKey("animation"))
                n["animation"] = new JSONObject
                {
                    ["color"] = n["_animation"]["_color"],
                    ["definitePosition"] = n["_animation"]["_definitePosition"],
                    ["dissolve"] = n["_animation"]["_dissolve"],
                    ["dissolveArrow"] = n["_animation"]["_dissolveArrow"],
                    ["interactable"] = n["_animation"]["_interactable"],
                    ["localRotation"] = n["_animation"]["_localRotation"],
                    ["offsetPosition"] = n["_animation"]["_position"],
                    ["offsetRotation"] = n["_animation"]["_rotation"],
                    ["scale"] = n["_animation"]["_scale"],
                    ["time"] = n["_animation"]["_time"]
                };

            if (n.HasKey("_color")) n.Remove("_color");
            if (n.HasKey("_position")) n.Remove("_position");
            if (n.HasKey("_disableNoteGravity")) n.Remove("_disableNoteGravity");
            if (n.HasKey("_disableNoteLook")) n.Remove("_disableNoteLook");
            if (n.HasKey("_flip")) n.Remove("_flip");
            if (n.HasKey("_cutDirection")) n.Remove("_cutDirection");
            if (n.HasKey("_localRotation")) n.Remove("_localRotation");
            if (n.HasKey("_noteJumpMovementSpeed")) n.Remove("_noteJumpMovementSpeed");
            if (n.HasKey("_noteJumpStartBeatOffset")) n.Remove("_noteJumpStartBeatOffset");
            if (n.HasKey("_disableSpawnEffect")) n.Remove("_disableSpawnEffect");
            if (n.HasKey("_scale")) n.Remove("_scale");
            if (n.HasKey("_track")) n.Remove("_track");
            if (n.HasKey("_interactable")) n.Remove("_interactable");
            if (n.HasKey("_rotation")) n.Remove("_rotation");
            if (n.HasKey("_animation")) n.Remove("_animation");

            return n;
        }

        public static JSONNode CustomDataEvent(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();

            var speed = n["_preciseSpeed"] ?? n["_speed"];
            if (n.HasKey("_color")) n["color"] = n.HasKey("color") ? n["color"] : n["_color"];
            if (n.HasKey("_lightID")) n["lightID"] = n.HasKey("lightID") ? n["lightID"] : n["_lightID"];
            if (n.HasKey("_easing")) n["easing"] = n.HasKey("easing") ? n["easing"] : n["_easing"];
            if (n.HasKey("_lerpType")) n["lerpType"] = n.HasKey("lerpType") ? n["lerpType"] : n["_lerpType"];
            if (n.HasKey("_nameFilter")) n["nameFilter"] = n.HasKey("nameFilter") ? n["nameFilter"] : n["_nameFilter"];
            if (n.HasKey("_rotation")) n["rotation"] = n.HasKey("rotation") ? n["rotation"] : n["_rotation"];
            if (n.HasKey("_step")) n["step"] = n.HasKey("step") ? n["step"] : n["_step"];
            if (n.HasKey("_prop")) n["prop"] = n.HasKey("prop") ? n["prop"] : n["_prop"];
            if (speed != null) n["speed"] = speed;
            if (n.HasKey("_direction")) n["direction"] = n.HasKey("direction") ? n["direction"] : n["_direction"];
            if (n.HasKey("_lockPosition")) n["lockRotation"] = n.HasKey("lockRotation") ? n["lockRotation"] : n["_lockPosition"];

            if (n.HasKey("_color")) n.Remove("_color");
            if (n.HasKey("_lightID")) n.Remove("_lightID");
            if (n.HasKey("_easing")) n.Remove("_easing");
            if (n.HasKey("_lerpType")) n.Remove("_lerpType");
            if (n.HasKey("_propID")) n.Remove("_propID");
            if (n.HasKey("_lightGradient")) n.Remove("_lightGradient");
            if (n.HasKey("_nameFilter")) n.Remove("_nameFilter");
            if (n.HasKey("_rotation")) n.Remove("_rotation");
            if (n.HasKey("_step")) n.Remove("_step");
            if (n.HasKey("_prop")) n.Remove("_prop");
            if (n.HasKey("_speed")) n.Remove("_speed");
            if (n.HasKey("_preciseSpeed")) n.Remove("_preciseSpeed");
            if (n.HasKey("_direction")) n.Remove("_direction");
            if (n.HasKey("_reset")) n.Remove("_reset");
            if (n.HasKey("_counterSpin")) n.Remove("_counterSpin");
            if (n.HasKey("_stepMult")) n.Remove("_stepMult");
            if (n.HasKey("_propMult")) n.Remove("_propMult");
            if (n.HasKey("_speedMult")) n.Remove("_speedMult");
            if (n.HasKey("_lockPosition")) n.Remove("_lockPosition");

            return n;
        }

        public static V3Difficulty Difficulty(V2Difficulty other)
        {
            var d = new V3Difficulty
                {
                    DirectoryAndFile = other.DirectoryAndFile,
                    Time = other.Time,
                    Obstacles = other.Obstacles.Select(Obstacle).Cast<BaseObstacle>().ToList(),
                    Arcs = other.Arcs.Select(Arc).Cast<BaseArc>().ToList(),
                    Waypoints = other.Waypoints.Select(Waypoint).Cast<BaseWaypoint>().ToList(),
                    EventTypesWithKeywords = other.EventTypesWithKeywords != null
                        ? new V3BasicEventTypesWithKeywords(other.EventTypesWithKeywords)
                        : new V3BasicEventTypesWithKeywords(),
                    
                    BpmChanges = other.BpmChanges.Select(BpmChange).Cast<BaseBpmChange>().ToList(),
                    Bookmarks = other.Bookmarks.Select(Bookmark).Cast<BaseBookmark>().ToList(),
                    CustomEvents = other.CustomEvents.Select(CustomEvent).Cast<BaseCustomEvent>().ToList(),
                    EnvironmentEnhancements = other.EnvironmentEnhancements.Select(EnvironmentEnhancement).Cast<BaseEnvironmentEnhancement>()
                        .ToList(),
                    
                    CustomData = other.CustomData?.Clone() ?? new JSONObject()
                };

            foreach (var n in other.Notes)
                switch (n.Type)
                {
                    case (int)NoteType.Bomb:
                        d.Bombs.Add(BombNote(n));
                        break;
                    case (int)NoteType.Red:
                    case (int)NoteType.Blue:
                        d.Notes.Add(ColorNote(n));
                        break;
                }

            foreach (var e in other.Events)
                switch (e.Type)
                {
                    case (int)EventTypeValue.ColorBoost:
                        d.ColorBoostEvents.Add(ColorBoostEvent(e));
                        break;
                    case (int)EventTypeValue.EarlyLaneRotation:
                    case (int)EventTypeValue.LateLaneRotation:
                        d.RotationEvents.Add(RotationEvent(e));
                        break;
                    case (int)EventTypeValue.BpmChange:
                        d.BpmEvents.Add(BpmEvent(e));
                        break;
                    default:
                        d.Events.Add(BasicEvent(e));
                        break;
                }

            return d;
        }
    }
}
