using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Helper;
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
            internal Definition[] Definitions = Array.Empty<Definition>();
            internal bool HasDefinitePosition;
            private int preparedVersion = -1;
            private int preparedTimingVersion = -1;

            internal void Refresh(BaseDifficulty map, int currentTimingVersion)
            {
                if (preparedVersion == Version && preparedTimingVersion == currentTimingVersion) return;

                var definitions = new List<Definition>();
                HasDefinitePosition = false;
                foreach (var ev in Events)
                {
                    foreach (var pair in ev.Data)
                    {
                        if ((pair.Value == null || pair.Value.IsNull) && !IsColor(pair.Key)) continue;
                        HasDefinitePosition |= IsDefinitePosition(pair.Key);
                        var transition = ev.DataDuration ?? 0f;
                        definitions.Add(new Definition
                        {
                            PropertyName = pair.Key,
                            Source = ev,
                            Parameters = new IPointDefinition.UntypedParams
                            {
                                Key = $"track_{pair.Key}",
                                Points = pair.Value,
                                PreparedIndex = -1,
                                Easing = ev.DataEasing,
                                Time = ev.SongBpmTime,
                                Transition = transition == 0f ? 0f
                                    : (float)map.JsonTimeToSongBpmTime(ev.JsonTime + transition) - ev.SongBpmTime
                            }
                        });
                    }
                }

                Definitions = definitions.ToArray();
                preparedVersion = Version;
                preparedTimingVersion = currentTimingVersion;
            }
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

                var count = 0;
                HasDefinitePosition = false;
                for (var i = 0; i < sources.Length; i++)
                {
                    sources[i].Refresh(map, currentTimingVersion);
                    count += sources[i].Definitions.Length;
                    HasDefinitePosition |= sources[i].HasDefinitePosition;
                    versions[i] = sources[i].Version;
                }

                Definitions = new Definition[count];
                var offset = 0;
                var properties = new Dictionary<string, List<Definition>>();
                for (var i = 0; i < sources.Length; i++)
                {
                    foreach (var definition in sources[i].Definitions)
                    {
                        var parameters = definition.Parameters;
                        if (IsColor(definition.PropertyName)) parameters.Key += "_" + i;
                        var prepared = new Definition
                        {
                            PropertyName = definition.PropertyName,
                            Source = definition.Source,
                            Parameters = parameters,
                            OriginalIndex = offset
                        };
                        Definitions[offset++] = prepared;
                        if (parameters.Points == null || parameters.Points.IsNull
                            || parameters.Points is JSONString named && !map.PointDefinitions.ContainsKey(named.Value))
                            continue;
                        if (!properties.TryGetValue(parameters.Key, out var property))
                            properties.Add(parameters.Key, property = new List<Definition>());
                        property.Add(prepared);
                    }
                }
                foreach (var property in properties.Values)
                {
                    property.Sort(static (a, b) =>
                    {
                        var order = a.Parameters.Time.CompareTo(b.Parameters.Time);
                        return order != 0 ? order : a.OriginalIndex.CompareTo(b.OriginalIndex);
                    });
                    for (var i = 0; i < property.Count; i++)
                    {
                        var parameters = property[i].Parameters;
                        parameters.PreparedIndex = i;
                        parameters.PreparedLength = property.Count;
                        property[i].Parameters = parameters;
                    }
                }
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

        // Keep the source object so preparations for unrelated tracks remain valid.
        internal void Replace(string track, IEnumerable<BaseCustomEvent> events)
        {
            var source = GetTrackEvents(track);
            source.Events.Clear();
            source.Events.AddRange(events);
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
                var tracks = PrepareTracks(obj.CustomTrack, map);

                preparation = new ObjectPathAnimationPreparation(this, mapVersion, obj, tracks);
                obj.PreparedPathAnimation = preparation;
            }

            preparation.TrackDefinitions.Refresh(map, timingVersion);
            return preparation;
        }

        internal TrackPreparation PrepareTracks(JSONNode trackData, BaseDifficulty map)
        {
            var key = trackData?.ToString() ?? string.Empty;
            if (!preparations.TryGetValue(key, out var tracks))
            {
                var names = TrackDataReader.ReadNames(trackData);
                var sources = new TrackEvents[names.Length];
                for (var i = 0; i < names.Length; i++) sources[i] = GetTrackEvents(names[i]);
                tracks = new TrackPreparation(names, sources);
                preparations.Add(key, tracks);
            }
            tracks.Refresh(map, timingVersion);
            return tracks;
        }

        internal static bool IsColor(string key) =>
            key is AnimationPropertyKeys.LegacyColorKey or AnimationPropertyKeys.ColorKey;

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
                            PreparedIndex = 0,
                            PreparedLength = 1,
                            Points = pair.Value,
                            LogSource = obj
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
