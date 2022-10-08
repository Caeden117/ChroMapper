using Beatmap.V2;
using Beatmap.V3;

namespace Beatmap.Converters
{
    public static class V2ToV3
    {
        public static V3BombNote BombNote(V2Note other) => new V3BombNote(other);

        public static V3BpmEvent BpmEvent(V2Event other) => new V3BpmEvent(other);

        public static V3ColorBoostEvent ColorBoostEvent(V2Event other) => new V3ColorBoostEvent(other);

        public static V3ColorNote ColorNote(V2Note other) => new V3ColorNote(other);

        public static V3BasicEventTypesWithKeywords EventTypesForKeywords(V2SpecialEventsKeywordFilters other) => new V3BasicEventTypesWithKeywords(other);

        public static V3BasicEventTypesForKeywords EventTypesWithKeywords(V2SpecialEventsKeywordFiltersKeywords other) => new V3BasicEventTypesForKeywords(other);

        public static V3Obstacle Obstacle(V2Obstacle other) => new V3Obstacle(other);

        public static V3RotationEvent RotationEvent(V2Event other) => new V3RotationEvent(other);

        public static V3Arc Slider(V2Arc other) => new V3Arc(other);

        public static V3Waypoint Waypoint(V2Waypoint other) => new V3Waypoint(other);
        
        public static V3Difficulty Difficulty(V2Difficulty other) =>
            new V3Difficulty()
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
