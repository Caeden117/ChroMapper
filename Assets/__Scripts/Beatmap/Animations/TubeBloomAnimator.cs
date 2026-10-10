using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    public class TubeBloomAnimator : MonoBehaviour
    {
        private const string ComponentName = "TubeBloomPrePassLight";

        public AudioTimeSyncController Atsc;

        private readonly Dictionary<string, AnimateProperty<float>> animatedProperties = new();
        private readonly Dictionary<string, float[]> baselines = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();
        private ParametricBloomFogLightController[] controllers = Array.Empty<ParametricBloomFogLightController>();
        private TrackAnimator trackAnimator;
        private bool resolved;

        private void Awake() => trackAnimator = GetComponent<TrackAnimator>();

        public void AddEvent(BaseCustomEvent ev)
        {
            if (ev.Data?[ComponentName] is not JSONObject component) return;

            foreach (var jprop in component)
            {
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
                    case "colorAlphaMultiplier":
                    case "bloomFogIntensityMultiplier":
                        break;
                    default:
                        continue;
                }

                var p = new IPointDefinition.UntypedParams
                {
                    Key = jprop.Key,
                    Points = jprop.Value,
                    Easing = ev.DataEasing,
                    Time = ev.JsonTime,
                    Duration = ev.DataDuration ?? 0,
                    TimeBegin = ev.JsonTime,
                    TimeEnd = ev.JsonTime + (ev.DataDuration ?? 0),
                    Repeat = ev.DataRepeat ?? 0
                };

                GetProperty(jprop.Key).AddPointDef(PointDataParsers.ParseFloat, p, ev);
            }

            RefreshProperties();
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
                    RestoreBaseline(key);
                    animatedProperties.Remove(key);
                }
            }

            RefreshProperties();
            RefreshControllers();
            PushOnStoppedTimeChanged();
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

            enabled = properties.Length > 0;
        }

        private void Update() => PushAt(Atsc != null ? Atsc.CurrentJsonTime : 0);

        // Apply stopped-time seeks before the frame renders.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc == null || Atsc.IsPlaying) return;
            PushAt(Atsc.CurrentJsonTime);
        }

        private void PushAt(float time)
        {
            if (properties.Length == 0) return;

            // Resolve lights after enhancements have spawned them, and save their authored values for backward seeks.
            if (!resolved)
            {
                ResolveControllers();
            }

            if (controllers.Length == 0) return;

            foreach (var pair in animatedProperties)
            {
                if (time >= pair.Value.StartTime)
                {
                    pair.Value.UpdateProperty(time);
                }
                else
                {
                    RestoreBaseline(pair.Key);
                }
            }

            RefreshControllers();
        }

        private void RefreshControllers()
        {
            for (var i = 0; i < controllers.Length; ++i)
            {
                var controller = controllers[i];
                if (controller.HasInitialized && !controller.UpdateAlways && controller.ShouldRefresh)
                {
                    controller.Refresh();
                }
            }
        }

        private void ResolveControllers()
        {
            if (trackAnimator.Children.Count == 0)
            {
                return;
            }

            resolved = true;

            controllers = trackAnimator.Children
                .Select(animator => animator.LocalTarget)
                .Where(target => target != null)
                .SelectMany(target => target.GetComponentsInChildren<ParametricBloomFogLightController>(true))
                .Distinct()
                .ToArray();

            foreach (var key in animatedProperties.Keys)
            {
                baselines[key] = controllers.Select(controller => ReadParam(controller, key)).ToArray();
                if (animatedProperties.TryGetValue(key, out var prop) && controllers.Length > 0)
                {
                    prop.Default = baselines[key][0];
                }
            }
        }

        // Named tracks survive map loads; discard the previous map's lights, animations, and captured values.
        public void ResetForMapLoad()
        {
            animatedProperties.Clear();
            baselines.Clear();
            properties = Array.Empty<IAnimateProperty>();
            controllers = Array.Empty<ParametricBloomFogLightController>();
            resolved = false;
            enabled = false;
        }

        public void RefreshBpmTiming()
        {
            foreach (var property in properties)
            {
                property.RefreshBpmTiming();
            }

            PushOnStoppedTimeChanged();
        }

        private AnimateProperty<float> GetProperty(string key)
        {
            if (!animatedProperties.TryGetValue(key, out var prop))
            {
                // Re-adding a deleted event must retain the authored values instead of capturing its last animated write.
                if (resolved && !baselines.ContainsKey(key))
                {
                    baselines[key] = controllers.Select(controller => ReadParam(controller, key)).ToArray();
                }

                prop = new AnimateProperty<float>(
                    new List<PointDefinition<float>>(),
                    value => WriteParam(key, value),
                    baselines.TryGetValue(key, out var values) && values.Length > 0 ? values[0] : 0f);
                animatedProperties[key] = prop;
            }

            return prop;
        }

        private float ReadParam(ParametricBloomFogLightController controller, string key) => key switch
        {
            "colorAlphaMultiplier" => controller.ColorAlphaMultiplier,
            "bloomFogIntensityMultiplier" => controller.BloomFogIntensityMultiplier,
            _ => 0f
        };

        // Lights can have different authored values. Restore each light's own value before its first event.
        private void RestoreBaseline(string key)
        {
            if (!baselines.TryGetValue(key, out var values)) return;
            for (var i = 0; i < controllers.Length && i < baselines[key].Length; ++i)
            {
                WriteParam(controllers[i], key, baselines[key][i]);
            }
        }

        private void WriteParam(string key, float value)
        {
            for (var i = 0; i < controllers.Length; ++i)
            {
                WriteParam(controllers[i], key, value);
            }
        }

        private static void WriteParam(ParametricBloomFogLightController controller, string key, float value)
        {
            switch (key)
            {
                case "colorAlphaMultiplier":
                    controller.SetColorAlphaMultiplier(value);
                    break;
                case "bloomFogIntensityMultiplier":
                    controller.SetBloomFogIntensityMultiplier(value);
                    break;
            }
        }
    }
}
