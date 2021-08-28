using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class SimpleMirrorTest
    {
        private BeatmapActionContainer _actionContainer;
        private MirrorSelection _mirror;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMapper();

            _actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            _mirror = Object.FindObjectOfType<MirrorSelection>();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupNotes();
            TestUtils.CleanupEvents();
            TestUtils.CleanupObstacles();
        }

        [Test]
        public void MirrorME()
        {
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            Transform root = notesContainer.transform.root;
            NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

            BeatmapNote noteA = new BeatmapNote(2, -2345, BeatmapNote.LineLayerBottom, BeatmapNote.NoteTypeA, BeatmapNote.NoteCutDirectionLeft);

            notePlacement.QueuedData = noteA;
            notePlacement.RoundedTime = notePlacement.QueuedData.Time;
            notePlacement.ApplyToMap();

            SelectionController.Select(noteA);

            _mirror.Mirror();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeB, 5345, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionRight);

            // Undo mirror
            _actionContainer.Undo();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeA, -2345, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
        }

        [Test]
        public void MirrorNoteNE()
        {
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            Transform root = notesContainer.transform.root;
            NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

            BeatmapNote noteA = new BeatmapNote
            {
                Time = 2,
                Type = BeatmapNote.NoteTypeA,
                LineIndex = BeatmapNote.LineIndexFarLeft,
                LineLayer = BeatmapNote.LineLayerBottom,
                CustomData = JSON.Parse("{\"_position\": [-1, 0]}"),
                CutDirection = BeatmapNote.NoteCutDirectionLeft
            };

            notePlacement.QueuedData = noteA;
            notePlacement.RoundedTime = notePlacement.QueuedData.Time;
            notePlacement.ApplyToMap();

            SelectionController.Select(noteA);

            _mirror.Mirror();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionRight, JSON.Parse("{\"_position\": [1, 0]}"));

            // Undo mirror
            _actionContainer.Undo();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft, JSON.Parse("{\"_position\": [-1, 0]}"));
        }

        [Test]
        public void MirrorProp()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (container is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade, JSON.Parse("{\"_lightID\": 2}"));

                eventPlacement.QueuedData = eventA;
                eventPlacement.QueuedValue = eventPlacement.QueuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.QueuedData.Time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(eventA);

                eventsContainer.EventTypeToPropagate = eventA.Type;
                eventsContainer.PropagationEditing = EventsContainer.PropMode.Light;

                _mirror.Mirror();
                // I'm sorry if you're here after changing the prop mapping for default env
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeBackLasers, MapEvent.LightValueBlueFade, JSON.Parse("{\"_lightID\": 9}"));

                // Undo mirror
                _actionContainer.Undo();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade, JSON.Parse("{\"_lightID\": 2}"));

                eventsContainer.PropagationEditing = EventsContainer.PropMode.Off;
            }
        }

        [Test]
        public void MirrorGradient()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (container is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));

                eventPlacement.QueuedData = eventA;
                eventPlacement.QueuedValue = eventPlacement.QueuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.QueuedData.Time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(eventA);

                _mirror.Mirror();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeBackLasers, MapEvent.LightValueBlueFade, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}"));

                // Undo mirror
                _actionContainer.Undo();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));
            }
        }

        [Test]
        public void MirrorWallME()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle);
            if (container is ObstaclesContainer wallsContainer)
            {
                Transform root = wallsContainer.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                // What the actual fuck - example from mirroring in MMA2
                //{"_time":1.5,"_lineIndex":1446,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                //{"_time":1.5,"_lineIndex":2958,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                BeatmapObstacle wallA = new BeatmapObstacle(2, 1446, 595141, 1, 2596);

                wallPlacement.QueuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.QueuedData.Time;
                wallPlacement.InstantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.QueuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                SelectionController.Select(wallA);

                _mirror.Mirror();
                WallTest.CheckWall(wallsContainer, 0, 2, 2958, 595141, 1, 2596);

                // Undo mirror
                _actionContainer.Undo();
                WallTest.CheckWall(wallsContainer, 0, 2, 1446, 595141, 1, 2596);
            }
        }

        [Test]
        public void MirrorWallNE()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle);
            if (container is ObstaclesContainer wallsContainer)
            {
                Transform root = wallsContainer.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                BeatmapObstacle wallA = new BeatmapObstacle(2, BeatmapNote.LineIndexFarLeft, BeatmapObstacle.ValueFullBarrier, 1, 2, JSON.Parse("{\"_position\": [-1.5, 0]}"));

                wallPlacement.QueuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.QueuedData.Time;
                wallPlacement.InstantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.QueuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                SelectionController.Select(wallA);

                _mirror.Mirror();
                WallTest.CheckWall(wallsContainer, 0, 2, BeatmapNote.LineIndexMidRight, BeatmapObstacle.ValueFullBarrier, 1, 2, JSON.Parse("{\"_position\": [-0.5, 0]}"));

                // Undo mirror
                _actionContainer.Undo();
                WallTest.CheckWall(wallsContainer, 0, 2, BeatmapNote.LineIndexFarLeft, BeatmapObstacle.ValueFullBarrier, 1, 2, JSON.Parse("{\"_position\": [-1.5, 0]}"));
            }
        }

        [Test]
        public void MirrorRotation()
        {
            RotationCallbackController rotationCb = Object.FindObjectOfType<RotationCallbackController>();
            rotationCb.Start();

            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (container is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(45), JSON.Parse("{\"_rotation\": 33}"));

                eventPlacement.QueuedData = eventA;
                eventPlacement.QueuedValue = eventPlacement.QueuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.QueuedData.Time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(eventA);

                _mirror.Mirror();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(-45), JSON.Parse("{\"_rotation\": -33}"));

                // Undo mirror
                _actionContainer.Undo();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(45), JSON.Parse("{\"_rotation\": 33}"));
            }
        }
    }
}
