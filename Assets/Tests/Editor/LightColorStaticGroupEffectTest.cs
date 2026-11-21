using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class LightColorStaticGroupEffectTest
    {
        private static readonly Color StaticTestColor = new(0.2f, 0.4f, 0.6f);
        private static readonly Color EnvWhite = new(0.9f, 0.8f, 0.7f);

        private sealed class TestableStaticEffect : LightColorStaticGroupEffect
        {
            public void RunUpdateObject(LightColorGroupContainer container) => UpdateObject(container);
            public void RunBoostChange(bool boost) => HandleBoostChange(boost);
        }

        private static TestableStaticEffect CreateEffect(
            out GameObject effectGo, out GameObject providerGo, out GameObject songGo, out GameObject boostGo)
        {
            // Reuse the mapper singleton; focused runs create their own fallback.
            songGo = null;
            if (BeatSaberSongContainer.Instance == null)
            {
                songGo = new GameObject("Static effect test song container");
                songGo.AddComponent<BeatSaberSongContainer>();
            }
            providerGo = new GameObject("Static effect test scheme provider");
            var provider = providerGo.AddComponent<ColorSchemeProvider>();
            var scheme = ScriptableObject.CreateInstance<ColorSchemeSO>();
            scheme.EnvironmentWhiteColor = EnvWhite;
            provider.ColorScheme = scheme;
            // OnDestroy unsubscribes HandleBoostChange from ColorBoostEffect; the component must exist and outlive the effect GameObject.
            boostGo = new GameObject("Static effect test boost");
            effectGo = new GameObject("Static effect test component");
            var effect = effectGo.AddComponent<TestableStaticEffect>();
            effect.StaticColor = StaticTestColor;
            effect.ColorSchemeProvider = provider;
            effect.ColorBoostEffect = boostGo.AddComponent<ColorBoostEffect>();
            return effect;
        }

        private static LightColorGroupContainer CreateContainer(
            BaseLightColorBase start,
            BaseLightColorBase end,
            BaseLightColorEventBox box = null,
            float chunkProgress = 0f,
            float lightProgress = 0f)
        {
            var startState = new LightColorEventStateData(start, 0f, 0f, box, chunkProgress, lightProgress)
            {
                EndTime = 1f
            };
            var endState = new LightColorEventStateData(end, 1f, 0f, box, chunkProgress, lightProgress);
            startState.Next = endState;
            endState.Previous = startState;
            var container = new LightColorGroupContainer();
            container.EventContainer.CurrentState = startState;
            return container;
        }

        private static Color HueShifted(Color color, float amount)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            var shifted = Color.HSVToRGB(Mathf.Repeat(h + amount, 1f), s, v, true);
            shifted.a = color.a;
            return shifted;
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-4f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-4f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-4f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1e-4f));
        }

        // Authored non-white event colors are replaced by the manager's fixed color on both endpoints.
        [Test]
        public void StaticColorSuppliesBothEndpointsForAuthoredNonWhiteEvents()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var start = new BaseLightColorBase { Color = (int)LightColor.Red, Brightness = 1f };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.Blue,
                    Brightness = 0.5f,
                    Easing = (int)EaseType.Linear
                };
                var container = CreateContainer(start, end);

                effect.RunUpdateObject(container);

                Assert.That(container.Tween.StartColor, Is.EqualTo(StaticTestColor));
                Assert.That(container.Tween.EndColor, Is.EqualTo(StaticTestColor));
                Assert.That(container.Tween.StartAlpha, Is.EqualTo(1f));
                Assert.That(container.Tween.EndAlpha, Is.EqualTo(0.5f));
                // No authored strobe timing collapses the strobe phase onto the static color.
                Assert.That(container.Tween.StartStrobeColor, Is.EqualTo(StaticTestColor));
                Assert.That(container.Tween.StartStrobeBrightness, Is.EqualTo(1f));
                Assert.That(container.Tween.Easing, Is.EqualTo(Easing.FromID((int)EaseType.Linear)));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // Authored White maps to the non-boost environment white instead of the fixed color.
        [Test]
        public void WhiteAuthoredEventsUseNonBoostEnvironmentWhite()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var start = new BaseLightColorBase { Color = (int)LightColor.White, Brightness = 1f };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.White,
                    Brightness = 0.5f,
                    Easing = (int)EaseType.Linear
                };
                var container = CreateContainer(start, end);

                effect.RunUpdateObject(container);

                Assert.That(container.Tween.StartColor, Is.EqualTo(EnvWhite));
                Assert.That(container.Tween.EndColor, Is.EqualTo(EnvWhite));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // An authored customData color outranks both the fixed color and environment white.
        [Test]
        public void CustomColorOverridesStaticAndEnvironmentWhite()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var start = new BaseLightColorBase
                {
                    Color = (int)LightColor.White,
                    Brightness = 1f,
                    CustomColor = Color.green
                };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Easing = (int)EaseType.Linear,
                    CustomColor = Color.blue
                };
                var container = CreateContainer(start, end);

                effect.RunUpdateObject(container);

                Assert.That(container.Tween.StartColor, Is.EqualTo(Color.green));
                Assert.That(container.Tween.EndColor, Is.EqualTo(Color.blue));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // Authored strobeColor wins while present and falls back to the resolved endpoint color when absent.
        [Test]
        public void StrobeColorOverrideAppliesWithStaticFallback()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var start = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Frequency = 2,
                    StrobeBrightness = 0.5f,
                    StrobeColor = Color.yellow
                };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Frequency = 4,
                    StrobeBrightness = 0.25f,
                    Easing = (int)EaseType.Linear
                };
                var container = CreateContainer(start, end);

                effect.RunUpdateObject(container);

                Assert.That(container.Tween.StartStrobeFrequency, Is.EqualTo(2f));
                Assert.That(container.Tween.StartStrobeColor, Is.EqualTo(Color.yellow));
                Assert.That(container.Tween.StartStrobeBrightness, Is.EqualTo(0.5f));
                Assert.That(container.Tween.EndStrobeFrequency, Is.EqualTo(4f));
                Assert.That(container.Tween.EndStrobeColor, Is.EqualTo(StaticTestColor));
                Assert.That(container.Tween.EndStrobeBrightness, Is.EqualTo(0.25f));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // Boost state changes must not retint the fixed-color path.
        [Test]
        public void BoostChangeLeavesStaticTweenUntouched()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var start = new BaseLightColorBase { Color = (int)LightColor.White, Brightness = 1f };
                var end = new BaseLightColorBase { Color = (int)LightColor.White, Brightness = 1f };
                var container = CreateContainer(start, end);
                effect.RunUpdateObject(container);
                var startColor = container.Tween.StartColor;
                var endColor = container.Tween.EndColor;

                effect.RunBoostChange(true);

                Assert.That(container.Tween.StartColor, Is.EqualTo(startColor));
                Assert.That(container.Tween.EndColor, Is.EqualTo(endColor));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // The manager's color overload creates the static effect once per group and returns the existing
        // registration on repeat calls.
        [Test]
        public void ManagerRegisterOverloadCreatesStaticEffectWithColor()
        {
            var managerGo = new GameObject("Static effect test manager");
            var manager = managerGo.AddComponent<LightColorGroupEffectManager>();
            // Register adds the effect component to managerGo; wire ColorBoostEffect so the manager's teardown OnDestroy unsubscribe resolves.
            var boostGo = new GameObject("Static effect test boost");
            try
            {
                var effect = manager.Register(3, 8, StaticTestColor);
                effect.ColorBoostEffect = boostGo.AddComponent<ColorBoostEffect>();

                Assert.That(effect, Is.TypeOf<LightColorStaticGroupEffect>());
                Assert.That(((LightColorStaticGroupEffect)effect).StaticColor, Is.EqualTo(StaticTestColor));
                Assert.That(effect.ID, Is.EqualTo(3));
                Assert.That(effect.Count, Is.EqualTo(8));
                Assert.AreSame(effect, manager.IdToEffect[3]);
                Assert.AreSame(effect, manager.Register(3, 8, Color.red),
                    "A second registration for the same group must return the existing effect.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // Authored event-level hue offsets rotate each endpoint's resolved static color.
        [Test]
        public void HueShiftAppliesToStaticNormalColors()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var start = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    CustomData = JSON.Parse("{\"colorDistributions\":[\"h,0.25,L\"]}")
                };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Easing = (int)EaseType.Linear,
                    CustomData = JSON.Parse("{\"colorDistributions\":[\"h,0.5,L\"]}")
                };
                var container = CreateContainer(start, end, chunkProgress: 1f);

                effect.RunUpdateObject(container);

                AssertColor(container.Tween.StartColor, HueShifted(StaticTestColor, 0.25f));
                AssertColor(container.Tween.EndColor, HueShifted(StaticTestColor, 0.5f));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // Strobe distributions apply on top of an authored strobeColor and fall back to the resolved static
        // color.
        [Test]
        public void HueShiftAppliesToStaticStrobeColors()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var startStrobe = new Color(0.2f, 0.1f, 0.4f);
                var start = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Frequency = 2,
                    StrobeBrightness = 0.5f,
                    CustomData = JSON.Parse(
                        "{\"strobeColor\":[0.2,0.1,0.4],\"strobeColorDistributions\":[\"h,0.4,L\"]}")
                };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Frequency = 4,
                    StrobeBrightness = 0.25f,
                    Easing = (int)EaseType.Linear,
                    CustomData = JSON.Parse("{\"strobeColorDistributions\":[\"h,0.5,L\"]}")
                };
                var container = CreateContainer(start, end, chunkProgress: 1f);

                effect.RunUpdateObject(container);

                AssertColor(container.Tween.StartStrobeColor, HueShifted(startStrobe, 0.4f));
                AssertColor(container.Tween.EndStrobeColor, HueShifted(StaticTestColor, 0.5f));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }

        // Box-level hue offsets apply to the static color and the l flag routes the offset through
        // AffectedLightProgress.
        [Test]
        public void BoxHueShiftAppliesToStaticColorsThroughAffectedLightProgress()
        {
            var effect = CreateEffect(out var effectGo, out var providerGo, out var songGo, out var boostGo);
            try
            {
                var box = new BaseLightColorEventBox();
                box.SetCustomData(JSON.Parse("{\"colorDistributions\":[\"h,0.5,L,l\"]}"));
                var start = new BaseLightColorBase { Color = (int)LightColor.Red, Brightness = 1f };
                var end = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    Easing = (int)EaseType.Linear
                };
                var container = CreateContainer(start, end, box, chunkProgress: 0f, lightProgress: 1f);

                effect.RunUpdateObject(container);

                AssertColor(container.Tween.StartColor, HueShifted(StaticTestColor, 0.5f));
                AssertColor(container.Tween.EndColor, HueShifted(StaticTestColor, 0.5f));
            }
            finally
            {
                Object.DestroyImmediate(songGo);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(effectGo);
                Object.DestroyImmediate(boostGo);
            }
        }
    }
}
