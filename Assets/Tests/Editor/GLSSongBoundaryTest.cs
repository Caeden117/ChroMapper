using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSSongBoundaryTest : SongBoundaryTestBase
    {
        public enum GlsKind { Color, Rotation, Translation, FloatFX }
        public enum PasteOverflow { BeforeSome, BeforeAll, AfterSome, AfterAll }
        public enum LowerShift { SomeBeforeGroup, AllBeforeGroup, LandsAtGroupStart }
        public enum InnerPasteStart { NoHover, NoHoverEndingAtSongEnd, HoverAtStart, HoverBeforeStart }
        public enum OuterDragTarget { BeforeSong, ChildrenBeyondEnd, EntireGroupBeyondEnd }

        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider groupProvider;
        private GLSEventGridProvider innerProvider;
        private BeatmapActionContainer actions;
        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;
        private string originalPage;

        // Paste rejects unavailable GLS tracks, so each test installs lanes for all four GLS types.
        [SetUp]
        public void ConfigureGlsTracks()
        {
            runtime = GetField<BeatmapRuntimeContext>(Selection, "beatmapRuntimeContext");
            groupProvider = GetField<GLSGroupGridProvider>(Selection, "glsGroupGridProvider");
            innerProvider = GetField<GLSEventGridProvider>(Selection, "glsEventGridProvider");
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            originalTracks = runtime.TrackDefinitions;
            originalPage = groupProvider.CurrentGroup;
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Copy(originalTracks);
            SetField(testTracks, "glsEntries", Enumerable.Range(1, 3).Select(id => new TrackDefinitionGLS
            {
                ID = id,
                Name = "Boundary lane " + id,
                Group = "Song boundary tests",
                ColorTrack = true,
                RotationTracks = new[] { true, true, true },
                TranslationTracks = new[] { true, true, true },
                FloatFXTrack = true
            }).ToList());
            testTracks.Initialize();
            runtime.TrackDefinitions = testTracks;
            runtime.NotifyTrackDefinitions();
            groupProvider.SetGroupPage("Song boundary tests");
            Assert.That(FinalBeat, Is.GreaterThan(32f), "The shared test song must fit the untouched source groups.");
        }

        [TearDown]
        public void RestoreGlsTracks()
        {
            if (runtime != null && originalTracks != null)
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
                groupProvider.SetGroupPage(originalPage);
            }
            if (testTracks != null)
            {
                Object.DestroyImmediate(testTracks);
            }
        }

        // Put the latest child in the earlier group to catch implementations that clamp only group start
        // beats.
        [Test]
        public void ShiftOuterGroupsTranslatesWholeSelectionToSongBoundary(
            [Values] GlsKind kind, [Values] bool forward)
        {
            SetMode(EditingMode.GLS);
            var firstBeat = forward ? FinalBeat - 5.5f : 0.25f;
            var secondBeat = firstBeat + 2.75f;
            var firstOffsets = new[] { new[] { 0f }, new[] { 4.75f } };
            var secondOffsets = new[] { new[] { 0f, 0.5f } };
            var first = PlaceGroup(kind, 1, firstBeat, firstOffsets);
            var second = PlaceGroup(kind, 2, secondBeat, secondOffsets);
            var untouched = PlaceGuard(kind);
            SelectOnly(first, second);

            ShiftWithKeyboard(forward);

            var delta = forward ? 0.75f : -0.25f;
            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(2));
            AssertRoundTrip(() => AssertState(delta), () => AssertState(0f));

            void AssertState(float appliedDelta)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(3));
                AssertGroup(Group(kind, 1), kind, firstBeat + appliedDelta, firstOffsets);
                AssertGroup(Group(kind, 2), kind, secondBeat + appliedDelta, secondOffsets);
                Assert.That(Group(kind, 2).JsonTime - Group(kind, 1).JsonTime, Is.EqualTo(2.75f).Within(0.0001f));
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // A later unselected sibling must not reduce the translation available to the selected inner-node range.
        [Test]
        public void ShiftInnerNodesTranslatesSelectionToSongEndWithoutMovingParent([Values] GlsKind kind)
        {
            SetMode(EditingMode.EventBox);
            var groupBeat = FinalBeat - 8f;
            var original = new[] { new[] { 4.5f, 7.5f }, new[] { 7.75f } };
            var moved = new[] { new[] { 5f, 8f }, new[] { 7.75f } };
            var group = PlaceGroup(kind, 1, groupBeat, original);
            var untouched = PlaceGuard(kind);
            OpenGroup(group);
            SelectOnly(group.ReadOnlyBoxes[0].ReadOnlyEvents.Cast<BaseObject>().ToArray());

            ShiftWithKeyboard(true);

            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(2));
            AssertRoundTrip(() => AssertState(moved), () => AssertState(original));

            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertInnerGroup(kind, 1, groupBeat, offsets);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Reject the whole shift if any node would move before its parent. Landing exactly at the parent beat
        // is valid.
        [Test]
        public void ShiftInnerNodesRetainsExistingGroupStartRestriction(
            [Values] GlsKind kind, [Values] LowerShift scenario)
        {
            SetMode(EditingMode.EventBox);
            var offsets = scenario == LowerShift.LandsAtGroupStart
                ? new[] { 1f, 3f }
                : new[] { 0.25f, scenario == LowerShift.AllBeforeGroup ? 0.5f : 3.25f };
            var original = new[] { offsets, new[] { 6f } };
            var group = PlaceGroup(kind, 1, 8f, original);
            var untouched = PlaceGuard(kind);
            OpenGroup(group);
            var selected = group.ReadOnlyBoxes[0].ReadOnlyEvents.Cast<BaseObject>().ToArray();
            SelectOnly(selected);

            ShiftWithKeyboard(false);

            if (scenario == LowerShift.LandsAtGroupStart)
            {
                AssertRoundTrip(
                    () => AssertState(new[] { new[] { 0f, 2f }, new[] { 6f } }),
                    () => AssertState(original));
            }
            else
            {
                AssertState(original);
                Assert.That(Group(kind, 1), Is.SameAs(group), "Rejection must not replace or rebase the parent.");
                CollectionAssert.AreEquivalent(selected, SelectionController.SelectedObjects);
                AssertNoAction();
                AssertState(original);
            }

            void AssertState(float[][] expected)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertInnerGroup(kind, 1, 8f, expected);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Copy first, then offset its relative clipboard beats to reach both negative-song scenarios without a negative cursor.
        [Test]
        public void PasteOuterGroupsTranslatesCompleteChildExtentAndPreservesClipboard(
            [Values] GlsKind kind, [Values] PasteOverflow overflow, [Values] bool hovered)
        {
            SetMode(EditingMode.GLS);
            var firstOffsets = new[] { new[] { 0f }, new[] { 4f } };
            var secondOffsets = new[] { new[] { 0f, 0.5f } };
            var first = PlaceGroup(kind, 1, 8f, firstOffsets);
            var second = PlaceGroup(kind, 2, 10f, secondOffsets);
            var untouched = PlaceGuard(kind);
            SelectOnly(first, second);
            CopyWithKeyboard();
            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(2));
            var atEnd = overflow == PasteOverflow.AfterSome || overflow == PasteOverflow.AfterAll;
            var allOutside = overflow == PasteOverflow.BeforeAll || overflow == PasteOverflow.AfterAll;
            var clipboardOffset = atEnd ? (allOutside ? 2f : 0f) : (allOutside ? -8f : -1f);
            foreach (var copied in SelectionController.CopiedObjects.Cast<BaseEventBoxGroup>())
            {
                copied.JsonTime += clipboardOffset;
                copied.RecomputeSongBpmTime();
            }
            var clipboard = ClipboardSnapshot();
            var anchor = atEnd ? (allOutside ? FinalBeat : FinalBeat - 3f) : 0f;
            Atsc.MoveToJsonTime(hovered ? 8f : anchor);
            ConfigureOuterPaste(kind, hovered, anchor);
            var attemptedNodes = SelectionController.CopiedObjects.Cast<BaseEventBoxGroup>()
                .SelectMany(g => g.ReadOnlyBoxes.SelectMany(b => b.ReadOnlyEvents)
                    .Select(e => anchor + g.JsonTime + e.RelativeJsonTime)).ToArray();
            AssertOutsideScenario(attemptedNodes, atEnd, allOutside);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(2));
            var expectedFirstBeat = atEnd ? FinalBeat - 4f : 0f;
            AssertRoundTrip(() => AssertState(true), () => AssertState(false));

            void AssertState(bool pasted)
            {
                var groups = Groups(kind);
                Assert.That(groups.Length, Is.EqualTo(pasted ? 5 : 3));
                Assert.That(groups.Any(g => ReferenceEquals(g, first)), Is.True);
                Assert.That(groups.Any(g => ReferenceEquals(g, second)), Is.True);
                AssertGroup(first, kind, 8f, firstOffsets);
                AssertGroup(second, kind, 10f, secondOffsets);
                if (pasted)
                {
                    var copies = groups.Where(g => !ReferenceEquals(g, first)
                        && !ReferenceEquals(g, second) && !ReferenceEquals(g, untouched)).ToArray();
                    Assert.That(copies.Length, Is.EqualTo(2));
                    AssertGroup(copies.Single(g => g.ID == 1), kind, expectedFirstBeat, firstOffsets);
                    AssertGroup(copies.Single(g => g.ID == 2), kind, expectedFirstBeat + 2f, secondOffsets);
                }
                AssertGuard(kind, untouched);
                CollectionAssert.AreEqual(clipboard, ClipboardSnapshot());
                AssertSelectedOwnership();
            }
        }

        [Test]
        public void PasteInnerNodesTranslatesCompleteSelectionToSongEndWithoutRebasing(
            [Values] GlsKind kind, [Values] bool allOutside)
        {
            SetMode(EditingMode.EventBox);
            var sourceOffsets = new[] { new[] { 0f, 3f }, new[] { 6f } };
            var source = PlaceGroup(kind, 1, 8f, sourceOffsets);
            var groupBeat = FinalBeat - 8f;
            var original = new[] { Array.Empty<float>(), new[] { 0.5f } };
            var target = PlaceGroup(kind, 2, groupBeat, original);
            var untouched = PlaceGuard(kind);
            CopyInnerNodes(source);
            var clipboard = ClipboardSnapshot();
            OpenGroup(target);
            Atsc.MoveToJsonTime(8f);
            var relativeAnchor = allOutside ? 10f : 7f;
            ConfigureInnerPaste(kind, target, true, relativeAnchor);
            AssertOutsideScenario(new[] { groupBeat + relativeAnchor, groupBeat + relativeAnchor + 3f }, true, allOutside);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            AssertRoundTrip(
                () => AssertState(new[] { new[] { 5f, 8f }, new[] { 0.5f } }),
                () => AssertState(original));

            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(3));
                AssertInnerGroup(kind, 2, groupBeat, offsets, true);
                Assert.That(Group(kind, 1), Is.SameAs(source));
                AssertGroup(source, kind, 8f, sourceOffsets);
                AssertGuard(kind, untouched);
                CollectionAssert.AreEqual(clipboard, ClipboardSnapshot());
                AssertSelectedOwnership();
            }
        }

        [Test]
        public void PasteInnerNodesRetainsNonnegativeGroupStartBehavior(
            [Values] GlsKind kind, [Values] InnerPasteStart scenario)
        {
            SetMode(EditingMode.EventBox);
            var sourceOffsets = new[] { new[] { 0f, 3f }, new[] { 6f } };
            var source = PlaceGroup(kind, 1, 2f, sourceOffsets);
            var original = new[] { Array.Empty<float>(), new[] { 0.5f } };
            var groupBeat = scenario == InnerPasteStart.NoHoverEndingAtSongEnd ? FinalBeat - 3f : 12f;
            var target = PlaceGroup(kind, 2, groupBeat, original);
            var untouched = PlaceGuard(kind);
            CopyInnerNodes(source);
            var clipboard = ClipboardSnapshot();
            OpenGroup(target);
            Atsc.MoveToJsonTime(FinalBeat);
            var hovered = scenario == InnerPasteStart.HoverAtStart || scenario == InnerPasteStart.HoverBeforeStart;
            ConfigureInnerPaste(kind, target, hovered, scenario == InnerPasteStart.HoverBeforeStart ? -1f : 0f);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            var lastOffset = scenario == InnerPasteStart.HoverBeforeStart ? 2f : 3f;
            AssertRoundTrip(
                () => AssertState(new[] { new[] { 0f, lastOffset }, new[] { 0.5f } }),
                () => AssertState(original));

            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(3));
                AssertInnerGroup(kind, 2, groupBeat, offsets, true);
                Assert.That(Group(kind, 1), Is.SameAs(source));
                AssertGroup(source, kind, 2f, sourceOffsets);
                AssertGuard(kind, untouched);
                CollectionAssert.AreEqual(clipboard, ClipboardSnapshot());
                AssertSelectedOwnership();
            }
        }

        [Test]
        public void AltLeftDragOuterGroupUsesMaximumChildOffsetAtSongBoundary(
            [Values] GlsKind kind, [Values] OuterDragTarget destination)
        {
            SetMode(EditingMode.GLS);
            var offsets = new[] { new[] { 0f, 1f }, new[] { 4.75f } };
            var group = PlaceGroup(kind, 1, 8f, offsets);
            var untouched = PlaceGuard(kind);
            SelectOnly(group);
            var target = destination == OuterDragTarget.BeforeSong
                ? -4f
                : destination == OuterDragTarget.ChildrenBeyondEnd ? FinalBeat - 1f : FinalBeat + 4f;
            var expected = destination == OuterDragTarget.BeforeSong ? 0f : FinalBeat - 4.75f;

            // Initialize outer placement with its track provider. Passing null leaves it without a valid GLS
            // lane.
            var placement = FindPlacement(OuterPlacement(kind).GetType());
            var provider = groupProvider.IdToTracks[group.ID].GetComponent<PlacementProvider>();
            Assert.That(provider, Is.Not.Null);
            placement.Initialize(provider);
            DragToBeat(placement, group, target);

            AssertRoundTrip(() => AssertState(expected), () => AssertState(8f));

            void AssertState(float beat)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertGroup(Group(kind, 1), kind, beat, offsets);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        [Test]
        public void AltLeftDragInnerNodeRetainsParentAndLowerLimitBehavior(
            [Values] GlsKind kind, [Values] bool atEnd)
        {
            SetMode(EditingMode.EventBox);
            var original = new[] { new[] { 1f }, new[] { 3f } };
            var group = PlaceGroup(kind, 1, 8f, original);
            var untouched = PlaceGuard(kind);
            OpenGroup(group);
            // Drag works without selection. Clear it so this test isolates the dragged node's restoration.
            SelectOnly();

            DragToBeat(FindPlacement(InnerPlacement(kind).GetType()), group.ReadOnlyBoxes[0].ReadOnlyEvents[0],
                atEnd ? FinalBeat + 4f : 7f);

            if (atEnd)
            {
                AssertRoundTrip(
                    () => AssertState(new[] { new[] { FinalBeat - 8f }, new[] { 3f } }),
                    () => AssertState(original));
            }
            else
            {
                AssertState(original);
                AssertNoAction();
                AssertState(original);
            }

            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertInnerGroup(kind, 1, 8f, offsets);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Select directly so selection changes do not add undo actions.
        private static void SelectOnly(params BaseObject[] objects)
        {
            SelectionController.DeselectAll();
            foreach (var obj in objects)
            {
                SelectionController.Select(obj, true, false, false);
            }
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        // Clear the previous context so deferred replacement state cannot override the group opened by this
        // test.
        private void OpenGroup(BaseEventBoxGroup group)
        {
            innerProvider.LastContext = null;
            innerProvider.GroupContext = group;
        }

        private void CopyInnerNodes(BaseEventBoxGroup source)
        {
            OpenGroup(source);
            SelectOnly(source.ReadOnlyBoxes[0].ReadOnlyEvents.Cast<BaseObject>().ToArray());
            CopyWithKeyboard();
            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(2));
            SelectionController.DeselectAll();
        }

        // SelectionController reads serialized placements, which need not be the first inactive sibling found in the scene.
        private BasePlacement OuterPlacement(GlsKind kind) => GetField<BasePlacement>(Selection, kind switch
        {
            GlsKind.Color => "glsGroupColorPlacement",
            GlsKind.Rotation => "glsGroupRotationPlacement",
            GlsKind.Translation => "glsGroupTranslationPlacement",
            GlsKind.FloatFX => "glsGroupFloatFXPlacement",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

        private BasePlacement InnerPlacement(GlsKind kind) => GetField<BasePlacement>(Selection, kind switch
        {
            GlsKind.Color => "glsEventColorPlacement",
            GlsKind.Rotation => "glsEventRotationPlacement",
            GlsKind.Translation => "glsEventTranslationPlacement",
            GlsKind.FloatFX => "glsEventFloatFXPlacement",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

        private void ConfigureOuterPaste(GlsKind kind, bool hovered, float beat)
        {
            var placement = OuterPlacement(kind);
            var queued = GetField<BaseEventBoxGroup>(placement, "QueuedData");
            queued.ID = 1;
            queued.JsonTime = beat;
            queued.SetMap(BeatSaberSongContainer.Instance.Map);
            queued.RecomputeSongBpmTime();
            placement.State = hovered ? PlacementState.Active : PlacementState.Idle;
        }

        // Hovered paste uses a relative beat. Idle paste anchors at the group start.
        private void ConfigureInnerPaste(GlsKind kind, BaseEventBoxGroup group, bool hovered, float relativeBeat)
        {
            var placement = InnerPlacement(kind);
            var queued = GetField<BaseGLSEvent>(placement, "QueuedData");
            queued.EventBoxGroupData = group;
            queued.EventBoxData = group.ReadOnlyBoxes[0];
            queued.BoxIndex = 0;
            queued.RelativeJsonTime = relativeBeat;
            queued.SetMap(BeatSaberSongContainer.Instance.Map);
            queued.RecomputeSongBpmTime();
            placement.State = hovered ? PlacementState.Active : PlacementState.Idle;
        }

        private BaseEventBoxGroup PlaceGroup(GlsKind kind, int id, float beat, params float[][] offsets)
        {
            BaseEventBoxGroup group = kind switch
            {
                GlsKind.Color => new BaseLightColorEventBoxGroup(),
                GlsKind.Rotation => new BaseLightRotationEventBoxGroup(),
                GlsKind.Translation => new BaseLightTranslationEventBoxGroup(),
                GlsKind.FloatFX => new BaseVfxEventEventBoxGroup(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            group.JsonTime = beat;
            group.ID = id;
            for (var lane = 0; lane < offsets.Length; lane++)
            {
                BaseEventBox box;
                switch (group)
                {
                    case BaseLightColorEventBoxGroup color:
                        var colorBox = new BaseLightColorEventBox();
                        color.Boxes.Add(colorBox);
                        box = colorBox;
                        break;
                    case BaseLightRotationEventBoxGroup rotation:
                        var rotationBox = new BaseLightRotationEventBox { Axis = lane };
                        rotation.Boxes.Add(rotationBox);
                        box = rotationBox;
                        break;
                    case BaseLightTranslationEventBoxGroup translation:
                        var translationBox = new BaseLightTranslationEventBox { Axis = lane };
                        translation.Boxes.Add(translationBox);
                        box = translationBox;
                        break;
                    case BaseVfxEventEventBoxGroup floatFx:
                        var floatBox = new BaseVfxEventEventBox();
                        floatFx.Boxes.Add(floatBox);
                        box = floatBox;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind));
                }
                var nodes = offsets[lane].Select((offset, index) => CreateNode(kind, offset, lane * 10 + index + 1)).ToArray();
                box.SetEvents(nodes);
            }
            // Normalize node conflicts before spawning the group, as the map loader does.
            switch (group)
            {
                case BaseLightColorEventBoxGroup color:
                    color.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightRotationEventBoxGroup rotation:
                    rotation.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightTranslationEventBoxGroup translation:
                    translation.NormalizeLoadedEventConflicts();
                    break;
                case BaseVfxEventEventBoxGroup floatFx:
                    floatFx.NormalizeLoadedEventConflicts();
                    break;
            }
            return Spawn(group);
        }

        // Distinct payloads expose dropped, reordered, or accidentally overwritten nodes even if their final beat range looks right.
        private static BaseGLSEvent CreateNode(GlsKind kind, float offset, float marker) => kind switch
        {
            GlsKind.Color => new BaseLightColorBase { RelativeJsonTime = offset, Brightness = marker },
            GlsKind.Rotation => new BaseLightRotationBase { RelativeJsonTime = offset, Rotation = marker },
            GlsKind.Translation => new BaseLightTranslationBase { RelativeJsonTime = offset, Translation = marker },
            GlsKind.FloatFX => new BaseFxEventFloat { RelativeJsonTime = offset, Value = marker },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        // Keep an unselected parent and child well away from every boundary operation to catch accidental whole-map translation.
        private BaseEventBoxGroup PlaceGuard(GlsKind kind) => PlaceGroup(kind, 3, FinalBeat / 2f, new[] { 0.75f });

        // LoadedObjects exposes backing collection data in this API, not just currently visible/pool-resident containers.
        private static BaseEventBoxGroup[] Groups(GlsKind kind) => BeatmapObjectContainerCollection.GetCollectionForType(kind switch
        {
            GlsKind.Color => ObjectType.GLSColor,
            GlsKind.Rotation => ObjectType.GLSRotation,
            GlsKind.Translation => ObjectType.GLSTranslation,
            GlsKind.FloatFX => ObjectType.GLSFloatFx,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }).LoadedObjects.Cast<BaseEventBoxGroup>().ToArray();

        private static BaseEventBoxGroup Group(GlsKind kind, int id) => Groups(kind).Single(g => g.ID == id);

        private static void AssertGroup(BaseEventBoxGroup group, GlsKind kind, float beat, float[][] offsets)
        {
            Assert.That(group.JsonTime, Is.EqualTo(beat).Within(0.0001f));
            Assert.That(group.ReadOnlyBoxes.Count, Is.EqualTo(offsets.Length));
            for (var lane = 0; lane < offsets.Length; lane++)
            {
                var box = group.ReadOnlyBoxes[lane];
                Assert.That(box.ReadOnlyEvents.Count, Is.EqualTo(offsets[lane].Length));
                for (var index = 0; index < offsets[lane].Length; index++)
                {
                    var node = box.ReadOnlyEvents[index];
                    Assert.That(node.GetType(), Is.EqualTo(CreateNode(kind, 0f, 0f).GetType()));
                    Assert.That(node.RelativeJsonTime, Is.EqualTo(offsets[lane][index]).Within(0.0001f));
                    Assert.That(node.JsonTime, Is.EqualTo(beat + offsets[lane][index]).Within(0.0001f));
                    Assert.That(node.RelativeJsonTime, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(node.EventBoxGroupData, Is.SameAs(group));
                    Assert.That(node.EventBoxData, Is.SameAs(box));
                    Assert.That(node.BoxIndex, Is.EqualTo(lane));
                    var marker = node switch
                    {
                        BaseLightColorBase color => color.Brightness,
                        BaseLightRotationBase rotation => rotation.Rotation,
                        BaseLightTranslationBase translation => translation.Translation,
                        BaseFxEventFloat floatFx => floatFx.Value,
                        _ => throw new ArgumentOutOfRangeException(nameof(kind))
                    };
                    Assert.That(marker, Is.EqualTo(lane * 10 + index + 1));
                }
            }
        }

        // Paste selects the replacement group. Other inner edits keep the edited children selected.
        private void AssertInnerGroup(GlsKind kind, int id, float beat, float[][] offsets, bool isPaste = false)
        {
            var group = Group(kind, id);
            Assert.That(innerProvider.GroupContext, Is.SameAs(group));
            AssertGroup(group, kind, beat, offsets);
            if (!isPaste)
            {
                Assert.That(SelectionController.SelectedObjects.OfType<BaseEventBoxGroup>(), Is.Empty);
            }
        }

        private void AssertGuard(GlsKind kind, BaseEventBoxGroup original)
        {
            Assert.That(Group(kind, 3), Is.SameAs(original));
            AssertGroup(original, kind, FinalBeat / 2f, new[] { new[] { 0.75f } });
            Assert.That(SelectionController.SelectedObjects.Contains(original), Is.False);
        }

        private static void AssertSelectedOwnership()
        {
            foreach (var selected in SelectionController.SelectedObjects)
            {
                var owner = selected is BaseGLSEvent node ? node.EventBoxGroupData : selected as BaseEventBoxGroup;
                Assert.That(owner, Is.Not.Null);
                var stored = BeatmapObjectContainerCollection.GetCollectionForType(owner.ObjectType).LoadedObjects;
                Assert.That(stored.Any(obj => ReferenceEquals(obj, owner)), Is.True);
                if (selected is BaseGLSEvent child)
                {
                    Assert.That(child.EventBoxData, Is.SameAs(owner.ReadOnlyBoxes[child.BoxIndex]));
                    Assert.That(child.EventBoxData.ReadOnlyEvents.Any(evt => ReferenceEquals(evt, child)), Is.True);
                }
            }
        }

        private void AssertOutsideScenario(float[] attempted, bool atEnd, bool allOutside)
        {
            var count = attempted.Count(beat => atEnd ? beat > FinalBeat : beat < 0f);
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(count == attempted.Length, Is.EqualTo(allOutside));
        }

        // Clipboard objects are mutable: snapshot both computed absolute beats and serialized data to detect paste-side mutation.
        private static string[] ClipboardSnapshot() => SelectionController.CopiedObjects
            .Select(obj => obj.JsonTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + obj.ToJson())
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        // Undo and redo must each apply the whole edit in one action.
        private void AssertRoundTrip(Action assertApplied, Action assertOriginal)
        {
            assertApplied();
            var action = actions.Undo();
            Assert.That(action, Is.Not.Null);
            assertOriginal();
            Assert.That(actions.Undo(), Is.Null, "The edit must be one atomic action.");
            Assert.That(actions.Redo(), Is.SameAs(action));
            assertApplied();
            Assert.That(actions.Redo(), Is.Null, "No extra edits may remain on the redo stack.");
        }

        private void AssertNoAction()
        {
            Assert.That(actions.Undo(), Is.Null);
            Assert.That(actions.Redo(), Is.Null);
        }
    }
}
