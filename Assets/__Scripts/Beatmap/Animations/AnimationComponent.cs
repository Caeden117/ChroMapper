using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Animations
{
    public class AnimationComponent : MonoBehaviour
    {
        [SerializeField] private GameObject AnimationObject;

        public AudioTimeSyncController Atsc;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();

        public void SetData(BaseGrid obj)
        {
            AnimationObject.transform.localEulerAngles = Vector3.zero;
            AnimationObject.transform.localPosition = Vector3.zero;

            // TODO: dictionary
            AnimatedProperties = new Dictionary<string, IAnimateProperty>();

            // TODO: object-specific bpn/njs/offset
            var njs = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var offset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
            var bpm = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                ?.FindLastBpm(obj.SongBpmTime)
                ?.Bpm ?? 0;
            var _hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);

            var half_path_duration = _hjd + ((obj is BaseObstacle obs) ? (obs.Duration / 2.0f) : 0);

            if (obj.CustomTrack != null)
            {
                var events = BeatmapObjectContainerCollection
                    .GetCollectionForType(ObjectType.CustomEvent)
                    .LoadedObjects
                    .Select(ev => ev as BaseCustomEvent);
                foreach (var ce in events.Where(ev => ev.Type == "AssignPathAnimation"))
                {
                    if (TracksMatch(obj.CustomTrack, ce.CustomTrack))
                    {
                        foreach (var jprop in ce.Data)
                        {
                            UntypedParams p = new UntypedParams
                            {
                                key = jprop.Key,
                                overwrite = false,
                                points = jprop.Value,
                                time = ce.JsonTime,
                                transition = ce.DataDuration ?? 0,
                                start = obj.JsonTime - half_path_duration,
                                end = obj.JsonTime + half_path_duration
                            };
                            switch (jprop.Key) {
                            case "_localRotation":
                            case "localRotation":
                                AddPointDef<Vector3>(LocalRotationSetter, PointDataParsers.ParseVector3, p);
                                break;
                            case "_position":
                            case "offsetPosition":
                                AddPointDef<Vector3>(PositionOffsetSetter, PointDataParsers.ParseVector3, p);
                                break;
                            }
                        }
                    }
                }
                foreach (var ce in events.Where(ev => ev.Type == "AnimateTrack"))
                {
                    if (TracksMatch(obj.CustomTrack, ce.CustomTrack))
                    {
                        foreach (var jprop in ce.Data)
                        {
                            UntypedParams p = new UntypedParams
                            {
                                key = jprop.Key,
                                overwrite = false,
                                points = jprop.Value,
                                time = ce.JsonTime,
                                start = ce.JsonTime,
                                end = ce.JsonTime + (ce.DataDuration ?? 0)
                            };
                            switch (jprop.Key) {
                            case "_localRotation":
                            case "localRotation":
                                AddPointDef<Vector3>(LocalRotationSetter, PointDataParsers.ParseVector3, p);
                                break;
                            case "_position":
                            case "offsetPosition":
                                AddPointDef<Vector3>(PositionOffsetSetter, PointDataParsers.ParseVector3, p);
                                break;
                            }
                        }
                    }
                }
            }

            // Individual Path Animation
            if (obj.CustomAnimation != null)
            {
                foreach (var jprop in obj.CustomAnimation.AsObject)
                {
                    UntypedParams p = new UntypedParams
                    {
                        key = jprop.Key,
                        overwrite = true,
                        points = jprop.Value,
                        start = obj.JsonTime - half_path_duration,
                        end = obj.JsonTime + half_path_duration
                    };
                    switch (jprop.Key)
                    {
                    case "_localRotation":
                    case "localRotation":
                        AddPointDef<Vector3>(LocalRotationSetter, PointDataParsers.ParseVector3, p);
                        break;
                    case "_position":
                    case "offsetPosition":
                        AddPointDef<Vector3>(PositionOffsetSetter, PointDataParsers.ParseVector3, p);
                        break;
                    }
                }
            }
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

        private void LocalRotationSetter(Vector3 v) => AnimationObject.transform.localEulerAngles = v;
        private void PositionOffsetSetter(Vector3 v) => AnimationObject.transform.localPosition = v * 0.7f;

        // My laziness knows no bounds
        private class UntypedParams
        {
            public string key;
            public bool overwrite;
            public JSONNode points;
            public float time = 0;
            public float transition = 0;
            public float start;
            public float end;
        }

        private void AddPointDef<T>(Action<T> setter, PointDefinition<T>.Parser parser, UntypedParams p) where T : struct
        {
            var pointdef = new PointDefinition<T>(
                parser,
                p.points switch {
                    JSONArray arr => arr,
                    JSONString pd => BeatSaberSongContainer.Instance.Map.PointDefinitions[pd],
                    _ => new JSONArray(),
                },
                p.time,
                p.transition,
                Easing.Linear, // TODO
                p.start,
                p.end
            );
            if (p.overwrite)
            {
                AnimatedProperties[p.key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>() { pointdef },
                    setter);
            }
            else
            {
                GetAnimateProperty<T>(p.key, setter).PointDefinitions.Add(pointdef);
            }
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

        private bool TracksMatch(JSONNode t1, JSONNode t2)
        {
            List<string> l1 = t1 switch {
                JSONString s => new List<string> { s },
                JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
                _ => new List<string>()
            };

            List<string> l2 = t2 switch {
                JSONString s => new List<string> { s },
                JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
                _ => new List<string>()
            };

            return l1.Any(a => l2.Any(b => a == b));
        }
    }
}
