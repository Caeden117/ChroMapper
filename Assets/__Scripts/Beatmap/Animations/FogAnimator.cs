using System.Collections.Generic;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    // Fog animation writes the preview renderer's environment parameters rather than object transforms.
    public class FogAnimator : MonoBehaviour
    {
        private const string ComponentKey = "BloomFogEnvironment";
        private const string AttenuationKey = "attenuation";
        private const string OffsetKey = "offset";
        private const string HeightKey = "height";
        private const string StartYKey = "startY";
        private const string LegacyAttenuationKey = "_attenuation";
        private const string LegacyOffsetKey = "_offset";
        private const string LegacyHeightKey = "_height";
        private const string LegacyStartYKey = "_startY";

        public AudioTimeSyncController Atsc;
        public BeatmapRuntimeContext Context;

        private readonly Dictionary<string, ComponentAnimationProperty> animatedProperties = new();
        private readonly Dictionary<string, float> baselines = new();
        // Keep a reusable snapshot so removing events can delete dictionary entries during iteration.
        private readonly List<KeyValuePair<string, ComponentAnimationProperty>> properties = new(4);
        private bool baselineCaptured;
        // Fog events take effect only when this track owns the fog component or is selected by AssignFogTrack.
        private bool hasComponentEvents;
        private bool hasFogComponentTarget;
        private LegacyFogBinding legacyBinding;
        private bool controlsLegacyBinding;

        public void AddEvent(BaseCustomEvent ev, bool refresh = true)
        {
            if (ev.Data?[ComponentKey] is not JSONObject component) return;

            hasComponentEvents = true;

            foreach (var jprop in component)
            {
                switch (jprop.Key)
                {
                    case AttenuationKey:
                    case OffsetKey:
                    case HeightKey:
                    case StartYKey:
                        break;
                    default:
                        continue;
                }

                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    GetProperty(jprop.Key).StopAt(ev);
                    continue;
                }

                AddProperty(jprop.Key, jprop.Value, ev);
            }

            if (refresh) RefreshProperties();
        }

        // V2 stores fog parameters on AnimateTrack; normalize their names to the V3 component names.
        public void AddLegacyEvent(BaseCustomEvent ev, bool refresh = true)
        {
            foreach (var jprop in ev.Data)
            {
                var key = jprop.Key switch
                {
                    LegacyAttenuationKey => AttenuationKey,
                    LegacyOffsetKey => OffsetKey,
                    LegacyHeightKey => HeightKey,
                    LegacyStartYKey => StartYKey,
                    _ => null
                };
                if (key == null) continue;

                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    animatedProperties.Remove(key);

                    continue;
                }

                AddProperty(key, jprop.Value, ev);
            }

            if (refresh) RefreshProperties();
        }

        public void CompleteEventLoad() => RefreshProperties();

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
            if (AnimateProperty<float>.SkipsMissingPointDefinition(p, ev))
                return;

            GetProperty(key).AddPointDef(PointDataParsers.ParseFloat, p, ev);
        }

        // Drop empty properties before RefreshProperties tries to sort them.
        public void RemoveEvent(BaseCustomEvent ev)
        {
            var ownsFog = hasFogComponentTarget
                || (legacyBinding != null && legacyBinding.IsAssignedTo(this, Atsc.CurrentJsonTime));
            foreach (var pair in properties)
            {
                var prop = pair.Value;
                prop.RemoveEvent(ev);
                if (prop.IsEmpty())
                {
                    if (ownsFog && baselines.TryGetValue(pair.Key, out var baseline))
                    {
                        WriteParam(pair.Key, baseline);
                    }

                    animatedProperties.Remove(pair.Key);
                }
            }

            RefreshProperties();
            Context.NotifyBloomFogParamsChanged();
            PushOnStoppedTimeChanged();
        }

        private void RefreshProperties()
        {
            properties.Clear();
            foreach (var pair in animatedProperties)
            {
                pair.Value.Sort();
                properties.Add(pair);
            }

            RefreshEnabled();
        }

        private void RefreshEnabled() =>
            enabled = (properties.Count > 0 && hasComponentEvents && hasFogComponentTarget)
                || controlsLegacyBinding;

        private void Update() => PushAt(Atsc != null ? Atsc.CurrentJsonTime : 0);

        // Apply stopped-time seeks before the frame renders.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc == null || Atsc.IsPlaying) return;
            PushAt(Atsc.CurrentJsonTime, true);
        }

        private void PushAt(float time, bool stoppedSeek = false)
        {
            if (controlsLegacyBinding)
            {
                legacyBinding.PushAt(time);
            }

            if (!hasComponentEvents || !hasFogComponentTarget || properties.Count == 0) return;

            PushValuesAt(time, stoppedSeek);
        }

        // The assignment timeline can read an inactive track without enabling its Update.
        public void PushLegacyValuesAt(float time)
        {
            PushValuesAt(time);
        }

        private void PushValuesAt(float time, bool stoppedSeek = false)
        {

            // Capture authored fog values after enhancements load, before the first animation write.
            // These become the defaults when seeking before the first event.
            if (!baselineCaptured)
            {
                baselineCaptured = true;
                foreach (var pair in animatedProperties)
                {
                    if (!baselines.TryGetValue(pair.Key, out var baseline))
                    {
                        baseline = ReadParam(pair.Key);
                        baselines.Add(pair.Key, baseline);
                    }

                    pair.Value.Default = baseline;
                }
            }

            for (var i = 0; i < properties.Count; ++i)
            {
                // Evaluate before StartTime too, so backward seeks restore the captured defaults.
                properties[i].Value.UpdateProperty(time, stoppedSeek);
            }

            // Publish descriptor changes to the renderer's shader globals.
            Context.NotifyBloomFogParamsChanged();
        }

        // Named tracks survive map loads; their animations and captured defaults must not.
        public void ResetForMapLoad()
        {
            animatedProperties.Clear();
            baselines.Clear();
            properties.Clear();
            baselineCaptured = false;
            hasComponentEvents = false;
            hasFogComponentTarget = false;
            legacyBinding = null;
            controlsLegacyBinding = false;
            enabled = false;
        }

        public void RefreshBpmTiming()
        {
            foreach (var pair in properties)
            {
                pair.Value.RefreshBpmTiming();
            }

            PushOnStoppedTimeChanged();
        }

        private ComponentAnimationProperty GetProperty(string key)
        {
            if (!animatedProperties.TryGetValue(key, out var prop))
            {
                prop = new ComponentAnimationProperty(
                    value => WriteParam(key, value),
                    baselines.TryGetValue(key, out var baseline) ? baseline : 0f);
                animatedProperties[key] = prop;
                baselineCaptured = false;
            }

            return prop;
        }

        private float ReadParam(string key)
        {
            var parameters = Context.Descriptor.BloomFogParams;
            return key switch
            {
                AttenuationKey => parameters.Attenuation,
                OffsetKey => parameters.Offset,
                HeightKey => parameters.Height,
                StartYKey => parameters.StartY,
                _ => 0f
            };
        }

        private void WriteParam(string key, float value)
        {
            var parameters = Context.Descriptor.BloomFogParams;
            switch (key)
            {
                case AttenuationKey:
                    parameters.Attenuation = value;
                    break;
                case OffsetKey:
                    parameters.Offset = value;
                    break;
                case HeightKey:
                    parameters.Height = value;
                    break;
                case StartYKey:
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

        private int GetAssignmentIndex(float time) =>
            assignments.AsSpan().UpperBoundBy(time, assignment => assignment.Time) - 1;

        internal bool IsAssignedTo(FogAnimator animator, float time)
        {
            var index = GetAssignmentIndex(time);
            return index >= 0 && assignments[index].Animator == animator;
        }

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

            var index = GetAssignmentIndex(time);

            // A replacement track may animate only some fog fields, or none. Start from the authored
            // environment parameters so the previous assignment cannot leave its values behind.
            RestoreBaselineValues();
            if (index >= 0)
            {
                assignments[index].Animator.PushLegacyValuesAt(time);
                return;
            }
            context.NotifyBloomFogParamsChanged();
        }

        public void RestoreBaseline()
        {
            if (!baselineCaptured)
                return;
            RestoreBaselineValues();
            context.NotifyBloomFogParamsChanged();
        }

        private void RestoreBaselineValues()
        {
            var fog = context.Descriptor.BloomFogParams;
            fog.Attenuation = attenuation;
            fog.Offset = offset;
            fog.Height = height;
            fog.StartY = startY;
        }
    }
}
