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
        public Track Track;
        public ObjectAnimator Animator;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        private IAnimateProperty[] properties = new IAnimateProperty[0];

        public List<TrackAnimator> Parents = new List<TrackAnimator>();
        public List<ObjectAnimator> Children = new List<ObjectAnimator>();
        public ObjectAnimator[] CachedChildren = new ObjectAnimator[] {};

        public void AddEvent(BaseCustomEvent ev)
        {
            foreach (var jprop in ev.Data)
            {
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
                AddPointDef(p, jprop.Key, ev);
            }

            RefreshProperties();
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            foreach (var prop in AnimatedProperties.Keys.ToList())
            {
                AnimatedProperties[prop].RemoveEvent(ev);
                if (AnimatedProperties[prop].IsEmpty())
                {
                    AnimatedProperties.Remove(prop);
                }
            }
            RefreshProperties();
        }

        private void RefreshProperties()
        {
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
            if (CachedChildren.Length == 0)
            {
                enabled = false;
                if (Animator != null) Animator.enabled = false;
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
            CachedChildren = Children.Where(o => o.enabled).ToArray();
            enabled = CachedChildren.Length > 0;
            if (Animator != null) Animator.enabled = enabled;
            Parents.ForEach((t) => t.OnChildrenChanged());
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
            case "_dissolve":
            case "dissolve":
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.Opacity.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_dissolveArrow":
            case "dissolveArrow":
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => animator.LocalRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_rotation":
            case "rotation":
            case "offsetWorldRotation":
                AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => animator.WorldRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_position":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "offsetPosition":
            case "localPosition":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v * 1.667f), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "position":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.WorldPosition.Add(v * 1.667f), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
                break;
            case "_color":
            case "color":
                AddPointDef<Color>(source, (ObjectAnimator animator, Color v) => animator.Colors.Add(v), PointDataParsers.ParseColor, p, Color.white);
                break;
            case "_time":
            case "time":
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.SetLifeTime(f), PointDataParsers.ParseFloat, p, -1);
                break;
            }
        }

        private void AddPointDef<T>(BaseCustomEvent source, Action<ObjectAnimator, T> _setter, PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, T _default) where T : struct
        {
            Action<T> setter = (v) => { for (var i = 0; i < CachedChildren.Length; ++i) { _setter(CachedChildren[i], v); } };

            GetAnimateProperty<T>(p.Key, setter, _default).AddPointDef(parser, p, source);
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
