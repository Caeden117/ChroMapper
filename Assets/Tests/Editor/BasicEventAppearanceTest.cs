using System;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class BasicEventAppearanceTest
    {
        // Low propagation must not increase an unrelated speed above-one value from
        // hundredths to thousandths in the Basic Event ring-rotation label.
        [Test]
        public void RingRotationBelowTwoPropagationDisplaysThreePropagationAndTwoSpeedDecimals()
        {
            var evt = new BaseEvent
            {
                CustomProp = 1.3456f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P1.346{Environment.NewLine}S12.35", GetRingRotationText(evt));
        }

        // Speed below one needs thousandths regardless of the propagation value displayed
        // beside it, while propagation independently retains its own thousandths.
        [Test]
        public void RingRotationSpeedBelowOneDisplaysThreeDecimalsWithLowPropagation()
        {
            var evt = new BaseEvent
            {
                CustomProp = 2.3456f,
                CustomSpeed = 0.3456f
            };

            Assert.AreEqual($"P2.346{Environment.NewLine}S0.346", GetRingRotationText(evt));
        }

        // High propagation must not alter the normal hundredths used by speed values at
        // or above one, proving speed precision is not selected from propagation.
        [Test]
        public void RingRotationAboveThreePropagationDisplaysThreePropagationAndTwoSpeedDecimals()
        {
            var evt = new BaseEvent
            {
                CustomProp = 123.4564f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P123.456{Environment.NewLine}S12.35", GetRingRotationText(evt));
        }

        // A sub-one speed must keep thousandths even with high propagation, guarding the
        // opposite independence direction from the low-propagation regression.
        [Test]
        public void RingRotationSpeedBelowOneDisplaysThreeDecimalsWithHighPropagation()
        {
            var evt = new BaseEvent
            {
                CustomProp = 123.4564f,
                CustomSpeed = 0.3456f
            };

            Assert.AreEqual($"P123.456{Environment.NewLine}S0.346", GetRingRotationText(evt));
        }

        // Basic Event ring zoom retains thousandths for both Chroma step data and SmoothStep's integer fallback path.
        [TestCase(false)]
        [TestCase(true)]
        public void RingZoomDisplaysThreeStepDecimals(bool isSmoothStep)
        {
            var evt = new BaseEvent
            {
                CustomStep = 2.3456f
            };

            Assert.AreEqual("Z2.346", GetRingZoomText(evt, isSmoothStep));
        }

        // Basic Event ring zoom needs thousandths below speed one so its fine speed
        // precision is visible instead of being rounded to the normal hundredths.
        [Test]
        public void RingZoomSpeedBelowOneDisplaysThreeDecimals()
        {
            var evt = new BaseEvent
            {
                CustomSpeed = 0.3456f
            };

            Assert.AreEqual("S0.346", GetRingZoomText(evt, false));
        }

        // A legacy V2 Chroma gradient is authored on an On source event rather than a vanilla transition destination,
        // so its node must not claim to be a T node even though its independently-authored ribbon remains visible.
        [Test]
        public void LegacyChromaGradientOnNodeDoesNotDisplayVanillaTransitionMarker()
        {
            var displayFloatValueText = Settings.Instance.DisplayFloatValueText;
            Settings.Instance.DisplayFloatValueText = true;
            try
            {
                var evt = new BaseEvent
                {
                    Value = (int)LightValue.BlueOn,
                    FloatValue = 1f,
                    CustomLightGradient = new ChromaLightGradient(
                        new Color(0.298f, 1f, 0.584f, 0f),
                        new Color(0.298f, 1f, 0.584f, 1f),
                        0.25f,
                        "easeLinear")
                };

                Assert.AreEqual(string.Empty, GetLightValueText(evt));
            }
            finally
            {
                Settings.Instance.DisplayFloatValueText = displayFloatValueText;
            }
        }

        private static string GetRingRotationText(BaseEvent evt)
        {
            // Invoke the production Basic Event formatter so no GLS appearance path participates in this regression.
            var method = typeof(EventAppearanceSO).GetMethod(
                "GetRingRotationText",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Basic Event ring rotation appearance formatter");
            return (string)method.Invoke(null, new object[] { evt });
        }

        private static string GetRingZoomText(BaseEvent evt, bool isSmoothStep)
        {
            // Invoke the production Basic Event zoom formatter so its two supported paths share this regression.
            var method = typeof(EventAppearanceSO).GetMethod(
                "GetRingZoomText",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Basic Event ring zoom appearance formatter");
            return (string)method.Invoke(null, new object[] { evt, isSmoothStep });
        }

        // LegacyChromaGradientOnNodeDoesNotDisplayVanillaTransitionMarker invokes the production light label formatter so a
        // passing test proves the rendered node text changed, not a parallel test-only classification helper.
        private static string GetLightValueText(BaseEvent evt)
        {
            var method = typeof(EventAppearanceSO).GetMethod(
                "GetLightValueText",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Basic Event light value appearance formatter");
            return (string)method.Invoke(null, new object[] { evt });
        }
    }
}
