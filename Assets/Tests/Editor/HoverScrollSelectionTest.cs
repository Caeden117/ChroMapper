using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class HoverScrollSelectionTest : TestBase
    {
        [Test]
        public void NoteDirectionScrollDoesNotModifySelectedConnectedArcs()
        {
            var first = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2, PosX = 1, PosY = 1, CutDirection = (int)NoteCutDirection.Down
            });
            var second = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 100, PosX = 1, PosY = 1, CutDirection = (int)NoteCutDirection.Down
            });
            var arc = PlaceUtils.Place(new BaseArc
            {
                JsonTime = 2, TailJsonTime = 4, PosX = 1, PosY = 1,
                CutDirection = (int)NoteCutDirection.Down
            });
            Select(first, second, arc);
            Object.FindAnyObjectByType<BeatmapNoteInputController>()
                .ScrollUpdateDirection(GetContainer<NoteContainer>(first), 1);
            var editedFirst = (BaseNote)GetObject(ObjectType.Note, 2);
            var editedSecond = (BaseNote)GetObject(ObjectType.Note, 100);
            Assert.AreNotEqual((int)NoteCutDirection.Down, editedFirst.CutDirection);
            Assert.AreEqual(editedFirst.CutDirection, editedSecond.CutDirection);
            Assert.AreEqual((int)NoteCutDirection.Down, arc.CutDirection);
            Assert.IsTrue(SelectionController.IsObjectSelected(arc));
        }

        [Test]
        public void RotationEventSelectionDoesNotChangeSelectedLights()
        {
            var first = PlaceUtils.Place(new BaseRotationEvent { JsonTime = 2, Rotation = 15 });
            var second = PlaceUtils.Place(new BaseRotationEvent { JsonTime = 100, Rotation = 30 });
            var light = PlaceUtils.Place(new BaseEvent { JsonTime = 3, Type = 0, FloatValue = 1 });
            Select(first, second, light);
            RotationCommand.ModifyHover(first, 1, 15);
            Assert.AreEqual(45, ((BaseRotationEvent)GetObject(ObjectType.RotationEvent, 100)).Rotation);
            Assert.AreEqual(1, light.FloatValue);
            Assert.IsTrue(SelectionController.IsObjectSelected(light));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ArcSelectionDoesNotChangeSelectedChains(bool tail)
        {
            var first = PlaceUtils.Place(new BaseArc { JsonTime = 2, TailJsonTime = 3 });
            var second = PlaceUtils.Place(new BaseArc { JsonTime = 100, TailJsonTime = 101 });
            var chain = PlaceUtils.Place(new BaseChain { JsonTime = 4, TailJsonTime = 5, Squish = 1 });
            var originalMu = tail
                ? second.TailControlPointLengthMultiplier
                : second.HeadControlPointLengthMultiplier;
            Select(first, second, chain);
            var controller = Object.FindAnyObjectByType<BeatmapArcInputController>();
            if (tail)
                controller.ChangeTmu(GetContainer<ArcContainer>(first), 0.5f);
            else
                controller.ChangeMu(GetContainer<ArcContainer>(first), 0.5f);

            var replacement = (BaseArc)GetObject(ObjectType.Arc, 100);
            var replacementMu = tail
                ? replacement.TailControlPointLengthMultiplier
                : replacement.HeadControlPointLengthMultiplier;
            Assert.AreEqual(originalMu + 0.5f, replacementMu);
            Assert.AreEqual(1, chain.Squish);
            Assert.IsTrue(SelectionController.IsObjectSelected(chain));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChainSelectionDoesNotChangeSelectedArcs(bool count)
        {
            var first = PlaceUtils.Place(new BaseChain { JsonTime = 2, TailJsonTime = 3, Squish = 1 });
            var second = PlaceUtils.Place(new BaseChain { JsonTime = 100, TailJsonTime = 101, Squish = 2, SliceCount = 5 });
            var arc = PlaceUtils.Place(new BaseArc { JsonTime = 4, TailJsonTime = 5 });
            var originalMu = arc.HeadControlPointLengthMultiplier;
            Select(first, second, arc);
            var controller = Object.FindAnyObjectByType<BeatmapChainInputController>();
            if (count)
            {
                controller.TweakValue(GetContainer<ChainContainer>(first), 2);
                Assert.AreEqual(7, ((BaseChain)GetObject(ObjectType.Chain, 100)).SliceCount);
            }
            else
            {
                controller.TweakChainSquish(GetContainer<ChainContainer>(first), 0.5f);
                Assert.AreEqual(2.5f, ((BaseChain)GetObject(ObjectType.Chain, 100)).Squish);
            }

            Assert.AreEqual(originalMu, arc.HeadControlPointLengthMultiplier);
            Assert.IsTrue(SelectionController.IsObjectSelected(arc));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BasicEventsBatchByMeaningAcrossLanes(bool ringRotation)
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.BasicEvent;
            var first = PlaceUtils.Place(new BaseEvent { JsonTime = 2, Type = 0, FloatValue = 1 });
            var second = PlaceUtils.Place(new BaseEvent { JsonTime = 100, Type = 1, FloatValue = 2 });
            var zoom = PlaceUtils.Place(new BaseEvent { JsonTime = 3, Type = 9, Value = 1 });
            var note = PlaceUtils.Place(new BaseNote { JsonTime = 4 });
            var container = GetContainer<EventContainer>(first);
            var definitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            var components = ringRotation
                ? BasicEventComponent.RingRotation
                : BasicEventComponent.None;
            definitions.Basic = new Dictionary<int, TrackDefinitionBasic>
            {
                [0] = new()
                {
                    Type = 0,
                    Kind = BasicEventKind.Lights,
                    Components = components
                },
                [1] = new()
                {
                    Type = 1,
                    Kind = BasicEventKind.Lights,
                    Components = components
                },
                [9] = new() { Type = 9, Components = BasicEventComponent.RingZoom }
            };
            var previousDefinitions = container.TrackDefinitions;
            try
            {
                container.TrackDefinitions = definitions;
                if (ringRotation)
                {
                    first.CustomRingRotation = 90;
                    second.CustomRingRotation = 180;
                    first.WriteCustom();
                    second.WriteCustom();
                }

                Select(first, second, zoom, note);
                var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
                Object.FindAnyObjectByType<BeatmapEventInputController>().TweakMain(container, 1);
                var replacement = (BaseEvent)GetObject(ObjectType.Event, 100);
                if (ringRotation)
                    Assert.That(replacement.CustomRingRotation, Is.EqualTo(180 + precision.GetCurrentRotationPrecision()).Within(0.001f));
                else
                    Assert.That(replacement.FloatValue, Is.EqualTo(2 + (precision.GetCurrentBrightnessPrecision() / 100f)).Within(0.001f));

                Assert.AreEqual(1, zoom.Value);
                Assert.IsTrue(SelectionController.IsObjectSelected(zoom));
                Assert.IsTrue(SelectionController.IsObjectSelected(note));
                Assert.AreEqual(4, SelectionController.SelectedObjects.Count);
                PlaceUtils.Undo();
                Assert.AreSame(second, GetObject(ObjectType.Event, 100));
                Assert.IsTrue(SelectionController.IsObjectSelected(first));
                Assert.IsTrue(SelectionController.IsObjectSelected(second));
            }
            finally
            {
                container.TrackDefinitions = previousDefinitions;
                Object.DestroyImmediate(definitions);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NoteAndBombSelectionsStaySeparate(bool hoverBomb)
        {
            var type = hoverBomb
                ? (int)NoteType.Bomb
                : (int)NoteType.Red;
            var otherType = hoverBomb
                ? (int)NoteType.Blue
                : (int)NoteType.Bomb;
            var first = PlaceUtils.Place(new BaseNote { JsonTime = 2, Type = type, AngleOffset = 10 });
            var second = PlaceUtils.Place(new BaseNote { JsonTime = 100, Type = type, AngleOffset = 20 });
            var other = PlaceUtils.Place(new BaseNote { JsonTime = 3, Type = otherType, AngleOffset = 30 });
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 4, Duration = 1 });
            Select(first, second, other, wall);
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>().GetCurrentAngleOffsetPrecision();
            Object.FindAnyObjectByType<BeatmapNoteInputController>()
                .ScrollPreciseUpdateDirection(GetContainer<NoteContainer>(first), 1);
            Assert.AreEqual(20 + precision, ((BaseNote)GetObject(ObjectType.Note, 100)).AngleOffset);
            Assert.AreEqual(30, other.AngleOffset);
            Assert.IsTrue(SelectionController.IsObjectSelected(other));
            Assert.IsTrue(SelectionController.IsObjectSelected(wall));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void WallSelectionIncludesOffscreenWallsAndKeepsOtherObjects(int property)
        {
            var first = PlaceUtils.Place(new BaseObstacle { JsonTime = 2, Duration = 1, Height = 2 });
            var second = PlaceUtils.Place(new BaseObstacle { JsonTime = 100, Duration = 2, Height = 2 });
            var note = PlaceUtils.Place(new BaseNote { JsonTime = 3 });
            Select(first, second, note);
            var controller = Object.FindAnyObjectByType<BeatmapObstacleInputController>();
            var container = GetContainer<ObstacleContainer>(first);
            switch (property)
            {
                case 0:
                    controller.TweakDuration(container, 0.5f);
                    Assert.AreEqual(2.5f, ((BaseObstacle)GetObject(ObjectType.Obstacle, 100)).Duration);
                    break;
                case 1:
                    controller.TweakLowerBound(container, 1);
                    Assert.AreEqual(1, ((BaseObstacle)GetObject(ObjectType.Obstacle, 100)).PosY);
                    break;
                default:
                    controller.TweakUpperBound(container, 1);
                    Assert.AreEqual(3, ((BaseObstacle)GetObject(ObjectType.Obstacle, 100)).Height);
                    break;
            }

            Assert.IsTrue(SelectionController.IsObjectSelected(note));
            PlaceUtils.Undo();
            Assert.AreEqual(1, ((BaseObstacle)GetObject(ObjectType.Obstacle, 2)).Duration);
            Assert.AreEqual(2, ((BaseObstacle)GetObject(ObjectType.Obstacle, 100)).Duration);
            Assert.AreEqual(0, ((BaseObstacle)GetObject(ObjectType.Obstacle, 100)).PosY);
            Assert.AreEqual(2, ((BaseObstacle)GetObject(ObjectType.Obstacle, 100)).Height);
        }

        [Test]
        public void UnselectedLightStaysSingleWhenOtherLightsAreSelected()
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.BasicEvent;
            var hovered = PlaceUtils.Place(new BaseEvent { JsonTime = 2, Type = 0, FloatValue = 1 });
            var selected = PlaceUtils.Place(new BaseEvent { JsonTime = 100, Type = 1, FloatValue = 2 });
            Select(selected);
            for (var tick = 0; tick < 2; tick++)
            {
                var target = (BaseEvent)GetObject(ObjectType.Event, 2);
                Object.FindAnyObjectByType<BeatmapEventInputController>().TweakMain(GetContainer<EventContainer>(target), 1);
                Assert.IsFalse(SelectionController.IsObjectSelected(GetObject(ObjectType.Event, 2)));
                Assert.IsTrue(SelectionController.IsObjectSelected(selected));
                Assert.AreEqual(1, SelectionController.SelectedObjects.Count);
            }

            Assert.AreEqual(2, selected.FloatValue);
            Assert.AreEqual(hovered.Type, ((BaseEvent)GetObject(ObjectType.Event, 2)).Type);
        }

        private static void Select(params BaseObject[] objects)
        {
            foreach (var obj in objects)
                SelectionController.Select(obj, true, false, false);
        }

        private static TContainer GetContainer<TContainer>(BaseObject obj) where TContainer : ObjectContainer
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);
            // The cursor cache advances in Update, so synchronous setup must establish the hover window explicitly.
            collection.RefreshPool(obj.SongBpmTime - 1f, obj.SongBpmTime + 1f, true);
            return (TContainer)collection.LoadedContainers[obj];
        }

        private static BaseObject GetObject(ObjectType type, float beat)
        {
            BaseObject result = null;
            var map = BeatSaberSongContainer.Instance.Map;
            var time = (float)map.JsonTimeToSongBpmTime(beat);
            BeatmapObjectContainerCollection.GetCollectionForType(type).ForEachObjectBetweenSongBpmTime(
                time - 0.001f,
                time + 0.001f,
                (_, obj) => result = obj);
            Assert.NotNull(result);
            return result;
        }
    }
}
