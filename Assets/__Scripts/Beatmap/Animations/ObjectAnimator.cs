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
        [SerializeField] public GameObject AnimationThis;
        [SerializeField] private ObjectContainer container;

        public Track AnimationTrack;
        public AudioTimeSyncController Atsc;
        public TracksManager tracksManager;

        [SerializeField] public Transform LocalTarget;
        public Transform WorldTarget;

        public List<Quaternion> LocalRotation;
        public List<Quaternion> WorldRotation;
        public List<Vector3> OffsetPosition;
        public List<Vector3> WorldPosition;
        public List<Vector3> Scale;
        public List<Color> Colors;
        public List<float> Opacity;
        public List<float> OpacityArrow;

        public bool AnimatedTrack { get; private set; } = false;
        public bool AnimatedLife { get; private set; } = false;
        public bool ShouldRecycle;

        private List<TrackAnimator> tracks = new List<TrackAnimator>();

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        private IAnimateProperty[] properties = new IAnimateProperty[0];

        private Vector3? wallSize = null;
        private Vector3? wallPosition = null;
        private Quaternion? localRotation = null;
        private Quaternion? worldRotation = null;

        public void ResetData()
        {
            AnimatedProperties = new Dictionary<string, IAnimateProperty>();
            properties = new IAnimateProperty[0];

            foreach (var track in tracks)
            {
                track.children.Remove(this);
                track.OnChildrenChanged();
            }
            tracks.Clear();

            if (AnimatedTrack)
            {
                if (container.transform.IsChildOf(AnimationTrack.transform))
                {
                    var track = tracksManager.GetTrackAtTime(container.ObjectData?.JsonTime ?? 0);
                    track.AttachContainer(container);
                }
                GameObject.Destroy(AnimationTrack.gameObject);
                AnimationTrack = null;
                AnimatedTrack = false;
            }

            LocalRotation = new List<Quaternion>();
            WorldRotation = new List<Quaternion>();
            OffsetPosition = new List<Vector3>();
            WorldPosition = new List<Vector3>();
            Scale = new List<Vector3>();
            Colors = new List<Color>();
            Opacity = new List<float>();
            OpacityArrow = new List<float>();

            _time = null;
            AnimatedLife = false;
            ShouldRecycle = false;

            wallSize = null;
            wallPosition = null;
            localRotation = null;
            worldRotation = null;

            if (LocalTarget != null)
            {
                LocalTarget.localEulerAngles = Vector3.zero;
                LocalTarget.localPosition = Vector3.zero;
                LocalTarget.localScale = Vector3.one;
            }

            if (container?.ObjectData != null)
            {
                container.UpdateGridPosition();
                container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", 1);
                container.MaterialPropertyBlock.SetFloat("_AnimationSpawned", 0);
                container.MaterialPropertyBlock.SetFloat("_AlwaysOpaque", 0);
                if (container is NoteContainer nc)
                {
                    nc.arrowMaterialPropertyBlock.SetFloat("_OpaqueAlpha", 1);
                }
                container.UpdateMaterials();
            }
        }

        private void OnDisable()
        {
            if (container?.ObjectData == null)
            {
                foreach (var track in tracks)
                {
                    track.children.Remove(this);
                    track.OnChildrenChanged();
                }
                tracks.Clear();
            }
        }

        public void SetData(BaseGrid obj)
        {
            ResetData();

            var isAnimated =
                    obj.CustomData != null
                && (obj.CustomTrack != null || obj.CustomAnimation != null);
            enabled = isAnimated;
            if (!isAnimated) return;

            var njs = obj.CustomNoteJumpMovementSpeed?.AsFloat
                ?? BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var offset = obj.CustomNoteJumpStartBeatOffset?.AsFloat
                ?? BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
            var bpm = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                ?.FindLastBpm(obj.SongBpmTime)
                ?.Bpm ?? BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var _hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);

            var duration = 0f;

            if (container is ObstacleContainer obs)
            {
                duration = obs.ObstacleData.Duration;
                (wallSize, wallPosition) = obs.ReadSizePosition();
            }
            if (obj.CustomLocalRotation is JSONNode rot)
                localRotation = Quaternion.Euler(rot.ReadVector3());
            if (obj.CustomWorldRotation is JSONNode wrot)
                worldRotation = Quaternion.Euler(wrot.ReadVector3());

            time_begin = obj.JsonTime - _hjd;
            time_end = obj.JsonTime + duration + _hjd;

            RequireAnimationTrack();
            WorldTarget = AnimationTrack.transform;

            if (obj.CustomTrack != null)
            {
                List<string> tracks = obj.CustomTrack switch {
                    JSONString s => new List<string> { s },
                    JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
                    _ => new List<string>()
                };
                foreach (var tr in tracks)
                {
                    var track = tracksManager.CreateAnimationTrack(tr);
                    track.children.Add(this);
                    track.Preload(this);
                    track.OnChildrenChanged();
                    this.tracks.Add(track);

                    List<BaseCustomEvent> events = null;

                    BeatmapObjectContainerCollection
                        .GetCollectionForType<CustomEventGridContainer>(ObjectType.CustomEvent)
                        .EventsByTrack
                        ?.TryGetValue(tr, out events);
                    if (events == null)
                    {
                        continue;
                    }
                    foreach (var ce in events.Where(ev => ev.Type == "AssignPathAnimation"))
                    {
                        foreach (var jprop in ce.Data)
                        {
                            var p = new IPointDefinition.UntypedParams
                            {
                                key = $"track_{jprop.Key}",
                                overwrite = false,
                                points = jprop.Value,
                                easing = ce.DataEasing,
                                time = ce.JsonTime,
                                transition = ce.DataDuration ?? 0,
                                time_begin = time_begin,
                                time_end = time_end,
                            };
                            AddPointDef(p, jprop.Key);
                        }
                    }
                }

                 var parent = tracksManager.CreateAnimationTrack(tracks[0]);
                 AnimationTrack.transform.SetParent(parent.track.ObjectParentTransform, false);
            }

            // Individual Path Animation
            if (obj.CustomAnimation != null)
            {
                foreach (var jprop in obj.CustomAnimation.AsObject)
                {
                    var p = new IPointDefinition.UntypedParams
                    {
                        key = jprop.Key,
                        overwrite = true,
                        points = jprop.Value,
                        easing = null,
                        time_begin = time_begin,
                        time_end = time_end,
                    };
                    AddPointDef(p, jprop.Key);
                }
            }

            properties = new IAnimateProperty[AnimatedProperties.Count];
            int i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            Update();
            LateUpdate();
        }

        public void SetTrack(Track track, string name)
        {
            ResetData();

            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;
        }

        private float? _time = null;
        private float time_begin;
        private float time_end;

        public void Update()
        {
            var time = _time ?? Atsc?.CurrentJsonTime ?? 0;

            if (container?.ObjectData is BaseGrid obj)
            {
                if (localRotation.HasValue)
                    LocalRotation.Insert(0, localRotation.Value);
                if (worldRotation.HasValue)
                    WorldRotation.Insert(0, worldRotation.Value);

                if (container is ObstacleContainer obs)
                {
                    OffsetPosition.Add(wallPosition.Value);
                    Scale.Add(wallSize.Value);
                }

                var NoodleAnimationLifetime = (time_begin > time || time > time_end) ? -1 : 1;
                //if (UIMode.SelectedMode == UIModeType.Playing || UIMode.SelectedMode == UIModeType.Preview)
                container?.MaterialPropertyBlock.SetFloat("_AnimationSpawned", NoodleAnimationLifetime);
                if (container is NoteContainer nc)
                {
                    nc.arrowMaterialPropertyBlock.SetFloat("_AnimationSpawned", NoodleAnimationLifetime);
                }
                AnimatedLife =
                       (_time != null && _time < obj.JsonTime)
                    || (WorldPosition.Count > 0)
                    || (obj.CustomFake && time < time_end);
                if (ShouldRecycle)
                {
                    var despawn_time = (WorldPosition.Count == 0 && !obj.CustomFake)
                        ? obj.JsonTime
                        : time_end;
                    if (time > despawn_time)
                    {
                        BeatmapObjectContainerCollection.GetCollectionForType(container.ObjectData.ObjectType).RecycleContainer(container.ObjectData);
                        AnimatedLife = false;
                        return;
                    }
                }
            }

            var l = properties.Length;
            for (var i = 0; i < l; ++i)
            {
                var prop = properties[i];
                if (time >= prop.StartTime)
                {
                    prop.UpdateProperty(time);
                }
            }

            if (AnimatedTrack) {
                var map_time = (_time == null)
                    ? Atsc.CurrentSongBpmTime
                    : BeatmapObjectContainerCollection
                        .GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                        .JsonTimeToSongBpmTime(time);
                AnimationTrack.UpdatePosition(-1 * map_time * EditorScaleController.EditorScale);
            }
        }

        public void LateUpdate()
        {
            LocalTarget.localRotation = Aggregate(LocalRotation, Quaternion.identity, (a, b) => a * b);

            LocalTarget.localPosition = Aggregate(OffsetPosition, Vector3.zero, (a, b) => a + b);

            LocalTarget.localScale = Aggregate(Scale, Vector3.one, (a, b) => Vector3.Scale(a, b));

            if (WorldTarget is Transform)
            {
                WorldTarget.localRotation = Aggregate(WorldRotation, Quaternion.identity, (a, b) => a * b);
            }
            if (WorldPosition.Count > 0)
            {
                AnimationTrack.UpdatePosition(0);
                container.transform.localPosition = Aggregate(WorldPosition, Vector3.zero, (a, b) => a + b);
            }
            if (container is ObjectContainer)
            {
                if (Colors.Count > 0)
                {
                    // SetColor is on both NoteContainer and ObstacleContainer, but not on an interface/base class >:( TODO
                    var color = Aggregate(Colors, Color.white, (a, b) => a * b);
                    (container as NoteContainer)?.SetColor(color);
                    (container as ObstacleContainer)?.SetColor(color);
                }

                if (container is NoteContainer nc)
                {
                    nc.arrowMaterialPropertyBlock.SetFloat("_OpaqueAlpha", Aggregate(OpacityArrow, 1.0f, (a, b) => a * b));
                }

                container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", Aggregate(Opacity, 1.0f, (a, b) => a * b));
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
                AnimationTrack.ObjectParentTransform.localPosition = new Vector3(container.transform.localPosition.x, container.transform.localPosition.y, 0);
                AnimationTrack.transform.localPosition = Vector3.zero;//new Vector3(0, 1.5f, 0);
                container.transform.localPosition = new Vector3(0, 0, container.transform.localPosition.z);
                AnimatedTrack = true;
            }
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key)
        {
            switch (key)
            {
            case "_dissolve":
            case "dissolve":
                AddPointDef<float>((float f) => Opacity.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_dissolveArrow":
            case "dissolveArrow":
                AddPointDef<float>((float f) => OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Quaternion>((Quaternion q) => LocalRotation.Add(q), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_rotation":
            case "offsetWorldRotation":
                AddPointDef<Quaternion>((Quaternion v) => WorldRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_position":
            case "offsetPosition":
            case "localPosition":
                AddPointDef<Vector3>((Vector3 v) => OffsetPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_definitePosition":
            case "definitePosition":
                AddPointDef<Vector3>((Vector3 v) => WorldPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>((Vector3 v) => Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
                break;
            case "_color":
            case "color":
                AddPointDef<Color>((Color c) => Colors.Add(c), PointDataParsers.ParseColor, p, Color.white);
                break;
            }
        }

        private void AddPointDef<T>(Action<T> setter, PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, T _default) where T : struct
        {
            try
            {
                var pointdef = new PointDefinition<T>(parser, p);
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
            catch (Exception e)
            {
                Debug.LogException(e);
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

        private T Aggregate<T>(List<T> list, T _default, Func<T, T, T> func)
        {
            if (list.Count == 0) return _default;
            var value = list[0];
            for (int i = 1; i < list.Count; ++i)
            {
                value = func(value, list[i]);
            }
            list.Clear();
            return value;
        }
    }
}
