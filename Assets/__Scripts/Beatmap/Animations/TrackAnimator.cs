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
    public class TrackAnimator : MonoBehaviour
    {
        public AudioTimeSyncController Atsc;
        public Track Track;
        public ObjectAnimator Animator;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new();
        private IAnimateProperty[] properties = new IAnimateProperty[0];
        private readonly Dictionary<string, Action<ObjectAnimator, float>> childPushers = new();
        private int sampledFrame = -1;
        public float SampledJsonTime { get; private set; }
        public int UpdateVersion { get; private set; }
        public bool ParentWorldPositionStays;

        public List<TrackAnimator> Parents = new();
        public List<ObjectAnimator> Children = new();
        public readonly List<ObjectAnimator> CachedChildren = new();
        public event Action ChildrenChanged;

        public void AddEvent(BaseCustomEvent ev, bool refresh = true)
        {
            foreach (var jprop in ev.Data)
            {
                if ((jprop.Value == null || jprop.Value.IsNull)
                    && !ObjectPathAnimationRegistry.IsColor(jprop.Key))
                {
                    AnimatedProperties.Remove(jprop.Key);
                    childPushers.Remove(jprop.Key);
                    continue;
                }
                var p = new IPointDefinition.UntypedParams
                {
                    Key = jprop.Key,
                    Points = jprop.Value,
                    Easing = ev.DataEasing,
                    Time = ev.JsonTime,
                    Duration = ev.DataDuration ?? 0,
                    TimeBegin = ev.JsonTime,
                    TimeEnd = ev.JsonTime + (ev.DataDuration ?? 0),
                    Repeat = ev.DataRepeat ?? 0
                };
                AddPointDef(p, jprop.Key, ev);
            }

            if (refresh) RefreshProperties();
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            foreach (var prop in AnimatedProperties.Keys.ToList())
            {
                AnimatedProperties[prop].RemoveEvent(ev);
                if (AnimatedProperties[prop].IsEmpty())
                {
                    foreach (var child in CachedChildren)
                        if (child != null) child.RestoreRemovedTrackProperty(prop);
                    AnimatedProperties.Remove(prop);
                    childPushers.Remove(prop);
                }
            }

            RefreshProperties();
        }

        public void ResetEvents(bool refresh = true)
        {
            ObjectAnimator.InvalidateTrackParentMatrices();
            AnimatedProperties.Clear();
            childPushers.Clear();
            properties = Array.Empty<IAnimateProperty>();
            if (refresh) RefreshProperties();
        }

        // Rebuild whole channels: a removed or null-valued event can reveal earlier definitions.
        internal void RebuildEvents(IReadOnlyList<BaseCustomEvent> events)
        {
            ResetEvents(false);
            foreach (var ev in events)
                if (ev.Type == "AnimateTrack") AddEvent(ev, false);
            RefreshProperties(false);
        }

        public void RefreshProperties() => RefreshProperties(true);

        internal void RefreshProperties(bool refreshChildren)
        {
            ObjectAnimator.InvalidateTrackParentMatrices();
            UpdateVersion++;
            properties = new IAnimateProperty[AnimatedProperties.Count];
            var i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            InvalidateChildGeometryAnchors();
            SampleAt(Atsc != null ? Atsc.CurrentJsonTime : 0);
            if (!refreshChildren) return;
            // Refresh can recycle children; iterate the snapshot rather than the mutable attachment list.
            var children = CachedChildren.ToArray();
            foreach (var child in children)
                if (child != null) child.RefreshTrackAnimation();
        }

        public void Update()
        {
            if (sampledFrame == Time.frameCount) return;
            // Unity time controllers need explicit null checks before reading their playback time.
            var time = Atsc != null ? Atsc.CurrentJsonTime : 0;
            SampleAt(time);
        }

        public void SampleAt(float time)
        {
            PointDefinitionParser.BeginSample(time);
            sampledFrame = Time.frameCount;
            SampledJsonTime = time;
            if (CachedChildren.Count == 0)
            {
                enabled = false;
                if (Animator != null) Animator.enabled = false;
                return;
            }

            var changed = false;
            for (var i = 0; i < properties.Length; ++i)
            {
                var prop = properties[i];
                changed |= time >= prop.StartTime ? prop.UpdateProperty(time) : prop.ResetEvaluatedValue();
            }
            if (changed) UpdateVersion++;
        }

        public void AddChild(ObjectAnimator oa)
        {
            if (Children.Contains(oa)) return;
            Children.Add(oa);
            if (oa != null && oa.enabled) CachedChildren.Add(oa);
            RefreshChildActivation();
            ChildrenChanged?.Invoke();
        }

        public void RemoveChild(ObjectAnimator oa)
        {
            if (!Children.Remove(oa)) return;
            CachedChildren.Remove(oa);
            RefreshChildActivation();
            ChildrenChanged?.Invoke();
        }

        public void OnChildrenChanged()
        {
            CachedChildren.Clear();
            foreach (var child in Children)
                if (child != null && child.enabled) CachedChildren.Add(child);
            RefreshChildActivation();
            ChildrenChanged?.Invoke();
        }

        private void RefreshChildActivation()
        {
            enabled = CachedChildren.Count > 0;
            if (Animator == null || Animator.enabled == enabled) return;
            Animator.enabled = enabled;
            // Only an activation change affects the parent's authored child order.
            foreach (var parent in Parents) parent.OnChildrenChanged();
        }

        internal void InvalidateChildGeometryAnchors()
        {
            foreach (var child in CachedChildren) child.InvalidateGeometryAnchors();
        }

        public void DestroyTrackBoundEnvironmentObjects()
        {
            foreach (var child in Children.ToArray())
                if (child != null) child.DestroyTrackBoundEnvironmentTarget();
        }

        public void PushToChild(ObjectAnimator child, float? jsonTime = null)
        {
            var time = jsonTime ?? (sampledFrame >= 0 ? SampledJsonTime : Atsc != null ? Atsc.CurrentJsonTime : 0);
            // Pool setup can request held values after attachment already applied this sample.
            if (child.HasCustomEventSampleAt(time)) return;
            foreach (var push in childPushers.Values) push(child, time);
        }

        public void RefreshPathAnimations()
        {
            foreach (var child in Children.ToArray())
                if (child != null) child.RefreshPathAnimation();
        }

        public void CompleteEventLoad() => RefreshProperties();

        public void PushOnStoppedTimeChanged()
        {
            if (isActiveAndEnabled && Atsc != null && !Atsc.IsPlaying) SampleAt(Atsc.CurrentJsonTime);
        }

        public void ResetForMapLoad()
        {
            ResetEvents(false);
            if (Track != null)
            {
                Track.SelfTransform.localPosition = Vector3.zero;
                Track.SelfTransform.localRotation = Quaternion.identity;
                Track.SelfTransform.localScale = Vector3.one;
                Track.ObjectParentTransform.localPosition = Vector3.zero;
                Track.ObjectParentTransform.localRotation = Quaternion.identity;
                Track.ObjectParentTransform.localScale = Vector3.one;
            }
            Parents.Clear();
            Children.Clear();
            CachedChildren.Clear();
            ParentWorldPositionStays = false;
            sampledFrame = -1;
            UpdateVersion = 0;
            enabled = false;
            if (Animator != null) Animator.enabled = false;
            ChildrenChanged?.Invoke();
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
                case AnimationPropertyKeys.LegacyDissolveKey:
                case AnimationPropertyKeys.DissolveKey:
                    AddPointDef<float>(
                        source,
                        (ObjectAnimator animator, float f) => animator.Opacity.Add(f),
                        PointDataParsers.ParseFloat,
                        p,
                        1);
                    break;
                case AnimationPropertyKeys.LegacyDissolveArrowKey:
                case AnimationPropertyKeys.DissolveArrowKey:
                    AddPointDef<float>(
                        source,
                        (ObjectAnimator animator, float f) => animator.OpacityArrow.Add(f),
                        PointDataParsers.ParseFloat,
                        p,
                        1);
                    break;
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    AddPointDef<Quaternion>(
                        source,
                        (ObjectAnimator animator, Quaternion v) => animator.LocalRotation.Add(v),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.RotationKey:
                    AddPointDef<Quaternion>(
                        source,
                        (ObjectAnimator animator, Quaternion v) =>
                        {
                            if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                                animator.WorldRotation.Add(v);
                        },
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.OffsetWorldRotationKey:
                    AddPointDef<Quaternion>(
                        source,
                        (ObjectAnimator animator, Quaternion v) => animator.WorldRotation.Add(v),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                    AddPointDef<Vector3>(
                        source,
                        (ObjectAnimator animator, Vector3 v) =>
                            animator.OffsetPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.OffsetPositionKey:
                    AddPointDef<Vector3>(
                        source,
                        (ObjectAnimator animator, Vector3 v) =>
                        {
                            if (animator.TargetType == ObjectAnimator.TargetTypes.GameplayObject)
                                animator.OffsetPosition.Add(v * BeatmapConstant.LaneSize);
                        },
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LocalPositionKey:
                    AddPointDef<Vector3>(
                        source,
                        (ObjectAnimator animator, Vector3 v) =>
                        {
                            if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                                animator.LocalPosition.Add(v);
                        },
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyLocalPositionKey:
                    AddPointDef<Vector3>(source, (animator, value) =>
                    {
                        if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                            animator.LocalPosition.Add(value * BeatmapConstant.LaneSize);
                    }, PointDataParsers.ParseVector3, p, Vector3.zero);
                    break;
                case AnimationPropertyKeys.PositionKey:
                    AddPointDef<Vector3>(
                        source,
                        (ObjectAnimator animator, Vector3 v) =>
                        {
                            if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                                animator.WorldPosition.Add(v);
                        },
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    AddPointDef<Vector3>(
                        source,
                        (ObjectAnimator animator, Vector3 v) => animator.Scale.Add(v),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.one);
                    break;
                case AnimationPropertyKeys.LegacyColorKey:
                case AnimationPropertyKeys.ColorKey:
                    AddPointDef<Color>(
                        source,
                        (ObjectAnimator animator, Color v) =>
                        {
                            if (animator.TargetType != ObjectAnimator.TargetTypes.Transform) animator.Colors.Add(v);
                        },
                        PointDataParsers.ParseColor,
                        p,
                        Color.white);
                    break;
                case AnimationPropertyKeys.LegacyTimeKey:
                case AnimationPropertyKeys.TimeKey:
                    AddPointDef<float>(
                        source,
                        (ObjectAnimator animator, float f) => animator.SetLifeTime(f),
                        PointDataParsers.ParseFloat,
                        p,
                        -1);
                    break;
                case AnimationPropertyKeys.LegacyInteractableKey:
                case AnimationPropertyKeys.InteractableKey:
                    AddPointDef<float>(source, (animator, value) => animator.Interactable.Add(value),
                        PointDataParsers.ParseFloat, p, 1);
                    break;
            }
        }

        private void AddPointDef<T>(
            BaseCustomEvent source,
            Action<ObjectAnimator, T> _setter,
            PointDefinition<T>.Parser parser,
            IPointDefinition.UntypedParams p,
            T _default) where T : struct
        {
            Action<T> setter = (v) =>
            {
                for (var i = 0; i < CachedChildren.Count; ++i) _setter(CachedChildren[i], v);
            };

            if (AnimateProperty<T>.SkipsMissingPointDefinition(p, source)) return;
            var property = GetAnimateProperty<T>(p.Key, setter, _default);
            if (p.Points == null || p.Points.IsNull)
                property.AddNullDefinition(p.Time, source);
            else
                property.AddPointDef(parser, p, source);
            childPushers[p.Key] = (child, time) =>
            {
                if (property.HasValueAt(time)) _setter(child, property.GetLerpedValue(time));
            };
        }

        private AnimateProperty<T> GetAnimateProperty<T>(string key, Action<T> setter, T _default) where T : struct
        {
            if (!AnimatedProperties.ContainsKey(key))
            {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>(),
                    setter,
                    _default
                );
            }

            return AnimatedProperties[key] as AnimateProperty<T>;
        }
    }
}
