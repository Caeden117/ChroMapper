using System;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    public abstract class SongBoundaryTestBase : TestBase
    {
        protected SelectionController Selection;
        protected AudioTimeSyncController Atsc;
        private BasePlacement[] placements;
        private int originalSnapping;
        private readonly System.Collections.Generic.HashSet<ObjectType> touchedObjectTypes = new();

        // Song limits must use the map's BPM conversion rather than assuming the clip length is measured in JSON beats.
        protected float FinalBeat => (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(
            Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

        // Reuse one map across cases; each case records and removes every collection it modifies.
        [OneTimeSetUp]
        public void SetUpBoundaryFixtureMap() => TestUtils.ResetSharedMapState();

        protected override void ResetSharedMapState()
        {
        }

        // A retained hover or clipboard can silently choose a different paste branch, so each boundary case starts idle.
        [SetUp]
        public void SetUpSongBoundaries()
        {
            touchedObjectTypes.Clear();
            Selection = Object.FindAnyObjectByType<SelectionController>();
            Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            originalSnapping = Atsc.GridMeasureSnapping;
            Atsc.GridMeasureSnapping = 1;
            placements = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var placement in placements)
                placement.State = PlacementState.Idle;
            SelectionController.CopiedObjects.Clear();
            Assert.That(FinalBeat, Is.GreaterThan(10f), "The shared clip must have room for isolated source and boundary ranges.");
        }

        protected override void BeforeCleanup()
        {
            foreach (var placement in placements)
                placement.State = PlacementState.Idle;
            SelectionController.CopiedObjects.Clear();
            Atsc.GridMeasureSnapping = originalSnapping;
            base.BeforeCleanup();
        }

        // Reset the collection's cached scroll position as well as the audio cursor. Synchronous teardown
        // does not run LateUpdate, so later tests would otherwise inherit the old pool window.
        protected override void AfterCleanup()
        {
            Atsc.MoveToJsonTime(0f);
            foreach (var collection in Object.FindObjectsByType<BeatmapObjectContainerCollection>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetField(collection, "previousAtscBeat", 0f);
                SetField(collection, "previousChunk", 0);
            }
            base.AfterCleanup();
        }

        protected void SetMode(EditingMode mode) =>
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = mode;

        protected T Spawn<T>(T obj) where T : BaseObject
        {
            touchedObjectTypes.Add(obj.ObjectType);
            if (obj is BaseEventBoxGroup)
            {
                touchedObjectTypes.Add(ObjectType.GLSEvent);
            }
            obj.SetMap(BeatSaberSongContainer.Instance.Map);
            obj.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);
            Assert.That(collection, Is.Not.Null);
            collection.SpawnObject(obj, false, false, true);
            // Spawning alone skips preview registration. Publish the placement action so preview state
            // matches normal editor placement.
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(obj, Array.Empty<BaseObject>(), "Placed song boundary fixture."));
            return obj;
        }

        protected override void CleanupTestObjects() => CleanupUtils.CleanupObjects(touchedObjectTypes);

        protected void ShiftWithKeyboard(bool forward) =>
            SendShortcut(Key.LeftShift, forward ? Key.UpArrow : Key.DownArrow);

        protected void CopyWithKeyboard() => SendShortcut(Key.LeftCtrl, Key.C);
        protected void PasteWithKeyboard() => SendShortcut(Key.LeftCtrl, Key.V);

        // Isolate same-frame modifier processing so neither native devices nor shared callbacks can execute a second edit.
        private void SendShortcut(Key modifier, Key key)
        {
            var shared = CMInputCallbackInstaller.InputInstance;
            var enabledMaps = shared.asset.actionMaps.Where(map => map.enabled).ToArray();
            shared.Disable();
            var fixture = new InputTestFixture();
            fixture.Setup();
            var input = new CMInput();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                input.ModifyingSelection.SetCallbacks(Selection);
                input.Selecting.SetCallbacks(Selection);
                input.ModifyingSelection.Enable();
                input.Selecting.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(modifier));
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(modifier, key));
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
            }
            finally
            {
                input.Dispose();
                fixture.TearDown();
                Selection.OnActivateShiftinTime(default);
                Selection.OnActivateShiftinPlace(default);
                foreach (var map in enabledMaps)
                    map.Enable();
            }
        }

        // Scene prefabs can contain inactive siblings; prefer the initialized placement used by the live editor.
        protected BasePlacement FindPlacement(Type type)
        {
            var placement = placements.Where(candidate => candidate.GetType() == type)
                .OrderByDescending(candidate => candidate.isActiveAndEnabled).FirstOrDefault();
            Assert.That(placement, Is.Not.Null, $"Missing {type.Name} in the test scene.");
            return placement;
        }

        protected void DragToBeat(BasePlacement placement, BaseObject obj, float targetBeat,
            IndicatorType indicatorType = IndicatorType.Head)
        {
            Assert.That(placement.CanClickAndDrag, Is.True);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);
            var container = collection.CreateContainer();
            var surface = new GameObject("Song boundary drag surface");
            var hitObject = new GameObject("Song boundary drag hit");
            hitObject.transform.SetParent(surface.transform);
            var oldBounds = placement.Bounds;
            try
            {
                container.ObjectData = obj;
                container.Setup();
                container.UpdateGridPosition();
                ObjectContainer target = container;
                if (placement is ArcIndicatorPlacement)
                {
                    target = container.GetComponentsInChildren<ArcIndicatorContainer>(true)
                        .Single(indicator => indicator.IndicatorType == indicatorType);
                }
                else if (placement is ChainIndicatorPlacement)
                {
                    // The chain prefab has two tail handles for the same endpoint. Either can exercise tail
                    // dragging.
                    target = container.GetComponentsInChildren<ChainIndicatorContainer>(true)
                        .First(indicator => indicator.IndicatorType == indicatorType);
                }
                if (obj is not BaseEventBoxGroup)
                    placement.Initialize(null);
                Assert.That(placement.StartDrag(target.gameObject), Is.Not.Null);
                placement.Bounds = new Bounds(new Vector3(32f, 2f, 0f), new Vector3(64f, 4f, 1000f));
                var localPoint = placement.PlacementTrack.InverseTransformPoint(target.transform.position);
                if (obj is BaseGLSEvent)
                    localPoint.x = 0.5f;
                if (obj is BaseEvent evt)
                {
                    var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
                    var lane = labels.EventToLaneId(evt);
                    Assert.That(lane, Is.GreaterThanOrEqualTo(0), "Basic event test lane must be visible.");
                    localPoint.x = lane + 0.5f;
                }
                var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(targetBeat);
                localPoint.z = songBeat * EditorScaleController.EditorScale;
                if (placement.AdjustZScale)
                    localPoint.z = (localPoint.z * BeatmapConstant.LaneSize) + BeatmapConstant.ZOffset;
                var worldPoint = placement.PlacementTrack.TransformPoint(localPoint);
                placement.UpdateState(new Intersections.IntersectionHit(hitObject,
                    new Bounds(Vector3.zero, Vector3.one), new Ray(worldPoint, Vector3.forward), 0f),
                    PlacementInputState.Drag);
                placement.FinishDrag();
                Assert.That(placement.IsDragging, Is.False, "Releasing a boundary drag must retire its state.");
            }
            finally
            {
                if (placement.IsDragging)
                    placement.FinishDrag();
                placement.Bounds = oldBounds;
                Object.DestroyImmediate(container.gameObject);
                Object.DestroyImmediate(surface);
            }
        }

        protected static T GetField<T>(object target, string name) => (T)FindField(target, name).GetValue(target);
        protected static void SetField(object target, string name, object value) => FindField(target, name).SetValue(target, value);

        private static FieldInfo FindField(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            throw new AssertionException($"Missing field {target.GetType().Name}.{name}");
        }
    }
}
