using System;
using System.Collections.Generic;
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
        public static V3BpmChange BpmChange(BaseBpmChange other) =>
            other switch
            {
                V3BpmChange o => o,
                V2BpmChange o => new V3BpmChange(o),
                _ => throw new ArgumentException("Unexpected object to convert v2 BPM change to v3 BPM change")
            };

        public static V3CustomEvent CustomEvent(BaseCustomEvent other) =>
            other switch
            {
                V3CustomEvent o => o,
                V2CustomEvent o => new V3CustomEvent(o),
                _ => throw new ArgumentException("Unexpected object to convert v2 custom event to v3 custom event")
            };

        public static V3EnvironmentEnhancement EnvironmentEnhancement(BaseEnvironmentEnhancement other) =>
            other switch
            {
                V3EnvironmentEnhancement o => o,
                V2EnvironmentEnhancement o => new V3EnvironmentEnhancement(o)
                {
                    Position = RescaleVector3(o.Position),
                    LocalPosition = RescaleVector3(o.LocalPosition),
                    Geometry = Geometry(other.Geometry?.AsObject)
                },
                _ => throw new ArgumentException(
                    "Unexpected object to convert v2 environment enhancement to v3 environment enhancement")
            };

        public static JSONObject Geometry(JSONObject other)
        {
            if (other == null) return null;
            var obj = new JSONObject();

            if (other["_type"] == "CUSTOM")
            {
                obj["type"] = other["_type"];
                obj["mesh"] = Mesh(obj["_mesh"]?.AsObject);
                obj["material"] = other["_material"].IsString
                    ? other["_material"]
                    : Material(obj["_material"]?.AsObject);
                obj["collision"] = other["_collision"];
            }
            else
            {
                obj["type"] = other["_type"];
                obj["material"] = other["_material"].IsString
                    ? other["_material"]
                    : Material(obj["material"]?.AsObject);
                obj["collision"] = other["_collision"];
            }

            return obj;
        }

        public static JSONObject Mesh(JSONObject other)
        {
            if (other == null) return null;
            var obj = new JSONObject { ["vertices"] = other["_vertices"] };

            if (other.HasKey("_uv")) obj["uv"] = other["_uv"];
            if (other.HasKey("_triangles")) obj["triangles"] = other["_triangles"];

            return obj;
        }

        public static JSONObject Material(JSONObject other)
        {
            if (other == null) return null;
            var obj = new JSONObject { ["shader"] = other["_shader"] };

            if (other.HasKey("_shaderKeywords")) obj["shaderKeywords"] = other["_shaderKeywords"];
            if (other.HasKey("_collision")) obj["collision"] = other["_collision"];
            if (other.HasKey("_track")) obj["track"] = other["_track"];
            if (other.HasKey("_color")) obj["color"] = other["_color"];

            return obj;
        }

        public static Vector3? RescaleVector3(Vector3? vec3) =>
            vec3 is { } v ? new Vector3(v.x * 0.6f, v.y * 0.6f, v.z * 0.6f) as Vector3? : null;

        public static JSONNode CustomDataObject(JSONNode node)
        {
            if (node == null) return null;
            if (!node.Children.Any()) return null;
            var n = node.Clone();

            if (n.HasKey("_color")) n["color"] = n.HasKey("color") ? n["color"] : n["_color"];
            if (n.HasKey("_position")) n["coordinates"] = n.HasKey("coordinates") ? n["coordinates"] : n["_position"];
            if (n.HasKey("_disableNoteGravity"))
                n["disableNoteGravity"] =
                    n.HasKey("disableNoteGravity") ? n["disableNoteGravity"] : n["_disableNoteGravity"];
            if (n.HasKey("_disableNoteLook"))
                n["disableNoteLook"] = n.HasKey("disableNoteLook") ? n["disableNoteLook"] : n["_disableNoteLook"];
            if (n.HasKey("_flip")) n["flip"] = n.HasKey("flip") ? n["flip"] : n["_flip"];
            if (n.HasKey("_localRotation"))
                n["localRotation"] = n.HasKey("localRotation") ? n["localRotation"] : n["_localRotation"];
            if (n.HasKey("_noteJumpMovementSpeed"))
                n["noteJumpMovementSpeed"] = n.HasKey("noteJumpMovementSpeed")
                    ? n["noteJumpMovementSpeed"]
                    : n["_noteJumpMovementSpeed"];
            if (n.HasKey("_noteJumpStartBeatOffset"))
                n["noteJumpStartBeatOffset"] = n.HasKey("noteJumpStartBeatOffset")
                    ? n["noteJumpStartBeatOffset"]
                    : n["_noteJumpStartBeatOffset"];
            if (n.HasKey("_disableSpawnEffect") && !n.HasKey("spawnEffect"))
                n["spawnEffect"] = !n["_disableSpawnEffect"];
            if (n.HasKey("_scale")) n["size"] = n.HasKey("size") ? n["size"] : n["_scale"];
            if (n.HasKey("_track")) n["track"] = n.HasKey("track") ? n["track"] : n["_track"];
            if (n.HasKey("_interactable") && !n.HasKey("uninteractable")) n["uninteractable"] = !n["_interactable"];
            if (n.HasKey("_rotation"))
                n["worldRotation"] = n.HasKey("worldRotation") ? n["worldRotation"] : n["_rotation"];
            if (n.HasKey("_animation") && !n.HasKey("animation"))
            {
                var obj = new JSONObject();
                if (n["_animation"].HasKey("_color")) obj["color"] = n["_animation"]["_color"];
                if (n["_animation"].HasKey("_definitePosition"))
                    obj["definitePosition"] = n["_animation"]["_definitePosition"];
                if (n["_animation"].HasKey("_dissolve")) obj["dissolve"] = n["_animation"]["_dissolve"];
                if (n["_animation"].HasKey("_dissolveArrow")) obj["dissolveArrow"] = n["_animation"]["_dissolveArrow"];
                if (n["_animation"].HasKey("_interactable")) obj["interactable"] = n["_animation"]["_interactable"];
                if (n["_animation"].HasKey("_localRotation")) obj["localRotation"] = n["_animation"]["_localRotation"];
                if (n["_animation"].HasKey("_position")) obj["offsetPosition"] = n["_animation"]["_position"];
                if (n["_animation"].HasKey("_rotation")) obj["offsetRotation"] = n["_animation"]["_rotation"];
                if (n["_animation"].HasKey("_scale")) obj["scale"] = n["_animation"]["_scale"];
                if (n["_animation"].HasKey("_time")) obj["time"] = n["_animation"]["_time"];
                if (obj.Children.Any())
                    n["animation"] = obj;
            }

            if (n.HasKey("_color")) n.Remove("_color");
            if (n.HasKey("_fake")) n.Remove("_fake");
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
            
            if (n.HasKey("_preciseSpeed")) n["speed"] = n["_preciseSpeed"];
            else if (n.HasKey("_speed")) n["speed"] = n.HasKey("speed") ? n["speed"] : n["_speed"];
            
            if (n.HasKey("_color")) n["color"] = n.HasKey("color") ? n["color"] : n["_color"];
            if (n.HasKey("_lightID")) n["lightID"] = n.HasKey("lightID") ? n["lightID"] : n["_lightID"];
            if (n.HasKey("_easing")) n["easing"] = n.HasKey("easing") ? n["easing"] : n["_easing"];
            if (n.HasKey("_lerpType")) n["lerpType"] = n.HasKey("lerpType") ? n["lerpType"] : n["_lerpType"];
            if (n.HasKey("_nameFilter")) n["nameFilter"] = n.HasKey("nameFilter") ? n["nameFilter"] : n["_nameFilter"];
            if (n.HasKey("_rotation")) n["rotation"] = n.HasKey("rotation") ? n["rotation"] : n["_rotation"];
            if (n.HasKey("_step")) n["step"] = n.HasKey("step") ? n["step"] : n["_step"];
            if (n.HasKey("_prop")) n["prop"] = n.HasKey("prop") ? n["prop"] : n["_prop"];
            if (n.HasKey("_direction")) n["direction"] = n.HasKey("direction") ? n["direction"] : n["_direction"];
            if (n.HasKey("_lockPosition"))
                n["lockRotation"] = n.HasKey("lockRotation") ? n["lockRotation"] : n["_lockPosition"];

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
    }
}
