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
                UntypedParams p = new UntypedParams
                {
                    key = jprop.Key,
                    points = jprop.Value,
                    easing = ev.DataEasing,
                    time = ev.JsonTime,
                    start = ev.JsonTime,
                    end = ev.JsonTime + (ev.DataDuration ?? 0)
                };
                AddPointDef(p, jprop.Key);
            }

            Update();
        }

        public List<ObjectAnimator> GetChildren()
        {
            var list = new List<ObjectAnimator>();
            list.AddRange(children);
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

        private class UntypedParams
        {
            public string key;
            public JSONNode points;
            public string easing;
            public float time = 0;
            public float transition = 0;
            public float start;
            public float end;
            // TODO: Repeat
        }

        private void AddPointDef(UntypedParams p, string key)
        {
            switch (key)
            {
            case "_dissolve":
                AddPointDef<float>((float f) => {
                    foreach (var animator in cachedChildren)
                    {
                        animator.Dissolve.Add(f);
                    }
                }, PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in cachedChildren)
                    {
                        animator.LocalRotation.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            case "_rotation":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in cachedChildren)
                    {
                        animator.WorldRotation.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            case "_position":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in cachedChildren)
                    {
                        animator.OffsetPosition.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            case "_scale":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in cachedChildren)
                    {
                        animator.Scale.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p, Vector3.one);
                break;
            case "_time":
                AddPointDef<float>((float f) => {
                    foreach (var animator in cachedChildren)
                    {
                        animator.SetLifeTime(f);
                    }
                }, PointDataParsers.ParseFloat, p, -1);
                break;
            }
        }

        private void AddPointDef<T>(Action<T> setter, PointDefinition<T>.Parser parser, UntypedParams p, T _default = default(T)) where T : struct
        {
            var pointdef = new PointDefinition<T>(
                parser,
                p.points switch {
                    JSONArray arr => arr,
                    JSONString pd => BeatSaberSongContainer.Instance.Map.PointDefinitions[pd],
                    _ => new JSONArray(), // TODO: Does this unset properly?
                },
                p.time,
                p.transition,
                Easing.Named(p.easing ?? "easeLinear"),
                p.start,
                p.end
            );

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
