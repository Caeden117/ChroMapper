using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class FireGroupEffect : LightColorGroupEffect
{
    public LightColorGroupEffectManager EffectManager;
    [SerializeField] private List<FireGroupEffectEntry> fireEntries = new();
    private FireGroupEffect playbackOwner;
    private FireGroupEffect[] fireGroups;
    private Playback playback;
    private bool eventsDirty;
    private bool needsSeek;
    private bool hasSample;
    private float sampleTime;

    public void Register(int elementId, FireEffect fire) =>
        fireEntries.Add(new FireGroupEffectEntry { ElementId = elementId, Effect = fire });

    protected override IEnumerable<int> GetElementIds() =>
        base.GetElementIds().Concat(fireEntries.Select(entry => entry.ElementId));

    public override void Initialize()
    {
        base.Initialize();
        if (playbackOwner == null)
        {
            // Fire groups share one manager and sample together to preserve shared-light write order.
            var groups = EffectManager
                .IdToEffect.Values
                .OfType<FireGroupEffect>()
                .ToArray();
            var owner = groups[0];
            owner.fireGroups = groups;
            foreach (var group in groups) group.playbackOwner = owner;
        }

        playbackOwner.playback = null;
        playbackOwner.hasSample = false;
        eventsDirty = true;
    }

    public override void InsertData(BaseLightColorEventBoxGroup data)
    {
        base.InsertData(data);
        eventsDirty = true;
    }

    public override void RemoveData(BaseLightColorEventBoxGroup reference, BaseLightColorEventBoxGroup original)
    {
        base.RemoveData(reference, original);
        eventsDirty = true;
    }

    protected override void HandleBoostChange(bool boost)
    {
        // no op
    }

    public override void Refresh()
    {
        playbackOwner.playback?.InvalidateOutputs(this);
        playbackOwner.needsSeek = true;
        if (!Atsc.IsPlaying)
        {
            playbackOwner.UpdateTime(
                false,
                playbackOwner.hasSample ? playbackOwner.sampleTime : Atsc.CurrentSongBpmTime);
        }
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        // LightshowController visits every group; the first fire group samples their shared state.
        if (playbackOwner != this) return;
        sampleTime = time;
        hasSample = true;
        var seconds = Atsc.GetSecondsFromBeat(time);
        if (playback == null)
        {
            playback = new Playback(Atsc, fireGroups);
            needsSeek = true;
        }

        if (needsSeek)
        {
            playback.Seek(seconds);
            needsSeek = false;
            if (!isPlaying) return;
        }

        if (isPlaying)
            playback.Advance(seconds);
        else
            playback.Seek(seconds);
    }

    private sealed class Playback
    {
        private readonly FireElement[] elements;
        private readonly FireTarget[] targets;
        private readonly FireGroupEffect[] groups;
        private readonly List<FireEvent> events = new();
        private readonly PointLightState[] pointLights;
        private readonly List<FireTarget> activeBursts = new();
        private int nextEvent;
        private float previousTime;
        private bool previewSample;

        public Playback(AudioTimeSyncController clock, FireGroupEffect[] groups)
        {
            this.groups = groups;
            elements = groups
                .SelectMany(group => group.activeContainers.Select(container =>
                    new FireElement(
                        group,
                        container,
                        clock,
                        group
                            .fireEntries.Where(entry => entry.ElementId == container.ElementId)
                            .Select(entry => entry.Effect))))
                .Where(element => element.Targets.Length > 0)
                .OrderBy(element => element.Group.ID)
                .ThenBy(element => element.Container.ElementId)
                .ToArray();
            targets = elements.SelectMany(element => element.Targets).ToArray();
            var channels =
                new Dictionary<(MaterialPropertyBlockController controller, bool emission), PointLightState>();
            foreach (var target in targets)
            {
                var fire = target.Effect;
                var controller = fire.UseEmissionColor
                    ? fire.EmissionTextureColorPropertyBlockController
                    : fire.PrivatePointLightPropertyBlockController;
                var key = (controller, fire.UseEmissionColor);
                if (!channels.TryGetValue(key, out var channel)) channels.Add(key, channel = new PointLightState());
                target.PointLight = channel;
            }

            pointLights = channels.Values.ToArray();

            foreach (var group in groups) group.eventsDirty = true;
            previousTime = float.NegativeInfinity;
        }

        public void InvalidateOutputs(FireGroupEffect group)
        {
            foreach (var element in elements)
            {
                if (element.Group != group) continue;
                element.InvalidateContribution();
                foreach (var target in element.Targets) target.InvalidateOutput();
            }
        }

        private bool UpdateEvents()
        {
            var changed = false;
            foreach (var group in groups) changed |= group.eventsDirty;
            if (!changed) return false;

            // Keep bindings and event storage, as the other lighting evaluators do.
            // Only an edited group's expanded event containers need to be read again.
            events.Clear();
            foreach (var element in elements)
            {
                if (element.Group.eventsDirty) element.RebuildEvents();
                events.AddRange(element.Events);
            }

            events.Sort(FireEvent.Compare);
            foreach (var group in groups) group.eventsDirty = false;
            return true;
        }

        private void ResetState()
        {
            nextEvent = 0;
            activeBursts.Clear();
            foreach (var element in elements) element.ResetContribution();
            foreach (var target in targets) target.Reset();
        }

        public void Seek(float songTime)
        {
            var eventsChanged = UpdateEvents();
            var configurationChanged = false;
            foreach (var target in targets) configurationChanged |= target.UpdatePreviewConfiguration();
            var reconstruct = eventsChanged || !previewSample || songTime < previousTime || configurationChanged;
            if (!reconstruct && songTime == previousTime)
            {
                Apply();
                return;
            }

            foreach (var element in elements) element.Reconstructing = true;
            if (reconstruct) ResetState();
            var sampledTime = reconstruct ? float.NegativeInfinity : previousTime;
            while (nextEvent < events.Count && events[nextEvent].Time <= songTime)
            {
                var eventTime = events[nextEvent].Time;
                CompletePreviewBurstsBefore(eventTime);
                // A paused preview reconstructs intervening playback. Same-time callbacks
                // still precede the first advancing tween tick, as they do in the game.
                foreach (var target in activeBursts)
                {
                    if (eventTime > target.TweenStartTime) target.TweenStarted = true;
                }

                UpdateContinuous(eventTime);
                DispatchCallbacks(eventTime);
                UpdateBurstTweens(eventTime, true);
                sampledTime = eventTime;
            }

            CompletePreviewBurstsBefore(songTime);
            if (sampledTime != songTime)
            {
                UpdateContinuous(songTime);
                UpdateBurstTweens(songTime, true);
            }

            previousTime = songTime;
            previewSample = true;
            Apply();
            foreach (var element in elements) element.Reconstructing = false;
        }

        private void CompletePreviewBurstsBefore(float songTime)
        {
            while (activeBursts.Count > 0)
            {
                var completionTime = float.PositiveInfinity;
                foreach (var target in activeBursts) completionTime = Mathf.Min(completionTime, target.TweenEndTime);
                if (!(completionTime < songTime)) return;
                UpdateContinuous(completionTime);
                UpdateBurstTweens(completionTime, true);
            }
        }

        public void Advance(float songTime)
        {
            if (songTime < previousTime)
            {
                Seek(songTime);
                return;
            }

            UpdateContinuous(songTime);
            // Native script orders are callbacks -10 and song-time tweens 0 in LateUpdate.
            DispatchCallbacks(songTime);
            UpdateBurstTweens(songTime);
            previousTime = songTime;
            previewSample = false;
            Apply();
        }

        private void UpdateContinuous(float songTime)
        {
            foreach (var target in targets)
            {
                if (target.Effect is ContinuousFireEffect && target.ContinuousEnabled)
                    target.SampleContinuous(songTime);
            }
        }

        private void DispatchCallbacks(float songTime)
        {
            while (nextEvent < events.Count && events[nextEvent].Time <= songTime)
            {
                var evt = events[nextEvent++];
                foreach (var target in evt.Element.Targets)
                {
                    if (target.Effect is ContinuousFireEffect)
                    {
                        if (evt.Brightness <= 0f)
                        {
                            target.ContinuousEnabled = false;
                            target.Visible = false;
                            target.Sample(0f, 0f);
                        }
                        else if (!target.ContinuousEnabled)
                        {
                            target.ContinuousEnabled = target.Visible = true;
                            target.ShaderStartTime = evt.Time;
                            target.ContinuousEndTime = evt.NextTime;
                            target.SampleContinuous(songTime);
                        }
                    }
                    else if (target.Effect is BurstFireEffect burst && evt.Brightness > 0f && !target.TweenActive)
                    {
                        target.Visible = true;
                        target.ShaderStartTime = evt.Time;
                        target.TweenStartTime = songTime;
                        // Mono can retain extra precision in a recomputed sum. Use one stored
                        // float deadline for both preview scheduling and completion.
                        target.TweenEndTime = songTime + Mathf.Max(burst.FadeOutDuration, 0f);
                        target.TweenProgress = 0f;
                        target.TweenStarted = false;
                        target.SampleBurst(0f);
                        if (!target.InTweenManager)
                        {
                            target.InTweenManager = true;
                            activeBursts.Add(target);
                        }
                    }
                }
            }
        }

        private void UpdateBurstTweens(float songTime) => UpdateBurstTweens(songTime, false);

        private void UpdateBurstTweens(float songTime, bool reconstructing)
        {
            for (var i = activeBursts.Count - 1; i >= 0; i--)
            {
                var target = activeBursts[i];
                var duration = ((BurstFireEffect)target.Effect).FadeOutDuration;
                var elapsed = Mathf.Min(songTime - target.TweenStartTime, duration + .001f);
                if (elapsed > 0f) target.TweenStarted = true;
                target.TweenProgress = duration > 0f ? Mathf.Max(0f, elapsed) / duration : 1f;
                // Seek reconstruction has exact analytical completion times, rather than
                // the first rendered frame whose floating-point tween progress reaches one.
                if (reconstructing && songTime >= target.TweenEndTime) target.TweenProgress = 1f;
                target.SampleBurst(target.TweenProgress);
                if (target.TweenProgress < 1f) continue;

                target.SampleBurst(1f);
                target.Visible = false;
                target.NotifyContribution(0f);
                target.InTweenManager = false;
                activeBursts[i] = activeBursts[^1];
                activeBursts.RemoveAt(activeBursts.Count - 1);
            }
        }

        private void Apply()
        {
            foreach (var target in targets) target.Apply();
            foreach (var element in elements) element.ApplyContribution();
            foreach (var pointLight in pointLights) pointLight.Apply();
        }
    }

    private sealed class FireElement
    {
        public readonly FireGroupEffect Group;
        public readonly LightColorGroupContainer Container;
        public readonly FireTarget[] Targets;
        public readonly List<FireEvent> Events = new();
        private readonly AudioTimeSyncController clock;
        private bool hasContribution;
        private FireEffect contributionWriter;
        private float contributionAlpha;
        private bool hasAppliedContribution;
        private Color appliedContribution;
        private bool contributionAttempted;
        public bool Reconstructing;

        public FireElement(
            FireGroupEffect group,
            LightColorGroupContainer container,
            AudioTimeSyncController atsc,
            IEnumerable<FireEffect> effects)
        {
            Group = group;
            Container = container;
            clock = atsc;
            Targets = effects.Select(effect => new FireTarget(effect, this)).ToArray();
        }

        public void RebuildEvents()
        {
            Events.Clear();
            var hasPrevious = false;
            var previousTime = 0f;
            var brightness = 0f;
            foreach (var state in Container.EventContainer.Collection)
            {
                if (state.StartTime == short.MinValue || state.StartTime == float.MaxValue) continue;
                var time = clock.GetSecondsFromBeat(state.StartTime);
                if (hasPrevious) Events.Add(new FireEvent(this, previousTime, time, brightness, Events.Count));
                if (!state.UsePrevious || !hasPrevious) brightness = state.Brightness;
                previousTime = time;
                hasPrevious = true;
            }

            if (hasPrevious)
            {
                Events.Add(
                    new FireEvent(this, previousTime, clock.SongAudioSource.clip.length, brightness, Events.Count));
            }
        }

        public void InvalidateContribution() => hasAppliedContribution = false;

        public void ResetContribution()
        {
            hasContribution = false;
            contributionAttempted = false;
        }

        public void SetContribution(FireEffect effect, float alpha)
        {
            contributionWriter = effect;
            contributionAlpha = alpha;
            hasContribution = true;
            if (Reconstructing) return;
            ApplyContributionNow();
            contributionAttempted = true;
        }

        public void ApplyContribution()
        {
            if (!contributionAttempted) ApplyContributionNow();
            contributionAttempted = false;
        }

        private void ApplyContributionNow()
        {
            if (!hasContribution) return;
            var contribution = contributionWriter.CustomLightColor.WithAlpha(contributionAlpha);
            if (hasAppliedContribution && appliedContribution.Equals(contribution)) return;
            var allInitialized = true;
            foreach (var light in Container.Lights)
            {
                light.SetColor(contribution);
                allInitialized &= light.HasInitialized;
            }

            appliedContribution = contribution;
            // A contribution before LightController.Start must be retried after initialization.
            hasAppliedContribution = allInitialized;
        }
    }

    private readonly struct FireEvent
    {
        public readonly FireElement Element;
        public readonly float Time;
        public readonly float NextTime;
        public readonly float Brightness;
        private readonly int sequence;

        public FireEvent(FireElement element, float time, float nextTime, float brightness, int sequence)
        {
            Element = element;
            Time = time;
            NextTime = nextTime;
            Brightness = brightness;
            this.sequence = sequence;
        }

        public static int Compare(FireEvent left, FireEvent right)
        {
            var order = left.Time.CompareTo(right.Time);
            if (order != 0) return order;
            order = left.Element.Group.ID.CompareTo(right.Element.Group.ID);
            if (order != 0) return order;
            order = left.Element.Container.ElementId.CompareTo(right.Element.Container.ElementId);
            // Preserve authored order within an equal-time element, even though List.Sort is unstable.
            return order != 0 ? order : left.sequence.CompareTo(right.sequence);
        }
    }

    private sealed class PointLightState
    {
        private FireEffect writer;
        private float alpha;
        private bool hasApplied;
        private Color appliedColor;

        public void Invalidate() => hasApplied = false;

        public void Set(FireEffect effect, float bloomAlpha)
        {
            writer = effect;
            alpha = bloomAlpha;
        }

        public void Apply()
        {
            if (writer == null) return;
            var color = writer.PointLightColor * alpha;
            if (hasApplied && appliedColor.Equals(color)) return;
            writer.ApplyPointLightColor(color);
            appliedColor = color;
            hasApplied = true;
        }
    }

    private sealed class FireTarget
    {
        public readonly FireEffect Effect;
        public readonly FireElement Element;
        public PointLightState PointLight;
        public bool ContinuousEnabled;
        public bool Visible;
        public float ShaderStartTime;
        public float ContinuousEndTime;
        public float TweenStartTime;
        public float TweenEndTime;
        public float TweenProgress;
        public bool TweenStarted;
        public bool InTweenManager;
        public bool TweenActive => InTweenManager && TweenStarted && TweenProgress < 1f;
        private float flipbookAlpha;
        private float bloomAlpha;
        private float contributionAlpha;
        private bool hasApplied;
        private (float start, bool visible, float flipbook, float bloom, Color contribution) applied;
        private (float fadeIn, float fadeOut, float sustain) previewDurations;
        private readonly CurveSnapshot previewFlipbookCurve = new();
        private readonly CurveSnapshot previewBloomCurve = new();

        public FireTarget(FireEffect effect, FireElement element)
        {
            Effect = effect;
            Element = element;
            Effect.EnsureInitialized();
        }

        public bool UpdatePreviewConfiguration()
        {
            (float fadeIn, float fadeOut, float sustain) durations;
            AnimationCurve flipbookCurve, bloomCurve;
            if (Effect is ContinuousFireEffect continuous)
            {
                durations = (continuous.FadeInDuration, continuous.FadeOutDuration, continuous.SustainDuration);
                flipbookCurve = continuous.FlipbookSustainCurve;
                bloomCurve = continuous.BloomSustainCurve;
            }
            else
            {
                var burst = (BurstFireEffect)Effect;
                durations = (0f, burst.FadeOutDuration, 0f);
                flipbookCurve = burst.FlipbookFadeOutCurve;
                bloomCurve = burst.BloomFadeOutCurve;
            }

            var curvesChanged = previewFlipbookCurve.Update(flipbookCurve) | previewBloomCurve.Update(bloomCurve);
            var changed = !previewDurations.Equals(durations) || curvesChanged;
            previewDurations = durations;
            return changed;
        }

        public void InvalidateOutput()
        {
            hasApplied = false;
            PointLight.Invalidate();
        }

        public void Reset()
        {
            ContinuousEnabled = Visible = TweenStarted = InTweenManager = false;
            ShaderStartTime = -1000f;
            ContinuousEndTime = TweenStartTime = TweenEndTime = TweenProgress = 0f;
            (flipbookAlpha, bloomAlpha) = Effect.InitialAlphas;
            PointLight.Set(Effect, bloomAlpha);
            NotifyContribution(0f);
        }

        public void Sample(float flipbook, float bloom)
        {
            flipbookAlpha = flipbook;
            bloomAlpha = bloom;
            NotifyContribution(bloom);
            PointLight.Set(Effect, bloom);
        }

        public void SampleContinuous(float songTime)
        {
            var (flipbook, bloom) = ((ContinuousFireEffect)Effect)
                .EvaluateEffect(songTime, ShaderStartTime, ContinuousEndTime);
            Sample(flipbook, bloom);
        }

        public void SampleBurst(float progress)
        {
            var burst = (BurstFireEffect)Effect;
            Sample(burst.FlipbookFadeOutCurve.Evaluate(progress), burst.BloomFadeOutCurve.Evaluate(progress));
        }

        public void NotifyContribution(float alpha)
        {
            contributionAlpha = alpha;
            if (Effect.ContributeCustomLightColor) Element.SetContribution(Effect, alpha);
        }

        public void Apply()
        {
            var state = (ShaderStartTime, Visible, flipbookAlpha, bloomAlpha * Effect.BloomIntensityMultiplier,
                Effect.CustomLightColor.WithAlpha(contributionAlpha));
            if (hasApplied && applied.Equals(state)) return;
            Effect.Apply(ShaderStartTime, Visible, flipbookAlpha, bloomAlpha, contributionAlpha);
            applied = state;
            hasApplied = true;
        }
    }

    private sealed class CurveSnapshot
    {
        private Keyframe[] keys;
        private Keyframe[] sampledKeys;
        private WrapMode preWrapMode;
        private WrapMode postWrapMode;

        public bool Update(AnimationCurve curve)
        {
            var length = curve.length;
            var preWrap = curve.preWrapMode;
            var postWrap = curve.postWrapMode;
            if (keys == null || keys.Length != length)
            {
                keys = new Keyframe[length];
                sampledKeys = new Keyframe[length];
                curve.GetKeys(keys);
                preWrapMode = preWrap;
                postWrapMode = postWrap;
                return true;
            }

            // AnimationCurve.Equals allocates when comparing distinct curves in this Unity version.
            // Read into reusable storage to detect in-place edits without allocating per paused sample.
            curve.GetKeys(sampledKeys);
            var unchanged = preWrapMode == preWrap && postWrapMode == postWrap;
            for (var i = 0; i < keys.Length && unchanged; i++)
            {
                var key = sampledKeys[i];
                var previous = keys[i];
                unchanged = key.time.Equals(previous.time)
                    && key.value.Equals(previous.value)
                    && key.inTangent.Equals(previous.inTangent)
                    && key.outTangent.Equals(previous.outTangent)
                    && key.inWeight.Equals(previous.inWeight)
                    && key.outWeight.Equals(previous.outWeight)
                    && key.weightedMode == previous.weightedMode;
            }

            if (unchanged) return false;

            (keys, sampledKeys) = (sampledKeys, keys);
            preWrapMode = preWrap;
            postWrapMode = postWrap;
            return true;
        }
    }
}

[Serializable]
public struct FireGroupEffectEntry
{
    public int ElementId;
    public FireEffect Effect;
}
