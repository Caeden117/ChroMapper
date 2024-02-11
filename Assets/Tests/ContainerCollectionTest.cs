using System;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ContainerCollectionTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            CleanupUtils.CleanupNotes();
        }

        [Test]
        public void GetBetween()
        {
            var epsilon = BeatmapObjectContainerCollection.Epsilon;
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteBlue0 = new V3ColorNote { Color = (int)NoteType.Blue };

            var noteBlue1 = new V3ColorNote { Color = (int)NoteType.Blue, JsonTime = 1 };
            var noteRed1 = new V3ColorNote { Color = (int)NoteType.Red, JsonTime = 1, PosX = 1 };

            var noteRed2 = new V3ColorNote { Color = (int)NoteType.Red, JsonTime = 2, PosX = 1 };

            foreach (var note in new List<BaseNote> { noteBlue0, noteBlue1, noteRed1, noteRed2 })
            {
                noteGridContainer.SpawnObject(note);
            }

            // Single point
            var result = noteGridContainer.GetBetween(0 - epsilon, 0 + epsilon);
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(noteBlue0, result[0]);

            result = noteGridContainer.GetBetween(1 - epsilon, 1 + epsilon);
            Assert.AreEqual(2, result.Length);
            Assert.AreSame(noteBlue1, result[0]);
            Assert.AreSame(noteRed1, result[1]);

            result = noteGridContainer.GetBetween(2 - epsilon, 2 + epsilon);
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(noteRed2, result[0]);

            // Sections
            result = noteGridContainer.GetBetween(0 - epsilon, 2 + epsilon);
            Assert.AreEqual(4, result.Length);
            Assert.AreSame(noteBlue0, result[0]);
            Assert.AreSame(noteBlue1, result[1]);
            Assert.AreSame(noteRed1, result[2]);
            Assert.AreSame(noteRed2, result[3]);

            result = noteGridContainer.GetBetween(0 - epsilon, 1 + epsilon);
            Assert.AreEqual(3, result.Length);
            Assert.AreSame(noteBlue0, result[0]);
            Assert.AreSame(noteBlue1, result[1]);
            Assert.AreSame(noteRed1, result[2]);

            result = noteGridContainer.GetBetween(1 - epsilon, 2 + epsilon);
            Assert.AreSame(noteBlue1, result[0]);
            Assert.AreSame(noteRed1, result[1]);
            Assert.AreSame(noteRed2, result[2]);

            // Empty section
            result = noteGridContainer.GetBetween(0.1f, 0.9f);
            Assert.AreEqual(0, result.Length);
        }
    }
}
