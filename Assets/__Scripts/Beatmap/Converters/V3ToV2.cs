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
        
        public static JSONNode CustomEventData(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();
            
            if (n.HasKey(V3CustomEvent.CustomKeyTrack)) 
            {
                n[V2CustomEvent.CustomKeyTrack] = n[V3CustomEvent.CustomKeyTrack];
                n.Remove(V3CustomEvent.CustomKeyTrack);
            }

            if (n.HasKey(V3CustomEvent.DataKeyDuration)) 
            {
                n[V2CustomEvent.DataKeyDuration] = n[V3CustomEvent.DataKeyDuration];
                n.Remove(V3CustomEvent.DataKeyDuration);
            }

            if (n.HasKey(V3CustomEvent.DataKeyEasing)) 
            {
                n[V2CustomEvent.DataKeyEasing] = n[V3CustomEvent.DataKeyEasing];
                n.Remove(V3CustomEvent.DataKeyEasing);
            }

            if (n.HasKey(V3CustomEvent.DataKeyRepeat)) 
            {
                n[V2CustomEvent.DataKeyRepeat] = n[V3CustomEvent.DataKeyRepeat];
                n.Remove(V3CustomEvent.DataKeyRepeat);
            }

            if (n.HasKey(V3CustomEvent.DataKeyChildrenTracks)) 
            {
                n[V2CustomEvent.DataKeyChildrenTracks] = n[V3CustomEvent.DataKeyChildrenTracks];
                n.Remove(V3CustomEvent.DataKeyChildrenTracks);
            }

            if (n.HasKey(V3CustomEvent.DataKeyParentTrack)) 
            {
                n[V2CustomEvent.DataKeyParentTrack] = n[V3CustomEvent.DataKeyParentTrack];
                n.Remove(V3CustomEvent.DataKeyParentTrack);
            }

            if (n.HasKey(V3CustomEvent.DataKeyWorldPositionStays)) 
            {
                n[V2CustomEvent.DataKeyWorldPositionStays] = n[V3CustomEvent.DataKeyWorldPositionStays];
                n.Remove(V3CustomEvent.DataKeyWorldPositionStays);
            }
            
            return n;
        }

        public static JSONNode CustomDataRoot(JSONNode node, BaseDifficulty difficulty)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();
            
            if (n.HasKey("time"))
            {
                n["_time"] = n["time"];
                n.Remove("time");
            }

            if (n.HasKey("bookmarks"))
            {
                n["_bookmarks"] = n["bookmarks"];
                n["_bookmarks"].Remove("bookmarksUseOfficialBpmEvents");
                n.Remove("bookmarks");
            }

            if (n.HasKey("customEvents"))
            {
                var array = new JSONArray();
                foreach (var customEvent in difficulty.CustomEvents)
                {
                    array.Add(V2CustomEvent.ToJson(customEvent));
                }

                n["_customEvents"] = array;
                n.Remove("customEvents");
            }

            if (n.HasKey("environment"))
            {
                var array = new JSONArray();
                foreach (var enhancement in difficulty.EnvironmentEnhancements)
                {
                    array.Add(V2EnvironmentEnhancement.ToJson(enhancement));
                }

                n["_environment"] = array;
                n.Remove("environment");
            }

            if (n.HasKey("pointDefinitions"))
            {
                var pointArray = new JSONArray();
                foreach (var p in difficulty.PointDefinitions)
                {
                    var obj = new JSONObject
                    {
                        ["_name"] = p.Key,
                        ["_points"] = p.Value
                    };
                    pointArray.Add(obj);
                }

                n["_pointDefinitions"] = pointArray;
                n.Remove("pointDefinitions");
            }

            if (n.HasKey("materials"))
            {
                n["_materials"] = new JSONObject();
                foreach (var m in difficulty.Materials)
                    n["_materials"][m.Key] = V2Material.ToJson(m.Value);

                n.Remove("materials");
            }

            n.Remove("fakeColorNotes");
            n.Remove("fakeBombNotes");
            n.Remove("fakeObstacles");
            n.Remove("fakeBurstSliders");

            return n;
        }
    }
}
