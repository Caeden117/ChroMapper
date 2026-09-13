using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    public class EventNextPrevTest : TestBase
    {
        // Basic movement cache regressions need samples before, during, and after every edited interval in both scrub directions.
        private static readonly float[] MovementPreviewSampleJsonTimes =
        {
            0.5f, 1.25f, 2.25f, 3.75f, 4.25f, 5.5f, 6.05f, 6.5f, 7.75f,
            8.25f, 9.5f, 10.25f, 11.75f, 12.25f, 14f, 9f, 5f, 2.5f
        };

        // Multi-node jump regressions need a rendered checkpoint after every crossed movement event in both scrub directions.
        private static readonly float[] MovementMultiNodeSampleJsonTimes =
        {
            0.5f, 2.25f, 3.25f, 4.75f, 5.25f, 6.75f, 7.25f, 8.75f,
            9.25f, 10.75f, 11.25f, 12.25f, 13.25f, 15f, 10f, 6f, 2.5f
        };

        // Laser-speed movement rows alternate active speeds and explicit stops without allocating per placed event.
        private static readonly int[] LaserMovementValues = { 1, 0, 3, 0, 5 };

        // Track every dedicated preview light so multi-track cache tests can unregister all of them safely.
        private readonly List<GameObject> livePreviewLightObjects = new();
        private readonly List<GameObject> basicEventHoverObjects = new();
        private bool? emulateChromaLiteBeforePreviewTest;
        private bool restrictedEventPoolForPreviewTest;
        private PlacementState? eventPlacementStateBeforePreviewTest;
        private EditingMode? editingModeBeforePreviewTest;
        private int? gridMeasureSnappingBeforePreviewTest;
        // Preserve the user's visual loading radius while chunk-boundary regressions use a narrow deterministic window.
        private int? chunkDistanceBeforePreviewTest;
        // Restore the shared propagation view after paste tests hover its synthetic All Lights lane.
        private EventGridContainer.PropMode? propagationEditingBeforePasteTest;
        private int? propagatedEventTypeBeforePasteTest;
        // Movement regressions render shared ring/laser transforms, so teardown must restore them after event cleanup.
        private bool restoreMovementPreviewAfterTest;
        // Own one immediately destroyed input asset per test so physical shortcuts have exactly one production callback target.
        private CMInput basicEventInteractionInput;
        private SelectionController basicEventInputSelectionController;
        private BeatmapActionContainer basicEventInputActionContainer;
        private bool? sharedModifyingSelectionInputWasEnabled;
        private bool? sharedActionsInputWasEnabled;
        // Retain action/device evidence when a physical shortcut fails before creating its expected beatmap action.
        private string lastPhysicalShortcutDiagnostics;

        protected override void AfterCleanup()
        {
            if (emulateChromaLiteBeforePreviewTest.HasValue)
            {
                // Restore the shared preview setting because these regressions require custom light colors.
                Settings.Instance.EmulateChromaLite = emulateChromaLiteBeforePreviewTest.Value;
                emulateChromaLiteBeforePreviewTest = null;
            }

            foreach (var livePreviewLightObject in livePreviewLightObjects)
            {
                if (livePreviewLightObject == null) continue;

                // Remove the test light after map cleanup so later shared-fixture tests cannot retain its effect state.
                var previewLight = livePreviewLightObject.GetComponent<LivePreviewLightController>();
                var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
                context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(previewLight.Type)
                    .Unregister(previewLight);
                context.Descriptor.BasicEventEffectManager.Reinitialize();
                Object.DestroyImmediate(livePreviewLightObject);
            }

            livePreviewLightObjects.Clear();

            if (restoreMovementPreviewAfterTest)
            {
                // Movement cache tests must leave the shared simulator and scene at the empty-map beat-zero baseline.
                var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
                context.Descriptor.BasicEventEffectManager.Reinitialize();
                context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0f);
                restoreMovementPreviewAfterTest = false;
            }

            foreach (var basicEventHoverObject in basicEventHoverObjects)
            {
                // Remove synthetic grid surfaces after the real placement hover path no longer needs them.
                Object.DestroyImmediate(basicEventHoverObject);
            }

            basicEventHoverObjects.Clear();

            if (restrictedEventPoolForPreviewTest)
            {
                // Restore ordinary viewport bounds after exercising edits with both neighbor points outside its window.
                GetEventsContainer().RefreshPool(-1f, 20f, true);
                restrictedEventPoolForPreviewTest = false;
            }

            if (editingModeBeforePreviewTest.HasValue)
            {
                // Restore the shared editor tab after exercising Basic Event clipboard filtering.
                Object.FindAnyObjectByType<EditModeContext>().EditingMode = editingModeBeforePreviewTest.Value;
                editingModeBeforePreviewTest = null;
            }

            if (eventPlacementStateBeforePreviewTest.HasValue)
            {
                // Restore shared placement state after exercising hover-anchored Basic Event paste.
                Object.FindAnyObjectByType<EventPlacement>().State = eventPlacementStateBeforePreviewTest.Value;
                eventPlacementStateBeforePreviewTest = null;
            }

            if (gridMeasureSnappingBeforePreviewTest.HasValue)
            {
                // Restore the shared grid precision after keyboard time-shift and hover tests finish.
                Object.FindAnyObjectByType<AudioTimeSyncController>().GridMeasureSnapping =
                    gridMeasureSnappingBeforePreviewTest.Value;
                gridMeasureSnappingBeforePreviewTest = null;
            }

            if (chunkDistanceBeforePreviewTest.HasValue)
            {
                // Restore the normal paused-editor chunk window after tests unload selected Basic Event visuals.
                Settings.Instance.ChunkDistance = chunkDistanceBeforePreviewTest.Value;
                GetEventsContainer().RefreshPool(true);
                chunkDistanceBeforePreviewTest = null;
            }

            if (propagationEditingBeforePasteTest.HasValue)
            {
                // Restore both propagation inputs together so the next test receives the same labels and lane mapping.
                var eventsContainer = GetEventsContainer();
                eventsContainer.EventTypeToPropagate = propagatedEventTypeBeforePasteTest.Value;
                eventsContainer.PropagationEditing = propagationEditingBeforePasteTest.Value;
                propagationEditingBeforePasteTest = null;
                propagatedEventTypeBeforePasteTest = null;
            }

            if (basicEventInteractionInput != null)
            {
                // Destroy the isolated asset immediately so synchronous cases cannot inherit deferred callbacks.
                basicEventInteractionInput.ModifyingSelection.Disable();
                basicEventInteractionInput.Actions.Disable();
                basicEventInteractionInput.ModifyingSelection.RemoveCallbacks(
                    basicEventInputSelectionController);
                basicEventInteractionInput.Actions.RemoveCallbacks(basicEventInputActionContainer);
                System.GC.SuppressFinalize(basicEventInteractionInput);
                Object.DestroyImmediate(basicEventInteractionInput.asset);
                basicEventInteractionInput = null;
                basicEventInputSelectionController = null;
                basicEventInputActionContainer = null;
            }

            if (CMInputCallbackInstaller.InputInstance != null)
            {
                // Restore only the installer-owned maps that the isolated test input temporarily replaced.
                if (sharedModifyingSelectionInputWasEnabled == true)
                {
                    CMInputCallbackInstaller.InputInstance.ModifyingSelection.Enable();
                }

                if (sharedActionsInputWasEnabled == true)
                {
                    CMInputCallbackInstaller.InputInstance.Actions.Enable();
                }
            }

            sharedModifyingSelectionInputWasEnabled = null;
            sharedActionsInputWasEnabled = null;
        }

        [Test]
        public void Placement()
        {
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            PlaceEvent(1);
            PlaceEvent(4);
            PlaceEvent(2);
            PlaceEvent(3);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after deleting
            // 1 ->   -> 3 -> 4
            var e2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 2);
            PlaceUtils.Delete(e2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            PlaceEvent(1);
            PlaceEvent(4);
            PlaceEvent(2);
            PlaceEvent(3);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after deleting
            // 1 ->   -> 3 ->
            var e2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 2);
            var e4 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 4);
            SelectionController.Select(e2);
            SelectionController.Select(e4, true);
            selectionController.Delete();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);
        }

        [Test]
        public void ShiftingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // A1 -> T2 -> A3 -> T4
            // B1 ->    -> B3 ->
            PlaceLeftLasers(1);  // A1
            PlaceLeftLasers(3);  // A3
            PlaceRightLasers(1); // B1
            PlaceRightLasers(3); // B3
            PlaceLeftLasers(2);  // T2
            PlaceLeftLasers(4);  // T4

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);

            // Check state after shifting eventT
            // A1 ->    -> A3 ->
            // B1 -> T2 -> B3 -> T4
            var t2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2f && e.Type == (int)EventTypeValue.Event2);
            var t4 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 4f && e.Type == (int)EventTypeValue.Event2);
            SelectionController.Select(t2);
            SelectionController.Select(t4, true);
            selectionController.ShiftSelection(1, 0);

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);

            // Check state after undo and redo
            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);

            PlaceUtils.Redo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // A -> T1 -> B -> T2
            PlaceLeftLasers(1);   // A
            PlaceLeftLasers(2);   // B
            PlaceLeftLasers(1.5f); // T1
            PlaceLeftLasers(2.5f); // T2
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after moving eventT
            // A ->   -> B -> T1 -> T2
            var t1 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 1.5f && e.Type == (int)EventTypeValue.Event2);
            var t2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2.5f && e.Type == (int)EventTypeValue.Event2);
            SelectionController.Select(t1);
            SelectionController.Select(t2, true);
            selectionController.MoveSelection(0.75f);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
        }

        // Reproduce the scene-only preview error by moving node 2, then playing through the unchanged grid timeline.
        [Test]
        public void MovingTransitionSourceSelectionUpdatesPreviewFadeSource()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 1f,
                Type = eventType,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomColor = Color.red
            });
            var movedEvent = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomColor = Color.green
            });
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 3f,
                Type = eventType,
                Value = (int)LightValue.BlueTransition,
                FloatValue = 1f,
                CustomColor = Color.blue
            });

            // Put the real editor preview before node 1 before shifting only node 2.
            atsc.MoveToJsonTime(0f);
            SelectionController.Select(movedEvent);
            selectionController.MoveSelection(0.5f);

            var movedPreviewSource = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(movedPreviewSource.Prev, Is.Not.Null);
            Assert.That(movedPreviewSource.Next, Is.Not.Null);
            Assert.That(movedPreviewSource.Prev.IsTransition, Is.False);
            Assert.That(movedPreviewSource.Next.IsTransition, Is.True);
            Assert.That(movedPreviewSource.Prev.Next, Is.EqualTo(movedPreviewSource));
            Assert.That(movedPreviewSource.Next.Prev, Is.EqualTo(movedPreviewSource));

            // Node 1 must remain solid before moved node 2; the grid already has no transition ribbon in this interval.
            atsc.MoveToJsonTime(1f);
            atsc.MoveToJsonTime(2f);
            Assert.That(previewLight.Color, Is.EqualTo(Color.red));

            // Once node 2 becomes active, the reported preview repairs itself and resumes its node-2-to-node-3 fade.
            atsc.MoveToJsonTime(2.5f);
            atsc.MoveToJsonTime(2.75f);

            var expectedAfterNode2 = Color.LerpUnclamped(Color.green, Color.blue, 0.5f);
            Assert.That(previewLight.Color, Is.EqualTo(expectedAfterNode2));
        }

        // Inserting node 2 into a live node-1-to-node-3 transition must recalculate the light cache before node 2.
        [Test]
        public void PlacingOnNodeInsideTransitionReplacesLivePreviewSource()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 1f,
                Type = eventType,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomColor = Color.red
            });
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 3f,
                Type = eventType,
                Value = (int)LightValue.BlueTransition,
                FloatValue = 1f,
                CustomColor = Color.blue
            });

            // Place the middle On node while the timeline remains before node 1, matching a grid insertion during preview.
            atsc.MoveToJsonTime(0f);
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomColor = Color.green
            });

            var eventsContainer = GetEventsContainer();
            var middle = eventsContainer.MapObjects.OfType<BaseEvent>().Single(evt => evt.JsonTime == 2f);
            Assert.That(middle.Prev.IsTransition, Is.False);
            Assert.That(middle.Next.IsTransition, Is.True);
            Assert.That(middle.Prev.Next, Is.EqualTo(middle));
            Assert.That(middle.Next.Prev, Is.EqualTo(middle));

            // Playback before node 2 must stay solid red even though the original transition previously ended at node 3.
            atsc.MoveToJsonTime(1f);
            atsc.MoveToJsonTime(1.5f);
            Assert.That(previewLight.Color, Is.EqualTo(Color.red));

            // The existing transition must then start from node 2 once node 2 is reached.
            atsc.MoveToJsonTime(2f);
            atsc.MoveToJsonTime(2.5f);
            var expectedAfterMiddle = Color.LerpUnclamped(Color.green, Color.blue, 0.5f);
            Assert.That(previewLight.Color, Is.EqualTo(expectedAfterMiddle));
        }

        // Each alpha-zero sample is an independent case because one broken fade-in assertion must not prevent NUnit
        // from validating the fade-out midpoint, destination, and persistent off state on both DefaultEnvironment lasers.
        [TestCase((int)EventTypeValue.Event2, 0f, 0f)]
        [TestCase((int)EventTypeValue.Event2, 0.125f, 0.5f)]
        [TestCase((int)EventTypeValue.Event2, 0.25f, 1f)]
        [TestCase((int)EventTypeValue.Event2, 0.5f, 0.5f)]
        [TestCase((int)EventTypeValue.Event2, 0.75f, 0f)]
        [TestCase((int)EventTypeValue.Event2, 1.25f, 0f)]
        [TestCase((int)EventTypeValue.Event3, 0f, 0f)]
        [TestCase((int)EventTypeValue.Event3, 0.125f, 0.5f)]
        [TestCase((int)EventTypeValue.Event3, 0.25f, 1f)]
        [TestCase((int)EventTypeValue.Event3, 0.5f, 0.5f)]
        [TestCase((int)EventTypeValue.Event3, 0.75f, 0f)]
        [TestCase((int)EventTypeValue.Event3, 1.25f, 0f)]
        public void LoadingLegacyAlphaZeroChromaGradientCachesEveryPreviewPhase(
            int eventType,
            float sampleOffset,
            float expectedAlpha)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();
            const float startTime = 2f;
            var opaqueColor = eventType == (int)EventTypeValue.Event2
                ? new Color(0.298f, 1f, 0.584f, 1f)
                : new Color(0.755f, 1f, 0.584f, 1f);
            PlaceLegacyAlphaGradientSequence(
                eventType,
                startTime,
                opaqueColor);

            context.Descriptor.BasicEventEffectManager.Reinitialize();
            context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);

            AssertPreviewColorAt(
                atsc,
                previewLight,
                startTime + sampleOffset,
                opaqueColor.WithAlpha(expectedAlpha));
        }

        // Distinct RGB endpoints with non-one alpha prove the preview continuously interpolates every color channel;
        // checking only the destination would allow a broken cache that snaps there when the final event is reached.
        [TestCase((int)EventTypeValue.Event2, 0.25f, 0.45f, 0.4f, 0.35f, 0.45f)]
        [TestCase((int)EventTypeValue.Event2, 0.75f, 0.8f, 0.6f, 0.4f, 0.65f)]
        [TestCase((int)EventTypeValue.Event3, 0.25f, 0.45f, 0.5f, 0.6f, 0.6f)]
        [TestCase((int)EventTypeValue.Event3, 0.75f, 0.2f, 0.9f, 0.3f, 0.8f)]
        public void LoadingLegacyNonOpaqueChromaGradientInterpolatesRenderedPreview(
            int eventType,
            float sampleOffset,
            float expectedRed,
            float expectedGreen,
            float expectedBlue,
            float expectedAlpha)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();
            const float startTime = 2f;
            var startColor = eventType == (int)EventTypeValue.Event2
                ? new Color(0.1f, 0.2f, 0.3f, 0.25f)
                : new Color(0.7f, 0.1f, 0.9f, 0.4f);
            var endColor = eventType == (int)EventTypeValue.Event2
                ? new Color(0.8f, 0.6f, 0.4f, 0.65f)
                : new Color(0.2f, 0.9f, 0.3f, 0.8f);
            PlaceLegacyNonOpaqueGradientSequence(eventType, startTime, startColor, endColor);

            context.Descriptor.BasicEventEffectManager.Reinitialize();
            context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);

            AssertPreviewColorAt(
                atsc,
                previewLight,
                startTime + sampleOffset,
                new Color(expectedRed, expectedGreen, expectedBlue, expectedAlpha));
        }

        // A legacy fade-out gradient owns its alpha ramp independently of the coincident final event, so deleting that
        // event must preserve the authored half-beat fade and its zero-alpha state instead of snapping back to full.
        [Test]
        public void DeletingCoincidentAlphaZeroEventPreservesAuthoredLegacyGradientCache()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var previewLight = CreateLivePreviewLight(context, (int)EventTypeValue.Event2);
            EnableChromaLitePreview();
            var events = PlaceLegacyAlphaGradientSequence(
                (int)EventTypeValue.Event2,
                2f,
                new Color(0.298f, 1f, 0.584f, 1f));
            RebuildBasicPreview(context);
            atsc.MoveToJsonTime(2.5f);

            PlaceUtils.Delete(events.Zero);

            AssertPreviewColorAt(atsc, previewLight, 2.5f, new Color(0.298f, 1f, 0.584f, 0.5f));
            AssertPreviewColorAt(atsc, previewLight, 2.74f, new Color(0.298f, 1f, 0.584f, 0.02f));
            AssertPreviewColorAt(atsc, previewLight, 2.75f, new Color(0.298f, 1f, 0.584f, 0f));
            AssertPreviewColorAt(atsc, previewLight, 3f, new Color(0.298f, 1f, 0.584f, 0f));
        }

        // Moving an On node backward into an existing transition must produce the same preview as a full cache rebuild.
        [Test]
        public void MovingOnNodeBackwardIntoTransitionMatchesFullPreviewRebuild()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceLightEvent(1f, eventType, LightValue.RedOn, Color.red);
            PlaceLightEvent(4f, eventType, LightValue.BlueTransition, Color.blue);
            var moved = PlaceLightEvent(6f, eventType, LightValue.BlueOn, Color.green);

            // Drive the same repeated Shift+Down gesture used to move the selected node backward into the interval.
            PrepareBasicEventEditorInput();
            atsc.MoveToJsonTime(1.75f);
            SelectionController.Select(moved);
            PressTimeShiftKeys(-4f);

            var movedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(movedEvent.JsonTime, Is.EqualTo(2f));
            Assert.That(movedEvent.Prev.IsTransition, Is.False);
            Assert.That(movedEvent.Next.IsTransition, Is.True);

            var incremental = AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight });
            AssertColorsEqualRoundedToThreeDecimalPlaces(
                Color.red,
                incremental[0],
                "live preview immediately before the shifted node");
        }

        // Pasting several On nodes over and inside a transition must invalidate both conflict-removal and insertion caches.
        [Test]
        public void PastingOnNodesIntoTransitionMatchesFullPreviewRebuild()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceLightEvent(1f, eventType, LightValue.RedOn, Color.red);
            PlaceLightEvent(2f, eventType, LightValue.BlueOn, Color.green);
            PlaceLightEvent(6f, eventType, LightValue.BlueTransition, Color.blue);
            var copiedFirst = PlaceLightEvent(10f, eventType, LightValue.RedOn, Color.yellow);
            var copiedSecond = PlaceLightEvent(11f, eventType, LightValue.RedOn, Color.magenta);

            PrepareBasicEventEditorInput();
            SelectionController.Select(copiedFirst);
            SelectionController.Select(copiedSecond, true);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            atsc.MoveToJsonTime(1.75f);
            HoverBasicEventLaneAt(2f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().OrderBy(evt => evt.JsonTime).ToArray();
            Assert.That(pasted.Select(evt => evt.JsonTime).ToArray(), Is.EqualTo(new[] { 2f, 3f }));
            Assert.That(pasted[0].Prev.IsTransition, Is.False);
            Assert.That(pasted[0].Next, Is.EqualTo(pasted[1]));
            Assert.That(pasted[1].Next.IsTransition, Is.True);

            var incremental = AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight });
            AssertColorsEqualRoundedToThreeDecimalPlaces(
                Color.red,
                incremental[0],
                "live preview immediately before pasted nodes");
        }

        // Reproduce copying a targeted node and physically pasting it onto lane zero in Light ID view.
        [TestCase(LightValue.BlueOn)]
        [TestCase(LightValue.BlueTransition)]
        public void PastingLightIdEventOntoAllLightsLaneClearsLightId(LightValue lightValue)
        {
            const int eventType = (int)EventTypeValue.Event0;
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var sourceLightId = labels.LaneToLightID(eventType, 0);
            Assert.That(sourceLightId, Is.GreaterThanOrEqualTo(0), "The test environment has no first light-ID lane.");

            var copied = PlaceLightEvent(
                2f,
                eventType,
                lightValue,
                Color.magenta,
                new[] { sourceLightId });

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventAllLightsLaneAt(6f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(pasted.JsonTime, Is.EqualTo(6f).Within(0.00001f));
            Assert.That(pasted.Type, Is.EqualTo(eventType));
            Assert.That(pasted.Value, Is.EqualTo((int)lightValue));
            Assert.That(
                pasted.CustomLightID,
                Is.Null,
                "Pasting onto the All Lights lane must clear the copied node's light ID.");
            Assert.That(copied.CustomLightID, Is.EqualTo(new[] { sourceLightId }));
        }

        // A Basic Event hover before the map must lock its ghost to beat zero so the physical paste succeeds at the
        // boundary instead of authoring a negative event or silently rejecting the user's clipboard operation.
        [Test]
        public void HoverPastingBasicEventsBeforeBeatZeroAnchorsEarliestAtBeatZero()
        {
            const int sourceType = (int)EventTypeValue.Event2;
            const int destinationType = (int)EventTypeValue.Event3;
            var copiedFirst = PlaceLightEvent(2f, sourceType, LightValue.BlueOn, Color.magenta);
            var copiedSecond = PlaceLightEvent(3f, sourceType, LightValue.RedOn, Color.cyan);

            PrepareBasicEventEditorInput();
            SelectionController.Select(copiedFirst);
            SelectionController.Select(copiedSecond, true);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0f);
            HoverBasicEventLaneAt(-0.25f, destinationType, 0f);

            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var events = BeatSaberSongContainer.Instance.Map.Events.ToArray();
            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().OrderBy(evt => evt.JsonTime).ToArray();
            Assert.That(events, Has.Length.EqualTo(4));
            Assert.That(pasted.Select(evt => evt.JsonTime).ToArray(), Is.EqualTo(new[] { 0f, 1f }));
            Assert.That(pasted, Has.All.Property(nameof(BaseEvent.Type)).EqualTo(destinationType));
            Assert.That(events, Has.All.Property(nameof(BaseEvent.JsonTime)).GreaterThanOrEqualTo(0f));
        }

        // The propagated All Lights ghost must share the ordinary Basic Event beat-zero clamp and paste the targeted
        // clipboard event at beat zero with its light ID cleared for that global destination.
        [Test]
        public void HoverPastingLightIdEventOntoAllLightsBeforeBeatZeroClampsToBeatZero()
        {
            const int eventType = (int)EventTypeValue.Event0;
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var sourceLightId = labels.LaneToLightID(eventType, 0);
            Assert.That(sourceLightId, Is.GreaterThanOrEqualTo(0), "The test environment has no first light-ID lane.");
            var copied = PlaceLightEvent(
                2f,
                eventType,
                LightValue.BlueOn,
                Color.magenta,
                new[] { sourceLightId });

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0f);
            HoverBasicEventAllLightsLaneAt(-0.25f, eventType, 0f);

            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var events = BeatSaberSongContainer.Instance.Map.Events.ToArray();
            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(events, Has.Length.EqualTo(2));
            Assert.That(pasted.JsonTime, Is.Zero);
            Assert.That(pasted.CustomLightID, Is.Null);
            Assert.That(events, Has.All.Property(nameof(BaseEvent.JsonTime)).GreaterThanOrEqualTo(0f));
        }

        // An ordinary-lane paste at the audio end must shift the complete clipboard range backward so its latest
        // Basic Event lands at the final legal song beat without changing the shared single-node placement ghost.
        [Test]
        public void HoverPastingBasicEventsAtSongEndAnchorsLatestAtFinalBeat()
        {
            const int sourceType = (int)EventTypeValue.Event2;
            const int destinationType = (int)EventTypeValue.Event3;
            var copiedFirst = PlaceLightEvent(2f, sourceType, LightValue.BlueOn, Color.magenta);
            var copiedSecond = PlaceLightEvent(3f, sourceType, LightValue.RedOn, Color.cyan);
            var finalJsonTime = GetFinalLegalJsonTime();
            const float copiedRange = 1f;
            var expectedAnchor = finalJsonTime - copiedRange;

            PrepareBasicEventEditorInput();
            SelectionController.Select(copiedFirst);
            SelectionController.Select(copiedSecond, true);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(finalJsonTime);
            HoverBasicEventLaneAt(finalJsonTime, destinationType, finalJsonTime);

            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var events = BeatSaberSongContainer.Instance.Map.Events.ToArray();
            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().OrderBy(evt => evt.JsonTime).ToArray();
            Assert.That(events, Has.Length.EqualTo(4));
            Assert.That(pasted[0].JsonTime, Is.EqualTo(expectedAnchor).Within(0.00001f));
            Assert.That(pasted[1].JsonTime, Is.EqualTo(finalJsonTime).Within(0.00001f));
            Assert.That(pasted, Has.All.Property(nameof(BaseEvent.Type)).EqualTo(destinationType));
            Assert.That(events, Has.All.Property(nameof(BaseEvent.JsonTime)).LessThanOrEqualTo(finalJsonTime));
        }

        // The propagated All Lights path must apply the same backward range shift and clear every pasted light ID
        // without letting its later targeted Basic Event cross the final legal song beat.
        [Test]
        public void HoverPastingLightIdEventsAtSongEndAnchorsLatestAtFinalBeat()
        {
            const int eventType = (int)EventTypeValue.Event0;
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var sourceLightId = labels.LaneToLightID(eventType, 0);
            Assert.That(sourceLightId, Is.GreaterThanOrEqualTo(0), "The test environment has no first light-ID lane.");
            var copiedFirst = PlaceLightEvent(
                2f,
                eventType,
                LightValue.BlueOn,
                Color.magenta,
                new[] { sourceLightId });
            var copiedSecond = PlaceLightEvent(
                3f,
                eventType,
                LightValue.RedOn,
                Color.cyan,
                new[] { sourceLightId });
            var finalJsonTime = GetFinalLegalJsonTime();
            const float copiedRange = 1f;
            var expectedAnchor = finalJsonTime - copiedRange;

            PrepareBasicEventEditorInput();
            SelectionController.Select(copiedFirst);
            SelectionController.Select(copiedSecond, true);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(finalJsonTime);
            HoverBasicEventAllLightsLaneAt(finalJsonTime, eventType, finalJsonTime);

            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var events = BeatSaberSongContainer.Instance.Map.Events.ToArray();
            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().OrderBy(evt => evt.JsonTime).ToArray();
            Assert.That(events, Has.Length.EqualTo(4));
            Assert.That(pasted[0].JsonTime, Is.EqualTo(expectedAnchor).Within(0.00001f));
            Assert.That(pasted[1].JsonTime, Is.EqualTo(finalJsonTime).Within(0.00001f));
            Assert.That(pasted, Has.All.Property(nameof(BaseEvent.CustomLightID)).Null);
            Assert.That(events, Has.All.Property(nameof(BaseEvent.JsonTime)).LessThanOrEqualTo(finalJsonTime));
        }

        // Moving an On node between Basic Event tracks must rebuild both its old and new lighting timelines.
        [Test]
        public void ShiftingOnNodeBetweenLightTracksMatchesFullPreviewRebuild()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int destinationType = (int)EventTypeValue.Event2;
            const int sourceType = (int)EventTypeValue.Event3;
            var destinationLight = CreateLivePreviewLight(context, destinationType);
            var sourceLight = CreateLivePreviewLight(context, sourceType);
            EnableChromaLitePreview();

            PlaceLightEvent(1f, destinationType, LightValue.RedOn, Color.red);
            PlaceLightEvent(5f, destinationType, LightValue.BlueTransition, Color.blue);
            PlaceLightEvent(1f, sourceType, LightValue.RedOn, Color.yellow);
            var shifted = PlaceLightEvent(2f, sourceType, LightValue.BlueOn, Color.green);
            PlaceLightEvent(5f, sourceType, LightValue.RedOn, Color.cyan);

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(1.5f);
            SelectionController.Select(shifted);
            PressKeyboardShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.LeftArrow);

            var shiftedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shiftedEvent.Type, Is.EqualTo(destinationType));
            Assert.That(shiftedEvent.Prev.IsTransition, Is.False);
            Assert.That(shiftedEvent.Next.IsTransition, Is.True);
            Assert.That(
                GetEventsContainer().MapObjects.OfType<BaseEvent>().Any(evt => evt.Type == sourceType && evt.JsonTime == 2f),
                Is.False);

            var incremental = AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { destinationLight, sourceLight });
            AssertColorsEqualRoundedToThreeDecimalPlaces(
                Color.red,
                incremental[0],
                "destination before shifted node");
            AssertColorsEqualRoundedToThreeDecimalPlaces(
                Color.yellow,
                incremental[1],
                "vacated source before shifted node");
        }

        // Cover forward and backward time shifts into every relevant transition-role arrangement.
        [TestCaseSource(nameof(TimeShiftIntoTransitionCases))]
        public void TimeShiftingNodeIntoIntervalMatchesFullPreviewRebuild(
            PreviewTransitionPattern pattern,
            bool movesForward)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            var roles = GetTransitionRoles(pattern);
            PlaceLightEvent(1f, eventType, LightValue.RedOn, Color.white);
            var previous = PlaceScenarioLightEvent(4f, eventType, roles.PreviousTransition, Color.red);
            var next = PlaceScenarioLightEvent(8f, eventType, roles.NextTransition, Color.blue);
            PlaceLightEvent(12f, eventType, LightValue.RedOn, Color.cyan);
            var originalTime = movesForward ? 2f : 10f;
            var moved = PlaceScenarioLightEvent(originalTime, eventType, roles.MovedTransition, Color.green);
            RebuildBasicPreview(context);

            PrepareBasicEventEditorInput();
            atsc.MoveToJsonTime(5.75f);
            SelectionController.Select(moved);
            PressTimeShiftKeys(movesForward ? 4f : -4f);

            var movedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(movedEvent.JsonTime, Is.EqualTo(6f));
            Assert.That(movedEvent.Prev, Is.EqualTo(previous));
            Assert.That(movedEvent.Next, Is.EqualTo(next));
            Assert.That(movedEvent.IsTransition, Is.EqualTo(roles.MovedTransition));

            AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight });
        }

        // Cover moving each middle-node role out in both directions so the vacated transition is rebuilt.
        [TestCaseSource(nameof(TimeShiftOutOfTransitionCases))]
        public void TimeShiftingNodeOutOfIntervalMatchesFullPreviewRebuild(
            PreviewTransitionPattern pattern,
            bool movesForward)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            var roles = GetTransitionRoles(pattern);
            PlaceLightEvent(1f, eventType, LightValue.RedOn, Color.white);
            var previous = PlaceScenarioLightEvent(4f, eventType, roles.PreviousTransition, Color.red);
            var moved = PlaceScenarioLightEvent(6f, eventType, roles.MovedTransition, Color.green);
            var next = PlaceScenarioLightEvent(8f, eventType, roles.NextTransition, Color.blue);
            PlaceLightEvent(12f, eventType, LightValue.RedOn, Color.cyan);
            RebuildBasicPreview(context);

            PrepareBasicEventEditorInput();
            atsc.MoveToJsonTime(5.75f);
            SelectionController.Select(moved);
            PressTimeShiftKeys(movesForward ? 4f : -4f);

            var movedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(movedEvent.JsonTime, Is.EqualTo(movesForward ? 10f : 2f));
            Assert.That(previous.Next, Is.EqualTo(next));
            Assert.That(next.Prev, Is.EqualTo(previous));

            AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight });
        }

        // Pair every destination transition arrangement with every vacated source-lane neighbor arrangement.
        [TestCaseSource(nameof(CrossLaneTransitionCases))]
        public void ShiftingNodeBetweenTracksMatchesBothFullPreviewRebuilds(
            PreviewTransitionPattern destinationPattern,
            SourceNeighborPattern sourcePattern,
            bool neighborsOutsideLoadedBounds)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int destinationType = (int)EventTypeValue.Event2;
            const int sourceType = (int)EventTypeValue.Event3;
            var destinationLight = CreateLivePreviewLight(context, destinationType);
            var sourceLight = CreateLivePreviewLight(context, sourceType);
            EnableChromaLitePreview();

            var destinationRoles = GetTransitionRoles(destinationPattern);
            var sourceRoles = GetSourceNeighborRoles(sourcePattern);
            PlaceLightEvent(1f, destinationType, LightValue.RedOn, Color.white);
            var destinationPrevious = PlaceScenarioLightEvent(
                4f,
                destinationType,
                destinationRoles.PreviousTransition,
                Color.red);
            var destinationNext = PlaceScenarioLightEvent(
                8f,
                destinationType,
                destinationRoles.NextTransition,
                Color.blue);
            PlaceLightEvent(1f, sourceType, LightValue.RedOn, Color.black);
            var sourcePrevious = PlaceScenarioLightEvent(
                4f,
                sourceType,
                sourceRoles.PreviousTransition,
                Color.yellow);
            var shifted = PlaceScenarioLightEvent(
                6f,
                sourceType,
                destinationRoles.MovedTransition,
                Color.green);
            var sourceNext = PlaceScenarioLightEvent(
                8f,
                sourceType,
                sourceRoles.NextTransition,
                Color.cyan);
            RebuildBasicPreview(context);

            PrepareBasicEventEditorInput();
            atsc.MoveToJsonTime(5.75f);
            if (neighborsOutsideLoadedBounds)
            {
                RestrictOrdinaryEventWindowToMiddle(destinationPrevious, destinationNext, sourcePrevious, sourceNext);
            }

            SelectionController.Select(shifted);
            PressKeyboardShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.LeftArrow);

            var shiftedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shiftedEvent.Type, Is.EqualTo(destinationType));
            Assert.That(shiftedEvent.Prev, Is.EqualTo(destinationPrevious));
            Assert.That(shiftedEvent.Next, Is.EqualTo(destinationNext));
            Assert.That(sourcePrevious.Next, Is.EqualTo(sourceNext));
            Assert.That(sourceNext.Prev, Is.EqualTo(sourcePrevious));

            AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { destinationLight, sourceLight });
        }

        // Exercise paste and its inverse undo for every relevant destination transition arrangement.
        [TestCaseSource(nameof(PasteTransitionCases))]
        public void PastingAndUndoingNodeMatchesFullPreviewRebuild(
            PreviewTransitionPattern pattern,
            bool neighborsOutsideLoadedBounds)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            var roles = GetTransitionRoles(pattern);
            PlaceLightEvent(1f, eventType, LightValue.RedOn, Color.white);
            var previous = PlaceScenarioLightEvent(4f, eventType, roles.PreviousTransition, Color.red);
            var next = PlaceScenarioLightEvent(8f, eventType, roles.NextTransition, Color.blue);
            var copied = PlaceScenarioLightEvent(12f, eventType, roles.MovedTransition, Color.green);
            RebuildBasicPreview(context);

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            atsc.MoveToJsonTime(5.75f);
            HoverBasicEventLaneAt(6f, eventType);
            if (neighborsOutsideLoadedBounds)
            {
                RestrictOrdinaryEventWindowToMiddle(previous, next);
            }

            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(pasted.JsonTime, Is.EqualTo(6f));
            Assert.That(pasted.Prev, Is.EqualTo(previous));
            Assert.That(pasted.Next, Is.EqualTo(next));
            AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight });

            PressUndoShortcutExpectingAction<SelectionPastedAction>();

            Assert.That(previous.Next, Is.EqualTo(next));
            Assert.That(next.Prev, Is.EqualTo(previous));
            Assert.That(
                GetEventsContainer().MapObjects.OfType<BaseEvent>().Any(evt => evt.Type == eventType && evt.JsonTime == 6f),
                Is.False);
            AssertCurrentLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight });
        }

        // Shift an unrendered selected node while the playhead remains in the same simulator bucket as the edit.
        [UnityTest]
        public IEnumerator TimeShiftWhileVisualIsUnloadedInSameStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftWithVisualChunkStateMatchesFullPreviewRebuild(8f, 30f, 12f, false);
        }

        // Shift an unrendered selected node while the simulator cursor is several state buckets beyond the edit.
        [UnityTest]
        public IEnumerator TimeShiftWhileVisualIsUnloadedInDistantStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftWithVisualChunkStateMatchesFullPreviewRebuild(22f, 42f, 26f, false);
        }

        // Shift a node behind the visible track while its grid container remains resident in a wider loaded chunk.
        [UnityTest]
        public IEnumerator TimeShiftWhileOffscreenButInLoadedChunkMatchesFullPreviewRebuild()
        {
            yield return TimeShiftWithVisualChunkStateMatchesFullPreviewRebuild(8f, 30f, 12f, true);
        }

        // Move an unrendered event between lanes so both source and destination caches must update from a distant view.
        [UnityTest]
        public IEnumerator CrossLaneShiftWhileVisualIsUnloadedMatchesBothFullPreviewRebuilds()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int destinationType = (int)EventTypeValue.Event2;
            const int sourceType = (int)EventTypeValue.Event3;
            var destinationLight = CreateLivePreviewLight(context, destinationType);
            var sourceLight = CreateLivePreviewLight(context, sourceType);
            EnableChromaLitePreview();

            PlaceLightEvent(0.5f, destinationType, LightValue.RedOn, Color.white);
            var destinationPrevious = PlaceLightEvent(3f, destinationType, LightValue.BlueOn, Color.red);
            var destinationNext = PlaceLightEvent(12f, destinationType, LightValue.BlueTransition, Color.blue);
            PlaceLightEvent(16f, destinationType, LightValue.RedOn, Color.magenta);
            PlaceLightEvent(0.5f, sourceType, LightValue.RedOn, Color.black);
            var sourcePrevious = PlaceLightEvent(3f, sourceType, LightValue.BlueOn, Color.yellow);
            var shifted = PlaceLightEvent(4f, sourceType, LightValue.BlueOn, Color.green);
            var sourceNext = PlaceLightEvent(12f, sourceType, LightValue.BlueTransition, Color.cyan);
            PlaceLightEvent(16f, sourceType, LightValue.RedOn, Color.white);
            RebuildBasicPreview(context);

            PrepareBasicEventEditorInput();
            UseNarrowVisualChunkWindow();
            SelectionController.Select(shifted);
            // Move beyond both source-lane ribbons so the selected beat-4 node is genuinely visually unloaded.
            yield return MoveViewAcrossChunkBoundary(30f, 20f);

            var eventsContainer = GetEventsContainer();
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(shifted), Is.False);
            PressKeyboardShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.LeftArrow);

            var shiftedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shiftedEvent.Type, Is.EqualTo(destinationType));
            Assert.That(shiftedEvent.JsonTime, Is.EqualTo(4f));
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(shiftedEvent), Is.False);
            Assert.That(shiftedEvent.Prev, Is.EqualTo(destinationPrevious));
            Assert.That(shiftedEvent.Next, Is.EqualTo(destinationNext));
            Assert.That(sourcePrevious.Next, Is.EqualTo(sourceNext));
            Assert.That(sourceNext.Prev, Is.EqualTo(sourcePrevious));

            AssertLivePreviewMatchesFullRebuild(
                context,
                new[] { destinationLight, sourceLight },
                8f,
                10f,
                2f,
                4.5f,
                13f,
                8f);
        }

        // Shift out and back with intervening scrubs, without allowing a full rebuild to repair the first edit.
        [UnityTest]
        public IEnumerator TimeShiftRoundTripAfterChunkUnloadAndScrubbingRestoresBaselinePreview()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceLightEvent(0.5f, eventType, LightValue.RedOn, Color.white);
            var moved = PlaceLightEvent(1f, eventType, LightValue.BlueOn, Color.green);
            var previous = PlaceLightEvent(3f, eventType, LightValue.BlueOn, Color.red);
            var next = PlaceLightEvent(12f, eventType, LightValue.BlueTransition, Color.blue);
            PlaceLightEvent(16f, eventType, LightValue.RedOn, Color.cyan);
            RebuildBasicPreview(context);

            var sampleTimes = new[] { 8f, 10f, 2f, 4.5f, 13f, 2f, 8f };
            var baseline = CaptureLivePreview(atsc, new[] { previewLight }, sampleTimes);
            PrepareBasicEventEditorInput();
            UseNarrowVisualChunkWindow();
            SelectionController.Select(moved);
            yield return MoveViewAcrossChunkBoundary(30f, 8f);

            Assert.That(GetEventsContainer().LoadedContainers.ContainsKey(moved), Is.False);
            PressTimeShiftKeys(3f);
            atsc.MoveToJsonTime(6f);
            yield return null;
            atsc.MoveToJsonTime(2f);
            yield return null;
            atsc.MoveToJsonTime(8f);
            yield return null;
            PressTimeShiftKeys(-3f);

            var restored = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(restored.JsonTime, Is.EqualTo(1f));
            Assert.That(previous.Prev, Is.EqualTo(restored));
            Assert.That(previous.Next, Is.EqualTo(next));
            AssertPreviewMatchesBaseline(
                baseline,
                CaptureLivePreview(atsc, new[] { previewLight }, sampleTimes),
                new[] { previewLight },
                sampleTimes,
                "time-shift round trip");
        }

        // Paste and undo with both endpoint nodes outside the ordinary visual window and no intervening full rebuild.
        [UnityTest]
        public IEnumerator PasteUndoAcrossVisualChunksAfterScrubbingRestoresBaselinePreview()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceLightEvent(1f, eventType, LightValue.RedOn, Color.white);
            var previous = PlaceLightEvent(3f, eventType, LightValue.BlueOn, Color.red);
            var next = PlaceLightEvent(35f, eventType, LightValue.BlueTransition, Color.blue);
            var copied = PlaceLightEvent(40f, eventType, LightValue.BlueOn, Color.green);
            RebuildBasicPreview(context);

            var sampleTimes = new[] { 2f, 10f, 19.75f, 25f, 34f, 36f, 19.75f };
            var baseline = CaptureLivePreview(atsc, new[] { previewLight }, sampleTimes);
            PrepareBasicEventEditorInput();
            UseNarrowVisualChunkWindow();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            yield return MoveViewAcrossChunkBoundary(30f, 19.75f);

            var eventsContainer = GetEventsContainer();
            // The beat-3 point is outside the ordinary window but must stay loaded to render its ribbon to beat 35.
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(previous), Is.True);
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(next), Is.False);
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(copied), Is.False);
            HoverBasicEventLaneAt(20f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            var pasted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(pasted.JsonTime, Is.EqualTo(20f));
            AssertColorsEqualRoundedToThreeDecimalPlaces(
                Color.red,
                previewLight.Color,
                "pasting an On node must immediately stop the old transition before the pasted beat");
            atsc.MoveToJsonTime(34f);
            yield return null;
            atsc.MoveToJsonTime(21f);
            yield return null;
            atsc.MoveToJsonTime(10f);
            yield return null;
            atsc.MoveToJsonTime(19.75f);
            yield return null;
            PressUndoShortcutExpectingAction<SelectionPastedAction>();

            Assert.That(previous.Next, Is.EqualTo(next));
            Assert.That(next.Prev, Is.EqualTo(previous));
            Assert.That(eventsContainer.MapObjects.OfType<BaseEvent>().Any(evt => evt.JsonTime == 20f), Is.False);
            AssertPreviewMatchesBaseline(
                baseline,
                CaptureLivePreview(atsc, new[] { previewLight }, sampleTimes),
                new[] { previewLight },
                sampleTimes,
                "paste, scrub, and undo");
        }

        // Movement snapshots must match a save/reload-equivalent rebuild after every Shift+Arrow interval crossing.
        [TestCaseSource(nameof(MovementTimeShiftCases))]
        public void TimeShiftingMovementNodeMatchesFullPreviewRebuild(
            BasicMovementPreviewKind kind,
            bool startsInside,
            bool movesForward)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementAnchors(eventType, kind);

            var (originalTime, targetTime) = GetMovementTimeShiftTimes(startsInside, movesForward);
            var moved = PlaceMovementEvent(originalTime, eventType, kind, 4);
            RebuildBasicMovementPreview(context);
            // TimeShiftingMovementNodeMatchesFullPreviewRebuild needs the pre-edit future chain cached first.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5.75f);
            SelectionController.Select(moved);
            PressTimeShiftKeys(targetTime - originalTime);

            var shifted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shifted.JsonTime, Is.EqualTo(targetTime).Within(0.00001f));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // A physical time-shift undo must restore the exact pre-action transforms, and redo must rebuild identically.
        [TestCaseSource(nameof(MovementTimeShiftUndoCases))]
        public void TimeShiftingAndUndoingMovementNodeRestoresPreview(
            BasicMovementPreviewKind kind,
            bool startsInside,
            bool movesForward)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementAnchors(eventType, kind);
            var (originalTime, targetTime) = GetMovementTimeShiftTimes(startsInside, movesForward);
            var moved = PlaceMovementEvent(originalTime, eventType, kind, 4);
            RebuildBasicMovementPreview(context);
            var baseline = CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes);
            // Exercise every production preview callback so a brief stale movement wave cannot fall between checkpoints.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(MovementPreviewSampleJsonTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackJsonTimes);

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5.75f);
            SelectionController.Select(moved);
            var shift = targetTime - originalTime;
            var pressCount = GetTimeShiftPressCount(shift);
            PressTimeShiftKeys(shift);
            for (var press = 0; press < pressCount; press++)
            {
                PressUndoShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>();
            }

            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes),
                targets,
                MovementPreviewSampleJsonTimes,
                "time-shift undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackJsonTimes),
                targets,
                playbackJsonTimes,
                "time-shift undo playback");

            for (var press = 0; press < pressCount; press++)
            {
                PressRedoShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>();
            }

            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Moving one zoom, rotation, or laser-speed node across five existing nodes must invalidate the entire crossed chain.
        [TestCaseSource(nameof(MovementMultiNodeJumpCases))]
        public void TimeShiftingMovementNodeAcrossMultipleNodesMatchesFullPreviewRebuild(
            BasicMovementPreviewKind kind,
            bool movesForward)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            var moved = PlaceMultiNodeMovementJumpSequence(eventType, kind, movesForward);
            RebuildBasicMovementPreview(context);
            // TimeShiftingMovementNodeAcrossMultipleNodesMatchesFullPreviewRebuild starts from a fully rendered future chain.
            PrimeMovementPreviewCache(MovementMultiNodeSampleJsonTimes);

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(7.75f);
            SelectionController.Select(moved);
            PressTimeShiftKeys(movesForward ? 10f : -10f);

            Assert.That(
                SelectionController.SelectedObjects.OfType<BaseEvent>().Single().JsonTime,
                Is.EqualTo(movesForward ? 12f : 2f).Within(0.00001f));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementMultiNodeSampleJsonTimes);
        }

        // Undo/redo of a five-node crossing must restore and then reproduce the same authoritative movement timeline.
        [TestCaseSource(nameof(MovementMultiNodeJumpUndoCases))]
        public void TimeShiftingAndUndoingMovementNodeAcrossMultipleNodesRestoresPreview(
            BasicMovementPreviewKind kind,
            bool movesForward)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            var moved = PlaceMultiNodeMovementJumpSequence(eventType, kind, movesForward);
            RebuildBasicMovementPreview(context);
            var baseline = CaptureMovementPreview(targets, MovementMultiNodeSampleJsonTimes);
            // The multi-node undo oracle includes every real preview callback through the crossed sequence.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(MovementMultiNodeSampleJsonTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackJsonTimes);

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(7.75f);
            SelectionController.Select(moved);
            const float shiftMagnitude = 10f;
            var shift = movesForward ? shiftMagnitude : -shiftMagnitude;
            var pressCount = GetTimeShiftPressCount(shift);
            PressTimeShiftKeys(shift);
            for (var press = 0; press < pressCount; press++)
            {
                PressUndoShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>();
            }

            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, MovementMultiNodeSampleJsonTimes),
                targets,
                MovementMultiNodeSampleJsonTimes,
                "multi-node time-shift undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackJsonTimes),
                targets,
                playbackJsonTimes,
                "multi-node time-shift undo playback");

            for (var press = 0; press < pressCount; press++)
            {
                PressRedoShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>();
            }

            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementMultiNodeSampleJsonTimes);
        }

        // Pasting a movement node must invalidate every dependent ring or laser-speed snapshot.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void PastingMovementNodeMatchesFullPreviewRebuild(BasicMovementPreviewKind kind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementAnchors(eventType, kind);
            var copied = PlaceMovementEvent(16f, eventType, kind, 4);
            RebuildBasicMovementPreview(context);
            // PastingMovementNodeMatchesFullPreviewRebuild reproduces a paste into an already-rendered sequence.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventLaneAt(6f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            Assert.That(SelectionController.SelectedObjects.OfType<BaseEvent>().Single().JsonTime, Is.EqualTo(6f));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Paste undo/redo must remove and restore movement states without leaving the pasted speed or wave active.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void PastingAndUndoingMovementNodeRestoresPreview(BasicMovementPreviewKind kind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementAnchors(eventType, kind);
            var copied = PlaceMovementEvent(16f, eventType, kind, 4);
            RebuildBasicMovementPreview(context);
            var baseline = CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes);
            // Exercise every production preview callback so a brief stale movement wave cannot fall between checkpoints.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(MovementPreviewSampleJsonTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackJsonTimes);

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventLaneAt(6f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);
            PressUndoShortcutExpectingAction<SelectionPastedAction>();

            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes),
                targets,
                MovementPreviewSampleJsonTimes,
                "paste undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackJsonTimes),
                targets,
                playbackJsonTimes,
                "paste undo playback");

            PressRedoShortcutExpectingAction<SelectionPastedAction>();
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Multi-selection time shifts must invalidate from the earliest edited movement state regardless of hash-set order.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void TimeShiftingMultipleMovementNodesMatchesFullPreviewRebuild(BasicMovementPreviewKind kind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementEvent(1f, eventType, kind, 0);
            var movedFirst = PlaceMovementEvent(2f, eventType, kind, 3);
            var movedSecond = PlaceMovementEvent(3f, eventType, kind, 4);
            PlaceMovementEvent(4f, eventType, kind, 1);
            PlaceMovementEvent(10f, eventType, kind, 2);
            PlaceMovementEvent(14f, eventType, kind, 0);
            RebuildBasicMovementPreview(context);
            // TimeShiftingMultipleMovementNodesMatchesFullPreviewRebuild needs cached states after both moved nodes.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5.75f);
            SelectionController.Select(movedFirst);
            SelectionController.Select(movedSecond, true);
            PressTimeShiftKeys(4f);

            Assert.That(
                SelectionController.SelectedObjects
                    .OfType<BaseEvent>()
                    .Select(evt => evt.JsonTime)
                    .OrderBy(time => time)
                    .ToArray(),
                Is.EqualTo(new[] { 6f, 7f }));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Multi-node paste must rebuild from the earliest pasted state even when the copied set enumerates later nodes first.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void PastingMultipleMovementNodesMatchesFullPreviewRebuild(BasicMovementPreviewKind kind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementEvent(1f, eventType, kind, 0);
            PlaceMovementEvent(4f, eventType, kind, 1);
            PlaceMovementEvent(10f, eventType, kind, 2);
            PlaceMovementEvent(14f, eventType, kind, 0);
            var copiedFirst = PlaceMovementEvent(16f, eventType, kind, 3);
            var copiedSecond = PlaceMovementEvent(17f, eventType, kind, 4);
            RebuildBasicMovementPreview(context);
            // PastingMultipleMovementNodesMatchesFullPreviewRebuild needs the destination chain cached before paste.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            SelectionController.Select(copiedFirst);
            SelectionController.Select(copiedSecond, true);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventLaneAt(6f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            Assert.That(
                SelectionController.SelectedObjects
                    .OfType<BaseEvent>()
                    .Select(evt => evt.JsonTime)
                    .OrderBy(time => time)
                    .ToArray(),
                Is.EqualTo(new[] { 6f, 7f }));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Multi-node paste undo/redo must remove and restore the entire dependent movement interval as one action.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void PastingAndUndoingMultipleMovementNodesRestoresPreview(BasicMovementPreviewKind kind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementEvent(1f, eventType, kind, 0);
            PlaceMovementEvent(4f, eventType, kind, 1);
            PlaceMovementEvent(10f, eventType, kind, 2);
            PlaceMovementEvent(14f, eventType, kind, 0);
            var copiedFirst = PlaceMovementEvent(16f, eventType, kind, 3);
            var copiedSecond = PlaceMovementEvent(17f, eventType, kind, 4);
            RebuildBasicMovementPreview(context);
            var baseline = CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes);
            // Exercise every production preview callback so a brief stale movement wave cannot fall between checkpoints.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(MovementPreviewSampleJsonTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackJsonTimes);

            PrepareBasicEventEditorInput();
            SelectionController.Select(copiedFirst);
            SelectionController.Select(copiedSecond, true);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventLaneAt(6f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);
            PressUndoShortcutExpectingAction<SelectionPastedAction>();

            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes),
                targets,
                MovementPreviewSampleJsonTimes,
                "multi-node paste undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackJsonTimes),
                targets,
                playbackJsonTimes,
                "multi-node paste undo playback");

            PressRedoShortcutExpectingAction<SelectionPastedAction>();
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // A horizontal shift between movement tracks must invalidate both the vacated and destination effect chains.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void ShiftingMovementNodeBetweenTracksMatchesFullPreviewRebuild(
            BasicMovementPreviewKind sourceKind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var destinationKind = GetOtherMovementKind(sourceKind);
            var sourceType = GetMovementEventType(context, sourceKind);
            var destinationType = GetMovementEventType(context, destinationKind);
            Assert.That(destinationType, Is.Not.EqualTo(sourceType));
            var targets = GetMovementPreviewTargets(context, sourceType, destinationType);
            PlaceMovementAnchors(sourceType, sourceKind);
            PlaceMovementAnchors(destinationType, destinationKind);
            var moved = PlaceMovementEvent(6f, sourceType, sourceKind, 4);
            RebuildBasicMovementPreview(context);
            // ShiftingMovementNodeBetweenTracksMatchesFullPreviewRebuild primes both source and destination chains.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5.75f);
            SelectionController.Select(moved);
            PressTrackShiftKeys(sourceType, destinationType);

            Assert.That(SelectionController.SelectedObjects.OfType<BaseEvent>().Single().Type, Is.EqualTo(destinationType));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Undoing and redoing a cross-track shift must restore both movement timelines without a manual preview refresh.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void ShiftingAndUndoingMovementNodeBetweenTracksRestoresBothPreviews(
            BasicMovementPreviewKind sourceKind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var destinationKind = GetOtherMovementKind(sourceKind);
            var sourceType = GetMovementEventType(context, sourceKind);
            var destinationType = GetMovementEventType(context, destinationKind);
            Assert.That(destinationType, Is.Not.EqualTo(sourceType));
            var targets = GetMovementPreviewTargets(context, sourceType, destinationType);
            PlaceMovementAnchors(sourceType, sourceKind);
            PlaceMovementAnchors(destinationType, destinationKind);
            var moved = PlaceMovementEvent(6f, sourceType, sourceKind, 4);
            RebuildBasicMovementPreview(context);
            var baseline = CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes);
            // Exercise every production preview callback so a brief stale movement wave cannot fall between checkpoints.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(MovementPreviewSampleJsonTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackJsonTimes);

            PrepareBasicEventEditorInput();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5.75f);
            SelectionController.Select(moved);
            var pressCount = PressTrackShiftKeys(sourceType, destinationType);
            for (var press = 0; press < pressCount; press++)
            {
                PressUndoShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>();
            }

            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes),
                targets,
                MovementPreviewSampleJsonTimes,
                "cross-track shift undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackJsonTimes),
                targets,
                playbackJsonTimes,
                "cross-track shift undo playback");

            for (var press = 0; press < pressCount; press++)
            {
                PressRedoShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>();
            }

            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Pasting onto the other movement track must rebuild its scene rings while leaving the copied source timeline intact.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void PastingMovementNodeBetweenTracksMatchesFullPreviewRebuild(
            BasicMovementPreviewKind sourceKind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var destinationKind = GetOtherMovementKind(sourceKind);
            var sourceType = GetMovementEventType(context, sourceKind);
            var destinationType = GetMovementEventType(context, destinationKind);
            Assert.That(destinationType, Is.Not.EqualTo(sourceType));
            var targets = GetMovementPreviewTargets(context, sourceType, destinationType);
            PlaceMovementAnchors(sourceType, sourceKind);
            PlaceMovementAnchors(destinationType, destinationKind);
            var copied = PlaceMovementEvent(16f, sourceType, sourceKind, 4);
            RebuildBasicMovementPreview(context);
            // PastingMovementNodeBetweenTracksMatchesFullPreviewRebuild primes both movement effect chains.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventLaneAt(6f, destinationType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);

            Assert.That(SelectionController.SelectedObjects.OfType<BaseEvent>().Single().Type, Is.EqualTo(destinationType));
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Cross-track paste undo/redo must remove and restore only the destination movement state.
        [TestCaseSource(nameof(AllMovementPreviewKinds))]
        public void PastingAndUndoingMovementNodeBetweenTracksRestoresBothPreviews(
            BasicMovementPreviewKind sourceKind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var destinationKind = GetOtherMovementKind(sourceKind);
            var sourceType = GetMovementEventType(context, sourceKind);
            var destinationType = GetMovementEventType(context, destinationKind);
            Assert.That(destinationType, Is.Not.EqualTo(sourceType));
            var targets = GetMovementPreviewTargets(context, sourceType, destinationType);
            PlaceMovementAnchors(sourceType, sourceKind);
            PlaceMovementAnchors(destinationType, destinationKind);
            var copied = PlaceMovementEvent(16f, sourceType, sourceKind, 4);
            RebuildBasicMovementPreview(context);
            var baseline = CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes);
            // Exercise every production preview callback so a brief stale movement wave cannot fall between checkpoints.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(MovementPreviewSampleJsonTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackJsonTimes);

            PrepareBasicEventEditorInput();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6f);
            HoverBasicEventLaneAt(6f, destinationType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);
            PressUndoShortcutExpectingAction<SelectionPastedAction>();

            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, MovementPreviewSampleJsonTimes),
                targets,
                MovementPreviewSampleJsonTimes,
                "cross-track paste undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackJsonTimes),
                targets,
                playbackJsonTimes,
                "cross-track paste undo playback");

            PressRedoShortcutExpectingAction<SelectionPastedAction>();
            AssertMovementPreviewMatchesFullRebuild(context, targets, MovementPreviewSampleJsonTimes);
        }

        // Repeat Shift+Arrow after normal chunk recycling has removed the selected zoom node's visual container.
        [UnityTest]
        public IEnumerator RingZoomTimeShiftWhileVisualIsUnloadedInSameStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.RingZoom,
                8f,
                30f,
                12f,
                false);
        }

        // Repeat the unloaded shift across distant state buckets for ring zoom snapshot invalidation.
        [UnityTest]
        public IEnumerator RingZoomTimeShiftWhileVisualIsUnloadedInDistantStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.RingZoom,
                22f,
                42f,
                26f,
                false);
        }

        // Distinguish an offscreen zoom node retained by a wide chunk window from a genuinely unloaded visual.
        [UnityTest]
        public IEnumerator RingZoomTimeShiftWhileOffscreenButInLoadedChunkMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.RingZoom,
                8f,
                30f,
                12f,
                true);
        }

        // Repeat Shift+Arrow after normal chunk recycling has removed the selected rotation node's visual container.
        [UnityTest]
        public IEnumerator RingRotationTimeShiftWhileVisualIsUnloadedInSameStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.RingRotation,
                8f,
                30f,
                12f,
                false);
        }

        // Repeat the unloaded shift across distant state buckets for ring rotation wave invalidation.
        [UnityTest]
        public IEnumerator RingRotationTimeShiftWhileVisualIsUnloadedInDistantStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.RingRotation,
                22f,
                42f,
                26f,
                false);
        }

        // Distinguish an offscreen rotation node retained by a wide chunk window from a genuinely unloaded visual.
        [UnityTest]
        public IEnumerator RingRotationTimeShiftWhileOffscreenButInLoadedChunkMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.RingRotation,
                8f,
                30f,
                12f,
                true);
        }

        // Paste and undo zoom after unloading the copied visual and scrubbing across both sides of the new event.
        [UnityTest]
        public IEnumerator RingZoomPasteUndoAcrossVisualChunksAfterScrubbingRestoresBaselinePreview()
        {
            yield return PasteUndoMovementAcrossVisualChunksRestoresBaselinePreview(
                BasicMovementPreviewKind.RingZoom);
        }

        // Paste and undo rotation after unloading the copied visual and scrubbing across both sides of the new event.
        [UnityTest]
        public IEnumerator RingRotationPasteUndoAcrossVisualChunksAfterScrubbingRestoresBaselinePreview()
        {
            yield return PasteUndoMovementAcrossVisualChunksRestoresBaselinePreview(
                BasicMovementPreviewKind.RingRotation);
        }

        // Repeat a left laser-speed shift after its event node has been recycled from the visual pool.
        [UnityTest]
        public IEnumerator LaserSpeedLeftTimeShiftWhileVisualIsUnloadedInSameStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.LaserSpeedLeft,
                8f,
                30f,
                12f,
                false);
        }

        // Cross distant state buckets while shifting an unloaded left laser-speed event.
        [UnityTest]
        public IEnumerator LaserSpeedLeftTimeShiftWhileVisualIsUnloadedInDistantStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.LaserSpeedLeft,
                22f,
                42f,
                26f,
                false);
        }

        // Distinguish an offscreen left laser-speed event retained in a loaded chunk from a recycled one.
        [UnityTest]
        public IEnumerator LaserSpeedLeftTimeShiftWhileOffscreenButInLoadedChunkMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.LaserSpeedLeft,
                8f,
                30f,
                12f,
                true);
        }

        // Repeat a right laser-speed shift after its event node has been recycled from the visual pool.
        [UnityTest]
        public IEnumerator LaserSpeedRightTimeShiftWhileVisualIsUnloadedInSameStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.LaserSpeedRight,
                8f,
                30f,
                12f,
                false);
        }

        // Cross distant state buckets while shifting an unloaded right laser-speed event.
        [UnityTest]
        public IEnumerator LaserSpeedRightTimeShiftWhileVisualIsUnloadedInDistantStateBucketMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.LaserSpeedRight,
                22f,
                42f,
                26f,
                false);
        }

        // Distinguish an offscreen right laser-speed event retained in a loaded chunk from a recycled one.
        [UnityTest]
        public IEnumerator LaserSpeedRightTimeShiftWhileOffscreenButInLoadedChunkMatchesFullPreviewRebuild()
        {
            yield return TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
                BasicMovementPreviewKind.LaserSpeedRight,
                8f,
                30f,
                12f,
                true);
        }

        // Paste and undo a left laser-speed event after unloading its copied visual and scrubbing both sides.
        [UnityTest]
        public IEnumerator LaserSpeedLeftPasteUndoAcrossVisualChunksAfterScrubbingRestoresBaselinePreview()
        {
            yield return PasteUndoMovementAcrossVisualChunksRestoresBaselinePreview(
                BasicMovementPreviewKind.LaserSpeedLeft);
        }

        // Paste and undo a right laser-speed event after unloading its copied visual and scrubbing both sides.
        [UnityTest]
        public IEnumerator LaserSpeedRightPasteUndoAcrossVisualChunksAfterScrubbingRestoresBaselinePreview()
        {
            yield return PasteUndoMovementAcrossVisualChunksRestoresBaselinePreview(
                BasicMovementPreviewKind.LaserSpeedRight);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Check state after placing
            // A -> B
            PlaceLeftLasers(1);
            PlaceLeftLasers(2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after pasting
            // A -> B -> A Copy -> B copy
            var a = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 1f && e.Type == (int)EventTypeValue.Event2);
            var b = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2f && e.Type == (int)EventTypeValue.Event2);
            SelectionController.Select(a);
            SelectionController.Select(b, true);
            atsc.MoveToJsonTime(3);
            if (eventPlacement.QueuedData != null) eventPlacement.QueuedData.JsonTime = 3;
            selectionController.Copy();
            selectionController.Paste();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
        }

        // Keep basic-event neighbor state correct when a collection edit moves several events across a populated lane.
        [Test]
        public void MovingSelectionAcrossExistingEventsKeepsNeighborsLinked()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            PlaceLeftLasers(1);
            PlaceLeftLasers(2);
            var movedA = PlaceLeftLasers(4);
            var movedB = PlaceLeftLasers(5);
            PlaceLeftLasers(7);

            SelectionController.Select(movedA);
            SelectionController.Select(movedB, true);
            selectionController.MoveSelection(-1.5f);

            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
        }

        // Preserve a shared name-filter lane until the final matching ring event is removed, then restore it on undo.
        [Test]
        public void RingNameFilterLanesTrackDuplicateEventsAcrossDeleteAndUndo()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ringType = GetRingRotationType();
            var first = PlaceRingRotation(1, ringType, "drums");
            var second = PlaceRingRotation(2, ringType, "drums");

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            Assert.AreNotEqual(labels.EventTypeToLaneId(first.Type), labels.EventToLaneId(first));

            PlaceUtils.Delete(first);
            Assert.AreNotEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));

            PlaceUtils.Delete(second);
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));

            PlaceUtils.Undo();
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreNotEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));
        }

        // Keep filter lanes distinct and alphabetical while duplicate events share a single lane.
        [Test]
        public void RingNameFilterLanesAreDistinctAndAlphabetical()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ringType = GetRingRotationType();
            var zebra = PlaceRingRotation(1, ringType, "zebra");
            var alpha = PlaceRingRotation(2, ringType, "alpha");
            PlaceRingRotation(3, ringType, "zebra");

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            var baseLane = labels.EventTypeToLaneId(ringType);
            var alphaLane = labels.EventToLaneId(alpha);
            var zebraLane = labels.EventToLaneId(zebra);
            Assert.Greater(alphaLane, baseLane);
            Assert.Greater(zebraLane, alphaLane);
        }

        // Prevent name filters on ordinary light tracks from creating ring-only virtual lanes.
        [Test]
        public void LightNameFiltersDoNotCreateVirtualLanes()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var light = PlaceLeftLasers(1);
            light.CustomNameFilter = "ignored";
            light.WriteCustom();

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            Assert.AreEqual(labels.EventTypeToLaneId(light.Type), labels.EventToLaneId(light));
        }

        // Apply collection replacements so filter counts follow final names and types without a map-wide scan.
        [Test]
        public void RingNameFilterLanesReflectCollectionReplacements()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ringType = GetRingRotationType();
            var first = PlaceRingRotation(1, ringType, "drums");
            var second = PlaceRingRotation(2, ringType, "synth");

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreNotEqual(labels.EventToLaneId(first), labels.EventToLaneId(second));

            second = ReplaceEvent(second, evt => evt.CustomNameFilter = "drums");
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventToLaneId(first), labels.EventToLaneId(second));

            first = ReplaceEvent(first, evt => evt.CustomNameFilter = null);
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventTypeToLaneId(first.Type), labels.EventToLaneId(first));
            Assert.AreNotEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));

            second = ReplaceEvent(second, evt => evt.Type = (int)EventTypeValue.Event2);
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));
        }

        // Ignore empty filter values so they cannot create a blank virtual lane.
        [Test]
        public void EmptyRingNameFiltersDoNotCreateVirtualLanes()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ring = PlaceRingRotation(1, GetRingRotationType(), string.Empty);

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            Assert.AreEqual(labels.EventTypeToLaneId(ring.Type), labels.EventToLaneId(ring));
        }

        private static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

        // Shared paste, multi-selection, and cross-track matrices cover both ring effects and both laser-speed sides.
        private static IEnumerable<TestCaseData> AllMovementPreviewKinds
        {
            get
            {
                foreach (BasicMovementPreviewKind kind in System.Enum.GetValues(typeof(BasicMovementPreviewKind)))
                {
                    yield return new TestCaseData(kind);
                }
            }
        }

        // Cover entering and vacating an established interval from both directions for rings and both laser-speed sides.
        private static IEnumerable<TestCaseData> MovementTimeShiftCases =>
            CreateMovementTimeShiftCases("MatchesFullPreviewRebuild");

        // Give undo/redo rows distinct NUnit identities while retaining the same directional coverage.
        private static IEnumerable<TestCaseData> MovementTimeShiftUndoCases =>
            CreateMovementTimeShiftCases("UndoRedoRestoresPreview");

        // Explicit long jumps cover both timeline directions for ring zoom, ring rotation, and each laser-speed side.
        private static IEnumerable<TestCaseData> MovementMultiNodeJumpCases =>
            CreateMovementMultiNodeJumpCases("MatchesFullPreviewRebuild");

        // Give the long-jump undo/redo rows distinct NUnit identities from their direct-action counterparts.
        private static IEnumerable<TestCaseData> MovementMultiNodeJumpUndoCases =>
            CreateMovementMultiNodeJumpCases("UndoRedoRestoresPreview");

        // Generate the same five-node crossing matrix for direct actions and complete undo/redo round trips.
        private static IEnumerable<TestCaseData> CreateMovementMultiNodeJumpCases(string expectedBehavior)
        {
            foreach (BasicMovementPreviewKind kind in System.Enum.GetValues(typeof(BasicMovementPreviewKind)))
            {
                yield return new TestCaseData(kind, true)
                    .SetName($"{kind}_TimeShiftForwardAcrossFiveNodes_{expectedBehavior}");
                yield return new TestCaseData(kind, false)
                    .SetName($"{kind}_TimeShiftBackwardAcrossFiveNodes_{expectedBehavior}");
            }
        }

        // Time-shift matrices need every ring/laser kind, enter/vacate operation, and timeline direction.
        private static IEnumerable<TestCaseData> CreateMovementTimeShiftCases(string expectedBehavior)
        {
            // Generate every movement kind, interval operation, and timeline direction without duplicating test bodies.
            foreach (BasicMovementPreviewKind kind in System.Enum.GetValues(typeof(BasicMovementPreviewKind)))
            {
                foreach (var startsInside in new[] { false, true })
                {
                    foreach (var movesForward in new[] { true, false })
                    {
                        var operation = startsInside ? "OutOfInterval" : "IntoInterval";
                        var direction = movesForward ? "Forward" : "Backward";
                        yield return new TestCaseData(kind, startsInside, movesForward)
                            .SetName($"{kind}_TimeShift{operation}_{direction}_{expectedBehavior}");
                    }
                }
            }
        }

        // Movement time-shift rows use symmetric four-beat crossings so direction is the only changed variable.
        private static (float OriginalTime, float TargetTime) GetMovementTimeShiftTimes(
            bool startsInside,
            bool movesForward)
        {
            // Use the same four-beat crossing for incremental and undo matrices so their only difference is action reversal.
            if (startsInside)
            {
                return movesForward ? (6f, 10f) : (6f, 2f);
            }

            return movesForward ? (2f, 6f) : (10f, 6f);
        }

        // Generate both time directions for each destination-role arrangement without duplicating scenario bodies.
        private static IEnumerable<TestCaseData> TimeShiftIntoTransitionCases =>
            CreateDirectionalTransitionCases("Into");

        // Reuse the same role matrix in reverse to prove the vacated interval reconnects correctly.
        private static IEnumerable<TestCaseData> TimeShiftOutOfTransitionCases =>
            CreateDirectionalTransitionCases("OutOf");

        // Cross every destination arrangement with all source-neighbor combinations and one offscreen-neighbor case.
        private static IEnumerable<TestCaseData> CrossLaneTransitionCases
        {
            get
            {
                foreach (PreviewTransitionPattern destinationPattern in System.Enum.GetValues(
                             typeof(PreviewTransitionPattern)))
                {
                    foreach (SourceNeighborPattern sourcePattern in System.Enum.GetValues(typeof(SourceNeighborPattern)))
                    {
                        yield return new TestCaseData(destinationPattern, sourcePattern, false)
                            .SetName($"CrossLane_{destinationPattern}_Vacates{sourcePattern}");
                    }
                }

                yield return new TestCaseData(
                        PreviewTransitionPattern.OnIntoOnTransition,
                        SourceNeighborPattern.OnTransition,
                        true)
                    .SetName("CrossLane_OnIntoOnTransition_WithBothSidesOutsideOrdinaryBounds");
            }
        }

        // Paste every destination arrangement and repeat a representative transition with both point nodes offscreen.
        private static IEnumerable<TestCaseData> PasteTransitionCases
        {
            get
            {
                foreach (PreviewTransitionPattern pattern in System.Enum.GetValues(typeof(PreviewTransitionPattern)))
                {
                    yield return new TestCaseData(pattern, false).SetName($"PasteUndo_{pattern}");
                }

                yield return new TestCaseData(PreviewTransitionPattern.OnIntoOnTransition, true)
                    .SetName("PasteUndo_OnIntoOnTransition_WithBothSidesOutsideOrdinaryBounds");
            }
        }

        private static IEnumerable<TestCaseData> CreateDirectionalTransitionCases(string operation)
        {
            // Give every role arrangement distinct forward and backward test identities in Unity's runner.
            foreach (PreviewTransitionPattern pattern in System.Enum.GetValues(typeof(PreviewTransitionPattern)))
            {
                yield return new TestCaseData(pattern, true).SetName($"TimeShift{operation}_{pattern}_Forward");
                yield return new TestCaseData(pattern, false).SetName($"TimeShift{operation}_{pattern}_Backward");
            }
        }

        private static (bool MovedTransition, bool PreviousTransition, bool NextTransition) GetTransitionRoles(
            PreviewTransitionPattern pattern)
        {
            // Spell out each node role so the matrix covers the requested A-E transition combinations explicitly.
            return pattern switch
            {
                PreviewTransitionPattern.OnIntoOnTransition => (false, false, true),
                PreviewTransitionPattern.OnIntoTransitionTransition => (false, true, true),
                PreviewTransitionPattern.TransitionIntoOnTransition => (true, false, true),
                PreviewTransitionPattern.TransitionIntoTransitionTransition => (true, true, true),
                PreviewTransitionPattern.TransitionIntoOnOn => (true, false, false),
                PreviewTransitionPattern.TransitionIntoTransitionOn => (true, true, false),
                _ => throw new System.ArgumentOutOfRangeException(nameof(pattern), pattern, null)
            };
        }

        private static (bool PreviousTransition, bool NextTransition) GetSourceNeighborRoles(
            SourceNeighborPattern pattern)
        {
            // Source-lane coverage includes every possible transition state ahead of and behind the shifted node.
            return pattern switch
            {
                SourceNeighborPattern.OnOn => (false, false),
                SourceNeighborPattern.OnTransition => (false, true),
                SourceNeighborPattern.TransitionOn => (true, false),
                SourceNeighborPattern.TransitionTransition => (true, true),
                _ => throw new System.ArgumentOutOfRangeException(nameof(pattern), pattern, null)
            };
        }

        private LivePreviewLightController CreateLivePreviewLight(BeatmapRuntimeContext context, int eventType)
        {
            // Use a dedicated scene light so this test reads the exact BasicLightEffect cache affected by the action.
            var livePreviewLightObject = new GameObject($"Live Preview Light Test Target {eventType}");
            livePreviewLightObjects.Add(livePreviewLightObject);
            var previewLight = livePreviewLightObject.AddComponent<LivePreviewLightController>();
            previewLight.Type = eventType;
            previewLight.ID = -1;

            var effect = context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(eventType);
            effect.Register(previewLight);
            effect.Initialize();
            return previewLight;
        }

        // Build deterministic authored movement values so stale speed, step, propagation, and direction remain distinguishable.
        private static BaseEvent PlaceMovementEvent(
            float time,
            int eventType,
            BasicMovementPreviewKind kind,
            int role)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = eventType,
                Value = GetMovementEventValue(kind, role),
                FloatValue = 1f
            };

            // Author both zoom and rotation fields so a cross-track move remains deterministic after changing consumers.
            var rotations = new[] { 45f, 90f, -120f, 180f, -240f };
            var steps = new[] { 0.5f, 2f, -1.5f, 4f, -4f };
            var propagations = new[] { 12f, 8f, 15f, 6f, 20f };
            var speeds = new[] { 3f, 5f, 2f, 8f, 10f };
            evt.CustomRingRotation = rotations[role];
            evt.CustomStep = steps[role];
            evt.CustomProp = propagations[role];
            evt.CustomSpeed = speeds[role];
            evt.CustomDirection = role % 2;
            // Laser-speed rebuilds need deterministic authored angles rather than a newly randomized start offset.
            evt.CustomLockRotation = kind is BasicMovementPreviewKind.LaserSpeedLeft
                or BasicMovementPreviewKind.LaserSpeedRight;

            return PlaceUtils.Place(evt);
        }

        // Laser-speed anchors alternate running and stopped states while ring event values retain their native semantics.
        private static int GetMovementEventValue(BasicMovementPreviewKind kind, int role)
        {
            if (kind == BasicMovementPreviewKind.RingRotation)
            {
                return 0;
            }

            if (kind is BasicMovementPreviewKind.LaserSpeedLeft or BasicMovementPreviewKind.LaserSpeedRight)
            {
                return LaserMovementValues[role];
            }

            return role + 1;
        }

        // All movement rows share the same distinguishable surrounding state chain for a rebuild comparison.
        private static void PlaceMovementAnchors(int eventType, BasicMovementPreviewKind kind)
        {
            // Reuse one surrounding sequence so cross-track tests differ only by the edited source and destination.
            PlaceMovementEvent(1f, eventType, kind, 0);
            PlaceMovementEvent(4f, eventType, kind, 1);
            PlaceMovementEvent(8f, eventType, kind, 2);
            PlaceMovementEvent(12f, eventType, kind, 3);
        }

        // Long-jump tests surround the moving node with five distinct cached states and stable outer sentinels.
        private static BaseEvent PlaceMultiNodeMovementJumpSequence(
            int eventType,
            BasicMovementPreviewKind kind,
            bool movesForward)
        {
            PlaceMovementEvent(0.5f, eventType, kind, 0);
            PlaceMovementEvent(3f, eventType, kind, 1);
            PlaceMovementEvent(5f, eventType, kind, 2);
            PlaceMovementEvent(7f, eventType, kind, 3);
            PlaceMovementEvent(9f, eventType, kind, 0);
            PlaceMovementEvent(11f, eventType, kind, 1);
            PlaceMovementEvent(13f, eventType, kind, 2);
            PlaceMovementEvent(15f, eventType, kind, 3);
            return PlaceMovementEvent(movesForward ? 2f : 12f, eventType, kind, 4);
        }

        // Cross-track movement rows require the opposite cached movement consumer for their destination.
        private static BasicMovementPreviewKind GetOtherMovementKind(BasicMovementPreviewKind kind)
        {
            // Cross-track matrices stay within the ring pair or the left/right laser-speed pair.
            return kind switch
            {
                BasicMovementPreviewKind.RingZoom => BasicMovementPreviewKind.RingRotation,
                BasicMovementPreviewKind.RingRotation => BasicMovementPreviewKind.RingZoom,
                BasicMovementPreviewKind.LaserSpeedLeft => BasicMovementPreviewKind.LaserSpeedRight,
                BasicMovementPreviewKind.LaserSpeedRight => BasicMovementPreviewKind.LaserSpeedLeft,
                _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        // Select an event type backed by a populated production movement effect instead of assuming one environment layout.
        private static int GetMovementEventType(
            BeatmapRuntimeContext context,
            BasicMovementPreviewKind kind)
        {
            foreach (var (eventType, effects) in context.Descriptor.BasicEventEffectManager.EventTypeToEffects)
            {
                var components = context.TrackDefinitions.GetBasicOrDefault(eventType).Components;
                var matchesLaserSide = kind switch
                {
                    BasicMovementPreviewKind.LaserSpeedLeft =>
                        components.HasFlag(BasicEventComponent.LightRotationLeft),
                    BasicMovementPreviewKind.LaserSpeedRight =>
                        components.HasFlag(BasicEventComponent.LightRotationRight),
                    _ => false
                };
                if (matchesLaserSide && effects.Any(HasDeterministicLaserSpeedVisual))
                {
                    return eventType;
                }

                for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    var effect = effects[effectIndex];
                    if (kind == BasicMovementPreviewKind.RingZoom
                        && effect is TrackLaneRingsPositionEffect positionEffect
                        && positionEffect.Visual != null
                        && positionEffect.Visual.RingManager != null
                        && positionEffect.Visual.RingManager.Rings.Count > 0)
                    {
                        return eventType;
                    }

                    if (kind == BasicMovementPreviewKind.RingRotation
                        && effect is TrackLaneRingsRotationEffect rotationEffect
                        && rotationEffect.Visual != null
                        && rotationEffect.Visual.Manager != null
                        && rotationEffect.Visual.Manager.Rings.Count > 0)
                    {
                        return eventType;
                    }
                }
            }

            Assert.Fail($"The active test environment has no populated {kind} Basic Event effect.");
            return -1;
        }

        // Laser-speed cache regressions need a rebuild oracle that cannot fail solely because a visual rerolled randomness.
        // LightPairSinMoveEffect is intentionally excluded: reinitialize clears and rerolls its random phase independently
        // of cache correctness, while these rotation visuals still consume the same real laser-speed event/action timeline.
        private static bool HasDeterministicLaserSpeedVisual(StateManager<BaseEvent> effect)
        {
            if (effect is LightRotationEffect rotationEffect)
            {
                return rotationEffect.Visual != null && rotationEffect.Visual.Transform != null;
            }

            if (effect is LightPairRotationEffect pairEffect
                && pairEffect.Visual != null
                && pairEffect.Visual.Transforms != null)
            {
                return pairEffect.Visual.Transforms.Any(
                    container => container != null && container.Transform != null);
            }

            return false;
        }

        // Capture every deterministic scene transform driven by the edited ring or laser-speed event type.
        private List<MovementPreviewTarget> GetMovementPreviewTargets(
            BeatmapRuntimeContext context,
            params int[] eventTypes)
        {
            // Every movement test that claims scene targets also owns restoring those shared transforms in AfterCleanup.
            restoreMovementPreviewAfterTest = true;
            var targets = new List<MovementPreviewTarget>();
            var seenTransforms = new HashSet<Transform>();

            void AddTarget(Transform transform, string description)
            {
                if (transform != null && seenTransforms.Add(transform))
                {
                    targets.Add(new MovementPreviewTarget(transform, description));
                }
            }

            for (var typeIndex = 0; typeIndex < eventTypes.Length; typeIndex++)
            {
                var eventType = eventTypes[typeIndex];
                Assert.That(
                    context.Descriptor.BasicEventEffectManager.EventTypeToEffects.TryGetValue(
                        eventType,
                        out var effects),
                    Is.True,
                    $"Basic Event type {eventType} has no registered preview effects.");

                for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    var effect = effects[effectIndex];
                    TrackLaneRingsManager manager = null;
                    if (effect is TrackLaneRingsPositionEffect positionEffect)
                    {
                        manager = positionEffect.Visual != null ? positionEffect.Visual.RingManager : null;
                    }
                    else if (effect is TrackLaneRingsRotationEffect rotationEffect)
                    {
                        manager = rotationEffect.Visual != null ? rotationEffect.Visual.Manager : null;
                    }

                    if (manager != null)
                    {
                        for (var ringIndex = 0; ringIndex < manager.Rings.Count; ringIndex++)
                        {
                            var ring = manager.Rings[ringIndex];
                            Assert.That(ring, Is.Not.Null, $"{effect.name} has a null ring at index {ringIndex}.");
                            Assert.That(
                                ring.CachedTransform,
                                Is.Not.Null,
                                $"{effect.name} ring {ringIndex} has not initialized its cached transform.");
                            // Movement preview regressions compare each initialized production ring transform exactly once.
                            AddTarget(
                                ring.CachedTransform,
                                $"Basic Event type {eventType}, effect {effect.name}, ring {ringIndex}");
                        }
                    }

                    if (effect is LightRotationEffect lightRotationEffect
                        && lightRotationEffect.Visual != null)
                    {
                        AddTarget(
                            lightRotationEffect.Visual.Transform,
                            $"Basic Event type {eventType}, effect {effect.name}, laser rotation");
                    }

                    if (effect is LightPairRotationEffect pairRotationEffect
                        && pairRotationEffect.Visual != null
                        && pairRotationEffect.Visual.Transforms != null)
                    {
                        for (var laserIndex = 0;
                             laserIndex < pairRotationEffect.Visual.Transforms.Length;
                             laserIndex++)
                        {
                            var laser = pairRotationEffect.Visual.Transforms[laserIndex];
                            AddTarget(
                                laser != null ? laser.Transform : null,
                                $"Basic Event type {eventType}, effect {effect.name}, paired laser {laserIndex}");
                        }
                    }
                }
            }

            Assert.That(
                targets,
                Is.Not.Empty,
                "No deterministic rendered ring or laser transforms were found for the movement preview test.");
            return targets;
        }

        private static BaseEvent PlaceLightEvent(
            float time,
            int eventType,
            LightValue value,
            Color customColor,
            int[] customLightIds = null)
        {
            // Route test setup through real Basic Event placement so every production action listener participates.
            return PlaceUtils.Place(new BaseEvent
            {
                JsonTime = time,
                Type = eventType,
                Value = (int)value,
                FloatValue = 1f,
                CustomColor = customColor,
                CustomLightID = customLightIds
            });
        }

        // The alpha-zero cache regressions use the reported V2 layout verbatim: an alpha-zero-to-one quarter-beat
        // gradient, a one-to-zero half-beat gradient, and a final alpha-zero custom-color node.
        private static (BaseEvent FadeIn, BaseEvent FadeOut, BaseEvent Zero) PlaceLegacyAlphaGradientSequence(
            int eventType,
            float startTime,
            Color opaqueColor)
        {
            var transparentColor = opaqueColor.WithAlpha(0f);
            var fadeIn = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = startTime,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomLightGradient = new ChromaLightGradient(
                    transparentColor,
                    opaqueColor,
                    0.25f,
                    "easeLinear")
            });
            var fadeOut = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = startTime + 0.25f,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomLightGradient = new ChromaLightGradient(
                    opaqueColor,
                    transparentColor,
                    0.5f,
                    "easeLinear")
            });
            var zero = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = startTime + 0.75f,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomColor = transparentColor
            });
            return (fadeIn, fadeOut, zero);
        }

        // Non-opaque coverage uses one self-contained legacy gradient plus its coincident final color event so midpoint
        // samples distinguish continuous interpolation from a cache that holds the source and snaps at the destination.
        private static void PlaceLegacyNonOpaqueGradientSequence(
            int eventType,
            float startTime,
            Color startColor,
            Color endColor)
        {
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = startTime,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomLightGradient = new ChromaLightGradient(
                    startColor,
                    endColor,
                    0.5f,
                    "easeLinear")
            });
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = startTime + 0.5f,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomColor = endColor
            });
        }

        // Read the actual BasicLightEffect output after a production playhead seek so each assertion covers cache lookup,
        // color interpolation, brightness multiplication, and the scene-light value seen by DefaultEnvironment.
        private static void AssertPreviewColorAt(
            AudioTimeSyncController atsc,
            LivePreviewLightController previewLight,
            float jsonTime,
            Color expected)
        {
            atsc.MoveToJsonTime(jsonTime);
            AssertColorsEqualRoundedToThreeDecimalPlaces(
                expected,
                previewLight.Color,
                $"Legacy Chroma gradient preview was wrong at JSON beat {jsonTime} for Basic Event type {previewLight.Type}");
        }

        private static BaseEvent PlaceScenarioLightEvent(
            float time,
            int eventType,
            bool transition,
            Color customColor)
        {
            // Keep transition role independent from custom color so every matrix row differs only in authored node type.
            return PlaceLightEvent(
                time,
                eventType,
                transition ? LightValue.BlueTransition : LightValue.BlueOn,
                customColor);
        }

        private static void RebuildBasicPreview(BeatmapRuntimeContext context)
        {
            // Start each mutation from a known-correct cache so a failure belongs to the action under test.
            context.Descriptor.BasicEventEffectManager.Reinitialize();
            context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0f);
        }

        private void PrepareBasicEventEditorInput()
        {
            // Real Basic Event shortcuts are active only while their editor tab owns the selected event nodes.
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            editingModeBeforePreviewTest ??= editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.BasicEvent;
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            eventPlacementStateBeforePreviewTest ??= eventPlacement.State;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            gridMeasureSnappingBeforePreviewTest ??= atsc.GridMeasureSnapping;
            atsc.GridMeasureSnapping = 4;

            // Isolate the same generated bindings and production callbacks from installer blockers left by unrelated tests.
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null, "The application's shared input asset was not initialized.");
            sharedModifyingSelectionInputWasEnabled = sharedInput.ModifyingSelection.enabled;
            sharedActionsInputWasEnabled = sharedInput.Actions.enabled;
            sharedInput.ModifyingSelection.Disable();
            sharedInput.Actions.Disable();

            basicEventInteractionInput = new CMInput();
            basicEventInputSelectionController = Object.FindAnyObjectByType<SelectionController>();
            basicEventInputActionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            basicEventInteractionInput.ModifyingSelection.SetCallbacks(basicEventInputSelectionController);
            basicEventInteractionInput.Actions.SetCallbacks(basicEventInputActionContainer);
            basicEventInteractionInput.ModifyingSelection.Enable();
            basicEventInteractionInput.Actions.Enable();

            Assert.That(basicEventInteractionInput.ModifyingSelection.enabled, Is.True);
            Assert.That(basicEventInteractionInput.Actions.enabled, Is.True);
        }

        // Negative-hover paste tests pass the clamped ghost beat separately while ordinary callers expect the raw hit beat.
        private void HoverBasicEventLaneAt(float jsonTime, int eventType, float? expectedJsonTime = null)
        {
            var eventsContainer = GetEventsContainer();
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var lane = labels.EventTypeToLaneId(eventType);
            Assert.That(lane, Is.GreaterThanOrEqualTo(0), $"No visible Basic Event lane exists for type {eventType}.");

            // Share the actual placement hit path with propagated-lane paste regressions.
            HoverBasicEventVisibleLaneAt(jsonTime, eventType, lane, expectedJsonTime);
        }

        // Propagated All Lights coverage must assert the same independently resolved ghost beat as ordinary lanes.
        private void HoverBasicEventAllLightsLaneAt(float jsonTime, int eventType, float? expectedJsonTime = null)
        {
            // Lane zero in Light ID propagation view is the production All Lights destination.
            var eventsContainer = GetEventsContainer();
            propagationEditingBeforePasteTest ??= eventsContainer.PropagationEditing;
            propagatedEventTypeBeforePasteTest ??= eventsContainer.EventTypeToPropagate;
            eventsContainer.EventTypeToPropagate = eventType;
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;

            const int allLightsLane = 0;
            HoverBasicEventVisibleLaneAt(jsonTime, eventType, allLightsLane, expectedJsonTime);
            Assert.That(
                Object.FindAnyObjectByType<EventPlacement>().QueuedData.CustomLightID,
                Is.Null,
                "Hovering the All Lights lane must resolve to an unscoped Basic Event before paste.");
        }

        // Resolve real grid input at the raw hit beat while allowing boundary tests to verify its clamped preview result.
        private void HoverBasicEventVisibleLaneAt(
            float jsonTime,
            int eventType,
            int lane,
            float? expectedJsonTime = null)
        {
            // Feed a real grid hit through EventPlacement so paste consumes the same hovered beat and lane as the editor.
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            Assert.That(lane, Is.LessThan(labels.LaneCount), $"Visible Basic Event lane {lane} is outside the grid.");

            // Supply the same lane bounds PlacementInputSystem normally derives from the active grid provider.
            eventPlacement.Bounds = new Bounds(
                new Vector3(labels.LaneCount / 2f, 0.5f, 0f),
                new Vector3(labels.LaneCount, 1f, 1f));

            var hoverRoot = new GameObject("Basic Event paste hover test surface");
            basicEventHoverObjects.Add(hoverRoot);
            var hoverHitObject = new GameObject("Basic Event paste hover test hit");
            hoverHitObject.transform.SetParent(hoverRoot.transform);
            var songBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
            var localPoint = new Vector3(lane + 0.5f, 0f, songBpmTime * EditorScaleController.EditorScale);
            var worldPoint = eventPlacement.PlacementTrack.TransformPoint(localPoint);
            eventPlacement.UpdateState(
                new Intersections.IntersectionHit(
                    hoverHitObject,
                    new Bounds(Vector3.zero, Vector3.one),
                    new Ray(worldPoint, Vector3.forward),
                    0f),
                PlacementInputState.Hover);

            Assert.That(eventPlacement.IsActive, Is.True, "Basic Event hover did not activate placement.");
            Assert.That(eventPlacement.QueuedData.Type, Is.EqualTo(eventType), "Hover resolved the wrong event lane.");
            var resolvedJsonTime = expectedJsonTime ?? jsonTime;
            Assert.That(
                eventPlacement.QueuedData.JsonTime,
                Is.EqualTo(resolvedJsonTime).Within(0.00001f),
                "Hover resolved the wrong paste beat.");
            // Hover-paste regressions require the visible ghost and queued anchor to resolve to the same beat.
            var resolvedSongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(resolvedJsonTime);
            Assert.That(
                eventPlacement.PlacementVisualContainer.transform.localPosition.z,
                Is.EqualTo(resolvedSongBpmTime * EditorScaleController.EditorScale).Within(0.00001f),
                "Basic Event ghost did not match the resolved paste beat.");
        }

        // End-boundary hover-paste tests use the loaded audio duration converted through the authoritative BPM map.
        private static float GetFinalLegalJsonTime()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var finalSongBpmTime = atsc.GetBeatFromSeconds(atsc.SongAudioSource.clip.length);
            return (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(finalSongBpmTime);
        }

        private void PressTimeShiftKeys(float beats)
        {
            // Repeat the authored Shift+Arrow gesture at the active grid precision instead of calling MoveSelection directly.
            var presses = GetTimeShiftPressCount(beats);
            var arrow = beats > 0f
                ? UnityEngine.InputSystem.Key.UpArrow
                : UnityEngine.InputSystem.Key.DownArrow;
            for (var press = 0; press < presses; press++)
            {
                PressKeyboardShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>(
                    UnityEngine.InputSystem.Key.LeftShift,
                    arrow);
            }
        }

        // Cross-track ring regressions must traverse the real Ctrl+Arrow lane sequence and count each undoable action.
        private int PressTrackShiftKeys(int sourceEventType, int destinationEventType)
        {
            // Drive every intervening Ctrl+Arrow lane change so source and destination caches see the production action sequence.
            var eventsContainer = GetEventsContainer();
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var sourceLane = labels.EventTypeToLaneId(sourceEventType);
            var destinationLane = labels.EventTypeToLaneId(destinationEventType);
            Assert.That(sourceLane, Is.GreaterThanOrEqualTo(0));
            Assert.That(destinationLane, Is.GreaterThanOrEqualTo(0));
            var presses = Mathf.Abs(destinationLane - sourceLane);
            Assert.That(presses, Is.GreaterThan(0));
            var arrow = destinationLane > sourceLane
                ? UnityEngine.InputSystem.Key.RightArrow
                : UnityEngine.InputSystem.Key.LeftArrow;
            for (var press = 0; press < presses; press++)
            {
                PressKeyboardShortcutExpectingAction<BeatmapObjectModifiedCollectionAction>(
                    UnityEngine.InputSystem.Key.LeftCtrl,
                    arrow);
            }

            return presses;
        }

        // Time-shift undo rows must reverse every physical Shift+Arrow action at the active grid precision.
        private static int GetTimeShiftPressCount(float beats)
        {
            // Undo/redo matrices must reverse exactly the number of physical Shift+Arrow actions used by the forward edit.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var step = 1f / atsc.GridMeasureSnapping;
            var presses = Mathf.RoundToInt(Mathf.Abs(beats) / step);
            Assert.That(presses * step, Is.EqualTo(Mathf.Abs(beats)).Within(0.00001f));
            return presses;
        }

        private TAction PressKeyboardShortcutExpectingAction<TAction>(
            UnityEngine.InputSystem.Key modifier,
            UnityEngine.InputSystem.Key key)
            where TAction : BeatmapAction
        {
            // Fail at the physical shortcut boundary before stale selection data can be mistaken for a preview-cache result.
            TAction createdAction = null;
            var matchingActions = 0;

            void HandleActionCreated(BeatmapAction action)
            {
                if (action is not TAction expectedAction)
                {
                    return;
                }

                createdAction = expectedAction;
                matchingActions++;
            }

            BeatmapActionContainer.OnActionCreated += HandleActionCreated;
            try
            {
                PressKeyboardShortcut(modifier, key);
            }
            finally
            {
                BeatmapActionContainer.OnActionCreated -= HandleActionCreated;
            }

            Assert.That(
                matchingActions,
                Is.EqualTo(1),
                $"The physical {modifier}+{key} shortcut did not create exactly one {typeof(TAction).Name}. "
                + lastPhysicalShortcutDiagnostics);
            return createdAction;
        }

        private void PressUndoShortcutExpectingAction<TAction>() where TAction : BeatmapAction
        {
            // Require one real undo callback so duplicated or missing input registrations fail before cache assertions.
            var matchingActions = 0;

            void HandleActionUndo(BeatmapAction action)
            {
                if (action is TAction)
                {
                    matchingActions++;
                }
            }

            BeatmapActionContainer.OnActionUndo += HandleActionUndo;
            try
            {
                PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.Z);
            }
            finally
            {
                BeatmapActionContainer.OnActionUndo -= HandleActionUndo;
            }

            Assert.That(
                matchingActions,
                Is.EqualTo(1),
                $"The physical Ctrl+Z shortcut did not undo exactly one {typeof(TAction).Name}. "
                + lastPhysicalShortcutDiagnostics);
        }

        private void PressRedoShortcutExpectingAction<TAction>() where TAction : BeatmapAction
        {
            // Movement cache round trips must exercise the production Ctrl+Y callback and prove exactly one action was replayed.
            var matchingActions = 0;

            void HandleActionRedo(BeatmapAction action)
            {
                if (action is TAction)
                {
                    matchingActions++;
                }
            }

            BeatmapActionContainer.OnActionRedo += HandleActionRedo;
            try
            {
                PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.Y);
            }
            finally
            {
                BeatmapActionContainer.OnActionRedo -= HandleActionRedo;
            }

            Assert.That(
                matchingActions,
                Is.EqualTo(1),
                $"The physical Ctrl+Y shortcut did not redo exactly one {typeof(TAction).Name}. "
                + lastPhysicalShortcutDiagnostics);
        }

        private void PressKeyboardShortcut(
            UnityEngine.InputSystem.Key modifier,
            UnityEngine.InputSystem.Key key)
        {
            // Queue physical keyboard states through InputSystem so CMInput invokes the same production callbacks as the user gesture.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var addedKeyboard = keyboard == null;
            if (addedKeyboard)
            {
                keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            }

            // Record whether generated bindings performed even when their production callback did not create an action.
            var performedActions = new List<string>();
            void HandlePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context) =>
                performedActions.Add($"{context.action.actionMap.name}/{context.action.name}");

            var inputActions = basicEventInteractionInput.asset.actionMaps.SelectMany(map => map.actions).ToArray();
            foreach (var action in inputActions)
            {
                action.performed += HandlePerformed;
            }

            var keyboardAddedBefore = keyboard.added;
            var keyboardEnabledBefore = keyboard.enabled;

            try
            {
                QueueKeyboardState(keyboard, modifier);
                QueueKeyboardState(keyboard, modifier, key);
                QueueKeyboardState(keyboard, modifier);
                QueueKeyboardState(keyboard);
            }
            finally
            {
                foreach (var action in inputActions)
                {
                    action.performed -= HandlePerformed;
                }

                lastPhysicalShortcutDiagnostics =
                    $"Input diagnostics: applicationFocused={Application.isFocused}, "
                    + $"keyboardAddedBefore={keyboardAddedBefore}, keyboardEnabledBefore={keyboardEnabledBefore}, "
                    + $"keyboardAddedAfter={keyboard.added}, keyboardEnabledAfter={keyboard.enabled}, "
                    + $"backgroundBehavior={UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior}, "
                    + $"editorInputBehavior={UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode}, "
                    + $"performed=[{string.Join(", ", performedActions)}].";

                if (addedKeyboard)
                {
                    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                }
            }
        }

        private static void QueueKeyboardState(
            UnityEngine.InputSystem.Keyboard keyboard,
            params UnityEngine.InputSystem.Key[] keys)
        {
            // Process each key edge immediately so modifier activation precedes the shortcut's performed callback.
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(
                keyboard,
                new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));
            UnityEngine.InputSystem.InputSystem.Update();
        }

        private IEnumerator TimeShiftWithVisualChunkStateMatchesFullPreviewRebuild(
            float viewJsonTime,
            float stagingJsonTime,
            float nextJsonTime,
            bool remainsInLoadedChunk)
        {
            // Keep this shared scenario identical except for whether the simulator cursor shares the edited state's bucket.
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            const int eventType = (int)EventTypeValue.Event2;
            var previewLight = CreateLivePreviewLight(context, eventType);
            EnableChromaLitePreview();

            PlaceLightEvent(0.5f, eventType, LightValue.RedOn, Color.white);
            var moved = PlaceLightEvent(1f, eventType, LightValue.BlueOn, Color.green);
            var previous = PlaceLightEvent(3f, eventType, LightValue.BlueOn, Color.red);
            // Keep the playhead inside the destination transition so the first sample catches a stale active tween.
            var next = PlaceLightEvent(nextJsonTime, eventType, LightValue.BlueTransition, Color.blue);
            PlaceLightEvent(nextJsonTime + 4f, eventType, LightValue.RedOn, Color.cyan);
            RebuildBasicPreview(context);

            PrepareBasicEventEditorInput();
            if (remainsInLoadedChunk)
            {
                // A wide pool keeps the selected event resident even though the track shader clips it behind the view.
                UseVisualChunkWindow(8);
            }
            else
            {
                UseNarrowVisualChunkWindow();
            }
            SelectionController.Select(moved);
            yield return MoveViewAcrossChunkBoundary(stagingJsonTime, viewJsonTime);

            var eventsContainer = GetEventsContainer();
            Assert.That(
                moved.SongBpmTime,
                Is.LessThan(Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentSongBpmTime
                    - (Settings.Instance.TrackLength / 4f)),
                "The selected node remained inside the visible rear track boundary.");
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(moved), Is.EqualTo(remainsInLoadedChunk));
            PressTimeShiftKeys(3f);

            var shifted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shifted.JsonTime, Is.EqualTo(4f));
            // The edit makes beat 4 the source of the transition to next, so its crossing ribbon must recreate the visual.
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(shifted), Is.True);
            Assert.That(shifted.Prev, Is.EqualTo(previous));
            Assert.That(shifted.Next, Is.EqualTo(next));
            AssertLivePreviewMatchesFullRebuild(
                context,
                new[] { previewLight },
                viewJsonTime,
                nextJsonTime - 2f,
                2f,
                4.5f,
                nextJsonTime + 1f,
                2f,
                viewJsonTime);
        }

        // Exercise the same physical time shift after LateUpdate has either retained or recycled the selected ring node visual.
        private IEnumerator TimeShiftMovementWithVisualChunkStateMatchesFullPreviewRebuild(
            BasicMovementPreviewKind kind,
            float viewJsonTime,
            float stagingJsonTime,
            float nextJsonTime,
            bool remainsInLoadedChunk)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementEvent(0.5f, eventType, kind, 0);
            var moved = PlaceMovementEvent(1f, eventType, kind, 4);
            PlaceMovementEvent(3f, eventType, kind, 1);
            PlaceMovementEvent(nextJsonTime, eventType, kind, 2);
            PlaceMovementEvent(nextJsonTime + 4f, eventType, kind, 3);
            RebuildBasicMovementPreview(context);
            // The unloaded-visual regression starts only after later state buckets have already been rendered.
            PrimeMovementPreviewCache();

            PrepareBasicEventEditorInput();
            if (remainsInLoadedChunk)
            {
                // A wide pool keeps the selected event resident while its node is clipped behind the visible track.
                UseVisualChunkWindow(8);
            }
            else
            {
                UseNarrowVisualChunkWindow();
            }

            SelectionController.Select(moved);
            yield return MoveViewAcrossChunkBoundary(stagingJsonTime, viewJsonTime);

            var eventsContainer = GetEventsContainer();
            Assert.That(
                moved.SongBpmTime,
                Is.LessThan(Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentSongBpmTime
                    - (Settings.Instance.TrackLength / 4f)),
                "The selected movement node remained inside the visible rear track boundary.");
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(moved), Is.EqualTo(remainsInLoadedChunk));
            PressTimeShiftKeys(3f);

            var shifted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shifted.JsonTime, Is.EqualTo(4f));
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(nextJsonTime - 2f);
            yield return null;
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(2f);
            yield return null;
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(viewJsonTime);
            yield return null;

            var sampleTimes = new[]
            {
                viewJsonTime, nextJsonTime - 2f, 2f, 4.5f, nextJsonTime + 1f, 2f, viewJsonTime
            };
            AssertMovementPreviewMatchesFullRebuild(context, targets, sampleTimes);
        }

        // Reproduce paste and undo after both source and destination visuals have crossed real chunk boundaries.
        private IEnumerator PasteUndoMovementAcrossVisualChunksRestoresBaselinePreview(
            BasicMovementPreviewKind kind)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var eventType = GetMovementEventType(context, kind);
            var targets = GetMovementPreviewTargets(context, eventType);
            PlaceMovementEvent(0.5f, eventType, kind, 0);
            PlaceMovementEvent(3f, eventType, kind, 1);
            PlaceMovementEvent(35f, eventType, kind, 2);
            var copied = PlaceMovementEvent(40f, eventType, kind, 4);
            PlaceMovementEvent(44f, eventType, kind, 3);
            RebuildBasicMovementPreview(context);
            var sampleTimes = new[] { 2f, 10f, 19.75f, 25f, 34f, 36f, 42f, 19.75f };
            // Playback must exercise every production callback through the distant pasted beat.
            var playbackSampleTimes = CreateMovementPlaybackJsonTimes(sampleTimes);
            var baseline = CaptureMovementPreview(targets, sampleTimes);
            var baselinePlayback = CaptureMovementPlayback(targets, playbackSampleTimes);

            PrepareBasicEventEditorInput();
            UseNarrowVisualChunkWindow();
            SelectionController.Select(copied);
            PressKeyboardShortcut(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.C);
            yield return MoveViewAcrossChunkBoundary(30f, 19.75f);

            var eventsContainer = GetEventsContainer();
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(copied), Is.False);
            HoverBasicEventLaneAt(20f, eventType);
            PressKeyboardShortcutExpectingAction<SelectionPastedAction>(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.V);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(30f);
            yield return null;
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(10f);
            yield return null;
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(19.75f);
            yield return null;
            PressUndoShortcutExpectingAction<SelectionPastedAction>();

            Assert.That(eventsContainer.MapObjects.OfType<BaseEvent>().Any(evt => evt.JsonTime == 20f), Is.False);
            AssertMovementPreviewMatchesBaseline(
                baseline,
                CaptureMovementPreview(targets, sampleTimes),
                targets,
                sampleTimes,
                "paste, chunk scrub, and undo");
            AssertMovementPreviewMatchesBaseline(
                baselinePlayback,
                CaptureMovementPlayback(targets, playbackSampleTimes),
                targets,
                playbackSampleTimes,
                "paste, chunk scrub, and undo playback");

            PressRedoShortcutExpectingAction<SelectionPastedAction>();
            AssertMovementPreviewMatchesFullRebuild(context, targets, sampleTimes);
        }

        private void UseNarrowVisualChunkWindow()
        {
            // Two grid chunks leave a five-beat loading radius, allowing selection to survive after its visual is recycled.
            UseVisualChunkWindow(2);
        }

        private void UseVisualChunkWindow(int chunkDistance)
        {
            // Preserve the configured radius once, then let real LateUpdate refreshes apply the requested test window.
            chunkDistanceBeforePreviewTest ??= Settings.Instance.ChunkDistance;
            Settings.Instance.ChunkDistance = chunkDistance;
        }

        private static IEnumerator MoveViewAcrossChunkBoundary(float stagingJsonTime, float targetJsonTime)
        {
            // Yield after each real playhead move so BeatmapObjectContainerCollection.LateUpdate performs normal chunk recycling.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(stagingJsonTime);
            yield return null;
            atsc.MoveToJsonTime(targetJsonTime);
            yield return null;
        }

        private void RestrictOrdinaryEventWindowToMiddle(params BaseEvent[] outsideEvents)
        {
            // Put both neighbor points outside the ordinary window while retaining any source needed by a crossing ribbon.
            var map = BeatSaberSongContainer.Instance.Map;
            var lowerBound = (float)map.JsonTimeToSongBpmTime(5.5f);
            var upperBound = (float)map.JsonTimeToSongBpmTime(6.5f);
            var eventsContainer = GetEventsContainer();
            eventsContainer.RefreshPool(lowerBound, upperBound, true);
            restrictedEventPoolForPreviewTest = true;

            foreach (var outsideEvent in outsideEvents)
            {
                var isOutsideOrdinaryWindow = outsideEvent.SongBpmTime < lowerBound
                    || outsideEvent.SongBpmTime > upperBound;
                Assert.That(
                    isOutsideOrdinaryWindow,
                    Is.True,
                    $"Expected the neighbor at JSON beat {outsideEvent.JsonTime} to be outside ordinary loaded bounds.");

                var shouldRetainRibbonSource = OwnsRibbonCrossingBoundary(outsideEvent, lowerBound);
                Assert.That(
                    eventsContainer.LoadedContainers.ContainsKey(outsideEvent),
                    Is.EqualTo(shouldRetainRibbonSource),
                    $"The offscreen neighbor at JSON beat {outsideEvent.JsonTime} had the wrong ribbon-retention state.");
            }
        }

        private static bool OwnsRibbonCrossingBoundary(BaseEvent source, float lowerBound)
        {
            if (!Settings.Instance.VisualizeChromaGradients || source.SongBpmTime >= lowerBound)
            {
                return false;
            }

            if (source.CustomLightGradient != null)
            {
                // Authored gradients use their rendered SongBpmTime duration for the same retention decision as the grid.
                return source.SongBpmTime + source.CustomLightGradient.Duration >= lowerBound;
            }

            // A synthesized Basic Event ribbon belongs to the preceding non-fade source, not its transition target.
            return !source.IsFade
                && !source.IsFlash
                && source.Next != null
                && source.Next.IsTransition
                && source.Next.SongBpmTime >= lowerBound;
        }

        // Rebuild every Basic Event effect from authoritative map data, matching the cache reset produced by save/reload.
        private static void RebuildBasicMovementPreview(BeatmapRuntimeContext context)
        {
            context.Descriptor.BasicEventEffectManager.Reinitialize();
            context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0f);
        }

        // Direct movement shift/paste regressions must begin after the previously-correct future chain has been rendered.
        private static void PrimeMovementPreviewCache() =>
            PrimeMovementPreviewCache(MovementPreviewSampleJsonTimes);

        // Long movement jumps use their wider checkpoint sequence to precompute every state they later cross.
        private static void PrimeMovementPreviewCache(IReadOnlyList<float> jsonTimes)
        {
            // Priming needs only to drive the production simulator; transform ownership is handled by later captures.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            for (var timeIndex = 0; timeIndex < jsonTimes.Count; timeIndex++)
            {
                atsc.MoveToJsonTime(jsonTimes[timeIndex]);
            }
        }

        // Sample the actual scene transforms while scrubbing forward and backward through the authored movement sequence.
        private static MovementPreviewFrame[,] CaptureMovementPreview(
            IReadOnlyList<MovementPreviewTarget> targets,
            IReadOnlyList<float> jsonTimes)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var frames = new MovementPreviewFrame[jsonTimes.Count, targets.Count];
            for (var timeIndex = 0; timeIndex < jsonTimes.Count; timeIndex++)
            {
                atsc.MoveToJsonTime(jsonTimes[timeIndex]);
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    var transform = targets[targetIndex].Transform;
                    frames[timeIndex, targetIndex] = new MovementPreviewFrame(
                        transform.localPosition,
                        transform.localRotation);
                }
            }

            return frames;
        }

        // Movement preview cache regressions must advance at the same 90 Hz callback times as live playback;
        // larger jumps can select BasicMovementEffect's snapshot path and conceal a stale incremental state.
        private static float[] CreateMovementPlaybackJsonTimes(IReadOnlyList<float> requiredJsonTimes)
        {
            Assert.That(requiredJsonTimes, Is.Not.Empty);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var map = BeatSaberSongContainer.Instance.Map;
            var maximumJsonTime = requiredJsonTimes.Max();
            var maximumSongBpmTime = (float)map.JsonTimeToSongBpmTime(maximumJsonTime);
            var maximumSeconds = atsc.GetSecondsFromBeat(maximumSongBpmTime);
            var renderCount = TimeHelper.GetPreviewRenderIndex(maximumSeconds);
            var callbackStepSeconds = TimeHelper.GetPreviewCallbackSeconds(0.001f);
            var jsonTimes = new float[renderCount + 1];

            for (var renderIndex = 0; renderIndex <= renderCount; renderIndex++)
            {
                var songBpmTime = atsc.GetBeatFromSeconds(renderIndex * callbackStepSeconds);
                jsonTimes[renderIndex] = (float)map.SongBpmTimeToJsonTime(songBpmTime);
            }

            return jsonTimes;
        }

        // Sample the same BasicEventManager isPlaying path used by continuous editor playback instead of approximating it with seeks.
        private static MovementPreviewFrame[,] CaptureMovementPlayback(
            IReadOnlyList<MovementPreviewTarget> targets,
            IReadOnlyList<float> jsonTimes)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var eventManager = Object.FindAnyObjectByType<BasicEventManager>();
            atsc.MoveToJsonTime(0f);
            var frames = new MovementPreviewFrame[jsonTimes.Count, targets.Count];
            for (var timeIndex = 0; timeIndex < jsonTimes.Count; timeIndex++)
            {
                var songBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                    jsonTimes[timeIndex]);
                eventManager.UpdateTime(true, songBpmTime);
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    var transform = targets[targetIndex].Transform;
                    frames[timeIndex, targetIndex] = new MovementPreviewFrame(
                        transform.localPosition,
                        transform.localRotation);
                }
            }

            // Return every effect to the paused path so later actions do not inherit a synthetic playback session.
            atsc.MoveToJsonTime(0f);
            return frames;
        }

        // Compare incremental rendering with a complete state-chain rebuild without consulting private cache fields.
        private static void AssertMovementPreviewMatchesFullRebuild(
            BeatmapRuntimeContext context,
            IReadOnlyList<MovementPreviewTarget> targets,
            IReadOnlyList<float> jsonTimes)
        {
            // Movement preview comparisons include every live callback through the furthest requested scrub beat.
            var playbackJsonTimes = CreateMovementPlaybackJsonTimes(jsonTimes);
            var incremental = CaptureMovementPreview(targets, jsonTimes);
            var incrementalPlayback = CaptureMovementPlayback(targets, playbackJsonTimes);
            RebuildBasicMovementPreview(context);
            var rebuilt = CaptureMovementPreview(targets, jsonTimes);
            var rebuiltPlayback = CaptureMovementPlayback(targets, playbackJsonTimes);
            AssertMovementPreviewMatchesBaseline(
                rebuilt,
                incremental,
                targets,
                jsonTimes,
                "incremental movement preview");
            AssertMovementPreviewMatchesBaseline(
                rebuiltPlayback,
                incrementalPlayback,
                targets,
                playbackJsonTimes,
                "incremental movement playback");
        }

        // Report the exact beat and scene ring whose transform diverges while tolerating only sub-millimeter/degree float noise.
        private static void AssertMovementPreviewMatchesBaseline(
            MovementPreviewFrame[,] expected,
            MovementPreviewFrame[,] actual,
            IReadOnlyList<MovementPreviewTarget> targets,
            IReadOnlyList<float> jsonTimes,
            string operation)
        {
            for (var timeIndex = 0; timeIndex < jsonTimes.Count; timeIndex++)
            {
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    var expectedFrame = expected[timeIndex, targetIndex];
                    var actualFrame = actual[timeIndex, targetIndex];
                    // Dense 90 Hz comparisons avoid allocating NUnit constraints unless a rounded scene position differs.
                    var expectedX = System.Math.Round(expectedFrame.Position.x, 3);
                    var expectedY = System.Math.Round(expectedFrame.Position.y, 3);
                    var expectedZ = System.Math.Round(expectedFrame.Position.z, 3);
                    var actualX = System.Math.Round(actualFrame.Position.x, 3);
                    var actualY = System.Math.Round(actualFrame.Position.y, 3);
                    var actualZ = System.Math.Round(actualFrame.Position.z, 3);
                    if (actualX != expectedX || actualY != expectedY || actualZ != expectedZ)
                    {
                        Assert.Fail(
                            $"{operation} diverged at JSON beat {jsonTimes[timeIndex]} for "
                            + $"{targets[targetIndex].Description}: localPosition expected {expectedFrame.Position} "
                            + $"({expectedX}, {expectedY}, {expectedZ}), actual {actualFrame.Position} "
                            + $"({actualX}, {actualY}, {actualZ}).");
                    }

                    // Quaternion.Angle covers arbitrary laser axes while remaining insensitive to equivalent quaternion signs.
                    var rotationDifference = Quaternion.Angle(expectedFrame.Rotation, actualFrame.Rotation);
                    if (System.Math.Round(rotationDifference, 3) != 0d)
                    {
                        Assert.Fail(
                            $"{operation} diverged at JSON beat {jsonTimes[timeIndex]} for "
                            + $"{targets[targetIndex].Description}: local rotation differed by "
                            + $"{rotationDifference:R} degrees.");
                    }
                }
            }
        }

        private static void AssertPreviewMatchesBaseline(
            Color[,] expected,
            Color[,] actual,
            IReadOnlyList<LivePreviewLightController> previewLights,
            IReadOnlyList<float> jsonTimes,
            string operation)
        {
            // A round trip must return the live cache to its pre-action output without an intervening full rebuild.
            for (var timeIndex = 0; timeIndex < jsonTimes.Count; timeIndex++)
            {
                for (var lightIndex = 0; lightIndex < previewLights.Count; lightIndex++)
                {
                    AssertColorsEqualRoundedToThreeDecimalPlaces(
                        expected[timeIndex, lightIndex],
                        actual[timeIndex, lightIndex],
                        $"{operation} did not restore the preview at JSON beat {jsonTimes[timeIndex]} "
                        + $"for Basic Event type {previewLights[lightIndex].Type}");
                }
            }
        }

        private static Color[,] AssertLivePreviewMatchesFullRebuild(
            BeatmapRuntimeContext context,
            IReadOnlyList<LivePreviewLightController> previewLights,
            params float[] jsonTimes)
        {
            // Compare incremental action handling with the authoritative rebuild that save/reload effectively performs.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var incremental = CaptureLivePreview(atsc, previewLights, jsonTimes);

            context.Descriptor.BasicEventEffectManager.Reinitialize();
            context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);
            var rebuilt = CaptureLivePreview(atsc, previewLights, jsonTimes);

            for (var timeIndex = 0; timeIndex < jsonTimes.Length; timeIndex++)
            {
                for (var lightIndex = 0; lightIndex < previewLights.Count; lightIndex++)
                {
                    AssertColorsEqualRoundedToThreeDecimalPlaces(
                        rebuilt[timeIndex, lightIndex],
                        incremental[timeIndex, lightIndex],
                        $"Incremental preview differed from full rebuild at JSON beat {jsonTimes[timeIndex]} "
                        + $"for Basic Event type {previewLights[lightIndex].Type}");
                }
            }

            return incremental;
        }

        private static Color[] AssertCurrentLivePreviewMatchesFullRebuild(
            BeatmapRuntimeContext context,
            IReadOnlyList<LivePreviewLightController> previewLights)
        {
            // Capture the live frame before any seek can repair stale active tweens, then compare it with save/reload-equivalent state.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var currentJsonTime = atsc.CurrentJsonTime;
            var incremental = previewLights.Select(light => light.Color).ToArray();

            context.Descriptor.BasicEventEffectManager.Reinitialize();
            context.Descriptor.BasicEventEffectManager.InsertData(BeatSaberSongContainer.Instance.Map.Events);
            atsc.MoveToJsonTime(currentJsonTime);
            var rebuilt = previewLights.Select(light => light.Color).ToArray();

            for (var lightIndex = 0; lightIndex < previewLights.Count; lightIndex++)
            {
                AssertColorsEqualRoundedToThreeDecimalPlaces(
                    rebuilt[lightIndex],
                    incremental[lightIndex],
                    $"Immediate incremental preview differed from full rebuild at JSON beat {currentJsonTime} "
                    + $"for Basic Event type {previewLights[lightIndex].Type}");
            }

            return incremental;
        }

        private static Color[,] CaptureLivePreview(
            AudioTimeSyncController atsc,
            IReadOnlyList<LivePreviewLightController> previewLights,
            IReadOnlyList<float> jsonTimes)
        {
            // Sample the same scene-light outputs a mapper sees while scrubbing the editor timeline.
            var colors = new Color[jsonTimes.Count, previewLights.Count];
            for (var timeIndex = 0; timeIndex < jsonTimes.Count; timeIndex++)
            {
                atsc.MoveToJsonTime(jsonTimes[timeIndex]);
                for (var lightIndex = 0; lightIndex < previewLights.Count; lightIndex++)
                {
                    colors[timeIndex, lightIndex] = previewLights[lightIndex].Color;
                }
            }

            return colors;
        }

        private static void AssertColorsEqualRoundedToThreeDecimalPlaces(
            Color expected,
            Color actual,
            string message)
        {
            // Match the three-decimal map-color serialization contract while retaining channel-specific failure evidence.
            Assert.That(
                System.Math.Round(actual.r, 3),
                Is.EqualTo(System.Math.Round(expected.r, 3)),
                $"{message}: red channel differed; expected {expected}, but preview rendered {actual}.");
            Assert.That(
                System.Math.Round(actual.g, 3),
                Is.EqualTo(System.Math.Round(expected.g, 3)),
                $"{message}: green channel differed; expected {expected}, but preview rendered {actual}.");
            Assert.That(
                System.Math.Round(actual.b, 3),
                Is.EqualTo(System.Math.Round(expected.b, 3)),
                $"{message}: blue channel differed; expected {expected}, but preview rendered {actual}.");
            Assert.That(
                System.Math.Round(actual.a, 3),
                Is.EqualTo(System.Math.Round(expected.a, 3)),
                $"{message}: alpha channel differed; expected {expected}, but preview rendered {actual}.");
        }

        private void EnableChromaLitePreview()
        {
            // Custom colors expose stale cached transition endpoints that ordinary Red/Blue values cannot reveal.
            emulateChromaLiteBeforePreviewTest ??= Settings.Instance.EmulateChromaLite;
            Settings.Instance.EmulateChromaLite = true;
        }

        // Read the active definition so the name-filter tests remain valid for every test environment.
        private static int GetRingRotationType()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            return context.TrackDefinitions.Basic.Values
                .First(definition => definition.Components.HasFlag(BasicEventComponent.RingRotation)).Type;
        }

        // Exercise the same final-object replacement action used by selection moves and mirrors.
        private static BaseEvent ReplaceEvent(BaseEvent original, System.Action<BaseEvent> edit)
        {
            var edited = BeatmapFactory.Clone(original);
            edit(edited);
            edited.WriteCustom();
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedCollectionAction(
                    new List<BaseObject> { edited },
                    new List<BaseObject> { original },
                    "Replace basic event filter."),
                true);
            return edited;
        }

        private void AssertLinksAndSorted(EventGridContainer eventsContainer, int eventType)
        {
            var laneEvents = eventsContainer.MapObjects.Where(x => x.Type == eventType).ToList();
            BeatmapAssertion.IsEqual(
                BeatmapAssertion.EventsAreLinkedAndSorted,
                laneEvents,
                "Events are linked and sorted");
        }

        private void PlaceEvent(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.Event4,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }

        private BaseEvent PlaceLeftLasers(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.BlueOn
            };
            return PlaceUtils.Place(evt);
        }

        // Create a ring-rotation event because only ring-rotation tracks expose name-filter lanes.
        private static BaseEvent PlaceRingRotation(float time, int eventType, string nameFilter)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = eventType,
                Value = 0,
                CustomNameFilter = nameFilter
            };
            return PlaceUtils.Place(evt);
        }

        private void PlaceRightLasers(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.Event3,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }

        // A test-only light receives the same tween output as an environment light without depending on a particular scene asset.
        public sealed class LivePreviewLightController : LightController
        {
            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }

        // Keep ring zoom, ring rotation, and both laser-speed sides separately named in shared cache regressions.
        public enum BasicMovementPreviewKind
        {
            RingZoom,
            RingRotation,
            LaserSpeedLeft,
            LaserSpeedRight
        }

        // Retain the rendered transform and a stable diagnostic identity for each production ring or laser visual.
        private sealed class MovementPreviewTarget
        {
            public readonly Transform Transform;
            public readonly string Description;

            public MovementPreviewTarget(Transform transform, string description)
            {
                Transform = transform;
                Description = description;
            }
        }

        // Store both movement channels so cross-effect contamination is visible in the same frame comparison.
        private readonly struct MovementPreviewFrame
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public MovementPreviewFrame(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }

        // Public nested case values keep NUnit's public parameterized methods accessibility-consistent.
        public enum PreviewTransitionPattern
        {
            OnIntoOnTransition,
            OnIntoTransitionTransition,
            TransitionIntoOnTransition,
            TransitionIntoTransitionTransition,
            TransitionIntoOnOn,
            TransitionIntoTransitionOn
        }

        // Public nested case values let Unity report each source-lane neighbor combination by name.
        public enum SourceNeighborPattern
        {
            OnOn,
            OnTransition,
            TransitionOn,
            TransitionTransition
        }
    }
}
