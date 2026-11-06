using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSAffectFirstPlacementDefaultsTest : GLSExtensionTestBase
    {
        private FieldInfo preferenceField;
        private bool previousPreference;

        [SetUp]
        public void CaptureDefault()
        {
            preferenceField = typeof(Settings).GetField(GLSAffectFirstDefaultsTest.SettingKey);
            if (preferenceField != null)
            {
                previousPreference = (bool)preferenceField.GetValue(Settings.Instance);
            }
        }

        [TearDown]
        public void RestoreDefault()
        {
            if (preferenceField != null)
            {
                Settings.ApplyOptionByName(GLSAffectFirstDefaultsTest.SettingKey, previousPreference);
            }
        }

        [Test]
        public void OuterPlacementUsesTheCurrentPreferenceWithoutReenteringTrack(
            [Values] GLSAffectFirstDefaultsTest.Kind kind, [Values(false, true)] bool initial)
        {
            SetPreference(initial);
            var provider = groupProvider.IdToTracks[0].GetComponent<PlacementProvider>();
            var placementType = kind switch
            {
                GLSAffectFirstDefaultsTest.Kind.Color => typeof(GLSGroupColorPlacement),
                GLSAffectFirstDefaultsTest.Kind.Rotation => typeof(GLSGroupRotationPlacement),
                GLSAffectFirstDefaultsTest.Kind.Translation => typeof(GLSGroupTranslationPlacement),
                _ => typeof(GLSGroupFloatFXPlacement)
            };
            var placement = provider.Placements.Single(candidate => candidate.GetType() == placementType);
            placement.Initialize(provider);
            var first = Field<BaseEventBoxGroup>(placement, "QueuedData");
            first.JsonTime = 8f;
            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);
            Assert.That(GLSAffectFirstDefaultsTest.GetAffectFirst(first.ReadOnlyBoxes[0]), Is.EqualTo(initial ? 1 : 0));
            SetPreference(!initial);
            var second = Field<BaseEventBoxGroup>(placement, "QueuedData");
            second.JsonTime = 16f;
            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);
            Assert.That(GLSAffectFirstDefaultsTest.GetAffectFirst(second.ReadOnlyBoxes[0]),
                Is.EqualTo(initial ? 0 : 1), "A reused placement queue must take the new preference without another hover entry.");
        }

        [Test]
        public void MaterializingPhantomLaneUsesTheCurrentPreference(
            [Values(false, true)] bool translation, [Values(false, true)] bool enabled)
        {
            SetPreference(!enabled);
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup { ID = 0, JsonTime = 8 }
                : new BaseLightRotationEventBoxGroup { ID = 0, JsonTime = 8 };
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            Assert.That(provider.TryGetDisplayedBox(0, out var phantom), Is.True);
            SetPreference(enabled);
            var materialized = GLSEventBoxCommand.MaterializeAutomaticLane(group, phantom);
            Assert.That(GLSAffectFirstDefaultsTest.GetAffectFirst(materialized.ReadOnlyBoxes[0]), Is.EqualTo(enabled ? 1 : 0));
            provider.GroupContext = null;
        }

        private void SetPreference(bool value)
        {
            Assert.That(preferenceField, Is.Not.Null, "The GLS Affect First preference must exist.");
            Settings.ApplyOptionByName(GLSAffectFirstDefaultsTest.SettingKey, value);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreferenceRefreshesPhantomOptionsWithoutChangingSavedBoxes(bool translation)
        {
            SetPreference(false);
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup { ID = 0, JsonTime = 8 }
                : new BaseLightRotationEventBoxGroup { ID = 0, JsonTime = 8 };
            var transform = (ILightTransformEventBoxGroup)group;
            var savedBox = transform.CreateTransformBox((int)Axis.X);
            transform.TryAddTransformBox(savedBox);
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            var view = Object.FindAnyObjectByType<EventBoxViewController>();
            Field<List<ToggleComponent>>(view, "instantiatedIdTab")[1].Value = true;
            SetPreference(true);
            Assert.That(provider.TryGetDisplayedBox(1, out var phantom), Is.True);
            Assert.That(((BaseLightTransformEventBox)phantom).AffectFirst, Is.EqualTo(1));
            var toggle = Field<ToggleComponent>(view, "affectFirstToggle");
            Assert.That(toggle.Value, Is.True);
            Assert.That(GLSAffectFirstDefaultsTest.GetAffectFirst(savedBox), Is.EqualTo(0));
            toggle.Value = false;
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(2));
            Assert.That(((BaseLightTransformEventBox)provider.GroupContext.ReadOnlyBoxes.Single(box => box.GetAxis() == Axis.Y)).AffectFirst,
                Is.EqualTo(0), "Changing a phantom option must create the explicitly configured saved box.");
            provider.GroupContext = null;
        }

        [Test]
        public void DuplicateAndSplitKeepTheSourceValueDespiteThePreference(
            [Values] GLSAffectFirstDefaultsTest.Kind kind, [Values(0, 1)] int value, [Values(false, true)] bool split)
        {
            SetPreference(value == 0);
            var box = GLSAffectFirstDefaultsTest.CreateBox(kind);
            GLSAffectFirstDefaultsTest.SetAffectFirst(box, value);
            box.IndexFilter.Param0 = 2;
            BaseEventBoxGroup group = box switch
            {
                BaseLightColorEventBox color => new BaseLightColorEventBoxGroup { Boxes = new() { color } },
                BaseLightRotationEventBox rotation => new BaseLightRotationEventBoxGroup { Boxes = new() { rotation } },
                BaseLightTranslationEventBox translation => new BaseLightTranslationEventBoxGroup { Boxes = new() { translation } },
                BaseVfxEventEventBox fx => new BaseVfxEventEventBoxGroup { Boxes = new() { fx } },
                _ => throw new ArgumentException(nameof(kind))
            };
            group.ID = 0;
            group.JsonTime = 8f;
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var result = split
                ? GLSEventBoxCommand.SplitSections(group, 0)
                : GLSEventBoxCommand.DuplicateEventBox(group, 0);
            Assert.That(result.ReadOnlyBoxes.Count, Is.EqualTo(2));
            foreach (var resultBox in result.ReadOnlyBoxes)
            {
                Assert.That(GLSAffectFirstDefaultsTest.GetAffectFirst(resultBox), Is.EqualTo(value));
            }
        }

        protected override void BeforeCleanup()
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = null;
        }
    }

    public class GLSAffectFirstDefaultsTest : TestBase
    {
        internal const string SettingKey = "DefaultGLSAffectFirst";
        private FieldInfo preferenceField;
        private bool previousPreference;
        private bool previousInstantTransitions;

        public enum Kind { Color, Rotation, Translation, FloatFX }
        public enum DistributionSetting { Value, TinyValue, Beat, TinyBeat, ValueType, BeatType, Easing, LimitDistribution }
        public enum ColorPayload { ColorDistribution, StrobeDistribution, ZeroDistribution, UnknownData }
        public enum NodeColorPayload { ColorDistribution, StrobeDistribution, Color, StrobeColor, ColorEasing,
            StrobeEasing, StrobeColorEasing, Interval, PulseWidth, Lerp }

        [SetUp]
        public void CapturePreference()
        {
            preferenceField = typeof(Settings).GetField(SettingKey);
            if (preferenceField != null)
            {
                previousPreference = (bool)preferenceField.GetValue(Settings.Instance);
            }
            previousInstantTransitions = Settings.Instance.InstantEscapeMenuTransitions;
        }

        [UnityTearDown]
        public IEnumerator RestorePreferenceAndOptions()
        {
            var options = Object.FindAnyObjectByType<OptionsController>();
            if (options != null)
            {
                Settings.Instance.InstantEscapeMenuTransitions = true;
                options.Close();
                yield return new WaitUntil(() => !SceneManager.GetSceneByName("04_Options").isLoaded);
            }
            Settings.Instance.InstantEscapeMenuTransitions = previousInstantTransitions;
            if (preferenceField != null)
            {
                Settings.ApplyOptionByName(SettingKey, previousPreference);
            }
        }

        [Test]
        public void NewInstallDefaultsToAffectFirstOn()
        {
            Assert.That(preferenceField, Is.Not.Null, "The GLS Affect First preference must exist.");
            Assert.That(preferenceField.GetValue(new Settings()), Is.True);
        }

        [Test]
        public void NewBoxesUseThePreference([Values] Kind kind, [Values(false, true)] bool enabled)
        {
            SetPreference(enabled);
            Assert.That(GetAffectFirst(CreateBox(kind)), Is.EqualTo(enabled ? 1 : 0));
        }

        [Test]
        public void CloningPreservesTheSourceInsteadOfThePreference([Values] Kind kind, [Values(0, 1)] int value)
        {
            var source = CreateBox(kind);
            SetAffectFirst(source, value);
            SetPreference(value == 0);
            Assert.That(GetAffectFirst(BeatmapFactory.Clone(source)), Is.EqualTo(value));
        }

        [Test]
        public void LoadedBoxesKeepStoredOrMissingValues(
            [Values] Kind kind, [Values(false, true)] bool v4, [Values(-1, 0, 1)] int storedValue)
        {
            SetPreference(storedValue != 1);
            var source = CreateBox(kind);
            SetAffectFirst(source, 0);
            var node = Serialize(source, v4);
            if (storedValue < 0)
            {
                node.Remove("b");
            }
            else
            {
                node["b"] = storedValue;
            }
            Assert.That(GetAffectFirst(Deserialize(kind, node, v4)), Is.EqualTo(Math.Max(storedValue, 0)));
        }

        [Test]
        public void PlainOnValueIsOmittedWithoutChangingTheEditableBox([Values] Kind kind, [Values(false, true)] bool v4)
        {
            var box = CreateBox(kind);
            SetAffectFirst(box, 1);
            Assert.That(Serialize(box, v4).HasKey("b"), Is.False,
                "A plain box with no distribution must not export an unnecessary on value.");
            Assert.That(GetAffectFirst(box), Is.EqualTo(1));
            Assert.That(GetAffectFirst(BeatmapFactory.Clone(box)), Is.EqualTo(1));
        }

        [Test]
        public void DistributionSettingsAlwaysKeepTheOnValue(
            [Values] Kind kind, [Values(false, true)] bool v4, [Values] DistributionSetting setting)
        {
            var box = CreateBox(kind);
            SetAffectFirst(box, 1);
            switch (setting)
            {
                case DistributionSetting.Value:
                    SetDistribution(box, 0.5f, (int)DistributionType.Wave);
                    break;
                case DistributionSetting.TinyValue:
                    SetDistribution(box, 0.00000001f, (int)DistributionType.Wave);
                    break;
                case DistributionSetting.Beat:
                    box.BeatDistribution = 1f;
                    break;
                case DistributionSetting.TinyBeat:
                    box.BeatDistribution = 0.00000001f;
                    break;
                case DistributionSetting.ValueType:
                    SetDistribution(box, 0f, (int)DistributionType.Step);
                    break;
                case DistributionSetting.BeatType:
                    box.BeatDistributionType = (int)DistributionType.Step;
                    break;
                case DistributionSetting.Easing:
                    box.Easing = (int)EaseType.InQuadratic;
                    break;
                case DistributionSetting.LimitDistribution:
                    box.IndexFilter.LimitAffectsType = (int)LimitAlsoAffectType.Distribution;
                    break;
            }
            Assert.That(Serialize(box, v4)["b"].AsInt, Is.EqualTo(1));
            Assert.That(GetAffectFirst(box), Is.EqualTo(1));
        }

        [Test]
        public void NodeCustomDataKeepsTheOnValue([Values] Kind kind, [Values(false, true)] bool v4)
        {
            var box = CreateBox(kind);
            SetAffectFirst(box, 1);
            AddCustomEvent(box, kind);
            Assert.That(Serialize(box, v4)["b"].AsInt, Is.EqualTo(1));
        }

        [Test]
        public void ColorDistributionPayloadsKeepTheOnValue([Values(false, true)] bool v4, [Values] ColorPayload payload)
        {
            var box = (BaseLightColorEventBox)CreateBox(Kind.Color);
            box.BrightnessAffectFirst = 1;
            switch (payload)
            {
                case ColorPayload.ColorDistribution:
                    box.ColorDistributions = new[] { "h,0.2,L" };
                    break;
                case ColorPayload.StrobeDistribution:
                    box.StrobeColorDistributions = new[] { "s,0.2,L" };
                    break;
                case ColorPayload.ZeroDistribution:
                    box.ColorDistributions = new[] { "h,0,L" };
                    break;
                case ColorPayload.UnknownData:
                    box.SetCustomData(new JSONObject { ["futureDistribution"] = 0.2f });
                    break;
            }
            Assert.That(Serialize(box, v4)["b"].AsInt, Is.EqualTo(1));
        }

        [Test]
        public void V4DeduplicationKeepsTheCustomDataDecision([Values] Kind kind)
        {
            var plain = CreateBox(kind);
            SetAffectFirst(plain, 1);
            var custom = BeatmapFactory.Clone(plain);
            AddCustomEvent(custom, kind);
            var records = new HashSet<object> { CommonRecord(plain), CommonRecord(custom) };
            Assert.That(records.Count, Is.EqualTo(2),
                "A V4 box that needs its on value must not share an omitted value's common record.");
            Assert.That(Serialize(plain, true).HasKey("b"), Is.False);
            Assert.That(Serialize(custom, true)["b"].AsInt, Is.EqualTo(1));
        }

        [Test]
        public void TypedNodeColorDataKeepsOnBeforeCustomJsonIsWritten(
            [Values(false, true)] bool v4, [Values] NodeColorPayload payload)
        {
            var box = new BaseLightColorEventBox { BrightnessAffectFirst = 1 };
            var evt = new BaseLightColorBase { CustomData = new JSONObject() };
            switch (payload)
            {
                case NodeColorPayload.ColorDistribution:
                    evt.ColorDistributions = new[] { "h,0.2,L" };
                    break;
                case NodeColorPayload.StrobeDistribution:
                    evt.StrobeColorDistributions = new[] { "h,0.2,L" };
                    break;
                case NodeColorPayload.Color:
                    evt.CustomColor = UnityEngine.Color.white;
                    break;
                case NodeColorPayload.StrobeColor:
                    evt.StrobeColor = UnityEngine.Color.white;
                    break;
                case NodeColorPayload.ColorEasing:
                    evt.ChromaColorEasing = (int)EaseType.InQuadratic;
                    break;
                case NodeColorPayload.StrobeEasing:
                    evt.ChromaStrobeEasing = (int)EaseType.InQuadratic;
                    break;
                case NodeColorPayload.StrobeColorEasing:
                    evt.ChromaStrobeColorEasing = (int)EaseType.InQuadratic;
                    break;
                case NodeColorPayload.Interval:
                    evt.ChromaStrobeInterval = 0.1f;
                    break;
                case NodeColorPayload.PulseWidth:
                    evt.ChromaStrobePulseWidth = 0.25f;
                    break;
                case NodeColorPayload.Lerp:
                    evt.CustomLerpType = BasicEventColorLerpType.TrueHSV;
                    break;
            }
            box.Events = new[] { evt };
            Assert.That(Serialize(box, v4)["b"].AsInt, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MappingOptionIsLocalizedAndUpdatesThePreference()
        {
            SetPreference(true);
            OptionsController.ShowOptions();
            yield return new WaitUntil(() => SceneManager.GetSceneByName("04_Options").isLoaded);
            yield return null;
            var binders = Object.FindObjectsByType<SimpleSettingsBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var binder = binders.SingleOrDefault(candidate => candidate.BindedSetting == SettingKey);
            Assert.That(binder, Is.Not.Null, "Mapping options must expose the Affect First default.");
            var ghostPreview = binders.Single(candidate => candidate.BindedSetting == "EnableGLSGhostPreview");
            Assert.That(binder.transform.parent, Is.SameAs(ghostPreview.transform.parent));
            Assert.That(binder.transform.GetSiblingIndex(), Is.EqualTo(ghostPreview.transform.GetSiblingIndex() + 1));
            var localized = binder.GetComponentsInChildren<LocalizeStringEvent>(true);
            Assert.That(localized.Any(label => label.StringReference.GetLocalizedString() == "\"Affect First\" default to on?"), Is.True);
            var tooltip = binder.GetComponent<Tooltip>();
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(tooltip.enabled, Is.True);
            Assert.That(tooltip.LocalizedTooltip.GetLocalizedString(), Is.EqualTo(
                "By default new event boxes will have Affect First defaulted to on, when this is enabled. Disable this if you prefer the Original Editor behavior. This value will not be serialized in your json unless the event box has some distribution related settings."));
            binder.SendValueToSettings(false);
            Assert.That(preferenceField.GetValue(Settings.Instance), Is.False);
            binder.SendValueToSettings(true);
            Assert.That(preferenceField.GetValue(Settings.Instance), Is.True);
        }

        private void SetPreference(bool value)
        {
            Assert.That(preferenceField, Is.Not.Null, "The GLS Affect First preference must exist.");
            Settings.ApplyOptionByName(SettingKey, value);
        }

        internal static BaseEventBox CreateBox(Kind kind) => kind switch
        {
            Kind.Color => new BaseLightColorEventBox(),
            Kind.Rotation => new BaseLightRotationEventBox(),
            Kind.Translation => new BaseLightTranslationEventBox(),
            _ => new BaseVfxEventEventBox()
        };

        internal static int GetAffectFirst(BaseEventBox box) => box switch
        {
            BaseLightColorEventBox color => color.BrightnessAffectFirst,
            BaseLightTransformEventBox transform => transform.AffectFirst,
            BaseVfxEventEventBox fx => fx.VfxAffectFirst,
            _ => throw new ArgumentException(nameof(box))
        };

        internal static void SetAffectFirst(BaseEventBox box, int value)
        {
            switch (box)
            {
                case BaseLightColorEventBox color:
                    color.BrightnessAffectFirst = value;
                    break;
                case BaseLightTransformEventBox transform:
                    transform.AffectFirst = value;
                    break;
                case BaseVfxEventEventBox fx:
                    fx.VfxAffectFirst = value;
                    break;
            }
        }

        private static void SetDistribution(BaseEventBox box, float value, int type)
        {
            switch (box)
            {
                case BaseLightColorEventBox color:
                    color.BrightnessDistribution = value;
                    color.BrightnessDistributionType = type;
                    break;
                case BaseLightTransformEventBox transform:
                    transform.ValueDistribution = value;
                    transform.ValueDistributionType = type;
                    break;
                case BaseVfxEventEventBox fx:
                    fx.VfxDistribution = value;
                    fx.VfxDistributionType = type;
                    break;
            }
        }

        private static void AddCustomEvent(BaseEventBox box, Kind kind)
        {
            BaseGLSEvent evt = kind switch
            {
                Kind.Color => new BaseLightColorBase(),
                Kind.Rotation => new BaseLightRotationBase(),
                Kind.Translation => new BaseLightTranslationBase(),
                _ => new BaseFxEventFloat()
            };
            evt.CustomData = new JSONObject { ["futureDistribution"] = 0.2f };
            box.SetEvents(new[] { evt });
        }

        private static object CommonRecord(BaseEventBox box) => box switch
        {
            BaseLightColorEventBox color => V4CommonData.LightColorEventBox.FromBaseLightColorEventBox(color),
            BaseLightRotationEventBox rotation => V4CommonData.LightRotationEventBox.FromBaseLightRotationEventBox(rotation),
            BaseLightTranslationEventBox translation => V4CommonData.LightTranslationEventBox.FromBaseLightTranslationEventBox(translation),
            BaseVfxEventEventBox fx => V4CommonData.FxEventBox.FromBaseFxEventBox(fx),
            _ => throw new ArgumentException(nameof(box))
        };

        private static JSONNode Serialize(BaseEventBox box, bool v4)
        {
            if (!v4)
            {
                return box.ToJson();
            }
            return CommonRecord(box) switch
            {
                V4CommonData.LightColorEventBox color => color.ToJson(),
                V4CommonData.LightRotationEventBox rotation => rotation.ToJson(),
                V4CommonData.LightTranslationEventBox translation => translation.ToJson(),
                V4CommonData.FxEventBox fx => fx.ToJson(),
                _ => throw new ArgumentException(nameof(box))
            };
        }

        private static BaseEventBox Deserialize(Kind kind, JSONNode node, bool v4)
        {
            if (!v4)
            {
                return kind switch
                {
                    Kind.Color => V3LightColorEventBox.GetFromJson(node),
                    Kind.Rotation => V3LightRotationEventBox.GetFromJson(node),
                    Kind.Translation => V3LightTranslationEventBox.GetFromJson(node),
                    _ => V3VfxEventEventBox.GetFromJson(node, Array.Empty<BaseFxEventFloat>())
                };
            }
            var group = JSON.Parse("{\"b\":16,\"g\":0,\"e\":[{\"f\":0,\"e\":0,\"l\":[]}]}");
            var filters = new[] { new BaseIndexFilter() };
            BaseEventBoxGroup parsed = kind switch
            {
                Kind.Color => V4LightColorEventBoxGroup.GetFromJson(group, filters,
                    new[] { V4CommonData.LightColorEventBox.GetFromJson(node) }, Array.Empty<V4CommonData.LightColorEvent>()),
                Kind.Rotation => V4LightRotationEventBoxGroup.GetFromJson(group, filters,
                    new[] { V4CommonData.LightRotationEventBox.GetFromJson(node) }, Array.Empty<V4CommonData.LightRotationEvent>()),
                Kind.Translation => V4LightTranslationEventBoxGroup.GetFromJson(group, filters,
                    new[] { V4CommonData.LightTranslationEventBox.GetFromJson(node) }, Array.Empty<V4CommonData.LightTranslationEvent>()),
                _ => V4VfxEventEventBoxGroup.GetFromJson(group, filters,
                    new[] { V4CommonData.FxEventBox.GetFromJson(node) }, Array.Empty<V4CommonData.FloatFxEvent>())
            };
            return parsed.ReadOnlyBoxes[0];
        }
    }
}
