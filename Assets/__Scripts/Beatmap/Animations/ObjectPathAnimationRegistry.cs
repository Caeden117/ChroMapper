using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    // Share definition order between objects with the same ordered track list. Only the source
    // references are shared: parsing points, object lifetime, setters, and evaluation state stay local.
    internal sealed class ObjectPathAnimationRegistry
    {
        private readonly Dictionary<string, TrackEvents> eventsByTrack = new();
        private readonly Dictionary<string, TrackPreparation> preparations = new();
        private int mapVersion;
        private int timingVersion;

        internal sealed class TrackEvents
        {
            internal readonly List<BaseCustomEvent> Events = new();
            internal int Version;
        }

        internal sealed class Definition
        {
            internal string PropertyName;
            internal IPointDefinition.UntypedParams Parameters;
            internal BaseCustomEvent Source;
            internal int OriginalIndex;
        }

        internal sealed class TrackPreparation
        {
            internal readonly string[] Names;
            private readonly TrackEvents[] sources;
            private readonly int[] versions;
            private int preparedTimingVersion = -1;
            internal Definition[] Definitions = Array.Empty<Definition>();
            internal bool HasDefinitePosition;

            internal TrackPreparation(string[] names, TrackEvents[] sources)
            {
                Names = names;
                this.sources = sources;
                versions = new int[sources.Length];
            }

            internal void Refresh(BaseDifficulty map, int currentTimingVersion)
            {
                var changed = preparedTimingVersion != currentTimingVersion;
                for (var i = 0; i < sources.Length; i++) changed |= versions[i] != sources[i].Version;
                if (!changed) return;

                var definitions = new List<Definition>();
                var properties = new Dictionary<string, List<Definition>>();
                HasDefinitePosition = false;
                for (var i = 0; i < sources.Length; i++)
                {
                    foreach (var ev in sources[i].Events)
                    {
                        foreach (var pair in ev.Data)
                        {
                            if (pair.Value == null || pair.Value.IsNull) continue;
                            HasDefinitePosition |= IsDefinitePosition(pair.Key);
                            var transition = ev.DataDuration ?? 0f;
                            var definition = new Definition
                            {
                                PropertyName = pair.Key,
                                Source = ev,
                                OriginalIndex = definitions.Count,
                                Parameters = new IPointDefinition.UntypedParams
                                {
                                    Key = $"track_{pair.Key}",
                                    Points = pair.Value,
                                    Easing = ev.DataEasing,
                                    Time = ev.SongBpmTime,
                                    Transition = transition == 0f ? 0f
                                        : (float)map.JsonTimeToSongBpmTime(ev.JsonTime + transition) - ev.SongBpmTime,
                                    PreparedIndex = -1
                                }
                            };
                            definitions.Add(definition);
                            if (!properties.TryGetValue(pair.Key, out var property))
                            {
                                property = new List<Definition>();
                                properties.Add(pair.Key, property);
                            }

                            // Binding still logs missing references. Exclude them from repeat boundaries
                            // here because the normal parser never creates definitions for those sources.
                            if (pair.Value is not JSONString named || map.PointDefinitions.ContainsKey(named.Value))
                                property.Add(definition);
                        }
                    }
                    versions[i] = sources[i].Version;
                }

                foreach (var property in properties.Values)
                {
                    property.Sort(static (a, b) =>
                    {
                        var byTime = a.Parameters.Time.CompareTo(b.Parameters.Time);
                        return byTime != 0 ? byTime : a.OriginalIndex.CompareTo(b.OriginalIndex);
                    });
                    var evaluated = AnimationDefinitionOrdering.FilterRepeatedSources(property,
                        static definition => definition.Parameters.Time, static definition => definition.Source);
                    for (var i = 0; i < evaluated.Length; i++)
                    {
                        var parameters = evaluated[i].Parameters;
                        parameters.PreparedIndex = i;
                        parameters.PreparedLength = evaluated.Length;
                        evaluated[i].Parameters = parameters;
                    }
                }

                Definitions = definitions.ToArray();
                preparedTimingVersion = currentTimingVersion;
            }
        }

        // Map reset releases event references and invalidates preparations retained by beatmap objects.
        internal void Reset()
        {
            eventsByTrack.Clear();
            preparations.Clear();
            mapVersion++;
            timingVersion++;
        }

        internal void Add(string track, BaseCustomEvent ev)
        {
            var source = GetTrackEvents(track);
            source.Events.Add(ev);
            source.Version++;
        }

        internal void Remove(string track, BaseCustomEvent ev)
        {
            var source = eventsByTrack[track];
            source.Events.Remove(ev);
            source.Version++;
        }

        internal void RefreshBpmTiming() => timingVersion++;

        internal ObjectPathAnimationPreparation Prepare(BaseGrid obj, BaseDifficulty map)
        {
            var preparation = obj.PreparedPathAnimation;
            if (preparation == null || !preparation.Matches(this, mapVersion, obj))
            {
                var key = obj.CustomTrack?.ToString() ?? string.Empty;
                if (!preparations.TryGetValue(key, out var tracks))
                {
                    var names = new List<string>();
                    if (obj.CustomTrack is JSONString name) names.Add(name.Value);
                    else if (obj.CustomTrack is JSONArray array)
                        foreach (var child in array.Children) names.Add(child.Value);

                    var sources = new TrackEvents[names.Count];
                    for (var i = 0; i < names.Count; i++) sources[i] = GetTrackEvents(names[i]);
                    tracks = new TrackPreparation(names.ToArray(), sources);
                    preparations.Add(key, tracks);
                }

                preparation = new ObjectPathAnimationPreparation(this, mapVersion, obj, tracks);
                obj.PreparedPathAnimation = preparation;
            }

            preparation.TrackDefinitions.Refresh(map, timingVersion);
            return preparation;
        }

        private TrackEvents GetTrackEvents(string name)
        {
            if (!eventsByTrack.TryGetValue(name, out var source))
            {
                source = new TrackEvents();
                eventsByTrack.Add(name, source);
            }

            return source;
        }

        internal static bool IsDefinitePosition(string key) =>
            key is AnimationPropertyKeys.LegacyDefinitePositionKey or AnimationPropertyKeys.DefinitePositionKey;
    }

    internal sealed class ObjectPathAnimationPreparation
    {
        private readonly ObjectPathAnimationRegistry registry;
        private readonly int mapVersion;
        private readonly JSONNode customTrack;
        private readonly JSONNode customAnimation;
        internal readonly ObjectPathAnimationRegistry.TrackPreparation TrackDefinitions;
        internal readonly ObjectPathAnimationRegistry.Definition[] ObjectDefinitions;
        private readonly bool objectHasDefinitePosition;
        internal bool HasDefinitePosition => TrackDefinitions.HasDefinitePosition || objectHasDefinitePosition;

        internal ObjectPathAnimationPreparation(ObjectPathAnimationRegistry registry, int mapVersion,
            BaseGrid obj, ObjectPathAnimationRegistry.TrackPreparation tracks)
        {
            this.registry = registry;
            this.mapVersion = mapVersion;
            customTrack = obj.CustomTrack;
            customAnimation = obj.CustomAnimation;
            TrackDefinitions = tracks;
            var definitions = new List<ObjectPathAnimationRegistry.Definition>();
            if (customAnimation != null)
            {
                foreach (var pair in customAnimation.AsObject)
                {
                    objectHasDefinitePosition |= ObjectPathAnimationRegistry.IsDefinitePosition(pair.Key);
                    definitions.Add(new ObjectPathAnimationRegistry.Definition
                    {
                        PropertyName = pair.Key,
                        Parameters = new IPointDefinition.UntypedParams
                        {
                            Key = pair.Key,
                            Overwrite = true,
                            Points = pair.Value,
                            LogSource = obj,
                            PreparedLength = 1
                        }
                    });
                }
            }

            ObjectDefinitions = definitions.ToArray();
        }

        internal bool Matches(ObjectPathAnimationRegistry owner, int version, BaseGrid obj) =>
            ReferenceEquals(registry, owner) && mapVersion == version
            && ReferenceEquals(customTrack, obj.CustomTrack) && ReferenceEquals(customAnimation, obj.CustomAnimation);
    }
}
