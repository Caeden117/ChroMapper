using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class NotesContainerTest : TestBase
    {
        [Test]
        public void RefreshSpecialAngles()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 4, Type = (int)NoteType.Red, PosX = (int)GridX.Left };
            noteA = PlaceUtils.Place(noteA);
            var containerA = noteGridContainer.LoadedContainers[noteA] as NoteContainer;

            var noteB = new BaseNote { JsonTime = 4, Type = (int)NoteType.Red, PosX = (int)GridX.MiddleLeft };
            noteB = PlaceUtils.Place(noteB);
            var containerB = noteGridContainer.LoadedContainers[noteB] as NoteContainer;

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, (int)GridX.MiddleLeft, (int)GridY.Upper, (int)NoteCutDirection.Right);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Right);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(90, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(333.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(63.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            noteA.JsonTime = 13;
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            noteGridContainer.RefreshSpecialAngles(noteB, true, false);
            Assert.AreEqual(45, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Make cleanup work
            noteA.JsonTime = 14;
        }

        // Beat Saber 1.44.1 authored fixture plus stack/dot cases from its decompiled row processor:
        // one fast test reports every snap-state and rendered-angle mismatch together.
        [Test]
        public void V4BeatSaberNoteSnappingFixtureMatchesGame()
        {
            var fixturePath = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "V4NoteSnappingPairs.json");
            var fixture = JSON.Parse(File.ReadAllText(fixturePath)).AsArray;
            var expected = new[]
            {
                true, false, true, true, true, true, false, true, true, true, false, false,
                false, false, false, true, true, false, true, false, false, false, false, true,
                true, false, true, false, false, false, false, true, true, true, true, true,
                true, false, true, true, true
            };
            var expectedExtendedAngles = new[]
            {
                (0f, 0f),
                (110f, 210f),
                (63.43495f, 63.43495f),
                (26.56505f, 26.56505f),
                (63.43495f, 63.43495f)
            };
            Assert.AreEqual(expected.Length * 2, fixture.Count);

            var failures = new List<string>();
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            for (var pairIndex = 0; pairIndex < expected.Length; pairIndex++)
            {
                var firstData = fixture[pairIndex * 2];
                var secondData = fixture[(pairIndex * 2) + 1];
                var authoredBeat = firstData["b"].AsFloat;
                var firstNote = NoteFromFixture(firstData);
                var secondNote = NoteFromFixture(secondData);
                firstNote.JsonTime = 4;
                secondNote.JsonTime = 4;
                var first = PlaceUtils.Place(firstNote);
                var second = PlaceUtils.Place(secondNote);
                var firstContainer = noteGridContainer.LoadedContainers[first] as NoteContainer;
                var secondContainer = noteGridContainer.LoadedContainers[second] as NoteContainer;
                var firstAuthored = NoteContainer.Directionalize(first).z;
                var secondAuthored = NoteContainer.Directionalize(second).z;
                var snapped = Mathf.Abs(Mathf.DeltaAngle(
                        firstAuthored, firstContainer.DirectionTarget.localEulerAngles.z)) > 0.01f
                    || Mathf.Abs(Mathf.DeltaAngle(
                        secondAuthored, secondContainer.DirectionTarget.localEulerAngles.z)) > 0.01f;

                if (snapped != expected[pairIndex])
                {
                    failures.Add(
                        $"Pair {pairIndex + 1} at beat {authoredBeat}: expected snap={expected[pairIndex]}, actual={snapped}.");
                }

                if (pairIndex >= 36)
                {
                    var expectedAngles = expectedExtendedAngles[pairIndex - 36];
                    var firstActual = firstContainer.DirectionTarget.localEulerAngles.z;
                    var secondActual = secondContainer.DirectionTarget.localEulerAngles.z;
                    if (Mathf.Abs(Mathf.DeltaAngle(expectedAngles.Item1, firstActual)) > 0.01f
                        || Mathf.Abs(Mathf.DeltaAngle(expectedAngles.Item2, secondActual)) > 0.01f)
                    {
                        failures.Add(
                            $"Pair {pairIndex + 1} at beat {authoredBeat}: expected angles "
                            + $"{expectedAngles.Item1:0.###}/{expectedAngles.Item2:0.###}, actual "
                            + $"{firstActual:0.###}/{secondActual:0.###}.");
                    }
                }

                PlaceUtils.Delete(first, triggersAction: false);
                PlaceUtils.Delete(second, triggersAction: false);
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private static BaseNote NoteFromFixture(JSONNode node) => new()
        {
            JsonTime = node["b"].AsFloat,
            PosX = node["x"].AsInt,
            PosY = node["y"].AsInt,
            Type = node["c"].AsInt,
            CutDirection = node["d"].AsInt,
            AngleOffset = node["a"].AsInt
        };

        [Test]
        public void RefreshSpecialAnglesOnDirectionChange()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌←◌◌
            var noteBottom = new BaseNote { JsonTime = 4, PosX = 1, CutDirection = (int)NoteCutDirection.Left };
            noteBottom = PlaceUtils.Place(noteBottom);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌←◌◌
            var noteTop =
                new BaseNote { JsonTime = 4, PosX = 2, PosY = 2, CutDirection = (int)NoteCutDirection.Down };
            noteTop = PlaceUtils.Place(noteTop);

            var containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            var containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↙◌◌
            // Direction keys own grid stepping now that Alt+scroll edits AngleOffset.
            NoteCommand.SetCutDirection(noteBottom, (int)NoteCutDirection.DownLeft);
            // Hover tweaks are selection-neutral now, so resolve the live replacement from the collection.
            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(n => n.JsonTime == 4 && n.PosX == 1);
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            NoteCommand.SetCutDirection(noteBottom, (int)NoteCutDirection.Down);
            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(n => n.JsonTime == 4 && n.PosX == 1);
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(333.43, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↘◌◌
            NoteCommand.SetCutDirection(noteBottom, (int)NoteCutDirection.DownRight);
            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(n => n.JsonTime == 4 && n.PosX == 1);
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);
        }

        // Coarse hover rotation follows Beat Saber by changing the raw direction while retaining the
        // authored offset; the raw-direction change releases this pair from snapping.
        [Test]
        public void CoarseHoverDirectionPreservesOffsetAndBreaksSpecialAngleSnapping()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            var noteBottom = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 4,
                PosX = (int)GridX.MiddleLeft,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down,
                AngleOffset = 20
            });
            var noteTop = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 4,
                PosX = (int)GridX.MiddleRight,
                PosY = (int)GridY.Top,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            });
            Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteBottom] as NoteContainer)
                .DirectionTarget.localEulerAngles.z, 0.01);

            inputController.ScrollUpdateDirection(noteGridContainer.LoadedContainers[noteBottom] as NoteContainer, 1);

            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(note => note.JsonTime == 4 && note.PosX == (int)GridX.MiddleLeft);
            Assert.AreEqual((int)NoteCutDirection.DownRight, noteBottom.CutDirection);
            Assert.AreEqual(20, noteBottom.AngleOffset);
            Assert.AreEqual(65, (noteGridContainer.LoadedContainers[noteBottom] as NoteContainer)
                .DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, (noteGridContainer.LoadedContainers[noteTop] as NoteContainer)
                .DirectionTarget.localEulerAngles.z, 0.01);
        }

        // Beat Saber snapping keys off the raw direction, so fine offset edits keep a matching pair snapped.
        [Test]
        public void PreciseHoverAngleOffsetKeepsBeatSaberSnapping()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            var noteBottom = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 5,
                PosX = (int)GridX.MiddleLeft,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            });
            var noteTop = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 5,
                PosX = (int)GridX.MiddleRight,
                PosY = (int)GridY.Top,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            });
            Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteBottom] as NoteContainer)
                .DirectionTarget.localEulerAngles.z, 0.01);

            inputController.ScrollPreciseUpdateDirection(
                noteGridContainer.LoadedContainers[noteBottom] as NoteContainer, 1);

            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(note => note.JsonTime == 5 && note.PosX == (int)GridX.MiddleLeft);
            Assert.AreNotEqual(0, noteBottom.AngleOffset);
            Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteBottom] as NoteContainer)
                .DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteTop] as NoteContainer)
                .DirectionTarget.localEulerAngles.z, 0.01);
        }

        // Returning a precise offset to zero keeps the same raw direction and therefore remains snapped.
        [Test]
        public void PreciseHoverAngleOffsetReturningToZeroKeepsBeatSaberSnapping()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            var precisionController = Object.FindAnyObjectByType<ScrollPrecisionController>();
            var previousPrecision = precisionController.CurrentPrecision;
            precisionController.CurrentPrecision = ScrollPrecision.Medium;

            try
            {
                var noteBottom = PlaceUtils.Place(new BaseNote
                {
                    JsonTime = 6,
                    PosX = (int)GridX.MiddleLeft,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down,
                    AngleOffset = 5
                });
                var noteTop = PlaceUtils.Place(new BaseNote
                {
                    JsonTime = 6,
                    PosX = (int)GridX.MiddleRight,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down
                });
                Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteBottom] as NoteContainer)
                    .DirectionTarget.localEulerAngles.z, 0.01);

                inputController.ScrollPreciseUpdateDirection(
                    noteGridContainer.LoadedContainers[noteBottom] as NoteContainer, -1);

                noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                    .Single(note => note.JsonTime == 6 && note.PosX == (int)GridX.MiddleLeft);
                Assert.AreEqual(0, noteBottom.AngleOffset);
                Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteBottom] as NoteContainer)
                    .DirectionTarget.localEulerAngles.z, 0.01);
                Assert.AreEqual(333.43, (noteGridContainer.LoadedContainers[noteTop] as NoteContainer)
                    .DirectionTarget.localEulerAngles.z, 0.01);
            }
            finally
            {
                precisionController.CurrentPrecision = previousPrecision;
            }
        }

        [Test]
        public void RefreshSpecialAnglesOnDirectionChange2()
        {
            // Test that angles are not changed when they shouldn't be
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            // ◌◌◌◌
            // ◌◌◌◌
            // ←◌◌◌
            var noteBottom = new BaseNote { JsonTime = 4, CutDirection = (int)NoteCutDirection.Left };
            noteBottom = PlaceUtils.Place(noteBottom);

            // ◌◌↓◌
            // ◌◌◌◌
            // ←◌◌◌
            var noteTop =
                new BaseNote { JsonTime = 4, PosX = 2, PosY = 2, CutDirection = (int)NoteCutDirection.Down };
            noteTop = PlaceUtils.Place(noteTop);

            var containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            var containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↙◌◌◌
            // Direction keys own grid stepping now that Alt+scroll edits AngleOffset.
            NoteCommand.SetCutDirection(noteBottom, (int)NoteCutDirection.DownLeft);
            // Hover tweaks are selection-neutral now, so resolve the live replacement from the collection.
            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(n => n.JsonTime == 4 && n.PosX == 0);
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↓◌◌◌
            NoteCommand.SetCutDirection(noteBottom, (int)NoteCutDirection.Down);
            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(n => n.JsonTime == 4 && n.PosX == 0);
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↘◌◌◌
            NoteCommand.SetCutDirection(noteBottom, (int)NoteCutDirection.DownRight);
            noteBottom = noteGridContainer.LoadedObjects.OfType<BaseNote>()
                .Single(n => n.JsonTime == 4 && n.PosX == 0);
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);
        }

        [Test]
        public void RefreshSpecialAnglesIgnoresPrecisionPlacement()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 4, PosX = 1 };
            noteA = PlaceUtils.Place(noteA);

            var noteB = new BaseNote { JsonTime = 4 };
            noteB = PlaceUtils.Place(noteB);

            var containerA = noteGridContainer.LoadedContainers[noteA] as NoteContainer;
            var containerB = noteGridContainer.LoadedContainers[noteB] as NoteContainer;

            // ME precision placed
            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, 1000);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, 1000);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // NE precision placed
            (containerA.ObjectData as BaseNote).CustomCoordinate = new JSONArray { [0] = 0, [1] = 2 };
            (containerB.ObjectData as BaseNote).CustomCoordinate = new JSONArray { [0] = -1, [1] = 0 };
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);
        }

        private void UpdateNote(NoteContainer container, int PosX, int PosY, int cutDirection)
        {
            var note = (BaseNote)container.ObjectData;
            note.PosX = PosX;
            note.PosY = PosY;
            note.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(note);
        }

        [Test]
        public void ShiftInTime()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 2, Type = (int)NoteType.Red };
            noteA = PlaceUtils.Place(noteA);

            var noteB = new BaseNote { JsonTime = 3, Type = (int)NoteType.Red };
            noteB = PlaceUtils.Place(noteB);

            SelectionController.Select(noteB, false, false, false);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            selectionController.MoveSelection(-2);

            var noteBAfterMove = new BaseNote { JsonTime = 1, Type = (int)NoteType.Red };

            PlaceUtils.Delete(noteBAfterMove);

            BeatmapAssertion.CollectionCount<BaseNote>(1);
        }
    }
}