using System;
using System.Collections.Generic;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base.Customs
{
    public abstract class BaseMaterial : BaseItem
    {
        // TODO: Add C.U.M.?

        protected BaseMaterial()
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

        public override JSONNode ToJson()
        {
            var node = new JSONObject();
            if (Color != null) node[KeyColor] = Color;
            node[KeyShader] = Shader;
            if (Track != null) node[KeyTrack] = Track;
            if (ShaderKeywords.Count > 0)
            {
                var keywords = new JSONArray();
                foreach (var keyword in ShaderKeywords)
                {
                    keywords.Add(keyword);
                }
                node[KeyShaderKeywords] = keywords;
            }
            return node;
        }

        public Color? Color { get; set; }
        public string Shader { get; set; }
        public string? Track { get; set; }
        public List<string> ShaderKeywords { get; set; }

        public abstract string KeyColor { get; }
        public abstract string KeyShader { get; }
        public abstract string KeyTrack { get; }
        public abstract string KeyShaderKeywords { get; }
    }
}
