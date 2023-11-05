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

namespace Beatmap.Converters
{
    public static class V3ToV2
    {
        public static V2Note Note(BaseNote other)
        {
            var note = other switch
            {
                V2Note o => o,
                V3ColorNote o => new V2Note(o) { CustomData = CustomDataObject(o.CustomData) },
                V3BombNote o => new V2Note(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 color note to v2 note")
            };

            if (other.AngleOffset % 45 != 0)
            {
                var customCutDirection = other.CutDirection switch
                {
                    (int)NoteCutDirection.Down => 0,
                    (int)NoteCutDirection.DownRight => 45,
                    (int)NoteCutDirection.Right => 90,
                    (int)NoteCutDirection.UpRight => 135,
                    (int)NoteCutDirection.Up => 180,
                    (int)NoteCutDirection.UpLeft => 225,
                    (int)NoteCutDirection.Left => 270,
                    (int)NoteCutDirection.DownLeft => 315,
                    (int)NoteCutDirection.Any => 0,
                    _ => 0
                };
                customCutDirection += other.AngleOffset;
                note.CustomData ??= new JSONObject();
                note.CustomData["_cutDirection"] = customCutDirection;
            }

            if (other.CustomFake) note.GetOrCreateCustom()["_fake"] = true;

            note.RefreshCustom();
            return note;
        }

        public static V2Event Event(BaseEvent other)
        {
            var evt = other switch
            {
                V2Event o => o,
                V3BasicEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V3ColorBoostEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V3RotationEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                V3BpmEvent o => new V2Event(o) { CustomData = CustomDataEvent(other.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 basic event to v2 event")
            };
            evt.RefreshCustom();
            return evt;
        }

        public static V2BpmEvent BpmEvent(BaseBpmEvent other) =>
            other switch
            {
                V2BpmEvent o => o,
                V3BpmEvent o => new V2BpmEvent(o),
                _ => throw new ArgumentException("Unexpected object to convert v3 bpm event to v2 bpm event")
            };

        public static V2SpecialEventsKeywordFilters EventTypesWithKeywords(BaseEventTypesWithKeywords other) =>
            other switch
            {
                V2SpecialEventsKeywordFilters o => o,
                V3BasicEventTypesWithKeywords o => new V2SpecialEventsKeywordFilters(o),
                _ => throw new ArgumentException("Unexpected object to convert")
            };

        public static V2SpecialEventsKeywordFiltersKeywords EventTypesForKeywords(BaseEventTypesForKeywords other) =>
            other switch
            {
                V2SpecialEventsKeywordFiltersKeywords o => o,
                V3BasicEventTypesForKeywords o => new V2SpecialEventsKeywordFiltersKeywords(o),
                _ => throw new ArgumentException("Unexpected object to convert")
            };

        public static V2Obstacle Obstacle(BaseObstacle other)
        {
            var obstacle = other switch
            {
                V2Obstacle o => o,
                V3Obstacle o => new V2Obstacle(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 obstacle to v2 obstacle")
            };
            if (other.CustomFake) obstacle.GetOrCreateCustom()["_fake"] = true;
            obstacle.RefreshCustom();
            return obstacle;
        }

        public static V2Arc Arc(BaseArc other)
        {
            var arc = other switch
            {
                V2Arc o => o,
                V3Arc o => new V2Arc(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 arc to v2 arc")
            };
            arc.RefreshCustom();
            return arc;
        }

        public static V2Waypoint Waypoint(BaseWaypoint other)
        {
            var waypoint = other switch
            {
                V2Waypoint o => o,
                V3Waypoint o => new V2Waypoint(o) { CustomData = CustomDataObject(o.CustomData) },
                _ => throw new ArgumentException("Unexpected object to convert v3 waypoint to v2 waypoint")
            };
            waypoint.RefreshCustom();
            return waypoint;
        }

        public static V2Bookmark Bookmark(BaseBookmark other) =>
            other switch
            {
                V2Bookmark o => o,
                V3Bookmark o => new V2Bookmark(o),
                _ => throw new ArgumentException("Unexpected object to convert v3 bookmark to v2 bookmark")
            };

        public static V2BpmChange BpmChange(BaseBpmChange other) =>
            other switch
            {
                V2BpmChange o => o,
                V3BpmChange o => new V2BpmChange(o),
                _ => throw new ArgumentException("Unexpected object to convert v3 BPM change to v2 BPM change")
            };

        public static V2CustomEvent CustomEvent(BaseCustomEvent other) =>
            other switch
            {
                V2CustomEvent o => o,
                V3CustomEvent o => new V2CustomEvent(o),
                _ => throw new ArgumentException("Unexpected object to convert v3 custom event to v2 custom event")
            };

        public static V2EnvironmentEnhancement EnvironmentEnhancement(BaseEnvironmentEnhancement other) =>
            other switch
            {
                V2EnvironmentEnhancement o => o,
                V3EnvironmentEnhancement o => new V2EnvironmentEnhancement(o)
                {
                    Position = RescaleVector3(o.Position),
                    LocalPosition = RescaleVector3(o.LocalPosition),
                    Geometry = Geometry(other.Geometry?.AsObject)
                },
                _ => throw new ArgumentException(
                    "Unexpected object to convert v3 environment enhancement to v2 environment enhancement")
            };

        public static JSONObject Geometry(JSONObject other)
        {
            if (other == null) return null;
            var obj = new JSONObject();

            if (other["type"] == "CUSTOM")
            {
                obj["_type"] = other["type"];
                obj["_mesh"] = Mesh(obj["mesh"]?.AsObject);
                obj["_material"] = other["material"].IsString
                    ? other["material"]
                    : Material(obj["material"]?.AsObject);
                obj["_collision"] = other["collision"];
            }
            else
            {
                obj["_type"] = other["type"];
                obj["_material"] = other["material"].IsString
                    ? other["material"]
                    : Material(obj["material"]?.AsObject);
                obj["_collision"] = other["collision"];
            }

            return obj;
        }

        public static JSONObject Mesh(JSONObject other)
        {
            if (other == null) return null;
            var obj = new JSONObject { ["_vertices"] = other["vertices"] };

            if (other.HasKey("uv")) obj["_uv"] = other["uv"];
            if (other.HasKey("triangles")) obj["_triangles"] = other["triangles"];

            return obj;
        }

        public static JSONObject Material(JSONObject other)
        {
            if (other == null) return null;
            var obj = new JSONObject { ["_shader"] = other["shader"] };

            if (other.HasKey("shaderKeywords")) obj["_shaderKeywords"] = other["shaderKeywords"];
            if (other.HasKey("collision")) obj["_collision"] = other["collision"];
            if (other.HasKey("track")) obj["_track"] = other["track"];
            if (other.HasKey("color")) obj["_color"] = other["color"];

            return obj;
        }

        public static Vector3? RescaleVector3(Vector3? vec3) =>
            vec3 is { } v ? new Vector3(v.x / 0.6f, v.y / 0.6f, v.z / 0.6f) as Vector3? : null;

        public static JSONNode CustomDataObject(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();

            if (n.HasKey("color")) n["_color"] = n.HasKey("_color") ? n["_color"] : n["color"];
            if (n.HasKey("coordinates")) n["_position"] = n.HasKey("_position") ? n["_position"] : n["coordinates"];
            if (n.HasKey("disableNoteGravity"))
                n["_disableNoteGravity"] =
                    n.HasKey("_disableNoteGravity") ? n["_disableNoteGravity"] : n["disableNoteGravity"];
            if (n.HasKey("disableNoteLook"))
                n["_disableNoteLook"] = n.HasKey("_disableNoteLook") ? n["_disableNoteLook"] : n["disableNoteLook"];
            if (n.HasKey("flip")) n["_flip"] = n.HasKey("_flip") ? n["_flip"] : n["flip"];
            if (n.HasKey("localRotation"))
                n["_localRotation"] = n.HasKey("_localRotation") ? n["_localRotation"] : n["localRotation"];
            if (n.HasKey("noteJumpMovementSpeed"))
                n["_noteJumpMovementSpeed"] = n.HasKey("_noteJumpMovementSpeed")
                    ? n["_noteJumpMovementSpeed"]
                    : n["noteJumpMovementSpeed"];
            if (n.HasKey("noteJumpStartBeatOffset"))
                n["_noteJumpStartBeatOffset"] = n.HasKey("_noteJumpStartBeatOffset")
                    ? n["_noteJumpStartBeatOffset"]
                    : n["noteJumpStartBeatOffset"];
            if (n.HasKey("spawnEffect") && !n.HasKey("_disableSpawnEffect"))
                n["_disableSpawnEffect"] = !n["spawnEffect"];
            if (n.HasKey("size")) n["_scale"] = n.HasKey("_scale") ? n["_scale"] : n["size"];
            if (n.HasKey("track")) n["_track"] = n.HasKey("_track") ? n["_track"] : n["track"];
            if (n.HasKey("uninteractable") && !n.HasKey("_interactable")) n["_interactable"] = !n["uninteractable"];
            if (n.HasKey("worldRotation")) n["_rotation"] = n.HasKey("_rotation") ? n["_rotation"] : n["worldRotation"];
            if (n.HasKey("animation") && !n.HasKey("_animation"))
            {
                var obj = new JSONObject();
                if (n["animation"].HasKey("color")) obj["_color"] = n["animation"]["color"];
                if (n["animation"].HasKey("definitePosition"))
                    obj["_definitePosition"] = n["animation"]["definitePosition"];
                if (n["animation"].HasKey("dissolve")) obj["_dissolve"] = n["animation"]["dissolve"];
                if (n["animation"].HasKey("dissolveArrow")) obj["_dissolveArrow"] = n["animation"]["dissolveArrow"];
                if (n["animation"].HasKey("interactable")) obj["_interactable"] = n["animation"]["interactable"];
                if (n["animation"].HasKey("localRotation")) obj["_localRotation"] = n["animation"]["localRotation"];
                if (n["animation"].HasKey("offsetPosition")) obj["_position"] = n["animation"]["offsetPosition"];
                if (n["animation"].HasKey("offsetRotation")) obj["_rotation"] = n["animation"]["offsetRotation"];
                if (n["animation"].HasKey("scale")) obj["_scale"] = n["animation"]["scale"];
                if (n["animation"].HasKey("time")) obj["_time"] = n["animation"]["time"];
                if (obj.Children.Any())
                    n["_animation"] = obj;
            }

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
            if (n.HasKey("lockRotation"))
                n["_lockPosition"] = n.HasKey("_lockPosition") ? n["_lockPosition"] : n["lockRotation"];
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

        public static V2Difficulty Difficulty(V3Difficulty other)
        {
            var d = new V2Difficulty
            {
                DirectoryAndFile = other.DirectoryAndFile,
                Events = other.Events,
                Notes = other.Notes,
                Obstacles = other.Obstacles,
                Waypoints = other.Waypoints,
                Arcs = other.Arcs,
                BpmChanges = other.BpmChanges,
                Bookmarks = other.Bookmarks,
                CustomEvents = other.CustomEvents,
                EnvironmentEnhancements = other.EnvironmentEnhancements,
                Time = other.Time,
                CustomData = other.CustomData?.Clone() ?? new JSONObject()
            };

            if (d.Materials.Any())
            {
                var newMat = d.Materials.ToDictionary(m => m.Key, m => (BaseMaterial)new V2Material(m.Value));
                d.Materials = newMat;
            }

            if (d.CustomData != null)
            {
                if (d.CustomData.HasKey("time")) d.CustomData.Remove("time");
                if (d.CustomData.HasKey("BPMChanges")) d.CustomData.Remove("BPMChanges");
                if (d.CustomData.HasKey("bookmarks")) d.CustomData.Remove("bookmarks");
                if (d.CustomData.HasKey("customEvents")) d.CustomData.Remove("customEvents");
                if (d.CustomData.HasKey("pointDefinitions")) d.CustomData.Remove("pointDefinitions");
                if (d.CustomData.HasKey("environment")) d.CustomData.Remove("environment");
                if (d.CustomData.HasKey("materials")) d.CustomData.Remove("materials");
                if (d.CustomData.HasKey(other.BookmarksUseOfficialBpmEventsKey)) d.CustomData.Remove(other.BookmarksUseOfficialBpmEventsKey);
            }

            return d;
        }
    }
}
