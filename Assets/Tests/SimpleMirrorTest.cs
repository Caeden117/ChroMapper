using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V2;
using Beatmap.V3;
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
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            Transform root = notesContainer.transform.root;
            NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

            BaseNote baseNoteA = new V3ColorNote(2, -2345, (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left);

            notePlacement.queuedData = baseNoteA;
            notePlacement.RoundedTime = notePlacement.queuedData.Time;
            notePlacement.ApplyToMap();

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            CheckUtils.CheckNote("Perform note mirror", notesContainer, 0, 2, (int)NoteType.Blue, 5345, (int)GridY.Base, (int)NoteCutDirection.Right, 0);

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckNote("Undo note mirror", notesContainer, 0, 2, (int)NoteType.Red, -2345, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
        }

        [Test]
        public void MirrorNoteNE()
        {
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            Transform root = notesContainer.transform.root;
            NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

            BaseNote baseNoteA = new V3ColorNote(2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, JSON.Parse("{\"coordinates\": [-1, 0]}"));

            notePlacement.queuedData = baseNoteA;
            notePlacement.RoundedTime = notePlacement.queuedData.Time;
            notePlacement.ApplyToMap();

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            CheckUtils.CheckNote("Perform NE note mirror", notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.Right, 0, JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [0, 0]}}"));

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckNote("Undo NE note inversion", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0, JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [-1, 0]}}"));
        }

        [Test]
        public void MirrorProp()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f, JSON.Parse("{\"lightID\": 2}"));

                eventPlacement.queuedData = baseEventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(baseEventA);

                eventsContainer.EventTypeToPropagate = baseEventA.Type;
                eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;

                _mirror.Mirror();
                // I'm sorry if you're here after changing the prop mapping for default env
                CheckUtils.CheckEvent("Perform mirror prop event", eventsContainer, 0, 2, (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade, 1f, JSON.Parse("{\"lightID\": [9]}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckEvent("Undo mirror prop event", eventsContainer, 0, 2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f, JSON.Parse("{\"lightID\": [2]}"));

                eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
            }
        }

        [Test]
        public void MirrorGradient()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V2Event(2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));

                eventPlacement.queuedData = baseEventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(baseEventA);
                
                _mirror.Mirror();
                CheckUtils.CheckEvent("Perform mirror gradient event", eventsContainer, 0, 2, (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade, 1f, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckEvent("Undo mirror gradient event", eventsContainer, 0, 2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f, JSON.Parse("{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));
            }
        }

        [Test]
        public void MirrorWallME()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (container is ObstacleGridContainer wallsContainer)
            {
                Transform root = wallsContainer.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                // What the actual fuck - example from mirroring in MMA2
                //{"_time":1.5,"_lineIndex":1446,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                //{"_time":1.5,"_lineIndex":2958,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                BaseObstacle wallA = new V2Obstacle(2, 1446, 595141, 1, 2596);
                
                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData.Time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                SelectionController.Select(wallA);

                _mirror.Mirror();
                CheckUtils.CheckWall("Perform ME wall mirror", wallsContainer, 0, 2, 2958, 0, 1, 2596, 0, 595141);

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckWall("Undo ME wall mirror", wallsContainer, 0, 2, 1446, 0, 1, 2596, 0, 595141);
            }
        }

        [Test]
        public void MirrorWallNE()
        {
            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (container is ObstacleGridContainer wallsContainer)
            {
                Transform root = wallsContainer.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                BaseObstacle wallA = new V3Obstacle(2, (int)GridX.Left, (int)GridY.Base, 1, 2, 5, JSON.Parse("{\"coordinates\": [-1.5, 0]}"));

                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData.Time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                SelectionController.Select(wallA);

                _mirror.Mirror();
                CheckUtils.CheckWall("Perform NE wall mirror", wallsContainer, 0, 2, (int)GridX.MiddleRight, (int)GridY.Base, 1, 2, 5, (int)ObstacleType.Full, JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-0.5, 0]}}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckWall("Undo NE wall mirror", wallsContainer, 0, 2, (int)GridX.Left, (int)GridY.Base, 1, 2, 5, (int)ObstacleType.Full, JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-1.5, 0]}}"));
            }
        }

        [Test]
        public void MirrorRotation()
        {
            RotationCallbackController rotationCb = Object.FindObjectOfType<RotationCallbackController>();
            rotationCb.Start();

            BeatmapObjectContainerCollection container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V2Event(2, (int)EventTypeValue.LateLaneRotation, (int)BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(45), 1f, JSON.Parse("{\"_rotation\": 33}"));

                eventPlacement.queuedData = baseEventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                SelectionController.Select(baseEventA);

                _mirror.Mirror();
                CheckUtils.CheckEvent("Perform mirror rotation event", eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(-45), 1f, JSON.Parse("{\"_rotation\": -33}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckEvent("Undo mirror rotation event", eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(45), 1f, JSON.Parse("{\"_rotation\": 33}"));
            }
        }
    }
}
