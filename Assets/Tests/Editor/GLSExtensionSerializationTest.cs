using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using Kind = Tests.Editor.GLSExtensionEasingPlacementTest.Kind;

namespace Tests.Editor
{
    public class GLSExtensionSerializationTest
    {
        [Test]
        public void SharedNodeSettingsSurviveCloneAndApply([Values] Kind kind, [Values] bool extension)
        {
            var original = CreateEvent(kind, extension, 2);
            var clone = (BaseGLSEvent)original.Clone();
            Assert.That(clone.Easing, Is.EqualTo(original.Easing));
            Assert.That(clone.UsePrevious, Is.EqualTo(original.UsePrevious));

            var applied = CreateEvent(kind, !extension, 0);
            applied.Easing = (int)EaseType.OutBounce;
            applied.Apply(clone);
            Assert.That(applied.Easing, Is.EqualTo(original.Easing));
            Assert.That(applied.UsePrevious, Is.EqualTo(original.UsePrevious));

            clone.Easing = (int)EaseType.InCubic;
            clone.UsePrevious = extension ? 0 : 1;
            Assert.That(original.Easing, Is.EqualTo((int)EaseType.InQuadratic));
            Assert.That(original.UsePrevious, Is.EqualTo(extension ? 1 : 0));
        }

        [TestCase(-0.2f, 0f, false)]
        [TestCase(0f, 0f, false)]
        [TestCase(0.25f, 0.25f, false)]
        [TestCase(0.5f, 0.5f, false)]
        [TestCase(1f, 1f, false)]
        [TestCase(1.2f, 1f, false)]
        [TestCase(-0.2f, 0f, true)]
        [TestCase(0f, 0f, true)]
        [TestCase(0.25f, 0.25f, true)]
        [TestCase(0.5f, 0.5f, true)]
        [TestCase(1f, 1f, true)]
        [TestCase(1.2f, 1f, true)]
        public void StrobePulseWidthRoundTripsAndNormalizes(float authored, float expected, bool v4)
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse("{\"f\":1,\"customData\":{}}"));
            evt.CustomData = new JSONObject { ["strobePulseWidth"] = authored };
            Assert.That(evt.IsChroma(), Is.True, "strobePulseWidth alone must enable ChromaGLS metadata.");
            evt = (BaseLightColorBase)evt.Clone();
            evt.WriteCustom();
            Serialize(evt, v4, out var custom);
            Assert.That(custom.HasKey("strobePulseWidth"), Is.EqualTo(expected != 0.5f));
            if (expected != 0.5f)
                Assert.That(custom["strobePulseWidth"].AsFloat, Is.EqualTo(expected));
        }

        [TestCase(false, "0")]
        [TestCase(true, "0")]
        [TestCase(false, "0.0")]
        [TestCase(true, "0.0")]
        public void ExplicitLinearColorEasingSurvivesParseCloneApplyAndExport(bool v4, string value)
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"i\":1,\"s\":1,\"customData\":{\"easing\":7,\"colorEasing\":" + value + "}}"));
            Assert.That(evt.ChromaColorEasing, Is.EqualTo((int)EaseType.Linear),
                "Explicit colorEasing=0 is a Linear override, not an absent curve.");
            var clone = (BaseLightColorBase)evt.Clone();
            var applied = new BaseLightColorBase();
            applied.Apply(clone);
            applied.WriteCustom();
            Assert.That(applied.ChromaColorEasing, Is.EqualTo((int)EaseType.Linear));
            Assert.That(applied.Easing, Is.EqualTo((int)EaseType.InCubic));
            Serialize(applied, v4, out var customData);
            Assert.That(customData.HasKey("colorEasing"), Is.True);
            Assert.That(customData["colorEasing"].AsInt, Is.EqualTo((int)EaseType.Linear));
        }

        // Rotation easing must round-trip even with zero loops; the other families' extension easings do no work.
        // Check emitted JSON for both formats, with normal-node controls, without inspecting implementation text.
        [Test]
        public void ExportKeepsOnlyMeaningfulExtensionEasing(
            [Values] Kind kind,
            [Values] bool extension,
            [Values] bool v4,
            [Values(0, 2)] int loops)
        {
            var evt = CreateEvent(kind, extension, loops);
            var node = Serialize(evt, v4, out var colorCustomData);
            var keepsEasing = !extension || kind == Kind.Rotation;
            var key = v4 || kind == Kind.Rotation || kind == Kind.Translation ? "e" : "i";
            if (kind != Kind.Color || v4)
            {
                Assert.That(node.HasKey(key), Is.EqualTo(keepsEasing));
                if (keepsEasing)
                    Assert.That(node[key].AsInt, Is.EqualTo((int)EaseType.InQuadratic));
            }
            if (evt is BaseLightColorBase color)
            {
                Assert.That(colorCustomData.HasKey("easing"), Is.EqualTo(!v4 && !extension));
                foreach (var easingKey in new[] { "colorEasing", "strobeEasing", "strobeColorEasing" })
                    Assert.That(colorCustomData.HasKey(easingKey), Is.EqualTo(!extension), easingKey);
                Assert.That(colorCustomData["keep"].Value, Is.EqualTo("unrelated metadata"));
                // Export cleanup owns its JSON copy; an in-memory node and its original custom data stay editable.
                Assert.That(color.Easing, Is.EqualTo((int)EaseType.InQuadratic));
                Assert.That(color.ChromaColorEasing, Is.EqualTo((int)EaseType.InCubic));
                Assert.That(color.CustomData.HasKey("colorEasing"), Is.True);
            }
            if (evt is BaseLightRotationBase rotation)
            {
                Assert.That(node["l"].AsInt, Is.EqualTo(loops));
                Assert.That(rotation.Easing, Is.EqualTo((int)EaseType.InQuadratic));
            }
        }

        // Direct event writers are used by node editing and V3 export; V4 common data is the real deduplicated output.
        private static JSONNode Serialize(BaseGLSEvent evt, bool v4, out JSONNode colorCustomData)
        {
            colorCustomData = new JSONObject();
            if (!v4)
            {
                var node = evt switch
                {
                    BaseLightColorBase color => V3LightColorBase.ToJson(color),
                    BaseLightRotationBase rotation => V3LightRotationBase.ToJson(rotation),
                    BaseLightTranslationBase translation => V3LightTranslationBase.ToJson(translation),
                    BaseFxEventFloat fx => V3FloatFxEvent.ToJson(fx),
                    _ => throw new System.ArgumentException(nameof(evt))
                };
                if (evt is BaseLightColorBase)
                    colorCustomData = node["customData"];
                return node;
            }
            if (evt is BaseLightColorBase v4Color)
            {
                var box = new BaseLightColorEventBox { Events = new[] { v4Color } };
                var group = new BaseLightColorEventBoxGroup { Boxes = new() { box } };
                var data = V4CommonData.LightColorEvent.FromBaseLightColorEvent(v4Color);
                var json = V4LightColorEventBoxGroup.ToJson(
                    group,
                    new List<V4CommonData.IndexFilter> { V4CommonData.IndexFilter.FromBaseIndexFilter(box.IndexFilter) },
                    new List<V4CommonData.LightColorEventBox> { V4CommonData.LightColorEventBox.FromBaseLightColorEventBox(box) },
                    new List<V4CommonData.LightColorEvent> { data });
                colorCustomData = json["e"][0]["l"][0]["customData"];
                return data.ToJson();
            }
            return evt switch
            {
                BaseLightRotationBase rotation => V4CommonData.LightRotationEvent.FromBaseLightRotationEvent(rotation).ToJson(),
                BaseLightTranslationBase translation => V4CommonData.LightTranslationEvent.FromBaseLightTranslationEvent(translation).ToJson(),
                BaseFxEventFloat fx => V4CommonData.FloatFxEvent.FromFloatFxEventBase(fx).ToJson(),
                _ => throw new System.ArgumentException(nameof(evt))
            };
        }

        // Each fixture carries a nondefault native curve and color's independent curves so stripping cannot pass by default.
        private static BaseGLSEvent CreateEvent(Kind kind, bool extension, int loops)
        {
            var previous = extension ? 1 : 0;
            return kind switch
            {
                Kind.Color => new BaseLightColorBase
                {
                    // Assign raw custom data first: its parser otherwise replaces the independent easing fixture fields.
                    CustomData = JSON.Parse("{\"keep\":\"unrelated metadata\"}"),
                    UsePrevious = previous,
                    Easing = (int)EaseType.InQuadratic,
                    ChromaColorEasing = (int)EaseType.InCubic,
                    ChromaStrobeEasing = (int)EaseType.OutBounce,
                    ChromaStrobeColorEasing = (int)EaseType.InQuadratic,
                    StrobeFade = 1
                },
                Kind.Rotation => new BaseLightRotationBase
                {
                    UsePrevious = previous, Easing = (int)EaseType.InQuadratic, Loop = loops
                },
                Kind.Translation => new BaseLightTranslationBase
                {
                    UsePrevious = previous, Easing = (int)EaseType.InQuadratic
                },
                _ => new BaseFxEventFloat { UsePrevious = previous, Easing = (int)EaseType.InQuadratic }
            };
        }
    }
}
