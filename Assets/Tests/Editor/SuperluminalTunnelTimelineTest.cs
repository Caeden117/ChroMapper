using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base.Customs;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class SuperluminalTunnelTimelineTest
    {
        private static readonly Vector3 HiddenPosition = new(0, -69420, 0);
        private static readonly Vector3 VisiblePosition = new(204.31633f, 217.5754395f, 1529.1003418f);

        [TestCase(0f)]
        [TestCase(63f)]
        public void ConstantAnimateTrackReplacesPreviousPointAtExactStart(float duration)
        {
            var property = CreatePositionProperty(duration, 0f);
            foreach (var beat in new[] { 195.999f, 196f, 196.015f, 259f, 195.999f, 196f })
            {
                var expected = beat < 196f
                    ? HiddenPosition
                    : VisiblePosition;
                Assert.That(
                    Vector3.Distance(property.GetLerpedValue(beat), expected),
                    Is.LessThan(0.001f),
                    $"Constant AnimateTrack duration {duration} must replace the previous point at beat {beat}.");
            }
        }

        [Test]
        public void AssignPathAnimationRetainsItsExplicitTransition()
        {
            var property = CreatePositionProperty(0f, 2f);
            foreach (var beat in new[] { 195.999f, 196f, 197f, 198f, 199f, 196f, 197f })
            {
                var expected = Vector3.Lerp(HiddenPosition, VisiblePosition, (beat - 196f) / 2f);
                Assert.That(
                    Vector3.Distance(property.GetLerpedValue(beat), expected),
                    Is.LessThan(0.01f),
                    $"AssignPathAnimation must preserve its two-beat transition at beat {beat}.");
            }
        }

        private static AnimateProperty<Vector3> CreatePositionProperty(float duration, float transition)
        {
            var property = new AnimateProperty<Vector3>(
                new List<PointDefinition<Vector3>>(),
                IgnorePosition,
                Vector3.zero);
            property.AddPointDef(
                PointDataParsers.ParseVector3,
                new IPointDefinition.UntypedParams
                {
                    Points = JSON.Parse("[0,-69420,0]"),
                    Time = 0f,
                    TimeBegin = 0f,
                    TimeEnd = 0f
                },
                new BaseCustomEvent { JsonTime = 0f });
            property.AddPointDef(
                PointDataParsers.ParseVector3,
                new IPointDefinition.UntypedParams
                {
                    Points = JSON.Parse("[204.31633,217.5754395,1529.1003418]"),
                    Time = 196f,
                    Duration = duration,
                    Transition = transition,
                    TimeBegin = 196f,
                    TimeEnd = 196f + duration
                },
                new BaseCustomEvent { JsonTime = 196f });
            property.Sort();
            return property;
        }

        private static void IgnorePosition(Vector3 position)
        {
        }
    }
}
