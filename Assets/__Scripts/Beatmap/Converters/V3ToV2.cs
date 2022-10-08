using Beatmap.V2;
using Beatmap.V3;

namespace Beatmap.Converters
{
    public static class V3ToV2
    {
        public static V2Note BombNote(V3BombNote other) => new V2Note(other);

        public static V2Event BpmEvent(V3BpmEvent other) => new V2Event(other);

        public static V2Event ColorBoostEvent(V3ColorBoostEvent other) => new V2Event(other);

        public static V2Note ColorNote(V3ColorNote other) => new V2Note(other);

        public static V2SpecialEventsKeywordFiltersKeywords EventTypesForKeywords(V3BasicEventTypesForKeywords other) => new V2SpecialEventsKeywordFiltersKeywords(other);

        public static V2SpecialEventsKeywordFilters EventTypesWithKeywords(V3BasicEventTypesWithKeywords other) => new V2SpecialEventsKeywordFilters(other);

        public static V2Obstacle Obstacle(V3Obstacle other) => new V2Obstacle(other);

        public static V2Event RotationEvent(V3RotationEvent other) => new V2Event(other);

        public static V2Arc Slider(V3Arc other) => new V2Arc(other);

        public static V2Waypoint Waypoint(V3Waypoint other) => new V2Waypoint(other);

        public static V2Difficulty Difficulty(V3Difficulty other) =>
            new V2Difficulty()
            {
                DirectoryAndFile = other.DirectoryAndFile,
                Time = other.Time,
                Events = other.Events,
                Notes = other.Notes,
                Obstacles = other.Obstacles,

                Waypoints = other.Waypoints,
                BpmChanges = other.BpmChanges,
                Bookmarks = other.Bookmarks,
                CustomEvents = other.CustomEvents,
                EnvironmentEnhancements = other.EnvironmentEnhancements,
                CustomData = other.CustomData
            };
    }
}
