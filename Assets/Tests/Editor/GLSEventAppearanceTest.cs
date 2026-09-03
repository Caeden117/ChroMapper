using Beatmap.Base;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSEventAppearanceTest
    {
        // Keep normal bright GLS nodes from rendering a dark strobe band merely because strobe brightness defaults to zero.
        [Test]
        public void BrightNonStrobingColorNodeDoesNotEnableStrobeBand()
        {
            var evt = new BaseLightColorBase
            {
                Brightness = 3.7f,
                Frequency = 0,
                StrobeBrightness = 0f,
                StrobeFade = 0
            };

            Assert.False(GLSEventCommon.IsStrobing(evt));
        }

        // Retain both OEM and Chroma timing forms as valid strobe-band triggers.
        [TestCase(1, null)]
        [TestCase(0, 1f)]
        public void TimedColorNodeEnablesStrobeBand(int frequency, float? chromaInterval)
        {
            var evt = new BaseLightColorBase
            {
                Frequency = frequency,
                ChromaStrobeInterval = chromaInterval
            };

            Assert.True(GLSEventCommon.IsStrobing(evt));
        }

        // CustomDataColorAlphaMultipliesBrightnessWithoutScalingHdrRgb protects basic-event RGBA parity for the normal GLS phase.
        [Test]
        public void CustomDataColorAlphaMultipliesBrightnessWithoutScalingHdrRgb()
        {
            var tween = CreateTween(
                new Color(4f, 0.25f, 2f, 3f),
                new Color(4f, 0.25f, 2f, 3f),
                startBrightness: 2f,
                endBrightness: 2f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 4f, 0.25f, 2f, 6f);
        }

        // ExplicitStrobeColorAlphaMultipliesSbWithoutScalingHdrRgb catches the regression where preview replaces authored alpha with sb.
        [Test]
        public void ExplicitStrobeColorAlphaMultipliesSbWithoutScalingHdrRgb()
        {
            var tween = CreateTween(
                new Color(0.5f, 0.25f, 0.125f, 1f),
                new Color(0.5f, 0.25f, 0.125f, 1f),
                startBrightness: 1f,
                endBrightness: 1f,
                startStrobeColor: new Color(4f, 0.25f, 2f, 3f),
                endStrobeColor: new Color(4f, 0.25f, 2f, 3f),
                startStrobeBrightness: 2f,
                endStrobeBrightness: 2f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 4f, 0.25f, 2f, 6f);
        }

        // InheritedStrobeColorComposesEndpointAlphaBeforeTweening covers omitted strobeColor and crossed HDR alpha/sb transitions.
        [Test]
        public void InheritedStrobeColorComposesEndpointAlphaBeforeTweening()
        {
            var tween = CreateTween(
                new Color(4f, 0.25f, 2f, 2f),
                new Color(0.5f, 3f, 0.125f, 4f),
                startBrightness: 1f,
                endBrightness: 1f,
                startStrobeBrightness: 3f,
                endStrobeBrightness: 5f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 1.375f, 2.3125f, 0.59375f, 16.5f);
        }

        // Tests construct one deterministic cycle so 0.75 beats always selects the hard strobe-on phase.
        private static LightColorTween CreateTween(
            Color startColor,
            Color endColor,
            float startBrightness,
            float endBrightness,
            Color startStrobeColor = default,
            Color endStrobeColor = default,
            float startStrobeBrightness = 0f,
            float endStrobeBrightness = 0f)
        {
            return new LightColorTween
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                StartColor = startColor,
                StartAlpha = startBrightness,
                StartStrobeFrequency = startStrobeBrightness > 0f ? 1f : 0f,
                StartStrobeBrightness = startStrobeBrightness,
                StartStrobeColor = startStrobeColor,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                EndColor = endColor,
                EndAlpha = endBrightness,
                EndStrobeFrequency = endStrobeBrightness > 0f ? 1f : 0f,
                EndStrobeBrightness = endStrobeBrightness,
                EndStrobeColor = endStrobeColor,
                ColorLerpType = BasicEventColorLerpType.RGB
            };
        }

        // Component assertions prove HDR channels remain independent instead of hiding a mismatch behind Color equality tolerances.
        private static void AssertColor(Color actual, float red, float green, float blue, float alpha)
        {
            Assert.That(actual.r, Is.EqualTo(red).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(green).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(blue).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(alpha).Within(0.0001f));
        }
    }
}
