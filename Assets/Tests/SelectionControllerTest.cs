using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class SelectionControllerTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        BaseNote baseNote1, baseNote2, baseNote3, baseNote4;
        BaseArc baseArc02, baseArc04, baseArc24, baseArc44;
        BaseEvent baseEvent1, baseEvent2, baseEvent3, baseEvent4;
        BaseRotationEvent baseRotationEvent2;
        BaseBpmEvent baseBpmEvent1, baseBpmEvent2, baseBpmEvent3;

        [SetUp]
        public void PlaceObjects()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var bpmEventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);

            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();
            var arcPlacement = root.GetComponentInChildren<ArcPlacement>();

            baseBpmEvent1 = new BaseBpmEvent { JsonTime = 1, Bpm = 100 };
            baseBpmEvent2 = new BaseBpmEvent { JsonTime = 2, Bpm = 100 };
            baseBpmEvent3 = new BaseBpmEvent { JsonTime = 3, Bpm = 100 };
            bpmEventsContainer.SpawnObject(baseBpmEvent1);
            bpmEventsContainer.SpawnObject(baseBpmEvent2);
            bpmEventsContainer.SpawnObject(baseBpmEvent3);

            baseNote1 = new V3ColorNote { JsonTime = 1 };
            baseNote2 = new V3ColorNote { JsonTime = 2 };
            baseNote3 = new V3ColorNote { JsonTime = 3 };
            baseNote4 = new V3ColorNote { JsonTime = 4 };
            PlaceUtils.PlaceNote(notePlacement, baseNote1);
            PlaceUtils.PlaceNote(notePlacement, baseNote2);
            PlaceUtils.PlaceNote(notePlacement, baseNote3);
            PlaceUtils.PlaceNote(notePlacement, baseNote4);

            baseEvent1 = new V3BasicEvent { JsonTime = 1 };
            baseEvent2 = new V3BasicEvent { JsonTime = 2 };
            baseEvent3 = new V3BasicEvent { JsonTime = 3 };
            baseEvent4 = new V3BasicEvent { JsonTime = 4 };
            PlaceUtils.PlaceEvent(eventPlacement, baseEvent1);
            PlaceUtils.PlaceEvent(eventPlacement, baseEvent2);
            PlaceUtils.PlaceEvent(eventPlacement, baseEvent3);
            PlaceUtils.PlaceEvent(eventPlacement, baseEvent4);

            baseRotationEvent2 = new V3RotationEvent { JsonTime = 2 };
            PlaceUtils.PlaceEvent(eventPlacement, baseRotationEvent2);

            baseArc02 = new V3Arc { JsonTime = 0, TailJsonTime = 2 };
            baseArc04 = new V3Arc { JsonTime = 0, TailJsonTime = 4 };
            baseArc24 = new V3Arc { JsonTime = 2, TailJsonTime = 4 };
            baseArc44 = new V3Arc { JsonTime = 4, TailJsonTime = 4 };
            PlaceUtils.PlaceArc(arcPlacement, baseArc02);
            PlaceUtils.PlaceArc(arcPlacement, baseArc04);
            PlaceUtils.PlaceArc(arcPlacement, baseArc24);
            PlaceUtils.PlaceArc(arcPlacement, baseArc44);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            SelectionController.DeselectAll();
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
            CleanupUtils.CleanupEvents();
            CleanupUtils.CleanupArcs();
            CleanupUtils.CleanupBPMChanges();
        }

        [Test]
        public void SelectBetweenNotes()
        {
            SelectionController.SelectBetween(baseNote1, baseNote3);
            AssertSelectedObjects(new List<BaseObject>
            {
                baseNote1, baseNote2, baseNote3,
                baseArc02, baseArc04, baseArc24,
                baseRotationEvent2,
            });
        }

        [Test]
        public void SelectBetweenEvents()
        {
            SelectionController.SelectBetween(baseEvent1, baseEvent3);
            AssertSelectedObjects(new List<BaseObject>{
                baseEvent1, baseEvent2, baseEvent3,
                baseRotationEvent2,
            });
        }

        [Test]
        public void SelectBetweenBpmEvents()
        {
            SelectionController.SelectBetween(baseBpmEvent1, baseBpmEvent3);
            AssertSelectedObjects(new List<BaseObject>{
                baseBpmEvent1, baseBpmEvent2, baseBpmEvent3,
            });
        }

        [Test]
        public void SelectBetweenNotesAndEvents()
        {
            SelectionController.SelectBetween(baseNote1, baseEvent3);
            AssertSelectedObjects(new List<BaseObject>
            {
                baseNote1, baseNote2, baseNote3,
                baseArc02, baseArc04, baseArc24,
                baseEvent1, baseEvent2, baseEvent3,
                baseRotationEvent2,
            });
        }

        [Test]
        public void SelectBetweenNotesAndBpmEvents()
        {
            SelectionController.SelectBetween(baseNote1, baseBpmEvent3);
            AssertSelectedObjects(new List<BaseObject>
            {
                baseNote1, baseNote2, baseNote3,
                baseArc02, baseArc04, baseArc24,
                baseRotationEvent2,
                baseBpmEvent1, baseBpmEvent2, baseBpmEvent3,
            });
        }

        [Test]
        public void SelectBetweenEventsAndBpmEvents()
        {
            SelectionController.SelectBetween(baseEvent1, baseBpmEvent3);
            AssertSelectedObjects(new List<BaseObject>
            {
                baseEvent1, baseEvent2, baseEvent3,
                baseRotationEvent2,
                baseBpmEvent1, baseBpmEvent2, baseBpmEvent3,
            });
        }

        private void AssertSelectedObjects(ICollection<BaseObject> objects)
        {
            Assert.True(Settings.Instance.MapVersion == 3);
            foreach (var baseObject in objects)
            {
                Assert.True(SelectionController.SelectedObjects.Contains(baseObject), $"{baseObject} should be selected");
            }

            Assert.AreEqual(objects.Count, SelectionController.SelectedObjects.Count, $"Selection should be the exact amount");
        }
    }
}