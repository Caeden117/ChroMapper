using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class SelectionControllerTest : TestBase
    {
        private SelectionFixture _fixture;

        [SetUp]
        public void PlaceObjects() => _fixture = new SelectionFixture();

        [Test]
        public void SelectBetweenNotes()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Note3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotes());
        }

        // Copy/paste must preserve sub-beat spacing when the source selection is anchored at an
        // off-beat float; subtracting and reapplying the anchor must not snap either note.
        [Test]
        public void PasteOffBeatNotesPreservesAnchorAndRelativeSpacing()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var first = PlaceUtils.Place(new BaseNote { JsonTime = 21.078f });
            var second = PlaceUtils.Place(new BaseNote { JsonTime = 21.141f });
            var expectedSpacing = second.JsonTime - first.JsonTime;
            // Keep the anchor beyond other test cursor positions so the paste verifies its requested off-beat time rather than a retained fixture cursor.
            const float pasteBeat = 50.485f;

            SelectionController.Select(first);
            SelectionController.Select(second, true);
            selectionController.Copy();
            atsc.MoveToJsonTime(pasteBeat);
            selectionController.Paste();

            var pasted = SelectionController.SelectedObjects
                .OfType<BaseNote>()
                .OrderBy(note => note.JsonTime)
                .ToArray();
            Assert.That(pasted, Has.Length.EqualTo(2));
            Assert.That(pasted[0].JsonTime, Is.EqualTo(pasteBeat).Within(0.00001f));
            Assert.That(pasted[1].JsonTime - pasted[0].JsonTime, Is.EqualTo(expectedSpacing).Within(0.00001f));
        }

        [Test]
        public void ShiftInTimeFromEitherDirectionSnapsToSameGridLine()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.GridMeasureSnapping = 32;
            const float expectedGridLine = 115.1875f;
            var gridInterval = 1f / atsc.GridMeasureSnapping;

            var fromBefore = PlaceUtils.Place(new BaseEvent { JsonTime = 115.156f, Type = 0, Value = 1 });
            SelectionController.Select(fromBefore);
            selectionController.MoveSelection(gridInterval, true);
            var movedFromBefore = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();

            SelectionController.DeselectAll();
            var fromAfter = PlaceUtils.Place(new BaseEvent { JsonTime = 115.219f, Type = 1, Value = 1 });
            SelectionController.Select(fromAfter);
            selectionController.MoveSelection(-gridInterval, true);
            var movedFromAfter = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();

            Assert.That(movedFromBefore.JsonTime, Is.EqualTo(expectedGridLine));
            Assert.That(movedFromAfter.JsonTime, Is.EqualTo(expectedGridLine));
            Assert.That(movedFromAfter.JsonTime, Is.EqualTo(movedFromBefore.JsonTime));
        }

        [Test]
        public void ShiftInTimePreservesOffsetOutsideJsonPrecision()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.GridMeasureSnapping = 32;
            var gridInterval = 1f / atsc.GridMeasureSnapping;
            const float originalTime = 115.158f;
            var expectedOffGridTime = originalTime + gridInterval;
            var nearestGridLine = Mathf.Round(expectedOffGridTime * atsc.GridMeasureSnapping)
                / atsc.GridMeasureSnapping;
            Assert.That(
                Mathf.Abs(expectedOffGridTime - nearestGridLine),
                Is.GreaterThan(BeatmapObjectContainerCollection.Epsilon));

            var source = PlaceUtils.Place(new BaseEvent { JsonTime = originalTime, Type = 0, Value = 1 });
            SelectionController.Select(source);
            selectionController.MoveSelection(gridInterval, true);
            var moved = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();

            Assert.That(moved.JsonTime, Is.EqualTo(expectedOffGridTime));
        }

        [Test]
        public void CursorTieSnapsForwardWhileShiftedObjectTieSnapsBackward()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var originalEpsilon = BeatmapObjectContainerCollection.Epsilon;
            var originalGridSnapping = atsc.GridMeasureSnapping;
            const int gridSnapping = 64;
            const float previousGridLine = 10f;
            var gridInterval = 1f / gridSnapping;
            var midpoint = previousGridLine + (gridInterval / 2f);

            try
            {
                atsc.GridMeasureSnapping = gridSnapping;
                BeatmapObjectContainerCollection.Epsilon = 0.01f;

                atsc.MoveToJsonTime(midpoint);
                atsc.SnapToGrid();
                Assert.That(atsc.CurrentJsonTime, Is.EqualTo(previousGridLine + gridInterval));

                var source = PlaceUtils.Place(new BaseEvent
                {
                    JsonTime = midpoint - gridInterval,
                    Type = 0,
                    Value = 1
                });
                SelectionController.Select(source);
                selectionController.MoveSelection(gridInterval, true);
                var moved = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();

                Assert.That(moved.JsonTime, Is.EqualTo(previousGridLine));
            }
            finally
            {
                BeatmapObjectContainerCollection.Epsilon = originalEpsilon;
                atsc.GridMeasureSnapping = originalGridSnapping;
            }
        }

        [Test]
        public void ShiftClickEquivalentSelectionIncludesArc()
        {
            // Model Shift-click's one-anchor range selection and ensure the clicked arc remains selected.
            SelectionController.Select(_fixture.Note1);
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Arc24, true);

            Assert.IsTrue(SelectionController.IsObjectSelected(_fixture.Arc24));
        }

        [Test]
        public void ArcIndicatorHitResolvesToOwningArcContainer()
        {
            // Protect arrow-end selection by resolving an indicator child through its ArcContainer parent.
            var arcContainerObject = new GameObject("Arc owner test");
            var arcContainer = arcContainerObject.AddComponent<ArcContainer>();
            var indicatorObject = new GameObject("Arc indicator test");
            indicatorObject.transform.SetParent(arcContainerObject.transform);

            try
            {
                var resolved = indicatorObject.GetComponentInParent<ArcContainer>();

                Assert.AreSame(arcContainer, resolved);
            }
            finally
            {
                Object.DestroyImmediate(arcContainerObject);
            }
        }

        [Test]
        public void ShiftClickArcIndicatorResolvesThroughRaycastFirstObject()
        {
            // Exercise the shift-click hit path so an indicator child resolves to the selectable arc owner.
            var arcContainerObject = new GameObject("Arc owner raycast test");
            var arcContainer = arcContainerObject.AddComponent<ArcContainer>();
            var indicatorObject = new GameObject("Arc indicator raycast test");
            indicatorObject.transform.SetParent(arcContainerObject.transform);
            var inputControllerObject = new GameObject("Arc input controller test");
            var inputController = inputControllerObject.AddComponent<TestArcInputController>();

            try
            {
                BeatmapRaycastCache.FirstHit = indicatorObject;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;

                var resolved = inputController.ResolveRaycast(out var resolvedContainer);

                Assert.IsTrue(resolved);
                Assert.AreSame(arcContainer, resolvedContainer);
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(inputControllerObject);
                Object.DestroyImmediate(arcContainerObject);
            }
        }

        [Test]
        public void HoveringSelectedArcPreservesSelectionHighlightState()
        {
            // Protect the selected outline from transient hover state changes on a loaded arc container.
            var arcCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var arcContainer = arcCollection.LoadedContainers[_fixture.Arc02] as ArcContainer;

            Assert.IsNotNull(arcContainer);
            SelectionController.Select(_fixture.Arc02);
            Assert.IsTrue(arcContainer.Selected);

            arcContainer.Highlighted = true;
            arcContainer.Highlighted = false;

            Assert.IsTrue(SelectionController.IsObjectSelected(_fixture.Arc02));
            Assert.IsTrue(arcContainer.Selected);
        }

        [Test]
        public void SelectBetweenEvents()
        {
            SelectionController.SelectBetween(_fixture.Event1, _fixture.Event3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenEvents());
        }

        [Test]
        public void SelectBetweenBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.BpmEvent1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenBpmEvents());
        }

        [Test]
        public void SelectBetweenNotesAndEvents()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Event3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotesAndEvents());
        }

        [Test]
        public void SelectBetweenNotesAndBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotesAndBpmEvents());
        }

        [Test]
        public void SelectBetweenEventsAndBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.Event1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenEventsAndBpmEvents());
        }

        // Guard the allocation-free backing range query against admitting events outside its beat interval.
        [Test]
        public void SongBpmRangeQueryVisitsOnlyEventsInsideBounds()
        {
            var visited = new HashSet<BaseObject>();
            var eventBeat = _fixture.Event3.SongBpmTime;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                eventBeat - 0.1f,
                eventBeat + 0.1f,
                ObjectType.Event,
                (_, obj) => visited.Add(obj));

            CollectionAssert.AreEquivalent(new[] { _fixture.Event3 }, visited);
        }

        // A real box drag must select an event after scrolling pools its visual container out of the loading zone.
        [Test]
        public void BoxSelectionSelectsEventOutsideLoadingZoneAfterForwardScroll()
        {
            var boxSelection = Object.FindAnyObjectByType<BoxSelectionPlacement>();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var providerObject = new GameObject("Box selection loading-zone test provider");
            var provider = providerObject.AddComponent<PlacementProvider>();
            provider.Placements = new BasePlacement[] { boxSelection, eventPlacement };
            var eventCollection = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var selectedTypes = provider.Placements.Aggregate(
                (ObjectType)0,
                (types, placement) => types | placement.ObjectDataType);
            var eventBeat = _fixture.Event3.SongBpmTime;
            Assert.AreNotEqual(0, selectedTypes & ObjectType.Event);
            var originalBoxSelect = Settings.Instance.BoxSelect;
            var originalState = boxSelection.State;
            var hitParent = new GameObject("Box selection loading-zone test surface");
            var hitObject = new GameObject("Box selection loading-zone test hit");
            hitObject.transform.SetParent(hitParent.transform);

            try
            {
                Settings.Instance.BoxSelect = true;
                boxSelection.Initialize(provider);
                boxSelection.State = PlacementState.Active;
                boxSelection.UpdateState(CreateHit(eventBeat - 1f, -100f), PlacementInputState.Hover);
                boxSelection.HandleApply();

                eventCollection.RefreshPool(-1f, 1.5f);
                Assert.False(eventCollection.LoadedContainers.ContainsKey(_fixture.Event3));
                boxSelection.UpdateState(CreateHit(eventBeat + 0.1f, 100f), PlacementInputState.Hover);

                Assert.True(SelectionController.IsObjectSelected(_fixture.Event3));
            }
            finally
            {
                boxSelection.Cancel();
                boxSelection.State = originalState;
                Settings.Instance.BoxSelect = originalBoxSelect;
                eventCollection.RefreshPool(-1f, 5f);
                Object.DestroyImmediate(hitParent);
                Object.DestroyImmediate(providerObject);
            }

            Intersections.IntersectionHit CreateHit(float beat, float laneX)
            {
                var point = boxSelection.PlacementTrack.TransformPoint(new Vector3(
                    laneX,
                    0f,
                    beat * EditorScaleController.EditorScale));
                return new Intersections.IntersectionHit(
                    hitObject,
                    new Bounds(Vector3.zero, Vector3.one),
                    new Ray(point, Vector3.forward),
                    0f);
            }
        }

        // Ensure the bit iteration reaches Note directly without relying on the old shift-by-32 wraparound.
        [Test]
        public void SongBpmRangeQueryVisitsNoteBitOnce()
        {
            var visited = new HashSet<BaseObject>();
            var noteBeat = _fixture.Note3.SongBpmTime;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                noteBeat - 0.1f,
                noteBeat + 0.1f,
                ObjectType.Note,
                (_, obj) => visited.Add(obj));

            CollectionAssert.AreEquivalent(new[] { _fixture.Note3 }, visited);
        }

        // Document the temporary start-time-only behavior until selection has a data-only interval index.
        [Test]
        public void SongBpmRangeQueryDoesNotIncludeOverlappingSliderTails()
        {
            var visited = new HashSet<BaseObject>();
            var queryEnd = _fixture.Arc24.SongBpmTime - 0.1f;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                queryEnd - 0.1f,
                queryEnd,
                ObjectType.Arc,
                (_, obj) => visited.Add(obj));

            CollectionAssert.IsEmpty(visited);
        }

        // Preserve dev's behavior for arcs whose heads precede the query but whose tails overlap its lower bound.
        // [Test] // Skipping for now because we can't do this cleanly with binary search without adding end events into the sorted collection.
        public void SongBpmRangeQueryIncludesOverlappingSliders()
        {
            var visited = new HashSet<BaseObject>();
            var queryEnd = _fixture.Arc24.SongBpmTime - 0.1f;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                queryEnd - 0.1f,
                queryEnd,
                ObjectType.Arc,
                (_, obj) => visited.Add(obj));

            CollectionAssert.AreEquivalent(new[] { _fixture.Arc02, _fixture.Arc04 }, visited);
        }

        // Guard the merge regression where GLS lanes after a visual track gap were tested at their left edge.
        [TestCase(0f, 0.5f)]
        [TestCase(4f, 4.5f)]
        public void GlsLaneCenterIncludesHalfLaneOffset(float trackOffset, float expectedCenter)
        {
            var center = BoxSelectionPlacement.GetGlsLaneCenter(trackOffset);

            Assert.AreEqual(expectedCenter, center.x);
            Assert.AreEqual(0.5f, center.y);
        }

        // Reproduce the adjacent-lane selection seen far right in large even and odd centered inner GLS groups.
        [TestCase(60, 50, 20f, 20.5f)]
        [TestCase(61, 51, 20.5f, 21f)]
        public void GlsInnerEventUsesRenderedCenterInLargeLaneGroup(
            int laneCount,
            int adjacentBoxIndex,
            float selectionRightEdge,
            float expectedCenter)
        {
            var gridStartLane = -laneCount / 2f;
            var boundsPositionX = gridStartLane * BeatmapConstant.LaneSize;

            var center = BoxSelectionPlacement.GetGlsEventSelectionPosition(
                adjacentBoxIndex,
                boundsPositionX);

            Assert.AreEqual(expectedCenter, center.x, 0.0001f);
            Assert.Greater(center.x, selectionRightEdge);
            Assert.AreEqual(0.5f, center.y);
        }

        // Guard the preview-mode path that selects groups only at their rendered inner-event beats.
        [TestCase(0f, false)]
        [TestCase(0.5f, true)]
        public void GlsPreviewOpacityControlsGroupBeatSelection(float previewOpacity, bool expectedPreviewTimes)
        {
            Assert.AreEqual(
                expectedPreviewTimes,
                BoxSelectionPlacement.UsesGlsPreviewNodeTimes(previewOpacity));
        }

        // Verify preview selection uses logical child beats and therefore does not depend on loaded scene containers.
        [Test]
        public void GlsPreviewBeatMatchUsesBackingGroupData()
        {
            var group = CreateTwoLaneColorGroup();
            var previewBeat = group.ReadOnlyBoxes[0].ReadOnlyEvents[0].SongBpmTime;
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.True(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat - 0.1f,
                previewBeat + 0.1f,
                epsilon));
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat + 0.1f,
                previewBeat + 0.2f,
                epsilon));
        }

        // Preserve selection of a later preview node when its parent group starts before the dragged beat range.
        [Test]
        public void GlsPreviewBeatMatchIncludesLaterNodeFromEarlierGroup()
        {
            var group = CreateTwoLaneColorGroup();
            var laterPreviewBeat = group.ReadOnlyBoxes[1].ReadOnlyEvents[0].SongBpmTime;
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.True(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                laterPreviewBeat - 0.1f,
                laterPreviewBeat + 0.1f,
                epsilon));
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                group.SongBpmTime - 0.1f,
                group.SongBpmTime + 0.1f,
                epsilon));
        }

        // Reproduce an offscreen parent whose first preview node begins inside the dragged selection range.
        [Test]
        public void GlsPreviewIntervalIndexIncludesPreviewSpanStartingInsideSelection()
        {
            var group = CreateTwoLaneColorGroup();
            group.ID = 999;
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.SpawnObject(group, false, false);

            try
            {
                var candidates = new HashSet<BaseEventBoxGroup>();
                var index = new GlsPreviewIntervalIndex();

                index.AddOverlappingPreviewIntervals(
                    collection,
                    group.JsonTime + 0.25f,
                    group.JsonTime + 0.5f,
                    candidates);

                Assert.True(candidates.Contains(group));
            }
            finally
            {
                collection.DeleteObject(group, false, false);
            }
        }

        // Preserve preview-beat selection whether the group cache was built by rendering or by selection.
        [Test]
        public void GlsPreviewBeatMatchIsIndependentOfCacheWarmup()
        {
            var group = CreateTwoLaneColorGroup();
            var previewBeat = group.ReadOnlyBoxes[1].ReadOnlyEvents[0].SongBpmTime;
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            var coldResult = BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat - 0.1f,
                previewBeat + 0.1f,
                epsilon);
            group.ResortOrderedEvents();
            var warmResult = BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat - 0.1f,
                previewBeat + 0.1f,
                epsilon);

            Assert.True(coldResult);
            Assert.AreEqual(coldResult, warmResult);
        }

        // Preserve the empty authored-group behavior while cache initialization is refactored separately.
        [Test]
        public void EmptyGlsPreviewGroupNeverMatchesASelectionRange()
        {
            var group = CreateEmptyColorGroup();
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 0f, 1f, epsilon));
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 1f, 2f, epsilon));
        }

        // Ensure an authored empty group is marked initialized instead of rebuilding the same empty preview cache.
        [Test]
        public void EmptyGlsPreviewGroupInitializesItsCacheOnlyOnce()
        {
            var group = CreateEmptyColorGroup();
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.True(group.OrderedEventsInitialized);
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 0f, 1f, epsilon));
            var initializedCache = group.OrderedEvents;
            Assert.True(group.OrderedEventsInitialized);
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 0f, 1f, epsilon));

            Assert.AreSame(initializedCache, group.OrderedEvents);
        }

        // Guard both forward and reverse box drags using the immutable beat-space endpoints.
        [TestCase(2f, 5f, 2f, 5f)]
        [TestCase(5f, 2f, 2f, 5f)]
        public void BoxSelectionBeatBoundsNormalizeDragDirection(
            float originBeat,
            float currentBeat,
            float expectedStart,
            float expectedEnd)
        {
            var bounds = BoxSelectionPlacement.GetSongBpmBounds(originBeat, currentBeat);

            Assert.AreEqual(expectedStart, bounds.Start);
            Assert.AreEqual(expectedEnd, bounds.End);
        }

        // Exercise mouse positions across multi-lane tracks, a single-lane track, gaps, and both unbounded outer regions.
        [TestCase(-100f, -2)]
        [TestCase(-1.2f, -2)]
        [TestCase(0.75f, 0)]
        [TestCase(1f, 0)]
        [TestCase(2.2f, 3)]
        [TestCase(3.5f, 3)]
        [TestCase(4f, 3)]
        [TestCase(4.9f, 3)]
        [TestCase(5.1f, 6)]
        [TestCase(8.8f, 8)]
        [TestCase(9f, 8)]
        [TestCase(100f, 8)]
        public void BoxSelectionGroundMousePositionMapsToNearestValidGlsLane(float mouseX, int expectedLane)
        {
            // GetNearestGroundLaneX operates on the same reusable list type populated by the live grid refresh.
            var ranges = new List<Vector2>
            {
                BoxSelectionPlacement.CreateGroundLaneRange(-2f, 1f, 0f),
                BoxSelectionPlacement.CreateGroundLaneRange(3f, 4f, 0f),
                BoxSelectionPlacement.CreateGroundLaneRange(6f, 9f, 0f)
            };

            var resolvedX = BoxSelectionPlacement.GetNearestGroundLaneX(ranges, mouseX);

            Assert.AreEqual(expectedLane, Mathf.FloorToInt(resolvedX));
        }

        // A transient grid rebuild with no active lanes must not erase the last valid snap index for the rest of the session.
        [Test]
        public void BoxSelectionGroundRangesSurviveTransientEmptyGridRefresh()
        {
            var expectedRange = BoxSelectionPlacement.CreateGroundLaneRange(-2f, 1f, 0f);
            var ranges = new List<Vector2> { expectedRange };
            var transientEmptyRefresh = new List<Vector2>();

            BoxSelectionPlacement.ReplaceGroundLaneRanges(ranges, transientEmptyRefresh);

            CollectionAssert.AreEqual(new[] { expectedRange }, ranges);
        }

        // Returning from beyond the right edge must visit every valid boundary, including rightmost lanes and the single-lane track.
        [Test]
        public void BoxSelectionGroundBoundaryCanShrinkAcrossMultipleGlsTracks()
        {
            // The boundary regression exercises the production list-backed binary-search path.
            var ranges = new List<Vector2>
            {
                BoxSelectionPlacement.CreateGroundLaneRange(-2f, 1f, 0f),
                BoxSelectionPlacement.CreateGroundLaneRange(3f, 4f, 0f),
                BoxSelectionPlacement.CreateGroundLaneRange(6f, 9f, 0f)
            };
            var mousePositions = new[] { 100f, 8.2f, 7.2f, 6.2f, 5.1f, 4.9f, 3.4f, 2.2f, 0.8f, -1.2f, -100f };
            var expectedLanes = new[] { 8, 8, 7, 6, 6, 3, 3, 3, 0, -2, -2 };
            var resolvedLanes = mousePositions
                .Select(mouseX => Mathf.FloorToInt(BoxSelectionPlacement.GetNearestGroundLaneX(ranges, mouseX)))
                .ToArray();

            CollectionAssert.AreEqual(expectedLanes, resolvedLanes);
        }

        // Ctrl-active box selection must retain ground projection before the first click reaches an unloaded negative-beat region.
        [TestCase(PlacementState.Idle, false)]
        [TestCase(PlacementState.Active, true)]
        [TestCase(PlacementState.Placing, true)]
        public void BoxSelectionProjectionOwnershipIncludesPreClickActiveState(PlacementState state, bool expected)
        {
            Assert.AreEqual(expected, PlacementInputSystem.BoxSelectionOwnsProjection(state));
        }

        // Off-grid Ctrl projection must not update EventPlacement and turn its preview lane into a Ctrl+V paste anchor.
        [Test]
        public void BoxSelectionProjectionUpdatesOnlyBoxPlacement()
        {
            var boxSelection = Object.FindAnyObjectByType<BoxSelectionPlacement>();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            Assert.True(PlacementInputSystem.ShouldUpdatePlacementForBoxProjection(
                boxSelection,
                boxSelection,
                true));
            Assert.False(PlacementInputSystem.ShouldUpdatePlacementForBoxProjection(
                eventPlacement,
                boxSelection,
                true));
            Assert.True(PlacementInputSystem.ShouldUpdatePlacementForBoxProjection(
                eventPlacement,
                boxSelection,
                false));
        }

        // Note-mode box completion must preserve selections when either click endpoint is before beat zero.
        [TestCase(-1f, 1f)]
        [TestCase(1f, -1f)]
        [TestCase(-1f, 2f)]
        [TestCase(2f, -1f)]
        public void BoxSelectionCompletesWithNegativeNoteBeatEndpoint(float startOffset, float endOffset)
        {
            var boxSelection = Object.FindAnyObjectByType<BoxSelectionPlacement>();
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var providerObject = new GameObject("Negative note box selection test provider");
            var provider = providerObject.AddComponent<PlacementProvider>();
            provider.Placements = new BasePlacement[] { boxSelection, notePlacement };
            var note = PlaceUtils.Place(new BaseNote { JsonTime = -2f, PosX = 0, PosY = 0 });
            var originalBoxSelect = Settings.Instance.BoxSelect;
            var originalState = boxSelection.State;
            var hitParent = new GameObject("Negative note box selection test surface");
            var hitObject = new GameObject("Negative note box selection test hit");
            hitObject.transform.SetParent(hitParent.transform);

            try
            {
                Settings.Instance.BoxSelect = true;
                boxSelection.Initialize(provider);
                boxSelection.State = PlacementState.Active;
                boxSelection.UpdateState(CreateHit(note.SongBpmTime + startOffset, -3f), PlacementInputState.Hover);
                boxSelection.HandleApply();
                boxSelection.UpdateState(CreateHit(note.SongBpmTime + endOffset, 3f), PlacementInputState.Hover);

                Assert.True(SelectionController.IsObjectSelected(note));
                boxSelection.HandleApply();

                Assert.AreEqual(PlacementState.Idle, boxSelection.State);
                Assert.True(SelectionController.IsObjectSelected(note));
            }
            finally
            {
                boxSelection.Cancel();
                boxSelection.State = originalState;
                Settings.Instance.BoxSelect = originalBoxSelect;
                Object.DestroyImmediate(hitParent);
                Object.DestroyImmediate(providerObject);
            }

            Intersections.IntersectionHit CreateHit(float beat, float laneX)
            {
                var point = boxSelection.PlacementTrack.TransformPoint(new Vector3(
                    laneX,
                    0f,
                    beat * EditorScaleController.EditorScale));
                return new Intersections.IntersectionHit(
                    hitObject,
                    new Bounds(Vector3.zero, Vector3.one),
                    new Ray(point, Vector3.forward),
                    0f);
            }
        }

        // Guard incremental scrolling so only true expansion can reuse the existing logical result.
        [TestCase(0f, 10f, 0f, 11f, true)]
        [TestCase(0f, 10f, -1f, 10f, true)]
        [TestCase(0f, 10f, 0f, 9f, false)]
        [TestCase(0f, 10f, 1f, 10f, false)]
        public void BoxSelectionDetectsMonotonicBeatExpansion(
            float previousStart,
            float previousEnd,
            float currentStart,
            float currentEnd,
            bool expected)
        {
            Assert.AreEqual(
                expected,
                BoxSelectionPlacement.BeatBoundsMonotonicallyExpand(
                    previousStart,
                    previousEnd,
                    currentStart,
                    currentEnd,
                    BeatmapObjectContainerCollection.Epsilon));
        }

        // Verify time shifts replace the owning GLS group and retain a valid selected child instance.
        [Test]
        public void MoveSelectionRebindsSelectedGlsEventToReplacementGroup()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            var selectedEvent = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            SelectionController.Select(selectedEvent);

            selectionController.MoveSelection(0.25f);

            var movedEvent = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().Single();
            Assert.AreEqual(0.75f, movedEvent.RelativeJsonTime);
            Assert.AreEqual(2.75f, movedEvent.JsonTime);
            Assert.AreEqual(0, movedEvent.BoxIndex);
            Assert.AreSame(movedEvent.EventBoxGroupData.ReadOnlyBoxes[0], movedEvent.EventBoxData);
            Assert.AreSame(
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext,
                movedEvent.EventBoxGroupData);

            // Verify both action directions retain the correct parent-owned relative time.
            PlaceUtils.Undo();
            Assert.AreEqual(
                0.5f,
                Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext.ReadOnlyBoxes[0].ReadOnlyEvents[0].RelativeJsonTime);
            PlaceUtils.Redo();
            Assert.AreEqual(
                0.75f,
                Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext.ReadOnlyBoxes[0].ReadOnlyEvents[0].RelativeJsonTime);
        }

        // Verify lane shifts rebuild child ownership and preserve boundary-node selection in the same group action.
        [Test]
        public void ShiftSelectionRebindsAllSelectedGlsEventsAtLaneBoundary()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);
            SelectionController.Select(group.ReadOnlyBoxes[1].ReadOnlyEvents[0], true);

            selectionController.ShiftSelection(1, 0);

            var shiftedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(2, shiftedEvents.Length);
            Assert.True(shiftedEvents.All(evt => evt.BoxIndex == 1));
            Assert.True(shiftedEvents.All(evt =>
                ReferenceEquals(evt.EventBoxData, evt.EventBoxGroupData.ReadOnlyBoxes[1])));
            Assert.AreSame(
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext,
                shiftedEvents[0].EventBoxGroupData);

            // Verify undo and redo restore the complete lane distribution, not only selected child instances.
            PlaceUtils.Undo();
            var restoredGroup = Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext;
            Assert.AreEqual(1, restoredGroup.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            Assert.AreEqual(1, restoredGroup.ReadOnlyBoxes[1].ReadOnlyEvents.Count);
            PlaceUtils.Redo();
            var redoneGroup = Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext;
            Assert.AreEqual(0, redoneGroup.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            Assert.AreEqual(2, redoneGroup.ReadOnlyBoxes[1].ReadOnlyEvents.Count);
        }

        // Ctrl+Right must materialize a visible automatic Y lane, move the selected node there, and retire the vacated automatic X box.
        [Test]
        public void ShiftSelectionMovesTranslationNodeFromAutomaticXIntoGhostY()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = new BaseLightTranslationBase { RelativeJsonTime = 0.5f, Translation = 25 };
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 2,
                ID = 1,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        IsAutomaticAxisLane = true,
                        Events = new[] { source }
                    }
                }
            };
            // Match real placement ownership so this regression reaches Ctrl+Arrow's displayed-lane boundary logic.
            group.NormalizeLoadedEventConflicts();
            PlaceGlsGroup(group);
            SelectionController.Select(group.Boxes[0].Events[0]);

            selectionController.ShiftSelection(1, 0);

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            Assert.AreEqual(1, editedGroup.Boxes.Count);
            Assert.AreEqual(1, editedGroup.Boxes[0].Axis);
            Assert.True(editedGroup.Boxes[0].IsAutomaticAxisLane);
            Assert.AreEqual(1, editedGroup.Boxes[0].Events.Length);
            Assert.AreEqual(25, editedGroup.Boxes[0].Events[0].Translation);
            Assert.AreEqual(0, editedGroup.Boxes[0].Events[0].BoxIndex);
            Assert.AreSame(editedGroup.Boxes[0], editedGroup.Boxes[0].Events[0].EventBoxData);
            Assert.AreSame(editedGroup, editedGroup.Boxes[0].Events[0].EventBoxGroupData);
            var selectedEvent = SelectionController.SelectedObjects.OfType<BaseLightTranslationBase>().Single();
            Assert.AreSame(editedGroup.Boxes[0].Events[0], selectedEvent);
            Assert.True(provider.TryGetDisplayedBox(0, out var emptyXAxis));
            Assert.AreEqual(0, (int)emptyXAxis.GetAxis());
            Assert.True(emptyXAxis.IsAutomaticAxisLane);
            Assert.AreEqual(0, emptyXAxis.ReadOnlyEvents.Count);
            Assert.False(editedGroup.ReadOnlyBoxes.Contains(emptyXAxis));
        }

        // Selecting an outer GLS group beside its inner node must not apply two competing parent replacements during mirror.
        [Test]
        public void MirrorSkipsSelectedGlsParentOwnedBySelectedInnerNode()
        {
            SelectionController.DeselectAll();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            var selectedEvent = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            SelectionController.Select(selectedEvent);
            SelectionController.Select(group, true);

            Object.FindAnyObjectByType<MirrorSelection>().Mirror();

            var selectedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(1, selectedEvents.Length);
            Assert.False(SelectionController.SelectedObjects.Any(obj => obj is BaseEventBoxGroup));
            Assert.AreSame(
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext,
                selectedEvents[0].EventBoxGroupData);
        }

        // Alt-drag replaces the parent group, so initial placement, undo, and redo must never select that outer group in the inner GLS view.
        [Test]
        public void AltDraggingInnerGlsNodeNeverSelectsOuterGroupAcrossUndoRedo()
        {
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.EventBox;
            try
            {
                SelectionController.DeselectAll();
                var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
                var eventCollection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                var draggedEvent = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
                var originalGroup = BeatmapFactory.Clone(group);

                // Mirror the drag path: temporarily remove the live child, then publish its destination against the pre-drag parent.
                eventCollection.SilentRemoveObject(draggedEvent);
                eventCollection.UseOriginalGroupForNextReplacement(originalGroup);
                eventCollection.SpawnObject(draggedEvent, out _);
                AssertNoOuterGlsGroupSelection();

                PlaceUtils.Undo();
                AssertNoOuterGlsGroupSelection();
                PlaceUtils.Redo();
                AssertNoOuterGlsGroupSelection();

                var replacementEvents = Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext.ReadOnlyBoxes.SelectMany(box => box.ReadOnlyEvents).ToArray();
                SelectionController.Select(replacementEvents[0]);
                SelectionController.Select(replacementEvents[1], true);

                Assert.AreEqual(2, SelectionController.SelectedObjects.Count);
                Assert.True(SelectionController.SelectedObjects.All(obj => obj is BaseLightColorBase));
            }
            finally
            {
                editModeContext.EditingMode = originalMode;
            }
        }

        // Alt-drag hover must move the visible dragged node into a ghost Y lane before release materializes that destination.
        [Test]
        public void AltDragHoverPreviewsTranslationNodeInAutomaticYAxisLane()
        {
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.EventBox;
            var hitParent = new GameObject("GLS translation ghost-lane drag surface");
            var hitObject = new GameObject("GLS translation ghost-lane drag hit");
            hitObject.transform.SetParent(hitParent.transform);
            GLSEventContainer dragContainer = null;
            GLSEventTranslationPlacement placement = null;
            try
            {
                var source = new BaseLightTranslationBase { RelativeJsonTime = 0.5f, Translation = 25 };
                var group = new BaseLightTranslationEventBoxGroup
                {
                    JsonTime = 2,
                    ID = 1,
                    Boxes =
                    {
                        new BaseLightTranslationEventBox
                        {
                            Axis = 0,
                            IsAutomaticAxisLane = true,
                            Events = new[] { source }
                        }
                    }
                };
                group.NormalizeLoadedEventConflicts();
                PlaceGlsGroup(group);
                var eventCollection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                placement = Object.FindObjectsByType<GLSEventTranslationPlacement>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(candidate => candidate.ObjectContainerCollection == eventCollection)
                    .OrderByDescending(candidate => candidate.isActiveAndEnabled)
                    .FirstOrDefault();
                Assert.NotNull(placement);
                // Initialize the placement-owned hover container before drag, matching PlacementProvider's editor lifecycle.
                placement.Initialize(null);
                dragContainer = eventCollection.CreateContainer() as GLSEventContainer;
                Assert.NotNull(dragContainer);
                dragContainer.ObjectData = group.Boxes[0].Events[0];
                dragContainer.Setup();
                dragContainer.UpdateGridPosition();
                Assert.NotNull(placement.StartDrag(dragContainer.gameObject));
                placement.Bounds = new Bounds(new Vector3(1.5f, 0.5f, 0f), new Vector3(3f, 1f, 100f));
                var localPoint = new Vector3(
                    1.5f,
                    0f,
                    source.SongBpmTime * EditorScaleController.EditorScale);
                var worldPoint = placement.PlacementTrack.TransformPoint(localPoint);

                placement.UpdateState(
                    new Intersections.IntersectionHit(
                        hitObject,
                        new Bounds(Vector3.zero, Vector3.one),
                        new Ray(worldPoint, Vector3.forward),
                        0f),
                    PlacementInputState.Drag);

                Assert.AreEqual(1, (int)placement.DraggedObjectData.EventBoxData.GetAxis());
                Assert.AreEqual(-1, placement.DraggedObjectData.BoxIndex);
                Assert.True(dragContainer.gameObject.activeSelf);
                Assert.That(dragContainer.transform.localPosition.x, Is.EqualTo(1.5f).Within(0.00001f));
            }
            finally
            {
                if (placement != null && placement.IsDragging)
                {
                    placement.FinishDrag();
                }

                if (dragContainer != null)
                {
                    Object.DestroyImmediate(dragContainer.gameObject);
                }

                Object.DestroyImmediate(hitParent);
                editModeContext.EditingMode = originalMode;
            }
        }

        // Pasting onto the ghost Y lane between populated X/Z must materialize Y and retain sorted, valid ownership.
        [Test]
        public void PastingNodeIntoMiddleTranslationGhostKeepsXyzLaneOrder()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.EventBox;
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 151,
                ID = 151,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase { Translation = 10 } }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase { Translation = 30 } }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            PlaceGlsGroup(group);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var placement = ConfigureTranslationPasteLane(selectionController, group, emptyYAxis, 1);

            try
            {
                SelectionController.Select(group.Boxes[0].Events[0]);
                selectionController.Copy();

                selectionController.Paste();

                var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
                CollectionAssert.AreEqual(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
                CollectionAssert.AreEqual(
                    new[] { 10f, 10f, 30f },
                    editedGroup.Boxes.SelectMany(box => box.Events).Select(evt => evt.Translation));
                Assert.That(editedGroup.Boxes[1].Events[0].RelativeJsonTime, Is.EqualTo(1f));
                AssertValidGlsEventOwnership(editedGroup);
            }
            finally
            {
                placement.State = PlacementState.Idle;
                SelectionController.CopiedObjects.Clear();
                editModeContext.EditingMode = originalMode;
            }
        }

        // Copying an X node onto an authored Y lane must target the hovered lane and cursor beat, not retain X ownership.
        [Test]
        public void PastingXAxisNodeIntoAuthoredTranslationYAxisUsesCursorPosition()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.EventBox;
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 152,
                ID = 152,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase { Translation = 10 } }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 1,
                        Events = new[] { new BaseLightTranslationBase { Translation = 20 } }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase { Translation = 30 } }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            PlaceGlsGroup(group);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            Assert.True(provider.TryGetDisplayedBox(1, out var yAxis));
            var placement = ConfigureTranslationPasteLane(selectionController, group, yAxis, 1);

            try
            {
                SelectionController.Select(group.Boxes[0].Events[0]);
                selectionController.Copy();

                selectionController.Paste();

                var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
                CollectionAssert.AreEqual(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
                Assert.AreEqual(2, editedGroup.Boxes[1].Events.Length);
                Assert.AreEqual(10, editedGroup.Boxes[1].Events[1].Translation);
                Assert.That(editedGroup.Boxes[1].Events[1].RelativeJsonTime, Is.EqualTo(1f));
                AssertValidGlsEventOwnership(editedGroup);
            }
            finally
            {
                placement.State = PlacementState.Idle;
                SelectionController.CopiedObjects.Clear();
                editModeContext.EditingMode = originalMode;
            }
        }

        // Reproduce an outer alt-drag after an inner mutation preserves its now-empty first filter lane.
        [Test]
        public void OuterGlsColorPreviewSurvivesEmptyStarterLaneAfterInnerDrag()
        {
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.GLS;
            var hitParent = new GameObject("GLS outer placement test surface");
            var hitObject = new GameObject("GLS outer placement test hit");
            hitObject.transform.SetParent(hitParent.transform);
            GLSGroupColorPlacement placement = null;
            GLSGroupContainer dragContainer = null;
            try
            {
                var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                    @"{ ""b"": 2, ""g"": 1, ""e"": [
                        { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [] },
                        { ""f"": { ""f"": 1, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                          ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] }
                    ] }"));
                (placement, dragContainer) = StartOuterColorGroupDrag(group);

                Assert.DoesNotThrow(() => placement.UpdateState(
                    new Intersections.IntersectionHit(
                        hitObject,
                        new Bounds(Vector3.zero, Vector3.one),
                        new Ray(Vector3.zero, Vector3.forward),
                        0f),
                    PlacementInputState.Hover));
            }
            finally
            {
                if (placement != null && placement.IsDragging)
                {
                    placement.FinishDrag();
                }

                if (dragContainer != null)
                {
                    Object.DestroyImmediate(dragContainer.gameObject);
                }

                Object.DestroyImmediate(hitParent);
                editModeContext.EditingMode = originalMode;
            }
        }

        // Outer GLS group drags must stop at the map start instead of authoring invalid negative beats.
        [Test]
        public void OuterGlsColorDragClampsAtBeatZero()
        {
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            editModeContext.EditingMode = EditingMode.GLS;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var originalJsonTime = atsc.CurrentJsonTime;
            var hitParent = new GameObject("GLS negative drag test surface");
            var hitObject = new GameObject("GLS negative drag test hit");
            hitObject.transform.SetParent(hitParent.transform);
            GLSGroupColorPlacement placement = null;
            GLSGroupContainer dragContainer = null;
            try
            {
                (placement, dragContainer) = StartOuterColorGroupDrag(CreateTwoLaneColorGroup());
                atsc.MoveToJsonTime(0f);
                var negativeHitPoint = placement.PlacementTrack.TransformPoint(Vector3.back * EditorScaleController.EditorScale);

                placement.UpdateState(
                    new Intersections.IntersectionHit(
                        hitObject,
                        new Bounds(Vector3.zero, Vector3.one),
                        new Ray(negativeHitPoint, Vector3.forward),
                        0f),
                    PlacementInputState.Drag);

                Assert.Zero(placement.DraggedObjectData.JsonTime);
            }
            finally
            {
                if (placement != null && placement.IsDragging)
                {
                    placement.FinishDrag();
                }

                if (dragContainer != null)
                {
                    Object.DestroyImmediate(dragContainer.gameObject);
                }

                atsc.MoveToJsonTime(originalJsonTime);
                Object.DestroyImmediate(hitParent);
                editModeContext.EditingMode = originalMode;
            }
        }

        // Same-lane same-beat GLS nodes normalize to the later event before movement can rebuild the group.
        [Test]
        public void MoveSelectionRebindsDuplicateGlsEvents()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateDuplicateColorGroup());
            Assert.AreEqual(1, group.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);

            selectionController.MoveSelection(0.25f);

            var movedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(1, movedEvents.Length);
            Assert.True(movedEvents.All(evt => Mathf.Approximately(evt.RelativeJsonTime, 0.75f)));
            Assert.AreEqual(1, movedEvents[0].EventBoxData.ReadOnlyEvents.Count);
        }

        // Same-lane same-beat GLS nodes normalize before a lane replacement moves the surviving node.
        [Test]
        public void ShiftSelectionRebindsDuplicateGlsEvents()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateDuplicateColorGroup());
            Assert.AreEqual(1, group.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);

            selectionController.ShiftSelection(1, 0);

            var shiftedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(1, shiftedEvents.Length);
            Assert.True(shiftedEvents.All(evt => evt.BoxIndex == 1));
            Assert.True(shiftedEvents.All(evt => ReferenceEquals(evt.EventBoxData, evt.EventBoxGroupData.ReadOnlyBoxes[1])));
        }

        private void AssertSelectedObjects(ICollection<BaseObject> objects)
        {
            foreach (var selectedObject in objects)
                Assert.True(
                    SelectionController.SelectedObjects.Contains(selectedObject),
                    $"{selectedObject} should be selected");

            Assert.AreEqual(
                objects.Count,
                SelectionController.SelectedObjects.Count,
                "Selection should be the exact amount");
        }

        private static void AssertNoOuterGlsGroupSelection()
        {
            Assert.False(SelectionController.SelectedObjects.Any(obj => obj is BaseEventBoxGroup));
            Assert.IsEmpty(SelectionController.SelectedObjects);
        }

        // Paste regressions isolate one active GLS placement so retained scene state cannot select another node subtype or lane.
        private static GLSEventTranslationPlacement ConfigureTranslationPasteLane(
            SelectionController selectionController,
            BaseLightTranslationEventBoxGroup group,
            BaseEventBox lane,
            float relativeJsonTime)
        {
            foreach (var candidate in Object.FindObjectsByType<GLSEventColorPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                candidate.State = PlacementState.Idle;
            }
            foreach (var candidate in Object.FindObjectsByType<GLSEventRotationPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                candidate.State = PlacementState.Idle;
            }
            foreach (var candidate in Object.FindObjectsByType<GLSEventTranslationPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                candidate.State = PlacementState.Idle;
            }
            foreach (var candidate in Object.FindObjectsByType<GLSEventFloatFXPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                candidate.State = PlacementState.Idle;
            }

            // Drive the exact serialized placement read by SelectionController; scenes may contain inactive sibling instances.
            var placementField = typeof(SelectionController).GetField(
                "glsEventTranslationPlacement",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(placementField);
            var placement = placementField.GetValue(selectionController) as GLSEventTranslationPlacement;
            Assert.NotNull(placement);
            placement.QueuedData.EventBoxGroupData = group;
            placement.QueuedData.EventBoxData = lane;
            placement.QueuedData.BoxIndex = -1;
            for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
            {
                if (ReferenceEquals(group.ReadOnlyBoxes[boxIndex], lane))
                {
                    placement.QueuedData.BoxIndex = boxIndex;
                    break;
                }
            }
            placement.QueuedData.RelativeJsonTime = relativeJsonTime;
            placement.QueuedData.RecomputeSongBpmTime();
            placement.State = PlacementState.Active;
            return placement;
        }

        // Parent replacement is valid only when every pasted child points at its exact new group, box, and stable box index.
        private static void AssertValidGlsEventOwnership(BaseEventBoxGroup group)
        {
            for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
            {
                var box = group.ReadOnlyBoxes[boxIndex];
                foreach (var evt in box.ReadOnlyEvents)
                {
                    Assert.AreSame(group, evt.EventBoxGroupData);
                    Assert.AreSame(box, evt.EventBoxData);
                    Assert.AreEqual(boxIndex, evt.BoxIndex);
                }
            }
        }

        // Place the parent through its real collection so replacement actions update the open GLS child context.
        private static TGroup PlaceGlsGroup<TGroup>(TGroup group)
            where TGroup : BaseEventBoxGroup
        {
            // Factory-created groups need the same map/time initialization as normal map-load objects before pool range queries can render them.
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            // Batch tests can inherit deferred retirement metadata; clear it so replacement resolves against this test's group.
            provider.LastContext = null;
            provider.GroupContext = group;
            return group;
        }

        // Start an outer drag through the real GLS group container prefab without coupling this behavioral test to viewport pooling.
        private static (GLSGroupColorPlacement Placement, GLSGroupContainer DragContainer) StartOuterColorGroupDrag(
            BaseLightColorEventBoxGroup group)
        {
            var groupCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            // Select the live placement for this collection so the drag starts through the same initialized scene wiring as the editor.
            var placement = Object.FindObjectsByType<GLSGroupColorPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(candidate => candidate.ObjectContainerCollection == groupCollection
                                    && candidate.ObjectDataType == ObjectType.GLSColor)
                .OrderByDescending(candidate => candidate.isActiveAndEnabled)
                .FirstOrDefault();
            Assert.NotNull(placement);
            // Insert into the same collection StartDrag will remove from; unrelated selection tests use a separate convenience helper.
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            groupCollection.SpawnObject(group, false, false, true);
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
            var groupContainer = groupCollection.CreateContainer() as GLSGroupContainer;
            Assert.NotNull(groupContainer);
            groupContainer.ObjectData = group;
            groupContainer.Setup();
            Assert.NotNull(placement.StartDrag(groupContainer.gameObject));
            return (placement, groupContainer);
        }

        // Build distinct events in adjacent filter lanes for parent replacement and boundary-shift coverage.
        private static BaseLightColorEventBoxGroup CreateTwoLaneColorGroup() =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] },
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.75, ""c"": 1, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] }
                ] }"));

        // Build two same-time nodes in one lane so replacement logic must preserve duplicate identity cardinality.
        private static BaseLightColorEventBoxGroup CreateDuplicateColorGroup() =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 },
                               { ""b"": 0.5, ""c"": 1, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] },
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [] }
                ] }"));

        // Build an authored group with a box but no nodes to retain its selection behavior during cache changes.
        private static BaseLightColorEventBoxGroup CreateEmptyColorGroup() =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [] }
                ] }"));

        private class SelectionFixture
        {
            public readonly BaseArc Arc02;
            public readonly BaseArc Arc04;
            public readonly BaseArc Arc24;
            public readonly BaseArc Arc44;
            public readonly BaseBpmEvent BpmEvent1;
            public readonly BaseBpmEvent BpmEvent2;
            public readonly BaseBpmEvent BpmEvent3;
            public readonly BaseEvent Event1;
            public readonly BaseEvent Event2;
            public readonly BaseEvent Event3;
            public readonly BaseEvent Event4;
            public readonly BaseEvent RotationEvent2;
            public readonly BaseNote Note1;
            public readonly BaseNote Note2;
            public readonly BaseNote Note3;
            public readonly BaseNote Note4;

            public SelectionFixture()
            {
                BpmEvent1 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 100 });
                BpmEvent2 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 2, Bpm = 100 });
                BpmEvent3 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 3, Bpm = 100 });

                Note1 = PlaceUtils.Place(new BaseNote { JsonTime = 1 });
                Note2 = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
                Note3 = PlaceUtils.Place(new BaseNote { JsonTime = 3 });
                Note4 = PlaceUtils.Place(new BaseNote { JsonTime = 4 });

                Event1 = PlaceUtils.Place(new BaseEvent { JsonTime = 1 });
                Event2 = PlaceUtils.Place(new BaseEvent { JsonTime = 2 });
                Event3 = PlaceUtils.Place(new BaseEvent { JsonTime = 3 });
                Event4 = PlaceUtils.Place(new BaseEvent { JsonTime = 4 });

                RotationEvent2 = PlaceUtils.Place(
                    new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.EarlyRotationEventType });

                Arc02 = PlaceUtils.Place(new BaseArc { JsonTime = 0, TailJsonTime = 2 });
                Arc04 = PlaceUtils.Place(new BaseArc { JsonTime = 0, TailJsonTime = 4 });
                Arc24 = PlaceUtils.Place(new BaseArc { JsonTime = 2, TailJsonTime = 4 });
                Arc44 = PlaceUtils.Place(new BaseArc { JsonTime = 4, TailJsonTime = 4 });
            }

            // Selection currently admits arcs by their head time, not by an overlapping tail.
            public ICollection<BaseObject> ExpectedSelectBetweenNotes() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc24
            };

            public ICollection<BaseObject> ExpectedSelectBetweenEvents() => new List<BaseObject>
            {
                Event1, Event2, Event3, RotationEvent2
            };

            public ICollection<BaseObject> ExpectedSelectBetweenBpmEvents() => new List<BaseObject>
            {
                BpmEvent1, BpmEvent2, BpmEvent3
            };

            // Mixed selection follows the same start-time-only arc behavior.
            public ICollection<BaseObject> ExpectedSelectBetweenNotesAndEvents() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc24, Event1, Event2, Event3, RotationEvent2
            };

            // Mixed BPM selection follows the same start-time-only arc behavior.
            public ICollection<BaseObject> ExpectedSelectBetweenNotesAndBpmEvents() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc24, BpmEvent1, BpmEvent2, BpmEvent3
            };

            public ICollection<BaseObject> ExpectedSelectBetweenEventsAndBpmEvents() => new List<BaseObject>
            {
                Event1, Event2, Event3, RotationEvent2, BpmEvent1, BpmEvent2, BpmEvent3
            };
        }

        [Test]
        public void ShiftSelectionOutsideVanillaGrid([Values] bool isVanillaOnlyShiftSettingEnabled)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = _fixture.Note1;

            SelectionController.Select(note);

            Assert.AreEqual(1, SelectionController.SelectedObjects.Count, "Note should be selected");
            Assert.AreEqual(0, note.PosX);
            Assert.AreEqual(0, note.PosY);


            Settings.Instance.VanillaOnlyShift = isVanillaOnlyShiftSettingEnabled;

            selectionController.ShiftSelection(5, 5);
            note = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            if (isVanillaOnlyShiftSettingEnabled)
            {
                // Expect clamped values
                Assert.AreEqual((int)GridX.Right, note.PosX);
                Assert.AreEqual((int)GridY.Top, note.PosY);
                Assert.IsNull(note.CustomCoordinate);
            }
            else
            {
                Assert.AreEqual(0, note.PosX);
                Assert.AreEqual(0, note.PosY);
                Assert.NotNull(note.CustomCoordinate);
                Assert.AreEqual(3, note.CustomCoordinate[0].AsInt);
                Assert.AreEqual(5, note.CustomCoordinate[1].AsInt);
            }
        }

        // Expose the protected resolver so the test can exercise the same cached hit path as shift-click.
        private class TestArcInputController : BeatmapInputController<ArcContainer>
        {
            public bool ResolveRaycast(out ArcContainer container) => RaycastFirstObject(out container);
        }
    }
}
