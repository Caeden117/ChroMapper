using System;
using System.Collections.Generic;
using Beatmap.Enums;
using Beatmap.Helper;
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
            TrackData = other.TrackData?.Clone();
            // Clone is important here
            ShaderKeywords = other.ShaderKeywords == null ? null : new List<string>(other.ShaderKeywords);
        }

        public BaseMaterial(JSONNode node)
        {
            Color = (node[KeyColor] is JSONArray color) ? color.ReadColor() : (Color?)null;
            Shader = RetrieveRequiredNode(node, KeyShader);
            TrackData = node.GetValueOrDefault(KeyTrack, null);
            if (node[KeyShaderKeywords] is JSONArray keywords)
            {
                ShaderKeywords = new List<string>();
                foreach (var keyword in keywords)
                {
                    ShaderKeywords.Add(keyword.Value);
                }
            }
        }

        public Color? Color { get; set; }
        public string Shader { get; set; }
        private JSONNode trackData;
        public JSONNode TrackData
        {
            get => trackData;
            set
            {
                trackData = value;
                TrackNames = TrackDataReader.ReadNames(value);
            }
        }
        public IReadOnlyList<string> TrackNames { get; private set; } = Array.Empty<string>();
        public string? Track
        {
            get => TrackData is JSONString name
                ? name.Value
                : null;
            set => TrackData = value == null
                ? null
                : new JSONString(value);
        }
        // Null when the authored JSON omitted the key. Chroma treats an absent shaderKeywords
        // differently from an explicit empty array, so empty presence must round-trip.
        public List<string>? ShaderKeywords { get; set; }

        public string KeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyColor,
            3 or 4 => V3Material.KeyColor
        };

        public string KeyShader => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyShader,
            3 or 4 => V3Material.KeyShader
        };

        public string KeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyTrack,
            3 or 4 => V3Material.KeyTrack
        };

        public string KeyShaderKeywords => Settings.Instance.MapVersion switch
        {
            2 => V2Material.KeyShaderKeywords,
            3 or 4 => V3Material.KeyShaderKeywords
        };

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2Material.ToJson(this),
            3 or 4 => V3Material.ToJson(this)
        };

        public override BaseItem Clone() => new BaseMaterial(this);
    }
}
