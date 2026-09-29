using System.Linq;
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
    // Hover tweaks replace their live node; every event binding must leave an unselected target unselected,
    // keep unrelated selections, and carry the selected state onto the replacement node.
    public class EventTweakSelectionTest : TestBase
    {
        private BeatmapEventInputController eventInputController;
        private BeatmapEventInputController EventInput =>
            eventInputController != null
                ? eventInputController
                : eventInputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

        // A selected node in an unrelated collection proves tweaks cannot collapse an existing selection.
        private static BaseNote PlaceUnrelatedSelectedNote()
        {
            var note = PlaceUtils.Place(new BaseNote { JsonTime = 1, Type = (int)NoteType.Red });
            SelectionController.Select(note, false, false);
            return note;
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

        private static EventContainer GetEventContainer(BaseEvent evt)
        {
            var events = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            Assert.True(events.LoadedContainers.ContainsKey(evt), "The placed event was not rendered.");
            return (EventContainer)events.LoadedContainers[evt];
        }

        [Test]
        public void BasicEventTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(
                new BaseEvent { JsonTime = 5, Type = (int)EventTypeValue.ColorBoostEventType });

            EventInput.TweakMain(GetEventContainer(target), 1);

            var replacement = RefreshObject<BaseEvent>(ObjectType.Event, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void BasicEventTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(
                new BaseEvent { JsonTime = 5, Type = (int)EventTypeValue.ColorBoostEventType });
            SelectionController.Select(target, true, false);

            EventInput.TweakMain(GetEventContainer(target), 1);

            var replacement = RefreshObject<BaseEvent>(ObjectType.Event, 5);
            Assert.AreNotSame(target, replacement);
            AssertSelected(unrelated, replacement);
        }

        [Test]
        public void BasicEventTweakUndoReselectsOriginalNode()
        {
            var target = PlaceUtils.Place(
                new BaseEvent { JsonTime = 5, Type = (int)EventTypeValue.ColorBoostEventType });
            SelectionController.Select(target, false, false);

            EventInput.TweakMain(GetEventContainer(target), 1);
            PlaceUtils.Undo();

            AssertSelected(target);
        }

        [Test]
        public void RotationEventTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(new BaseRotationEvent
            {
                JsonTime = 5, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 45
            });

            RotationCommand.ModifyHover(target, 1, 15f);

            var replacement = RefreshObject<BaseRotationEvent>(ObjectType.RotationEvent, 5);
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void RotationEventTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(new BaseRotationEvent
            {
                JsonTime = 5, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 45
            });
            SelectionController.Select(target, true, false);

            RotationCommand.ModifyHover(target, 1, 15f);

            var replacement = RefreshObject<BaseRotationEvent>(ObjectType.RotationEvent, 5);
            Assert.AreNotSame(target, replacement);
            AssertSelected(unrelated, replacement);
        }

        // In-place tweaks record the action unperformed, so the selection contract is exercised on undo and redo.
        [Test]
        public void BpmEventTweakUndoRedoLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 5, Bpm = 100 });
            var bpmGrid = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                ObjectType.BpmChange);
            var container = (BpmEventContainer)bpmGrid.LoadedContainers[target];

            BeatmapBPMChangeInputController.ChangeBpm(container, "222");
            PlaceUtils.Undo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(target));
            PlaceUtils.Redo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(target));
        }

        [Test]
        public void BpmEventTweakUndoKeepsSelectedNodeAndUnrelatedSelection()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 5, Bpm = 100 });
            SelectionController.Select(target, true, false);
            var bpmGrid = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                ObjectType.BpmChange);
            var container = (BpmEventContainer)bpmGrid.LoadedContainers[target];

            BeatmapBPMChangeInputController.ChangeBpm(container, "222");
            PlaceUtils.Undo();

            AssertSelected(unrelated, target);
        }

        [Test]
        public void VnjsEasingTweakUndoRedoLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(new BaseNJSEvent
            {
                JsonTime = 5, Easing = (int)EaseType.None, RelativeNJS = 5f
            });
            var njsGrid = BeatmapObjectContainerCollection.GetCollectionForType<NJSEventGridContainer>(
                ObjectType.NJSEvent);
            var container = (NJSEventContainer)njsGrid.LoadedContainers[target];

            VNJSEventCommand.SetEasing(container, (int)EaseType.OutQuadratic);
            PlaceUtils.Undo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(target));
            PlaceUtils.Redo();
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(target));
        }

        [Test]
        public void VnjsEasingTweakUndoKeepsSelectedNodeAndUnrelatedSelection()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var target = PlaceUtils.Place(new BaseNJSEvent
            {
                JsonTime = 5, Easing = (int)EaseType.None, RelativeNJS = 5f
            });
            SelectionController.Select(target, true, false);
            var njsGrid = BeatmapObjectContainerCollection.GetCollectionForType<NJSEventGridContainer>(
                ObjectType.NJSEvent);
            var container = (NJSEventContainer)njsGrid.LoadedContainers[target];

            VNJSEventCommand.SetEasing(container, (int)EaseType.OutQuadratic);
            PlaceUtils.Undo();

            AssertSelected(unrelated, target);
        }

        // GLS inner tweaks replace the whole owning group, so selection must rebind across the swap.
        private static (BaseLightColorEventBoxGroup group, BaseLightColorBase first, BaseLightColorBase second)
            PlaceOpenColorGroup()
        {
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 20, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                                 { ""b"": 0.75, ""c"": 1, ""s"": 1, ""i"": 0, ""f"": 1, ""sb"": 1, ""sf"": 0 } ] }
                ] }"));
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            // LastContext and markRemove only clear in LateUpdate, which does not run between synchronous
            // tests; reset the provider so a prior test's retired group cannot poison this fixture's swap.
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = null;
            provider.LastContext = null;
            provider.GroupContext = group;
            return (
                group,
                (BaseLightColorBase)group.ReadOnlyBoxes[0].ReadOnlyEvents[0],
                (BaseLightColorBase)group.ReadOnlyBoxes[0].ReadOnlyEvents[1]);
        }

        private static BaseLightColorEventBoxGroup GetOpenColorGroup() =>
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext as BaseLightColorEventBoxGroup;

        [Test]
        public void GlsInnerEventTweakLeavesUnselectedHoverTargetUnselected()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var (_, first, _) = PlaceOpenColorGroup();

            GLSEventColorCommand.SetBrightness(first, 0.5f);

            var replacement = (BaseLightColorBase)GetOpenColorGroup().ReadOnlyBoxes[0].ReadOnlyEvents[0];
            AssertSelected(unrelated);
            Assert.False(SelectionController.IsObjectSelected(replacement));
        }

        [Test]
        public void GlsInnerEventTweakReselectsReplacedSelectedNode()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var (_, first, second) = PlaceOpenColorGroup();
            SelectionController.Select(first, true, false);
            SelectionController.Select(second, true, false);

            GLSEventColorCommand.SetBrightness(first, 0.5f);

            var replacementGroup = GetOpenColorGroup();
            var newFirst = (BaseLightColorBase)replacementGroup.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            var newSecond = (BaseLightColorBase)replacementGroup.ReadOnlyBoxes[0].ReadOnlyEvents[1];
            Assert.AreNotSame(first, newFirst);
            AssertSelected(unrelated, newFirst, newSecond);
        }

        [Test]
        public void GlsInnerEventTweakKeepsSelectedOuterGroupSelected()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var (group, first, _) = PlaceOpenColorGroup();
            SelectionController.Select(group, true, false);

            GLSEventColorCommand.SetBrightness(first, 0.5f);

            var replacementGroup = GetOpenColorGroup();
            Assert.AreNotSame(group, replacementGroup);
            AssertSelected(unrelated, replacementGroup);
        }

        [Test]
        public void GlsInnerEventTweakUndoReselectsOriginalNode()
        {
            var (_, first, _) = PlaceOpenColorGroup();
            SelectionController.Select(first, false, false);

            GLSEventColorCommand.SetBrightness(first, 0.5f);
            PlaceUtils.Undo();

            var restored = (BaseLightColorBase)GetOpenColorGroup().ReadOnlyBoxes[0].ReadOnlyEvents[0];
            AssertSelected(restored);
        }

        // An axis move changes a node's stable lane identity, so the rebound selection must follow the moved node.
        [Test]
        public void GlsAxisCycleReselectsMovedNode()
        {
            var unrelated = PlaceUnrelatedSelectedNote();
            var group = BeatmapFactory.LightRotationEventBoxGroups(JSON.Parse(
                @"{ ""b"": 20, ""g"": 2, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""s"": 45, ""t"": 1, ""b"": 0, ""a"": 0, ""r"": 0, ""i"": 0,
                      ""l"": [ { ""b"": 0.5, ""r"": 45, ""o"": 1, ""e"": 0, ""l"": 0, ""p"": 0 },
                                 { ""b"": 0.75, ""r"": -45, ""o"": 1, ""e"": 0, ""l"": 0, ""p"": 0 } ] }
                ] }"));
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = null;
            provider.LastContext = null;
            provider.GroupContext = group;
            var movedSource = (BaseLightRotationBase)group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            var sibling = (BaseLightRotationBase)group.ReadOnlyBoxes[0].ReadOnlyEvents[1];
            SelectionController.Select(movedSource, true, false);
            SelectionController.Select(sibling, true, false);

            GLSCommonCommand.CycleTransformEventAxis(movedSource, 1);

            var replacementGroup =
                (BaseLightRotationEventBoxGroup)Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext;
            var movedReplacement = (BaseLightRotationBase)replacementGroup.ReadOnlyBoxes
                .Single(box => (int)box.GetAxis() == 1)
                .ReadOnlyEvents[0];
            var siblingReplacement = (BaseLightRotationBase)replacementGroup.ReadOnlyBoxes
                .Single(box => (int)box.GetAxis() == 0)
                .ReadOnlyEvents[0];
            AssertSelected(unrelated, movedReplacement, siblingReplacement);
        }
    }
}
