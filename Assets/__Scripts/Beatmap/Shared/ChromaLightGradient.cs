using System;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Shared
{
    public class ChromaLightGradient : BaseItem
    {
        private const string EasingKey = "_easing";
        private SerializedChromaEnumName easingType;

        public float Duration;
        public Color EndColor;
        public Color StartColor;

        public string EasingType
        {
            get => easingType.RuntimeName ?? "easeLinear";
            set => easingType.SetEasingName(value);
        }

        public ChromaLightGradient(JSONNode node)
        {
            if (node["_startColor"] == null)
                throw new ArgumentException("Gradient object must have a start color named \"_startColor\"");
            if (node["_endColor"] == null)
                throw new ArgumentException("Gradient object must have a end color named \"_endColor\"");
            Duration = node?["_duration"] ?? 0;
            StartColor = node["_startColor"];
            EndColor = node["_endColor"];
            if (node.HasKey("_easing"))
            {
                if (!Easing.ByName.ContainsKey(node["_easing"]))
                    throw new ArgumentException("Gradient object contains invalid easing type.");
                EasingType = node["_easing"];
            }
            else
            {
                EasingType = "easeLinear";
            }
        }

        public ChromaLightGradient(Color start, Color end, float duration = 1, string easing = "easeLinear")
        {
            StartColor = start;
            EndColor = end;
            Duration = duration;
            EasingType = easing;
        }

        internal void SetSerializedEasing(JSONNode value) =>
            easingType.SetSerializedEasing(value);

        public override BaseItem Clone() => new ChromaLightGradient(StartColor, EndColor, Duration)
        {
            easingType = easingType.Clone()
        };

        public override JSONNode ToJson()
        {
            var output = new JSONObject
            {
                ["_duration"] = Duration,
                ["_startColor"] = StartColor,
                ["_endColor"] = EndColor
            };
            easingType.WriteTo(output, EasingKey);
            return output;
        }
    }
}
