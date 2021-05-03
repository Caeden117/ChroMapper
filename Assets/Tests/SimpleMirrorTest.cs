using NUnit.Framework;
using System.Collections;
using System.Linq;
using SimpleJSON;
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
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            var noteA = new BeatmapNote(2, -2345, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_TYPE_A, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            
            notePlacement.queuedData = noteA;
            notePlacement.RoundedTime = notePlacement.queuedData._time;
            notePlacement.ApplyToMap();

            SelectionController.Select(noteA);

            _mirror.Mirror();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_B, 5345, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);

            // Undo mirror
            _actionContainer.Undo();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, -2345, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
        }
        
        [Test]
        public void MirrorNoteNE()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();
            
            var noteA = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A,
                _lineIndex = BeatmapNote.LINE_INDEX_FAR_LEFT,
                _lineLayer = BeatmapNote.LINE_LAYER_BOTTOM,
                _customData = JSON.Parse("{\"_position\": [-1, 0]}"),
                _cutDirection = BeatmapNote.NOTE_CUT_DIRECTION_LEFT
            };
            
            notePlacement.queuedData = noteA;
            notePlacement.RoundedTime = notePlacement.queuedData._time;
            notePlacement.ApplyToMap();

            SelectionController.Select(noteA);

            _mirror.Mirror();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT, JSON.Parse("{\"_position\": [1, 0]}"));

            // Undo mirror
            _actionContainer.Undo();
            NoteTest.CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT, JSON.Parse("{\"_position\": [-1, 0]}"));
        }

        [Test]
        public void MirrorProp()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (container is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE, JSON.Parse("{\"_lightID\": 2}"));

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData._value;
                eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(eventA);

                eventsContainer.EventTypeToPropagate = eventA._type;
                eventsContainer.PropagationEditing = EventsContainer.PropMode.Light;

                _mirror.Mirror();
                // I'm sorry if you're here after changing the prop mapping for default env
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_BLUE_FADE, JSON.Parse("{\"_lightID\": 9}"));

                // Undo mirror
                _actionContainer.Undo();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE, JSON.Parse("{\"_lightID\": 2}"));
                
                eventsContainer.PropagationEditing = EventsContainer.PropMode.Off;
            }
        }
        
        [Test]
        public void MirrorGradient()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (container is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData._value;
                eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(eventA);

                _mirror.Mirror();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_BLUE_FADE, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}"));

                // Undo mirror
                _actionContainer.Undo();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));
            }
        }

        [Test]
        public void MirrorWallME()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE);
            if (container is ObstaclesContainer wallsContainer)
            {
                var root = wallsContainer.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                // What the actual fuck - example from mirroring in MMA2
                //{"_time":1.5,"_lineIndex":1446,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                //{"_time":1.5,"_lineIndex":2958,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                var wallA = new BeatmapObstacle(2, 1446, 595141, 1, 2596);

                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData._time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData._duration * EditorScaleController.EditorScale);
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
            var container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE);
            if (container is ObstaclesContainer wallsContainer)
            {
                var root = wallsContainer.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                var wallA = new BeatmapObstacle(2, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapObstacle.VALUE_FULL_BARRIER, 1, 2, JSON.Parse("{\"_position\": [-1.5, 0]}"));

                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData._time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData._duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                SelectionController.Select(wallA);

                _mirror.Mirror();
                WallTest.CheckWall(wallsContainer, 0, 2, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapObstacle.VALUE_FULL_BARRIER, 1, 2, JSON.Parse("{\"_position\": [-0.5, 0]}"));

                // Undo mirror
                _actionContainer.Undo();
                WallTest.CheckWall(wallsContainer, 0, 2, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapObstacle.VALUE_FULL_BARRIER, 1, 2, JSON.Parse("{\"_position\": [-1.5, 0]}"));
            }
        }
        
        [Test]
        public void MirrorRotation()
        {
            var rotationCb = Object.FindObjectOfType<RotationCallbackController>();
            rotationCb.Start();
            
            var container = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (container is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(45), JSON.Parse("{\"_rotation\": 33}"));

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData._value;
                eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(eventA);

                _mirror.Mirror();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(-45), JSON.Parse("{\"_rotation\": -33}"));

                // Undo mirror
                _actionContainer.Undo();
                EventTest.CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(45), JSON.Parse("{\"_rotation\": 33}"));
            }
        }
    }
}
