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

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        public List<ObjectAnimator> children = new List<ObjectAnimator>();
        public List<ObjectAnimator> cachedChildren = new List<ObjectAnimator>();
        public List<TrackAnimator> childTracks = new List<TrackAnimator>();

        public void AddEvent(BaseCustomEvent ev)
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
                    time_end = ev.JsonTime + (ev.DataDuration ?? 0)
                };
                AddPointDef(p, jprop.Key);
            }

            Update();
        }

        public List<ObjectAnimator> GetChildren()
        {
            var list = new List<ObjectAnimator>();
            children
                .Where((child) => child.isActiveAndEnabled).ToList()
                .ForEach((child) => list.Add(child));
            foreach (var child in childTracks)
            {
                list.AddRange(child.GetChildren());
            }
            return list;
        }

        private float lastTime = -1;
        private readonly float minTime = 0.001f;
        private bool preload = false;

        public void Update()
        {
            var time = Atsc?.CurrentBeat ?? 0;
            if (preload || Math.Abs(time - lastTime) > minTime)
            {
                if (!preload)
                {
                    cachedChildren = GetChildren().Distinct().ToList();

                }
                foreach (var prop in AnimatedProperties)
                {
                    prop.Value.UpdateProperty(time);
                }
                lastTime = time;
            }
        }

        // This sucks
        public void Preload(ObjectAnimator animator)
        {
            cachedChildren = new List<ObjectAnimator>() { animator };
            preload = true;
            Update();
            preload = false;
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key)
        {
            switch (key)
            {
            case "_dissolve":
                AddPointDef<float>((ObjectAnimator animator, float f) => animator.Dissolve.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
                AddPointDef<Quaternion>((ObjectAnimator animator, Quaternion v) => animator.LocalRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_rotation":
                AddPointDef<Quaternion>((ObjectAnimator animator, Quaternion v) => animator.WorldRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_position":
                AddPointDef<Vector3>((ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_scale":
                AddPointDef<Vector3>((ObjectAnimator animator, Vector3 v) => animator.Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
                break;
            case "_time":
                AddPointDef<float>((ObjectAnimator animator, float f) => animator.SetLifeTime(f), PointDataParsers.ParseFloat, p, -1);
                break;
            }
        }

        private void AddPointDef<T>(Action<ObjectAnimator, T> _setter, PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, T _default) where T : struct
        {
            var pointdef = new PointDefinition<T>(parser, p);

            Action<T> setter = (v) => cachedChildren.ForEach((animator) => _setter(animator, v));

            GetAnimateProperty<T>(p.key, setter, _default).PointDefinitions.Add(pointdef);
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
