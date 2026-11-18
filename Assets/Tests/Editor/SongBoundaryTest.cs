using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // All non-GLS lanes use the same boundary matrix so a Basic Event-only fix cannot hide regressions in other objects.
    public class SongBoundaryTest : SongBoundaryTestBase
    {
        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;
        private BeatmapRuntimeContext runtimeContext;
        private CreateEventTypeLabels labels;
        private EventGridContainer eventCollection;

        // Include every serialized Basic Event number once, including boost, legacy BPM/rotation, and special events.
        private static IEnumerable<string> Lanes => new[]
        {
            "RedNote", "BlueNote", "Bomb", "Wall", "CrouchWall", "NegativeWall", "Arc", "Chain",
            "Bpm", "EarlyRotation", "LateRotation", "Njs", "AnimateTrack", "AssignPathAnimation",
            "AssignTrackParent", "AssignPlayerToTrack", "AnimateComponent", "CustomAnimation"
        }.Concat(Enum.GetValues(typeof(EventTypeValue)).Cast<EventTypeValue>().Select(value => (int)value)
            .Where(value => value >= 0).Distinct().Select(value => $"Basic{value}"));

        // Both a partly out-of-range selection and a wholly out-of-range selection must translate, never collapse or disappear.
        private static IEnumerable<TestCaseData> BoundaryCases =>
            from lane in Lanes
            from upper in new[] { false, true }
            from allOutside in new[] { false, true }
            select new TestCaseData(lane, upper, allOutside);

        // Only Basic Events support this hover-paste branch; filter the source rather than reporting other lanes as skipped.
        private static IEnumerable<TestCaseData> BasicBoundaryCases => BoundaryCases
            .Where(test => ((string)test.Arguments[0]).StartsWith("Basic", StringComparison.Ordinal));

        // Custom-event drag is intentionally unsupported; arcs and chains expose separate draggable head/tail indicators.
        private static IEnumerable<TestCaseData> DragCases =>
            from lane in Lanes.Where(lane => !IsAnimation(lane))
            from upper in new[] { false, true }
            from indicator in lane is "Arc" or "Chain"
                ? new[] { IndicatorType.Head, IndicatorType.Tail }
                : new[] { IndicatorType.Head }
            select new TestCaseData(lane, upper, indicator);

        // Expose missing environment-specific basic lanes through test-owned definitions without changing shared assets.
        [OneTimeSetUp]
        public void SetUpAllBasicLanes()
        {
            runtimeContext = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            eventCollection = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            eventCollection.PropagationEditing = EventGridContainer.PropMode.Off;
            originalTracks = runtimeContext.TrackDefinitions;
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Basic = new Dictionary<int, TrackDefinitionBasic>(originalTracks.Basic);
            testTracks.Gls = new Dictionary<int, TrackDefinitionGLS>(originalTracks.Gls);
            foreach (var lane in Lanes.Where(lane => lane.StartsWith("Basic", StringComparison.Ordinal)))
            {
                var type = int.Parse(lane.Substring(5));
                if (!testTracks.Basic.ContainsKey(type))
                    testTracks.Basic.Add(type, new TrackDefinitionBasic { Type = type, Name = lane });
            }
            runtimeContext.TrackDefinitions = testTracks;
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
        }

        // Restore lane definitions once after this fixture's many cases rather than rebuilding them after
        // every case.
        [OneTimeTearDown]
        public void RestoreAllBasicLanes()
        {
            runtimeContext.TrackDefinitions = originalTracks;
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Object.DestroyImmediate(testTracks);
        }

        // ShiftSelectionClampsWholeRange proves Shift+Arrow adjusts one shared delta, preserving spacing and endpoint lengths.
        [TestCaseSource(nameof(BoundaryCases))]
        public void ShiftSelectionClampsWholeRange(string lane, bool upper, bool allOutside)
        {
            SetLaneMode(lane);
            var gap = allOutside ? 0.25f : 1.5f;
            var extent = Extent(lane);
            var firstBeat = upper ? FinalBeat - gap - extent - 0.125f : 0.125f;
            var source = new[] { Spawn(Create(lane, firstBeat)), Spawn(Create(lane, firstBeat + gap)) };
            var snapshots = source.Select(BeatmapFactory.Clone).ToArray();
            var untouched = Spawn(Create(lane, 6f));
            var untouchedJson = untouched.ToString();
            Select(source);

            ShiftWithKeyboard(upper);

            var expectedDelta = upper ? 0.125f : -0.125f;
            var moved = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            AssertTranslated(snapshots, moved, expectedDelta);
            AssertAtBoundary(moved, upper);
            Assert.That(untouched.ToString(), Is.EqualTo(untouchedJson));
            AssertUndoRedo(snapshots, moved, expectedDelta, untouched);
        }

        // Off-grid clipboard contents can straddle either bound; paste must correct the whole range before spawning conflicts.
        [TestCaseSource(nameof(BoundaryCases))]
        public void PasteSelectionClampsWholeRange(string lane, bool upper, bool allOutside)
        {
            SetLaneMode(lane);
            var source = new[] { Spawn(Create(lane, 4f)), Spawn(Create(lane, 5.5f)) };
            var snapshots = source.Select(BeatmapFactory.Clone).ToArray();
            Select(source);
            CopyWithKeyboard();
            Atsc.MoveToJsonTime(upper ? FinalBeat : 0f);
            var clipboard = SelectionController.CopiedObjects.OrderBy(Earliest).ToArray();
            var span = clipboard.Max(Latest) - clipboard.Min(Earliest);
            var desiredEarliest = upper
                ? allOutside ? 0.125f : -span + 0.125f
                : allOutside ? -span - 0.125f : -0.125f;
            Translate(clipboard, desiredEarliest - clipboard.Min(Earliest));
            var clipboardJson = clipboard.Select(obj => obj.ToString()).ToArray();

            PasteWithKeyboard();

            var pasted = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            var expectedFirst = upper ? FinalBeat - span : 0f;
            AssertTranslated(snapshots, pasted, expectedFirst - snapshots.Min(Earliest));
            AssertAtBoundary(pasted, upper);
            Assert.That(clipboard.Select(obj => obj.ToString()).ToArray(), Is.EqualTo(clipboardJson), "Paste must not mutate the clipboard.");
            AssertTranslated(snapshots, source, 0f);
            PlaceUtils.Undo().ToArray();
            // This NUnit version only exposes string Not.Contain; test authoritative membership explicitly after undo.
            foreach (var obj in pasted)
                Assert.That(CurrentObjects(obj.ObjectType).Contains(obj), Is.False, "One undo must remove the whole paste.");
            foreach (var obj in source)
                Assert.That(CurrentObjects(obj.ObjectType), Does.Contain(obj), "Undo must leave every source intact.");
            PlaceUtils.Redo().ToArray();
            AssertTranslated(snapshots, pasted, expectedFirst - snapshots.Min(Earliest));
            foreach (var obj in pasted)
                Assert.That(CurrentObjects(obj.ObjectType), Does.Contain(obj));
        }

        // AltDragClampsAtSongBoundary exercises each placement's actual hit/transfer/release path, including wall back ends.
        [TestCaseSource(nameof(DragCases))]
        public void AltDragClampsAtSongBoundary(string lane, bool upper, IndicatorType indicator)
        {
            SetLaneMode(lane);
            var source = Create(lane, 4f);
            if (source is BaseSlider slider)
            {
                slider.JsonTime = upper ? FinalBeat - 0.5f : 0f;
                slider.TailJsonTime = upper ? FinalBeat : 0.5f;
            }
            Spawn(source);
            var original = BeatmapFactory.Clone(source);
            var untouched = Spawn(Create(lane, 8f));
            var untouchedJson = untouched.ToString();
            var target = upper ? FinalBeat + 2f : -2f;
            var placement = FindPlacement(PlacementType(lane));

            DragToBeat(placement, source, target, indicator);

            var draggedTime = source is BaseSlider changed && indicator == IndicatorType.Tail
                ? changed.TailJsonTime
                : source.JsonTime;
            var expectedTime = upper ? FinalBeat : 0f;
            if (source is BaseObstacle obstacle)
                expectedTime -= upper ? Mathf.Max(0f, obstacle.Duration) : Mathf.Min(0f, obstacle.Duration);
            Assert.That(draggedTime, Is.EqualTo(expectedTime).Within(0.0001f), "The dragged endpoint must stop at the song boundary.");
            AssertAtBoundary(new[] { source }, upper);
            Assert.That(untouched.ToString(), Is.EqualTo(untouchedJson));
            var result = BeatmapFactory.Clone(source);
            PlaceUtils.Undo().ToArray();
            Assert.That(CurrentObjects(source.ObjectType).Any(obj => obj.ToString() == original.ToString()), Is.True,
                "One undo must restore the pre-drag object.");
            PlaceUtils.Redo().ToArray();
            Assert.That(CurrentObjects(source.ObjectType).Any(obj => obj.ToString() == result.ToString()), Is.True,
                "One redo must restore the clamped drag.");
        }

        // Hovered Basic Event paste already has a separate clamp; retain it for every event subtype as the generic path is fixed.
        [TestCaseSource(nameof(BasicBoundaryCases))]
        public void HoverPasteClampsWholeBasicEventRange(string lane, bool upper, bool allOutside)
        {
            SetLaneMode(lane);
            var source = new[] { Spawn(Create(lane, 4f)), Spawn(Create(lane, 5.5f)) };
            Select(source);
            CopyWithKeyboard();
            var placement = GetField<EventPlacement>(Selection, "eventPlacement");
            placement.QueuedData = (BaseEvent)BeatmapFactory.Clone(source[0]);
            placement.QueuedData.JsonTime = upper
                ? allOutside ? FinalBeat + 2f : FinalBeat - 0.5f
                : allOutside ? -2f : -0.5f;
            placement.State = PlacementState.Active;

            PasteWithKeyboard();

            var pasted = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            AssertTranslated(source, pasted, (upper ? FinalBeat - 1.5f : 0f) - 4f);
            AssertAtBoundary(pasted, upper);
        }

        // A mixed gameplay selection must share one correction; clamping separately per lane changes its timing relationship.
        [TestCase(false)]
        [TestCase(true)]
        public void MixedGameplaySelectionUsesOneBoundaryCorrection(bool paste)
        {
            SetMode(EditingMode.Gameplay);
            var source = new[]
            {
                Spawn(Create("Njs", FinalBeat - 1.5f)),
                Spawn(Create("RedNote", FinalBeat - 1f)),
                Spawn(Create("Wall", FinalBeat - 0.625f)),
                Spawn(Create("AnimateTrack", FinalBeat - 0.25f))
            };
            var snapshots = source.Select(BeatmapFactory.Clone).ToArray();
            Select(source);
            if (paste)
            {
                CopyWithKeyboard();
                Atsc.MoveToJsonTime(FinalBeat);
                PasteWithKeyboard();
            }
            else
                ShiftWithKeyboard(true);
            var moved = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            AssertTranslated(snapshots, moved, 0.125f);
            AssertAtBoundary(moved, true);
        }

        // The upper limit is an unsnapped JSON beat after BPM changes, not clip seconds or the preceding grid line.
        [TestCase(false)]
        [TestCase(true)]
        public void SongEndUsesBpmConvertedUnsnappedBeat(bool paste)
        {
            // Rebuild the map's timing cache after inserting a BPM change so this case tests the true audio-end conversion.
            Spawn(new BaseBpmEvent { JsonTime = 2.25f, Bpm = 137f });
            BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                .RefreshModifiedBeat();
            var final = FinalBeat;
            var source = new[] { Spawn(Create("RedNote", final - 1.625f)), Spawn(Create("Bomb", final - 0.125f)) };
            var snapshots = source.Select(BeatmapFactory.Clone).ToArray();
            Select(source);
            if (paste)
            {
                CopyWithKeyboard();
                Atsc.MoveToJsonTime(final);
                PasteWithKeyboard();
            }
            else
                ShiftWithKeyboard(true);
            var moved = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            AssertTranslated(snapshots, moved, 0.125f);
            Assert.That(moved.Max(Latest), Is.EqualTo(final).Within(0.0001f));
        }

        // Moving a faster BPM past audio end removes its old contribution to song length, so the boundary moves
        // with the payload: Shift+Arrow rejects the out-of-range offset atomically. A drag instead clamps inside
        // BasePlacement.StartDrag against the song end computed after the dragged event is removed, a bound that
        // does not move with the payload and stays well-defined.
        [TestCase(false)]
        [TestCase(true)]
        public void MovingBpmPastSongEndRejectsShiftAndClampsDrag(bool drag)
        {
            var constantTempoEnd = FinalBeat;
            var bpm = Spawn(new BaseBpmEvent
            {
                JsonTime = constantTempoEnd - 0.125f,
                Bpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute * 2f
            });
            Assert.That(FinalBeat, Is.GreaterThan(constantTempoEnd));
            Select(new BaseObject[] { bpm });
            if (drag)
                DragToBeat(FindPlacement(typeof(BPMChangePlacement)), bpm, FinalBeat + 2f);
            else
                ShiftWithKeyboard(true);
            var moved = CurrentObjects(ObjectType.BpmChange).OfType<BaseBpmEvent>().Single(evt => evt.Bpm == bpm.Bpm);
            Assert.That(moved.JsonTime, Is.EqualTo(drag ? constantTempoEnd : constantTempoEnd - 0.125f).Within(0.0001f));
            Assert.That(moved.JsonTime, Is.LessThanOrEqualTo(FinalBeat + 0.0001f));
        }

        // A slower pasted BPM changes the clip's end beat. Reject the paste if its notes would no longer fit
        // under that tempo map.
        [Test]
        public void PastingBpmAndNotePastResultingSongEndIsRejected()
        {
            var baseBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var bpm = Spawn(new BaseBpmEvent { JsonTime = 4f, Bpm = baseBpm / 2f });
            var note = Spawn(Create("RedNote", 5f));
            Spawn(new BaseBpmEvent { JsonTime = 6f, Bpm = baseBpm * 2f });
            Select(new BaseObject[] { bpm, note });
            CopyWithKeyboard();
            Atsc.MoveToJsonTime(FinalBeat);

            PasteWithKeyboard();

            Assert.That(CurrentObjects(ObjectType.BpmChange), Has.Length.EqualTo(2), "A rejected paste must not add BPM events.");
            Assert.That(CurrentObjects(ObjectType.Note), Has.Length.EqualTo(1), "A rejected paste must not add notes.");
            Assert.That(SelectionController.SelectedObjects, Is.EquivalentTo(new BaseObject[] { bpm, note }),
                "A rejected paste must leave the source selection untouched.");
        }

        [Test]
        public void UnorderedTempoProjectionRejectsOutOfRangeOffset()
        {
            var baseBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var earlierBpm = Spawn(new BaseBpmEvent { JsonTime = 2f, Bpm = baseBpm * 2f });
            var laterBpm = Spawn(new BaseBpmEvent { JsonTime = 4f, Bpm = baseBpm / 2f });
            var note = Spawn(Create("RedNote", 5f));
            var objects = new HashSet<BaseObject> { laterBpm, earlierBpm, note };
            var songEndAtBaseTempo = Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length);
            var offset = songEndAtBaseTempo;

            var result = CommonBeatmapUtils.TryClampOffsetWhenMovingBpmEvents(
                objects,
                Atsc,
                false,
                false,
                ref offset,
                out var changesTempo);

            Assert.That(result, Is.False);
            Assert.That(changesTempo, Is.True);
            Assert.That(offset, Is.EqualTo(songEndAtBaseTempo).Within(0.0001f), "A rejected offset must be returned unmodified.");
        }

        // Boundary correction must not affect an ordinary in-range edit or collapse a valid fractional selection gap.
        [TestCaseSource(nameof(Lanes))]
        public void InRangeShiftAndPastePreserveTiming(string lane)
        {
            SetLaneMode(lane);
            var source = new[] { Spawn(Create(lane, 4.125f)), Spawn(Create(lane, 5.625f)) };
            var snapshots = source.Select(BeatmapFactory.Clone).ToArray();
            Select(source);
            ShiftWithKeyboard(true);
            var shifted = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            AssertTranslated(snapshots, shifted, 1f);
            CopyWithKeyboard();
            Atsc.MoveToJsonTime(10f);
            PasteWithKeyboard();
            var pasted = SelectionController.SelectedObjects.OrderBy(Earliest).ToArray();
            AssertTranslated(snapshots, pasted, 10f - snapshots.Min(obj => obj.JsonTime));
        }

        // A boundary case needs the corresponding editor tab so Copy/Paste uses its normal supported object filter.
        private void SetLaneMode(string lane) => SetMode(lane.StartsWith("Basic", StringComparison.Ordinal)
            ? EditingMode.BasicEvent : EditingMode.Gameplay);

        // Animation lanes are extensible strings; cover the built-in families and an unknown animation event with the same contract.
        private static bool IsAnimation(string lane) => lane is "AnimateTrack" or "AssignPathAnimation"
            or "AssignTrackParent" or "AssignPlayerToTrack" or "AnimateComponent" or "CustomAnimation";

        // Nonzero durations distinguish real endpoint protection from a head-only clamp, including backwards authored walls.
        private static float Extent(string lane) => lane is "Wall" or "CrouchWall" or "NegativeWall" or "Arc" or "Chain" ? 0.5f : 0f;

        // Create distinct payloads through shared beatmap types, leaving animation duration unchanged because the selected node is its trigger.
        private static BaseObject Create(string lane, float start)
        {
            if (lane.StartsWith("Basic", StringComparison.Ordinal))
                return new BaseEvent { JsonTime = start, Type = int.Parse(lane.Substring(5)), Value = 1, FloatValue = 1f };
            if (IsAnimation(lane))
                return new BaseCustomEvent { JsonTime = start, Type = lane, Data = new JSONObject { ["duration"] = 2f } };
            return lane switch
            {
                "RedNote" => new BaseNote { JsonTime = start, Color = 0, PosX = 1, CutDirection = 1 },
                "BlueNote" => new BaseNote { JsonTime = start, Color = 1, PosX = 2, CutDirection = 0 },
                "Bomb" => new BaseNote { JsonTime = start, Type = (int)NoteType.Bomb, PosX = 1 },
                "Wall" => new BaseObstacle { JsonTime = start, Duration = 0.5f, Width = 1, Height = 5 },
                "CrouchWall" => new BaseObstacle { JsonTime = start, Duration = 0.5f, Width = 4, Type = (int)ObstacleType.Crouch },
                "NegativeWall" => new BaseObstacle { JsonTime = start + 0.5f, Duration = -0.5f, Width = 1, Height = 5 },
                "Arc" => new BaseArc { JsonTime = start, TailJsonTime = start + 0.5f, PosX = 1, TailPosX = 2 },
                "Chain" => new BaseChain { JsonTime = start, TailJsonTime = start + 0.5f, PosX = 1, TailPosX = 2, SliceCount = 3 },
                "Bpm" => new BaseBpmEvent { JsonTime = start, Bpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute },
                "EarlyRotation" => new BaseRotationEvent { JsonTime = start, Type = 0, Rotation = 15 },
                "LateRotation" => new BaseRotationEvent { JsonTime = start, Type = 1, Rotation = -15 },
                "Njs" => new BaseNJSEvent { JsonTime = start, RelativeNJS = 2f, Easing = 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(lane))
            };
        }

        // Exercise each actual time-transfer override; arc/chain bodies do not implement time dragging, their indicators do.
        private static Type PlacementType(string lane)
        {
            if (lane.StartsWith("Basic", StringComparison.Ordinal))
                return typeof(EventPlacement);
            return lane switch
            {
                "RedNote" or "BlueNote" => typeof(NotePlacement),
                "Bomb" => typeof(BombPlacement),
                "Wall" or "CrouchWall" or "NegativeWall" => typeof(ObstaclePlacement),
                "Arc" => typeof(ArcIndicatorPlacement),
                "Chain" => typeof(ChainIndicatorPlacement),
                "Bpm" => typeof(BPMChangePlacement),
                "EarlyRotation" or "LateRotation" => typeof(RotationEventPlacement),
                "Njs" => typeof(NJSEventPlacement),
                _ => throw new ArgumentOutOfRangeException(nameof(lane))
            };
        }

        // Tests derive expected extents independently of any production clamp implementation.
        private static float Earliest(BaseObject obj) => obj switch
        {
            BaseObstacle wall => Mathf.Min(wall.JsonTime, wall.JsonTime + wall.Duration),
            BaseSlider slider => Mathf.Min(slider.JsonTime, slider.TailJsonTime),
            _ => obj.JsonTime
        };
        private static float Latest(BaseObject obj) => obj switch
        {
            BaseObstacle wall => Mathf.Max(wall.JsonTime, wall.JsonTime + wall.Duration),
            BaseSlider slider => Mathf.Max(slider.JsonTime, slider.TailJsonTime),
            _ => obj.JsonTime
        };

        // A clipboard can contain external offsets; translate its endpoints together without altering the source objects.
        private static void Translate(IEnumerable<BaseObject> objects, float delta)
        {
            foreach (var obj in objects)
            {
                obj.JsonTime += delta;
                if (obj is BaseSlider slider)
                    slider.TailJsonTime += delta;
            }
        }

        // Selection actions are not the operation under test, so isolate a single undoable shift or paste.
        private static void Select(IEnumerable<BaseObject> objects)
        {
            SelectionController.DeselectAll();
            foreach (var obj in objects)
                SelectionController.Select(obj, true, false, false);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        // Assert every node's delta and payload, not merely the extreme, to catch per-object clamps that destroy spacing.
        private static void AssertTranslated(BaseObject[] before, BaseObject[] after, float delta)
        {
            Assert.That(after, Has.Length.EqualTo(before.Length), "The whole selection must survive the boundary operation.");
            for (var i = 0; i < before.Length; i++)
            {
                Assert.That(after[i].JsonTime, Is.EqualTo(before[i].JsonTime + delta).Within(0.0001f),
                    $"Object {i} must receive the same boundary-corrected delta.");
                var restored = BeatmapFactory.Clone(after[i]);
                restored.JsonTime = before[i].JsonTime;
                if (before[i] is BaseSlider originalSlider && restored is BaseSlider restoredSlider)
                {
                    Assert.That(restoredSlider.TailJsonTime, Is.EqualTo(originalSlider.TailJsonTime + delta).Within(0.0001f));
                    restoredSlider.TailJsonTime = originalSlider.TailJsonTime;
                }
                Assert.That(restored.ToString(), Is.EqualTo(before[i].ToString()), "Boundary movement must preserve authored payloads.");
                Assert.That(CurrentObjects(after[i].ObjectType), Does.Contain(after[i]), "The result must be authoritative map data.");
            }
        }

        // Endpoint comparisons cover the whole authored interval and require the maximally violating endpoint to reach the limit.
        private void AssertAtBoundary(BaseObject[] objects, bool upper)
        {
            Assert.That(objects.Min(Earliest), Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(objects.Max(Latest), Is.LessThanOrEqualTo(FinalBeat + 0.0001f));
            Assert.That(upper ? objects.Max(Latest) : objects.Min(Earliest),
                Is.EqualTo(upper ? FinalBeat : 0f).Within(0.0001f));
        }

        // Query data, not loaded visuals, so objects near the song end remain verifiable outside the pooling window.
        private static BaseObject[] CurrentObjects(ObjectType type)
        {
            var result = new List<BaseObject>();
            BeatmapObjectContainerCollection.GetCollectionForType(type).ForEachObjectBetweenSongBpmTime(
                float.NegativeInfinity, float.PositiveInfinity, (_, obj) => result.Add(obj));
            return result.ToArray();
        }

        // A selection-wide correction must remain one undo step and redo every lane without touching unrelated map data.
        private static void AssertUndoRedo(BaseObject[] snapshots, BaseObject[] moved, float delta, BaseObject untouched)
        {
            PlaceUtils.Undo().ToArray();
            foreach (var snapshot in snapshots)
                Assert.That(CurrentObjects(snapshot.ObjectType).Any(obj => obj.ToString() == snapshot.ToString()), Is.True);
            Assert.That(CurrentObjects(untouched.ObjectType), Does.Contain(untouched));
            PlaceUtils.Redo().ToArray();
            AssertTranslated(snapshots, moved, delta);
        }
    }
}
