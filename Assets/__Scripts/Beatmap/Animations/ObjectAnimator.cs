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
        public readonly Aggregator<Vector3> LocalPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> WorldPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> Scale = new(Vector3.one, Vector3.Scale);
        public readonly Aggregator<Color> Colors = new(Color.white, (a, b) => a * b);
        public readonly Aggregator<float> Opacity = new(1f, (a, b) => a * b);
        public readonly Aggregator<float> OpacityArrow = new(1f, (a, b) => a * b);
        public readonly Aggregator<float> Interactable = new(1f, (a, b) => a * b);

        public bool AnimatedTrack { get; private set; }
        public bool AnimatedLife { get; private set; }
        public bool ShouldRecycle;
        private bool animatedColorApplied;
        private bool colorRestorePending;
        private bool disableNoteLook;

        public enum TargetTypes
        {
            None,
            GameplayObject,
            Transform,
            Material,
        };

        public TargetTypes TargetType;

        private List<TrackAnimator> tracks = new();

        private bool timeCallbacksSubscribed;

        private void SubscribeTimeCallbacks()
        {
            if (timeCallbacksSubscribed) return;
            Context.Atsc.OnTimeChanged += OnTimeChanged;
            Context.Atsc.OnTimeFlushPending += FlushPendingAnimations;
            timeCallbacksSubscribed = true;
        }

        private bool directEnvironmentTarget;
        private bool directEnvironmentTargetIsV2;
        private bool directEnvironmentTargetIsTrackLaneRing;
        private TrackLaneRing directEnvironmentTrackLaneRing;
        // Keep the light's mesh reference instead of searching for it during animation updates.
        private ParametricBoxLight directEnvironmentBoxLight;
        private bool directEnvironmentHasBoxLight;
        // Save the authored pose so seeks before the first event can restore it.
        private Vector3 directEnvironmentSpawnPosition;
        private Quaternion directEnvironmentSpawnRotation;
        private Vector3 directEnvironmentSpawnScale;
        private ObjectAnimator directEnvironmentParentAnimator;
        private bool directEnvironmentHasParentAnimator;
        private Matrix4x4 directEnvironmentParentMatrix;
        private Vector3 directEnvironmentSpawnLocalPosition;
        private bool directEnvironmentEverApplied;
        private int directEnvironmentLastTrackUpdateVersion = -1;
        private bool trackParentTarget;
        private bool trackParentTargetIsV2;
        private TrackAnimator trackParentPropertySource;
        private int trackParentLastUpdateVersion = -1;
        private ObjectAnimator trackParentAncestor;
        private bool trackParentHasAncestor;
        private Matrix4x4 trackParentRootMatrix;
        private Vector3 trackParentSpawnPosition;
        private Quaternion trackParentSpawnRotation;
        private Vector3 trackParentSpawnScale;
        private AnimateProperty<Vector3> geometryWorldPositionProperty;
        private ObjectAnimator geometryParentAnimator;
        private bool geometryHasWorldPositionAnchor;
        private int geometryAnchorUpdateVersion = -1;
        private float geometryAnchorTime;
        private Vector3 geometryAnchoredLocalPosition;
        // Materials upgraded from Standard to unlit Glowing must keep zero alpha during color animation.
        private bool materialZeroAlpha;

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
            // Pooled animators must discard their previous target before attachment.
            directEnvironmentTarget = false;
            directEnvironmentTargetIsV2 = false;
            directEnvironmentTargetIsTrackLaneRing = false;
            directEnvironmentTrackLaneRing = null;
            directEnvironmentBoxLight = null;
            directEnvironmentHasBoxLight = false;
            directEnvironmentEverApplied = false;
            directEnvironmentLastTrackUpdateVersion = -1;
            directEnvironmentSpawnPosition = Vector3.zero;
            directEnvironmentSpawnRotation = Quaternion.identity;
            directEnvironmentSpawnScale = Vector3.one;
            directEnvironmentParentAnimator = null;
            directEnvironmentHasParentAnimator = false;
            trackParentTarget = false;
            trackParentTargetIsV2 = false;
            trackParentPropertySource = null;
            trackParentLastUpdateVersion = -1;
            trackParentAncestor = null;
            trackParentHasAncestor = false;
            geometryWorldPositionProperty = null;
            geometryParentAnimator = null;
            geometryHasWorldPositionAnchor = false;
            geometryAnchorUpdateVersion = -1;
            materialZeroAlpha = false;

            OnDisable();
            tracks.Clear();

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
            LocalPosition.Reset();
            WorldPosition.Reset();
            Scale.Reset();
            Colors.Reset();
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
            Interactable.Reset();
            animatedColorApplied = false;
            colorRestorePending = false;
            disableNoteLook = false;

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

        // Disabled animators unsubscribe from seeks; restore those subscriptions when they become active.
        private void OnEnable()
        {
            if (Context == null || Context.Atsc == null) return;
            SubscribeTimeCallbacks();

            var reattached = false;
            foreach (var track in tracks)
            {
                if (track != null && !track.Children.Contains(this))
                {
                    track.AddChild(this);
                    track.PushToChild(this);
                    reattached = true;
                }
            }

            // The target may have been reset while disabled without its source track changing.
            // Force the next update to reapply the held pose
            if (trackParentTarget)
                trackParentLastUpdateVersion = -1;

            if (reattached)
                OnTimeChanged();
        }

        private void OnDisable()
        {
            // Discard pending track values on disable so the next seek cannot combine them with stale contributions.
            if (trackParentTarget)
            {
                FlushPendingAnimations();
            }
            if (Context != null && timeCallbacksSubscribed)
            {
                Context.Atsc.OnTimeChanged -= OnTimeChanged;
                Context.Atsc.OnTimeFlushPending -= FlushPendingAnimations;
                timeCallbacksSubscribed = false;
            }

            foreach (var track in tracks)
            {
                if (track != null)
                {
                    track.RemoveChild(this);
                }
            }
        }

        // Clear streamed values before a stopped seek pushes values for its new time.
        public void FlushPendingAnimations()
        {
            LocalRotation.Flush();
            WorldRotation.Flush();
            OffsetPosition.Flush();
            LocalPosition.Flush();
            WorldPosition.Flush();
            Scale.Flush();
            Colors.Flush();
            Opacity.Flush();
            OpacityArrow.Flush();
            Interactable.Flush();
            colorRestorePending |= animatedColorApplied;
        }

        public void AttachToObject(BaseGrid obj)
        {
            ResetData();

            TargetType = TargetTypes.GameplayObject;
            OffsetPosition.HoldUntilFlush = true;

            enabled = UIMode.AnimationMode && TracksManager != null;
            if (!enabled) return;

            obj.RecomputeSpawnParameters();
            var noteLookKey = TracksManager.IsV2Map ? "_disableNoteLook" : "disableNoteLook";
            disableNoteLook = obj.CustomData?.HasKey(noteLookKey) == true
                && obj.CustomData[noteLookKey].AsBool;
            float duration;
            switch (container)
            {
                case ObstacleContainer obs:
                    duration = obs.ObstacleData.DurationSongBpmTime;
                    OffsetPosition.Preload(obs.ReadPosition() - new Vector3(0, 0, 0.25f));
                    // Multiply track scale by the obstacle's authored visual scale.
                    Scale.Preload(obs.ObstacleData.CustomVisualScale);
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
                            // Null leaves this path property unassigned.
                            if (jprop.Value == null || jprop.Value.IsNull)
                                continue;

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

            SubscribeTimeCallbacks();
        }

        public void AttachToGeometry(BaseEnvironmentEnhancement eh)
        {
            // Use the loaded map's version to choose legacy position units.
            var v2 = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = AnimationThis.transform;
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

                // Paradigm's held child positions must move with later parent animations, including
                // stopped seeks across the door cut and the boss streaks (ParadigmMapParityTest).
                geometryParentAnimator = tracks[0].Animator;
                var positionKey = v2 ? "_position" : "position";
                if (tracks[0].AnimatedProperties.TryGetValue(positionKey, out var positionProperty))
                {
                    geometryWorldPositionProperty = positionProperty as AnimateProperty<Vector3>;
                }

                geometryHasWorldPositionAnchor = geometryParentAnimator != null && geometryWorldPositionProperty != null;
            }

            SubscribeTimeCallbacks();

            OnTimeChanged();
        }

        // Only supplied track properties overwrite an environment object's authored transform.
        public void AttachToEnvironmentObject(
            Transform target,
            string track,
            bool v2,
            ParametricBoxLight boxLight)
        {
            ResetData();

            TargetType = TargetTypes.Transform;
            LocalTarget = target;
            WorldTarget = target;
            directEnvironmentTarget = true;
            directEnvironmentTargetIsV2 = v2;
            // Reuse the enhancement's mesh reference to avoid searching during animation updates.
            directEnvironmentBoxLight = boxLight;
            directEnvironmentHasBoxLight = boxLight != null;

            // Cache the ring so position updates can preserve its wave displacement without component searches.
            directEnvironmentTrackLaneRing = target.GetComponent<TrackLaneRing>();
            directEnvironmentTargetIsTrackLaneRing = directEnvironmentTrackLaneRing != null;

            // Save the post-enhancement pose for seeks before the first animation event.
            directEnvironmentSpawnPosition = target.position;
            directEnvironmentSpawnRotation = target.rotation;
            directEnvironmentSpawnScale = target.localScale;

            AddParent(track);
            // Physically parent enhanced objects only when their track has an AssignTrackParent ancestor.
            // Other tracks leave the environment's original hierarchy intact.
            var trackAnimator = tracks[^1];
            if (trackAnimator.Parents.Count > 0)
            {
                LocalTarget.SetParent(
                    trackAnimator.Track.ObjectParentTransform,
                    trackAnimator.ParentWorldPositionStays);
            }

            CaptureDirectEnvironmentParent(trackAnimator.Parents.Count > 0 ? trackAnimator.Animator : null);
            // Rotation snapshots can be computed while the rendered parent is collapsed or at another beat.
            var pairRotation = target.GetComponent<LightPairRotationEffect>();
            if (pairRotation != null)
                pairRotation.BindPositionSampler(GetEnvironmentPositionZAt);

            SubscribeTimeCallbacks();
        }

        // Apply track parenting to enhanced objects attached before the parenting event.
        public void ParentDirectTargetToTrack(Transform trackParent, bool worldPositionStays)
        {
            if (!directEnvironmentTarget)
                return;

            LocalTarget.SetParent(trackParent, worldPositionStays);
            CaptureDirectEnvironmentParent(trackParent.GetComponentInParent<ObjectAnimator>());
        }

        private void CaptureDirectEnvironmentParent(ObjectAnimator parentAnimator)
        {
            directEnvironmentParentAnimator = parentAnimator;
            directEnvironmentHasParentAnimator = parentAnimator != null;
            directEnvironmentParentMatrix = LocalTarget.parent != null
                ? LocalTarget.parent.localToWorldMatrix
                : Matrix4x4.identity;
            directEnvironmentSpawnLocalPosition = LocalTarget.localPosition;
        }

        // Sample sorted track properties without moving scene objects or changing the playhead.
        // Native laser resets read world Z at dispatch, rather than at snapshot construction.
        private float GetEnvironmentPositionZAt(float jsonTime)
        {
            var parent = directEnvironmentHasParentAnimator
                ? directEnvironmentParentAnimator.GetTrackParentMatrix(jsonTime)
                : directEnvironmentParentMatrix;
            var source = tracks[0];
            var localKey = directEnvironmentTargetIsV2 ? "_localPosition" : "localPosition";
            var worldKey = directEnvironmentTargetIsV2 ? "_position" : "position";
            var units = directEnvironmentTargetIsV2 ? BeatmapConstant.LaneSize : 1f;
            if (source.AnimatedProperties.TryGetValue(localKey, out var localProperty)
                && jsonTime >= localProperty.StartTime)
            {
                var position = ((AnimateProperty<Vector3>)localProperty).GetLerpedValue(jsonTime) * units;
                return parent.MultiplyPoint3x4(position).z;
            }

            if (source.AnimatedProperties.TryGetValue(worldKey, out var worldProperty)
                && jsonTime >= worldProperty.StartTime)
            {
                return ((AnimateProperty<Vector3>)worldProperty).GetLerpedValue(jsonTime).z * units;
            }

            return parent.MultiplyPoint3x4(directEnvironmentSpawnLocalPosition).z;
        }

        internal void DestroyTrackBoundEnvironmentTarget()
        {
            if (!directEnvironmentTarget) return;
            enabled = false;
            if (LocalTarget == null) return;
            if (LocalTarget.parent == null || LocalTarget.parent.GetComponentInParent<Track>() == null) return;
            DestroyImmediate(LocalTarget.gameObject);
        }

        public void AttachToTrack(Track track, string name, bool isV2Map)
        {
            ResetData();

            TargetType = TargetTypes.Transform;

            trackParentTarget = true;
            trackParentTargetIsV2 = isV2Map;
            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;
            trackParentSpawnPosition = WorldTarget.localPosition;
            trackParentSpawnRotation = WorldTarget.localRotation;
            trackParentSpawnScale = WorldTarget.localScale;

            SubscribeTimeCallbacks();
        }

        public void SetTrackParentMapVersion(bool isV2Map)
        {
            trackParentTargetIsV2 = isV2Map;
        }

        // Reapply held world positions only when the property source changes, allowing ancestor motion to carry the child.
        public void BindPropertySource(TrackAnimator source)
        {
            trackParentPropertySource = source;
            trackParentLastUpdateVersion = -1;
            trackParentAncestor = source.Animator;
            trackParentHasAncestor = trackParentAncestor != null;
            trackParentRootMatrix = WorldTarget.parent.localToWorldMatrix;
        }

        // Sample the existing parents pose without moving Unity objects or replaying map events.
        // This is queried only when a completed geometry animation needs a new parent-relative anchor.
        private Matrix4x4 GetTrackParentMatrix(float jsonTime)
        {
            var ancestor = trackParentHasAncestor
                ? trackParentAncestor.GetTrackParentMatrix(jsonTime)
                : trackParentRootMatrix;
            var source = trackParentPropertySource;
            var rotation = ReadTrackProperty(source, "_rotation", "rotation", jsonTime, trackParentSpawnRotation);
            var localRotation = ReadTrackProperty(source, "_localRotation", "localRotation", jsonTime, Quaternion.identity);
            var scale = ReadTrackProperty(source, "_scale", "scale", jsonTime, Vector3.one);
            var self = ancestor * Matrix4x4.TRS(trackParentSpawnPosition, rotation, trackParentSpawnScale);
            var offset = ReadTrackProperty(source, "_position", "offsetPosition", jsonTime, Vector3.zero)
                * BeatmapConstant.LaneSize;
            var position = offset;
            if (!trackParentTargetIsV2)
            {
                if (source.AnimatedProperties.TryGetValue("localPosition", out var localProperty)
                    && jsonTime >= localProperty.StartTime)
                {
                    position = ((AnimateProperty<Vector3>)localProperty).GetLerpedValue(jsonTime);
                }
                else if (source.AnimatedProperties.TryGetValue("position", out var worldProperty)
                    && jsonTime >= worldProperty.StartTime)
                {
                    var world = ((AnimateProperty<Vector3>)worldProperty).GetLerpedValue(jsonTime);
                    position = self.inverse.MultiplyVector(world);
                }
            }

            return self * Matrix4x4.TRS(position, localRotation, scale);
        }

        private static T ReadTrackProperty<T>(
            TrackAnimator source, string legacyKey, string key, float time, T defaultValue) where T : struct
        {
            if (!source.AnimatedProperties.TryGetValue(key, out var property))
            {
                source.AnimatedProperties.TryGetValue(legacyKey, out property);
            }

            if (property is AnimateProperty<T> typed && time >= typed.StartTime)
            {
                return typed.GetLerpedValue(time);
            }

            return defaultValue;
        }

        // Keep completed world positions relative to the parent's pose at the animation endpoint.
        // Writing that old world position every frame cancels later parent motion in Paradigm's door and streaks.
        private void ApplyGeometryWorldPosition(Vector3 position)
        {
            if (!geometryHasWorldPositionAnchor)
            {
                WorldTarget.position = position;
                return;
            }

            var jsonTime = Context.Atsc.CurrentJsonTime;
            var anchorTime = geometryWorldPositionProperty.GetTransformAnchorTime(jsonTime);
            if (anchorTime == jsonTime)
            {
                WorldTarget.position = position;
                geometryAnchorUpdateVersion = -1;
                return;
            }

            var version = tracks[0].UpdateVersion;
            if (geometryAnchorUpdateVersion != version || geometryAnchorTime != anchorTime)
            {
                geometryAnchoredLocalPosition = geometryParentAnimator.GetTrackParentMatrix(anchorTime)
                    .inverse.MultiplyPoint3x4(position);
                geometryAnchorUpdateVersion = version;
                geometryAnchorTime = anchorTime;
            }

            LocalTarget.localPosition = geometryAnchoredLocalPosition;
        }

        public void AttachToMaterial(GeometryContainer con, string track, bool zeroAlpha = false)
        {
            ResetData();
            materialZeroAlpha = zeroAlpha;

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
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);

            if (container != null && container.ObjectData is BaseGrid obj)
            {
                var noodleAnimationLifetime = time > timeEnd ? -1 : 1;
                if (!(container is ChainContainer))
                {
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
            // Only apply properties supplied by the track. Defaults would reset untouched transform fields.
            if (directEnvironmentTarget)
            {
                ApplyDirectEnvironmentTargets();
                return;
            }

            if (LocalRotation.Count > 0) LocalTarget.localRotation = LocalRotation.Get();

            // Choose the version-specific position source before writing transforms.
            if (trackParentTarget || (TargetType == TargetTypes.Transform && container is GeometryContainer))
            {
                // Apply rotation first so it cannot move the world position written below.
                if (trackParentTarget && !trackParentTargetIsV2 && WorldTarget is Transform && WorldRotation.Count > 0)
                    WorldTarget.localRotation = WorldRotation.Get();
                ApplyTransformPosition(false);
            }
            else
            {
                var hasLocalPosition = LocalPosition.Count > 0;
                var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
                var hasOffsetPosition = OffsetPosition.Count > 0;
                var offsetPosition = hasOffsetPosition ? OffsetPosition.Get() : Vector3.zero;
                if (hasLocalPosition)
                    LocalTarget.localPosition = localPosition;
                else if (hasOffsetPosition)
                    LocalTarget.localPosition = offsetPosition;
            }

            if (Scale.Count > 0) LocalTarget.localScale = Scale.Get();

            // Rotation was already consumed above; reading the drained aggregator would reset it to identity.
            if (WorldTarget is Transform && WorldRotation.Count > 0)
                if (container is not GeometryContainer && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();

            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
            if (!trackParentTarget && container is not GeometryContainer && WorldPosition.Count > 0)
            {
                if (timeBegin < time && time < timeEnd) AnimationTrack.HoldDefinitePosition();
                if (container is not null and not GeometryContainer)
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }

            if (container is ObjectContainer && (Colors.Count > 0 || OpacityArrow.Count > 0 || Opacity.Count > 0 || colorRestorePending))
            {
                if (Colors.Count > 0)
                {
                    var color = Colors.Get();
                    if (materialZeroAlpha)
                        color.a = 0f;
                    switch (container)
                    {
                        case ObstacleContainer obstacle:
                            obstacle.SetColor(color);
                            break;
                        case ChainContainer chain:
                            chain.SetColor(color);
                            break;
                        case NoteContainer note:
                            note.SetColor(color);
                            break;
                        default:
                            container.MpbController.Mpb.SetColor(colorId, color);
                            break;
                    }

                    animatedColorApplied = true;
                }
                else if (colorRestorePending)
                {
                    RestoreAuthoredColor();
                    animatedColorApplied = false;
                }

                colorRestorePending = false;

                if (container is NoteContainer nc)
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 1f - OpacityArrow.Get());

                container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
                container.UpdateMaterials();
            }

            if (UIMode.PreviewMode && !disableNoteLook && container is NoteContainer lookNote)
                ApplyNoteLook(lookNote, time);
        }

        private void ApplyNoteLook(NoteContainer note, float beat)
        {
            var data = note.NoteData;
            var window = data.SongBpmTime - data.SpawnSongBpmTime;
            var blend = window > Mathf.Epsilon
                ? Mathf.Clamp01((beat - data.SpawnSongBpmTime) / window)
                : 1f;
            if (blend <= 0f)
                return;

            var headPos = TracksManager.CameraManager.SelectedCameraController.transform.position;
            var target = note.DirectionTarget.position;
            headPos.y = Mathf.Lerp(headPos.y, target.y, 0.8f);
            var direction = target - headPos;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            var baseRotation = note.DirectionTarget.parent.rotation
                * Quaternion.Euler(note.DirectionTargetEuler);
            var look = Quaternion.LookRotation(direction, baseRotation * Vector3.up);
            note.DirectionTarget.rotation = Quaternion.Slerp(baseRotation, look, blend);
        }

        private void RestoreAuthoredColor()
        {
            var scheme = Context != null ? Context.ColorScheme : null;
            switch (container)
            {
                case NoteContainer note:
                {
                    var data = note.NoteData;
                    if (data == null)
                        break;

                    if (data.CustomColor is Color customColor)
                    {
                        note.SetColor(customColor);
                    }
                    else if (scheme != null
                        && (data.Type == (int)NoteType.Red || data.Type == (int)NoteType.Blue))
                    {
                        note.SetColor(data.Type == (int)NoteType.Red
                            ? scheme.LeftNoteColor
                            : scheme.RightNoteColor);
                    }
                    else
                    {
                        note.SetColor(null);
                    }

                    break;
                }
                case ChainContainer chain:
                {
                    var data = chain.ChainData;
                    if (data == null)
                        break;

                    if (data.CustomColor is Color customColor)
                    {
                        chain.SetColor(customColor);
                    }
                    else if (scheme != null
                        && (data.Color == (int)NoteColor.Red || data.Color == (int)NoteColor.Blue))
                    {
                        chain.SetColor(data.Color == (int)NoteColor.Red
                            ? scheme.LeftNoteColor
                            : scheme.RightNoteColor);
                    }

                    break;
                }
            }
        }

        private bool ApplyDirectEnvironmentTargets()
        {
            var applied = false;
            var positionChanged = false;
            var scaleChanged = false;

            var hasLocalRotation = LocalRotation.Count > 0;
            var localRotation = hasLocalRotation ? LocalRotation.Get() : Quaternion.identity;

            var hasLocalPosition = LocalPosition.Count > 0;
            var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
            var hasWorldPosition = WorldPosition.Count > 0;
            var worldPosition = hasWorldPosition ? WorldPosition.Get() : Vector3.zero;
            var hasOffsetPosition = OffsetPosition.Count > 0;
            var offsetPosition = hasOffsetPosition ? OffsetPosition.Get() : Vector3.zero;
            var hasScale = Scale.Count > 0;
            var scale = hasScale ? Scale.Get() : Vector3.one;
            var hasWorldRotation = WorldRotation.Count > 0;
            var worldRotation = hasWorldRotation ? WorldRotation.Get() : Quaternion.identity;

            var hasProperty = hasLocalRotation || hasLocalPosition || hasWorldPosition
                || hasOffsetPosition || hasScale || hasWorldRotation;
            var updateVersion = tracks[0].UpdateVersion;
            if (directEnvironmentLastTrackUpdateVersion == updateVersion)
                return hasProperty;

            directEnvironmentLastTrackUpdateVersion = updateVersion;

            if (hasLocalRotation)
            {
                LocalTarget.localRotation = localRotation;
                applied = true;
            }

            if (hasLocalPosition || hasWorldPosition || hasOffsetPosition)
            {
                var selectedPosition = hasLocalPosition
                    ? localPosition
                    : hasWorldPosition
                        ? worldPosition
                        : offsetPosition;
                ApplyDirectEnvironmentPosition(
                    selectedPosition,
                    !hasLocalPosition && (hasWorldPosition || directEnvironmentTargetIsV2));
                positionChanged = true;
                applied = true;
            }

            if (hasScale)
            {
                LocalTarget.localScale = scale;
                scaleChanged = true;
                applied = true;
            }

            if (hasWorldRotation)
            {
                WorldTarget.rotation = worldRotation;
                applied = true;
            }

            // Update only animated mesh overrides; untouched fields stay under the light controller's control.
            if (directEnvironmentHasBoxLight)
            {
                if (positionChanged)
                    directEnvironmentBoxLight.CaptureAuthoredPosition();
                if (scaleChanged)
                    directEnvironmentBoxLight.CaptureAuthoredScale();
            }

            if (applied)
                directEnvironmentEverApplied = true;
            return applied;
        }

        private void ApplyTransformPosition(bool includeSpawnDefault)
        {
            var hasLocalPosition = LocalPosition.Count > 0;
            var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
            var hasWorldPosition = WorldPosition.Count > 0;
            var worldPosition = hasWorldPosition ? WorldPosition.Get() : Vector3.zero;
            var hasOffsetPosition = OffsetPosition.Count > 0;
            var offsetPosition = hasOffsetPosition || includeSpawnDefault
                ? OffsetPosition.Get()
                : Vector3.zero;

            if (trackParentTargetIsV2)
            {
                if (hasOffsetPosition || includeSpawnDefault)
                    LocalTarget.localPosition = offsetPosition;
                return;
            }

            if (hasLocalPosition)
                LocalTarget.localPosition = localPosition;
            else if (hasWorldPosition)
            {
                if (trackParentTarget)
                {
                    var source = trackParentPropertySource;
                    // Reapply held world positions only when the property source changes, allowing ancestor motion to carry the child.
                    if (source == null || source.UpdateVersion != trackParentLastUpdateVersion)
                    {
                        LocalTarget.position = worldPosition + WorldTarget.position;
                        if (source != null)
                            trackParentLastUpdateVersion = source.UpdateVersion;
                    }
                }
                else
                {
                    ApplyGeometryWorldPosition(worldPosition);
                }
            }
            else if (hasOffsetPosition)
            {
                if (trackParentTarget)
                    LocalTarget.localPosition = offsetPosition;
                else
                {
                    ApplyGeometryWorldPosition(offsetPosition);
                }
            }
            else if (includeSpawnDefault)
                LocalTarget.localPosition = offsetPosition;
        }

        // Restore the authored pose when seeking before animation. Rings retain their wave displacement,
        // and box lights must recapture that pose before their controller refreshes.
        private void RestoreDirectEnvironmentSpawnPose()
        {
            if (directEnvironmentTargetIsTrackLaneRing && directEnvironmentTrackLaneRing != null)
            {
                var localPosition = LocalTarget.parent != null
                    ? LocalTarget.parent.InverseTransformPoint(directEnvironmentSpawnPosition)
                    : directEnvironmentSpawnPosition;
                directEnvironmentTrackLaneRing.RebasePositionOffset(localPosition);
                LocalTarget.rotation = directEnvironmentSpawnRotation;
                LocalTarget.localScale = directEnvironmentSpawnScale;
                if (directEnvironmentHasBoxLight)
                    directEnvironmentBoxLight.RecaptureAuthoredTransform();
                return;
            }

            LocalTarget.SetPositionAndRotation(directEnvironmentSpawnPosition, directEnvironmentSpawnRotation);
            LocalTarget.localScale = directEnvironmentSpawnScale;
            if (directEnvironmentHasBoxLight)
                directEnvironmentBoxLight.RecaptureAuthoredTransform();
        }

        // Rebase animated ring positions while retaining each segment's wave displacement.
        // Setting position directly would collapse the segments onto the same point.
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

            if (directEnvironmentTarget)
            {
                if (!ApplyDirectEnvironmentTargets() && directEnvironmentEverApplied)
                {
                    RestoreDirectEnvironmentSpawnPose();
                    directEnvironmentEverApplied = false;
                }

                return;
            }

            LocalTarget.localRotation = LocalRotation.Get();

            if (trackParentTarget || (TargetType == TargetTypes.Transform && container is GeometryContainer))
            {
                // Keep stopped-seek ordering consistent with LateUpdate: rotation before world position.
                if (trackParentTarget && !trackParentTargetIsV2 && WorldTarget is Transform)
                    WorldTarget.localRotation = WorldRotation.Get();
                ApplyTransformPosition(true);
            }
            else
            {
                var offsetPosition = OffsetPosition.Get();
                LocalTarget.localPosition = LocalPosition.Count > 0
                    ? LocalPosition.Get()
                    : offsetPosition;
            }

            LocalTarget.localScale = Scale.Get();

            if (TargetType == TargetTypes.Transform && container == null && !trackParentTarget
                && WorldPosition.Count > 0)
            {
                WorldTarget.localPosition = WorldPosition.Get();
            }

            if (WorldTarget is Transform)
            {
                // The V3 parent rotation was consumed before the position write.
                if (!(container is GeometryContainer) && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();
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
                case "_interactable":
                case "interactable":
                    AddPointDef(source, f => Interactable.Add(f), PointDataParsers.ParseFloat, p, 1);
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
            if (AnimateProperty<T>.SkipsMissingPointDefinition(p))
                return;

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

            public bool HoldUntilFlush;
            private bool hasHeldValue;
            private T heldValue;

            public T Get()
            {
                if (HoldUntilFlush && hasHeldValue && Count == Keep)
                    return heldValue;
                if (Count == 0)
                    return Default;
                var value = items[0];
                for (var i = 1; i < Count; ++i)
                    value = Func(value, items[i]);

                if (HoldUntilFlush && Count > Keep)
                {
                    heldValue = value;
                    hasHeldValue = true;
                }

                Count = Keep;
                return value;
            }

            public void Flush()
            {
                Count = Keep;
                hasHeldValue = false;
            }

            public void Reset()
            {
                Default = instancedDefault;
                Count = 0;
                Keep = 0;
                HoldUntilFlush = false;
                hasHeldValue = false;
                heldValue = default;
                for (var i = 0; i < items.Length; i++)
                    items[i] = default;
            }

            private readonly T[] items = new T[4];
        }
    }
}
