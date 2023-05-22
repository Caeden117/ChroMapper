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

        public void AddEvent(BaseCustomEvent ev)
        {
            foreach (var jprop in ev.Data)
            {
                UntypedParams p = new UntypedParams
                {
                    key = jprop.Key,
                    points = jprop.Value,
                    time = ev.JsonTime,
                    start = ev.JsonTime,
                    end = ev.JsonTime + (ev.DataDuration ?? 0)
                };
                AddPointDef(p, jprop.Key);
            }

            Update();
        }

        private float lastTime = -1;
        private readonly float minTime = 0.001f;

        public void Update()
        {
            var time = Atsc?.CurrentBeat ?? 0;
            if (Math.Abs(time - lastTime) > minTime) {
                foreach (var prop in AnimatedProperties)
                {
                    prop.Value.UpdateProperty(time);
                }
            }
            lastTime = time;
        }

        public void Preload(ObjectAnimator animator)
        {
            var backup = children;
            children = new List<ObjectAnimator>() { animator };
            Update();
            children = backup;
        }

        private class UntypedParams
        {
            public string key;
            public JSONNode points;
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
            case "dissolve":
                AddPointDef<float>((float f) => {
                    foreach (var animator in children)
                    {
                        animator.Dissolve.Add(f);
                    }
                }, PointDataParsers.ParseFloat, p);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in children)
                    {
                        animator.LocalRotation.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            case "_rotation":
            case "offsetWorldRotation":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in children)
                    {
                        animator.WorldRotation.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            case "_position":
            case "offsetPosition":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in children)
                    {
                        animator.OffsetPosition.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>((Vector3 v) => {
                    foreach (var animator in children)
                    {
                        animator.Scale.Add(v);
                    }
                }, PointDataParsers.ParseVector3, p);
                break;
            }
        }

        private void AddPointDef<T>(Action<T> setter, PointDefinition<T>.Parser parser, UntypedParams p) where T : struct
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
                Easing.Linear, // TODO
                p.start,
                p.end
            );

            GetAnimateProperty<T>(p.key, setter).PointDefinitions.Add(pointdef);
        }

        private AnimateProperty<T> GetAnimateProperty<T>(string key, Action<T> setter) where T : struct
        {
            if (!AnimatedProperties.ContainsKey(key)) {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>(),
                    setter
                );
            }
            return AnimatedProperties[key] as AnimateProperty<T>;
        }
    }
}
