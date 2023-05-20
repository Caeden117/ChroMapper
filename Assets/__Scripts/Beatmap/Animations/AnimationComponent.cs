using System.Collections.Generic;
using UnityEngine;

using Beatmap.Base;
using Beatmap.Enums;

namespace Beatmap.Animations
{
    public class AnimationComponent : MonoBehaviour
    {
        public AudioTimeSyncController Atsc;

        public List<IAnimateProperty> AnimatedProperties = new List<IAnimateProperty>();

        public void SetData(BaseGrid obj)
        {
            // TODO: dictionary
            AnimatedProperties = new List<IAnimateProperty>();

            // TODO: object-specific bpn/njs/offset
            var njs = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var offset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
            var bpm = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                .FindLastBpm(obj.SongBpmTime)
                .Bpm;
            var _hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);

            var half_path_duration = _hjd + ((obj is BaseObstacle obs) ? (obs.Duration / 2.0f) : 0);

            // Get from inline animation
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

                        // TODO: This shouldn't always add
                        AnimatedProperties.Add(new AnimateProperty<Vector3>(
                            new List<PointDefinition<Vector3>>() { pointdef },
                            (v) => transform.localEulerAngles = v));
                        break;
                    }
                }
            }
        }

        public void Update()
        {
            foreach (var prop in AnimatedProperties)
            {
                prop.UpdateProperty(Atsc.CurrentBeat);
            }
        }
    }
}
