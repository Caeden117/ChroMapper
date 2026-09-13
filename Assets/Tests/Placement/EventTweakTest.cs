using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Placement
{
    public class EventTweakTest : TestBase
    {
        [Test]
        public void TweakLaserSpeedMainValue()
        {
            var eventA = PlaceEvent(2, EventTypeValue.Event12, 2);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Laser speed depends on scroll precision; Medium makes the main-value increment exactly one.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e => e.Value = 3,
                3f,
                e => e.CustomSpeed,
                "Laser speed main value");
        }

        [Test]
        public void TweakRingRotationMainValue()
        {
            var eventA = PlaceEvent(4, EventTypeValue.Event8);
            // An existing Chroma rotation remains precision-adjustable after the directional initialization gesture.
            eventA.CustomRingRotation = 90f;
            eventA.CustomDirection = 0;
            eventA.WriteCustom();
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Ring rotation depends on scroll precision; High selects the 2.5-degree main-value increment.
            precision.CurrentPrecision = ScrollPrecision.High;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e =>
                {
                    e.CustomRingRotation = 92.5f;
                    e.WriteCustom();
                },
                92.5f,
                e => e.CustomRingRotation,
                "Ring rotation main value");
        }

        [Test]
        public void TweakUnsetRingRotationUpStartsAtClockwiseNinetyDegrees()
        {
            // The first upward Alt+Scroll is a directional initializer, not a precision increment above the baseline.
            var eventA = PlaceEvent(4.25f, EventTypeValue.Event8);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.Low;

            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            Assert.AreEqual(90f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Initial upward ring rotation");
            Assert.AreEqual(1, eventA.CustomDirection, "Initial upward ring direction");
        }

        [Test]
        public void TweakUnsetRingRotationDownStartsAtCounterClockwiseNinetyDegrees()
        {
            // The first downward Alt+Scroll initializes the same positive magnitude and expresses CCW through direction.
            var eventA = PlaceEvent(4.5f, EventTypeValue.Event8);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.Low;

            controller.TweakMain(GetContainer(eventA), -1);
            eventA = Refresh(eventA);

            Assert.AreEqual(90f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Initial downward ring rotation");
            Assert.AreEqual(0, eventA.CustomDirection, "Initial downward ring direction");
        }

        [Test]
        public void TweakRingRotationDownClampsAtZero()
        {
            // Crossing zero must clamp the non-negative magnitude instead of encoding direction through a negative rotation.
            var eventA = PlaceEvent(4.75f, EventTypeValue.Event8);
            eventA.CustomRingRotation = 10f;
            eventA.CustomDirection = 0;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.Low;

            controller.TweakMain(GetContainer(eventA), -1);
            eventA = Refresh(eventA);

            Assert.AreEqual(0f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Ring rotation lower bound");
            Assert.AreEqual(0, eventA.CustomDirection, "Clamping rotation must preserve direction");
        }

        [Test]
        public void TweakNegativeDirectedRingRotationNormalizesEquivalentDirectionBeforeEditing()
        {
            // A signed rotation with an explicit direction is redundant, so normalize it before applying the scroll delta.
            var eventA = PlaceEvent(4.875f, EventTypeValue.Event8);
            eventA.CustomRingRotation = -90f;
            eventA.CustomDirection = 1;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.High;

            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            Assert.AreEqual(92.5f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Normalized ring rotation");
            Assert.AreEqual(0, eventA.CustomDirection, "Normalized ring direction");
        }

        [Test]
        public void TweakRingZoomMainValue()
        {
            var eventA = PlaceEvent(5, EventTypeValue.Event9);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Ring zoom starts at its 2-unit baseline and Medium now uses the configured
            // 0.1-unit zoom step, so TweakRingZoomMainValue must expect 2.1.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e =>
                {
                    // Match the current Medium ring-zoom precision rather than the retired 0.25 step.
                    e.CustomStep = 2.1f;
                    e.WriteCustom();
                },
                // Assert the same updated Chroma value returned by the editor tweak.
                2.1f,
                e => e.CustomStep,
                "Ring zoom main value");
        }

        [Test]
        public void TweakTheSecondRingZoomUsesUltraStepPrecision()
        {
            var eventA = PlaceEvent(5.25f, EventTypeValue.Event9, 2);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            var tracksDefinition = Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions;
            var eventDefinition = tracksDefinition.GetBasicOrDefault((int)EventTypeValue.Event9);
            var originalComponents = eventDefinition.Components;

            try
            {
                // The Second's special Event 9 path must preserve the same 0.005 Ultra increment and thousandth precision as ordinary ring zoom.
                eventDefinition.Components = BasicEventComponent.SmoothStepRingZoom;
                precision.CurrentPrecision = ScrollPrecision.Ultra;
                controller.TweakMain(GetContainer(eventA), 1);
                eventA = Refresh(eventA);

                Assert.AreEqual(2, eventA.Value, "The Second integer fallback");
                Assert.AreEqual(2.005f, eventA.CustomStep ?? float.NaN, 0.0001f, "The Second Ultra ring zoom step");
                Assert.That(eventA.CustomData.HasKey("step"), Is.True, "The Second Ultra step serialization");
            }
            finally
            {
                // Restore shared environment metadata so this classification change cannot leak into later placement tests.
                eventDefinition.Components = originalComponents;
            }
        }

        [TestCase(0, 0.5f, -1, 0, TestName = "TheSecondRingZoomFloatToZeroRemovesStep")]
        [TestCase(2, 2.5f, 1, 3, TestName = "TheSecondRingZoomFloatToMiddleIntegerRemovesStep")]
        [TestCase(8, 8.5f, 1, 9, TestName = "TheSecondRingZoomFloatToNineRemovesStep")]
        public void TheSecondRingZoomFloatToInRangeIntegerRemovesStep(
            int initialValue,
            float initialCustomStep,
            int modifier,
            int expectedValue)
        {
            // Landing on an OEM integer anywhere in the inclusive 0..9 range must remove customData.step entirely.
            AssertTheSecondRingZoomScrolls(
                initialValue,
                initialCustomStep,
                ScrollPrecision.Low,
                modifier,
                1,
                expectedValue,
                null);
        }

        [Test]
        public void TheSecondRingZoomIOnlyNineToTenKeepsStepAndClampsI()
        {
            // Two Low hover-scroll ticks prove an i-only 9 transitions through 9.5 to step 10 while i remains clamped at 9.
            AssertTheSecondRingZoomScrolls(9, null, ScrollPrecision.Low, 1, 2, 9, 10f);
        }

        [Test]
        public void TheSecondRingZoomIOnlyZeroToNegativeOneKeepsStepAndClampsI()
        {
            // Two negative Low hover-scroll ticks prove an i-only 0 transitions through -0.5 to step -1 while i remains clamped at 0.
            AssertTheSecondRingZoomScrolls(0, null, ScrollPrecision.Low, -1, 2, 0, -1f);
        }

        [TestCase(1, 2, 2.1f, TestName = "TheSecondRingZoomIOnlyToFractionalStepRoundsIDown")]
        [TestCase(6, 3, 2.6f, TestName = "TheSecondRingZoomIOnlyToFractionalStepRoundsIUp")]
        public void TheSecondRingZoomIOnlyToFractionalStepWritesStepAndNearestI(
            int tickCount,
            int expectedValue,
            float expectedCustomStep)
        {
            // Medium 0.1 ticks cover both sides of the nearest-integer boundary from an i-only starting node.
            AssertTheSecondRingZoomScrolls(
                2,
                null,
                ScrollPrecision.Medium,
                1,
                tickCount,
                expectedValue,
                expectedCustomStep);
        }

        [Test]
        public void TheSecondRingZoomFontShrinksForLongRenderedStep()
        {
            var eventA = PlaceEvent(5.5f, EventTypeValue.Event9, 1);
            var container = GetContainer(eventA);
            var eventDefinition = container.TrackDefinitions.GetBasicOrDefault((int)EventTypeValue.Event9);
            var originalComponents = eventDefinition.Components;

            try
            {
                // The Second's single-line zoom label must scale from its rendered character count so Z-20.025 fits where Z1 already fits.
                eventDefinition.Components = BasicEventComponent.SmoothStepRingZoom;
                container.RefreshAppearance();
                var valueDisplay = GetEventValueDisplay(container);
                Assert.AreEqual("Z1", valueDisplay.text, "Short The Second ring zoom label");
                var shortFontSize = valueDisplay.fontSize;

                eventA.CustomStep = -20.025f;
                eventA.WriteCustom();
                container.RefreshAppearance();

                Assert.AreEqual("Z-20.025", valueDisplay.text, "Long The Second ring zoom label");
                Assert.AreEqual(
                    shortFontSize * 0.375f,
                    valueDisplay.fontSize,
                    0.001f,
                    "Long The Second ring zoom label font scale");
            }
            finally
            {
                // Restore shared environment metadata so the appearance classification cannot leak into later tests.
                eventDefinition.Components = originalComponents;
            }
        }

        [Test]
        public void TweakLightMainBrightness()
        {
            var eventA = PlaceEvent(6, EventTypeValue.Event0, 2);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Light brightness depends on scroll precision; Medium selects the 0.1 brightness increment.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            BeatmapAssertion.IsEqualWithChanges(
                original,
                eventA,
                e => e.FloatValue = 1.1f,
                "Light main brightness");
        }

        [Test]
        public void TweakColorBoostMainValue()
        {
            var eventA = PlaceEvent(7, EventTypeValue.ColorBoostEventType);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            BeatmapAssertion.IsEqualWithChanges(
                original,
                eventA,
                e => e.Value = 1,
                "Color boost main value");
        }

        [Test]
        public void InvertLaserSpeedDirection()
        {
            var eventA = PlaceEvent(8, EventTypeValue.Event12, 2);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.InvertEvent(GetContainer(eventA));
            eventA = Refresh(eventA);

            Assert.AreEqual(1, eventA.CustomDirection, "Laser direction Chroma value");
        }

        [Test]
        public void InvertRingRotationDirection()
        {
            var eventA = PlaceEvent(9, EventTypeValue.Event8);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.InvertEvent(GetContainer(eventA));
            eventA = Refresh(eventA);

            Assert.AreEqual(1, eventA.CustomDirection, "Ring rotation direction Chroma value");
        }

        [Test]
        public void InvertRingZoomStep()
        {
            var eventA = PlaceEvent(10, EventTypeValue.Event9);
            eventA.CustomStep = 0.25f;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.InvertEvent(GetContainer(eventA));
            eventA = Refresh(eventA);

            Assert.AreEqual(-0.25f, eventA.CustomStep, 0.001f, "Ring zoom step Chroma value");
        }

        private static BaseEvent PlaceEvent(float time, EventTypeValue type, int value = 0)
        {
            return PlaceUtils.Place(new BaseEvent { JsonTime = time, Type = (int)type, Value = value });
        }

        private static EventContainer GetContainer(BaseEvent beatmapEvent)
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            if (eventsContainer.LoadedContainers[beatmapEvent] is not EventContainer container)
                throw new Exception($"Wrong event container for type {beatmapEvent.Type}");
            return container;
        }

        private static void AssertTheSecondRingZoomScrolls(
            int initialValue,
            float? initialCustomStep,
            ScrollPrecision precisionValue,
            int modifier,
            int tickCount,
            int expectedValue,
            float? expectedCustomStep)
        {
            var eventA = PlaceEvent(5.375f, EventTypeValue.Event9, initialValue);
            eventA.CustomStep = initialCustomStep;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            var eventDefinition = GetContainer(eventA).TrackDefinitions.GetBasicOrDefault((int)EventTypeValue.Event9);
            var originalComponents = eventDefinition.Components;

            try
            {
                // These canonicalization regressions require The Second's smooth-step path rather than ordinary Event 9 behavior.
                eventDefinition.Components = BasicEventComponent.SmoothStepRingZoom;
                precision.CurrentPrecision = precisionValue;

                // Re-resolve the replaced beatmap object after every tweak so multi-tick boundary tests exercise the authored result of the prior tick.
                for (var i = 0; i < tickCount; i++)
                {
                    controller.TweakMain(GetContainer(eventA), modifier);
                    eventA = Refresh(eventA);
                }

                Assert.AreEqual(expectedValue, eventA.Value, "The Second serialized integer fallback");
                if (expectedCustomStep.HasValue)
                {
                    Assert.AreEqual(
                        expectedCustomStep.Value,
                        eventA.CustomStep ?? float.NaN,
                        0.0001f,
                        "The Second authored custom step");
                }
                else
                {
                    Assert.That(eventA.CustomStep, Is.Null, "The Second OEM integer must not retain a custom step");
                }

                Assert.AreEqual(
                    expectedCustomStep.HasValue,
                    eventA.CustomData.HasKey("step"),
                    "The Second serialized customData.step presence");
            }
            finally
            {
                // Restore shared environment metadata so this classification change cannot leak into later placement tests.
                eventDefinition.Components = originalComponents;
            }
        }

        private static TMPro.TextMeshPro GetEventValueDisplay(EventContainer container)
        {
            // Read the production label itself so this regression measures the font size rendered on the node.
            var field = typeof(EventContainer).GetField(
                "valueDisplay",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field, "EventContainer value display field");
            return (TMPro.TextMeshPro)field.GetValue(container);
        }

        private static BaseEvent Refresh(BaseEvent beatmapEvent)
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            return eventsContainer.MapObjects.First(
                x => Mathf.Approximately(x.JsonTime, beatmapEvent.JsonTime) && x.Type == beatmapEvent.Type);
        }

        private static void AssertEventWithChroma(
            BaseEvent baseline,
            BaseEvent actual,
            Action<BaseEvent> applyExpectedChanges,
            float expectedChromaValue,
            Func<BaseEvent, float?> getChromaValue,
            string message)
        {
            BeatmapAssertion.IsEqualWithChanges(baseline, actual, e =>
            {
                e.FloatValue = 1f;
                applyExpectedChanges(e);
            }, message);
            Assert.AreEqual(expectedChromaValue, getChromaValue(actual) ?? actual.Value, 0.001f, message + ": Chroma value");
        }
    }
}
