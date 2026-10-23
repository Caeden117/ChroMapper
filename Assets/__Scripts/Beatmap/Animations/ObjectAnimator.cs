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
        private bool hasHeldRenderedColor;
        private Color heldRenderedColor;
        private float lastColorSampleTime = float.NegativeInfinity;
        private Color[] heldSliceColors = Array.Empty<Color>();
        private bool[] hasHeldSliceColors = Array.Empty<bool>();
        private bool animatedOpacityApplied;
        private bool opacityRestorePending;
        private bool disableNoteLook;
        private bool deferColorUntilBirth;
        private float colorBirthTime;
        private TrackAnimator controlledTrack;

        public enum TargetTypes
        {
            None,
            GameplayObject,
            Transform,
            Material,
        };

        public TargetTypes TargetType;

        private readonly List<TrackAnimator> tracks = new();
        public bool HasAnimationParents => tracks.Count > 0;
        private string boundObjectCustomData;
        internal BaseGrid AnimationObject => container != null ? container.ObjectData as BaseGrid : null;
        internal bool HasChangedObjectAnimation => TargetType == TargetTypes.GameplayObject
            && AnimationObject != null && boundObjectCustomData != AnimationObject.CustomData?.ToString();
        private Color authoredColor;
        private bool hasAuthoredColor;
        private int customEventSampleFrame = -1;
        private float sampledSongBpmTime;
        private float sampledJsonTime;
        private bool applyingCustomEventSample;
        private float SampledJsonTime => customEventSampleFrame == Time.frameCount || applyingCustomEventSample
            ? sampledJsonTime : Context.Atsc.CurrentJsonTime;

        private bool timeCallbacksSubscribed;

        private void SubscribeTimeCallbacks()
        {
            // The wall collection owns stopped seeks for its active pool.
            if (container is ObstacleContainer) return;
            if (timeCallbacksSubscribed) return;
            Context.Atsc.OnTimeChanged += OnTimeChanged;
            timeCallbacksSubscribed = true;
        }

        private void UnsubscribeTimeCallbacks()
        {
            if (Context == null || !timeCallbacksSubscribed) return;
            Context.Atsc.OnTimeChanged -= OnTimeChanged;
            timeCallbacksSubscribed = false;
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
        private Transform directEnvironmentOriginalParent;
        private bool directEnvironmentEverApplied;
        private int[] directEnvironmentTrackVersions = Array.Empty<int>();
        private bool trackParentTarget;
        private bool trackParentTargetIsV2;
        private TrackAnimator trackParentPropertySource;
        private int trackParentLastUpdateVersion = -1;
        private Vector3 trackParentAnchoredLocalPosition;
        private Matrix4x4 trackParentRootMatrix;
        private Vector3 trackParentSpawnPosition;
        private Quaternion trackParentSpawnRotation;
        private Vector3 trackParentSpawnScale;
        private AnimateProperty<Vector3> geometryWorldPositionProperty;
        private ObjectAnimator geometryParentAnimator;
        private bool geometryHasParentAnimator;
        private int[] geometryTrackVersions = Array.Empty<int>();
        private bool geometryRotationAnchorReady;
        private float geometryRotationAnchorTime;
        private Quaternion geometryAnchoredLocalRotation;
        private Quaternion geometryAnchorWorldRotation;
        private int geometryRotationMatrixRevision = -1;
        private static readonly string[] GeometryTransformKeys =
        {
            AnimationPropertyKeys.PositionKey, AnimationPropertyKeys.LegacyPositionKey,
            AnimationPropertyKeys.LocalPositionKey, AnimationPropertyKeys.LegacyLocalPositionKey,
            AnimationPropertyKeys.RotationKey, AnimationPropertyKeys.LegacyRotationKey,
            AnimationPropertyKeys.LocalRotationKey, AnimationPropertyKeys.LegacyLocalRotationKey,
            AnimationPropertyKeys.ScaleKey, AnimationPropertyKeys.LegacyScaleKey
        };
        private bool geometryHasWorldPositionAnchor;
        private int geometryAnchorUpdateVersion = -1;
        private int geometryAnchorMatrixRevision = -1;
        private float geometryAnchorTime;
        private Vector3 geometryAnchoredLocalPosition;
        private static int trackParentMatrixRevision;
        private int trackParentAppliedMatrixRevision = -1;
        private int cachedParentMatrixRevision = -1;
        private int cachedParentMatrixSample = -1;
        private Dictionary<float, ParentMatrixSample> parentMatrixSamples;

        private struct ParentMatrixSample
        {
            public Matrix4x4 Self;
            public Matrix4x4 Children;
            public bool HasChildren;
        }

        // Source edits and parenting can change an ancestor without changing a descendant's source.
        internal static void InvalidateTrackParentMatrices() => ++trackParentMatrixRevision;

        // Materials upgraded from Standard to unlit Glowing must keep zero alpha during color animation.
        private bool materialZeroAlpha;
        private JSONNode materialTrackData;
        private AnimateProperty<Color>[] materialPathColors = Array.Empty<AnimateProperty<Color>>();
        private AnimateProperty<Color>[] chainPathColors = Array.Empty<AnimateProperty<Color>>();
        private AnimateProperty<Color>[] objectPathColors = Array.Empty<AnimateProperty<Color>>();

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int arrowDissolveId = Shader.PropertyToID("_ArrowDissolve");
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private static readonly int cutoutTexOffsetId = Shader.PropertyToID("_CutoutTexOffset");
        private static readonly int animSpawnedId = Shader.PropertyToID("_AnimationSpawned");

        public void ResetData() => ResetData(false);

        private void ResetData(bool retainTrackAttachments)
        {
            if (trackParentTarget) InvalidateTrackParentMatrices();
            var wasDirectEnvironmentTarget = directEnvironmentTarget;
            if (wasDirectEnvironmentTarget && LocalTarget != null)
                ClearDirectTrackParent();
            hasAuthoredColor = false;
            InvalidateColorHistory();
            heldSliceColors = Array.Empty<Color>();
            hasHeldSliceColors = Array.Empty<bool>();
            customEventSampleFrame = -1;
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
            ResetDirectEnvironmentTrackVersions();
            directEnvironmentSpawnPosition = Vector3.zero;
            directEnvironmentSpawnRotation = Quaternion.identity;
            directEnvironmentSpawnScale = Vector3.one;
            directEnvironmentParentAnimator = null;
            directEnvironmentOriginalParent = null;
            directEnvironmentHasParentAnimator = false;
            trackParentTarget = false;
            trackParentTargetIsV2 = false;
            trackParentPropertySource = null;
            trackParentLastUpdateVersion = -1;
            trackParentAppliedMatrixRevision = -1;
            geometryWorldPositionProperty = null;
            geometryParentAnimator = null;
            geometryHasParentAnimator = false;
            geometryRotationAnchorReady = false;
            geometryRotationMatrixRevision = -1;
            geometryTrackVersions = Array.Empty<int>();
            controlledTrack = null;
            geometryHasWorldPositionAnchor = false;
            geometryHasParentAnimator = false;
            geometryAnchorUpdateVersion = -1;
            geometryAnchorMatrixRevision = -1;
            materialZeroAlpha = false;
            materialTrackData = null;
            materialPathColors = Array.Empty<AnimateProperty<Color>>();
            chainPathColors = Array.Empty<AnimateProperty<Color>>();
            objectPathColors = Array.Empty<AnimateProperty<Color>>();
            deferColorUntilBirth = false;

            if (retainTrackAttachments)
                UnsubscribeTimeCallbacks();
            else
            {
                OnDisable();
                tracks.Clear();
            }

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
            animatedOpacityApplied = false;
            opacityRestorePending = false;
            disableNoteLook = false;

            time = null;
            AnimatedLife = false;
            ShouldRecycle = false;

            if (LocalTarget != null && !wasDirectEnvironmentTarget)
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
                    Random.onUnitSphere * 10f);
                container.MpbController.Mpb.SetFloat(animSpawnedId, 0);
                if (container is NoteContainer nc)
                {
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 0);
                    nc.ArrowMpbController.Mpb.SetFloat(arrowDissolveId, 1f);
                    nc.ArrowMpbController.Mpb.SetVector(
                        cutoutTexOffsetId,
                        Random.onUnitSphere * 10f);
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
            if (trackParentTarget) trackParentLastUpdateVersion = -1;
            if (directEnvironmentTarget) ResetDirectEnvironmentTrackVersions();
            if (TargetType == TargetTypes.Transform && container is GeometryContainer)
                InvalidateGeometryAnchors();

            if (reattached)
                OnTimeChanged();
        }

        private void OnDisable()
        {
            customEventSampleFrame = -1;
            InvalidateColorHistory();
            // Discard pending track values on disable so the next seek cannot combine them with stale contributions.
            if (trackParentTarget)
            {
                FlushPendingAnimations();
            }
            UnsubscribeTimeCallbacks();

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
            opacityRestorePending |= animatedOpacityApplied;
        }

        internal void InvalidateGeometryAnchors()
        {
            geometryAnchorUpdateVersion = -1;
            geometryRotationAnchorReady = false;
            geometryAnchorMatrixRevision = -1;
            if (trackParentTarget && controlledTrack != null)
                controlledTrack.InvalidateChildGeometryAnchors();
        }

        internal bool HasCustomEventSampleAt(float jsonTime) =>
            customEventSampleFrame == Time.frameCount && sampledJsonTime == jsonTime;

        public void ClearAnimationContributions()
        {
            FlushPendingAnimations();
            customEventSampleFrame = -1;
            time = null;
        }

        public void ApplyCustomEventSample(bool transformsOnly, float? sampleTime = null)
        {
            if (!isActiveAndEnabled) return;
            sampledSongBpmTime = sampleTime ?? Context.Atsc.CurrentSongBpmTime;
            sampledJsonTime = BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(sampledSongBpmTime)
                ?? sampledSongBpmTime;
            applyingCustomEventSample = true;
            try
            {
                if (!transformsOnly) SampleAt(sampledSongBpmTime);
                // Sampling can recycle a note, clearing its data and disabling its hierarchy.
                if (!isActiveAndEnabled) return;
                if (directEnvironmentTarget)
                {
                    if (!ApplyDirectEnvironmentTargets() && directEnvironmentEverApplied)
                    {
                        RestoreDirectEnvironmentSpawnPose();
                        directEnvironmentEverApplied = false;
                    }
                }
                LateUpdate();
                if (!transformsOnly) customEventSampleFrame = Time.frameCount;
            }
            finally
            {
                applyingCustomEventSample = false;
            }
        }

        public void RefreshTrackAnimation()
        {
            InvalidateColorHistory();
            if (TargetType == TargetTypes.Material
                || TargetType == TargetTypes.GameplayObject && container != null && container.ObjectData is BaseGrid)
            {
                RefreshPathAnimation();
                return;
            }
            if (TargetType == TargetTypes.Transform && container is GeometryContainer)
                RefreshGeometryAnchor();
            ClearAnimationContributions();
            if (directEnvironmentTarget)
            {
                RestoreDirectEnvironmentSpawnPose();
                ResetDirectEnvironmentTrackVersions();
            }
            else if (LocalTarget != null && TargetType == TargetTypes.Transform)
            {
                LocalTarget.SetLocalPositionAndRotation(OffsetPosition.Default, LocalRotation.Default);
                LocalTarget.localScale = Scale.Default;
            }
            foreach (var track in tracks) track.PushToChild(this);
        }

        // Track curve edits do not change a note's own paths, lifetime, travel track or authored defaults.
        internal void RefreshTrackContributions()
        {
            InvalidateColorHistory();
            if (TargetType is not (TargetTypes.GameplayObject or TargetTypes.Material))
            {
                RefreshTrackAnimation();
                return;
            }
            ClearAnimationContributions();
            foreach (var track in tracks) track.PushToChild(this);
        }

        public void AttachToObject(BaseGrid obj) => AttachToObject(obj, false);

        private void AttachToObject(BaseGrid obj, bool retainTrackAttachments)
        {
            ResetData(retainTrackAttachments);
            boundObjectCustomData = obj.CustomData?.ToString();

            TargetType = TargetTypes.GameplayObject;
            OffsetPosition.HoldUntilFlush = true;
            LocalRotation.HoldUntilFlush = true;
            WorldRotation.HoldUntilFlush = true;
            Scale.HoldUntilFlush = true;
            authoredColor = Colors.Default;
            hasAuthoredColor = true;

            enabled = UIMode.AnimationMode && TracksManager != null;
            if (!enabled)
            {
                ApplyEditorObstacleColor(obj);
                return;
            }

            obj.RecomputeSpawnParameters();
            deferColorUntilBirth = container is ObstacleContainer or ArcContainer;
            RefreshColorBirthTime(obj);
            var noteLookKey = BeatSaberSongContainer.Instance.Map.MajorVersion == 2 ? "_disableNoteLook" : "disableNoteLook";
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

            var preparation = TracksManager.PathAnimations.Prepare(obj, BeatSaberSongContainer.Instance.Map);
            if (!retainTrackAttachments)
                foreach (var name in preparation.TrackDefinitions.Names) AddParent(name);
            foreach (var definition in preparation.ObjectDefinitions)
                if (ObjectPathAnimationRegistry.IsColor(definition.PropertyName)) BindPreparedPathDefinition(definition);
            var hasObjectColor = AnimatedProperties.ContainsKey(AnimationPropertyKeys.ColorKey)
                || AnimatedProperties.ContainsKey(AnimationPropertyKeys.LegacyColorKey);
            BindPreparedPathDefinitions(preparation.TrackDefinitions.Definitions, hasObjectColor);
            if (preparation.TrackDefinitions.Names.Length > 0)
                AnimationTrack.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);
            BindPreparedPathDefinitions(preparation.ObjectDefinitions, true);
            var bug = preparation.HasDefinitePosition;

            // Reading AsBool on a missing SimpleJSON node inserts a default and invalidates the object snapshot.
            var gravityFlag = obj.CustomData?["_disableNoteGravity"];
            if (gravityFlag == null) gravityFlag = obj.CustomData?["disableNoteGravity"];
            if (bug && gravityFlag != null && gravityFlag.AsBool)
            {
                Debug.LogError($"disableNoteGravity is bugged when combined with definitePosition, please remove it! {BeatmapLogContext.Describe(obj)}");
                var position = AnimationTrack.ObjectParentTransform.localPosition;
                position.y = (position.y * -0.1f) + 1;
                AnimationTrack.ObjectParentTransform.localPosition = position;
            }

            var boundProperties = new List<IAnimateProperty>(AnimatedProperties.Count);
            var colors = new List<AnimateProperty<Color>>();
            foreach (var property in AnimatedProperties.Values)
            {
                property.CompletePreparedOrder();
                if (property is AnimateProperty<Color> color) colors.Add(color);
                if (container is not ChainContainer || property is not AnimateProperty<Color>)
                    boundProperties.Add(property);
            }
            properties = boundProperties.ToArray();
            objectPathColors = colors.ToArray();
            if (container is ChainContainer) chainPathColors = objectPathColors;

            // Pool callbacks attach notes after the shared tracks have sampled this frame.
            ClearAnimationContributions();
            foreach (var track in tracks) track.PushToChild(this, Context.Atsc.CurrentJsonTime);
            ApplyCustomEventSample(false);

            SubscribeTimeCallbacks();
        }

        private void ApplyEditorObstacleColor(BaseGrid obj)
        {
            if (container is not ObstacleContainer obstacle || obj.CustomAnimation == null)
                return;

            var key = BeatSaberSongContainer.Instance.Map.MajorVersion == 2
                ? AnimationPropertyKeys.LegacyColorKey
                : AnimationPropertyKeys.ColorKey;
            if (!obj.CustomAnimation.HasKey(key))
                return;

            // Editing keeps walls on the grid, but their initial authored color must survive disabling motion.
            var parameters = new IPointDefinition.UntypedParams
            {
                Key = key,
                Points = obj.CustomAnimation[key],
                LogSource = obj,
                Overwrite = true,
                PreparedIndex = 0,
                PreparedLength = 1
            };
            AddPointDef<Color>(null, Colors.Add, PointDataParsers.ParseColor, parameters, Color.white);
            if (!AnimatedProperties.TryGetValue(key, out var property))
                return;

            var colorProperty = (AnimateProperty<Color>)property;
            if (colorProperty.IsEmpty() || colorProperty.PointDefinitions[0].Points.Length == 0)
                return;

            obstacle.SetColor(colorProperty.PointDefinitions[0].Interpolate(0f));
        }

        internal void RefreshColorBirthTime(BaseGrid obj)
        {
            if (!deferColorUntilBirth) return;
            var birth = TracksManager.GetColorBirthTime(obj);
            if (birth != colorBirthTime) InvalidateColorHistory();
            colorBirthTime = birth;
        }

        // Event edits rebind affected objects without replacing their authored color with an animated color.
        public void RefreshPathAnimation()
        {
            InvalidateColorHistory();
            if (TargetType == TargetTypes.Material)
            {
                var trackData = materialTrackData;
                var zeroAlpha = materialZeroAlpha;
                RestoreAuthoredColor();
                container.UpdateMaterials();
                AttachToMaterial((GeometryContainer)container, trackData, zeroAlpha);
                return;
            }
            if (TargetType != TargetTypes.GameplayObject) return;

            var obj = (BaseGrid)container.ObjectData;
            if (HasChangedObjectAnimation) obj.PreparedPathAnimation = null;
            var retainTrackAttachments = false;
            if (isActiveAndEnabled && UIMode.AnimationMode && TracksManager != null)
            {
                var names = TracksManager.PathAnimations.Prepare(obj, BeatSaberSongContainer.Instance.Map)
                    .TrackDefinitions.Names;
                retainTrackAttachments = tracks.Count == names.Length;
                for (var i = 0; retainTrackAttachments && i < names.Length; i++)
                    retainTrackAttachments = tracks[i] == TracksManager.GetAnimationTrack(names[i]);
            }

            // Rebuilding points must not rebuild every shared track's child array for each note.
            // All authored state and the individual travel track still reset normally.
            RestoreAuthoredColor();
            AttachToObject(obj, retainTrackAttachments);
        }

        public void AttachToGeometry(BaseEnvironmentEnhancement eh)
        {
            // MapLoader supplies the incoming version before swapping the shared map during a difficulty change.
            var v2 = TracksManager.IsV2Map;
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = AnimationThis.transform;
            WorldTarget = AnimationThis.transform;

            if (eh.Scale is Vector3 scale) Scale.Default = scale;
            if (eh.Position is Vector3 p) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * p;
            if (eh.LocalPosition is Vector3 lp) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * lp;
            if (eh.Rotation is Vector3 r) LocalRotation.Default = Quaternion.Euler(r.x, r.y, r.z);
            if (eh.LocalRotation is Vector3 lr) LocalRotation.Default = Quaternion.Euler(lr.x, lr.y, lr.z);

            if (eh.TrackNames.Count > 0)
            {
                foreach (var name in eh.TrackNames) AddParent(name);
                container.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);

                RefreshGeometryAnchor();
            }

            SubscribeTimeCallbacks();

            OnTimeChanged();
        }

        /// <summary>Binds a prefab's current authored pose to a track name or array of track names and samples it immediately.</summary>
        /// <param name="vivifyObject">The initialized prefab whose root this animator controls.</param>
        /// <param name="track">A string or array of strings. Supplied track channels replace the authored pose channels.</param>
        public void AttachToVivify(VivifyObject vivifyObject, JSONNode track)
        {
            var position = vivifyObject.transform.localPosition;
            var rotation = vivifyObject.transform.localRotation;
            var scale = vivifyObject.transform.localScale;
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = AnimationThis.transform;
            WorldTarget = AnimationThis.transform;
            LocalTarget.SetLocalPositionAndRotation(position, rotation);
            LocalTarget.localScale = scale;
            vivifyObject.SetAnimatorDefault();

            enabled = true;
            if (track is JSONString name)
                AddParent(name.Value);
            else if (track is JSONArray names)
                foreach (var child in names.Children)
                    if (child is JSONString item) AddParent(item.Value);
            if (tracks.Count > 0)
                vivifyObject.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);

            foreach (var parent in tracks) parent.PushToChild(this, Context.Atsc.CurrentJsonTime);
            ApplyCustomEventSample(false);

            SubscribeTimeCallbacks();
        }

        // Only supplied track properties overwrite an environment object's authored transform.
        public void AttachToEnvironmentObject(
            Transform target,
            string track,
            bool v2,
            ParametricBoxLight boxLight = null) =>
            AttachToEnvironmentObject(target, new[] { track }, v2, boxLight);

        public void AttachToEnvironmentObject(Transform target, IReadOnlyList<string> trackNames,
            bool v2, ParametricBoxLight boxLight = null)
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

            // Ring interpolation adds its wave to PositionOffset. Capture the authored base,
            // otherwise restoration would rebase to the visible pose and add the wave twice.
            directEnvironmentSpawnPosition = directEnvironmentTargetIsTrackLaneRing
                ? target.parent != null
                    ? target.parent.TransformPoint(directEnvironmentTrackLaneRing.PositionOffset)
                    : directEnvironmentTrackLaneRing.PositionOffset
                : target.position;
            directEnvironmentSpawnRotation = target.rotation;
            directEnvironmentSpawnScale = target.localScale;
            directEnvironmentOriginalParent = target.parent;

            foreach (var name in trackNames) AddParent(name);
            directEnvironmentTrackVersions = new int[tracks.Count];
            ResetDirectEnvironmentTrackVersions();
            // Physically parent enhanced objects only when their track has an AssignTrackParent ancestor.
            // Other tracks leave the environment's original hierarchy intact.
            var trackAnimator = tracks[^1];
            foreach (var source in tracks)
                if (source.Parents.Count > 0) trackAnimator = source;
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
            if (TargetType == TargetTypes.Transform && container is GeometryContainer)
                RefreshGeometryAnchor();
            if (!directEnvironmentTarget)
                return;

            LocalTarget.SetParent(trackParent, worldPositionStays);
            CaptureDirectEnvironmentParent(trackParent.GetComponentInParent<ObjectAnimator>());
        }

        public void ClearDirectTrackParent()
        {
            geometryHasWorldPositionAnchor = false;
            geometryHasParentAnimator = false;
            geometryRotationAnchorReady = false;
            geometryAnchorUpdateVersion = -1;
            geometryAnchorMatrixRevision = -1;
            if (!directEnvironmentTarget) return;
            directEnvironmentParentAnimator = null;
            directEnvironmentHasParentAnimator = false;
            ResetDirectEnvironmentTrackVersions();
            // Scene teardown can destroy the target before the event manager clears its track ownership.
            if (LocalTarget == null)
            {
                directEnvironmentParentMatrix = Matrix4x4.identity;
                directEnvironmentSpawnLocalPosition = Vector3.zero;
                return;
            }
            var originalParent = directEnvironmentOriginalParent != null ? directEnvironmentOriginalParent : null;
            directEnvironmentTargetIsTrackLaneRing = directEnvironmentTrackLaneRing != null;
            directEnvironmentHasBoxLight = directEnvironmentBoxLight != null;
            LocalTarget.SetParent(originalParent, true);
            RestoreDirectEnvironmentSpawnPose();
            CaptureDirectEnvironmentParent(null);
        }

        private void CaptureDirectEnvironmentParent(ObjectAnimator parentAnimator)
        {
            directEnvironmentParentAnimator = parentAnimator;
            directEnvironmentHasParentAnimator = parentAnimator != null;
            directEnvironmentParentMatrix = LocalTarget.parent != null
                ? LocalTarget.parent.localToWorldMatrix
                : Matrix4x4.identity;
            directEnvironmentSpawnLocalPosition = directEnvironmentTargetIsTrackLaneRing
                ? directEnvironmentTrackLaneRing.PositionOffset : LocalTarget.localPosition;
        }

        // Sample sorted track properties without moving scene objects or changing the playhead.
        // Native laser resets read world Z at dispatch, rather than at snapshot construction.
        private float GetEnvironmentPositionZAt(float jsonTime)
        {
            var parent = directEnvironmentHasParentAnimator
                ? directEnvironmentParentAnimator.GetTrackParentMatrix(jsonTime)
                : directEnvironmentParentMatrix;
            var localKey = directEnvironmentTargetIsV2
                ? AnimationPropertyKeys.LegacyLocalPositionKey
                : AnimationPropertyKeys.LocalPositionKey;
            var worldKey = directEnvironmentTargetIsV2
                ? AnimationPropertyKeys.LegacyPositionKey
                : AnimationPropertyKeys.PositionKey;
            var units = directEnvironmentTargetIsV2 ? BeatmapConstant.LaneSize : 1f;
            var localPosition = Vector3.zero;
            var worldPosition = Vector3.zero;
            var hasLocalPosition = false;
            var hasWorldPosition = false;
            foreach (var source in tracks)
            {
                if (source.AnimatedProperties.TryGetValue(localKey, out var localProperty)
                    && jsonTime >= localProperty.StartTime)
                {
                    localPosition += ((AnimateProperty<Vector3>)localProperty).GetLerpedValue(jsonTime);
                    hasLocalPosition = true;
                }
                if (source.AnimatedProperties.TryGetValue(worldKey, out var worldProperty)
                    && jsonTime >= worldProperty.StartTime)
                {
                    worldPosition += ((AnimateProperty<Vector3>)worldProperty).GetLerpedValue(jsonTime);
                    hasWorldPosition = true;
                }
            }
            if (hasLocalPosition)
                return parent.MultiplyPoint3x4(localPosition * units).z;

            if (hasWorldPosition)
            {
                return worldPosition.z * units;
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

        public void AttachToTrack(Track track, string name, bool? isV2Map = null)
        {
            ResetData();

            TargetType = TargetTypes.Transform;

            trackParentTarget = true;
            controlledTrack = track.GetComponent<TrackAnimator>();
            trackParentTargetIsV2 = isV2Map ?? BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;
            trackParentSpawnPosition = WorldTarget.localPosition;
            trackParentSpawnRotation = WorldTarget.localRotation;
            trackParentSpawnScale = WorldTarget.localScale;

            SubscribeTimeCallbacks();
        }

        public void SetTrackParentMapVersion(bool isV2Map)
        {
            if (trackParentTargetIsV2 != isV2Map) InvalidateTrackParentMatrices();
            trackParentTargetIsV2 = isV2Map;
        }

        // Reapply held world positions only when the property source changes, allowing ancestor motion to carry the child.
        public void BindPropertySource(TrackAnimator source)
        {
            InvalidateTrackParentMatrices();
            trackParentPropertySource = source;
            trackParentLastUpdateVersion = -1;
            trackParentAppliedMatrixRevision = -1;
            trackParentRootMatrix = WorldTarget.parent != null
                ? WorldTarget.parent.localToWorldMatrix : Matrix4x4.identity;
        }

        // Sample the parent pose without depending on Unity's update order or moving the playhead.
        private Matrix4x4 GetTrackParentSelfMatrix(float jsonTime)
        {
            RefreshParentMatrixSample();
            if (parentMatrixSamples.TryGetValue(jsonTime, out var cached)) return cached.Self;

            // A source can gain a parent after this child was attached. Read the current binding
            // when rebuilding the cache rather than retaining its earlier ancestor.
            var parentAnimator = trackParentPropertySource != null ? trackParentPropertySource.Animator : null;
            var ancestor = parentAnimator != null && parentAnimator.trackParentTarget
                ? parentAnimator.GetTrackParentMatrix(jsonTime) : trackParentRootMatrix;
            var rotation = ReadTrackProperty(trackParentPropertySource,
                AnimationPropertyKeys.LegacyRotationKey, AnimationPropertyKeys.RotationKey, jsonTime,
                trackParentSpawnRotation);
            var self = ancestor * Matrix4x4.TRS(trackParentSpawnPosition, rotation, trackParentSpawnScale);
            parentMatrixSamples[jsonTime] = new ParentMatrixSample { Self = self };
            return self;
        }

        private Matrix4x4 GetTrackParentMatrix(float jsonTime)
        {
            RefreshParentMatrixSample();
            if (parentMatrixSamples.TryGetValue(jsonTime, out var cached) && cached.HasChildren)
                return cached.Children;

            var source = trackParentPropertySource;
            var self = GetTrackParentSelfMatrix(jsonTime);
            var localRotation = ReadTrackProperty(source,
                AnimationPropertyKeys.LegacyLocalRotationKey, AnimationPropertyKeys.LocalRotationKey,
                jsonTime, Quaternion.identity);
            var scale = ReadTrackProperty(source,
                AnimationPropertyKeys.LegacyScaleKey, AnimationPropertyKeys.ScaleKey, jsonTime, Vector3.one);
            var offset = ReadTrackProperty(source,
                AnimationPropertyKeys.LegacyPositionKey, AnimationPropertyKeys.OffsetPositionKey, jsonTime, Vector3.zero)
                * BeatmapConstant.LaneSize;
            var position = offset;
            if (!trackParentTargetIsV2)
            {
                if (source.AnimatedProperties.TryGetValue(AnimationPropertyKeys.LocalPositionKey, out var localProperty)
                    && jsonTime >= localProperty.StartTime)
                {
                    position = ((AnimateProperty<Vector3>)localProperty).GetLerpedValue(jsonTime);
                }
                else if (source.AnimatedProperties.TryGetValue(AnimationPropertyKeys.PositionKey, out var worldProperty)
                    && jsonTime >= worldProperty.StartTime)
                {
                    var property = (AnimateProperty<Vector3>)worldProperty;
                    var world = property.GetLerpedValue(jsonTime);
                    var anchorTime = property.GetTransformAnchorTime(jsonTime);
                    var anchor = anchorTime == jsonTime
                        ? self
                        : GetTrackParentSelfMatrix(anchorTime);
                    position = anchor.inverse.MultiplyVector(world);
                }
            }

            var matrix = self * Matrix4x4.TRS(position, localRotation, scale);
            parentMatrixSamples[jsonTime] = new ParentMatrixSample
            {
                Self = self,
                Children = matrix,
                HasChildren = true
            };
            return matrix;
        }

        private void RefreshParentMatrixSample()
        {
            var sample = PointDefinitionParser.SampleVersion;
            if (cachedParentMatrixSample == sample && cachedParentMatrixRevision == trackParentMatrixRevision)
                return;

            // A history rebuild can evaluate several beats in one Unity frame. Endpoint anchors
            // need separate entries, while siblings at the same time share one ancestor evaluation.
            parentMatrixSamples ??= new Dictionary<float, ParentMatrixSample>();
            parentMatrixSamples.Clear();
            cachedParentMatrixSample = sample;
            cachedParentMatrixRevision = trackParentMatrixRevision;
        }

        private static T ReadTrackProperty<T>(
            TrackAnimator source, string legacyKey, string key, float time, T defaultValue) where T : struct
        {
            if (source == null) return defaultValue;
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
        private void RefreshGeometryAnchor()
        {
            geometryWorldPositionProperty = null;
            geometryParentAnimator = tracks.Count > 0 ? tracks[0].Animator : null;
            geometryHasParentAnimator = geometryParentAnimator != null && geometryParentAnimator.trackParentTarget;
            if (tracks.Count > 0)
            {
                var key = BeatSaberSongContainer.Instance.Map.MajorVersion == 2
                    ? AnimationPropertyKeys.LegacyPositionKey : AnimationPropertyKeys.PositionKey;
                if (tracks[0].AnimatedProperties.TryGetValue(key, out var property))
                    geometryWorldPositionProperty = property as AnimateProperty<Vector3>;
            }
            geometryHasWorldPositionAnchor = geometryHasParentAnimator;
            geometryTrackVersions = new int[tracks.Count];
            InvalidateGeometryAnchors();
        }

        private float GetGeometryTransformAnchorTime(float jsonTime)
        {
            var anchor = float.NegativeInfinity;
            foreach (var source in tracks)
            {
                foreach (var key in GeometryTransformKeys)
                {
                    if (source.AnimatedProperties.TryGetValue(key, out var property) && jsonTime >= property.StartTime)
                        anchor = Mathf.Max(anchor, property.GetTransformAnchorTime(jsonTime));
                }
            }
            return float.IsNegativeInfinity(anchor) ? jsonTime : anchor;
        }

        private void ApplyGeometryWorldPosition(Vector3 position)
        {
            if (!geometryHasWorldPositionAnchor)
            {
                WorldTarget.position = position;
                return;
            }

            var jsonTime = SampledJsonTime;
            var anchorTime = GetGeometryTransformAnchorTime(jsonTime);
            var changed = false;
            for (var i = 0; i < tracks.Count; i++)
            {
                var source = tracks[i];
                changed |= geometryTrackVersions[i] != source.UpdateVersion;
                geometryTrackVersions[i] = source.UpdateVersion;
            }
            if (anchorTime == jsonTime)
            {
                // Parent LateUpdate can run after this child, so project through its sampled pose even during an active write.
                LocalTarget.localPosition = geometryParentAnimator.GetTrackParentMatrix(anchorTime)
                    .inverse.MultiplyPoint3x4(position);
                geometryAnchorUpdateVersion = -1;
                return;
            }

            if (geometryAnchorUpdateVersion < 0 || changed || geometryAnchorTime != anchorTime
                || geometryAnchorMatrixRevision != trackParentMatrixRevision)
            {
                geometryAnchoredLocalPosition = geometryParentAnimator.GetTrackParentMatrix(anchorTime)
                    .inverse.MultiplyPoint3x4(position);
                geometryAnchorUpdateVersion = 0;
                geometryAnchorTime = anchorTime;
                geometryAnchorMatrixRevision = trackParentMatrixRevision;
            }

            LocalTarget.localPosition = geometryAnchoredLocalPosition;
        }

        public void AttachToMaterial(GeometryContainer con, ObjectAnimator animator, string track, bool zeroAlpha = false)
        {
            TracksManager = animator.TracksManager;
            Context = animator.Context;
            AttachToMaterial(con, (JSONNode)new JSONString(track), zeroAlpha);
        }

        public void AttachToMaterial(GeometryContainer con, string track, bool zeroAlpha = false) =>
            AttachToMaterial(con, (JSONNode)new JSONString(track), zeroAlpha);

        public void AttachToMaterial(GeometryContainer con, JSONNode trackData, bool zeroAlpha = false)
        {
            ResetData();
            materialZeroAlpha = zeroAlpha;
            materialTrackData = trackData;
            TargetType = TargetTypes.Material;
            container = con;
            authoredColor = con.MpbController.Mpb.GetColor(colorId);
            hasAuthoredColor = true;
            Colors.Default = authoredColor;
            enabled = true;
            var preparation = TracksManager.PathAnimations.PrepareTracks(trackData, BeatSaberSongContainer.Instance.Map);
            foreach (var name in preparation.Names) AddParent(name);
            foreach (var definition in preparation.Definitions)
                if (ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
                {
                    var parameters = definition.Parameters;
                    parameters.TimeBegin = 0f;
                    parameters.TimeEnd = 1f;
                    AddPointDef(parameters, definition.PropertyName, definition.Source);
                }
            materialPathColors = AnimatedProperties.Values.OfType<AnimateProperty<Color>>().ToArray();
            foreach (var path in materialPathColors) path.CompletePreparedOrder();
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
            if (customEventSampleFrame == Time.frameCount) return;
            SampleAt(Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
        }

        public void SampleAt(float songBpmTime)
        {
            var time = this.time ?? songBpmTime;

            var l = properties.Length;
            for (var i = 0; i < l; ++i)
            {
                var prop = properties[i];
                if (time >= prop.StartTime) prop.UpdateProperty(time);
            }

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

            foreach (var path in materialPathColors)
                if (path.HasValueAt(time)) Colors.Add(path.GetPathValue(time, 0f));
            if (AnimatedTrack)
            {
                AnimationTrack.UpdateTime(time);
                if (WorldPosition.Count > 0 && timeBegin < time && time < timeEnd)
                    AnimationTrack.HoldDefinitePosition();
            }
        }

        public void LateUpdate()
        {
            if (!applyingCustomEventSample && customEventSampleFrame == Time.frameCount) return;
            if (TargetType == TargetTypes.Material)
            {
                ApplyAnimatedColor();
                return;
            }
            // Only apply properties supplied by the track. Defaults would reset untouched transform fields.
            if (directEnvironmentTarget)
            {
                ApplyDirectEnvironmentTargets();
                return;
            }

            var singleTransformTarget = TargetType == TargetTypes.Transform && LocalTarget == WorldTarget;
            if (singleTransformTarget)
            {
                if (container is GeometryContainer) ApplyGeometryRotation(applyingCustomEventSample);
                else ApplyTransformRotation(applyingCustomEventSample);
            }
            else if (LocalRotation.Count > 0 || applyingCustomEventSample)
                LocalTarget.localRotation = LocalRotation.Get();

            // Choose the version-specific position source before writing transforms.
            if (trackParentTarget || singleTransformTarget)
            {
                // Apply rotation first so it cannot move the world position written below.
                if (trackParentTarget && !trackParentTargetIsV2 && WorldTarget is Transform && (WorldRotation.Count > 0 || applyingCustomEventSample))
                    WorldTarget.localRotation = WorldRotation.Get();
                ApplyTransformPosition(applyingCustomEventSample);
            }
            else
            {
                var hasLocalPosition = LocalPosition.Count > 0;
                var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
                var hasOffsetPosition = OffsetPosition.Count > 0;
                var offsetPosition = hasOffsetPosition ? OffsetPosition.Get() : Vector3.zero;
                if (hasLocalPosition)
                    LocalTarget.localPosition = localPosition;
                else if (hasOffsetPosition || applyingCustomEventSample)
                    LocalTarget.localPosition = hasOffsetPosition ? offsetPosition : OffsetPosition.Default;
            }

            if (Scale.Count > 0 || applyingCustomEventSample) LocalTarget.localScale = Scale.Get();

            // Rotation was already consumed above; reading the drained aggregator would reset it to identity.
            if (WorldTarget is Transform && (WorldRotation.Count > 0 || applyingCustomEventSample))
                if (!singleTransformTarget && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();

            var time = this.time ?? (applyingCustomEventSample ? sampledSongBpmTime
                : Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
            if (!trackParentTarget && !singleTransformTarget && WorldPosition.Count > 0)
            {
                if (timeBegin < time && time < timeEnd) AnimationTrack.HoldDefinitePosition();
                if (container is not null and not GeometryContainer)
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }

            ApplyAnimatedColor();

            if (UIMode.PreviewMode && !disableNoteLook && container is NoteContainer lookNote)
                ApplyNoteLook(lookNote, time);
        }

        private void ApplyAnimatedColor()
        {
            if (container == null) return;
            var songTime = applyingCustomEventSample ? sampledSongBpmTime : Context.Atsc.CurrentSongBpmTime;
            var pathTime = time ?? songTime;
            var beforeBirth = deferColorUntilBirth && songTime < colorBirthTime;
            if (beforeBirth)
            {
                InvalidateColorHistory();
                Colors.Flush();
                colorRestorePending |= animatedColorApplied;
            }
            var hasChainPath = false;
            foreach (var path in chainPathColors) hasChainPath |= path.HasValueAt(pathTime);
            var hasColor = Colors.Count > 0;
            var color = hasColor ? Colors.Get() : Color.white;
            if (!beforeBirth && !hasColor && !hasChainPath
                && (TargetType is TargetTypes.GameplayObject or TargetTypes.Material))
            {
                var birth = TargetType == TargetTypes.Material ? float.NegativeInfinity
                    : deferColorUntilBirth ? colorBirthTime : timeBegin;
                if (CanUseRenderedColor(songTime) && hasHeldRenderedColor)
                {
                    color = heldRenderedColor;
                    hasColor = true;
                }
                else hasColor = TryGetHeldColor(songTime, pathTime, birth, null, out color);
            }
            var updateColor = hasColor || hasChainPath;
            if (container is ChainContainer chainContainer && !beforeBirth && chainPathColors.Length > 0)
            {
                color = hasColor ? color : hasChainPath ? Color.white : authoredColor;
                ApplyChainPathColors(chainContainer, color, songTime, pathTime);
                updateColor = true;
            }
            else if (updateColor)
            {
                if (materialZeroAlpha) color.a = 0f;
                switch (container)
                {
                    case ObstacleContainer obstacle: obstacle.SetColor(color); break;
                    case ChainContainer chain: chain.SetColor(color); break;
                    case NoteContainer note: note.SetColor(color); break;
                    default: container.MpbController.Mpb.SetColor(colorId, color); break;
                }
            }
            else if (colorRestorePending)
                RestoreAuthoredColor();

            if (updateColor && !beforeBirth)
            {
                heldRenderedColor = color;
                hasHeldRenderedColor = true;
            }
            else hasHeldRenderedColor = false;
            lastColorSampleTime = songTime;
            animatedColorApplied = updateColor;
            var updateAppearance = updateColor || colorRestorePending || OpacityArrow.Count > 0
                || Opacity.Count > 0 || opacityRestorePending;
            colorRestorePending = false;
            if (!updateAppearance) return;
            animatedOpacityApplied = Opacity.Count > 0 || OpacityArrow.Count > 0;
            opacityRestorePending = false;
            if (container is NoteContainer nc)
            {
                var dissolve = OpacityArrow.Get();
                nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 1f - dissolve);
                nc.ArrowMpbController.Mpb.SetFloat(arrowDissolveId, dissolve);
            }
            container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
            container.UpdateMaterials();
        }

        // Every slice is a native note with its own birth and cut time. Transform paths keep the chain clock.
        private void ApplyChainPathColors(ChainContainer chain, Color globalColor, float songTime, float pathTime)
        {
            chain.SetColor(globalColor);
            var data = chain.ChainData;
            if (heldSliceColors.Length != data.SliceCount - 1)
            {
                heldSliceColors = new Color[data.SliceCount - 1];
                hasHeldSliceColors = new bool[data.SliceCount - 1];
            }
            var hasGlobalColor = HasGlobalColorAt(songTime);
            var keepRendered = CanUseRenderedColor(songTime);
            var headBirth = TracksManager.GetColorBirthTime(data);
            for (var i = 0; i < data.SliceCount - 1; i++)
            {
                var cutTime = Mathf.Lerp(data.SongBpmTime, data.TailSongBpmTime,
                    (i + 1f) / (data.SliceCount - 1f));
                var birth = headBirth + (cutTime - data.SongBpmTime);
                var normalized = Mathf.Clamp01((pathTime - (cutTime - data.HalfJumpDuration))
                    / (data.HalfJumpDuration * 2f));
                var color = globalColor;
                var hasPath = false;
                foreach (var path in chainPathColors)
                    if (path.HasValueAt(pathTime))
                    {
                        color *= path.GetPathValue(pathTime, normalized);
                        hasPath = true;
                    }
                if (!hasPath && !hasGlobalColor)
                {
                    if (keepRendered && hasHeldSliceColors[i]) color = heldSliceColors[i];
                    else if (TryGetHeldColor(songTime, pathTime, birth, cutTime, out var held)) color = held;
                    else color = authoredColor;
                }
                chain.SetNodeColor(i, color);
                heldSliceColors[i] = color;
                hasHeldSliceColors[i] = songTime >= birth;
            }
        }

        private void InvalidateColorHistory()
        {
            hasHeldRenderedColor = false;
            lastColorSampleTime = float.NegativeInfinity;
            Array.Clear(hasHeldSliceColors, 0, hasHeldSliceColors.Length);
        }

        private bool CanUseRenderedColor(float songTime) =>
            !PointDefinitionParser.ResetSmoothing && songTime >= lastColorSampleTime
            && (Context.Atsc.IsPlaying || songTime == lastColorSampleTime);

        private bool HasGlobalColorAt(float songTime)
        {
            var jsonTime = BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(songTime) ?? songTime;
            foreach (var source in tracks)
                foreach (var key in ColorPropertyKeys)
                    if (source.AnimatedProperties.TryGetValue(key, out var property)
                        && property is AnimateProperty<Color> color && color.HasValueAt(jsonTime)) return true;
            return false;
        }

        private static readonly string[] ColorPropertyKeys =
            { AnimationPropertyKeys.LegacyColorKey, AnimationPropertyKeys.ColorKey };

        // Color cancellation preserves the last write for existing objects. Sampling that boundary also
        // makes a direct seek agree with playback without replaying every intermediate event or frame.
        private bool TryGetHeldColor(float songTime, float pathTime, float birth, float? sliceCutTime, out Color color)
        {
            var map = BeatSaberSongContainer.Instance.Map;
            var jsonTime = map.SongBpmTimeToJsonTime(songTime) ?? songTime;
            var writeTime = float.NegativeInfinity;
            foreach (var source in tracks)
                foreach (var key in ColorPropertyKeys)
                    if (source.AnimatedProperties.TryGetValue(key, out var property)
                        && property is AnimateProperty<Color> global
                        && global.TryGetLastWriteTime(jsonTime, out var last))
                        writeTime = Mathf.Max(writeTime, (float)map.JsonTimeToSongBpmTime(last));
            var paths = TargetType == TargetTypes.Material ? materialPathColors : objectPathColors;
            foreach (var path in paths)
                if (path.TryGetLastWriteTime(pathTime, out var last)) writeTime = Mathf.Max(writeTime, last);
            color = Color.white;
            if (float.IsNegativeInfinity(writeTime) || writeTime < birth) return false;
            var writeJsonTime = map.SongBpmTimeToJsonTime(writeTime) ?? writeTime;
            var hasValue = false;
            foreach (var source in tracks)
                foreach (var key in ColorPropertyKeys)
                    if (source.AnimatedProperties.TryGetValue(key, out var property)
                        && property is AnimateProperty<Color> global && global.HasValueAt(writeJsonTime))
                    {
                        color *= global.GetLerpedValue(writeJsonTime);
                        hasValue = true;
                    }
            foreach (var path in paths)
                if (path.HasValueAt(writeTime))
                {
                    color *= TargetType == TargetTypes.Material ? path.GetPathValue(writeTime, 0f)
                        : sliceCutTime.HasValue && container is ChainContainer chain
                            ? path.GetPathValue(writeTime, Mathf.Clamp01(
                                (writeTime - (sliceCutTime.Value - chain.ChainData.HalfJumpDuration))
                                / (chain.ChainData.HalfJumpDuration * 2f)))
                            : path.GetLerpedValue(writeTime);
                    hasValue = true;
                }
            return hasValue;
        }

        // Pool setup can change cut directions after the first pose sample. Reapply only direction,
        // leaving inline transforms, definite-position holding and material values untouched.
        internal void RefreshNoteDirection()
        {
            if (!isActiveAndEnabled || TargetType != TargetTypes.GameplayObject || container is not NoteContainer note) return;
            var beat = time ?? Context.Atsc.CurrentSongBpmTime;
            if (AnimatedTrack) AnimationTrack.UpdateNoteRotation(beat);
            if (UIMode.PreviewMode && !disableNoteLook) ApplyNoteLook(note, beat);
        }

        private void ApplyNoteLook(NoteContainer note, float beat)
        {
            var data = note.NoteData;
            if (data == null || data.Chains.Count > 0) return;
            var normalTime = Mathf.Clamp01((beat - data.SpawnSongBpmTime) / (data.HalfJumpDuration * 2f));
            var baseRotation = note.DirectionTarget.parent.rotation;
            var spawnRotation = Track.GetNoteSpawnRotation(note, normalTime);
            var authoredRotation = baseRotation * spawnRotation;
            var inverseWorldRotation = Quaternion.Inverse(WorldTarget.rotation);
            // The movable Preview camera must not turn notes away from the authored player pose.
            var headPos = TracksManager.CameraManager.PlayerCameraController.transform.position;
            var target = note.DirectionTarget.position;
            var headHeight = (inverseWorldRotation * headPos).y;
            var targetHeight = (inverseWorldRotation * target).y;
            headPos += WorldTarget.rotation * Vector3.up * (Mathf.Lerp(headHeight, targetHeight, 0.8f) - headHeight);
            var direction = target - headPos;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            // Noodle rotates the head ray through the authored base pose. Spawn progress remains independent of travel.
            var lookDirection = baseRotation * (inverseWorldRotation * direction.normalized);
            var look = Quaternion.LookRotation(lookDirection, authoredRotation * Vector3.up);
            note.DirectionTarget.rotation = Quaternion.Lerp(authoredRotation, look, normalTime * 2f);
        }

        private void RestoreAuthoredColor()
        {
            if (hasAuthoredColor && TargetType == TargetTypes.Material && container != null)
            {
                container.MpbController.Mpb.SetColor(colorId, authoredColor);
                container.UpdateMaterials();
                return;
            }
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
                case ArcContainer arc:
                    arc.SetColor(arc.ArcData.CustomColor
                        ?? (arc.ArcData.Color == (int)NoteColor.Red ? scheme.LeftNoteColor : scheme.RightNoteColor));
                    break;
                case ObstacleContainer obstacle:
                    obstacle.SetColor(obstacle.ObstacleData.CustomColor ?? Colors.Default);
                    break;
                case GeometryContainer geometry:
                    geometry.MpbController.Mpb.SetColor(colorId, Colors.Default);
                    break;
            }
        }

        private void ResetDirectEnvironmentTrackVersions()
        {
            for (var i = 0; i < directEnvironmentTrackVersions.Length; i++)
                directEnvironmentTrackVersions[i] = -1;
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
            var changed = false;
            for (var i = 0; i < tracks.Count; i++)
            {
                changed |= directEnvironmentTrackVersions[i] != tracks[i].UpdateVersion;
                directEnvironmentTrackVersions[i] = tracks[i].UpdateVersion;
            }
            if (!changed) return hasProperty;

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

            if (hasWorldRotation && !hasLocalRotation)
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

        // Heck chooses localRotation over rotation for a transform target. Sharing the two aggregators
        // multiplied noncommuting quaternions in dictionary order, which changed after deleting and undoing events.
        private void ApplyGeometryRotation(bool restoreDefault)
        {
            var hasLocal = LocalRotation.Count > 0;
            var hasWorld = WorldRotation.Count > 0;
            var local = LocalRotation.Get();
            var world = WorldRotation.Get();
            if (hasLocal || (restoreDefault && !hasWorld))
            {
                LocalTarget.localRotation = local;
                geometryRotationAnchorReady = false;
            }
            else if (hasWorld)
            {
                if (!geometryHasParentAnimator)
                {
                    WorldTarget.rotation = world;
                    return;
                }
                var jsonTime = SampledJsonTime;
                var anchorTime = GetGeometryTransformAnchorTime(jsonTime);
                if (anchorTime == jsonTime)
                {
                    LocalTarget.localRotation = Quaternion.Inverse(geometryParentAnimator.GetTrackParentMatrix(anchorTime).rotation) * world;
                    geometryRotationAnchorReady = false;
                    return;
                }
                // A held world rotation must inherit later parent rotation, just as its held world position does.
                if (!geometryRotationAnchorReady || geometryRotationAnchorTime != anchorTime || geometryAnchorWorldRotation != world
                    || geometryRotationMatrixRevision != trackParentMatrixRevision)
                {
                    geometryAnchoredLocalRotation = Quaternion.Inverse(geometryParentAnimator.GetTrackParentMatrix(anchorTime).rotation) * world;
                    geometryRotationAnchorTime = anchorTime;
                    geometryAnchorWorldRotation = world;
                    geometryRotationAnchorReady = true;
                    geometryRotationMatrixRevision = trackParentMatrixRevision;
                }
                LocalTarget.localRotation = geometryAnchoredLocalRotation;
            }
        }

        private void ApplyTransformRotation(bool restoreDefault)
        {
            var hasLocal = LocalRotation.Count > 0;
            var hasWorld = WorldRotation.Count > 0;
            var local = LocalRotation.Get();
            var world = WorldRotation.Get();
            if (hasLocal || (restoreDefault && !hasWorld))
                LocalTarget.localRotation = local;
            else if (hasWorld)
                WorldTarget.rotation = world;
        }

        public void RestoreRemovedTrackProperty(string key)
        {
            if (ObjectPathAnimationRegistry.IsColor(key)) InvalidateColorHistory();
            if (key is AnimationPropertyKeys.LegacyTimeKey or AnimationPropertyKeys.TimeKey)
                SetLifeTime(-1);

            if ((key is AnimationPropertyKeys.LegacyDissolveKey or AnimationPropertyKeys.DissolveKey) && container != null)
            {
                Opacity.Flush();
                container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
                container.UpdateMaterials();
            }

            if (!directEnvironmentTarget)
                return;

            // Other properties can remain animated when one is deleted, so a whole-pose scrub reset is insufficient.
            switch (key)
            {
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    LocalTarget.localScale = directEnvironmentSpawnScale;
                    if (directEnvironmentHasBoxLight)
                        directEnvironmentBoxLight.CaptureAuthoredScale();

                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                case AnimationPropertyKeys.PositionKey:
                case AnimationPropertyKeys.LegacyLocalPositionKey:
                case AnimationPropertyKeys.LocalPositionKey:
                    ApplyDirectEnvironmentPosition(directEnvironmentHasParentAnimator
                        ? directEnvironmentSpawnLocalPosition
                        : directEnvironmentSpawnPosition, !directEnvironmentHasParentAnimator);
                    if (directEnvironmentHasBoxLight)
                        directEnvironmentBoxLight.CaptureAuthoredPosition();

                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.RotationKey:
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    LocalTarget.rotation = directEnvironmentSpawnRotation;
                    break;
            }
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
                    if (source == null)
                        LocalTarget.position = worldPosition + WorldTarget.position;
                    else
                    {
                        var changed = source.UpdateVersion != trackParentLastUpdateVersion
                            || trackParentAppliedMatrixRevision != trackParentMatrixRevision;
                        if (changed)
                        {
                            // The held point is relative to the parent's pose at its final write.
                            var property = (AnimateProperty<Vector3>)source.AnimatedProperties[AnimationPropertyKeys.PositionKey];
                            var anchorTime = property.GetTransformAnchorTime(SampledJsonTime);
                            trackParentAnchoredLocalPosition = GetTrackParentSelfMatrix(anchorTime)
                                .inverse.MultiplyVector(worldPosition);
                            trackParentLastUpdateVersion = source.UpdateVersion;
                            trackParentAppliedMatrixRevision = trackParentMatrixRevision;
                        }
                        // Workspace changes can reset the proxy without changing its source curve.
                        if (changed || includeSpawnDefault && LocalTarget.localPosition != trackParentAnchoredLocalPosition)
                            LocalTarget.localPosition = trackParentAnchoredLocalPosition;
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
            // Environment transforms can be destroyed before their track controllers during scene teardown.
            if (LocalTarget == null) return;
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
            if (customEventSampleFrame == Time.frameCount && sampledSongBpmTime == Context.Atsc.CurrentSongBpmTime) return;

            ApplyStoppedSeek();
        }

        internal void ApplyStoppedSeek()
        {
            if (!isActiveAndEnabled) return;
            ClearAnimationContributions();
            foreach (var track in tracks) track.PushToChild(this, Context.Atsc.CurrentJsonTime);
            ApplyCustomEventSample(false);
        }

        private void ApplyCurrentPose()
        {

            if (directEnvironmentTarget)
            {
                if (!ApplyDirectEnvironmentTargets() && directEnvironmentEverApplied)
                {
                    RestoreDirectEnvironmentSpawnPose();
                    directEnvironmentEverApplied = false;
                }

                return;
            }

            var singleTransformTarget = TargetType == TargetTypes.Transform && LocalTarget == WorldTarget;
            if (singleTransformTarget)
            {
                if (container is GeometryContainer) ApplyGeometryRotation(true);
                else ApplyTransformRotation(true);
            }
            else
                LocalTarget.localRotation = LocalRotation.Get();

            if (trackParentTarget || singleTransformTarget)
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

            if (TargetType == TargetTypes.Transform && !singleTransformTarget && container == null && !trackParentTarget
                && WorldPosition.Count > 0)
            {
                WorldTarget.localPosition = WorldPosition.Get();
            }

            if (WorldTarget is Transform)
            {
                // The V3 parent rotation was consumed before the position write.
                if (!singleTransformTarget && (!trackParentTarget || trackParentTargetIsV2))
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

        private void BindPreparedPathDefinitions(ObjectPathAnimationRegistry.Definition[] definitions, bool skipColor = false)
        {
            foreach (var definition in definitions)
            {
                if (skipColor && ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
                    continue;

                BindPreparedPathDefinition(definition);
            }
        }

        private void BindPreparedPathDefinition(ObjectPathAnimationRegistry.Definition definition)
        {
            var parameters = definition.Parameters;
            parameters.TimeBegin = timeBegin;
            parameters.TimeEnd = timeEnd;
            if (ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
            {
                if (container is ArcContainer arc)
                {
                    // Native SliderUpdateColorize divides by jumpDuration + .75 * jumpDuration + span.
                    parameters.TimeEnd = timeBegin + (arc.ArcData.HalfJumpDuration * 3.5f)
                        + arc.ArcData.DurationSongBpmTime;
                }
                else if (container is ChainContainer)
                {
                    parameters.TimeBegin = 0f;
                    parameters.TimeEnd = 1f;
                }
            }
            AddPointDef(parameters, definition.PropertyName, definition.Source);
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
                case AnimationPropertyKeys.LegacyDissolveKey:
                case AnimationPropertyKeys.DissolveKey:
                    AddPointDef(source, f => Opacity.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case AnimationPropertyKeys.LegacyDissolveArrowKey:
                case AnimationPropertyKeys.DissolveArrowKey:
                    AddPointDef(source, f => OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    AddPointDef(
                        source,
                        q => LocalRotation.Add(q),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.OffsetWorldRotationKey:
                    AddPointDef(
                        source,
                        v => WorldRotation.Add(v),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                case AnimationPropertyKeys.OffsetPositionKey:
                    AddPointDef(
                        source,
                        v => OffsetPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyDefinitePositionKey:
                case AnimationPropertyKeys.DefinitePositionKey:
                    AddPointDef(
                        source,
                        v => WorldPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    AddPointDef(
                        source,
                        v => Scale.Add(v),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.one);
                    break;
                case AnimationPropertyKeys.LegacyColorKey:
                case AnimationPropertyKeys.ColorKey:
                    AddPointDef<Color>(source, (Color c) => Colors.Add(c), PointDataParsers.ParseColor, p, Color.white);
                    break;
                case AnimationPropertyKeys.LegacyInteractableKey:
                case AnimationPropertyKeys.InteractableKey:
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
            if (typeof(T) == typeof(Color) && (p.Points == null || p.Points.IsNull))
            {
                if (source != null)
                {
                    var property = GetAnimateProperty(p.Key, setter, @default);
                    property.UsePathClock = true;
                    property.AddNullDefinition(p.Time, source);
                }
                return;
            }
            if (AnimateProperty<T>.SkipsMissingPointDefinition(p, source)) return;

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

                var property = GetAnimateProperty(p.Key, setter, @default);
                property.UsePathClock = typeof(T) == typeof(Color);
                property.AddPreparedPointDef(parser, p, source);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not load animation property. {new AnimationLogContext(p, source).Describe()}\n{e}");
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

        public class Aggregator<T> where T : struct
        {
            public int Count;
            public readonly Func<T, T, T> Func;
            public T Default;
            private readonly T instancedDefault;
            public int Keep;
            private T value;
            private T retainedValue;

            public Aggregator(T def, Func<T, T, T> func)
            {
                Default = def;
                instancedDefault = def;
                Func = func;
            }

            // Track arrays have no contributor limit. Fold their values without allocating each frame.
            public void Add(T contribution)
            {
                value = Count == 0
                    ? contribution
                    : Func(value, contribution);
                Count++;
            }

            public void Preload(T contribution)
            {
                Add(contribution);
                retainedValue = Keep == 0
                    ? contribution
                    : Func(retainedValue, contribution);
                Keep++;
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

                var result = value;
                if (HoldUntilFlush && Count > Keep)
                {
                    heldValue = result;
                    hasHeldValue = true;
                }
                Count = Keep;
                value = retainedValue;
                return result;
            }

            public void Flush()
            {
                Count = Keep;
                value = retainedValue;
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
                value = default;
                retainedValue = default;
            }
        }
    }
}
