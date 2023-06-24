using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Animations
{
    public class TrackAnimator : MonoBehaviour
    {
        public AudioTimeSyncController Atsc;
        public Track track;
        public ObjectAnimator animator;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        private IAnimateProperty[] properties = new IAnimateProperty[0];

        public List<TrackAnimator> parents = new List<TrackAnimator>();
        public List<ObjectAnimator> children = new List<ObjectAnimator>();
        public ObjectAnimator[] cachedChildren = new ObjectAnimator[] {};

        public void SetEvents(List<BaseCustomEvent> events)
        {
            foreach (var ev in events)
            {
                foreach (var jprop in ev.Data)
                {
                    var p = new IPointDefinition.UntypedParams
                    {
                        key = jprop.Key,
                        points = jprop.Value,
                        easing = ev.DataEasing,
                        time = ev.JsonTime,
                        duration = ev.DataDuration ?? 0,
                        time_begin = ev.JsonTime,
                        time_end = ev.JsonTime + (ev.DataDuration ?? 0),
                        repeat = ev.DataRepeat ?? 0
                    };
                    AddPointDef(p, jprop.Key);
                }
            }

            properties = new IAnimateProperty[AnimatedProperties.Count];
            var i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            Update();
        }

        private bool preload = false;

        public void Update()
        {
            var time = Atsc?.CurrentJsonTime ?? 0;
            if (cachedChildren.Length == 0)
            {
                enabled = false;
                if (animator != null) animator.enabled = false;
                return;
            }
            for (var i = 0; i < properties.Length; ++i)
            {
                var prop = properties[i];
                if (time >= prop.StartTime)
                {
                    prop.UpdateProperty(time);
                }
            }
        }

        public void OnChildrenChanged()
        {
            cachedChildren = children.Where(o => o.enabled).ToArray();
            enabled = cachedChildren.Length > 0;
            if (animator != null) animator.enabled = enabled;
            parents.ForEach((t) => t.OnChildrenChanged());
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key)
        {
            switch (key)
            {
            case "_dissolve":
            case "dissolve":
                AddPointDef<float>((ObjectAnimator animator, float f) => animator.Opacity.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_dissolveArrow":
            case "dissolveArrow":
                AddPointDef<float>((ObjectAnimator animator, float f) => animator.OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Quaternion>((ObjectAnimator animator, Quaternion v) => animator.LocalRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_rotation":
            case "rotation":
            case "offsetWorldRotation":
                AddPointDef<Quaternion>((ObjectAnimator animator, Quaternion v) => animator.WorldRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_position":
                AddPointDef<Vector3>((ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "offsetPosition":
            case "localPosition":
                AddPointDef<Vector3>((ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v * 1.667f), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "position":
                AddPointDef<Vector3>((ObjectAnimator animator, Vector3 v) => animator.WorldPosition.Add(v * 1.667f), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>((ObjectAnimator animator, Vector3 v) => animator.Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
                break;
            case "_color":
            case "color":
                AddPointDef<Color>((ObjectAnimator animator, Color v) => animator.Colors.Add(v), PointDataParsers.ParseColor, p, Color.white);
                break;
            case "_time":
            case "time":
                AddPointDef<float>((ObjectAnimator animator, float f) => animator.SetLifeTime(f), PointDataParsers.ParseFloat, p, -1);
                break;
            }
        }

        private void AddPointDef<T>(Action<ObjectAnimator, T> _setter, PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, T _default) where T : struct
        {
            Action<T> setter = (v) => { for (var i = 0; i < cachedChildren.Length; ++i) { _setter(cachedChildren[i], v); } };

            GetAnimateProperty<T>(p.key, setter, _default).AddPointDef(parser, p);
        }

        private AnimateProperty<T> GetAnimateProperty<T>(string key, Action<T> setter, T _default) where T : struct
        {
            if (!AnimatedProperties.ContainsKey(key)) {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>(),
                    setter,
                    _default
                );
            }
            return AnimatedProperties[key] as AnimateProperty<T>;
        }
    }
}
