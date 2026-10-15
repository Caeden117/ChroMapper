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

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        // Removing an event can remove dictionary entries, so keep a reusable snapshot of the properties.
        private readonly List<KeyValuePair<string, IAnimateProperty>> properties = new();

        public int UpdateVersion { get; private set; }

        public List<TrackAnimator> Parents = new List<TrackAnimator>();
        public List<ObjectAnimator> Children = new List<ObjectAnimator>();
        public readonly List<ObjectAnimator> CachedChildren = new();

        private readonly Dictionary<string, Action<ObjectAnimator>> childPushers = new();

        // Enhancements attach after parenting events load, so keep worldPositionStays for new children.
        public bool ParentWorldPositionStays;

        public void AddEvent(BaseCustomEvent ev, bool refresh = true)
        {
            foreach (var jprop in ev.Data)
            {
                // In heck, null stops writing this property, and objects already holding a value keep it.
                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    if (AnimatedProperties.Remove(jprop.Key))
                    {
                        childPushers.Remove(jprop.Key);
                    }

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

            if (refresh)
                RefreshProperties();
        }

        public void CompleteEventLoad() => RefreshProperties();

        public void RemoveEvent(BaseCustomEvent ev)
        {
            foreach (var pair in properties)
            {
                var prop = pair.Key;
                pair.Value.RemoveEvent(ev);
                if (pair.Value.IsEmpty())
                {
                    foreach (var child in CachedChildren)
                        child.RestoreRemovedTrackProperty(prop);

                    AnimatedProperties.Remove(prop);
                    childPushers.Remove(prop);
                }
            }
            RefreshProperties(true);
        }

        private void RefreshProperties(bool refreshTargets = false)
        {
            // Structural edits invalidate held target poses even when the remaining sampled values are unchanged.
            UpdateVersion++;
            properties.Clear();
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties.Add(prop);
            }

            DoUpdate();
            if (refreshTargets)
                foreach (var child in CachedChildren)
                    child.RefreshTrackAnimation();
        }

        private bool preload = false;

        public void Update() => DoUpdate();

        private void DoUpdate()
        {
            var time = Atsc.CurrentJsonTime;
            if (CachedChildren.Count == 0)
            {
                enabled = false;
                if (Animator != null) Animator.enabled = false;
                return;
            }
            var changed = false;
            for (var i = 0; i < properties.Count; ++i)
            {
                var prop = properties[i].Value;
                if (time >= prop.StartTime)
                {
                    changed |= prop.UpdateProperty(time);
                }
                else
                {
                    changed |= prop.ResetEvaluatedValue();
                }
            }
            if (changed)
                UpdateVersion++;
        }

        public void AddChild(ObjectAnimator oa)
        {
            Children.Add(oa);
            if (oa.enabled)
                CachedChildren.Add(oa);
            RefreshChildActivation();
        }

        public void RemoveChild(ObjectAnimator oa)
        {
            Children.Remove(oa);
            CachedChildren.Remove(oa);
            RefreshChildActivation();
        }

        public void RefreshPathAnimations()
        {
            // Reattachment updates Children, so retain only the affected track's current children for this edit.
            foreach (var child in Children.ToArray())
                child.RefreshPathAnimation();
        }

        public void PushToChild(ObjectAnimator child)
        {
            foreach (var push in childPushers.Values)
                push(child);
        }

        public void OnChildrenChanged()
        {
            CachedChildren.Clear();
            foreach (var child in Children)
            {
                if (child.enabled)
                    CachedChildren.Add(child);
            }

            RefreshChildActivation();
        }

        private void RefreshChildActivation()
        {
            enabled = CachedChildren.Count > 0;
            if (Animator == null || Animator.enabled == enabled)
                return;

            Animator.enabled = enabled;
            // A track proxy stays in its parents' child lists while inactive. Only an activation change
            // affects those parents, and their child order must remain the authored order.
            foreach (var parent in Parents)
                parent.OnChildrenChanged();
        }

        // Push values before ObjectAnimator applies them. Playback already pushes each frame.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc.IsPlaying)
                return;
            DoUpdate();
        }

        // Named tracks survive map loads; clear their animation and parenting state before reuse.
        public void ResetForMapLoad()
        {
            // Reset in local space so a parent's old pose cannot leak into a child's reset.
            Track.SelfTransform.localPosition = Vector3.zero;
            Track.SelfTransform.localRotation = Quaternion.identity;
            Track.SelfTransform.localScale = Vector3.one;
            Track.ObjectParentTransform.localPosition = Vector3.zero;
            Track.ObjectParentTransform.localRotation = Quaternion.identity;
            Track.ObjectParentTransform.localScale = Vector3.one;

            AnimatedProperties.Clear();
            properties.Clear();
            childPushers.Clear();
            UpdateVersion = 0;
            Parents.Clear();
            Children.Clear();
            CachedChildren.Clear();
            ParentWorldPositionStays = false;
            enabled = false;
            if (Animator != null)
                Animator.enabled = false;
        }

        public void DestroyTrackBoundEnvironmentObjects()
        {
            foreach (var child in Children.ToArray())
            {
                child.DestroyTrackBoundEnvironmentTarget();
            }
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
                case AnimationPropertyKeys.LegacyDissolveKey:
                case AnimationPropertyKeys.DissolveKey:
                    AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.Opacity.Add(f), PointDataParsers.ParseFloat, p, 1);
                    break;
                case AnimationPropertyKeys.LegacyDissolveArrowKey:
                case AnimationPropertyKeys.DissolveArrowKey:
                    AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 1);
                    break;
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => animator.LocalRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                    break;
                case AnimationPropertyKeys.RotationKey:
                    AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => { if (animator.TargetType == ObjectAnimator.TargetTypes.Transform) animator.WorldRotation.Add(v); }, PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.OffsetWorldRotationKey:
                    AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => animator.WorldRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                    AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v * BeatmapConstant.LaneSize), PointDataParsers.ParseVector3, p, Vector3.zero);
                    break;
                case AnimationPropertyKeys.OffsetPositionKey:
                    // Convert gameplay offsets from lane units to world units.
                    AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => { if (animator.TargetType == ObjectAnimator.TargetTypes.GameplayObject) animator.OffsetPosition.Add(v * BeatmapConstant.LaneSize); }, PointDataParsers.ParseVector3, p, Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyLocalPositionKey:
                    AddPointDef<Vector3>(
                        source,
                        (ObjectAnimator animator, Vector3 v) =>
                        {
                            if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                                animator.LocalPosition.Add(v * BeatmapConstant.LaneSize);
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
                case AnimationPropertyKeys.PositionKey:
                    AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) =>
                    {
                        if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                        {
                            animator.WorldPosition.Add(v);
                        }
                    }, PointDataParsers.ParseVector3, p, Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
                    break;
                case AnimationPropertyKeys.LegacyColorKey:
                case AnimationPropertyKeys.ColorKey:
                    AddPointDef<Color>(source, (ObjectAnimator animator, Color v) => { if (animator.TargetType != ObjectAnimator.TargetTypes.Transform) animator.Colors.Add(v); }, PointDataParsers.ParseColor, p, Color.white);
                    break;
                case AnimationPropertyKeys.LegacyTimeKey:
                case AnimationPropertyKeys.TimeKey:
                    AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.SetLifeTime(f), PointDataParsers.ParseFloat, p, -1);
                    break;
                case AnimationPropertyKeys.LegacyInteractableKey:
                case AnimationPropertyKeys.InteractableKey:
                    AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.Interactable.Add(f), PointDataParsers.ParseFloat, p, 1);
                    break;
            }
        }

        private void AddPointDef<T>(BaseCustomEvent source, Action<ObjectAnimator, T> _setter, PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, T _default) where T : struct
        {
            if (AnimateProperty<T>.SkipsMissingPointDefinition(p, source))
                return;

            Action<T> setter = (v) => { for (var i = 0; i < CachedChildren.Count; ++i) { _setter(CachedChildren[i], v); } };

            var animateProperty = GetAnimateProperty<T>(p.Key, setter, _default);
            animateProperty.AddPointDef(parser, p, source);
            childPushers[p.Key] = child =>
            {
                var time = Atsc.CurrentJsonTime;
                if (time >= animateProperty.StartTime)
                {
                    _setter(child, animateProperty.GetLerpedValue(time));
                }
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
