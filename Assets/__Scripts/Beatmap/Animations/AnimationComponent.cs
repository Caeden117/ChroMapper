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
        public AudioTimeSyncController Atsc;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();

        public void SetData(BaseGrid obj)
        {
            // TODO: dictionary
            AnimatedProperties = new Dictionary<string, IAnimateProperty>();

            // TODO: object-specific bpn/njs/offset
            var njs = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var offset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
            var bpm = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                .FindLastBpm(obj.SongBpmTime)
                .Bpm;
            var _hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);

            var half_path_duration = _hjd + ((obj is BaseObstacle obs) ? (obs.Duration / 2.0f) : 0);

            if (obj.CustomTrack != null) {
                var events = BeatmapObjectContainerCollection
                    .GetCollectionForType(ObjectType.CustomEvent)
                    .LoadedObjects
                    .Select(ev => ev as BaseCustomEvent);
                var apa = events.Where(ev => ev.Type == "AssignPathAnimation");
                foreach (var ce in apa) {
                    if (TracksMatch(obj.CustomTrack, ce.CustomTrack)) {
                        foreach (var jprop in ce.Data) {
                            switch (jprop.Key) {
                            case "_localRotation":
                            case "localRotation":
                                Debug.Log($"AssignPathAnimation {ce.DataDuration}");
                                var pointdef = new PointDefinition<Vector3>(
                                    PointDataParsers.ParseVector3,
                                    jprop.Value.AsArray,
                                    ce.JsonTime,
                                    ce.DataDuration ?? 0,
                                    Easing.Linear,
                                    obj.JsonTime - half_path_duration,
                                    obj.JsonTime + half_path_duration
                                );

                                GetAnimateProperty<Vector3>(jprop.Key, LocalRotationSetter).PointDefinitions.Add(pointdef);
                                break;
                            }
                        }
                    }
                }
            }

            // TODO: AnimateTrack

            // Individual Path Animation
            if (obj.CustomAnimation != null)
            {
                foreach (var jprop in obj.CustomAnimation.AsObject) {
                    switch (jprop.Key)
                    {
                    case "_localRotation":
                    case "localRotation":
                        var pointdef = new PointDefinition<Vector3>(
                            PointDataParsers.ParseVector3,
                            jprop.Value.AsArray,
                            0, 0,
                            Easing.Linear,
                            obj.JsonTime - half_path_duration,
                            obj.JsonTime + half_path_duration
                        );

                        // Individual Path Animations overwrite AssignPathAnimation
                        AnimatedProperties[jprop.Key] = new AnimateProperty<Vector3>(
                            new List<PointDefinition<Vector3>>() { pointdef },
                            LocalRotationSetter);
                        break;
                    }
                }
            }
        }

        public void Update()
        {
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.UpdateProperty(Atsc.CurrentBeat);
            }
        }

        private void LocalRotationSetter(Vector3 v) => transform.localEulerAngles = v;

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
