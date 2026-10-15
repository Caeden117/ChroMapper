using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSPreviewWorkspaceStateTest : PreviewWorkflowTestBase
    {
        public enum PreviewRoute { Preview, Playing, PreviewThenPlaying, PlayingThenPreview }

        private static readonly ObjectType[] GlsKinds =
        {
            ObjectType.GLSColor, ObjectType.GLSRotation, ObjectType.GLSTranslation, ObjectType.GLSFloatFx
        };

        private InputTestFixture input;
        private CMInput shortcuts;
        private Keyboard keyboard;
        private InputActionMap[] enabledSharedMaps;
        private AudioTimeSyncController atsc;
        private EditModeContext editMode;
        private GLSEventGridProvider provider;
        private EventBoxViewController boxView;
        private int previousSnapping;
        private Dictionary<IEditorStateProvider, JSONObject> previousPlacementStates;

        [SetUp]
        public void ConfigureWorkspaceInput()
        {
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            editMode = Object.FindAnyObjectByType<EditModeContext>();
            provider = Object.FindAnyObjectByType<GLSEventGridProvider>(FindObjectsInactive.Include);
            boxView = Object.FindAnyObjectByType<EventBoxViewController>(FindObjectsInactive.Include);
            previousSnapping = atsc.GridMeasureSnapping;
            previousPlacementStates = new Dictionary<IEditorStateProvider, JSONObject>();
            SeedPlacement(Object.FindAnyObjectByType<GLSEventColorPlacement>(FindObjectsInactive.Include), "brightness", 2.5f);
            SeedPlacement(Object.FindAnyObjectByType<GLSEventRotationPlacement>(FindObjectsInactive.Include), "rotation", 37.5f);
            SeedPlacement(Object.FindAnyObjectByType<GLSEventTranslationPlacement>(FindObjectsInactive.Include), "translation", 1.75f);
            SeedPlacement(Object.FindAnyObjectByType<GLSEventFloatFXPlacement>(FindObjectsInactive.Include), "value", 2.25f);
            InitializeIsolatedInput();
        }

        protected override void BeforeCleanup()
        {
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            provider.GroupContext = null;
            atsc.GridMeasureSnapping = previousSnapping;
            foreach (var pair in previousPlacementStates)
                pair.Key.LoadEditorState(pair.Value);
        }

        [UnityTearDown]
        public IEnumerator RestoreWorkspaceInput()
        {
            shortcuts.Dispose();
            input.TearDown();
            foreach (var map in enabledSharedMaps)
                map.Enable();
            TestUtils.ResetSharedInputState();
            yield break;
        }

        [Test]
        public void InnerWorkspaceSurvivesPreviewRoundTrip(
            [Values(ObjectType.GLSColor, ObjectType.GLSRotation, ObjectType.GLSTranslation, ObjectType.GLSFloatFx)] ObjectType kind,
            [Values] PreviewRoute route,
            [Values] bool escape)
        {
            var group = OpenGroup(kind);
            var origin = atsc.VisualBeatOrigin;
            var cursor = atsc.CurrentJsonTime;
            var scale = EditorScaleController.EditorScale;
            var lane = ReadField<GridLane>(provider, "gridLane");
            var lanePosition = lane.transform.localPosition;
            var boxes = Enumerable.Range(0, provider.DisplayedLaneCount).Select(index =>
            {
                Assert.That(provider.TryGetDisplayedBox(index, out var box), Is.True);
                return box;
            }).ToArray();
            var measureLines = Object.FindAnyObjectByType<MeasureLinesController>(FindObjectsInactive.Include);
            var measures = VisibleMeasures(measureLines);
            Assert.That(measures.Any(line => line.Text == "0"), Is.True, "The fixture must display relative beat zero.");
            var controls = CaptureControls();
            var selected = SelectionController.SelectedObjects.ToArray();
            var innerNodes = ((GLSEventGridContainer)BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSEvent))
                .MapObjects.ToArray();
            var selectedBox = ReadField<BaseEventBox>(boxView, "boxContext");
            var groupData = group.ToJson().ToString();

            RoundTrip(route, escape);

            Assert.That(editMode.EditingMode, Is.EqualTo(EditingMode.EventBox));
            Assert.That(provider.GroupContext, Is.SameAs(group));
            Assert.That(group.ToJson().ToString(), Is.EqualTo(groupData));
            Assert.That(atsc.VisualBeatOrigin, Is.EqualTo(origin), "Preview discarded the inner group's relative beat origin.");
            Assert.That(Shader.GetGlobalFloat("_SongTimeOrigin"), Is.EqualTo(group.JsonTime).Within(0.0001f));
            Assert.That(atsc.CurrentJsonTime, Is.EqualTo(cursor).Within(0.0001f));
            Assert.That(atsc.GridMeasureSnapping, Is.EqualTo(4));
            Assert.That(atsc.IsSnapped, Is.True, "The relative grid cursor lost its snapped state.");
            Assert.That(EditorScaleController.EditorScale, Is.EqualTo(scale));
            Assert.That(VisibleMeasures(measureLines), Is.EqualTo(measures), "Rendered measure labels or their beat positions changed.");
            Assert.That(provider.DisplayedLaneCount, Is.EqualTo(boxes.Length));
            for (var index = 0; index < boxes.Length; index++)
            {
                Assert.That(provider.TryGetDisplayedBox(index, out var box), Is.True);
                Assert.That(box, Is.SameAs(boxes[index]));
            }

            Assert.That(lane.transform.localPosition, Is.EqualTo(lanePosition));
            Assert.That(lane.XZ.Grid.enabled, Is.True);
            Assert.That(lane.XY.Grid.enabled, Is.True);
            Assert.That(ReadField<int>(boxView, "boxIndex"), Is.EqualTo(1));
            Assert.That(ReadField<BaseEventBox>(boxView, "boxContext"), Is.SameAs(selectedBox));
            Assert.That(SelectionController.SelectedObjects, Is.EquivalentTo(selected));
            Assert.That(((GLSEventGridContainer)BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSEvent))
                .MapObjects, Is.EqualTo(innerNodes));
            AssertControlsUnchanged(controls);

            atsc.MoveToJsonTime(cursor + 0.1f);
            atsc.SnapToGrid();
            Assert.That(atsc.CurrentJsonTime, Is.EqualTo(cursor).Within(0.0001f), "Snapping now uses absolute beats.");
        }

        [Test]
        public void PlayingPreservesInnerSelection(
            [Values(ObjectType.GLSColor, ObjectType.GLSRotation, ObjectType.GLSTranslation, ObjectType.GLSFloatFx)] ObjectType kind)
        {
            OpenGroup(kind);
            var selected = SelectionController.SelectedObjects.ToArray();
            Assert.That(selected.Length, Is.EqualTo(2));

            RoundTrip(PreviewRoute.Playing, false);

            Assert.That(SelectionController.SelectedObjects, Is.EquivalentTo(selected), "Playing cleared the inner node selection.");
        }

        [Test]
        public void PlayingPreservesActiveEventBox(
            [Values(ObjectType.GLSColor, ObjectType.GLSRotation, ObjectType.GLSTranslation, ObjectType.GLSFloatFx)] ObjectType kind)
        {
            OpenGroup(kind);

            RoundTrip(PreviewRoute.Playing, false);

            Assert.That(ReadField<int>(boxView, "boxIndex"), Is.EqualTo(1), "Playing reset the active event-box tab.");
        }

        [Test]
        public void LeavingInnerWorkspaceStillResetsItsTransientState()
        {
            OpenGroup(ObjectType.GLSTranslation);

            editMode.EditingMode = EditingMode.GLS;

            Assert.That(atsc.VisualBeatOrigin, Is.Zero);
            Assert.That(SelectionController.SelectedObjects, Is.Empty);
            Assert.That(ReadField<int>(boxView, "boxIndex"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator InnerWorkspaceSurvivesFramesInBothPreviewModes([ValueSource(nameof(GlsKinds))] ObjectType kind)
        {
            var group = OpenGroup(kind);
            var selected = SelectionController.SelectedObjects.ToArray();
            var origin = atsc.VisualBeatOrigin;
            var controls = CaptureControls();
            var measureLines = Object.FindAnyObjectByType<MeasureLinesController>(FindObjectsInactive.Include);
            var measures = VisibleMeasures(measureLines);

            ToggleMode(UIModeType.Playing);
            yield return null;
            ToggleMode(UIModeType.Preview);
            yield return null;
            ToggleMode(UIModeType.Normal);
            yield return null;

            Assert.That(editMode.EditingMode, Is.EqualTo(EditingMode.EventBox));
            Assert.That(provider.GroupContext, Is.SameAs(group));
            Assert.That(atsc.VisualBeatOrigin, Is.EqualTo(origin));
            Assert.That(atsc.IsSnapped, Is.True);
            Assert.That(ReadField<int>(boxView, "boxIndex"), Is.EqualTo(1));
            Assert.That(SelectionController.SelectedObjects, Is.EquivalentTo(selected));
            Assert.That(VisibleMeasures(measureLines), Is.EqualTo(measures));
            AssertControlsUnchanged(controls);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSEvent);
            foreach (var node in selected)
            {
                Assert.That(collection.LoadedContainers.TryGetValue(node, out var container), Is.True);
                Assert.That(container.Selected, Is.True, "The restored selection lost its rendered outline.");
            }
        }

        private void InitializeIsolatedInput()
        {
            enabledSharedMaps = CMInputCallbackInstaller.InputInstance.asset.actionMaps.Where(map => map.enabled).ToArray();
            foreach (var map in enabledSharedMaps)
                map.Disable();

            input = new InputTestFixture();
            input.Setup();
            InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            shortcuts = new CMInput();
            shortcuts.UIMode.AddCallbacks(Object.FindAnyObjectByType<UIMode>());
            shortcuts.UIMode.Enable();
        }

        private BaseEventBoxGroup OpenGroup(ObjectType kind)
        {
            editMode.EditingMode = EditingMode.GLS;
            BaseEventBoxGroup group;
            switch (kind)
            {
                case ObjectType.GLSColor:
                    var color = new BaseLightColorEventBoxGroup();
                    for (var index = 0; index < 2; index++)
                        color.Boxes.Add(new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase { RelativeJsonTime = 0.5f } } });

                    color.NormalizeLoadedEventConflicts();
                    group = color;
                    break;
                case ObjectType.GLSRotation:
                    var rotation = new BaseLightRotationEventBoxGroup();
                    for (var index = 0; index < 2; index++)
                        rotation.Boxes.Add(new BaseLightRotationEventBox { Axis = index, Events = new[] { new BaseLightRotationBase { RelativeJsonTime = 0.5f } } });

                    rotation.NormalizeLoadedEventConflicts();
                    group = rotation;
                    break;
                case ObjectType.GLSTranslation:
                    var translation = new BaseLightTranslationEventBoxGroup();
                    for (var index = 0; index < 2; index++)
                        translation.Boxes.Add(new BaseLightTranslationEventBox { Axis = index, Events = new[] { new BaseLightTranslationBase { RelativeJsonTime = 0.5f } } });

                    translation.NormalizeLoadedEventConflicts();
                    group = translation;
                    break;
                case ObjectType.GLSFloatFx:
                    var floatFx = new BaseVfxEventEventBoxGroup();
                    for (var index = 0; index < 2; index++)
                        floatFx.Boxes.Add(new BaseVfxEventEventBox { Events = new[] { new BaseFxEventFloat { RelativeJsonTime = 0.5f } } });

                    floatFx.NormalizeLoadedEventConflicts();
                    group = floatFx;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            // A fractional parent beat makes relative snapping observably different from absolute snapping.
            group.JsonTime = 8.125f;
            group.RecomputeSongBpmTime();

            atsc.MoveToJsonTime(group.JsonTime);
            editMode.EditingMode = EditingMode.EventBox;
            atsc.VisualBeatOrigin = group.SongBpmTime;
            provider.GroupContext = group;
            atsc.GridMeasureSnapping = 4;
            atsc.MoveToJsonTime(group.JsonTime + 0.5f);
            foreach (var box in group.ReadOnlyBoxes)
                SelectionController.Select(box.ReadOnlyEvents[0], true, addActionEvent: false);

            ReadField<List<ToggleComponent>>(boxView, "instantiatedIdTab")[1].Value = true;
            Assert.That(ReadField<int>(boxView, "boxIndex"), Is.EqualTo(1));
            Assert.That(atsc.IsSnapped, Is.True);
            return group;
        }

        private void RoundTrip(PreviewRoute route, bool escape)
        {
            var first = route is PreviewRoute.Playing or PreviewRoute.PlayingThenPreview
                ? UIModeType.Playing
                : UIModeType.Preview;
            ToggleMode(first);
            if (route == PreviewRoute.PlayingThenPreview)
                ToggleMode(UIModeType.Preview);
            else if (route == PreviewRoute.PreviewThenPlaying)
                ToggleMode(UIModeType.Playing);

            if (escape)
                Assert.That(Object.FindAnyObjectByType<UIMode>().TryExitPreviewMode(), Is.True);
            else
                ToggleMode(UIModeType.Normal);

            Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Normal));
            var cameras = Object.FindAnyObjectByType<CameraManager>();
            Assert.That(cameras.SelectedCameraController, Is.SameAs(cameras.CameraControllers[0]));
        }

        private void ToggleMode(UIModeType mode)
        {
            var key = mode == UIModeType.Normal
                ? keyboard.digit1Key
                : mode == UIModeType.Playing
                    ? keyboard.digit5Key
                    : keyboard.digit4Key;
            // UnityTest queues input without processing it, and packed keyboard deltas need each edge applied before the next.
            input.Press(keyboard.leftCtrlKey, queueEventOnly: true);
            InputSystem.Update();
            input.Press(key, queueEventOnly: true);
            InputSystem.Update();
            input.Release(key, queueEventOnly: true);
            InputSystem.Update();
            input.Release(keyboard.leftCtrlKey, queueEventOnly: true);
            InputSystem.Update();
            Assert.That(UIMode.SelectedMode, Is.EqualTo(mode), "The production Ctrl+number shortcut did not switch UI mode.");
        }

        private static Dictionary<IEditorStateProvider, string> CaptureControls() => Object
            .FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<IEditorStateProvider>()
            .Where(owner => !string.IsNullOrEmpty(owner.StateKey))
            .ToDictionary(owner => owner, CaptureState);

        private static void AssertControlsUnchanged(Dictionary<IEditorStateProvider, string> controls)
        {
            foreach (var pair in controls)
                Assert.That(CaptureState(pair.Key), Is.EqualTo(pair.Value), $"Preview changed {pair.Key.StateKey} controls.");
        }

        private void SeedPlacement(IEditorStateProvider owner, string property, float value)
        {
            var state = new JSONObject();
            owner.CaptureEditorState(state);
            previousPlacementStates.Add(owner, state);
            var seeded = new JSONObject();
            seeded[property] = value;
            owner.LoadEditorState(seeded);
        }

        private static string CaptureState(IEditorStateProvider owner)
        {
            var state = new JSONObject();
            owner.CaptureEditorState(state);
            return state.ToString();
        }

        private static (string Text, Vector3 Position)[] VisibleMeasures(MeasureLinesController controller) =>
            ReadField<List<TextMeshProUGUI>>(controller, "pool")
                .Where(line => line.gameObject.activeSelf)
                .Select(line => (line.text, line.transform.localPosition))
                .OrderBy(line => line.localPosition.y)
                .ToArray();

        private static T ReadField<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
