using System;
using System.Collections.Generic;
using Beatmap.Base.Customs;

namespace Beatmap.Animations
{
    // Component null requests stop their timeline without deleting earlier writes or changing AnimateTrack null semantics.
    internal sealed class ComponentAnimationProperty
    {
        private readonly AnimateProperty<float> property;
        private readonly List<BaseCustomEvent> stops = new();
        private (float Time, bool Stops, int FileOrder, int Order, int PreviousAnimation)[] requests =
            Array.Empty<(float, bool, int, int, int)>();
        private int lastAnimation = -1;

        internal ComponentAnimationProperty(Action<float> setter, float baseline)
        {
            property = new AnimateProperty<float>(new List<PointDefinition<float>>(), setter, baseline);
        }

        internal float Default
        {
            get => property.Default;
            set => property.Default = value;
        }

        internal float StartTime => property.IsEmpty()
            ? float.PositiveInfinity
            : property.StartTime;
        internal bool IsEmpty() => property.IsEmpty() && stops.Count == 0;
        internal void StopAt(BaseCustomEvent source) => stops.Add(source);

        internal void AddPointDef(
            PointDefinition<float>.Parser parser,
            IPointDefinition.UntypedParams parameters,
            BaseCustomEvent source)
        {
            property.AddPointDef(parser, parameters, source);
        }

        internal void RemoveEvent(BaseCustomEvent source)
        {
            property.RemoveEvent(source);
            stops.RemoveAll(candidate => ReferenceEquals(candidate, source));
        }

        internal void Sort()
        {
            var timeline = new List<(float Time, bool Stops, int FileOrder, int Order, int PreviousAnimation)>();
            var sources = new HashSet<BaseCustomEvent>();
            if (!property.IsEmpty())
            {
                property.Sort();
                foreach (var definition in property.PointDefinitions)
                {
                    var source = definition.Source;
                    if (sources.Add(source))
                        timeline.Add((source.JsonTime, false, source.FileOrder, timeline.Count, -1));

                }
            }

            foreach (var source in stops)
                timeline.Add((source.JsonTime, true, source.FileOrder, timeline.Count, -1));

            timeline.Sort(
                static (a, b) =>
                {
                    var byChronology = Beatmap.Comparers.EventOrderComparer.CompareKeys(
                        a.Time,
                        a.FileOrder,
                        b.Time,
                        b.FileOrder);
                    return byChronology != 0
                        ? byChronology
                        : a.Order.CompareTo(b.Order);
                });
            requests = timeline.ToArray();
            var previousAnimation = -1;
            for (var i = 0; i < requests.Length; i++)
            {
                if (!requests[i].Stops)
                    previousAnimation = i;

                requests[i].PreviousAnimation = previousAnimation;
            }

            lastAnimation = -1;
        }

        internal void UpdateProperty(float time, bool stoppedSeek = false)
        {
            if (property.IsEmpty())
                return;

            var index = requests.AsSpan().UpperBoundBy(time, static request => request.Time) - 1;
            if (index >= 0 && requests[index].Stops)
            {
                var previousAnimation = requests[index].PreviousAnimation;
                if (previousAnimation < 0 || (!stoppedSeek && lastAnimation == previousAnimation))
                    return;

                time = requests[index].Time;
                lastAnimation = previousAnimation;
            }

            else
                lastAnimation = index;

            property.UpdateProperty(time);
        }

        internal void RefreshBpmTiming()
        {
            property.RefreshBpmTiming();
            Sort();
        }
    }
}
