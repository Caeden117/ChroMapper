using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Repeated decimal grid steps must not accumulate float error, and exact grid fractions and
    // authored duration remainders must survive editing.
    public class ObstacleDurationPrecisionTest : TestBase
    {
        private bool originalCap;
        private int originalDecimalPrecision;
        private int originalTimeValueDecimalPrecision;

        [SetUp]
        public void CaptureJsonPrecision()
        {
            originalCap = JSONNumber.CapNumbersToDecimals;
            originalDecimalPrecision = JSONNumber.DecimalPrecision;
            originalTimeValueDecimalPrecision = Settings.Instance.TimeValueDecimalPrecision;

            // Keep the export setting and JSON formatter precision in sync for these cases.
            Settings.Instance.TimeValueDecimalPrecision = 6;
            JSONNumber.DecimalPrecision = 6;
        }

        [TearDown]
        public void RestoreJsonPrecision()
        {
            JSONNumber.CapNumbersToDecimals = originalCap;
            JSONNumber.DecimalPrecision = originalDecimalPrecision;
            Settings.Instance.TimeValueDecimalPrecision = originalTimeValueDecimalPrecision;
        }

        [TestCase(4, 1.4f, TestName = "FourTenthBeatTweaksLandOnDecimalDuration")]
        [TestCase(100, 11f, TestName = "HundredTenthBeatTweaksLandOnDecimalDuration")]
        public void TenthBeatTweaksLandOnDecimalDuration(int ticks, float expected)
        {
            var wall = PlaceWallAndTweakDuration(1f, 0.1f, ticks);

            TestContext.WriteLine($"ticks={ticks} duration={wall.Duration:R}");

            Assert.AreEqual(expected, wall.Duration, "In-memory duration drifted off the decimal grid");
        }

        // V2 emits the raw float duration while V3 routes it through RoundBeat, so the capped six-decimal
        // path can hide drift the uncapped and V2 exports still write out; compare the emitted JSON only.
        [TestCase(4, 2, true, 1.4f, TestName = "FourTenthBeatTweaksSaveWithoutAccumulatedErrorV2Capped")]
        [TestCase(4, 2, false, 1.4f, TestName = "FourTenthBeatTweaksSaveWithoutAccumulatedErrorV2Uncapped")]
        [TestCase(4, 3, true, 1.4f, TestName = "FourTenthBeatTweaksSaveWithoutAccumulatedErrorV3Capped")]
        [TestCase(4, 3, false, 1.4f, TestName = "FourTenthBeatTweaksSaveWithoutAccumulatedErrorV3Uncapped")]
        [TestCase(100, 2, true, 11f, TestName = "HundredTenthBeatTweaksSaveWithoutAccumulatedErrorV2Capped")]
        [TestCase(100, 2, false, 11f, TestName = "HundredTenthBeatTweaksSaveWithoutAccumulatedErrorV2Uncapped")]
        [TestCase(100, 3, true, 11f, TestName = "HundredTenthBeatTweaksSaveWithoutAccumulatedErrorV3Capped")]
        [TestCase(100, 3, false, 11f, TestName = "HundredTenthBeatTweaksSaveWithoutAccumulatedErrorV3Uncapped")]
        public void TenthBeatTweaksSaveWithoutAccumulatedError(
            int ticks,
            int version,
            bool cap,
            float expected)
        {
            JSONNumber.CapNumbersToDecimals = cap;
            var wall = PlaceWallAndTweakDuration(1f, 0.1f, ticks);

            var node = version == 2
                ? V2Obstacle.ToJson(wall)
                : V3Obstacle.ToJson(wall);
            var json = node.ToString();
            TestContext.WriteLine(
                $"ticks={ticks} version={version} cap={cap} duration={wall.Duration:R} json={json}");

            var parsed = JSON.Parse(json);
            var durationKey = version == 2
                ? "_duration"
                : "d";
            Assert.AreEqual(
                expected,
                parsed[durationKey].AsFloat,
                "Saved duration carried accumulated float error");
        }

        // Power-of-two grid steps are exactly representable in binary and must not lose precision
        // during editing.
        [TestCase(16, 1, 1.0625f)]
        [TestCase(16, 16, 2f)]
        [TestCase(64, 64, 2f)]
        public void GridTweaksPreserveExactDuration(int denominator, int ticks, float expected)
        {
            var wall = PlaceWallAndTweakDuration(1f, 1f / denominator, ticks);

            Assert.AreEqual(expected, wall.Duration, "Grid-fraction duration changed");
        }

        // An authored sub-millibeat duration is not grid-aligned; one quarter-beat tweak must keep the
        // authored remainder instead of snapping the whole duration to the decimal grid.
        [Test]
        public void QuarterBeatTweakPreservesAuthoredSubMillibeatPrecision()
        {
            var wall = PlaceWallAndTweakDuration(1.234567f, 0.25f, 1);

            Assert.AreEqual(1.484567f, wall.Duration, "Sub-millibeat duration remainder was lost");
        }

        private static BaseObstacle PlaceWallAndTweakDuration(float duration, float stepDelta, int ticks)
        {
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 5, Duration = duration });
            var container = (ObstacleContainer)BeatmapObjectContainerCollection
                .GetCollectionForType(ObjectType.Obstacle)
                .LoadedContainers[wall];
            var controller = Object.FindAnyObjectByType<BeatmapObstacleInputController>();

            // Duration tweaks edit the placed wall in place, so the same container serves every tick.
            for (var i = 0; i < ticks; i++)
                controller.TweakDuration(container, stepDelta);

            return wall;
        }
    }
}
