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
    public class AnimationComponent : MonoBehaviour
    {
        [SerializeField] private GameObject AnimationThis;
        [SerializeField] private ObjectContainer container;

        public Track AnimationTrack;
        public AudioTimeSyncController Atsc;
        public TracksManager tracksManager;

        public bool AnimatedTrack { get { return AnimationTrack != null; } }

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();

        public void ResetData()
        {
            AnimatedProperties = new Dictionary<string, IAnimateProperty>();

            if (AnimatedTrack)
            {
                GameObject.Destroy(AnimationTrack);
                AnimationTrack = null;
            }

            AnimationThis.transform.localEulerAngles = Vector3.zero;
            AnimationThis.transform.localPosition = Vector3.zero;
            AnimationThis.transform.localScale = Vector3.one;
            container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", 1);
            container.UpdateMaterials();
        }

        public void SetData(BaseGrid obj)
        {
            ResetData();

            // TODO: object-specific bpm/njs/offset
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
                    RequireAnimationTrack();
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
                            AddPointDef(p, jprop.Key);
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
                                end = ce.JsonTime + (ce.DataDuration ?? 0),
                                track = true
                            };
                            AddPointDef(p, jprop.Key);
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
                    AddPointDef(p, jprop.Key);
                }
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
                if (AnimationTrack != null) {
                    AnimationTrack.UpdatePosition(-1 * time * EditorScaleController.EditorScale);
                }
            }
            lastTime = time;
        }

        private void RequireAnimationTrack()
        {
            if (AnimationTrack == null)
            {
                AnimationTrack = tracksManager.CreateAnimationTrack(container.ObjectData as BaseGrid);
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
            public float time = 0;
            public float transition = 0;
            public float start;
            public float end;
            public bool track = false;
            // TODO: Repeat
        }

        private void AddPointDef(UntypedParams p, string key)
        {
            var target = p.track ? AnimationTrack.ObjectParentTransform.gameObject : AnimationThis.gameObject;
            switch (key)
            {
            case "_dissolve":
            case "dissolve":
                AddPointDef<float>((float f) => { container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", f); container.UpdateMaterials(); }, PointDataParsers.ParseFloat, p);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Vector3>((Vector3 v) => target.transform.localEulerAngles = v, PointDataParsers.ParseVector3, p);
                break;
            case "_rotation":
            case "offsetWorldRotation":
                RequireAnimationTrack();
                AddPointDef<Vector3>((Vector3 v) => AnimationTrack.transform.localEulerAngles = v, PointDataParsers.ParseVector3, p);
                break;
            case "_position":
            case "offsetPosition":
                AddPointDef<Vector3>((Vector3 v) => target.transform.localPosition = v, PointDataParsers.ParseVector3, p);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>((Vector3 v) => target.transform.localScale = v, PointDataParsers.ParseVector3, p);
                break;
            }
        }

        private void AddPointDef<T>(Action<T> setter, PointDefinition<T>.Parser parser, UntypedParams p) where T : struct
        {
            var key = p.track ? $"track_{p.key}" : p.key;
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
            if (p.overwrite)
            {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>() { pointdef },
                    setter);
            }
            else
            {
                GetAnimateProperty<T>(key, setter).PointDefinitions.Add(pointdef);
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
