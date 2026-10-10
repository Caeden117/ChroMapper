using System;
using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class BasicLightAdapterAlphaParityTest : TestBase
    {
        private GameObject probeObject;
        private LightController probe;
        private BasicLightEffect effect;
        private bool previousChromaLite;

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(3, null, beatsPerMinute: 150,
                environmentName: "BillieEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [SetUp]
        public void EnableCustomColors()
        {
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            Settings.Instance.EmulateChromaLite = true;
        }

        // The shipped Billie switches and Chroma dispatch .7490196 normal, 1 highlight,
        // and .8 boosted normal to every ILightWithId, before each adapter's own multiplier.
        [TestCase(typeof(MaterialLightController))]
        [TestCase(typeof(InstancedMaterialLightController))]
        [TestCase(typeof(DirectionalLightController))]
        [TestCase(typeof(SpriteLightController))]
        [TestCase(typeof(SpriteArrayLightController))]
        [TestCase(typeof(ParticleSystemLightController))]
        [TestCase(typeof(RectangleFakeGlowLightController))]
        [TestCase(typeof(BloomPrePassBackgroundColorsGradientTintColorLightController))]
        [TestCase(typeof(ColorArrayData))]
        [TestCase(typeof(LightIntensityData))]
        [TestCase(typeof(LightmapIntensityData))]
        [TestCase(typeof(LightmapsIntensityData))]
        [TestCase(typeof(EnableRendererLightController))]
        public void EveryAdapterReceivesNativeEndpointsAcrossSeeks(Type adapterType)
        {
            CreateProbe(adapterType);
            foreach (var (beat, value) in new (float Beat, LightValue Value)[]
            {
                (0, LightValue.RedOn), (4, LightValue.RedFlash), (8, LightValue.RedFade),
                (12, LightValue.Off), (16, LightValue.WhiteOn), (21, LightValue.RedOn),
                (24, LightValue.RedFlash)
            })
            {
                PlaceUtils.Place(new BaseEvent
                {
                    JsonTime = beat, Type = probe.Type, Value = (int)value, FloatValue = 2,
                    CustomColor = value == LightValue.WhiteOn ? null : new Color(.2f, .4f, 1, .5f)
                });
            }

            PlaceUtils.Place(new BaseEvent { JsonTime = 20, Type = 5, Value = 1 });
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var flashEase = 1 - Mathf.Pow(1 - .5f / 1.5f, 3);
            foreach (var (beat, expectedAlpha) in new (float Beat, float Alpha)[]
            {
                (1, .7490196f), (4, 1), (4.5f, Mathf.Lerp(1, .7490196f, flashEase)),
                (6, .7490196f), (8, 1), (9, Mathf.Pow(2, -10 / 3.75f)), (12, 0),
                (16, 2), (21, .8f), (24, 1), (24.5f, Mathf.Lerp(1, .8f, flashEase)),
                (26, .8f), (1, .7490196f), (4.5f, Mathf.Lerp(1, .7490196f, flashEase))
            })
            {
                clock.MoveToJsonTime(beat);
                if (probe is EnableRendererLightController visibility)
                {
                    Assert.That(visibility.Renderer.enabled,
                        Is.EqualTo(expectedAlpha >= .9f && expectedAlpha <= 1.05f),
                        $"{adapterType.Name} visibility at beat {beat} used the wrong input alpha.");
                }
                else
                {
                    Assert.That(ReadSourceAlpha(), Is.EqualTo(expectedAlpha).Within(.0001f),
                        $"{adapterType.Name} at beat {beat} differs from native ColorSO dispatch.");
                }
            }
        }

        [Test]
        public void InstancedColorOnlyRetainsItsFirstAlpha()
        {
            CreateProbe(typeof(InstancedMaterialLightController), true);
            probe.SetColor(new Color(.6f, .7f, .8f, .9f));
            Assert.That(ReadPropertyColor().a, Is.EqualTo(.35f).Within(.0001f),
                "Native InstancedMaterialLightWithId keeps the first alpha when setColorOnly is enabled.");
            probe.SetColor(new Color(.8f, .7f, .6f, .1f));
            Assert.That(ReadPropertyColor().a, Is.EqualTo(.35f).Within(.0001f));
        }

        private void CreateProbe(Type adapterType, bool colorOnly = false)
        {
            probeObject = new GameObject("Native adapter alpha probe");
            probeObject.SetActive(false);
            probe = (LightController)probeObject.AddComponent(adapterType);
            probe.Type = (int)EventTypeValue.Event2;
            probe.ID = -1;
            switch (probe)
            {
                case MaterialLightController material:
                    material.Renderer = probeObject.AddComponent<MeshRenderer>();
                    material.Property = "_Color";
                    material.AlphaIntensity = 2;
                    break;
                case InstancedMaterialLightController instanced:
                    instanced.MpbColorSetter = probeObject.AddComponent<MaterialPropertyBlockColorSetter>();
                    instanced.MpbColorSetter.Property = "_Color";
                    instanced.MpbColorSetter.AlphaProperty = "_Alpha";
                    instanced.MpbColorSetter.Controller = probeObject.AddComponent<MaterialPropertyBlockController>();
                    instanced.Intensity = 2;
                    instanced.SetColorOnly = colorOnly;
                    instanced.Color = new Color(.2f, .3f, .4f, .35f);
                    break;
                case DirectionalLightController directional:
                    directional.Light = probeObject.AddComponent<DirectionalLight>();
                    directional.Intensity = 2;
                    break;
                case SpriteLightController sprite:
                    sprite.Renderer = probeObject.AddComponent<SpriteRenderer>();
                    sprite.Intensity = 2;
                    break;
                case SpriteArrayLightController sprites:
                    sprites.SpriteRenderers = new[] { probeObject.AddComponent<SpriteRenderer>() };
                    sprites.Intensity = 2;
                    break;
                case ParticleSystemLightController particles:
                    particles.ParticleSystem = probeObject.AddComponent<ParticleSystem>();
                    particles.Intensity = 2;
                    break;
                case RectangleFakeGlowLightController rectangle:
                    rectangle.MpbController = probeObject.AddComponent<MaterialPropertyBlockController>();
                    rectangle.AlphaMultiplier = 2;
                    break;
                case BloomPrePassBackgroundColorsGradientTintColorLightController gradient:
                    gradient.BloomPrePassBackgroundColorsGradient =
                        probeObject.AddComponent<BloomPrePassBackgroundColorsGradient>();
                    gradient.BloomPrePassBackgroundColorsGradient.enabled = false;
                    break;
                case EnableRendererLightController visibility:
                    visibility.Renderer = probeObject.AddComponent<MeshRenderer>();
                    visibility.HideAlphaRangeMin = .9f;
                    visibility.HideAlphaRangeMax = 1.05f;
                    break;
            }

            probeObject.SetActive(true);
            probe.Start();
            effect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(probe.Type);
            effect.Register(probe);
            effect.Initialize();
        }

        private Color ReadPropertyColor()
        {
            if (probe is InstancedMaterialLightController instanced)
                return instanced.MpbColorSetter.Controller.Mpb.GetColor("_Color");

            if (probe is RectangleFakeGlowLightController rectangle)
                return rectangle.MpbController.Mpb.GetColor("_Color");

            var block = new MaterialPropertyBlock();
            ((MaterialLightController)probe).Renderer.GetPropertyBlock(block);
            return block.GetColor("_Color");
        }

        private float ReadSourceAlpha()
        {
            switch (probe)
            {
                case MaterialLightController:
                case InstancedMaterialLightController:
                case RectangleFakeGlowLightController:
                    return ReadPropertyColor().a / 2;
                case DirectionalLightController directional:
                    return directional.Light.Intensity / 2;
                case SpriteLightController sprite:
                    return sprite.Renderer.color.a / 2;
                case SpriteArrayLightController sprites:
                    return sprites.SpriteRenderers[0].color.a / 2;
                case ParticleSystemLightController particles:
                    return particles.ParticleSystem.main.startColor.color.a / 2;
                case BloomPrePassBackgroundColorsGradientTintColorLightController gradient:
                    return gradient.BloomPrePassBackgroundColorsGradient.TintColor.a;
                default:
                    return probe.Color.a;
            }
        }

        protected override void BeforeCleanup()
        {
            Settings.Instance.EmulateChromaLite = previousChromaLite;
            if (probe != null && effect != null)
                effect.Unregister(probe);

            if (probeObject != null)
                Object.DestroyImmediate(probeObject);

            probeObject = null;
            probe = null;
            effect = null;
        }
    }
}
