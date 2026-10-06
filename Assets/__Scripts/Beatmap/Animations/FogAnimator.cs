using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    // Fog animation writes the preview renderer's environment parameters rather than object transforms.
    public class FogAnimator : MonoBehaviour
    {
        private const string ComponentName = "BloomFogEnvironment";

        public AudioTimeSyncController Atsc;
        public BeatmapRuntimeContext Context;

        private readonly Dictionary<string, AnimateProperty<float>> animatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();
        private bool baselineCaptured;
        // Fog events take effect only when this track owns the fog component or is selected by AssignFogTrack.
        private bool hasComponentEvents;
        private bool hasFogComponentTarget;
        private LegacyFogBinding legacyBinding;
        private bool controlsLegacyBinding;

        public void AddEvent(BaseCustomEvent ev)
        {
            if (ev.Data?[ComponentName] is not JSONObject component) return;

            hasComponentEvents = true;

            foreach (var jprop in component)
            {
                // Null stops further writes and leaves the current fog value in place.
                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    if (animatedProperties.Remove(jprop.Key))
                    {
                        RefreshProperties();
                    }

                    continue;
                }

                switch (jprop.Key)
                {
                    case "attenuation":
                    case "offset":
                    case "height":
                    case "startY":
                        break;
                    default:
                        continue;
                }

                AddProperty(jprop.Key, jprop.Value, ev);
            }

            RefreshProperties();
        }

        // V2 stores fog parameters on AnimateTrack; normalize their names to the V3 component names.
        public void AddLegacyEvent(BaseCustomEvent ev)
        {
            foreach (var jprop in ev.Data)
            {
                var key = jprop.Key switch
                {
                    "_attenuation" => "attenuation",
                    "_offset" => "offset",
                    "_height" => "height",
                    "_startY" => "startY",
                    _ => null
                };
                if (key == null) continue;

                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    if (animatedProperties.Remove(key))
                    {
                        RefreshProperties();
                    }

                    continue;
                }

                AddProperty(key, jprop.Value, ev);
            }

            RefreshProperties();
        }

        // Events may load before fog is attached. Apply them only after this track becomes the component owner.
        public void BindFogComponentTarget()
        {
            hasFogComponentTarget = true;
            RefreshEnabled();
        }

        // Only the controller updates the shared assignment timeline, preventing competing fog writes.
        public void SetLegacyBinding(LegacyFogBinding binding, bool controller)
        {
            legacyBinding = binding;
            controlsLegacyBinding = controller;
            RefreshEnabled();
        }

        private void AddProperty(string key, JSONNode points, BaseCustomEvent ev)
        {
            var p = new IPointDefinition.UntypedParams
            {
                Key = key,
                Points = points,
                Easing = ev.DataEasing,
                Time = ev.JsonTime,
                Duration = ev.DataDuration ?? 0,
                TimeBegin = ev.JsonTime,
                TimeEnd = ev.JsonTime + (ev.DataDuration ?? 0),
                Repeat = ev.DataRepeat ?? 0
            };

            // Skip missing point definitions before creating a property. Empty properties cannot be sorted.
            if (AnimateProperty<float>.SkipsMissingPointDefinition(p)) return;

            GetProperty(key).AddPointDef(PointDataParsers.ParseFloat, p, ev);
        }

        // Drop empty properties before RefreshProperties tries to sort them.
        public void RemoveEvent(BaseCustomEvent ev)
        {
            foreach (var key in animatedProperties.Keys.ToList())
            {
                var prop = animatedProperties[key];
                prop.RemoveEvent(ev);
                if (prop.IsEmpty())
                {
                    animatedProperties.Remove(key);
                }
            }

            RefreshProperties();
        }

        private void RefreshProperties()
        {
            properties = new IAnimateProperty[animatedProperties.Count];
            var i = 0;
            foreach (var prop in animatedProperties.Values)
            {
                prop.Sort();
                properties[i++] = prop;
            }

            RefreshEnabled();
        }

        private void RefreshEnabled() =>
            enabled = (properties.Length > 0 && hasComponentEvents && hasFogComponentTarget)
                || controlsLegacyBinding;

        private void Update() => PushAt(Atsc != null ? Atsc.CurrentJsonTime : 0);

        // Apply stopped-time seeks before the frame renders.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc == null || Atsc.IsPlaying) return;
            PushAt(Atsc.CurrentJsonTime);
        }

        private void PushAt(float time)
        {
            if (controlsLegacyBinding)
            {
                legacyBinding.PushAt(time);
            }

            if (!hasComponentEvents || !hasFogComponentTarget || properties.Length == 0) return;

            PushValuesAt(time);
        }

        // The assignment timeline can read an inactive track without enabling its Update.
        public void PushLegacyValuesAt(float time)
        {
            if (properties.Length > 0) PushValuesAt(time);
        }

        private void PushValuesAt(float time)
        {

            // Capture authored fog values after enhancements load, before the first animation write.
            // These become the defaults when seeking before the first event.
            if (!baselineCaptured)
            {
                baselineCaptured = true;
                foreach (var pair in animatedProperties)
                {
                    pair.Value.Default = ReadParam(pair.Key);
                }
            }

            for (var i = 0; i < properties.Length; ++i)
            {
                // Evaluate before StartTime too, so backward seeks restore the captured defaults.
                properties[i].UpdateProperty(time);
            }

            // Publish descriptor changes to the renderer's shader globals.
            Context.NotifyBloomFogParamsChanged();
        }

        // Named tracks survive map loads; their animations and captured defaults must not.
        public void ResetForMapLoad()
        {
            animatedProperties.Clear();
            properties = Array.Empty<IAnimateProperty>();
            baselineCaptured = false;
            hasComponentEvents = false;
            hasFogComponentTarget = false;
            legacyBinding = null;
            controlsLegacyBinding = false;
            enabled = false;
        }

        private AnimateProperty<float> GetProperty(string key)
        {
            if (!animatedProperties.TryGetValue(key, out var prop))
            {
                prop = new AnimateProperty<float>(
                    new List<PointDefinition<float>>(),
                    value => WriteParam(key, value),
                    0f);
                animatedProperties[key] = prop;
            }

            return prop;
        }

        private float ReadParam(string key)
        {
            var parameters = Context.Descriptor.BloomFogParams;
            return key switch
            {
                "attenuation" => parameters.Attenuation,
                "offset" => parameters.Offset,
                "height" => parameters.Height,
                "startY" => parameters.StartY,
                _ => 0f
            };
        }

        private void WriteParam(string key, float value)
        {
            var parameters = Context.Descriptor.BloomFogParams;
            switch (key)
            {
                case "attenuation":
                    parameters.Attenuation = value;
                    break;
                case "offset":
                    parameters.Offset = value;
                    break;
                case "height":
                    parameters.Height = value;
                    break;
                case "startY":
                    parameters.StartY = value;
                    break;
            }
        }
    }

    // Keep assignments in beat order so playback and backward seeks choose the same fog track.
    public sealed class LegacyFogBinding
    {
        private readonly List<(float Time, FogAnimator Animator)> assignments = new();
        private readonly BeatmapRuntimeContext context;
        private bool baselineCaptured;
        private float attenuation;
        private float offset;
        private float height;
        private float startY;

        public LegacyFogBinding(BeatmapRuntimeContext context) => this.context = context;

        public void Add(float time, FogAnimator animator) => assignments.Add((time, animator));

        public void PushAt(float time)
        {
            if (!baselineCaptured)
            {
                var parameters = context.Descriptor.BloomFogParams;
                attenuation = parameters.Attenuation;
                offset = parameters.Offset;
                height = parameters.Height;
                startY = parameters.StartY;
                baselineCaptured = true;
            }

            var low = 0;
            var high = assignments.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (assignments[middle].Time <= time) low = middle + 1;
                else high = middle;
            }

            if (low > 0)
            {
                assignments[low - 1].Animator.PushLegacyValuesAt(time);
                return;
            }

            var fog = context.Descriptor.BloomFogParams;
            fog.Attenuation = attenuation;
            fog.Offset = offset;
            fog.Height = height;
            fog.StartY = startY;
            context.NotifyBloomFogParamsChanged();
        }
    }
}
