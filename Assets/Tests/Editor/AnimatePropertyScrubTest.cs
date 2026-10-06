using System;
using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base.Customs;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    // AnimatePropertyScrubTest constrains animation evaluation as a pure function of the requested beat, so seeking
    // backward, restarting at beat zero, and replaying a previously visited beat cannot inherit a later transform.
    public class AnimatePropertyScrubTest
    {
        private static readonly float[] SampleTimes = { 0f, 2f, 4f, 5f, 7.5f, 8f, 10f };

        // ScrubbingBeforeFirstEventRestoresEveryPropertyDefault reproduces the stale-state leak for all values used by
        // track/object animation: a later evaluation must not leave position, rotation, scale, color, or float state set.
        [Test]
        public void ScrubbingBeforeFirstEventRestoresEveryPropertyDefault()
        {
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseVector3,
                new Vector3(11f, 12f, 13f),
                "[[20,30,40,0],[50,60,70,1]]",
                (expected, actual) => Vector3.Distance(expected, actual) < 0.0001f,
                "position");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseQuaternion,
                Quaternion.Euler(11f, 12f, 13f),
                "[[20,30,40,0],[50,60,70,1]]",
                (expected, actual) => Quaternion.Angle(expected, actual) < 0.001f,
                "rotation");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseVector3,
                new Vector3(2f, 3f, 4f),
                "[[5,6,7,0],[8,9,10,1]]",
                (expected, actual) => Vector3.Distance(expected, actual) < 0.0001f,
                "scale");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseColor,
                new Color(0.1f, 0.2f, 0.3f, 0.4f),
                "[[0.5,0.6,0.7,0.8,0],[0.9,1,0.8,0.7,1]]",
                (expected, actual) => Vector4.Distance(expected, actual) < 0.0001f,
                "color");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseFloat,
                0.25f,
                "[[0.75,0],[1,1]]",
                (expected, actual) => Mathf.Abs(expected - actual) < 0.0001f,
                "float");
        }

        // ScrubbingInArbitraryOrderMatchesForwardPlayback constrains every sampled beat to the value obtained by a
        // fresh monotonic pass, including repeated forward/backward seeks across two distinct AnimateTrack events.
        [Test]
        public void ScrubbingInArbitraryOrderMatchesForwardPlayback()
        {
            var forward = CreatePositionProperty();
            var expected = new Dictionary<float, Vector3>();
            foreach (var sampleTime in SampleTimes)
            {
                expected[sampleTime] = forward.GetLerpedValue(sampleTime);
            }

            var scrubbed = CreatePositionProperty();
            var scrubOrder = new[] { 10f, 0f, 7.5f, 2f, 8f, 4f, 5f, 10f, 0f, 5f };
            foreach (var sampleTime in scrubOrder)
            {
                var actual = scrubbed.GetLerpedValue(sampleTime);
                Assert.That(
                    Vector3.Distance(actual, expected[sampleTime]),
                    Is.LessThan(0.0001f),
                    $"Seeking to beat {sampleTime} depended on the previously evaluated beat.");
            }
        }

        // Regression source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
        // The repeat events below are synthetic reductions of its color-strobe regression.
        // Heck's CoroutineEventManager stops the running coroutine when the next event on the same
        // property starts, so an expanded repeat must not evaluate past the next event's start;
        // removing that later event must let the earlier event's repeats resume. The Salty b176
        // repeat=6969 strobe previously swallowed the b177 restore and b189 fade on the color
        // property, leaving notes black for the rest of the map.
        [Test]
        public void RepeatsStopAtNextEventAndResumeWhenItIsRemoved()
        {
            var repeating = new BaseCustomEvent { JsonTime = 0f };
            var later = new BaseCustomEvent { JsonTime = 4f };
            var property = CreateRepeatingFloatProperty(repeating, later);

            Assert.That(
                property.GetLerpedValue(3.5f),
                Is.EqualTo(5f),
                "The live repeat inside its own window must evaluate normally.");
            Assert.That(
                property.GetLerpedValue(4.5f),
                Is.EqualTo(9f),
                "The later event must kill the earlier event's repeats starting at its own beat.");
            Assert.That(
                property.GetLerpedValue(6.5f),
                Is.EqualTo(9f),
                "Repeats expanded past the later event must stay dead.");

            property.RemoveEvent(later);
            property.Sort();
            Assert.That(
                property.GetLerpedValue(6.5f),
                Is.EqualTo(5f),
                "Removing the later event must let the earlier event's repeats resume.");
        }

        // A same-beat event fires after the earlier one in file order, so Heck stops the earlier
        // coroutine and its repeats even though the start beats are equal.
        [Test]
        public void SameBeatLaterEventKillsEarlierRepeats()
        {
            var repeating = new BaseCustomEvent { JsonTime = 0f };
            var later = new BaseCustomEvent { JsonTime = 0f };
            var property = CreateRepeatingFloatProperty(repeating, later, laterStart: 0f);

            Assert.That(
                property.GetLerpedValue(2.5f),
                Is.EqualTo(9f),
                "A same-beat event authored after a repeating event must kill its repeats.");
        }

        private static AnimateProperty<float> CreateRepeatingFloatProperty(
            BaseCustomEvent repeating,
            BaseCustomEvent later,
            float laterStart = 4f)
        {
            var property = new AnimateProperty<float>(
                new List<PointDefinition<float>>(),
                _ => { },
                -1f);
            property.AddPointDef(
                PointDataParsers.ParseFloat,
                new IPointDefinition.UntypedParams
                {
                    Points = JSON.Parse("[[5,0]]"),
                    Time = 0f,
                    Duration = 1f,
                    TimeBegin = 0f,
                    TimeEnd = 1f,
                    Repeat = 8
                },
                repeating);
            property.AddPointDef(
                PointDataParsers.ParseFloat,
                new IPointDefinition.UntypedParams
                {
                    Points = JSON.Parse("[[9,0]]"),
                    Time = laterStart,
                    Duration = 0f,
                    TimeBegin = laterStart,
                    TimeEnd = laterStart
                },
                later);
            property.Sort();
            return property;
        }

        // Use two non-contiguous events so the arbitrary-order test covers interpolation, post-event holds, and
        // selection of the correct prior event rather than only the before-first-event reset edge case.
        private static AnimateProperty<Vector3> CreatePositionProperty()
        {
            var property = new AnimateProperty<Vector3>(
                new List<PointDefinition<Vector3>>(),
                _ => { },
                new Vector3(1f, 2f, 3f));
            AddEvent(property, PointDataParsers.ParseVector3, 4f, 2f, "[[10,20,30,0],[20,30,40,1]]");
            AddEvent(property, PointDataParsers.ParseVector3, 8f, 2f, "[[50,60,70,0],[80,90,100,1]]");
            property.Sort();
            return property;
        }

        // Evaluating the later point first mirrors the reported scrub sequence; UpdateProperty exercises the same
        // setter path used by TrackAnimator and ObjectAnimator before checking that beat zero restores the default.
        private static void AssertReturnsDefaultBeforeFirstEvent<T>(
            PointDefinition<T>.Parser parser,
            T defaultValue,
            string points,
            Func<T, T, bool> equals,
            string propertyName)
            where T : struct
        {
            var actual = defaultValue;
            var property = new AnimateProperty<T>(
                new List<PointDefinition<T>>(),
                value => actual = value,
                defaultValue);
            AddEvent(property, parser, 4f, 2f, points);
            property.Sort();

            property.UpdateProperty(5f);
            Assert.That(equals(defaultValue, actual), Is.False, $"The {propertyName} fixture did not leave its default.");
            property.UpdateProperty(0f);
            Assert.That(
                equals(defaultValue, actual),
                Is.True,
                $"Seeking before the first {propertyName} event retained a later animated value.");
        }

        // Construct production point definitions through the same JSON parser and timing fields used by AnimateTrack.
        private static void AddEvent<T>(
            AnimateProperty<T> property,
            PointDefinition<T>.Parser parser,
            float startTime,
            float duration,
            string points)
            where T : struct
        {
            property.AddPointDef(
                parser,
                new IPointDefinition.UntypedParams
                {
                    Points = JSON.Parse(points),
                    Time = startTime,
                    Duration = duration,
                    TimeBegin = startTime,
                    TimeEnd = startTime + duration
                },
                null);
        }
    }
}
