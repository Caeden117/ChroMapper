using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.Editor
{
    // Edit and undo nodes through the editor, then compare live lights and existing ribbons without
    // rebuilding either.
    public class GLSColorRibbonMutationTest : GLSColorRibbonTestBase
    {
        private const int SyntheticPrimaryGroupId = 1000;
        private const int SyntheticSecondaryGroupId = 1001;

        // Resolve a physical color lane plus one fixture lane on the same page so outer paste/shift has valid metadata.
        private static int PrimaryGroupId => GetColorLanePair().Primary;
        private static int SecondaryGroupId => GetColorLanePair().Secondary;

        private static readonly Color colorA = new(0.91f, 0.13f, 0.21f, 0.71f);
        private static readonly Color colorB = new(0.17f, 0.83f, 0.29f, 0.59f);
        private static readonly Color colorC = new(0.23f, 0.31f, 0.94f, 0.43f);

        private static readonly int[] easingValues =
        {
            (int)EaseType.Linear,
            (int)EaseType.None,
            (int)EaseType.InQuadratic
        };

        private StackTraceLogType originalWarningStackTrace;
        private BeatmapActionContainer actionContainer;

        [SetUp]
        public void CacheActionContainer()
        {
            actionContainer = UnityEngine.Object.FindAnyObjectByType<BeatmapActionContainer>();
        }

        // Omit repeated localization-warning stacks during the many seek samples, while retaining warning
        // messages and all errors.
        [OneTimeSetUp]
        public void SuppressRepeatedWarningStackTraces()
        {
            originalWarningStackTrace = Application.GetStackTraceLogType(LogType.Warning);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        }

        [OneTimeTearDown]
        public void RestoreWarningStackTraces() =>
            Application.SetStackTraceLogType(LogType.Warning, originalWarningStackTrace);

        // Keep synthetic track insertion order stable across all 27 easing iterations, then remove it after the NUnit case.
        [TearDown]
        public void RemoveSyntheticColorTracks()
        {
            var runtime = GetGroupCollection().BeatmapContext;
            if (runtime == null)
            {
                return;
            }

            runtime.TrackDefinitions.Gls.Remove(SyntheticPrimaryGroupId);
            runtime.TrackDefinitions.Gls.Remove(SyntheticSecondaryGroupId);
            // Page-aware pooling must see the fixture lanes disappear through the same production notification path.
            runtime.NotifyTrackDefinitions();
        }

        private static IEnumerable<TestCaseData> MovementCases()
        {
            foreach (var view in Enum.GetValues(typeof(RibbonView)).Cast<RibbonView>())
            foreach (var operation in Enum.GetValues(typeof(MovementOperation)).Cast<MovementOperation>())
            foreach (var destination in Enum.GetValues(typeof(ChronologicalDestination)).Cast<ChronologicalDestination>())
            {
                yield return new TestCaseData(view, operation, destination)
                    .SetName($"{view}_{operation}_{destination}_KeepsRibbonAndPreviewSynchronized");
            }
        }

        private static IEnumerable<TestCaseData> PositionalCases()
        {
            foreach (var view in Enum.GetValues(typeof(RibbonView)).Cast<RibbonView>())
            foreach (var position in Enum.GetValues(typeof(ChronologicalDestination)).Cast<ChronologicalDestination>())
            {
                yield return new TestCaseData(view, position)
                    .SetName($"{view}_DeleteAndUndo_{position}_KeepsRibbonAndPreviewSynchronized");
            }
        }

        private static IEnumerable<TestCaseData> HoverCases()
        {
            foreach (var view in Enum.GetValues(typeof(RibbonView)).Cast<RibbonView>())
            foreach (var mutation in Enum.GetValues(typeof(HoverMutation)).Cast<HoverMutation>())
            foreach (var position in Enum.GetValues(typeof(ChronologicalDestination)).Cast<ChronologicalDestination>())
            {
                yield return new TestCaseData(view, mutation, position)
                    .SetName($"{view}_{mutation}_{position}_KeepsRibbonAndPreviewSynchronized");
            }
        }

        [TestCaseSource(nameof(MovementCases))]
        public void MovingColorNodeKeepsRibbonAndPreviewSynchronized(
            RibbonView view,
            MovementOperation operation,
            ChronologicalDestination destination)
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            var scenario = CreateMovementScenario(view, operation, destination, GetEasingTriples().First());
            try
            {
                foreach (var easings in GetEasingTriples())
                {
                    ApplyScenarioEasings(easings);
                    AssertScenario(scenario, preview, "before edit");

                    if (operation == MovementOperation.CutPaste)
                    {
                        CutMovedObject(scenario);
                        AssertScenario(scenario, preview, "after cut");
                        PasteMovedObject(scenario);
                        AssertExpectedOrder(scenario, ExpectedOrder(destination));
                        AssertScenario(scenario, preview, "after paste");

                        Undo();
                        AssertScenario(scenario, preview, "after undoing paste");
                        Undo();
                    }
                    else
                    {
                        ApplyMovement(scenario, operation);
                        AssertExpectedOrder(scenario, ExpectedOrder(destination));
                        AssertScenario(scenario, preview, "after edit");
                        Undo();
                    }

                    AssertExpectedOrder(
                        scenario,
                        operation == MovementOperation.LaneShift
                            ? ExpectedOrder(destination).Replace(scenario.MovedLabel.ToString(), string.Empty)
                            : "ABC");
                    AssertScenario(scenario, preview, "after final undo");
                }
            }
            finally
            {
                CleanupScenario();
            }
        }

        // OrderedEvents can sort lazily, hiding a stale raw Events array. GLSColorTimeline and saved JSON
        // also read that raw order, so check it after shifting part of a box.
        [Test]
        public void PartialBoxTimeShiftKeepsBoxEventsChronological()
        {
            var scenario = CreateInnerScenario("ABC", GetEasingTriples().First(), 'A');
            try
            {
                SelectMovedObject(scenario);
                scenario.Selection.MoveSelection(5f);

                var group = FindScenarioGroups(scenario.GroupId).Single();
                var events = group.Boxes[0].Events;
                Assert.That(events.Length, Is.EqualTo(3));
                for (var index = 1; index < events.Length; index++)
                {
                    Assert.That(
                        events[index].RelativeJsonTime,
                        Is.GreaterThanOrEqualTo(events[index - 1].RelativeJsonTime),
                        $"box.Events[{index}] lost chronological order after the partial-box shift.");
                }

                Assert.That(GetLabel(events[^1]), Is.EqualTo('A'));
            }
            finally
            {
                CleanupScenario();
            }
        }

        [TestCase(RibbonView.Inner)]
        [TestCase(RibbonView.Outer)]
        public void RibbonVisibilitySettingRefreshesLoadedContainersImmediately(RibbonView view)
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            var scenario = view == RibbonView.Inner
                ? CreateInnerScenario("ABC", GetEasingTriples().First())
                : CreateOuterScenario("ABC", GetEasingTriples().First());
            try
            {
                AssertScenario(scenario, preview, "before visibility setting");
                var controllers = GetLoadedColorRibbonControllers(view, scenario.GroupId);
                var initialVisibility = controllers.ToDictionary(
                    controller => controller,
                    controller => controller.gameObject.activeSelf);
                Assert.That(initialVisibility.Values, Has.Some.True, "The scenario needs at least one visible ribbon.");

                Settings.Instance.VisualizeGLSLightTransitions = false;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    nameof(Settings.VisualizeGLSLightTransitions),
                    false);
                Assert.That(controllers, Has.All.Matches<LightGradientController>(
                    controller => !controller.gameObject.activeSelf));

                Settings.Instance.VisualizeGLSLightTransitions = true;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    nameof(Settings.VisualizeGLSLightTransitions),
                    true);
                foreach (var pair in initialVisibility)
                {
                    Assert.That(pair.Key.gameObject.activeSelf, Is.EqualTo(pair.Value));
                }
            }
            finally
            {
                Settings.Instance.VisualizeGLSLightTransitions = true;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    nameof(Settings.VisualizeGLSLightTransitions),
                    true);
                CleanupScenario();
            }
        }

        // Snapshot only the loaded controllers owned by the active synthetic lane so unrelated fixture ribbons cannot mask a missed live refresh.
        private static List<LightGradientController> GetLoadedColorRibbonControllers(RibbonView view, int groupId)
        {
            var controllers = new List<LightGradientController>();
            if (view == RibbonView.Inner)
            {
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                foreach (var container in collection.LoadedContainers.Values.OfType<GLSEventContainer>())
                {
                    if (container.EventData?.EventBoxGroupData?.ID != groupId)
                    {
                        continue;
                    }

                    controllers.Add(container.LightGradientController);
                    controllers.Add(container.IncomingLightGradientController);
                }
            }
            else
            {
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
                var owners = new HashSet<GLSGroupContainer>(
                    collection.LoadedContainers
                        .Where(pair => pair.Key is BaseLightColorEventBoxGroup group && group.ID == groupId)
                        .Select(pair => pair.Value)
                        .OfType<GLSGroupContainer>());
                foreach (var container in UnityEngine.Object.FindObjectsByType<GLSGroupContainer>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (!owners.Contains(container) && !owners.Contains(container.DragTarget))
                    {
                        continue;
                    }

                    controllers.Add(container.lightGradientController);
                    controllers.Add(container.IncomingLightGradientController);
                }
            }

            return controllers.Where(controller => controller != null).Distinct().ToList();
        }

        [TestCaseSource(nameof(PositionalCases))]
        public void DeleteAndUndoColorNode(
            RibbonView view,
            ChronologicalDestination position)
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            var scenario = CreatePositionalScenario(view, position, GetEasingTriples().First());
            try
            {
                foreach (var easings in GetEasingTriples())
                {
                    ApplyScenarioEasings(easings);
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "before delete");

                    DeleteMovedObject(scenario);
                    AssertScenario(scenario, preview, "after delete");

                    Undo();
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "after undoing delete");
                }
            }
            finally
            {
                CleanupScenario();
            }
        }

        [TestCaseSource(nameof(HoverCases))]
        public void HoverMutatingColorNodeAndUndo(
            RibbonView view,
            HoverMutation mutation,
            ChronologicalDestination position)
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            var scenario = CreatePositionalScenario(view, position, GetEasingTriples().First());
            HoverInputSession inputSession = null;
            try
            {
                // Reuse one isolated Input System session across the easing combinations.
                inputSession = new HoverInputSession();
                foreach (var easings in GetEasingTriples())
                {
                    ApplyScenarioEasings(easings);
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "before hover mutation");

                    var originalEvent = ApplyHoverMutation(scenario, mutation, inputSession);
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "after hover mutation");

                    Undo();
                    AssertColorEventPropertiesEqual(originalEvent, GetMovedObject(scenario), "after undoing hover mutation");
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "after undoing hover mutation");
                }
            }
            finally
            {
                inputSession?.Dispose();
                CleanupScenario();
            }
        }

        // Inserting, moving, or removing a boost inside a held palette span must repaint the already-
        // rendered ribbon even though its source predates the invalidated interval.
        [TestCase(RibbonView.Inner)]
        [TestCase(RibbonView.Outer)]
        public void BoostEditInsideHeldPaletteRibbonRefreshesUploadedColors(RibbonView view)
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 1);
            var collection = GetGroupCollection();
            var map = BeatSaberSongContainer.Instance.Map;
            // The test map's color scheme leaves boost colors black, which cannot distinguish palette
            // epochs in a rendered pixel. Borrow the shared appearance's boost entry; both GLS appearance
            // assets resolve through the same EventAppearanceSO instance.
            var appearance = preview.EventAppearance;
            var originalBoostColor = appearance.RedBoostColor;
            appearance.RedBoostColor = new Color(0.2f, 0.85f, 0.3f, 1f);
            BaseLightColorEventBoxGroup group = null;
            BaseEvent boost = null;
            try
            {
                collection.EditContext.EditingMode = view == RibbonView.Inner
                    ? EditingMode.EventBox
                    : EditingMode.GLS;
                if (view == RibbonView.Outer)
                {
                    // A nonzero preview opacity enables each outer node's ribbon.
                    Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
                    ConfigureOuterColorPage();
                }

                var held = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                };
                group = CreateGroup(1f, PrimaryGroupId, 0, held);
                SpawnGroup(group);
                if (view == RibbonView.Inner)
                {
                    var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
                    provider.LastContext = null;
                    provider.GroupContext = group;
                }

                var source = map.LightColorEventBoxGroups
                    .Single(candidate => candidate.ID == PrimaryGroupId)
                    .Boxes[0].Events[0];
                var ribbons = FindHeldRibbonControllers(view, source);
                Assert.That(
                    ribbons,
                    Is.Not.Empty,
                    $"The held node's ribbon did not load in the {view} view.");

                foreach (var ribbon in ribbons)
                {
                    AssertBoostedRibbonPixel(ribbon, preview, source, 2f, $"{view} before boost insert");
                }

                boost = SpawnBoost(3f);
                foreach (var ribbon in ribbons)
                {
                    AssertBoostedRibbonPixel(ribbon, preview, source, 3.5f, $"{view} after boost insert");
                }

                var selection = UnityEngine.Object.FindAnyObjectByType<SelectionController>();
                SelectionController.DeselectAll();
                SelectionController.Select(boost, true, false, false);
                Assert.That(
                    SelectionController.SelectedObjects.Contains(boost),
                    Is.True,
                    "Selection rejected the spawned boost event.");
                selection.MoveSelection(3f);
                boost = map.Events.Single(evt => evt.IsColorBoostEvent() && evt.JsonTime == 6f);
                foreach (var ribbon in ribbons)
                {
                    AssertBoostedRibbonPixel(ribbon, preview, source, 3.5f, $"{view} after boost move (before it)");
                    AssertBoostedRibbonPixel(ribbon, preview, source, 6.5f, $"{view} after boost move (after it)");
                }

                BeatmapActionContainer.AddAction(new SelectionDeletedAction(new[] { boost }), true);
                boost = null;
                foreach (var ribbon in ribbons)
                {
                    AssertBoostedRibbonPixel(ribbon, preview, source, 6.5f, $"{view} after boost removal");
                }
            }
            finally
            {
                appearance.RedBoostColor = originalBoostColor;
                SelectionController.DeselectAll();
                var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
                if (provider != null)
                {
                    provider.LastContext = null;
                    provider.GroupContext = null;
                }

                if (boost != null && map.Events.Contains(boost))
                {
                    BeatmapActionContainer.AddAction(new SelectionDeletedAction(new[] { boost }), true);
                }

                if (group != null && map.LightColorEventBoxGroups.Contains(group))
                {
                    BeatmapActionContainer.AddAction(new SelectionDeletedAction(new[] { group }), true);
                }

                BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            }
        }

        // Outer non-primary nodes upload through ConfigureAsPreviewGhost, not the parent's stored ribbon
        // inputs; a held ghost node also needs the boost index for its immediate upload.
        // Inserting the boost after load drives the synchronous RefreshBoostDependentAppearances path.
        [Test]
        public void HeldPaletteGhostRibbonSwapsAtBoostBoundary()
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 1);
            var collection = GetGroupCollection();
            var map = BeatSaberSongContainer.Instance.Map;
            var appearance = preview.EventAppearance;
            var originalBoostColor = appearance.RedBoostColor;
            appearance.RedBoostColor = new Color(0.2f, 0.85f, 0.3f, 1f);
            BaseLightColorEventBoxGroup group = null;
            BaseEvent boost = null;
            try
            {
                collection.EditContext.EditingMode = EditingMode.GLS;
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
                ConfigureOuterColorPage();

                var primary = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                };
                var held = new BaseLightColorBase
                {
                    Color = (int)LightColor.Red,
                    Brightness = 1f,
                    RelativeJsonTime = 1f,
                };
                group = CreateGroup(1f, PrimaryGroupId, 0, primary, held);
                SpawnGroup(group);

                var heldSource = group.Boxes[0].Events[1];
                var ribbons = FindHeldRibbonControllers(RibbonView.Outer, heldSource);
                Assert.That(
                    ribbons,
                    Is.Not.Empty,
                    "The held node's outer ghost ribbon did not load.");

                foreach (var ribbon in ribbons)
                {
                    AssertBoostedRibbonPixel(ribbon, preview, heldSource, 3f, "ghost before boost insert");
                }

                boost = SpawnBoost(4f);
                foreach (var ribbon in ribbons)
                {
                    AssertBoostedRibbonPixel(ribbon, preview, heldSource, 3f, "ghost before boost");
                    AssertBoostedRibbonPixel(ribbon, preview, heldSource, 4.5f, "ghost inside boost");
                }
            }
            finally
            {
                appearance.RedBoostColor = originalBoostColor;
                SelectionController.DeselectAll();
                var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
                if (provider != null)
                {
                    provider.LastContext = null;
                    provider.GroupContext = null;
                }

                if (boost != null && map.Events.Contains(boost))
                {
                    BeatmapActionContainer.AddAction(new SelectionDeletedAction(new[] { boost }), true);
                }

                if (group != null && map.LightColorEventBoxGroups.Contains(group))
                {
                    BeatmapActionContainer.AddAction(new SelectionDeletedAction(new[] { group }), true);
                }

                BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            }
        }

        private static BaseEvent SpawnBoost(float jsonTime)
        {
            var boost = new BaseEvent
            {
                JsonTime = jsonTime,
                Type = (int)EventTypeValue.ColorBoostEventType,
                Value = 1,
            };
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    boost,
                    Array.Empty<BaseObject>(),
                    "Placed a color boost event."),
                true);
            return boost;
        }

        // Read the loaded containers that already render the held source without refreshing them; the
        // inner grid binds EventData while outer ghosts bind PreviewEventData under their drag owner.
        private static List<LightGradientController> FindHeldRibbonControllers(
            RibbonView view,
            BaseLightColorBase source)
        {
            var controllers = new List<LightGradientController>();
            if (view == RibbonView.Inner)
            {
                var innerCollection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                foreach (var container in innerCollection.LoadedContainers.Values.OfType<GLSEventContainer>())
                {
                    if (ReferenceEquals(container.EventData, source))
                    {
                        controllers.Add(container.LightGradientController);
                    }
                }

                return controllers;
            }

            var outerCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            var ownedParents = new HashSet<GLSGroupContainer>(
                outerCollection.LoadedContainers.Values.OfType<GLSGroupContainer>()
                    .Where(container => ReferenceEquals(container.EventBoxGroupData, source.EventBoxGroupData)));
            var trackParents = new HashSet<Transform>();
            var siblings = new List<GLSGroupContainer>();
            foreach (var parent in ownedParents)
            {
                if (!trackParents.Add(parent.transform.parent))
                {
                    continue;
                }

                siblings.Clear();
                parent.transform.parent.GetComponentsInChildren(true, siblings);
                foreach (var container in siblings)
                {
                    if (ownedParents.Contains(container.DragTarget)
                        && ReferenceEquals(container.PreviewEventData, source)
                        && container.lightGradientController != null)
                    {
                        controllers.Add(container.lightGradientController);
                    }
                }
            }

            return controllers;
        }

        // Render the uploaded ribbon at one beat and compare it against the production tween
        // re-resolved with both endpoints under the live boost state at that beat: the game's palette swap
        // changes endpoints together instead of blending between them.
        private static void AssertBoostedRibbonPixel(
            LightGradientController controller,
            GlsColorScenarioContext preview,
            BaseLightColorBase source,
            float jsonTime,
            string context)
        {
            var renderer = controller.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null, $"{context}: the ribbon had no mesh renderer.");
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(
                properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")),
                Is.EqualTo(1f),
                $"{context}: the ribbon uploaded no per-light timeline.");
            var material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
            try
            {
                var map = BeatSaberSongContainer.Instance.Map;
                var songBpmTime = (float)map.JsonTimeToSongBpmTime(jsonTime);
                var progress = (songBpmTime - controller.ColorTimelineStart)
                    / controller.ColorTimelineDuration;
                var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, 0.5f).gamma;

                var eventCollection = BeatmapObjectContainerCollection
                    .GetCollectionForType<EventGridContainer>(ObjectType.Event);
                var sampleBoost = eventCollection.IsBoostAt(jsonTime);
                var timeline = GLSEventCommon.GetColorTimeline(source, preview.Effect.Count);
                Assert.That(timeline, Is.Not.Null, $"{context}: no color timeline for the held source.");
                Assert.That(
                    timeline.TryGetOutgoing(source, preview.LightId, out var state),
                    Is.True,
                    $"{context}: no outgoing ribbon state for the held source.");
                var expected = new LightColorTween();
                timeline.ConfigureTween(expected, state, preview.EventAppearance, _ => sampleBoost);
                expected.UpdateTime(songBpmTime);
                var expectedPixel = GLSColorTransitionCacheTest.CalculateExpectedRibbonPixel(expected.Color);
                var message = $"{context} at JSON beat {jsonTime} with boost "
                    + (sampleBoost ? "on" : "off")
                    + $": expected {GLSRibbonTestDiagnostics.FormatColor(expectedPixel)}, "
                    + $"rendered {GLSRibbonTestDiagnostics.FormatColor(pixel)}.";
                Assert.That(pixel.r, Is.EqualTo(expectedPixel.r).Within(0.02f), message);
                Assert.That(pixel.g, Is.EqualTo(expectedPixel.g).Within(0.02f), message);
                Assert.That(pixel.b, Is.EqualTo(expectedPixel.b).Within(0.02f), message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static IEnumerable<EasingTriple> GetEasingTriples()
        {
            foreach (var first in easingValues)
            foreach (var second in easingValues)
            foreach (var third in easingValues)
                yield return new EasingTriple(first, second, third);
        }

        private static void ApplyScenarioEasings(EasingTriple easings)
        {
            var groups = FindScenarioGroups(PrimaryGroupId)
                .Concat(FindScenarioGroups(SecondaryGroupId))
                .ToArray();
            foreach (var group in groups)
            {
                var replacement = BeatmapFactory.Clone(group) as BaseLightColorEventBoxGroup;
                var changed = false;
                foreach (var evt in replacement.Boxes.SelectMany(box => box.Events))
                {
                    if (!TryGetLabel(evt, out var label))
                    {
                        continue;
                    }

                    var easing = label switch
                    {
                        'A' => easings.A,
                        'B' => easings.B,
                        _ => easings.C
                    };
                    if (evt.Easing == easing)
                    {
                        continue;
                    }

                    evt.Easing = easing;
                    changed = true;
                }

                if (changed)
                {
                    GLSCommonCommand.TriggerModifyEventBoxAction(
                        group,
                        replacement,
                        ActionMergeType.ModifyGLSEventEasing);
                }
            }

            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        private static MutationScenario CreateMovementScenario(
            RibbonView view,
            MovementOperation operation,
            ChronologicalDestination destination,
            EasingTriple easings)
        {
            if (operation == MovementOperation.LaneShift)
            {
                return CreateLaneShiftScenario(view, destination, easings);
            }

            var scenario = view == RibbonView.Inner
                ? CreateInnerScenario("ABC", easings, MovedLabel(destination))
                : CreateOuterScenario("ABC", easings, MovedLabel(destination));
            scenario.DestinationTime = DestinationTime(destination);
            return scenario;
        }

        private static MutationScenario CreatePositionalScenario(
            RibbonView view,
            ChronologicalDestination position,
            EasingTriple easings) => view == RibbonView.Inner
                ? CreateInnerScenario(ExpectedOrder(position), easings)
                : CreateOuterScenario(ExpectedOrder(position), easings);

        private static MutationScenario CreateInnerScenario(string order, EasingTriple easings)
            => CreateInnerScenario(order, easings, order is "ACB" or "CAB" ? 'C' : 'A');

        private static MutationScenario CreateInnerScenario(string order, EasingTriple easings, char movedLabel)
        {
            // Inner selection, deletion, and paste commands are enabled only on the Event Box editing tab.
            GetGroupCollection().EditContext.EditingMode = EditingMode.EventBox;
            var group = CreateGroup(1f, PrimaryGroupId, 0, CreateOrderedEvents(order, easings));
            SpawnGroup(group);
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            return new MutationScenario(
                RibbonView.Inner,
                movedLabel,
                group.ID);
        }

        private static MutationScenario CreateOuterScenario(string order, EasingTriple easings)
            => CreateOuterScenario(order, easings, order is "ACB" or "CAB" ? 'C' : 'A');

        private static MutationScenario CreateOuterScenario(string order, EasingTriple easings, char movedLabel)
        {
            // Outer group commands and preview containers are owned by the GLS editing tab.
            GetGroupCollection().EditContext.EditingMode = EditingMode.GLS;
            // A nonzero authored preview opacity is what enables each outer node's represented-event ribbon.
            Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
            ConfigureOuterColorPage();
            for (var index = 0; index < order.Length; index++)
            {
                var label = order[index];
                var group = CreateGroup(2f + (index * 2f), PrimaryGroupId, 0, CreateEvent(label, easings));
                SpawnGroup(group);
            }

            return new MutationScenario(
                RibbonView.Outer,
                movedLabel,
                PrimaryGroupId);
        }

        private static MutationScenario CreateLaneShiftScenario(
            RibbonView view,
            ChronologicalDestination destination,
            EasingTriple easings)
        {
            // Lane-shift selection uses the same tab-specific edit permissions as direct movement.
            GetGroupCollection().EditContext.EditingMode = view == RibbonView.Inner
                ? EditingMode.EventBox
                : EditingMode.GLS;
            // Outer lane-shift cases require the same visible represented-event preview as ordinary outer cases.
            if (view == RibbonView.Outer)
            {
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
                ConfigureOuterColorPage();
            }

            var movedLabel = destination is ChronologicalDestination.BackwardBetween or ChronologicalDestination.BackwardFirst
                ? 'C'
                : 'A';
            var destinationOrder = ExpectedOrder(destination).Replace(movedLabel.ToString(), string.Empty);
            var movedTime = LaneDestinationTime(destination);

            if (view == RibbonView.Inner)
            {
                var destinationEvents = CreateEventsAtFinalOrderSlots(
                    ExpectedOrder(destination),
                    movedLabel,
                    easings,
                    1f);
                var sourceEvent = CreateEvent(movedLabel, easings);
                sourceEvent.RelativeJsonTime = movedTime - 1f;
                var group = new BaseLightColorEventBoxGroup
                {
                    JsonTime = 1f,
                    ID = PrimaryGroupId,
                    Boxes =
                    {
                        CreateBox(0, destinationEvents),
                        CreateBox(1, new[] { sourceEvent })
                    }
                };
                group.NormalizeLoadedEventConflicts();
                SpawnGroup(group);
                var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
                provider.LastContext = null;
                provider.GroupContext = group;
                return new MutationScenario(view, movedLabel, PrimaryGroupId) { LaneDirection = -1 };
            }

            for (var index = 0; index < destinationOrder.Length; index++)
            {
                var destinationLabel = destinationOrder[index];
                var finalIndex = ExpectedOrder(destination).IndexOf(destinationLabel);
                SpawnGroup(CreateGroup(
                    2f + (finalIndex * 2f),
                    PrimaryGroupId,
                    0,
                    CreateEvent(destinationLabel, easings)));
            }

            // Keep the light filter unchanged so Ctrl+Left/Right must move the group between lanes rather
            // than change the sampled light.
            SpawnGroup(CreateGroup(movedTime, SecondaryGroupId, 0, CreateEvent(movedLabel, easings)));
            // Teardown can change track insertion order; find the actual Ctrl+Arrow direction from the
            // current lane positions.
            var pair = GetColorLanePair();
            var colorLaneIds = GetGroupCollection().BeatmapContext.TrackDefinitions.Gls
                .Where(entry => entry.Value.ColorTrack && entry.Value.Group == pair.Group)
                .Select(entry => entry.Key)
                .ToArray();
            var laneDirection = Array.IndexOf(colorLaneIds, pair.Primary)
                - Array.IndexOf(colorLaneIds, pair.Secondary);
            return new MutationScenario(view, movedLabel, PrimaryGroupId) { LaneDirection = laneDirection };
        }

        private static void ApplyMovement(MutationScenario scenario, MovementOperation operation)
        {
            switch (operation)
            {
                case MovementOperation.AltDrag:
                    AltDragMovedObject(scenario);
                    break;
                case MovementOperation.TimeShift:
                    SelectMovedObject(scenario);
                    scenario.Selection
                        .MoveSelection(scenario.DestinationTime - GetMovedObject(scenario).JsonTime, true);
                    break;
                case MovementOperation.LaneShift:
                    SelectMovedObject(scenario);
                    scenario.Selection
                        .ShiftSelection(scenario.LaneDirection, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        private static void AltDragMovedObject(MutationScenario scenario)
        {
            var moved = GetMovedObject(scenario);
            if (scenario.View == RibbonView.Inner)
            {
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                // The test scene retains inactive placement templates; select the initialized instance bound to this collection.
                var placement = UnityEngine.Object.FindObjectsByType<GLSEventColorPlacement>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(candidate => ReferenceEquals(candidate.ObjectContainerCollection, collection))
                    .OrderByDescending(candidate => candidate.isActiveAndEnabled)
                    .FirstOrDefault();
                Assert.That(placement, Is.Not.Null);
                // Build the drag hit container exactly as PlacementInputSystem does; pooled loaded containers may be
                // absent when the shared test playhead has just sampled the final ribbon interval.
                placement.Initialize(null);
                var dragContainer = collection.CreateContainer() as GLSEventContainer;
                Assert.That(dragContainer, Is.Not.Null);
                dragContainer.ObjectData = moved;
                dragContainer.Setup();
                dragContainer.UpdateGridPosition();
                Assert.That(placement.StartDrag(dragContainer.gameObject), Is.Not.Null);
                placement.DraggedObjectData.RelativeJsonTime = scenario.DestinationTime
                    - placement.DraggedObjectData.EventBoxGroupData.JsonTime;
                placement.DraggedObjectData.RecomputeSongBpmTime();
                placement.FinishDrag();
                return;
            }

            // Find the current parent in the map; a replaced child can still reference the old group
            // instance.
            var group = GetOwningGroup(moved);
            var groupCollection = GetGroupCollection();
            var groupPlacement = GetOuterColorPlacement(groupCollection);
            // Build the hit container directly because the shared test viewport may have pooled the authored outer node.
            var groupContainer = groupCollection.CreateContainer() as GLSGroupContainer;
            Assert.That(groupContainer, Is.Not.Null);
            groupContainer.ObjectData = group;
            groupContainer.Setup();
            Assert.That(groupPlacement.StartDrag(groupContainer.gameObject), Is.Not.Null);
            groupPlacement.DraggedObjectData.JsonTime = scenario.DestinationTime;
            RecomputeGroupEventTimes(groupPlacement.DraggedObjectData);
            groupPlacement.FinishDrag();
        }

        private static void CutMovedObject(MutationScenario scenario)
        {
            SelectMovedObject(scenario);
            scenario.Selection.Copy(true);
        }

        private static void PasteMovedObject(MutationScenario scenario)
        {
            if (scenario.View == RibbonView.Inner)
            {
                var placement = UnityEngine.Object.FindAnyObjectByType<GLSEventColorPlacement>();
                var group = FindScenarioGroups(scenario.GroupId).Single();
                placement.State = PlacementState.Active;
                placement.QueuedData.EventBoxGroupData = group;
                placement.QueuedData.EventBoxData = group.Boxes[0];
                placement.QueuedData.BoxIndex = 0;
                placement.QueuedData.RelativeJsonTime = scenario.DestinationTime - group.JsonTime;
                placement.QueuedData.RecomputeSongBpmTime();
            }
            else
            {
                var placement = GetOuterColorPlacement(GetGroupCollection());
                placement.State = PlacementState.Active;
                placement.QueuedData.ID = scenario.GroupId;
                placement.QueuedData.JsonTime = scenario.DestinationTime;
            }

            scenario.Selection.Paste();
        }

        private static void DeleteMovedObject(MutationScenario scenario)
        {
            SelectMovedObject(scenario);
            scenario.Selection.Delete();
        }

        private static BaseLightColorBase ApplyHoverMutation(
            MutationScenario scenario,
            HoverMutation mutation,
            HoverInputSession inputSession)
        {
            var moved = GetMovedObject(scenario);
            var originalEvent = BeatmapFactory.Clone(moved) as BaseLightColorBase;
            GameObject containerObject = null;
            GameObject controllerObject = null;
            try
            {
                containerObject = new GameObject($"{scenario.View} GLS ribbon hover target");
                controllerObject = new GameObject($"{scenario.View} GLS ribbon hover controller");
                CMInput.IGLSColorObjectsActions controller;
                if (scenario.View == RibbonView.Inner)
                {
                    GetGroupCollection().EditContext.EditingMode = EditingMode.EventBox;
                    var container = containerObject.AddComponent<GLSEventContainer>();
                    container.VisualSettings = inputSession.VisualSettings;
                    container.EventData = moved;
                    SetHighlightedWithoutVisualRefresh(container);
                    var innerController = controllerObject.AddComponent<TestGLSEventColorInputController>();
                    innerController.IsHovering = true;
                    innerController.HoveredObject = container;
                    innerController.RaycastTarget = container;
                    innerController.SetPrecision(inputSession.Precision);
                    controller = innerController;
                }
                else
                {
                    GetGroupCollection().EditContext.EditingMode = EditingMode.GLS;
                    var container = containerObject.AddComponent<GLSGroupContainer>();
                    container.VisualSettings = inputSession.VisualSettings;
                    // After placement, hover edits must target the parent instance currently stored in the
                    // map.
                    container.EventBoxGroupData = GetOwningGroup(moved);
                    container.PreviewEventData = moved;
                    var outerController = controllerObject.AddComponent<TestGLSGroupColorInputController>();
                    outerController.IsHovering = true;
                    outerController.HoveredObject = container;
                    outerController.RaycastTarget = container;
                    controller = outerController;
                }

                inputSession.Input.GLSColorObjects.Disable();
                inputSession.Input.GLSColorObjects.SetCallbacks(controller);
                inputSession.Input.GLSColorObjects.Enable();
                SendHoverScroll(inputSession.Fixture, inputSession.Keyboard, inputSession.Mouse, mutation);
                AssertHoverMutationApplied(originalEvent, GetMovedObject(scenario), mutation);
            }
            finally
            {
                if (controllerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(controllerObject);
                }

                if (containerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(containerObject);
                }
            }

            return originalEvent;
        }

        private sealed class HoverInputSession : IDisposable
        {
            private readonly CMInput sharedInput;
            private readonly bool sharedMapWasEnabled;

            public HoverInputSession()
            {
                VisualSettings = GetInitializedVisualSettings();
                Precision = UnityEngine.Object.FindAnyObjectByType<ScrollPrecisionController>();
                sharedInput = CMInputCallbackInstaller.InputInstance;
                Assert.That(sharedInput, Is.Not.Null);
                sharedMapWasEnabled = sharedInput.GLSColorObjects.enabled;
                sharedInput.GLSColorObjects.Disable();
                Fixture = new IsolatedInputTestFixture();
                Fixture.Setup();
                Input = new CMInput();
                Keyboard = InputSystem.AddDevice<Keyboard>();
                Mouse = InputSystem.AddDevice<Mouse>();
            }

            public InputTestFixture Fixture { get; }
            public CMInput Input { get; }
            public Keyboard Keyboard { get; }
            public Mouse Mouse { get; }
            public VisualSettingsSO VisualSettings { get; }
            public ScrollPrecisionController Precision { get; }

            public void Dispose()
            {
                Input.GLSColorObjects.Disable();
                Input.Dispose();
                Fixture.TearDown();
                if (sharedMapWasEnabled)
                {
                    sharedInput.GLSColorObjects.Enable();
                }
            }
        }

        private static void AssertHoverMutationApplied(
            BaseLightColorBase original,
            BaseLightColorBase edited,
            HoverMutation mutation)
        {
            switch (mutation)
            {
                case HoverMutation.Brightness:
                    Assert.That(edited.Brightness, Is.Not.EqualTo(original.Brightness));
                    break;
                case HoverMutation.StrobeFrequency:
                    Assert.That(
                        edited.Frequency != original.Frequency
                        || edited.ChromaStrobeInterval != original.ChromaStrobeInterval,
                        Is.True);
                    break;
                case HoverMutation.StrobeBrightness:
                    Assert.That(edited.StrobeBrightness, Is.Not.EqualTo(original.StrobeBrightness));
                    break;
                case HoverMutation.StrobeFade:
                    Assert.That(edited.StrobeFade, Is.Not.EqualTo(original.StrobeFade));
                    break;
                case HoverMutation.Easing:
                    Assert.That(
                        edited.Easing != original.Easing
                        || edited.ChromaColorEasing != original.ChromaColorEasing,
                        Is.True);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null);
            }
        }

        private static void AssertColorEventPropertiesEqual(
            BaseLightColorBase expected,
            BaseLightColorBase actual,
            string checkpoint)
        {
            Assert.That(actual.Brightness, Is.EqualTo(expected.Brightness), checkpoint);
            Assert.That(actual.Frequency, Is.EqualTo(expected.Frequency), checkpoint);
            Assert.That(actual.ChromaStrobeInterval, Is.EqualTo(expected.ChromaStrobeInterval), checkpoint);
            Assert.That(actual.StrobeBrightness, Is.EqualTo(expected.StrobeBrightness), checkpoint);
            Assert.That(actual.StrobeFade, Is.EqualTo(expected.StrobeFade), checkpoint);
            Assert.That(actual.Easing, Is.EqualTo(expected.Easing), checkpoint);
            Assert.That(actual.ChromaColorEasing, Is.EqualTo(expected.ChromaColorEasing), checkpoint);
        }

        private static void SendHoverScroll(
            InputTestFixture inputFixture,
            Keyboard keyboard,
            Mouse mouse,
            HoverMutation mutation)
        {
            // Queue the complete chord as one device state; separate queued key events overwrite earlier modifiers.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(HoverModifiers(mutation).ToArray()));
            InputSystem.Update();
            inputFixture.Set(mouse.scroll, new Vector2(0f, 1f), queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Set(mouse.scroll, Vector2.zero, queueEventOnly: true);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
        }

        private static IEnumerable<Key> HoverModifiers(HoverMutation mutation) => mutation switch
        {
            HoverMutation.Brightness => new[] { Key.LeftAlt },
            HoverMutation.StrobeFrequency => new[] { Key.LeftAlt, Key.LeftCtrl },
            HoverMutation.StrobeBrightness => new[] { Key.LeftAlt, Key.LeftCtrl, Key.LeftShift },
            HoverMutation.StrobeFade => new[] { Key.LeftShift },
            HoverMutation.Easing => new[] { Key.LeftCtrl, Key.LeftShift },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null)
        };

        private void AssertScenario(
            MutationScenario scenario,
            GlsColorScenarioContext preview,
            string checkpoint)
        {
            AssertAllCurrentGlsIntervalsMatchExpected(
                preview,
                $"{scenario.View}, moved {scenario.MovedLabel}, {checkpoint}");
        }

        private static void AssertExpectedOrder(MutationScenario scenario, string expected)
        {
            // Materialize the map snapshot before NUnit unwinds the failed case and cleanup restores prior groups.
            var mapSnapshot = BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups
                .Where(group => group.Boxes.SelectMany(box => box.Events).Any(evt => TryGetLabel(evt, out _)))
                .Select(group => $"{group.ID}@{group.JsonTime}:"
                    + string.Join("/", group.Boxes.Select(box => $"f{box.IndexFilter.Param0}={string.Concat(box.Events.Select(GetLabel))}")))
                .ToArray();
            var actual = FindScenarioGroups(scenario.GroupId)
                .SelectMany(group => group.Boxes)
                .Where(box => box.IndexFilter.Param0 == 0)
                .SelectMany(box => box.Events)
                .OrderBy(evt => evt.EventBoxGroupData.JsonTime + evt.RelativeJsonTime)
                .Select(GetLabel)
                .ToArray();
            Assert.That(
                string.Concat(actual),
                Is.EqualTo(expected),
                $"{scenario}; map snapshot: {string.Join(", ", mapSnapshot)}");
        }

        private static void SelectMovedObject(MutationScenario scenario)
        {
            SelectionController.DeselectAll();
            var moved = GetMovedObject(scenario);
            // Outer editing selects the map-owned group; child ownership back-references can lag replacement normalization.
            BaseObject selectedObject = scenario.View == RibbonView.Inner ? moved : GetOwningGroup(moved);
            SelectionController.Select(
                selectedObject,
                true,
                false,
                false);
            // A simulated keyboard edit is meaningful only if the same production selection gate accepted its target.
            Assert.That(
                SelectionController.SelectedObjects.Contains(selectedObject),
                Is.True,
                $"Selection rejected {selectedObject.GetType().Name} from {selectedObject.ObjectType} collection.");
        }

        private static BaseLightColorEventBoxGroup GetOwningGroup(BaseLightColorBase evt) =>
            BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups.Single(group => group.Boxes
                .SelectMany(box => box.Events)
                .Any(candidate => ReferenceEquals(candidate, evt)));

        private static BaseLightColorBase GetMovedObject(MutationScenario scenario)
        {
            var groups = scenario.View == RibbonView.Inner
                ? FindScenarioGroups(scenario.GroupId)
                : FindScenarioGroups(PrimaryGroupId).Concat(FindScenarioGroups(SecondaryGroupId));
            var events = groups.SelectMany(group => group.Boxes)
                .SelectMany(box => box.Events)
                .ToArray();
            var matches = events.Where(evt => TryGetLabel(evt, out var label) && label == scenario.MovedLabel).ToArray();
            Assert.That(
                matches.Length,
                Is.EqualTo(1),
                $"Expected one moved {scenario.MovedLabel} event, found {matches.Length}; fixture events: "
                + string.Join(", ", events.Select(evt => $"{evt.CustomColor}@{evt.JsonTime}")));
            return matches[0];
        }

        private static BaseLightColorEventBoxGroup CreateGroup(
            float jsonTime,
            int groupId,
            int lightId,
            params BaseLightColorBase[] events)
        {
            var group = new BaseLightColorEventBoxGroup
            {
                JsonTime = jsonTime,
                ID = groupId,
                Boxes = { CreateBox(lightId, events) }
            };
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        private static BaseLightColorEventBox CreateBox(int lightId, IEnumerable<BaseLightColorBase> events) => new()
        {
            IndexFilter = new BaseIndexFilter
            {
                Type = (int)IndexFilterType.StepAndOffset,
                Param0 = lightId,
                Param1 = 0
            },
            Events = events.OrderBy(evt => evt.RelativeJsonTime).ToArray()
        };

        private static BaseLightColorBase[] CreateOrderedEvents(string order, EasingTriple easings) => order
            .Select((label, index) =>
            {
                var evt = CreateEvent(label, easings);
                evt.RelativeJsonTime = 1f + (index * 2f);
                return evt;
            })
            .ToArray();

        private static BaseLightColorBase[] CreateEventsAtFinalOrderSlots(
            string finalOrder,
            char excluded,
            EasingTriple easings,
            float relativeOffset) => finalOrder
            .Select((label, index) => (label, index))
            .Where(entry => entry.label != excluded)
            .Select(entry =>
            {
                var evt = CreateEvent(entry.label, easings);
                evt.RelativeJsonTime = relativeOffset + (entry.index * 2f);
                return evt;
            })
            .ToArray();

        private static BaseLightColorBase CreateEvent(char label, EasingTriple easings)
        {
            var evt = new BaseLightColorBase
            {
                Color = (int)LightColor.White,
                Brightness = label switch { 'A' => 0.35f, 'B' => 0.7f, _ => 1.1f },
                StrobeBrightness = 0.5f,
                CustomColor = label switch { 'A' => colorA, 'B' => colorB, _ => colorC },
                Easing = label switch { 'A' => easings.A, 'B' => easings.B, _ => easings.C }
            };
            evt.WriteCustom();
            return evt;
        }

        // Fixture placement uses the real placement action so the action-driven GLS light manager and rendered collection receive identical data.
        private static void SpawnGroup(BaseLightColorEventBoxGroup group) =>
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    group,
                    Array.Empty<BaseObject>(),
                    "Placed a GLS ribbon mutation fixture."),
                true);

        private static void RecomputeGroupEventTimes(BaseLightColorEventBoxGroup group)
        {
            foreach (var evt in group.Boxes.SelectMany(box => box.Events))
                evt.RecomputeSongBpmTime();
        }

        private void Undo() => actionContainer.Undo();

        private static void CleanupScenario()
        {
            SelectionController.DeselectAll();
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = null;
            var groups = FindScenarioGroups(PrimaryGroupId).Concat(FindScenarioGroups(SecondaryGroupId)).ToArray();
            // Fixture removal also uses an action so incremental live-light state is empty before the next easing permutation.
            if (groups.Length > 0)
            {
                BeatmapActionContainer.AddAction(new SelectionDeletedAction(groups), true);
            }

            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            SelectionController.CopiedObjects.Clear();
        }

        // Use distinctive event colors to find fixture groups after cloning; placement rewrites unsupported
        // group customData.
        private static IEnumerable<BaseLightColorEventBoxGroup> FindScenarioGroups(int groupId) =>
            BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups
                .Where(group => group.ID == groupId && group.Boxes
                    .SelectMany(box => box.Events)
                    .Any(evt => TryGetLabel(evt, out _)));

        private static GLSGroupColorGridContainer GetGroupCollection() =>
            BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);

        private static (int Primary, int Secondary, string Group) GetColorLanePair()
        {
            var runtime = GetGroupCollection().BeatmapContext;
            Assert.That(runtime, Is.Not.Null);
            const string fixturePage = "GLS ribbon mutation fixtures";
            if (!runtime.TrackDefinitions.Gls.ContainsKey(SyntheticPrimaryGroupId))
            {
                runtime.TrackDefinitions.Gls.Add(
                    SyntheticPrimaryGroupId,
                    new TrackDefinitionGLS
                    {
                        ID = SyntheticPrimaryGroupId,
                        Group = fixturePage,
                        Name = "GLS ribbon mutation source",
                        ColorTrack = true
                    });
            }

            if (!runtime.TrackDefinitions.Gls.ContainsKey(SyntheticSecondaryGroupId))
            {
                runtime.TrackDefinitions.Gls.Add(
                    SyntheticSecondaryGroupId,
                    new TrackDefinitionGLS
                    {
                        ID = SyntheticSecondaryGroupId,
                        Group = fixturePage,
                        Name = "GLS ribbon mutation destination",
                        ColorTrack = true
                    });
            }

            return (SyntheticPrimaryGroupId, SyntheticSecondaryGroupId, fixturePage);
        }

        private static void ConfigureOuterColorPage()
        {
            var pair = GetColorLanePair();
            var runtime = GetGroupCollection().BeatmapContext;
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSGroupGridProvider>();
            Assert.That(provider, Is.Not.Null);
            // Publishing the synthetic tracks rebuilds the provider's active-ID set before outer ribbon materialization.
            runtime.NotifyTrackDefinitions();
            provider.SetGroupPage(pair.Group);
        }

        private static GLSGroupColorPlacement GetOuterColorPlacement(GLSGroupColorGridContainer collection)
        {
            var placement = UnityEngine.Object.FindObjectsByType<GLSGroupColorPlacement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(candidate => ReferenceEquals(candidate.ObjectContainerCollection, collection))
                .OrderByDescending(candidate => candidate.isActiveAndEnabled)
                .FirstOrDefault();
            Assert.That(placement, Is.Not.Null);
            return placement;
        }

        // Serialization can round-trip color channels with minor float differences, so labels use a tight component tolerance.
        private static char GetLabel(BaseLightColorBase evt)
        {
            if (TryGetLabel(evt, out var label))
            {
                return label;
            }

            Assert.Fail($"Unrecognized GLS mutation-test color {evt.CustomColor}.");
            return '?';
        }

        private static bool TryGetLabel(BaseLightColorBase evt, out char label)
        {
            if (ColorsApproximatelyEqual(evt.CustomColor, colorA))
            {
                label = 'A';
                return true;
            }

            if (ColorsApproximatelyEqual(evt.CustomColor, colorB))
            {
                label = 'B';
                return true;
            }

            if (ColorsApproximatelyEqual(evt.CustomColor, colorC))
            {
                label = 'C';
                return true;
            }

            label = default;
            return false;
        }

        private static bool ColorsApproximatelyEqual(Color? actual, Color expected) =>
            actual.HasValue
            && Mathf.Abs(actual.Value.r - expected.r) < 0.0001f
            && Mathf.Abs(actual.Value.g - expected.g) < 0.0001f
            && Mathf.Abs(actual.Value.b - expected.b) < 0.0001f
            && Mathf.Abs(actual.Value.a - expected.a) < 0.0001f;

        private static VisualSettingsSO GetInitializedVisualSettings() => UnityEngine.Object
            .FindObjectsByType<ObjectContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(container => container.VisualSettings)
            .First(settings => settings != null);

        // These hover targets contain data only, so set their highlight flag without refreshing missing
        // renderers.
        private static void SetHighlightedWithoutVisualRefresh(ObjectContainer container)
        {
            var field = typeof(ObjectContainer).GetField("highlighted", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(container, true);
        }

        private static char MovedLabel(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween or ChronologicalDestination.ForwardLast => 'A',
            ChronologicalDestination.BackwardBetween or ChronologicalDestination.BackwardFirst => 'C',
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        private static float DestinationTime(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween => 5f,
            ChronologicalDestination.ForwardLast => 7f,
            ChronologicalDestination.BackwardBetween => 3f,
            ChronologicalDestination.BackwardFirst => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        private static float LaneDestinationTime(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween => 4f,
            ChronologicalDestination.ForwardLast => 6f,
            ChronologicalDestination.BackwardBetween => 4f,
            ChronologicalDestination.BackwardFirst => 2f,
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        private static string ExpectedOrder(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween => "BAC",
            ChronologicalDestination.ForwardLast => "BCA",
            ChronologicalDestination.BackwardBetween => "ACB",
            ChronologicalDestination.BackwardFirst => "CAB",
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        public enum RibbonView
        {
            Inner,
            Outer
        }

        public enum MovementOperation
        {
            AltDrag,
            TimeShift,
            CutPaste,
            LaneShift
        }

        public enum ChronologicalDestination
        {
            ForwardBetween,
            ForwardLast,
            BackwardBetween,
            BackwardFirst
        }

        public enum HoverMutation
        {
            Brightness,
            StrobeFrequency,
            StrobeBrightness,
            StrobeFade,
            Easing
        }

        private readonly struct EasingTriple
        {
            public EasingTriple(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }

            public int A { get; }
            public int B { get; }
            public int C { get; }
        }

        private sealed class MutationScenario
        {
            public MutationScenario(RibbonView view, char movedLabel, int groupId)
            {
                View = view;
                MovedLabel = movedLabel;
                GroupId = groupId;
                Selection = UnityEngine.Object.FindAnyObjectByType<SelectionController>();
            }

            public RibbonView View { get; }
            public char MovedLabel { get; }
            public int GroupId { get; }
            public SelectionController Selection { get; }
            public float DestinationTime { get; set; }
            public int LaneDirection { get; set; }

            public override string ToString() => $"{View} moved={MovedLabel} group={GroupId}";
        }

        private sealed class TestGLSEventColorInputController : BeatmapGLSEventColorInputController
        {
            public GLSEventContainer RaycastTarget;

            public void SetPrecision(ScrollPrecisionController precision) => ScrollPrecisionController = precision;

            protected override bool RaycastFirstObject(out GLSEventContainer firstObject)
            {
                firstObject = RaycastTarget;
                return firstObject != null;
            }
        }

        private sealed class TestGLSGroupColorInputController : BeatmapGLSGroupColorInputController
        {
            public GLSGroupContainer RaycastTarget;

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                return firstObject != null;
            }
        }
    }
}
