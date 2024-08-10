using System;
using System.Collections.Generic;
using Beatmap.Enums;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base.Customs
{
    public class BaseMaterial : BaseItem
    {
        // TODO: Add C.U.M.?

        public BaseMaterial()
        {
        }

        public BaseMaterial(BaseMaterial other)
        {
            Color = other.Color;
            Shader = other.Shader;
            Track = other.Track;
            ShaderKeywords = other.ShaderKeywords;
        }

        public BaseMaterial(JSONNode node)
        {
            Color = (node[KeyColor] is JSONArray color) ? color.ReadColor() : (Color?)null;
            Shader = RetrieveRequiredNode(node, KeyShader);
            Track = (node[KeyTrack] is JSONString track) ? (string)track : (string)null;
            ShaderKeywords = new List<string>();
            if (node[KeyShaderKeywords] is JSONArray keywords)
            {
                foreach (var keyword in keywords)
                {
                    ShaderKeywords.Add(keyword.Value);
                }
            }
        }

        public Color? Color { get; set; }
        public string Shader { get; set; }
        public string? Track { get; set; }
        public List<string> ShaderKeywords { get; set; }

        public string KeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyColor,
            3 => V3Material.KeyColor
        };

        public string KeyShader => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyShader,
            3 => V3Material.KeyShader
        };

        public string KeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyTrack,
            3 => V3Material.KeyTrack
        };

        public string KeyShaderKeywords => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyShaderKeywords,
            3 => V3Material.KeyShaderKeywords
        };

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2Material.ToJson(this),
            3 => V3Material.ToJson(this)
        };

        public override BaseItem Clone() => new BaseMaterial(this);
    }
}
