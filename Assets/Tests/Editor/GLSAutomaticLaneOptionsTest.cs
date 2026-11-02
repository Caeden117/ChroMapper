using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSAutomaticLaneOptionsTest : InputTestBase
    {
        private const int GroupId = 1000901;
        private GLSEventGridProvider provider;
        private EventBoxViewController view;
        private TrackDefinitionsSO definitions;
        private TrackDefinitionGLS previousDefinition;
        private bool hadDefinition;
        private GameObject inputEventsRoot;
        private DefaultInputActions uiInput;
        private readonly List<EventSystem> disabledEventSystems = new();
        private static readonly BoxKind[] SplitBoxKinds = { BoxKind.Color, BoxKind.Rotation, BoxKind.Translation, BoxKind.FloatFX };
        protected override EditingMode InitialEditingMode => EditingMode.EventBox;

        public enum BoxOption
        {
            BeatDistribution, BeatDistributionType, FilterType, SectionCount, SectionIndex,
            Chunks, Reverse, Random, KeepOrder, Seed, Limit, LimitDuration, LimitDistribution,
            Flip, ValueDistribution, ValueDistributionType, AffectFirst, Easing
        }

        public enum BoxKind { Color, Rotation, Translation, FloatFX }
        public enum UnavailableSplit { Default, Complete, StepFilter }

        [UnityTest]
        public IEnumerator SplitCanBeClickedDirectlyAfterTypingSections([ValueSource(nameof(SplitBoxKinds))] BoxKind kind)
        {
            yield return InitializeSplitUiInput();
            OpenSplitGroup(CreateSplitGroup(kind, 1, 0));
            var original = provider.GroupContext;
            var input = ReadField<TextBoxIntComponent>(view, "p0Input").GetComponentInChildren<TMP_InputField>(true);
            var split = SplitButton();
            Assert.That(split.Selectable.interactable, Is.False);
            yield return null;
            EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
            yield return null;
            Assert.That(input.isFocused, Is.True, "Sections must own focus before typing.");

            input.text = "4";

            Assert.That(input.isFocused, Is.True);
            Assert.That(provider.GroupContext, Is.SameAs(original), "Typing must keep the normal deferred commit behavior.");
            Assert.That(original.ReadOnlyBoxes[0].IndexFilter.Param0, Is.EqualTo(1));
            Assert.That(split.Selectable.interactable, Is.True, "Split must enable before Sections loses focus.");
            Canvas.ForceUpdateCanvases();
            var canvas = split.GetComponentInParent<Canvas>();
            var rect = (RectTransform)split.Selectable.transform;
            var point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            SetVirtualMouseState(point, Vector2.zero);
            yield return null;
            PressVirtualButton(VirtualMouse.leftButton);
            yield return null;
            ReleaseVirtualButton(VirtualMouse.leftButton);
            yield return null;

            Assert.That(input.isFocused, Is.False, "Clicking Split must defocus and commit Sections.");
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(4));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 },
                provider.GroupContext.ReadOnlyBoxes.Select(box => box.IndexFilter.Param1));
            Assert.That(provider.GroupContext.ReadOnlyBoxes.All(box => box.IndexFilter.Param0 == 4), Is.True);
            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.Undo();
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(1));
            Assert.That(provider.GroupContext.ReadOnlyBoxes[0].IndexFilter.Param0, Is.EqualTo(4));
            actions.Undo();
            Assert.That(provider.GroupContext.ReadOnlyBoxes[0].IndexFilter.Param0, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SplitTracksValidDraftsWithoutCommittingThem()
        {
            yield return InitializeSplitUiInput();
            OpenSplitGroup(CreateSplitGroup(BoxKind.Color, 1, 0));
            var original = provider.GroupContext;
            var input = ReadField<TextBoxIntComponent>(view, "p0Input").GetComponentInChildren<TMP_InputField>(true);
            var split = SplitButton();
            yield return null;
            EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
            yield return null;
            Assert.That(input.isFocused, Is.True);
            input.text = "4";
            Assert.That(split.Selectable.interactable, Is.True);
            input.text = "";
            Assert.That(split.Selectable.interactable, Is.False);
            input.text = "4+";
            Assert.That(split.Selectable.interactable, Is.False);
            input.text = "1";
            Assert.That(split.Selectable.interactable, Is.False);
            input.text = "2+2";
            Assert.That(split.Selectable.interactable, Is.True);
            Assert.That(input.isFocused, Is.True);
            Assert.That(provider.GroupContext, Is.SameAs(original));
            Assert.That(original.ReadOnlyBoxes[0].IndexFilter.Param0, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SplitStaysDisabledWhenTheDraftSectionsAlreadyExist()
        {
            yield return InitializeSplitUiInput();
            var group = CreateSplitGroup(BoxKind.Color, 1, 0);
            for (var section = 1; section < 4; section++)
            {
                var box = BeatmapFactory.Clone(group.ReadOnlyBoxes[0]);
                box.IndexFilter.Param0 = 4;
                box.IndexFilter.Param1 = section;
                AddSplitBox(group, box);
            }
            OpenSplitGroup(group);
            var input = ReadField<TextBoxIntComponent>(view, "p0Input").GetComponentInChildren<TMP_InputField>(true);
            yield return null;
            EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
            yield return null;
            Assert.That(input.isFocused, Is.True);
            input.text = "4";
            Assert.That(SplitButton().Selectable.interactable, Is.False);
            Assert.That(provider.GroupContext.ReadOnlyBoxes[0].IndexFilter.Param0, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InvalidDraftRestoresSplitAvailabilityAfterDefocus()
        {
            yield return InitializeSplitUiInput();
            OpenSplitGroup(CreateSplitGroup(BoxKind.Color, 4, 0));
            var input = ReadField<TextBoxIntComponent>(view, "p0Input").GetComponentInChildren<TMP_InputField>(true);
            var split = SplitButton();
            yield return null;
            EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
            yield return null;
            Assert.That(input.isFocused, Is.True);
            input.text = "4+";
            Assert.That(split.Selectable.interactable, Is.False);
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(input.isFocused, Is.False);
            Assert.That(input.text, Is.EqualTo("4"));
            Assert.That(split.Selectable.interactable, Is.True);
        }

        private IEnumerator InitializeSplitUiInput()
        {
            foreach (var events in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (events.enabled)
                {
                    disabledEventSystems.Add(events);
                    events.enabled = false;
                }
            }
            InitializeVirtualInput(true);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputEventsRoot = new GameObject("Split input regression events");
            inputEventsRoot.SetActive(false);
            inputEventsRoot.AddComponent<EventSystem>();
            var module = inputEventsRoot.AddComponent<InputSystemUIInputModule>();
            uiInput = new DefaultInputActions();
            uiInput.asset.devices = new InputDevice[] { VirtualMouse, VirtualKeyboard };
            module.actionsAsset = uiInput.asset;
            module.point = InputActionReference.Create(uiInput.UI.Point);
            module.leftClick = InputActionReference.Create(uiInput.UI.Click);
            inputEventsRoot.SetActive(true);
            // Finish the previous case's deferred group retirement before opening this case's box options.
            yield return null;
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
        }

        [TearDown]
        public void RestoreSplitUiInput()
        {
            if (inputEventsRoot != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                Object.DestroyImmediate(inputEventsRoot);
                inputEventsRoot = null;
                uiInput.Disable();
                Object.DestroyImmediate(uiInput.asset);
                uiInput = null;
            }
            TearDownVirtualInput();
            foreach (var events in disabledEventSystems)
            {
                events.enabled = true;
            }
            disabledEventSystems.Clear();
        }

        [Test]
        public void SplitButtonShowsLocalizedTooltip()
        {
            const string expectedText = "After updating Sections count, click this button to duplicate this event box for each of the other section ids in this split";
            OpenSplitGroup(CreateSplitGroup(BoxKind.Color, 4, 0));
            var button = SplitButton();
            Assert.That(button.Selectable.targetGraphic.raycastTarget, Is.True);
            var tooltip = button.Selectable.GetComponentInParent<Tooltip>();
            Assert.That(tooltip, Is.Not.Null, "Split needs a tooltip on its hover target.");
            Assert.That(tooltip.enabled, Is.True);
            Assert.That(tooltip.LocalizedTooltip.GetLocalizedString(), Is.EqualTo(expectedText));
            Assert.That(tooltip.TooltipOverride, Is.Null.Or.Empty);
            try
            {
                tooltip.OnPointerEnter(null);
                Assert.That(tooltip.TooltipActive, Is.True);
                Assert.That(ReadField<string>(PersistentUI.Instance, "currentTooltipMessage"), Is.EqualTo(expectedText));
            }
            finally
            {
                tooltip.OnPointerExit(null);
            }
        }

        [Test]
        public void AffectFirstLabelAndCheckboxShareLocalizedTooltip()
        {
            const string expectedText = "When unchecked, the distribution will NOT apply to the first event in this event box.";
            OpenSplitGroup(CreateSplitGroup(BoxKind.Color, 4, 0));
            var affectFirst = ReadField<ToggleComponent>(view, "affectFirstToggle");
            var label = Label(affectFirst);
            Assert.That(label.raycastTarget, Is.True);
            Assert.That(affectFirst.Selectable.targetGraphic.raycastTarget, Is.True);
            foreach (var target in new[] { label.gameObject, affectFirst.Selectable.gameObject })
            {
                var tooltip = target.GetComponentsInParent<Tooltip>(true).FirstOrDefault(candidate => candidate.enabled);
                Assert.That(tooltip, Is.Not.Null, "Both Affect First hover targets need an enabled tooltip.");
                Assert.That(tooltip.LocalizedTooltip.GetLocalizedString(), Is.EqualTo(expectedText));
                Assert.That(tooltip.TooltipOverride, Is.Null.Or.Empty);
                try
                {
                    tooltip.OnPointerEnter(null);
                    Assert.That(tooltip.TooltipActive, Is.True);
                    Assert.That(ReadField<string>(PersistentUI.Instance, "currentTooltipMessage"), Is.EqualTo(expectedText));
                }
                finally
                {
                    tooltip.OnPointerExit(null);
                }
            }
        }

        [Test]
        public void SplitCopiesOnlyMissingSectionsAndSupportsUndoRedo(
            [Values] BoxKind kind,
            [Values(0, 2)] int selectedSection,
            [Values(false, true)] bool hasExistingSection)
        {
            var group = CreateSplitGroup(kind, 4, selectedSection);
            var source = group.ReadOnlyBoxes[0];
            var existing = BeatmapFactory.Clone(source);
            existing.IndexFilter.Param1 = 1;
            existing.ReadOnlyEvents[0].CustomData["splitTest"] = "existing";
            if (hasExistingSection)
            {
                AddSplitBox(group, existing);
            }
            var differentCount = BeatmapFactory.Clone(source);
            differentCount.IndexFilter.Param0 = 8;
            differentCount.IndexFilter.Param1 = 3;
            AddSplitBox(group, differentCount);
            var stepFilter = BeatmapFactory.Clone(source);
            stepFilter.IndexFilter.Type = (int)IndexFilterType.StepAndOffset;
            stepFilter.IndexFilter.Param1 = 3;
            AddSplitBox(group, stepFilter);
            if (source is BaseLightTransformEventBox transform)
            {
                var otherAxis = BeatmapFactory.Clone(transform);
                otherAxis.Axis = (int)Axis.Z;
                otherAxis.IndexFilter.Param1 = 3;
                AddSplitBox(group, otherAxis);
            }
            GLSCommonCommand.RebindGroup(group);
            var originalJson = group.ToJson().ToString();
            OpenSplitGroup(group);
            var button = SplitButton();
            Assert.That(button.Selectable.interactable, Is.True);

            ((Button)button.Selectable).onClick.Invoke();

            var splitGroup = provider.GroupContext;
            Assert.That(splitGroup.ReadOnlyBoxes.Count,
                Is.EqualTo(group.ReadOnlyBoxes.Count + (hasExistingSection ? 2 : 3)));
            var sections = splitGroup.ReadOnlyBoxes.Where(box =>
                box.GetAxis() == source.GetAxis()
                && box.IndexFilter.Type == (int)IndexFilterType.Division
                && box.IndexFilter.Param0 == 4).ToArray();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, sections.Select(box => box.IndexFilter.Param1));
            foreach (var box in sections)
            {
                var expectedSource = hasExistingSection && box.IndexFilter.Param1 == 1 ? existing : source;
                var expectedJson = expectedSource.ToJson().Clone();
                expectedJson["f"]["t"] = box.IndexFilter.Param1;
                Assert.That(box.ToJson().ToString(), Is.EqualTo(expectedJson.ToString()));
                Assert.That(box.IndexFilter, Is.Not.SameAs(source.IndexFilter));
                for (var index = 0; index < box.ReadOnlyEvents.Count; index++)
                {
                    var evt = box.ReadOnlyEvents[index];
                    Assert.That(evt, Is.Not.SameAs(source.ReadOnlyEvents[index]));
                    Assert.That(evt.CustomData, Is.Not.SameAs(source.ReadOnlyEvents[index].CustomData));
                    Assert.That(evt.EventBoxGroupData, Is.SameAs(splitGroup));
                    Assert.That(evt.EventBoxData, Is.SameAs(box));
                    Assert.That(splitGroup.ReadOnlyBoxes[evt.BoxIndex], Is.SameAs(box));
                    Assert.That(evt.JsonTime, Is.EqualTo(splitGroup.JsonTime + evt.RelativeJsonTime));
                }
            }
            Assert.That(ReadField<BaseEventBox>(view, "boxContext").IndexFilter.Param1,
                Is.EqualTo(selectedSection));
            Assert.That(button.Selectable.interactable, Is.False);
            Assert.That(group.ToJson().ToString(), Is.EqualTo(originalJson));
            var splitJson = splitGroup.ToJson().ToString();
            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.Undo();
            Assert.That(provider.GroupContext.ToJson().ToString(), Is.EqualTo(originalJson));
            Assert.That(button.Selectable.interactable, Is.True);
            actions.Redo();
            Assert.That(provider.GroupContext.ToJson().ToString(), Is.EqualTo(splitJson));
            Assert.That(button.Selectable.interactable, Is.False);
        }

        [Test]
        public void SplitIsUnavailableWhenThereAreNoMissingSections(
            [Values] BoxKind kind,
            [Values] UnavailableSplit scenario)
        {
            var group = CreateSplitGroup(kind, scenario == UnavailableSplit.Default ? 1 : 4, 0);
            if (scenario == UnavailableSplit.Complete)
            {
                for (var section = 1; section < 4; section++)
                {
                    var box = BeatmapFactory.Clone(group.ReadOnlyBoxes[0]);
                    box.IndexFilter.Param1 = section;
                    AddSplitBox(group, box);
                }
            }
            if (scenario == UnavailableSplit.StepFilter)
            {
                group.ReadOnlyBoxes[0].IndexFilter.Type = (int)IndexFilterType.StepAndOffset;
            }
            OpenSplitGroup(group);
            var button = SplitButton();
            Assert.That(button.Selectable.interactable, Is.False);
            Assert.That(button.gameObject.activeSelf, Is.EqualTo(scenario != UnavailableSplit.StepFilter));
            var originalJson = group.ToJson().ToString();

            ((Button)button.Selectable).onClick.Invoke();

            Assert.That(provider.GroupContext, Is.SameAs(group));
            Assert.That(group.ToJson().ToString(), Is.EqualTo(originalJson));
        }

        [Test]
        public void SplitAvailabilityUpdatesWhenSectionCountOrFilterChanges([Values] BoxKind kind)
        {
            OpenSplitGroup(CreateSplitGroup(kind, 1, 0));
            var button = SplitButton();
            Assert.That(button.Selectable.interactable, Is.False);
            ReadField<TextBoxIntComponent>(view, "p0Input").Value = 4;
            Assert.That(button.Selectable.interactable, Is.True);
            ((Button)button.Selectable).onClick.Invoke();
            Assert.That(button.Selectable.interactable, Is.False);
            ReadField<ToggleComponent>(view, "filterTypeStepToggle").Value = true;
            Assert.That(button.gameObject.activeSelf, Is.False);
            ReadField<ToggleComponent>(view, "filterTypeSectionToggle").Value = true;
            Assert.That(button.gameObject.activeSelf, Is.True);
            Assert.That(button.Selectable.interactable, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefaultPhantomLaneCannotBeSplit(bool translation)
        {
            var group = CreateGroup(translation);
            provider.GroupContext = group;
            var button = SplitButton();
            Assert.That(button.Selectable.interactable, Is.False);
            ((Button)button.Selectable).onClick.Invoke();
            Assert.That(provider.GroupContext, Is.SameAs(group));
            Assert.That(group.ReadOnlyBoxes.Count, Is.Zero);
        }

        [Test]
        public void SplitButtonIsLocalizedAndPlacedImmediatelyAfterSectionsInput()
        {
            OpenSplitGroup(CreateSplitGroup(BoxKind.Color, 4, 0));
            var button = SplitButton();
            var sections = ReadField<TextBoxIntComponent>(view, "p0Input");
            Assert.That(button.transform.parent, Is.SameAs(sections.transform.parent));
            Assert.That(button.transform.GetSiblingIndex(), Is.EqualTo(sections.transform.GetSiblingIndex() + 1));
            var localized = button.GetComponentInChildren<LocalizeStringEvent>(true);
            Assert.That(localized, Is.Not.Null);
            Assert.That(localized.StringReference.GetLocalizedString(), Is.EqualTo("Split"));
            Assert.That(button.GetComponentInChildren<TextMeshProUGUI>(true).text, Is.EqualTo("Split"));
            Canvas.ForceUpdateCanvases();
            var splitLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
            var wave = ReadField<ToggleComponent>(view, "beatDistributionWaveToggle");
            var step = ReadField<ToggleComponent>(view, "beatDistributionStepToggle");
            Assert.That(splitLabel.fontSize, Is.EqualTo(Label(wave).fontSize),
                "Split must use the compact Wave/Step font size.");
            Assert.That(splitLabel.fontSize, Is.EqualTo(Label(step).fontSize));
            var compactHeight = ((RectTransform)wave.transform).rect.height;
            Assert.That(((RectTransform)button.transform).rect.height,
                Is.EqualTo(compactHeight).Within(0.1f), "Split must not make the Sections row taller.");
            Assert.That(((RectTransform)sections.transform).rect.height,
                Is.EqualTo(compactHeight).Within(0.1f));
            Assert.That(((RectTransform)ReadField<TextBoxIntComponent>(view, "p1Input").transform).rect.height,
                Is.EqualTo(compactHeight).Within(0.1f));
            var buttonBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                sections.transform.parent, button.transform);
            var inputBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                sections.transform.parent, sections.transform);
            Assert.That(buttonBounds.min.x, Is.GreaterThanOrEqualTo(inputBounds.max.x));
            Assert.That(buttonBounds.size.x, Is.GreaterThan(25f));
            Assert.That(sections.GetComponentInChildren<TMP_InputField>(true).textViewport.rect.width,
                Is.GreaterThanOrEqualTo(20f), "The Sections entry must retain room for its value.");
            Assert.That(ReadField<TextBoxIntComponent>(view, "p1Input")
                    .GetComponentInChildren<TMP_InputField>(true).textViewport.rect.width,
                Is.GreaterThanOrEqualTo(20f), "The section ID entry must retain room for its value.");
        }

        private ButtonComponent SplitButton()
        {
            var field = typeof(EventBoxViewController).GetField(
                "splitSectionsButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var button = field?.GetValue(view) as ButtonComponent;
            Assert.That(button, Is.Not.Null, "Sections must have a wired Split button.");
            return button;
        }

        private void OpenSplitGroup(BaseEventBoxGroup group)
        {
            GLSCommonCommand.RebindGroup(group);
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            provider.LastContext = null;
            provider.GroupContext = group;
            Tabs()[provider.GetDisplayedLaneIndex(0)].Value = true;
        }

        private static BaseEventBoxGroup CreateSplitGroup(BoxKind kind, int sections, int section)
        {
            BaseEventBoxGroup group = kind switch
            {
                BoxKind.Color => new BaseLightColorEventBoxGroup(),
                BoxKind.Rotation => new BaseLightRotationEventBoxGroup(),
                BoxKind.Translation => new BaseLightTranslationEventBoxGroup(),
                _ => new BaseVfxEventEventBoxGroup()
            };
            BaseEventBox box = kind switch
            {
                BoxKind.Color => new BaseLightColorEventBox(),
                BoxKind.Rotation => new BaseLightRotationEventBox { Axis = (int)Axis.Y },
                BoxKind.Translation => new BaseLightTranslationEventBox { Axis = (int)Axis.Y },
                _ => new BaseVfxEventEventBox()
            };
            BaseGLSEvent evt = kind switch
            {
                BoxKind.Color => new BaseLightColorBase { Brightness = 0.75f },
                BoxKind.Rotation => new BaseLightRotationBase { Rotation = 90f },
                BoxKind.Translation => new BaseLightTranslationBase { Translation = 0.5f },
                _ => new BaseFxEventFloat { Value = 0.75f }
            };
            evt.RelativeJsonTime = 1.25f;
            evt.Easing = 2;
            evt.CustomData = new SimpleJSON.JSONObject { ["splitTest"] = "source" };
            var second = BeatmapFactory.Clone(evt);
            second.RelativeJsonTime = 2.5f;
            box.SetEvents(new[] { evt, second });
            box.IndexFilter = new BaseIndexFilter
            {
                Type = (int)IndexFilterType.Division, Param0 = sections, Param1 = section,
                Reverse = 1, Chunks = 2, Limit = 0.75f,
                LimitAffectsType = (int)(LimitAlsoAffectType.Duration | LimitAlsoAffectType.Distribution),
                Random = (int)(RandomType.RandomElements | RandomType.KeepOrder), Seed = 314159
            };
            box.BeatDistribution = 0.5f;
            box.BeatDistributionType = (int)DistributionType.Step;
            box.Easing = 2;
            switch (box)
            {
                case BaseLightColorEventBox color:
                    color.BrightnessDistribution = 0.3f;
                    color.BrightnessDistributionType = (int)DistributionType.Step;
                    color.BrightnessAffectFirst = 1;
                    color.SetCustomData(new SimpleJSON.JSONObject { ["future"] = 42 });
                    color.ColorDistributions = new[] { "h,-0.2,IO^5", "f,0.1,I^2" };
                    color.StrobeColorDistributions = new[] { "s,-0.2,L,l" };
                    color.SaveCustom();
                    break;
                case BaseLightTransformEventBox transform:
                    transform.Flip = 1;
                    transform.ValueDistribution = 0.3f;
                    transform.ValueDistributionType = (int)DistributionType.Step;
                    transform.AffectFirst = 1;
                    break;
                case BaseVfxEventEventBox fx:
                    fx.VfxDistribution = 0.3f;
                    fx.VfxDistributionType = (int)DistributionType.Step;
                    fx.VfxAffectFirst = 1;
                    break;
            }
            group.ID = GroupId;
            group.JsonTime = 16f;
            AddSplitBox(group, box);
            return group;
        }

        private static void AddSplitBox(BaseEventBoxGroup group, BaseEventBox box)
        {
            switch (group)
            {
                case BaseLightColorEventBoxGroup color:
                    color.Boxes.Add((BaseLightColorEventBox)box);
                    break;
                case ILightTransformEventBoxGroup transform:
                    transform.TryAddTransformBox((BaseLightTransformEventBox)box);
                    break;
                case BaseVfxEventEventBoxGroup fx:
                    fx.Boxes.Add((BaseVfxEventEventBox)box);
                    break;
            }
        }

        [SetUp]
        public void SetUpOptions()
        {
            provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            view = Object.FindAnyObjectByType<EventBoxViewController>();
            definitions = Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions;
            hadDefinition = definitions.Gls.TryGetValue(GroupId, out previousDefinition);
            definitions.Gls[GroupId] = new TrackDefinitionGLS
            {
                ID = GroupId,
                RotationTracks = new[] { true, true, true },
                TranslationTracks = new[] { true, true, true }
            };
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DaftPunkTrianglesOnlyDisplayDeclaredAxes(bool translation)
        {
            var daft = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(
                "Assets/__Scripts/Environments/TrackDefinitions/DaftPunkEnvironmentTrackDefinitions.asset");
            var triangles = daft.Gls.Values.First(definition =>
                (definition.Group + " " + definition.Name).IndexOf("tri", StringComparison.OrdinalIgnoreCase) >= 0);
            definitions.Gls[GroupId] = triangles;
            var group = CreateGroup(translation);
            provider.GroupContext = group;

            var enabled = ((ILightTransformEventBoxGroup)group).GetEnabledAxes(triangles);
            var expectedAxes = Enumerable.Range(0, 3).Where(axis => enabled[axis]).ToArray();
            CollectionAssert.AreEqual(expectedAxes, DisplayedBoxes().Select(box => (int)box.GetAxis()));
            Assert.That(group.ReadOnlyBoxes.Count, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnsupportedAuthoredAxisRemainsAccessibleWithoutUnsupportedPlaceholders(bool translation)
        {
            definitions.Gls[GroupId].RotationTracks = new[] { false, false, true };
            definitions.Gls[GroupId].TranslationTracks = new[] { false, false, true };
            provider.GroupContext = CreateGroup(translation, 0);

            CollectionAssert.AreEqual(new[] { 0, 2 }, DisplayedBoxes().Select(box => (int)box.GetAxis()));
        }

        [Test]
        public void AxisOptionsOnlyShowSupportedAxesAndRestoreWhenSwitchingGroups(
            [Values(false, true)] bool translation,
            [Values(1, 2, 3, 4, 5, 6, 7)] int supportedAxes)
        {
            var enabled = new[]
            {
                (supportedAxes & 1) != 0,
                (supportedAxes & 2) != 0,
                (supportedAxes & 4) != 0
            };
            definitions.Gls[GroupId].RotationTracks = enabled;
            definitions.Gls[GroupId].TranslationTracks = enabled;
            OpenGroup(translation);
            var axisToggles = new[]
            {
                ReadField<ToggleComponent>(view, "axisXToggle"),
                ReadField<ToggleComponent>(view, "axisYToggle"),
                ReadField<ToggleComponent>(view, "axisZToggle")
            };

            foreach (var tab in Tabs())
            {
                tab.Value = true;
                for (var axis = 0; axis < axisToggles.Length; axis++)
                {
                    Assert.That(axisToggles[axis].gameObject.activeSelf, Is.EqualTo(enabled[axis]),
                        $"The {(Axis)axis} option must be hidden when that axis is unsupported.");
                }
            }

            definitions.Gls[GroupId].RotationTracks = new[] { true, true, true };
            definitions.Gls[GroupId].TranslationTracks = new[] { true, true, true };
            provider.GroupContext = CreateGroup(translation, 0);
            foreach (var toggle in axisToggles)
            {
                Assert.That(toggle.gameObject.activeSelf, Is.True,
                    "Switching to a group supporting every axis must restore all axis options.");
                Assert.That(toggle.Selectable.interactable, Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HeaderIndexesMatchDisplayedLanesAndDimOnlyPlaceholders(bool translation)
        {
            OpenGroup(translation);
            var tabs = Tabs();
            Assert.That(tabs.Length, Is.EqualTo(provider.DisplayedLaneCount));
            for (var index = 0; index < tabs.Length; index++)
            {
                Assert.That(Label(tabs[index]).text, Is.EqualTo((index + 1).ToString()));
                tabs[index].Value = true;
                provider.TryGetDisplayedBox(index, out var displayedBox);
                Assert.That(ReadField<BaseEventBox>(view, "boxContext"), Is.SameAs(displayedBox));
            }

            Assert.That(Label(tabs[1]).color.a, Is.EqualTo(Label(tabs[0]).color.a * 0.375f).Within(0.001f));
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(2));
        }

        [Test]
        public void EditingPlaceholderOptionsMaterializesOneBoxAndUndoRestoresIt(
            [Values(false, true)] bool translation,
            [Values] BoxOption option)
        {
            OpenGroup(translation);
            var placeholder = DisplayedBoxes().Single(box => box.GetAxis() == Axis.Y);
            var expectedAffectFirst = ((BaseLightTransformEventBox)placeholder).AffectFirst == 1 ? 0 : 1;
            var middle = Tabs().Single(tab => Label(tab).text == "2");
            middle.Value = true;

            ChangeOption(option);

            var editedGroup = provider.GroupContext;
            Assert.That(editedGroup.ReadOnlyBoxes.Count, Is.EqualTo(3), option.ToString());
            var savedBox = editedGroup.ReadOnlyBoxes.Single(box => box.GetAxis() == Axis.Y);
            Assert.That(savedBox.IsAutomaticAxisLane, Is.False);
            Assert.That(savedBox.ReadOnlyEvents.Count, Is.Zero);
            AssertOptionValue(savedBox, option, translation, expectedAffectFirst);
            Assert.That(editedGroup.ToJson()["e"].AsArray.Count, Is.EqualTo(3));
            Assert.That(placeholder.IsAutomaticAxisLane, Is.True);
            Assert.That(Label(Tabs()[1]).color.a, Is.EqualTo(Label(Tabs()[0]).color.a).Within(0.001f));

            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.Undo();
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(2));
            Assert.That(DisplayedBoxes().Single(box => box.GetAxis() == Axis.Y).IsAutomaticAxisLane, Is.True);
            actions.Redo();
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(3));
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Single(box => box.GetAxis() == Axis.Y).IsAutomaticAxisLane, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HeaderEditsTheDisplayedBoxInsteadOfItsAuthoredIndex(bool translation)
        {
            OpenGroup(translation);
            Tabs()[0].Value = true;

            ReadField<TextBoxFloatComponent>(view, "beatDistributionInput").Value = 4f;

            Assert.That(provider.GroupContext.ReadOnlyBoxes.Single(box => box.GetAxis() == Axis.X).BeatDistribution,
                Is.EqualTo(4f));
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Single(box => box.GetAxis() == Axis.Z).BeatDistribution,
                Is.Zero);
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(2));
        }

        [Test]
        public void ChangingPlaceholderAxisKeepsTheNewBoxSelected(
            [Values(false, true)] bool translation,
            [Values(Axis.X, Axis.Z)] Axis axis)
        {
            OpenGroup(translation);
            Tabs()[1].Value = true;

            ReadField<ToggleComponent>(view, axis == Axis.X ? "axisXToggle" : "axisZToggle").Value = true;

            var selected = ReadField<BaseEventBox>(view, "boxContext");
            Assert.That(selected.GetAxis(), Is.EqualTo(axis));
            Assert.That(selected.IsAutomaticAxisLane, Is.False);
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Contains(selected), Is.True);
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(3));
        }

        [Test]
        public void AddAndDuplicateOnPlaceholderCreateOneSavedBox(
            [Values(false, true)] bool translation,
            [Values("addEventBoxButton", "duplicateEventBoxButton")] string buttonField)
        {
            OpenGroup(translation);
            Tabs()[1].Value = true;

            ((Button)ReadField<ButtonComponent>(view, buttonField).Selectable).onClick.Invoke();

            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(3));
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Single(box => box.GetAxis() == Axis.Y).IsAutomaticAxisLane,
                Is.False);
            Object.FindAnyObjectByType<BeatmapActionContainer>().Undo();
            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(2));
        }

        [TestCase(ObjectType.GLSColor)]
        [TestCase(ObjectType.GLSFloatFx)]
        public void AddFirstBoxStillWorksForGroupsWithoutAxisPlaceholders(ObjectType type)
        {
            BaseEventBoxGroup group = type == ObjectType.GLSColor
                ? new BaseLightColorEventBoxGroup { ID = GroupId, JsonTime = 16f }
                : new BaseVfxEventEventBoxGroup { ID = GroupId, JsonTime = 16f };
            BeatmapObjectContainerCollection.GetCollectionForType(type).SpawnObject(group, false, false, true);
            provider.LastContext = null;
            provider.GroupContext = group;
            Assert.That(provider.DisplayedLaneCount, Is.Zero);

            ((Button)ReadField<ButtonComponent>(view, "addEventBoxButton").Selectable).onClick.Invoke();

            Assert.That(provider.GroupContext.ReadOnlyBoxes.Count, Is.EqualTo(1));
            Assert.That(Tabs().Length, Is.EqualTo(1));
        }

        private static void AssertOptionValue(BaseEventBox box, BoxOption option, bool translation, int expectedAffectFirst)
        {
            var transform = (BaseLightTransformEventBox)box;
            var actual = option switch
            {
                BoxOption.BeatDistribution => box.BeatDistribution,
                BoxOption.BeatDistributionType => box.BeatDistributionType,
                BoxOption.FilterType => box.IndexFilter.Type,
                BoxOption.SectionCount => box.IndexFilter.Param0,
                BoxOption.SectionIndex => box.IndexFilter.Param1,
                BoxOption.Chunks => box.IndexFilter.Chunks,
                BoxOption.Reverse => box.IndexFilter.Reverse,
                BoxOption.Random or BoxOption.KeepOrder => box.IndexFilter.Random,
                BoxOption.Seed => box.IndexFilter.Seed,
                BoxOption.Limit => box.IndexFilter.Limit,
                BoxOption.LimitDuration or BoxOption.LimitDistribution => box.IndexFilter.LimitAffectsType,
                BoxOption.Flip => transform.Flip,
                BoxOption.ValueDistribution => transform.ValueDistribution,
                BoxOption.ValueDistributionType => transform.ValueDistributionType,
                BoxOption.AffectFirst => transform.AffectFirst,
                BoxOption.Easing => box.Easing,
                _ => throw new ArgumentOutOfRangeException(nameof(option))
            };
            var expected = option switch
            {
                BoxOption.BeatDistribution or BoxOption.SectionCount or BoxOption.Chunks => 2f,
                BoxOption.FilterType => (int)IndexFilterType.StepAndOffset,
                BoxOption.BeatDistributionType or BoxOption.ValueDistributionType => (int)DistributionType.Step,
                BoxOption.Random => (int)RandomType.RandomElements,
                BoxOption.KeepOrder => (int)RandomType.KeepOrder,
                BoxOption.LimitDuration => (int)LimitAlsoAffectType.Duration,
                BoxOption.LimitDistribution => (int)LimitAlsoAffectType.Distribution,
                BoxOption.Seed => 23f,
                BoxOption.Limit => 0.75f,
                BoxOption.ValueDistribution => translation ? 0.3f : 30f,
                BoxOption.AffectFirst => expectedAffectFirst,
                _ => 1f
            };
            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f), option.ToString());
        }

        private void ChangeOption(BoxOption option)
        {
            switch (option)
            {
                case BoxOption.BeatDistribution:
                    ReadField<TextBoxFloatComponent>(view, "beatDistributionInput").Value = 2f;
                    break;
                case BoxOption.BeatDistributionType:
                    ReadField<ToggleComponent>(view, "beatDistributionStepToggle").Value = true;
                    break;
                case BoxOption.FilterType:
                    ReadField<ToggleComponent>(view, "filterTypeStepToggle").Value = true;
                    break;
                case BoxOption.SectionCount:
                    ReadField<TextBoxIntComponent>(view, "p0Input").Value = 2;
                    break;
                case BoxOption.SectionIndex:
                    ReadField<TextBoxIntComponent>(view, "p1Input").Value = 2;
                    break;
                case BoxOption.Chunks:
                    ReadField<TextBoxIntComponent>(view, "chunkInput").Value = 2;
                    break;
                case BoxOption.Seed:
                    ReadField<TextBoxIntComponent>(view, "seedInput").Value = 23;
                    break;
                case BoxOption.Limit:
                    ReadField<TextBoxFloatComponent>(view, "limitInput").Value = 75f;
                    break;
                case BoxOption.ValueDistribution:
                    ReadField<TextBoxFloatComponent>(view, "valueDistributionInput").Value = 30f;
                    break;
                case BoxOption.Easing:
                    ReadField<DropdownComponent>(view, "easeTypeDropdown").Value = 1;
                    break;
                default:
                    var field = option switch
                    {
                        BoxOption.Reverse => "reverseToggle",
                        BoxOption.Random => "randomToggle",
                        BoxOption.KeepOrder => "inOrderToggle",
                        BoxOption.LimitDuration => "limitDurationToggle",
                        BoxOption.LimitDistribution => "limitDistributionToggle",
                        BoxOption.Flip => "flipToggle",
                        BoxOption.ValueDistributionType => "valueDistributionStepToggle",
                        BoxOption.AffectFirst => "affectFirstToggle",
                        _ => throw new ArgumentOutOfRangeException(nameof(option))
                    };
                    var toggle = ReadField<ToggleComponent>(view, field);
                    toggle.Value = option == BoxOption.AffectFirst ? !toggle.Value : true;
                    break;
            }
        }

        private void OpenGroup(bool translation)
        {
            var group = CreateGroup(translation, 2, 0);
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            provider.LastContext = null;
            provider.GroupContext = group;
        }

        private static BaseEventBoxGroup CreateGroup(bool translation, params int[] axes)
        {
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup { ID = GroupId, JsonTime = 16f }
                : new BaseLightRotationEventBoxGroup { ID = GroupId, JsonTime = 16f };
            var transformGroup = (ILightTransformEventBoxGroup)group;
            foreach (var axis in axes)
            {
                transformGroup.TryAddTransformBox(transformGroup.CreateTransformBox(axis));
            }
            return group;
        }

        private IEnumerable<BaseEventBox> DisplayedBoxes()
        {
            for (var index = 0; index < provider.DisplayedLaneCount; index++)
            {
                provider.TryGetDisplayedBox(index, out var box);
                yield return box;
            }
        }

        private ToggleComponent[] Tabs() => ReadField<List<ToggleComponent>>(view, "instantiatedIdTab")
            .Where(tab => tab.gameObject.activeSelf).ToArray();

        private static TextMeshProUGUI Label(ToggleComponent tab) =>
            (TextMeshProUGUI)typeof(CMUIComponentWithLabel<bool>)
                .GetField("labelText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tab);

        private static T ReadField<T>(object instance, string name) =>
            (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

        protected override void BeforeCleanup()
        {
            provider.LastContext = null;
            provider.GroupContext = null;
            if (hadDefinition)
            {
                definitions.Gls[GroupId] = previousDefinition;
            }
            else
            {
                definitions.Gls.Remove(GroupId);
            }
        }
    }
}
