using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Ctrl+Shift+click range selection regressed on dev: the second clicked endpoint was left out of
    // the selection. These tests drive the authored chord through the shared input asset so the real
    // scene-installed callbacks, modifier actions, and the InputSystemPatch overlap rules all apply.
    public class SelectionRangeClickInputTest : TestBase
    {
        private InputTestFixture inputFixture;
        private CMInput isolatedInput;
        private Mouse virtualMouse;
        private Keyboard virtualKeyboard;
        private readonly List<InputActionMap> sharedEnabledActionMaps = new();
        private bool selectionChanged;

        // CtrlShiftClickSelectsInclusiveNoteRange reproduces the reported regression exactly: one
        // selected note, then a Ctrl+Shift+click on a later note must select every note in between,
        // including the clicked endpoint.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveNoteRange()
        {
            var noteA = PlaceUtils.Place(new BaseNote { JsonTime = 1 });
            var noteMiddle = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
            var noteB = PlaceUtils.Place(new BaseNote { JsonTime = 3 });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(noteA);

            var noteInput = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            selectionChanged = false;
            SelectionController.OnSelectionChanged += FlagSelectionChanged;

            PressCtrlShift();
            var massSelectAction = isolatedInput.BeatmapObjects.Get().FindAction("Mass Select Modifier", false);
            Assert.That(
                massSelectAction,
                Is.Not.Null,
                "The authored Mass Select Modifier action was not found.");
            Assert.That(
                massSelectAction.phase,
                Is.EqualTo(InputActionPhase.Performed),
                $"Ctrl press left the action in {massSelectAction.phase}; "
                + $"bound controls={string.Join("|", massSelectAction.controls.Select(c => c.path))}");
            Assert.That(
                ReadField<bool>(noteInput, "MassSelect"),
                Is.True,
                "The Mass Select Modifier latch did not follow the virtual Ctrl press.");
            SeedFirstHit<NoteContainer>(noteB, ObjectType.Note);

            inputFixture.Press(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            Assert.That(
                noteInput.IsSelecting,
                Is.True,
                "The Select Objects action never reached BeatmapInputController.OnSelectObjects.");
            inputFixture.Release(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            SelectionController.OnSelectionChanged -= FlagSelectionChanged;

            Assert.That(
                selectionChanged,
                Is.True,
                "The click produced no selection callback at all.");
            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(noteA),
                Is.True,
                "The first selected note was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(noteMiddle),
                Is.True,
                "A note inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(noteB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint note was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveNoteRangeViaRaycast exercises the same gesture but resolves
        // the endpoint through the real per-frame camera raycast instead of a seeded cache hit, so a
        // mispicked collider or stale pointer position would surface here.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveNoteRangeViaRaycast()
        {
            var noteA = PlaceUtils.Place(new BaseNote { JsonTime = 1 });
            var noteMiddle = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
            var noteB = PlaceUtils.Place(new BaseNote { JsonTime = 3 });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(noteA);

            var noteInput = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            var camera = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.Camera;
            var noteGrid = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            Assert.That(
                noteGrid.LoadedContainers.TryGetValue(noteB, out var endpointContainer),
                Is.True,
                "The clicked fixture object was not rendered.");

            // Aim the real raycast at the endpoint: the pointer-position event must land in the same
            // controller field that RaycastFirstObject reads during the click.
            var screenPoint = camera.WorldToScreenPoint(endpointContainer.transform.position);
            Assert.That(
                screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= Screen.width
                    && screenPoint.y >= 0 && screenPoint.y <= Screen.height,
                Is.True,
                $"Endpoint off-screen at {screenPoint} ({Screen.width}x{Screen.height}); the real raycast cannot reach it.");
            inputFixture.Set(virtualMouse.position, new Vector2(screenPoint.x, screenPoint.y), queueEventOnly: true);
            InputSystem.Update();

            PressCtrlShift();
            inputFixture.Press(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            Assert.That(
                noteInput.IsSelecting,
                Is.True,
                "The Select Objects action never reached BeatmapInputController.OnSelectObjects.");
            inputFixture.Release(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();

            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(noteA),
                Is.True,
                "The first selected note was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(noteMiddle),
                Is.True,
                "A note inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(noteB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint note was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveObstacleRange covers walls, which share the note selection
        // group so the chord must include every wall between the endpoints.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveObstacleRange()
        {
            var wallA = PlaceUtils.Place(new BaseObstacle { JsonTime = 1 });
            var wallMiddle = PlaceUtils.Place(new BaseObstacle { JsonTime = 2 });
            var wallB = PlaceUtils.Place(new BaseObstacle { JsonTime = 3 });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(wallA);

            PressCtrlShift();
            SeedFirstHit<ObstacleContainer>(wallB, ObjectType.Obstacle);
            ClickLeftButton();

            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(wallA),
                Is.True,
                "The first selected obstacle was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(wallMiddle),
                Is.True,
                "An obstacle inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(wallB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint obstacle was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveArcRange covers sliders: the endpoint is resolved through
        // the arc container (child indicator hits reach the owning arc) and must stay selected.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveArcRange()
        {
            var arcA = PlaceUtils.Place(new BaseArc { JsonTime = 1, TailJsonTime = 1.5f });
            var arcMiddle = PlaceUtils.Place(new BaseArc { JsonTime = 2, TailJsonTime = 2.5f });
            var arcB = PlaceUtils.Place(new BaseArc { JsonTime = 3, TailJsonTime = 3.5f });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(arcA);

            PressCtrlShift();
            SeedFirstHit<ArcContainer>(arcB, ObjectType.Arc);
            ClickLeftButton();

            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(arcA),
                Is.True,
                "The first selected arc was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(arcMiddle),
                Is.True,
                "An arc inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(arcB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint arc was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveChainRange covers burst sliders in the same selection group.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveChainRange()
        {
            var chainA = PlaceUtils.Place(new BaseChain { JsonTime = 1, TailJsonTime = 1.5f });
            var chainMiddle = PlaceUtils.Place(new BaseChain { JsonTime = 2, TailJsonTime = 2.5f });
            var chainB = PlaceUtils.Place(new BaseChain { JsonTime = 3, TailJsonTime = 3.5f });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(chainA);

            PressCtrlShift();
            SeedFirstHit<ChainContainer>(chainB, ObjectType.Chain);
            ClickLeftButton();

            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(chainA),
                Is.True,
                "The first selected chain was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(chainMiddle),
                Is.True,
                "A chain inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(chainB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint chain was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveEventRange covers the Basic Event group that shares the
        // same SelectBetween grouping path as notes.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveEventRange()
        {
            // Event containers only activate in Basic Event mode, and the mode change clears any
            // prior selection, so switch before placing and selecting the fixture events.
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.BasicEvent;
            yield return null;

            var eventA = PlaceUtils.Place(new BaseEvent { JsonTime = 1 });
            var eventMiddle = PlaceUtils.Place(new BaseEvent { JsonTime = 2 });
            var eventB = PlaceUtils.Place(new BaseEvent { JsonTime = 3 });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(eventA);

            var eventInput = Object.FindAnyObjectByType<BeatmapEventInputController>();
            Assert.That(eventInput, Is.Not.Null, "The mapper scene has no BeatmapEventInputController.");

            PressCtrlShift();
            SeedFirstHit<EventContainer>(eventB, ObjectType.Event);
            inputFixture.Press(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            Assert.That(
                eventInput.IsSelecting,
                Is.True,
                "The Select Objects action never reached the event controller.");
            inputFixture.Release(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();

            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(eventA),
                Is.True,
                "The first selected event was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(eventMiddle),
                Is.True,
                "An event inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(eventB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint event was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveRotationEventRange covers ring-rotation events, whose
        // controller is ObjectContainer-typed and relies on SpecialCaseContainer to accept only
        // RotationEventContainer hits.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveRotationEventRange()
        {
            var rotationA = PlaceUtils.Place(new BaseRotationEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 15
            });
            var rotationMiddle = PlaceUtils.Place(new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 30
            });
            var rotationB = PlaceUtils.Place(new BaseRotationEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 45
            });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(rotationA);

            PressCtrlShift();
            SeedFirstHit<RotationEventContainer>(rotationB, ObjectType.RotationEvent);
            ClickLeftButton();

            Assert.That(
                SelectionController.SelectedObjects.Count,
                Is.EqualTo(3),
                $"Expected the full inclusive range; actual: {DescribeSelection()}.");
            Assert.That(
                SelectionController.IsObjectSelected(rotationA),
                Is.True,
                "The first selected rotation event was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(rotationMiddle),
                Is.True,
                "A rotation event inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(rotationB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint rotation event was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveBpmEventRange covers the standalone BPM change group.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveBpmEventRange()
        {
            var bpmA = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 100 });
            var bpmMiddle = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 2, Bpm = 100 });
            var bpmB = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 3, Bpm = 100 });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(bpmA);

            PressCtrlShift();
            SeedFirstHit<BpmEventContainer>(bpmB, ObjectType.BpmChange);
            ClickLeftButton();

            Assert.That(
                SelectionController.IsObjectSelected(bpmA),
                Is.True,
                "The first selected BPM change was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(bpmMiddle),
                Is.True,
                "A BPM change inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(bpmB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint BPM change was not selected.");
        }

        // CtrlShiftClickSelectsInclusiveNjsEventRange covers the standalone NJS event group.
        [UnityTest]
        public IEnumerator CtrlShiftClickSelectsInclusiveNjsEventRange()
        {
            var njsA = PlaceUtils.Place(new BaseNJSEvent { JsonTime = 1 });
            var njsMiddle = PlaceUtils.Place(new BaseNJSEvent { JsonTime = 2 });
            var njsB = PlaceUtils.Place(new BaseNJSEvent { JsonTime = 3 });
            yield return null;

            InitializeIsolatedInput();
            SelectionController.Select(njsA);

            PressCtrlShift();
            SeedFirstHit<NJSEventContainer>(njsB, ObjectType.NJSEvent);
            ClickLeftButton();

            Assert.That(
                SelectionController.IsObjectSelected(njsA),
                Is.True,
                "The first selected NJS event was lost after Ctrl+Shift+click.");
            Assert.That(
                SelectionController.IsObjectSelected(njsMiddle),
                Is.True,
                "An NJS event inside the Ctrl+Shift+click range was not selected.");
            Assert.That(
                SelectionController.IsObjectSelected(njsB),
                Is.True,
                "The Ctrl+Shift+clicked endpoint NJS event was not selected.");
        }

        // Restore the shared input maps and frame cache the chord simulation overrode.
        protected override void AfterCleanup()
        {
            BeatmapRaycastCache.Invalidate();
            if (isolatedInput != null)
            {
                // Dispose every isolated callback and action state before restoring the application runtime.
                isolatedInput.Dispose();
                isolatedInput = null;
            }
            if (inputFixture != null)
            {
                inputFixture.TearDown();
                inputFixture = null;
            }

            virtualMouse = null;
            virtualKeyboard = null;

            // Restore only maps that were enabled before this fixture, now against the original runtime.
            foreach (var actionMap in sharedEnabledActionMaps) actionMap.Enable();
            sharedEnabledActionMaps.Clear();
        }

        // Re-enable the shared action maps on the isolated device runtime so every production
        // callback installed by the scene still resolves the authored chord.
        private void InitializeIsolatedInput()
        {
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null, "The application's shared input asset was not initialized.");
            // InputTestFixture replaces the global runtime; keep shared maps disabled until they can
            // re-resolve against either this fixture's devices or the restored application runtime.
            sharedEnabledActionMaps.Clear();
            foreach (var actionMap in sharedInput.asset.actionMaps)
            {
                if (!actionMap.enabled) continue;
                sharedEnabledActionMaps.Add(actionMap);
                actionMap.Disable();
            }

            inputFixture = new InputTestFixture();
            inputFixture.Setup();
            virtualMouse = InputSystem.AddDevice<Mouse>();
            virtualKeyboard = InputSystem.AddDevice<Keyboard>();

            // The editor's paranoid read-value check logs a false-positive error when a queued event
            // mutates a control whose cached value was already evaluated in the same manual update;
            // the check self-corrects the cache, so silence only that diagnostic for this fixture.
            InputSystem.settings.SetInternalFeatureFlag("PARANOID_READ_VALUE_CACHING_CHECKS", false);

            // Bind production handlers to an asset owned entirely by this virtual runtime.
            isolatedInput = new CMInput();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is CMInput.IBeatmapObjectsActions beatmapObjectsHandler)
                    isolatedInput.BeatmapObjects.AddCallbacks(beatmapObjectsHandler);
                if (behaviour is CMInput.IBoxSelectActions boxSelectHandler)
                    isolatedInput.BoxSelect.AddCallbacks(boxSelectHandler);
            }

            isolatedInput.BeatmapObjects.Enable();
            isolatedInput.BoxSelect.Enable();
            MovePointerInsideEditor();
        }

        // Keep the virtual pointer inside the editor so UI-over-pointer early returns match a real click.
        private void MovePointerInsideEditor()
        {
#if UNITY_EDITOR
            var gameViewSize = UnityEditor.Handles.GetMainGameViewSize();
#else
            var gameViewSize = new Vector2(Screen.width, Screen.height);
#endif
            inputFixture.Set(virtualMouse.position, gameViewSize * 0.5f, queueEventOnly: true);
            InputSystem.Update();
        }

        // Report the live selection so failures name the exact objects the chord produced.
        private static string DescribeSelection() =>
            string.Join(
                ",",
                SelectionController.SelectedObjects.Select(obj => $"{obj.GetType().Name}@{obj.JsonTime}"));

        private void FlagSelectionChanged() => selectionChanged = true;

        // Protected input latches describe which production branch the chord reached.
        private static T ReadField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            return (T)field.GetValue(instance);
        }

        // Queue Ctrl then Shift so the Mass Select Modifier latch is owned by the production callback.
        private void PressCtrlShift()
        {
            inputFixture.Press(virtualKeyboard.leftCtrlKey, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Press(virtualKeyboard.leftShiftKey, queueEventOnly: true);
            InputSystem.Update();
        }

        // Drive the authored Select Objects chord (Shift+leftButton) with release events included.
        private void ClickLeftButton()
        {
            inputFixture.Press(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Release(virtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
        }

        // Seed the same-frame raycast cache so the click resolves the authoritative loaded container.
        private static void SeedFirstHit<TContainer>(BaseObject data, ObjectType type)
            where TContainer : ObjectContainer
        {
            var grid = BeatmapObjectContainerCollection.GetCollectionForType(type);
            Assert.That(
                grid.LoadedContainers.TryGetValue(data, out var container),
                Is.True,
                "The clicked fixture object was not rendered.");
            BeatmapRaycastCache.FirstHit = container.gameObject;
            BeatmapRaycastCache.HasHit = true;
            BeatmapRaycastCache.HasRaycastThisFrame = true;
        }
    }
}
