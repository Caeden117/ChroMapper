using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Samples custom-event properties at an absolute song beat. Later events own a property
/// until superseded; removing all registrations or resetting restores its original value.
/// </summary>
public sealed class CustomEventPropertyTimeline
{
    private readonly Dictionary<ChannelKey, Channel> channelsByKey = new();
    private readonly List<Channel> channels = new();
    private long registrationOrder;

    /// <summary>
    /// Registers a value sampler without changing the target. A null target represents a global property.
    /// The property key must identify the same native property across all registrations for that target.
    /// </summary>
    /// <param name="owner">Event with canonical sequence and start/end times in song BPM beats.</param>
    /// <param name="target">Object whose lifetime owns the property, or null for a global property.</param>
    /// <param name="property">Native property key shared by registrations addressing the same value.</param>
    /// <param name="read">Reads the original value when first registered; may be null when restore is supplied.</param>
    /// <param name="write">Applies sampled values and restores the original value.</param>
    /// <param name="sample">Evaluates a value at normalized event progress.</param>
    /// <param name="easing">Optional easing applied to normalized event progress.</param>
    /// <param name="restore">Optional ownership cleanup replacing baseline capture and restoration.</param>
    public void Register<T>(CustomEventStateData owner, UnityEngine.Object target, string property,
        Func<T> read, Action<T> write, Func<float, T> sample, Func<float, float> easing = null, Action restore = null)
    {
        var key = new ChannelKey(target, property);
        if (!channelsByKey.TryGetValue(key, out var channel))
        {
            if (restore == null)
            {
                var baseline = read();
                restore = () => write(baseline);
            }
            channel = new Channel(key, restore);
            channelsByKey.Add(key, channel);
            channels.Add(channel);
        }

        var registration = new Registration(owner, registrationOrder++, t => write(sample(t)), easing);
        var index = channel.Registrations.Count;
        while (index > 0 && registration.CompareTo(channel.Registrations[index - 1]) < 0) index--;
        channel.Registrations.Insert(index, registration);
    }

    /// <summary>
    /// Applies the latest eligible registration for every owned property, or its baseline.
    /// Zero-duration events sample their endpoint at the start beat; completed events hold that endpoint.
    /// </summary>
    public void Sample(float beatTime)
    {
        foreach (var channel in channels)
        {
            if (!channel.IsTargetAlive) continue;
            var registrations = channel.Registrations;
            var left = 0;
            var right = registrations.Count - 1;
            while (left <= right)
            {
                var middle = left + ((right - left) / 2);
                if (registrations[middle].Owner.StartTime <= beatTime)
                    left = middle + 1;
                else
                    right = middle - 1;
            }

            if (right < 0)
                channel.Restore();
            else
                registrations[right].Apply(beatTime);
        }
    }

    /// <summary>Removes an event's registrations. Sample again to apply the remaining event at the cursor.</summary>
    public void Remove(CustomEventStateData state)
    {
        for (var i = channels.Count - 1; i >= 0; i--)
        {
            var channel = channels[i];
            channel.Registrations.RemoveAll(registration => ReferenceEquals(registration.Owner, state));
            if (channel.Registrations.Count == 0) RemoveChannel(i);
        }
    }

    /// <summary>Restores and forgets an object's properties before it is pooled or destroyed.</summary>
    public void RemoveTarget(UnityEngine.Object target)
    {
        for (var i = channels.Count - 1; i >= 0; i--)
            if (ReferenceEquals(channels[i].Key.Target, target)) RemoveChannel(i);
    }

    /// <summary>Restores all surviving targets and releases all registrations and captured values.</summary>
    public void Reset()
    {
        for (var i = channels.Count - 1; i >= 0; i--)
            if (channels[i].IsTargetAlive) channels[i].Restore();
        channels.Clear();
        channelsByKey.Clear();
        registrationOrder = 0;
    }

    private void RemoveChannel(int index)
    {
        var channel = channels[index];
        if (channel.IsTargetAlive) channel.Restore();
        channelsByKey.Remove(channel.Key);
        channels.RemoveAt(index);
    }

    private readonly struct ChannelKey : IEquatable<ChannelKey>
    {
        public readonly UnityEngine.Object Target;
        private readonly string property;

        public ChannelKey(UnityEngine.Object target, string property)
        {
            Target = target;
            this.property = property;
        }

        public bool Equals(ChannelKey other) => ReferenceEquals(Target, other.Target) && property == other.property;
        public override bool Equals(object obj) => obj is ChannelKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            ReferenceEquals(Target, null) ? 0 : RuntimeHelpers.GetHashCode(Target), property);
    }

    private sealed class Channel
    {
        public readonly ChannelKey Key;
        public readonly Action Restore;
        public readonly List<Registration> Registrations = new();
        public bool IsTargetAlive => ReferenceEquals(Key.Target, null) || Key.Target != null;

        public Channel(ChannelKey key, Action restore)
        {
            Key = key;
            Restore = restore;
        }
    }

    private sealed class Registration
    {
        public readonly CustomEventStateData Owner;
        private readonly long order;
        private readonly Action<float> apply;
        private readonly Func<float, float> easing;

        public Registration(CustomEventStateData owner, long order, Action<float> apply, Func<float, float> easing)
        {
            Owner = owner;
            this.order = order;
            this.apply = apply;
            this.easing = easing;
        }

        public int CompareTo(Registration other)
        {
            var comparison = Owner.StartTime.CompareTo(other.Owner.StartTime);
            if (comparison != 0) return comparison;
            comparison = Owner.Sequence.CompareTo(other.Owner.Sequence);
            return comparison != 0 ? comparison : order.CompareTo(other.order);
        }

        public void Apply(float beatTime)
        {
            var progress = Owner.NormalizeTime != null
                ? Owner.NormalizeTime(beatTime)
                : Owner.EndTimeAnimation <= Owner.StartTime
                    ? 1f
                    : Mathf.Clamp01((beatTime - Owner.StartTime) / (Owner.EndTimeAnimation - Owner.StartTime));
            apply(easing == null ? progress : easing(progress));
        }
    }
}
