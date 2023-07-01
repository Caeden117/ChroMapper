using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V2.Customs;
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

        public Aggregator<Quaternion> LocalRotation;
        public Aggregator<Quaternion> WorldRotation;
        public Aggregator<Vector3> OffsetPosition;
        public Aggregator<Vector3> WorldPosition;
        public Aggregator<Vector3> Scale;
        public Aggregator<Color> Colors;
        public Aggregator<float> Opacity;
        public Aggregator<float> OpacityArrow;

        public bool AnimatedTrack { get; private set; } = false;
        public bool AnimatedLife { get; private set; } = false;
        public bool ShouldRecycle;

        private List<TrackAnimator> tracks = new List<TrackAnimator>();

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        private IAnimateProperty[] properties = new IAnimateProperty[0];

        public void ResetData()
        {
            AnimatedProperties = new Dictionary<string, IAnimateProperty>();
            properties = new IAnimateProperty[0];

            OnDisable();

            if (AnimatedTrack)
            {
                if (container.transform.IsChildOf(AnimationTrack.transform))
                {
                    var track = tracksManager.GetTrackAtTime(container.ObjectData?.SongBpmTime ?? 0);
                    track.AttachContainer(container);
                }
                GameObject.Destroy(AnimationTrack.gameObject);
                AnimationTrack = null;
                AnimatedTrack = false;
            }

            LocalRotation = new Aggregator<Quaternion>(Quaternion.identity, (a, b) => a * b);
            WorldRotation = new Aggregator<Quaternion>(Quaternion.identity, (a, b) => a * b);
            OffsetPosition = new Aggregator<Vector3>(Vector3.zero, (a, b) => a + b);
            WorldPosition = new Aggregator<Vector3>(Vector3.zero, (a, b) => a + b);
            Scale = new Aggregator<Vector3>(Vector3.one, (a, b) => Vector3.Scale(a, b));
            Colors = new Aggregator<Color>(
                container?.MaterialPropertyBlock?.GetColor((container is ObstacleContainer) ? "_ColorTint" : "_Color") ?? Color.white,
                (a, b) => a * b);
            Opacity = new Aggregator<float>(1.0f, (a, b) => a * b);
            OpacityArrow = new Aggregator<float>(1.0f, (a, b) => a * b);

            _time = null;
            AnimatedLife = false;
            ShouldRecycle = false;

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
                    nc.directionTarget.localPosition = Vector3.zero;
                }
                container.UpdateMaterials();
            }
        }

        private void OnDisable()
        {
            if (Atsc != null)
            {
                Atsc.TimeChanged -= OnTimeChanged;
            }

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

            enabled = (UIMode.AnimationMode && tracksManager != null);
            if (!enabled) return;

            obj.RecomputeSongBpmTime();

            var duration = 0f;

            if (container is ObstacleContainer obs)
            {
                duration = obs.ObstacleData.Duration;
                (var wallSize, var wallPosition) = obs.ReadSizePosition();
                OffsetPosition.Preload(wallPosition);
                Scale.Preload(new Vector3(WallClamp(wallSize.x), WallClamp(wallSize.y), WallClamp(wallSize.z)));
            }
            if (container is NoteContainer note)
            {
                note.directionTarget.localPosition = new Vector3(0, 0, 0.4f);
            }

            if (obj.CustomLocalRotation is JSONNode rot)
                LocalRotation.Preload(Quaternion.Euler(rot.ReadVector3()));
            if (obj.CustomWorldRotation is JSONArray wrot)
                WorldRotation.Preload(Quaternion.Euler(wrot.ReadVector3()));
            if (obj.CustomWorldRotation is JSONNumber yrot)
                WorldRotation.Preload(Quaternion.Euler(0, yrot, 0));

            time_begin = obj.SpawnJsonTime;
            // Can't use DespawnJsonTime because obstacles jump out at 0.75
            time_end = obj.JsonTime + duration + obj.Hjd;

            RequireAnimationTrack();
            WorldTarget = AnimationTrack.transform;

            bool bug = false;

            if (obj.CustomTrack != null)
            {
                List<string> tracks = obj.CustomTrack switch {
                    JSONString s => new List<string> { s },
                    JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
                    _ => new List<string>()
                };
                foreach (var tr in tracks)
                {
                    AddParent(tr);

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
                            if (jprop.Key == "_definitePosition" || jprop.Key == "definitePosition") bug = true;
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

                 AnimationTrack.transform.SetParent(this.tracks[0].track.ObjectParentTransform, false);
            }

            // Individual Path Animation
            if (obj.CustomAnimation != null)
            {
                foreach (var jprop in obj.CustomAnimation.AsObject)
                {
                    if (jprop.Key == "_definitePosition" || jprop.Key == "definitePosition") bug = true;
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

            // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            if (bug && (obj.CustomData["_disableNoteGravity"]?.AsBool ?? obj.CustomData["disableNoteGravity"]?.AsBool ?? false))
            {
                Debug.LogError("disableNoteGravity is bugged when combined with definitePosition, please remove it!");
                var position = AnimationTrack.ObjectParentTransform.localPosition;
                position.y = (position.y * -0.1f) + 1;
                AnimationTrack.ObjectParentTransform.localPosition = position;
            }

            properties = new IAnimateProperty[AnimatedProperties.Count];
            int i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            Update();

            Atsc.TimeChanged += OnTimeChanged;
        }

        public void SetGeometry(BaseEnvironmentEnhancement eh)
        {
            var v2 = eh is V2EnvironmentEnhancement;
            ResetData();

            LocalTarget = AnimationThis.transform;
            WorldTarget = container.transform;

            if (eh.Scale is Vector3 scale)
                Scale.Preload(scale);
            if (eh.Position is Vector3 p)
                WorldPosition.Preload((v2 ? 1 : 1.667f) * p);
            if (eh.LocalPosition is Vector3 lp)
                OffsetPosition.Preload((v2 ? 1 : 1.667f) * lp);
            if (eh.Rotation is Vector3 r)
                WorldRotation.Preload(Quaternion.Euler(r.x, r.y, r.z));
            if (eh.LocalRotation is Vector3 lr)
                LocalRotation.Preload(Quaternion.Euler(lr.x, lr.y, lr.z));

            if (eh.Track != null)
            {
                AddParent(eh.Track);
                container.transform.SetParent(this.tracks[0].track.ObjectParentTransform, false);
            }

            Atsc.TimeChanged += OnTimeChanged;
        }

        public void SetTrack(Track track, string name)
        {
            ResetData();

            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;

            Atsc.TimeChanged += OnTimeChanged;
        }

        public void AddParent(string name)
        {
            var track = tracksManager.CreateAnimationTrack(name);
            track.children.Add(this);
            track.OnChildrenChanged();
            this.tracks.Add(track);
        }

        private float? _time = null;
        private float time_begin;
        private float time_end;

        public void Update()
        {
            var time = _time ?? Atsc?.CurrentJsonTime ?? 0;

            if (container?.ObjectData is BaseGrid obj)
            {
                var NoodleAnimationLifetime = (time > time_end) ? -1 : 1;
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
                // Jump in should have t=0 path animation
                else if (time < time_begin)
                {
                    prop.UpdateProperty(time_begin);
                }
            }

            if (AnimatedTrack)
            {
                AnimationTrack.UpdateTime(time);
            }
        }

        public void LateUpdate()
        {
            if (LocalRotation.Count > 0)
                LocalTarget.localRotation = LocalRotation.Get();

            if (OffsetPosition.Count > 0)
                LocalTarget.localPosition = OffsetPosition.Get();

            if (Scale.Count > 0)
                LocalTarget.localScale = Scale.Get();

            if (WorldTarget is Transform && WorldRotation.Count > 0)
            {
                WorldTarget.localRotation = WorldRotation.Get();
            }
            var time = _time ?? Atsc?.CurrentJsonTime ?? 0;
            if (WorldPosition.Count > 0)
            {
                if (time_begin < time && time < time_end)
                    AnimationTrack.UpdatePosition(0);
                if (!(container is null))
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }
            if (container is ObjectContainer && (Colors.Count > 0 || OpacityArrow.Count > 0 || Opacity.Count > 0))
            {
                if (Colors.Count > 0)
                {
                    var color = Colors.Get();
                    container.MaterialPropertyBlock
                        .SetColor((container is ObstacleContainer) ? "_ColorTint" : "_Color", color);
                }

                if (container is NoteContainer nc)
                {
                    nc.arrowMaterialPropertyBlock.SetFloat("_OpaqueAlpha", OpacityArrow.Get());
                }

                container.MaterialPropertyBlock.SetFloat("_OpaqueAlpha", Opacity.Get());
                container.UpdateMaterials();
            }
        }

        public void SetLifeTime(float normalTime)
        {
            _time = (normalTime < 0)
                ? (float?)null
                : (float?)Mathf.LerpUnclamped(time_begin, time_end, normalTime);
        }

        private void OnTimeChanged()
        {
            if (Atsc.IsPlaying) return;

            LocalTarget.localRotation = LocalRotation.Get();

            LocalTarget.localPosition = OffsetPosition.Get();

            LocalTarget.localScale = Scale.Get();

            if (WorldTarget is Transform)
            {
                WorldTarget.localRotation = WorldRotation.Get();
            }
        }

        private void RequireAnimationTrack()
        {
            if (AnimationTrack == null)
            {
                AnimationTrack = tracksManager.CreateIndividualTrack(container.ObjectData as BaseGrid);
                AnimationTrack.AttachContainer(container);
                AnimationTrack.ObjectParentTransform.localPosition = new Vector3(container.transform.localPosition.x, container.transform.localPosition.y, 0);
                AnimationTrack.transform.localPosition = Vector3.zero;
                container.transform.localPosition = Vector3.zero;
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
                AddPointDef<Vector3>((Vector3 v) => OffsetPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_definitePosition":
                AddPointDef<Vector3>((Vector3 v) => WorldPosition.Add(v), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "position":
            case "definitePosition":
                AddPointDef<Vector3>((Vector3 v) => WorldPosition.Add(v * 1.667f), PointDataParsers.ParseVector3, p, Vector3.zero);
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
                if (p.overwrite)
                {
                    AnimatedProperties[p.key] = new AnimateProperty<T>(
                        new List<PointDefinition<T>>(),
                        setter,
                        _default
                    );
                }
                GetAnimateProperty<T>(p.key, setter, _default).AddPointDef(parser, p);
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

        private static float minWall = 0.06f;

        private static float WallClamp(float a)
        {
            if (-minWall < a && a < minWall)
            {
                return minWall;
            }
            return a;
        }

        // I should never be allowed to use a profiler
        public class Aggregator<T> where T : struct
        {
            public int Count = 0;
            public Func<T, T, T> func;
            public T _default;
            public int Keep = 0;

            public Aggregator(T _default, Func<T, T, T> func)
            {
                this._default = _default;
                this.func = func;
            }

            public void Add(T v)
            {
                // This shouldn't ever go above 3, but check anyway
                if (Count >= 4)
                    return;
                else
                    items[Count] = v;
                ++Count;
            }

            public void Preload(T v)
            {
                Add(v);
                ++Keep;
            }

            public T Get()
            {
                if (Count == 0) return _default;
                var value = items[0];
                for (int i = 1; i < Count; ++i)
                {
                    value = func(value, items[i]);
                }
                Count = Keep;
                return value;
            }

            private T[] items = new T[4];
        }
    }
}
