using System;
using System.Globalization;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap
{
    internal static class BeatmapLogContext
    {
        internal static string Describe(BaseObject source)
        {
            if (source == null)
                return "No beatmap object was supplied.";

            if (source is BaseEnvironmentEnhancement enhancement)
            {
                var song = BeatSaberSongContainer.Instance;
                var index = song != null && song.Map != null
                    ? song.Map.EnvironmentEnhancements.IndexOf(enhancement)
                    : -1;
                var location = index >= 0
                    ? $"environment[{index}]"
                    : "environment enhancement";
                return $"{location}: ID [{enhancement.ID}], lookup [{enhancement.LookupMethod}], "
                    + $"track [{enhancement.Track}], geometry [{enhancement.Geometry}].";
            }

            var beat = source.JsonTime.ToString("R", CultureInfo.InvariantCulture);
            var track = source.CustomTrack;
            var tracks = track == null || track.IsNull
                ? string.Empty
                : $", track {track}";

            if (source is BaseCustomEvent customEvent)
                return $"{customEvent.Type} at beat {beat}{tracks}.";

            if (source is BaseGrid grid)
                return $"{source.ObjectType} at beat {beat}, position ({grid.PosX}, {grid.PosY}){tracks}.";

            if (source is BaseGLSEvent gls)
                return $"{source.GetType().Name} at beat {beat}, group ID [{gls.EventBoxGroupData?.ID}], box [{gls.BoxIndex}].";

            if (source is BaseEventBoxGroup group)
                return $"{source.GetType().Name} at beat {beat}, group ID [{group.ID}].";

            if (source is BaseEvent basicEvent)
                return $"Basic event at beat {beat}, type [{basicEvent.Type}]{tracks}.";

            return $"{source.ObjectType} at beat {beat}{tracks}.";
        }
    }

    internal readonly struct AnimationLogContext
    {
        private readonly BaseObject source;
        private readonly string property;
        private readonly string pointDefinition;
        private readonly float beat;

        internal AnimationLogContext(
            IPointDefinition.UntypedParams parameters,
            BaseCustomEvent source)
        {
            this.source = source ?? parameters.LogSource;
            property = parameters.Key;
            pointDefinition = parameters.Points is JSONString named
                ? named.Value
                : null;
            beat = parameters.Time;
        }

        internal string Describe(int pointIndex = -1)
        {
            var propertyName = property?.StartsWith("track_", StringComparison.Ordinal) == true
                ? property[6..]
                : property;
            var location = source != null
                ? BeatmapLogContext.Describe(source)
                : $"Animation at beat {beat.ToString("R", CultureInfo.InvariantCulture)}.";
            var definition = pointDefinition != null
                ? $", point definition [{pointDefinition}]"
                : string.Empty;
            var point = pointIndex >= 0
                ? $", point[{pointIndex}]"
                : string.Empty;
            return $"{location} Property [{propertyName}]{definition}{point}";
        }
    }
}
