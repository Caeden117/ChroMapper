using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Comparers;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;

namespace Tests.Infrastructure
{
    public static class BeatmapAssertion
    {
        public static readonly object NotesAreSorted = new();
        public static readonly object EventsAreSorted = new();
        public static readonly object EventsAreLinkedAndSorted = new();

        public static void IsInCollection<TObject>(TObject expected, string message = null)
            where TObject : BaseObject
        {
            var collection =
                BeatmapObjectContainerCollection.GetCollectionForType(expected.ObjectType) as
                    BeatmapObjectContainerCollection<TObject>;
            if (collection == null) Assert.Fail($"{expected.ObjectType} does not have a collection.");
            Assert.True(collection.MapObjects.Contains(expected), message);
        }

        public static void CollectionCount<TObject>(int count, string message = null)
            where TObject : BaseObject
        {
            var collection =
                BeatmapObjectContainerCollection.GetCollectionForType<BeatmapObjectContainerCollection<TObject>,
                    TObject>();
            if (collection == null) Assert.Fail($"{typeof(TObject).Name} does not have a collection.");

            Assert.AreEqual(count, collection.MapObjects.Count, message);
        }

        public static void IsEqual(object expected, object actual, string message)
        {
            switch (expected, actual)
            {
                case (BaseNote expectedNote, BaseNote actualNote):
                    AssertNote(expectedNote, actualNote, message);
                    break;
                case (BaseObstacle expectedObstacle, BaseObstacle actualObstacle):
                    AssertObstacle(expectedObstacle, actualObstacle, message);
                    break;
                case (BaseRotationEvent expectedRotationEvent, BaseRotationEvent actualRotationEvent):
                    AssertRotationEvent(expectedRotationEvent, actualRotationEvent, message);
                    break;
                case (BaseEvent expectedEvent, BaseEvent actualEvent):
                    AssertEvent(expectedEvent, actualEvent, message);
                    break;
                case (BaseArc expectedArc, BaseArc actualArc):
                    AssertArc(expectedArc, actualArc, message);
                    break;
                case (BaseChain expectedChain, BaseChain actualChain):
                    AssertChain(expectedChain, actualChain, message);
                    break;
                case (Type expectedType, BaseObject actualObject):
                    AssertObjectType(expectedType, actualObject, message);
                    break;
                case var _ when ReferenceEquals(expected, NotesAreSorted) && actual is IReadOnlyList<BaseNote> notes:
                    AssertNotesAreSorted(notes, message);
                    break;
                case var _ when ReferenceEquals(expected, EventsAreSorted) && actual is IReadOnlyList<BaseEvent> events:
                    AssertEventsAreSorted(events, message);
                    break;
                case var _ when ReferenceEquals(expected, EventsAreLinkedAndSorted)
                    && actual is IReadOnlyList<BaseEvent> linkedEvents:
                    AssertEventsLinksAreCorrectAndSorted(linkedEvents, message);
                    break;
                default:
                    throw new AssertionException($"{message}: Unsupported assertion types.");
            }
        }

        private static void AssertNote(BaseNote expected, BaseNote actual, string message)
        {
            Assert.AreEqual(expected.JsonTime, actual.JsonTime, 0.001f, $"{message}: Mismatched time");
            Assert.AreEqual(expected.Type, actual.Type, $"{message}: Mismatched type");
            Assert.AreEqual(expected.PosX, actual.PosX, $"{message}: Mismatched position X");
            Assert.AreEqual(expected.PosY, actual.PosY, $"{message}: Mismatched position Y");
            Assert.AreEqual(
                expected.CutDirection,
                actual.CutDirection,
                $"{message}: Mismatched cut direction");
            Assert.AreEqual(
                expected.AngleOffset,
                actual.AngleOffset,
                $"{message}: Mismatched angle offset");
            AssertCustomData(expected, actual, message);
        }

        private static void AssertObstacle(BaseObstacle expected, BaseObstacle actual, string message)
        {
            Assert.AreEqual(expected.JsonTime, actual.JsonTime, 0.001f, $"{message}: Mismatched time");
            Assert.AreEqual(expected.PosX, actual.PosX, $"{message}: Mismatched position X");
            Assert.AreEqual(expected.PosY, actual.PosY, $"{message}: Mismatched position Y");
            Assert.AreEqual(
                expected.Duration,
                actual.Duration,
                0.001f,
                $"{message}: Mismatched duration");
            Assert.AreEqual(expected.Width, actual.Width, $"{message}: Mismatched width");
            Assert.AreEqual(expected.Height, actual.Height, $"{message}: Mismatched height");
            Assert.AreEqual(expected.Type, actual.Type, $"{message}: Mismatched type");
            AssertCustomData(expected, actual, message);
        }

        private static void AssertEvent(BaseEvent expected, BaseEvent actual, string message)
        {
            Assert.AreEqual(expected.JsonTime, actual.JsonTime, 0.001f, $"{message}: Mismatched time");
            Assert.AreEqual(expected.Type, actual.Type, $"{message}: Mismatched type");
            Assert.AreEqual(expected.Value, actual.Value, $"{message}: Mismatched value");
            Assert.AreEqual(
                expected.FloatValue,
                actual.FloatValue,
                0.001f,
                $"{message}: Mismatched float value");
            AssertCustomData(expected, actual, message);
        }

        private static void AssertRotationEvent(BaseRotationEvent expected, BaseRotationEvent actual, string message)
        {
            Assert.AreEqual(expected.JsonTime, actual.JsonTime, 0.001f, $"{message}: Mismatched time");
            Assert.AreEqual(expected.Type, actual.Type, $"{message}: Mismatched type");
            Assert.AreEqual(
                expected.Rotation,
                actual.Rotation,
                0.001f,
                $"{message}: Mismatched rotation");
            AssertCustomData(expected, actual, message);
        }

        private static void AssertArc(BaseArc expected, BaseArc actual, string message)
        {
            Assert.AreEqual(expected.JsonTime, actual.JsonTime, 0.001f, $"{message}: Mismatched time");
            Assert.AreEqual(expected.Color, actual.Color, $"{message}: Mismatched color");
            Assert.AreEqual(expected.PosX, actual.PosX, $"{message}: Mismatched position X");
            Assert.AreEqual(expected.PosY, actual.PosY, $"{message}: Mismatched position Y");
            Assert.AreEqual(
                expected.CutDirection,
                actual.CutDirection,
                $"{message}: Mismatched cut direction");
            Assert.AreEqual(
                expected.AngleOffset,
                actual.AngleOffset,
                $"{message}: Mismatched angle offset");
            Assert.AreEqual(
                expected.HeadControlPointLengthMultiplier,
                actual.HeadControlPointLengthMultiplier,
                $"{message}: Mismatched head control point length multiplier");
            Assert.AreEqual(
                expected.TailJsonTime,
                actual.TailJsonTime,
                0.001f,
                $"{message}: Mismatched tail time");
            Assert.AreEqual(
                expected.TailPosX,
                actual.TailPosX,
                $"{message}: Mismatched tail position X");
            Assert.AreEqual(
                expected.TailPosY,
                actual.TailPosY,
                $"{message}: Mismatched tail position Y");
            Assert.AreEqual(
                expected.TailCutDirection,
                actual.TailCutDirection,
                $"{message}: Mismatched tail cut direction");
            Assert.AreEqual(
                expected.TailControlPointLengthMultiplier,
                actual.TailControlPointLengthMultiplier,
                $"{message}: Mismatched tail control point length multiplier");
            Assert.AreEqual(
                expected.MidAnchorMode,
                actual.MidAnchorMode,
                $"{message}: Mismatched mid anchor mode");
            AssertCustomData(expected, actual, message, true);
        }

        private static void AssertChain(BaseChain expected, BaseChain actual, string message)
        {
            Assert.AreEqual(expected.JsonTime, actual.JsonTime, 0.001f, $"{message}: Mismatched time");
            Assert.AreEqual(expected.Color, actual.Color, $"{message}: Mismatched color");
            Assert.AreEqual(expected.PosX, actual.PosX, $"{message}: Mismatched position X");
            Assert.AreEqual(expected.PosY, actual.PosY, $"{message}: Mismatched position Y");
            Assert.AreEqual(
                expected.CutDirection,
                actual.CutDirection,
                $"{message}: Mismatched cut direction");
            Assert.AreEqual(
                expected.AngleOffset,
                actual.AngleOffset,
                $"{message}: Mismatched angle offset");
            Assert.AreEqual(
                expected.TailJsonTime,
                actual.TailJsonTime,
                0.001f,
                $"{message}: Mismatched tail time");
            Assert.AreEqual(
                expected.TailPosX,
                actual.TailPosX,
                $"{message}: Mismatched tail position X");
            Assert.AreEqual(
                expected.TailPosY,
                actual.TailPosY,
                $"{message}: Mismatched tail position Y");
            Assert.AreEqual(
                expected.SliceCount,
                actual.SliceCount,
                $"{message}: Mismatched slice count");
            Assert.AreEqual(expected.Squish, actual.Squish, $"{message}: Mismatched squish");
            AssertCustomData(expected, actual, message, true);
        }

        private static void AssertObjectType(Type expectedType, BaseObject actualObject, string message)
        {
            if (expectedType == typeof(V3Object) && actualObject is not V3Object)
                Assert.Fail($"{message}: Object is not beatmap v3 object");

            if (expectedType == typeof(V2Object) && actualObject is not V2Object)
                Assert.Fail($"{message}: Object is not beatmap v2 object");
        }

        private static void AssertCustomData(
            BaseObject expected,
            BaseObject actual,
            string message,
            bool writeCustom = false)
        {
            if (expected.CustomData == null) return;

            if (writeCustom) actual.WriteCustom();
            Assert.AreEqual(
                expected.CustomData.ToString(),
                actual.CustomData?.ToString(),
                $"{message}: Mismatched custom data");
        }

        public static void IsEqualWithChanges<TObject>(
            TObject baseline,
            TObject actual,
            Action<TObject> applyExpectedChanges,
            string message)
            where TObject : BaseItem
        {
            var expected = BeatmapFactory.Clone(baseline);
            applyExpectedChanges(expected);
            IsEqual(expected, actual, message);
        }

        public static void IsUnchanged<TObject>(TObject baseline, TObject actual, string message = null)
            where TObject : BaseItem
        {
            var expected = BeatmapFactory.Clone(baseline);
            IsEqual(expected, actual, message);
        }

        private static void AssertNotesAreSorted(IReadOnlyList<BaseNote> noteMapObjects, string message)
        {
            for (var i = 1; i < noteMapObjects.Count; i++)
                if (noteMapObjects[i - 1].CompareTo(noteMapObjects[i]) == 1)
                    Assert.Fail(
                        $"{message}: Notes {noteMapObjects[i - 1]} and {noteMapObjects[i]} are out of order | i = {i}");
        }

        // Basic-event collections now preserve authored ties, so verify their chronological comparer rather than payload ordering.
        private static void AssertEventsAreSorted(IReadOnlyList<BaseEvent> eventMapObjects, string message)
        {
            for (var i = 1; i < eventMapObjects.Count; i++)
                if (EventOrderComparer.Instance.Compare(eventMapObjects[i - 1], eventMapObjects[i]) > 0)
                    Assert.Fail(
                        $"{message}: Events {eventMapObjects[i - 1]} and {eventMapObjects[i]} are out of order | i = {i}");
        }

        private static void AssertEventsLinksAreCorrectAndSorted(
            IReadOnlyList<BaseEvent> eventMapObjects,
            string message)
        {
            if (eventMapObjects.Count == 1)
            {
                AssertEventPrevAndNext($"{message}: 0", eventMapObjects[0], null, null);
                return;
            }

            for (var i = 0; i < eventMapObjects.Count; i++)
                if (i == 0)
                    AssertEventPrevAndNext($"{message}: {i}", eventMapObjects[i], null, eventMapObjects[i + 1]);
                else if (i == eventMapObjects.Count - 1)
                    AssertEventPrevAndNext($"{message}: {i}", eventMapObjects[i], eventMapObjects[i - 1], null);
                else
                    AssertEventPrevAndNext(
                        $"{message}: {i}",
                        eventMapObjects[i],
                        eventMapObjects[i - 1],
                        eventMapObjects[i + 1]);

            AssertEventsAreSorted(eventMapObjects, message);
        }

        private static void AssertEventPrevAndNext(string message, BaseEvent evt, BaseEvent prevEvt, BaseEvent nextEvt)
        {
            if (prevEvt != null) Assert.AreEqual(evt, prevEvt.Next, $"{message} Mismatched Prev.Next");

            Assert.AreEqual(prevEvt, evt.Prev, $"{message} Mismatched Event.Prev");
            Assert.AreEqual(nextEvt, evt.Next, $"{message} Mismatched Event.Next");

            if (nextEvt != null) Assert.AreEqual(evt, nextEvt.Prev, $"{message} Mismatched Next.Prev");
        }
    }
}
