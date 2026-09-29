using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // Hover tweaks on note, bomb, arc, chain, and obstacle nodes must satisfy the same selection contract as
    // event tweaks: an unselected hover target stays unselected, unrelated selections survive, and a selected
    // target's replacement inherits the selection.
    public class ObjectTweakSelectionTest : TestBase
    {
        private BeatmapNoteInputController noteInputController;
        private BeatmapObstacleInputController obstacleInputController;
        private BeatmapArcInputController arcInputController;
        private BeatmapChainInputController chainInputController;

        private BeatmapNoteInputController NoteInput =>
            noteInputController != null
                ? noteInputController
                : noteInputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

        private BeatmapObstacleInputController ObstacleInput =>
            obstacleInputController != null
                ? obstacleInputController
                : obstacleInputController = Object.FindAnyObjectByType<BeatmapObstacleInputController>();

        private BeatmapArcInputController ArcInput =>
            arcInputController != null
                ? arcInputController
                : arcInputController = Object.FindAnyObjectByType<BeatmapArcInputController>();

        private BeatmapChainInputController ChainInput =>
            chainInputController != null
                ? chainInputController
                : chainInputController = Object.FindAnyObjectByType<BeatmapChainInputController>();

        // A selected node in an unrelated collection proves tweaks cannot collapse an existing selection.
        private static BaseEvent PlaceUnrelatedSelectedEvent()
        {
            var evt = PlaceUtils.Place(
                new BaseEvent { JsonTime = 1, Type = (int)EventTypeValue.ColorBoostEventType });
            SelectionController.Select(evt, false, false);
            return evt;
        }

        private static void AssertSelected(params BaseObject[] expected)
        {
            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(expected.Length));
            foreach (var obj in expected)
            {
                Assert.True(
                    SelectionController.IsObjectSelected(obj),
                    $"Expected {obj.GetType().Name} at {obj.JsonTime} to be selected.");
            }
        }

        private static T RefreshObject<T>(ObjectType type, float jsonTime) where T : BaseObject =>
            (T)BeatmapObjectContainerCollection
                .GetCollectionForType(type)
                .LoadedObjects
                .First(obj => Mathf.Abs(obj.JsonTime - jsonTime) < 0.001f);

        private static T GetContainer<T>(ObjectType type, BaseObject data) where T : ObjectContainer =>
            (T)BeatmapObjectContainerCollection.GetCollectionForType(type).LoadedContainers[data];

        private static NoteContainer PlaceNoteContainer(
            float jsonTime = 5, int type = (int)NoteType.Red)
        {
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = jsonTime,
                Type = type,
                CutDirection = (int)NoteCutDirection.Down
            });
            return GetContainer<NoteContainer>(ObjectType.Note, note);
        }

        [Test]
        public void NoteCoarseScrollTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer();

            NoteInput.ScrollUpdateDirection(container, 1);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void NoteCoarseScrollTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer();
            var original = container.NoteData;
            SelectionController.Select(original, true, false);

            NoteInput.ScrollUpdateDirection(container, 1);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            Assert.AreNotSame(original, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void NoteDirectionKeyTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer();

            NoteCommand.SetCutDirection(container.NoteData, (int)NoteCutDirection.Up);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void NoteDirectionKeyTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer();
            var original = container.NoteData;
            SelectionController.Select(original, true, false);

            NoteCommand.SetCutDirection(original, (int)NoteCutDirection.Up);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            Assert.AreNotSame(original, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void NoteInvertColorTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer();

            NoteCommand.InvertColor(container.NoteData);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void NoteInvertColorTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer();
            var original = container.NoteData;
            SelectionController.Select(original, true, false);

            NoteCommand.InvertColor(original);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            Assert.AreNotSame(original, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void BombScrollTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer(type: (int)NoteType.Bomb);

            NoteInput.ScrollUpdateDirection(container, 1);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void BombScrollTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer(type: (int)NoteType.Bomb);
            var original = container.NoteData;
            SelectionController.Select(original, true, false);

            NoteInput.ScrollUpdateDirection(container, 1);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            Assert.AreNotSame(original, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void BombDirectionKeyTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer(type: (int)NoteType.Bomb);

            NoteCommand.SetCutDirection(container.NoteData, (int)NoteCutDirection.Up);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void BombPreciseScrollTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer(type: (int)NoteType.Bomb);

            NoteInput.ScrollPreciseUpdateDirection(container, 1);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void BombDirectionKeyTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var container = PlaceNoteContainer(type: (int)NoteType.Bomb);
            var original = container.NoteData;
            SelectionController.Select(original, true, false);

            NoteCommand.SetCutDirection(original, (int)NoteCutDirection.Up);

            var replacement = RefreshObject<BaseNote>(ObjectType.Note, 5);
            Assert.AreNotSame(original, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void ArcMuTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var arc = PlaceUtils.Place(new BaseArc { JsonTime = 5, TailJsonTime = 7 });
            var container = GetContainer<ArcContainer>(ObjectType.Arc, arc);

            ArcInput.ChangeMu(container, 0.5f);

            var replacement = RefreshObject<BaseArc>(ObjectType.Arc, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void ArcMuTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var arc = PlaceUtils.Place(new BaseArc { JsonTime = 5, TailJsonTime = 7 });
            SelectionController.Select(arc, true, false);
            var container = GetContainer<ArcContainer>(ObjectType.Arc, arc);

            ArcInput.ChangeMu(container, 0.5f);

            var replacement = RefreshObject<BaseArc>(ObjectType.Arc, 5);
            Assert.AreNotSame(arc, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void ArcTailMuTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var arc = PlaceUtils.Place(new BaseArc { JsonTime = 5, TailJsonTime = 7 });
            var container = GetContainer<ArcContainer>(ObjectType.Arc, arc);

            ArcInput.ChangeTmu(container, 0.5f);

            var replacement = RefreshObject<BaseArc>(ObjectType.Arc, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void ArcTailMuTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var arc = PlaceUtils.Place(new BaseArc { JsonTime = 5, TailJsonTime = 7 });
            SelectionController.Select(arc, true, false);
            var container = GetContainer<ArcContainer>(ObjectType.Arc, arc);

            ArcInput.ChangeTmu(container, 0.5f);

            var replacement = RefreshObject<BaseArc>(ObjectType.Arc, 5);
            Assert.AreNotSame(arc, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void ArcInvertColorTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var arc = PlaceUtils.Place(new BaseArc { JsonTime = 5, TailJsonTime = 7 });

            SliderCommand.InvertColor(arc);

            var replacement = RefreshObject<BaseArc>(ObjectType.Arc, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void ChainSliceCountTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var chain = PlaceUtils.Place(new BaseChain { JsonTime = 5, TailJsonTime = 6 });
            var container = GetContainer<ChainContainer>(ObjectType.Chain, chain);

            ChainInput.TweakValue(container, 1);

            var replacement = RefreshObject<BaseChain>(ObjectType.Chain, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void ChainSliceCountTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var chain = PlaceUtils.Place(new BaseChain { JsonTime = 5, TailJsonTime = 6 });
            SelectionController.Select(chain, true, false);
            var container = GetContainer<ChainContainer>(ObjectType.Chain, chain);

            ChainInput.TweakValue(container, 1);

            var replacement = RefreshObject<BaseChain>(ObjectType.Chain, 5);
            Assert.AreNotSame(chain, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void ChainInvertColorTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var chain = PlaceUtils.Place(new BaseChain { JsonTime = 5, TailJsonTime = 6 });

            SliderCommand.InvertColor(chain);

            var replacement = RefreshObject<BaseChain>(ObjectType.Chain, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void ChainSquishTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var chain = PlaceUtils.Place(new BaseChain { JsonTime = 5, TailJsonTime = 6 });
            SelectionController.Select(chain, true, false);
            var container = GetContainer<ChainContainer>(ObjectType.Chain, chain);

            ChainInput.TweakChainSquish(container, 0.5f);

            var replacement = RefreshObject<BaseChain>(ObjectType.Chain, 5);
            Assert.AreNotSame(chain, replacement);
            AssertSelected(unrelated, replacement);
        }

        // In-place obstacle tweaks record the action unperformed, so the selection contract is exercised on
        // undo and redo rather than at tweak time.
        [Test]
        public void ObstacleDurationTweakUndoRedoLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 5, Duration = 1 });
            var container = GetContainer<ObstacleContainer>(ObjectType.Obstacle, wall);

            ObstacleInput.TweakDuration(container, 0.25f);
            PlaceUtils.Undo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(wall));
            PlaceUtils.Redo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(wall));
        }

        [Test]
        public void ObstacleBoundTweakUndoRedoLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 5, Duration = 1, Height = 2 });
            var container = GetContainer<ObstacleContainer>(ObjectType.Obstacle, wall);

            ObstacleInput.TweakLowerBound(container, 1);
            ObstacleInput.TweakUpperBound(container, -1);
            PlaceUtils.Undo();
            PlaceUtils.Undo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(wall));
            PlaceUtils.Redo();
            PlaceUtils.Redo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(wall));
        }

        [Test]
        public void ObstacleBoundTweakUndoKeepsSelectedNodeAndUnrelatedSelection()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 5, Duration = 1, Height = 2 });
            SelectionController.Select(wall, true, false);
            var container = GetContainer<ObstacleContainer>(ObjectType.Obstacle, wall);

            ObstacleInput.TweakLowerBound(container, 1);
            PlaceUtils.Undo();

            AssertSelected(unrelated, wall);
        }

        // ToggleHyperWall performs a real replacement, so an unselected hover target must stay unselected
        // at tweak time while a selected wall's hyper replacement inherits the selection.
        [Test]
        public void ObstacleHyperWallTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 5, Duration = 1 });
            var container = GetContainer<ObstacleContainer>(ObjectType.Obstacle, wall);

            ObstacleInput.ToggleHyperWall(container);

            var replacement = RefreshObject<BaseObstacle>(ObjectType.Obstacle, 6);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void ObstacleHyperWallTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedEvent();
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 5, Duration = 1 });
            SelectionController.Select(wall, true, false);
            var container = GetContainer<ObstacleContainer>(ObjectType.Obstacle, wall);

            ObstacleInput.ToggleHyperWall(container);

            var replacement = RefreshObject<BaseObstacle>(ObjectType.Obstacle, 6);
            Assert.AreNotSame(wall, replacement);
            AssertSelected(unrelated, replacement);
        }
    }
}
