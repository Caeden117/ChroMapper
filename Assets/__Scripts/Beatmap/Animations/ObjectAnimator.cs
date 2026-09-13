using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using Random = UnityEngine.Random;

namespace Beatmap.Animations
{
    public class ObjectAnimator : MonoBehaviour
    {
        [SerializeField] public GameObject AnimationThis;
        [SerializeField] private ObjectContainer container;

        public BeatmapRuntimeContext Context;
        public Track AnimationTrack;
        public TracksManager TracksManager;

        [SerializeField] public Transform LocalTarget;
        public Transform WorldTarget;

        public readonly Aggregator<Quaternion> LocalRotation = new(Quaternion.identity, (a, b) => a * b);
        public Aggregator<Quaternion> WorldRotation = new(Quaternion.identity, (a, b) => a * b);
        public readonly Aggregator<Vector3> OffsetPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> WorldPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> Scale = new(Vector3.one, Vector3.Scale);
        public readonly Aggregator<Color> Colors = new(Color.white, (a, b) => a * b);
        public readonly Aggregator<float> Opacity = new(1f, (a, b) => a * b);
        public readonly Aggregator<float> OpacityArrow = new(1f, (a, b) => a * b);

        public bool AnimatedTrack { get; private set; }
        public bool AnimatedLife { get; private set; }
        public bool ShouldRecycle;

        public enum TargetTypes
        {
            None,
            GameplayObject,
            Transform,
            Material,
        };

        public TargetTypes TargetType;

        private List<TrackAnimator> tracks = new();

        // Environment enhancement tracks target existing scene transforms directly so their OEM parent-child
        // hierarchy is never flattened into GeometryContainer's single animation wrapper.
        private bool directEnvironmentTarget;
        private bool directEnvironmentTargetIsV2;
        private bool directEnvironmentTargetIsTrackLaneRing;
        private TrackLaneRing directEnvironmentTrackLaneRing;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private static readonly int cutoutTexOffsetId = Shader.PropertyToID("_CutoutTexOffset");
        private static readonly int animSpawnedId = Shader.PropertyToID("_AnimationSpawned");

        public void ResetData()
        {
            AnimatedProperties.Clear();
            properties = Array.Empty<IAnimateProperty>();

            TargetType = TargetTypes.None;
            // EnvironmentEnhancementWith*Track* regression tests require pooled animators to discard cached direct
            // target state before they are attached to a different environment object.
            directEnvironmentTarget = false;
            directEnvironmentTargetIsV2 = false;
            directEnvironmentTargetIsTrackLaneRing = false;
            directEnvironmentTrackLaneRing = null;

            OnDisable();

            if (AnimatedTrack)
            {
                if (container.transform.IsChildOf(AnimationTrack.transform))
                {
                    var track = TracksManager.GetTrackAtTime(
                        container.ObjectData?.SongBpmTime ?? 0,
                        container.ObjectData is BaseGrid grid ? grid.Rotation : 0);
                    track.AttachContainer(container);
                }

                TracksManager.Remove(AnimationTrack);
                AnimationTrack = null;
                AnimatedTrack = false;
            }

            LocalRotation.Reset();
            WorldRotation.Reset();
            OffsetPosition.Reset();
            WorldPosition.Reset();
            Scale.Reset();
            Colors.Reset();
            // Unity containers need explicit null checks before reading their current material color.
            if (container != null)
            {
                Colors.Default = container.MpbController.Mpb.GetColor(colorId);
            }
            else
            {
                Colors.Default = Color.white;
            }
            Opacity.Reset();
            OpacityArrow.Reset();

            time = null;
            AnimatedLife = false;
            ShouldRecycle = false;

            if (LocalTarget != null)
            {
                LocalTarget.localEulerAngles = Vector3.zero;
                LocalTarget.localPosition = Vector3.zero;
                LocalTarget.localScale = Vector3.one;
            }

            if (container != null && !(container is GeometryContainer))
            {
                container.UpdateGridPosition();
                container.MpbController.Mpb.SetFloat(cutoutId, 0);
                container.MpbController.Mpb.SetVector(
                    cutoutTexOffsetId,
                    Random.insideUnitCircle * 10f);
                container.MpbController.Mpb.SetFloat(animSpawnedId, 0);
                if (container is NoteContainer nc)
                {
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 0);
                    nc.ArrowMpbController.Mpb.SetVector(
                        cutoutTexOffsetId,
                        Random.insideUnitCircle * 10f);
                    nc.DirectionTarget.localPosition = Vector3.zero;
                }

                container.UpdateMaterials();
            }
        }

        private void OnDisable()
        {
            if (Context != null) Context.Atsc.OnTimeChanged -= OnTimeChanged;

            // ObjectAnimatorDisableAfterTrackDestroyedDoesNotThrow proves mapper teardown can destroy a parent
            // TrackAnimator before this child disables, so detach only from Unity objects that are still alive.
            foreach (var track in tracks)
            {
                if (track != null)
                {
                    track.RemoveChild(this);
                }
            }

            tracks.Clear();
        }

        public void AttachToObject(BaseGrid obj)
        {
            ResetData();

            TargetType = TargetTypes.GameplayObject;

            enabled = UIMode.AnimationMode && TracksManager != null;
            if (!enabled) return;

            obj.RecomputeSpawnParameters();

            float duration;
            switch (container)
            {
                case ObstacleContainer obs:
                    duration = obs.ObstacleData.DurationSongBpmTime;
                    OffsetPosition.Preload(obs.ReadPosition() - new Vector3(0, 0, 0.25f));
                    Scale.Preload(Vector3.one);
                    break;
                case ArcContainer arc:
                    duration = arc.ArcData.DurationSongBpmTime;
                    break;
                case ChainContainer chain:
                    duration = chain.ChainData.DurationSongBpmTime;
                    break;
                default:
                    duration = 0f;
                    break;
            }

            if (obj.CustomLocalRotation is JSONNode rot) LocalRotation.Preload(Quaternion.Euler(rot.ReadVector3()));
            switch (obj.CustomWorldRotation)
            {
                case JSONArray wrot:
                    WorldRotation.Preload(Quaternion.Euler(wrot.ReadVector3()));
                    break;
                case JSONNumber yrot:
                    WorldRotation.Preload(Quaternion.Euler(0, yrot, 0));
                    break;
                default:
                    if (BeatSaberSongContainer.Instance.Map.MajorVersion == 4)
                        WorldRotation.Preload(Quaternion.Euler(0, obj.Rotation, 0));
                    break;
            }

            timeBegin = obj.SpawnSongBpmTime;
            // Can't use DespawnSongBpmTime because obstacles jump out early
            timeEnd = obj.SongBpmTime + duration + obj.HalfJumpDuration;

            RequireAnimationTrack();
            WorldTarget = AnimationTrack.transform;

            var bug = false;

            if (obj.CustomTrack != null)
            {
                var tracks = obj.CustomTrack switch
                {
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
                    if (events == null) continue;

                    var map = BeatSaberSongContainer.Instance.Map;
                    foreach (var ce in events.Where(ev => ev.Type == "AssignPathAnimation"))
                    {
                        foreach (var jprop in ce.Data)
                        {
                            if (jprop.Key == "_definitePosition" || jprop.Key == "definitePosition") bug = true;
                            var p = new IPointDefinition.UntypedParams
                            {
                                Key = $"track_{jprop.Key}",
                                Overwrite = false,
                                Points = jprop.Value,
                                Easing = ce.DataEasing,
                                Time = ce.SongBpmTime,
                                Transition = ce.DataDuration ?? 0,
                                TimeBegin = timeBegin,
                                TimeEnd = timeEnd,
                            };
                            if (p.Transition != 0)
                            {
                                p.Transition = (float)map.JsonTimeToSongBpmTime(ce.JsonTime + p.Transition)
                                    - ce.SongBpmTime;
                            }

                            AddPointDef(p, jprop.Key, ce);
                        }
                    }
                }

                if (tracks.Count > 0)
                    AnimationTrack.transform.SetParent(this.tracks[0].Track.ObjectParentTransform, false);
            }

            // Individual Path Animation
            if (obj.CustomAnimation != null)
            {
                foreach (var jprop in obj.CustomAnimation.AsObject)
                {
                    if (jprop.Key == "_definitePosition" || jprop.Key == "definitePosition") bug = true;
                    var p = new IPointDefinition.UntypedParams
                    {
                        Key = jprop.Key,
                        Overwrite = true,
                        Points = jprop.Value,
                        Easing = null,
                        TimeBegin = timeBegin,
                        TimeEnd = timeEnd,
                    };
                    AddPointDef(p, jprop.Key, null);
                }
            }

            // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            if (bug
                && (obj.CustomData["_disableNoteGravity"]?.AsBool
                    ?? obj.CustomData["disableNoteGravity"]?.AsBool ?? false))
            {
                Debug.LogError("disableNoteGravity is bugged when combined with definitePosition, please remove it!");
                var position = AnimationTrack.ObjectParentTransform.localPosition;
                position.y = (position.y * -0.1f) + 1;
                AnimationTrack.ObjectParentTransform.localPosition = position;
            }

            properties = new IAnimateProperty[AnimatedProperties.Count];
            var i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            Update();

            Context.Atsc.OnTimeChanged += OnTimeChanged;
        }

        public void AttachToGeometry(BaseEnvironmentEnhancement eh)
        {
            // Map version, rather than the static V2 serializer helper, determines legacy position unit conversion.
            var v2 = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = AnimationThis.transform;
            //WorldTarget = container.transform;
            WorldTarget = AnimationThis.transform;

            WorldRotation = LocalRotation;

            if (eh.Scale is Vector3 scale) Scale.Default = scale;
            if (eh.Position is Vector3 p) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * p;
            if (eh.LocalPosition is Vector3 lp) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * lp;
            if (eh.Rotation is Vector3 r) LocalRotation.Default = Quaternion.Euler(r.x, r.y, r.z);
            if (eh.LocalRotation is Vector3 lr) LocalRotation.Default = Quaternion.Euler(lr.x, lr.y, lr.z);

            if (eh.Track != null)
            {
                AddParent(eh.Track);
                container.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);
            }

            Context.Atsc.OnTimeChanged += OnTimeChanged;

            OnTimeChanged();
        }

        // EnvironmentEnhancementWith*Track* regression tests require missing track properties to preserve the spawn
        // transform, while authored properties directly overwrite each matched scene object as they do in Chroma.
        public void AttachToEnvironmentObject(Transform target, string track, bool v2)
        {
            ResetData();

            TargetType = TargetTypes.Transform;
            LocalTarget = target;
            WorldTarget = target;
            directEnvironmentTarget = true;
            directEnvironmentTargetIsV2 = v2;

            // Cache the optional native ring dependency during attachment so animated position updates can preserve
            // DefaultEnvironment's per-segment wave without performing component discovery in LateUpdate.
            directEnvironmentTrackLaneRing = target.GetComponent<TrackLaneRing>();
            directEnvironmentTargetIsTrackLaneRing = directEnvironmentTrackLaneRing != null;

            AddParent(track);
            Context.Atsc.OnTimeChanged += OnTimeChanged;
        }

        public void AttachToTrack(Track track, string name)
        {
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;

            Context.Atsc.OnTimeChanged += OnTimeChanged;
        }

        public void AttachToMaterial(GeometryContainer con, string track)
        {
            ResetData();

            TargetType = TargetTypes.Material;
            container = con;

            enabled = true;
            AddParent(track);
        }

        public void AddParent(string name)
        {
            var track = TracksManager.GetAnimationTrack(name);
            track.AddChild(this);
            tracks.Add(track);
        }

        private float? time;
        private float timeBegin;
        private float timeEnd;

        public void Update()
        {
            // Unity time controllers need explicit null checks before reading their playback time.
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);

            if (container != null && container.ObjectData is BaseGrid obj)
            {
                var noodleAnimationLifetime = time > timeEnd ? -1 : 1;
                if (!(container is ChainContainer))
                {
                    // Unity containers need explicit null checks before updating spawned-state shader values.
                    container.MpbController.Mpb.SetFloat(
                        animSpawnedId,
                        noodleAnimationLifetime);
                    if (container is NoteContainer nc)
                    {
                        nc.ArrowMpbController.Mpb.SetFloat(
                            animSpawnedId,
                            noodleAnimationLifetime);
                    }
                }

                AnimatedLife =
                    (this.time != null && this.time < obj.SongBpmTime)
                    || WorldPosition.Count > 0
                    || (obj.CustomFake && time < timeEnd);
                if (ShouldRecycle)
                {
                    var despawnTime = WorldPosition.Count == 0 && !obj.CustomFake
                        ? obj.SongBpmTime
                        : timeEnd;
                    if (time > despawnTime)
                    {
                        BeatmapObjectContainerCollection
                            .GetCollectionForType(container.ObjectData.ObjectType)
                            .RecycleContainer(container.ObjectData);
                        AnimatedLife = false;
                        return;
                    }
                }
            }

            var l = properties.Length;
            for (var i = 0; i < l; ++i)
            {
                var prop = properties[i];
                if (time >= prop.StartTime) prop.UpdateProperty(time);
            }

            if (AnimatedTrack) AnimationTrack.UpdateTime(time);
        }

        public void LateUpdate()
        {
            // Direct environment targets apply only properties supplied by AnimateTrack. Reading aggregator defaults
            // here would incorrectly reset absent position, rotation, or scale fields on empty and scale-only events.
            if (directEnvironmentTarget)
            {
                if (LocalRotation.Count > 0) LocalTarget.localRotation = LocalRotation.Get();

                if (OffsetPosition.Count > 0)
                {
                    var position = OffsetPosition.Get();
                    ApplyDirectEnvironmentPosition(position, directEnvironmentTargetIsV2);
                }

                if (Scale.Count > 0) LocalTarget.localScale = Scale.Get();

                if (WorldRotation.Count > 0) WorldTarget.rotation = WorldRotation.Get();

                // EnvironmentEnhancementWithZeroPositionAnimateTrackKeepsDefaultEnvironmentBigRingsVisibleAtWorldOrigin
                // requires definite position to rebase native ring motion just like legacy V2 position does.
                if (WorldPosition.Count > 0)
                {
                    ApplyDirectEnvironmentPosition(WorldPosition.Get(), true);
                }

                return;
            }

            if (TargetType == TargetTypes.Material)
            {
                if (Colors.Count > 0)
                {
                    var color = Colors.Get();
                    container.MpbController.Mpb.SetColor(colorId, color);
                    container.UpdateMaterials();
                }

                return;
            }

            if (LocalRotation.Count > 0) LocalTarget.localRotation = LocalRotation.Get();

            if (OffsetPosition.Count > 0) LocalTarget.localPosition = OffsetPosition.Get();

            if (Scale.Count > 0) LocalTarget.localScale = Scale.Get();

            if (WorldTarget is Transform && WorldRotation.Count > 0)
                if (container is not GeometryContainer)
                    WorldTarget.localRotation = WorldRotation.Get();

            // Unity time controllers need explicit null checks before reading their playback time.
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
            if (WorldPosition.Count > 0)
            {
                if (timeBegin < time && time < timeEnd) AnimationTrack.UpdatePosition(0);
                if (container is not null and not GeometryContainer)
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }

            if (container is ObjectContainer && (Colors.Count > 0 || OpacityArrow.Count > 0 || Opacity.Count > 0))
            {
                if (Colors.Count > 0)
                {
                    var color = Colors.Get();
                    if (container is ObstacleContainer obstacle)
                        obstacle.SetColor(color);
                    else
                        container.MpbController.Mpb.SetColor(colorId, color);
                }

                if (container is NoteContainer nc)
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 1f - OpacityArrow.Get());

                container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
                container.UpdateMaterials();
            }
        }

        // Chroma treats an animated environment position as the TrackLaneRing's new base and retains its current wave
        // displacement; assigning Transform.position directly would instead stack every segment at the animated point.
        private void ApplyDirectEnvironmentPosition(Vector3 position, bool worldSpace)
        {
            if (directEnvironmentTargetIsTrackLaneRing)
            {
                var localPosition = worldSpace && LocalTarget.parent != null
                    ? LocalTarget.parent.InverseTransformPoint(position)
                    : position;
                directEnvironmentTrackLaneRing.RebasePositionOffset(localPosition);
                return;
            }

            if (worldSpace)
            {
                LocalTarget.position = position;
            }
            else
            {
                LocalTarget.localPosition = position;
            }
        }

        public void SetLifeTime(float normalTime)
        {
            time = normalTime < 0
                ? null
                : Mathf.LerpUnclamped(timeBegin, timeEnd, normalTime);
        }

        private void OnTimeChanged()
        {
            if (Context.Atsc.IsPlaying) return;

            // TrackAnimator refreshes direct environment properties before LateUpdate; an empty event must not apply
            // ObjectAnimator's identity defaults during a stopped-time callback.
            if (directEnvironmentTarget) return;

            LocalTarget.localRotation = LocalRotation.Get();

            LocalTarget.localPosition = OffsetPosition.Get();

            LocalTarget.localScale = Scale.Get();

            if (WorldTarget is Transform)
            {
                if (!(container is GeometryContainer)) WorldTarget.localRotation = WorldRotation.Get();
            }
        }

        private void RequireAnimationTrack()
        {
            if (AnimationTrack == null)
            {
                AnimationTrack = TracksManager.CreateIndividualTrack(container.ObjectData as BaseGrid);
                AnimationTrack.AttachContainer(container);
                AnimationTrack.ObjectParentTransform.localPosition = new Vector3(
                    container.transform.localPosition.x,
                    container.transform.localPosition.y,
                    0);
                AnimationTrack.transform.localPosition = Vector3.zero;
                container.transform.localPosition = Vector3.zero;
                AnimatedTrack = true;
            }
        }

        // Only used for gameplay objects?
        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
                case "_dissolve":
                case "dissolve":
                    AddPointDef(source, f => Opacity.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case "_dissolveArrow":
                case "dissolveArrow":
                    AddPointDef(source, f => OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case "_localRotation":
                case "localRotation":
                    AddPointDef(
                        source,
                        q => LocalRotation.Add(q),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case "_rotation":
                case "offsetWorldRotation":
                    AddPointDef(
                        source,
                        v => WorldRotation.Add(v),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case "_position":
                case "offsetPosition":
                    AddPointDef(
                        source,
                        v => OffsetPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case "_definitePosition":
                case "definitePosition":
                    AddPointDef(
                        source,
                        v => WorldPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case "_scale":
                case "scale":
                    AddPointDef(
                        source,
                        v => Scale.Add(v),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.one);
                    break;
                case "_color":
                case "color":
                    AddPointDef<Color>(source, (Color c) => Colors.Add(c), PointDataParsers.ParseColor, p, Color.white);
                    break;
            }
        }

        private void AddPointDef<T>(
            BaseCustomEvent source,
            Action<T> setter,
            PointDefinition<T>.Parser parser,
            IPointDefinition.UntypedParams p,
            T @default) where T : struct
        {
            try
            {
                if (p.Overwrite)
                {
                    AnimatedProperties[p.Key] = new AnimateProperty<T>(
                        new List<PointDefinition<T>>(),
                        setter,
                        @default
                    );
                }

                GetAnimateProperty(p.Key, setter, @default).AddPointDef(parser, p, source);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private AnimateProperty<T> GetAnimateProperty<T>(string key, Action<T> setter, T @default) where T : struct
        {
            if (!AnimatedProperties.ContainsKey(key))
            {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>(),
                    setter,
                    @default
                );
            }

            return AnimatedProperties[key] as AnimateProperty<T>;
        }

        private static float minWall = 0.06f;

        private static float WallClamp(float a)
        {
            if (-minWall < a && a < minWall) return minWall;

            return a;
        }

        // I should never be allowed to use a profiler
        public class Aggregator<T> where T : struct
        {
            public int Count;
            public readonly Func<T, T, T> Func;
            public T Default;
            private readonly T instancedDefault;
            public int Keep;

            public Aggregator(T def, Func<T, T, T> func)
            {
                Default = def;
                instancedDefault = Default;
                Func = func;
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
                if (Count == 0) return Default;
                var value = items[0];
                for (var i = 1; i < Count; ++i) value = Func(value, items[i]);

                Count = Keep;
                return value;
            }

            public void Reset()
            {
                Default = instancedDefault;
                Count = 0;
                Keep = 0;
                for (var i = 0; i < items.Length; i++) items[i] = default;
            }

            private readonly T[] items = new T[4];
        }
    }
}
