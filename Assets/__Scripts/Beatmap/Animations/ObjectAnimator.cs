using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using SimpleJSON;

namespace Beatmap.Animations
{
    public class ObjectAnimator : MonoBehaviour
    {
        [SerializeField] private GameObject AnimationThis;
        [SerializeField] private ObjectContainer container;

        public Track AnimationTrack;
        public AudioTimeSyncController Atsc;
        public TracksManager tracksManager;

        public Transform LocalTarget;
        public Transform WorldTarget;

        public List<Vector3> LocalRotation;
        public List<Vector3> WorldRotation;
        public List<Vector3> OffsetPosition;
        public List<Vector3> Scale;
        public List<float> Dissolve;

        public bool AnimatedTrack { get { return AnimationTrack != null; } }
        private List<TrackAnimator> tracks = new List<TrackAnimator>();

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();

        public void ResetData()
        {
            AnimatedProperties = new Dictionary<string, IAnimateProperty>();

            foreach (var track in tracks) track.children.Remove(this);
            tracks.Clear();

            if (AnimatedTrack)
            {
                GameObject.Destroy(AnimationTrack.gameObject);
                AnimationTrack = null;
            }

            LocalRotation = new List<Vector3>();
            WorldRotation = new List<Vector3>();
            OffsetPosition = new List<Vector3>();
            Scale = new List<Vector3>();
            Dissolve = new List<float>();

            _time = null;

            AnimationThis.transform.localEulerAngles = Vector3.zero;
            AnimationThis.transform.localPosition = Vector3.zero;
            AnimationThis.transform.localScale = Vector3.one;
            container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", 1);
            container.UpdateMaterials();
        }

        public void SetData(BaseGrid obj)
        {
            ResetData();

            var isAnimated =
                    obj.CustomData != null
                && (obj.CustomTrack != null || obj.CustomAnimation != null);
            enabled = isAnimated;
            if (!isAnimated) return;

            var njs = obj.CustomNoteJumpMovementSpeed ?? BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var offset = obj.CustomNoteJumpStartBeatOffset ?? BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
            var bpm = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                ?.FindLastBpm(obj.SongBpmTime)
                ?.Bpm ?? BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var _hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);

            var half_path_duration = _hjd + ((obj is BaseObstacle obs) ? (obs.Duration / 2.0f) : 0);

            time_begin = obj.JsonTime - half_path_duration;
            time_end = obj.JsonTime + half_path_duration;

            RequireAnimationTrack();
            LocalTarget = AnimationThis.transform;
            WorldTarget = AnimationTrack.transform;

            if (obj.CustomTrack != null)
            {
                var events = BeatmapObjectContainerCollection
                    .GetCollectionForType(ObjectType.CustomEvent)
                    .LoadedObjects
                    .Select(ev => ev as BaseCustomEvent)
                    .Where(ev => ev.Type == "AssignPathAnimation");
                foreach (var ce in events)
                {
                    if (TracksMatch(obj.CustomTrack, ce.CustomTrack))
                    {
                        foreach (var jprop in ce.Data)
                        {
                            UntypedParams p = new UntypedParams
                            {
                                key = $"track_{jprop.Key}",
                                overwrite = false,
                                points = jprop.Value,
                                easing = ce.DataEasing,
                                time = ce.JsonTime,
                                transition = ce.DataDuration ?? 0,
                            };
                            AddPointDef(p, jprop.Key);
                        }
                    }
                }
                var tracks = new List<string>();
                switch (obj.CustomTrack)
                {
                case JSONArray arr:
                    foreach (var t in arr) tracks.Add(t.Value);
                    break;
                case JSONString s:
                    tracks.Add(s);
                    break;
                };
                if (obj is V2Object)
                {
                    foreach (var t in tracks) {
                        var track = tracksManager.CreateAnimationTrack(t);
                        track.children.Add(this);
                        track.Preload(this);
                        this.tracks.Add(track);
                    }
                }
                else
                {
                    // TODO: V3 uses parenting
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
                        easing = null,
                    };
                    AddPointDef(p, jprop.Key);
                }
            }

            Update();
            LateUpdate();
        }

        private float lastTime = -1;
        private readonly float minTime = 0.001f;
        private bool updateFrame = true;
        private float? _time = null;
        private float time_begin;
        private float time_end;

        public void Update()
        {
            var time = _time ?? Atsc?.CurrentBeat ?? 0;
            updateFrame = Math.Abs(time - lastTime) > minTime;
            if (updateFrame)
            {
                foreach (var prop in AnimatedProperties)
                {
                    prop.Value.UpdateProperty(time);
                }
                if (AnimatedTrack) {
                    AnimationTrack.UpdatePosition(-1 * time * EditorScaleController.EditorScale);
                }
                lastTime = time;
            }
        }

        public void LateUpdate()
        {
            if (updateFrame)
            {
                LocalTarget.localEulerAngles = AggrigateSum(ref LocalRotation, Vector3.zero);
                LocalTarget.localPosition = AggrigateSum(ref OffsetPosition, Vector3.zero) * 0.6f;
                LocalTarget.localScale = AggrigateMul(ref Scale, Vector3.one);
                WorldTarget.localEulerAngles = AggrigateSum(ref WorldRotation, Vector3.zero);
                container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", AggrigateMul(ref Dissolve, 1.0f));
                container.UpdateMaterials();
            }
        }

        public void SetLifeTime(float normalTime)
        {
            _time = (normalTime < 0)
                ? (float?)null
                : (float?)Mathf.LerpUnclamped(time_begin, time_end, normalTime);
        }

        private void RequireAnimationTrack()
        {
            if (AnimationTrack == null)
            {
                AnimationTrack = tracksManager.CreateIndividualTrack(container.ObjectData as BaseGrid);
                AnimationTrack.AttachContainer(container);
                AnimationTrack.transform.localPosition = new Vector3(container.transform.localPosition.x, container.transform.localPosition.y, 0);
                container.transform.localPosition = new Vector3(0, 0, container.transform.localPosition.z);
            }
        }

        // My laziness knows no bounds
        private class UntypedParams
        {
            public string key;
            public bool overwrite;
            public JSONNode points;
            public string easing;
            public float time = 0;
            public float transition = 0;
            // TODO: Repeat
        }

        private void AddPointDef(UntypedParams p, string key)
        {
            switch (key)
            {
            case "_dissolve":
            case "dissolve":
                AddPointDef<float>((float f) => Dissolve.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Vector3>((Vector3 v) => LocalRotation.Add(v), PointDataParsers.ParseVector3, p);
                break;
            case "_rotation":
            case "offsetWorldRotation":
                RequireAnimationTrack();
                AddPointDef<Vector3>((Vector3 v) => WorldRotation.Add(v), PointDataParsers.ParseVector3, p);
                break;
            case "_position":
            case "offsetPosition":
                AddPointDef<Vector3>((Vector3 v) => OffsetPosition.Add(v), PointDataParsers.ParseVector3, p);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>((Vector3 v) => Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
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
                true,
                Easing.Named(p.easing ?? "easeLinear"),
                time_begin,
                time_end
            );
            if (p.overwrite)
            {
                AnimatedProperties[p.key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>() { pointdef },
                    setter,
                    _default
                );
            }
            else
            {
                GetAnimateProperty<T>(p.key, setter, _default).PointDefinitions.Add(pointdef);
            }
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

        private T AggrigateSum<T>(ref List<T> list, T _default) where T : struct
        {
            if (list.Count == 0) return _default;
            dynamic value = list[0];
            for (int i = 1; i < list.Count; ++i)
            {
                value += list[i];
            }
            list.Clear();
            return value;
        }

        private float AggrigateMul(ref List<float> list, float _default = 1)
        {
            var value = _default;
            foreach (var it in list)
            {
                value *= it;
            }
            list.Clear();
            return value;
        }
        private Vector3 AggrigateMul(ref List<Vector3> list, Vector3 _default)
        {
            var value = _default;
            foreach (var it in list)
            {
                value = Vector3.Scale(value, it);
            }
            list.Clear();
            return value;
        }
    }
}
