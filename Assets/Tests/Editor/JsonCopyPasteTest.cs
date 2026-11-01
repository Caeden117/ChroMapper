using Beatmap.Base;
using Beatmap.Enums;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class JsonCopyPasteTest : JsonCopyPasteTestBase
    {
        [Test]
        public void CopyExportsJsonOnlyAfterPointerLeavesChroMapper()
        {
            const string clipboardSentinel = "clipboard must remain unchanged inside ChroMapper";
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 12.5f,
                PosX = 1,
                PosY = 2,
                Color = 1,
                CutDirection = 8,
                AngleOffset = -45
            });

            SelectionController.Select(note);
            ExternalClipboardText = clipboardSentinel;
            selectionController.Copy();

            Assert.That(ExternalClipboardText, Is.EqualTo(clipboardSentinel));

            LeaveChroMapperWindow();

            Assert.That(
                ExternalClipboardText,
                Does.StartWith("["),
                "Leaving ChroMapper did not replace the system clipboard with the copied selection's JSON array.");
            var exported = JSON.Parse(ExternalClipboardText);
            Assert.That(exported.IsArray, Is.True);
            Assert.That(exported.AsArray.Count, Is.EqualTo(1));
            Assert.That(exported[0]["b"].AsFloat, Is.EqualTo(12.5f));
            Assert.That(exported[0]["x"].AsInt, Is.EqualTo(1));
            Assert.That(exported[0]["y"].AsInt, Is.EqualTo(2));
            Assert.That(exported[0]["c"].AsInt, Is.EqualTo(1));
            Assert.That(exported[0]["d"].AsInt, Is.EqualTo(8));
            Assert.That(exported[0]["a"].AsInt, Is.EqualTo(-45));
        }

        [Test]
        public void LeavingWithTwentyThousandBasicEventsFormatsOneClipboardWrite()
        {
            var copiedObjects = Enumerable.Range(0, 20000)
                .Select(index =>
                {
                    var evt = new BaseEvent
                    {
                        JsonTime = index / 8f,
                        Type = 1,
                        Value = index % 8,
                        FloatValue = 1
                    };
                    return new ClipboardJsonObject(evt, evt.ToJson());
                })
                .ToList();

            ClipboardJsonController.CaptureCopiedObjects(copiedObjects);
            LeaveChroMapperWindow();

            Assert.That(ExternalClipboardWriteCount, Is.EqualTo(1));
            Assert.That(JSON.Parse(ExternalClipboardText).Count, Is.EqualTo(20000));
        }

        [Test]
        public void CopyExportsMultipleDifficultyCollectionsInMinimalOuterObject()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var lateColorNote = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 7,
                PosX = 2,
                Type = (int)NoteType.Blue
            });
            var bomb = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 5,
                PosX = 1,
                Type = (int)NoteType.Bomb
            });
            var earlyColorNote = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 3,
                PosX = 0,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(lateColorNote);
            SelectionController.Select(bomb, true);
            SelectionController.Select(earlyColorNote, true);
            selectionController.Copy();
            LeaveChroMapperWindow();

            var exported = JSON.Parse(ExternalClipboardText);
            Assert.That(exported.IsObject, Is.True, ExternalClipboardText);
            Assert.That(exported.Count, Is.EqualTo(2));
            Assert.That(exported["colorNotes"].AsArray.Count, Is.EqualTo(2));
            Assert.That(exported["bombNotes"].AsArray.Count, Is.EqualTo(1));
            Assert.That(exported["colorNotes"][0]["b"].AsFloat, Is.EqualTo(3));
            Assert.That(exported["colorNotes"][1]["b"].AsFloat, Is.EqualTo(7));
            Assert.That(exported["bombNotes"][0]["b"].AsFloat, Is.EqualTo(5));
        }

        [Test]
        public void DisabledClipboardSyncDoesNotExportOnPointerLeave()
        {
            const string clipboardSentinel = "leave this clipboard alone";
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 4,
                Type = (int)NoteType.Red
            });

            Settings.Instance.SyncCopiedObjectsWithSystemClipboard = false;
            SelectionController.Select(note);
            ExternalClipboardText = clipboardSentinel;
            selectionController.Copy();
            LeaveChroMapperWindow();

            Assert.That(ExternalClipboardText, Is.EqualTo(clipboardSentinel));
        }

        [Test]
        public void ReturningWithUnchangedExportKeepsExistingCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 9,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(note);
            selectionController.Copy();
            var copiedNote = SelectionController.CopiedObjects.Single();
            LeaveChroMapperWindow();
            var writesAfterExport = ExternalClipboardWriteCount;
            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Single(), Is.SameAs(copiedNote));
            Assert.That(ExternalClipboardWriteCount, Is.EqualTo(writesAfterExport));
        }

        [Test]
        public void CutExportsTheDeletedSelectionWithItsOriginalBeat()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 16,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(note);
            selectionController.Copy(true);
            LeaveChroMapperWindow();

            var exported = JSON.Parse(ExternalClipboardText);
            Assert.That(exported.AsArray.Count, Is.EqualTo(1));
            Assert.That(exported[0]["b"].AsFloat, Is.EqualTo(16));
        }

        [Test]
        public void ReturningWithEditedColorNoteJsonRebuildsRelativeCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = @"[
  { ""b"": 20, ""x"": 1, ""y"": 0, ""c"": 1, ""d"": 8, ""a"": -45 },
  { ""b"": 22.5, ""x"": 0, ""y"": 2, ""c"": 0, ""d"": 7, ""a"": 0 }
]";

            ReturnToChroMapperWindow();

            var importedNotes = SelectionController.CopiedObjects.Cast<BaseNote>().OrderBy(note => note.JsonTime).ToList();
            Assert.That(importedNotes.Count, Is.EqualTo(2));
            Assert.That(importedNotes[0].JsonTime, Is.EqualTo(0));
            Assert.That(importedNotes[0].PosX, Is.EqualTo(1));
            Assert.That(importedNotes[0].Color, Is.EqualTo(1));
            Assert.That(importedNotes[0].AngleOffset, Is.EqualTo(-45));
            Assert.That(importedNotes[1].JsonTime, Is.EqualTo(2.5f));
            Assert.That(importedNotes[1].PosY, Is.EqualTo(2));
            Assert.That(importedNotes[1].CutDirection, Is.EqualTo(7));
        }

        [Test]
        public void ReturningWithLooseTrailingCommaObjectsImportsThemAsOneArray()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = @"
{ ""b"": 8, ""x"": 1, ""y"": 0, ""c"": 1, ""d"": 8, ""a"": -45 },
{ ""b"": 9, ""x"": 0, ""y"": 1, ""c"": 0, ""d"": 7, ""a"": 0 },";

            ReturnToChroMapperWindow();

            var importedNotes = SelectionController.CopiedObjects.Cast<BaseNote>().OrderBy(note => note.JsonTime).ToList();
            Assert.That(importedNotes.Count, Is.EqualTo(2));
            Assert.That(importedNotes[0].JsonTime, Is.EqualTo(0));
            Assert.That(importedNotes[1].JsonTime, Is.EqualTo(1));
        }

        [Test]
        public void ReturningWithLooseColorNotesWithoutBeatsImportsThemAtOneRelativeBeat()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote { JsonTime = 2, Type = (int)NoteType.Red });
            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = @"
{ ""x"": 1, ""y"": 0, ""c"": 1, ""d"": 8, ""a"": -45 },
{ ""x"": 0, ""y"": 1, ""c"": 0, ""d"": 7, ""a"": 0 },";

            ReturnToChroMapperWindow();

            var importedNotes = SelectionController.CopiedObjects.Cast<BaseNote>().ToList();
            Assert.That(importedNotes.Count, Is.EqualTo(2));
            Assert.That(importedNotes.All(note => note.JsonTime == 0), Is.True);
        }

        [TestCase("[{ \"b\": 4, \"x\": 1, \"y\": 0, \"d\": 2, \"w\": 1, \"h\": 3 }]", ObjectType.Obstacle)]
        [TestCase("[{ \"b\": 4, \"c\": 0, \"x\": 1, \"y\": 0, \"d\": 1, \"mu\": 1, \"tb\": 5, \"tx\": 2, \"ty\": 1, \"tc\": 1, \"tmu\": 1, \"m\": 0 }]", ObjectType.Arc)]
        [TestCase("[{ \"b\": 4, \"c\": 0, \"x\": 1, \"y\": 0, \"d\": 1, \"tb\": 5, \"tx\": 2, \"ty\": 1, \"sc\": 4, \"s\": 1 }]", ObjectType.Chain)]
        [TestCase("[{ \"b\": 4, \"x\": 1, \"y\": 0, \"d\": 2 }]", ObjectType.Waypoint)]
        [TestCase("[{ \"b\": 4, \"et\": 1, \"i\": 2, \"f\": 0.5 }]", ObjectType.Event)]
        [TestCase("[{ \"b\": 4, \"e\": 0, \"r\": 15 }]", ObjectType.RotationEvent)]
        [TestCase("[{ \"b\": 4, \"m\": 150 }]", ObjectType.BpmChange)]
        [TestCase("[{ \"b\": 4, \"t\": \"AnimateTrack\", \"d\": { \"track\": \"a\" } }]", ObjectType.CustomEvent)]
        [TestCase("[{ \"b\": 4, \"n\": \"drop\", \"c\": [1, 0, 0, 1] }]", ObjectType.Bookmark)]
        [TestCase("[{ \"beat\": 4, \"usePrevious\": 0, \"easing\": 1, \"relative-njs\": 2 }]", ObjectType.NJSEvent)]
        public void ReturningWithUnambiguousRawArrayInfersEntityType(string json, ObjectType expectedType)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = json;

            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Single().ObjectType, Is.EqualTo(expectedType));
        }

        [Test]
        public void ReturningWithMultipleDifficultyCollectionsImportsAllNonEmptyCollections()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = @"{
  ""colorNotes"": [
    { ""b"": 12, ""x"": 1, ""y"": 0, ""c"": 1, ""d"": 8, ""a"": -45 }
  ],
  ""bombNotes"": [
    { ""b"": 14, ""x"": 2, ""y"": 1 }
  ]
}";

            ReturnToChroMapperWindow();

            var importedNotes = SelectionController.CopiedObjects.Cast<BaseNote>().OrderBy(note => note.JsonTime).ToList();
            Assert.That(importedNotes.Count, Is.EqualTo(2));
            Assert.That(importedNotes[0].Type, Is.EqualTo((int)NoteType.Blue));
            Assert.That(importedNotes[0].JsonTime, Is.EqualTo(0));
            Assert.That(importedNotes[1].Type, Is.EqualTo((int)NoteType.Bomb));
            Assert.That(importedNotes[1].JsonTime, Is.EqualTo(2));
        }

        [Test]
        public void ReturningWithEmptyRecognizedCollectionsDoesNotReplaceCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(source);
            selectionController.Copy();
            var copiedObject = SelectionController.CopiedObjects.Single();
            LeaveChroMapperWindow();
            ExternalClipboardText = "{ \"colorNotes\": [], \"bombNotes\": [] }";

            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(1));
            Assert.That(SelectionController.CopiedObjects.Single(), Is.SameAs(copiedObject));
        }

        [Test]
        public void VersionFourExportsAndImportsVersionThreeNoteJson()
        {
            var originalMapVersion = Settings.Instance.MapVersion;
            try
            {
                Settings.Instance.MapVersion = 4;
                var selectionController = Object.FindAnyObjectByType<SelectionController>();
                var source = PlaceUtils.Place(new BaseNote
                {
                    JsonTime = 6,
                    PosX = 1,
                    PosY = 2,
                    Type = (int)NoteType.Blue,
                    CutDirection = 3,
                    AngleOffset = 15
                });

                SelectionController.Select(source);
                selectionController.Copy();
                LeaveChroMapperWindow();

                var exported = JSON.Parse(ExternalClipboardText);
                Assert.That(exported[0].HasKey("b"), Is.True);
                Assert.That(exported[0].HasKey("c"), Is.True);
                Assert.That(exported[0].HasKey("i"), Is.False);

                ExternalClipboardText = ExternalClipboardText.Replace("\"b\" : 6", "\"b\" : 10");
                ReturnToChroMapperWindow();

                var importedNote = SelectionController.CopiedObjects.Single() as BaseNote;
                Assert.That(importedNote, Is.Not.Null);
                Assert.That(importedNote.Type, Is.EqualTo((int)NoteType.Blue));
                Assert.That(importedNote.JsonTime, Is.EqualTo(0));
            }
            finally
            {
                Settings.Instance.MapVersion = originalMapVersion;
            }
        }

        [Test]
        public void ReturningWithWindowsNewlinesAndUnicodeJsonRebuildsCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = "[\r\n"
                + "  { \"b\": 20, \"x\": 1, \"y\": 0, \"c\": 1, \"d\": 8, \"a\": -45,\r\n"
                + "    \"customData\": { \"track\": \"照明-ä\" } }\r\n"
                + "]";

            ReturnToChroMapperWindow();

            var importedNote = SelectionController.CopiedObjects.Single() as BaseNote;
            Assert.That(importedNote, Is.Not.Null);
            Assert.That(importedNote.JsonTime, Is.EqualTo(0));
            Assert.That(importedNote.ToJson()["customData"]["track"].Value, Is.EqualTo("照明-ä"));
        }

        [Test]
        public void ReturningWithOnlyNativeNewlineNormalizationKeepsExistingCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });

            SelectionController.Select(source);
            selectionController.Copy();
            var copiedObject = SelectionController.CopiedObjects.Single();
            LeaveChroMapperWindow();
            ExternalClipboardText = ExternalClipboardText.Contains("\r\n")
                ? ExternalClipboardText.Replace("\r\n", "\n")
                : ExternalClipboardText.Replace("\n", "\r\n");

            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Single(), Is.SameAs(copiedObject));
        }

        [Test]
        public void DuplicatePresenceNotificationsReadEditedClipboardOnlyOnce()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            SelectionController.Select(source);
            selectionController.Copy();
            ClipboardJsonController.HandleChroMapperPresenceChanged(false);
            ClipboardJsonController.HandleChroMapperPresenceChanged(false);
            ExternalClipboardText =
                "[{ \"b\": 8, \"x\": 1, \"y\": 0, \"c\": 1, \"d\": 8, \"a\": -45 }]";
            var readsBeforeReturn = ExternalClipboardReadCount;

            ClipboardJsonController.HandleChroMapperPresenceChanged(true);
            var importedObject = SelectionController.CopiedObjects.Single();
            ClipboardJsonController.HandleChroMapperPresenceChanged(true);

            Assert.That(ExternalClipboardReadCount - readsBeforeReturn, Is.EqualTo(1));
            Assert.That(SelectionController.CopiedObjects.Single(), Is.SameAs(importedObject));
        }

        [Test]
        public void MixedDifficultyAndBpmSelectionExportsFilenameEnvelopeWithSelectedRegion()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var colorNote = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            var bomb = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 3,
                Type = (int)NoteType.Bomb
            });
            var selectedBpm = PlaceUtils.Place(new BaseBpmEvent(4, 150));
            PlaceUtils.Place(new BaseBpmEvent(8, 180));

            SelectionController.Select(colorNote);
            SelectionController.Select(bomb, true);
            SelectionController.Select(selectedBpm, true);
            selectionController.Copy();
            LeaveChroMapperWindow();

            var exported = JSON.Parse(ExternalClipboardText);
            Assert.That(exported.IsObject, Is.True);
            Assert.That(exported.Count, Is.EqualTo(2));
            Assert.That(exported["Difficulty.dat"]["colorNotes"].Count, Is.EqualTo(1));
            Assert.That(exported["Difficulty.dat"]["bombNotes"].Count, Is.EqualTo(1));
            Assert.That(exported["BPMInfo.dat"]["_regions"].Count, Is.EqualTo(1));
            Assert.That(exported["BPMInfo.dat"]["_regions"][0]["_startBeat"].AsFloat, Is.EqualTo(4));
            Assert.That(exported["BPMInfo.dat"]["_regions"][0]["_endBeat"].AsFloat, Is.EqualTo(8));
        }

        [Test]
        public void VersionTwoMixedDifficultyAndBpmSelectionUsesVersionTwoDifficultyAndBpmInfoFormats()
        {
            var originalMapVersion = Settings.Instance.MapVersion;
            try
            {
                Settings.Instance.MapVersion = 2;
                var selectionController = Object.FindAnyObjectByType<SelectionController>();
                var note = PlaceUtils.Place(new BaseNote
                {
                    JsonTime = 2,
                    PosX = 1,
                    PosY = 2,
                    Type = (int)NoteType.Red,
                    CutDirection = 1
                });
                var selectedBpm = PlaceUtils.Place(new BaseBpmEvent(4, 150));
                PlaceUtils.Place(new BaseBpmEvent(8, 180));
                SelectionController.Select(note);
                SelectionController.Select(selectedBpm, true);

                selectionController.Copy();
                LeaveChroMapperWindow();

                var exported = JSON.Parse(ExternalClipboardText);
                Assert.That(exported["Difficulty.dat"]["_notes"].Count, Is.EqualTo(1));
                Assert.That(exported["Difficulty.dat"]["_notes"][0].HasKey("_time"), Is.True);
                Assert.That(exported["BPMInfo.dat"]["_regions"].Count, Is.EqualTo(1));
                Assert.That(exported["BPMInfo.dat"].HasKey("_events"), Is.False);
            }
            finally
            {
                Settings.Instance.MapVersion = originalMapVersion;
            }
        }

        [Test]
        public void ReturningWithFilenameEnvelopeImportsDifficultyAndBpmRegions()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            const int audioFrequency = 48000;
            SetAudioMetadata(audioFrequency * 60, audioFrequency);

            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = $@"{{
  ""BPMInfo.dat"": {{
    ""_regions"": [
      {{ ""_startSampleIndex"": 0, ""_endSampleIndex"": {audioFrequency * 2}, ""_startBeat"": 10, ""_endBeat"": 14 }}
    ]
  }},
  ""Difficulty.dat"": {{
    ""colorNotes"": [
      {{ ""b"": 12, ""x"": 1, ""y"": 0, ""c"": 1, ""d"": 8, ""a"": -45 }}
    ]
  }}
}}";

            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(2));
            var importedBpm = SelectionController.CopiedObjects.OfType<BaseBpmEvent>().Single();
            var importedNote = SelectionController.CopiedObjects.OfType<BaseNote>().Single();
            Assert.That(importedBpm.JsonTime, Is.EqualTo(0));
            Assert.That(importedBpm.Bpm, Is.EqualTo(120).Within(0.001f));
            Assert.That(importedNote.JsonTime, Is.EqualTo(2));
        }

        [Test]
        public void ReturningWithZeroLengthBpmRegionDoesNotReplaceCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            SetAudioMetadata(48000 * 60, 48000);
            SelectionController.Select(source);
            selectionController.Copy();
            var copiedObject = SelectionController.CopiedObjects.Single();
            LeaveChroMapperWindow();
            ExternalClipboardText = @"[
  { ""_startSampleIndex"": 24000, ""_endSampleIndex"": 24000, ""_startBeat"": 4, ""_endBeat"": 8 }
]";

            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Single(), Is.SameAs(copiedObject));
        }

        [Test]
        public void CopyingInnerGlsEventsFromOneFilterLaneExportsRawEventArray()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.Boxes[0].Events[1]);
            SelectionController.Select(group.Boxes[0].Events[2], true);

            selectionController.Copy();
            LeaveChroMapperWindow();

            var exported = JSON.Parse(ExternalClipboardText);
            Assert.That(exported.IsArray, Is.True);
            Assert.That(exported.Count, Is.EqualTo(2));
            Assert.That(exported[0]["b"].AsFloat, Is.EqualTo(1));
            Assert.That(exported[1]["b"].AsFloat, Is.EqualTo(2));
        }

        [TestCase("color", "[{ \"b\": 4, \"c\": 1, \"s\": 2, \"i\": 0, \"f\": 0, \"sb\": 0, \"sf\": 0 }]")]
        [TestCase("rotation", "[{ \"b\": 4, \"r\": 45, \"o\": 0, \"e\": 1, \"l\": 0, \"p\": 0 }]")]
        [TestCase("translation", "[{ \"b\": 4, \"p\": 0, \"e\": 1, \"t\": 2.5 }]")]
        [TestCase("float", "[{ \"b\": 4, \"p\": 0, \"v\": 2.5, \"i\": 1 }]")]
        public void ReturningWithRawInnerGlsArrayUsesOpenGroupContext(string kind, string json)
        {
            var context = PlaceGlsGroup(CreateSingleLaneGlsGroup(kind));
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = json;

            ReturnToChroMapperWindow();

            var importedEvent = SelectionController.CopiedObjects.OfType<BaseGLSEvent>().Single();
            Assert.That(importedEvent.EventBoxGroupData, Is.SameAs(context));
            Assert.That(importedEvent.EventBoxData, Is.SameAs(context.ReadOnlyBoxes[0]));
            Assert.That(importedEvent.BoxIndex, Is.EqualTo(0));
            Assert.That(importedEvent.RelativeJsonTime, Is.EqualTo(4));
            Assert.That(importedEvent.JsonTime, Is.EqualTo(context.JsonTime + 4));
        }

        [TestCase("[{ \"b\": 4, \"g\": 1, \"e\": [{ \"f\": { \"f\": 1, \"p\": 0 }, \"w\": 1, \"d\": 0, \"r\": 0, \"t\": 0, \"b\": 0, \"i\": 0, \"e\": [{ \"b\": 0, \"c\": 0, \"s\": 1, \"i\": 0, \"f\": 0, \"sb\": 0, \"sf\": 0 }] }] }]", ObjectType.GLSColor)]
        [TestCase("[{ \"b\": 4, \"g\": 1, \"e\": [{ \"f\": { \"f\": 1, \"p\": 0 }, \"w\": 1, \"d\": 0, \"s\": 0, \"t\": 0, \"b\": 0, \"a\": 0, \"r\": 0, \"i\": 0, \"l\": [{ \"b\": 0, \"r\": 45, \"o\": 0, \"e\": 1, \"l\": 0, \"p\": 0 }] }] }]", ObjectType.GLSRotation)]
        [TestCase("[{ \"b\": 4, \"g\": 1, \"e\": [{ \"f\": { \"f\": 1, \"p\": 0 }, \"w\": 1, \"d\": 0, \"s\": 0, \"t\": 0, \"b\": 0, \"a\": 0, \"r\": 0, \"i\": 0, \"l\": [{ \"b\": 0, \"p\": 0, \"e\": 1, \"t\": 2.5 }] }] }]", ObjectType.GLSTranslation)]
        public void ReturningWithRawGlsGroupArrayInfersGroupType(string json, ObjectType expectedType)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            SelectionController.Select(source);
            selectionController.Copy();
            LeaveChroMapperWindow();
            ExternalClipboardText = json;

            ReturnToChroMapperWindow();

            var importedGroup = SelectionController.CopiedObjects.OfType<BaseEventBoxGroup>().Single();
            Assert.That(importedGroup.ObjectType, Is.EqualTo(expectedType));
            Assert.That(importedGroup.JsonTime, Is.EqualTo(0));
            Assert.That(importedGroup.ReadOnlyBoxes[0].ReadOnlyEvents.Count, Is.EqualTo(1));
        }

        [TestCase("[{ \"_time\": 4, \"_lineIndex\": 1, \"_lineLayer\": 2, \"_type\": 0, \"_cutDirection\": 1 }]", typeof(BaseNote))]
        [TestCase("[{ \"_time\": 4, \"_lineIndex\": 1, \"_type\": 0, \"_duration\": 2, \"_width\": 1 }]", typeof(BaseObstacle))]
        [TestCase("[{ \"_time\": 4, \"_lineIndex\": 1, \"_lineLayer\": 2, \"_offsetDirection\": 1 }]", typeof(BaseWaypoint))]
        [TestCase("[{ \"_colorType\": 0, \"_headTime\": 4, \"_headLineIndex\": 1, \"_headLineLayer\": 2, \"_headCutDirection\": 1, \"_headControlPointLengthMultiplier\": 1, \"_tailTime\": 6, \"_tailLineIndex\": 2, \"_tailLineLayer\": 1, \"_tailCutDirection\": 0, \"_tailControlPointLengthMultiplier\": 1, \"_sliderMidAnchorMode\": 0 }]", typeof(BaseArc))]
        [TestCase("[{ \"_time\": 4, \"_type\": 1, \"_value\": 5, \"_floatValue\": 1 }]", typeof(BaseEvent))]
        [TestCase("[{ \"_time\": 4, \"_type\": 14, \"_value\": 1, \"_floatValue\": 0 }]", typeof(BaseRotationEvent))]
        [TestCase("[{ \"_time\": 4, \"_type\": 100, \"_value\": 120, \"_floatValue\": 120 }]", typeof(BaseBpmEvent))]
        public void ReturningWithRawV2ArrayInfersEntityType(string json, System.Type expectedType)
        {
            var originalMapVersion = Settings.Instance.MapVersion;
            try
            {
                Settings.Instance.MapVersion = 2;
                var selectionController = Object.FindAnyObjectByType<SelectionController>();
                var source = PlaceUtils.Place(new BaseNote { JsonTime = 2, Type = (int)NoteType.Red });
                SelectionController.Select(source);
                selectionController.Copy();
                LeaveChroMapperWindow();
                ExternalClipboardText = json;

                ReturnToChroMapperWindow();

                Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(1));
                var importedObject = SelectionController.CopiedObjects.Single();
                Assert.That(importedObject.GetType(), Is.EqualTo(expectedType));
                Assert.That(importedObject.JsonTime, Is.EqualTo(0));
            }
            finally
            {
                Settings.Instance.MapVersion = originalMapVersion;
            }
        }

        [Test]
        public void CopyingInnerGlsEventsFromMultipleFilterLanesExportsPartialGroup()
        {
            var partialGroupComment = ClipboardJsonController.GetPartialEventBoxComment();
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.Boxes[0].Events[1]);
            SelectionController.Select(group.Boxes[1].Events[2], true);

            selectionController.Copy();
            LeaveChroMapperWindow();

            Assert.True(ExternalClipboardText.Contains($"{{\n  {partialGroupComment}"));
            var exported = JSON.Parse(ExternalClipboardText.Replace(partialGroupComment, string.Empty));
            Assert.That(exported["b"].AsFloat, Is.EqualTo(24));
            Assert.That(exported["g"].AsInt, Is.EqualTo(7));
            Assert.That(exported["e"].Count, Is.EqualTo(2));
            Assert.That(exported["e"][0]["e"].Count, Is.EqualTo(1));
            Assert.That(exported["e"][0]["e"][0]["b"].AsFloat, Is.EqualTo(1));
            Assert.That(exported["e"][1]["e"].Count, Is.EqualTo(1));
            Assert.That(exported["e"][1]["e"][0]["b"].AsFloat, Is.EqualTo(2));
        }

        [Test]
        public void CopyingInnerFloatFxEventsExportsAndImportsDependentEventCollection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneFloatFxGroup());
            SelectionController.Select(group.Boxes[0].Events[1]);
            SelectionController.Select(group.Boxes[1].Events[0], true);

            selectionController.Copy();
            LeaveChroMapperWindow();

            var withoutMarker = ExternalClipboardText.Replace(
                ClipboardJsonController.GetPartialEventBoxComment(),
                string.Empty);
            var exported = JSON.Parse(withoutMarker);
            Assert.That(exported["vfxEventBoxGroups"].Count, Is.EqualTo(1));
            Assert.That(exported["_fxEventsCollection"]["_fl"].Count, Is.EqualTo(2));
            Assert.That(exported["vfxEventBoxGroups"][0]["e"][0]["l"][0].AsInt, Is.EqualTo(0));
            Assert.That(exported["vfxEventBoxGroups"][0]["e"][1]["l"][0].AsInt, Is.EqualTo(1));

            SelectionController.CopiedObjects.Clear();
            ExternalClipboardText = ExternalClipboardText.Replace("\"v\" : 2", "\"v\" : 9");
            ReturnToChroMapperWindow();

            Assert.That(
                SelectionController.CopiedObjects.OfType<BaseVfxEventEventBoxGroup>().Count(),
                Is.EqualTo(1),
                ExternalClipboardText);
            var importedGroup = SelectionController.CopiedObjects.OfType<BaseVfxEventEventBoxGroup>().Single();
            Assert.That(importedGroup.Boxes.Count, Is.EqualTo(2));
            Assert.That(importedGroup.Boxes[0].Events[0].Value, Is.EqualTo(9));
            Assert.That(SelectionController.CopiedObjects.OfType<BaseFxEventFloat>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void CopyingFloatFxGroupExportsAndImportsDependentEventCollection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneFloatFxGroup());
            SelectionController.Select(group);

            selectionController.Copy();
            LeaveChroMapperWindow();

            var exported = JSON.Parse(ExternalClipboardText);
            Assert.That(exported["vfxEventBoxGroups"].Count, Is.EqualTo(1));
            Assert.That(exported["_fxEventsCollection"]["_fl"].Count, Is.EqualTo(3));

            SelectionController.CopiedObjects.Clear();
            ExternalClipboardText = ExternalClipboardText.Replace("\"v\" : 2", "\"v\" : 9");
            ReturnToChroMapperWindow();

            var importedGroup = SelectionController.CopiedObjects.OfType<BaseVfxEventEventBoxGroup>().Single();
            Assert.That(importedGroup.Boxes[0].Events[1].Value, Is.EqualTo(9));
        }

        [Test]
        public void ReturningWithEditedPartialGlsGroupRebuildsGroupAndInnerEvents()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.Boxes[0].Events[1]);
            SelectionController.Select(group.Boxes[1].Events[2], true);
            selectionController.Copy();
            LeaveChroMapperWindow();
            var editedJson = JSON.Parse(
                ExternalClipboardText.Replace(ClipboardJsonController.GetPartialEventBoxComment(), string.Empty));
            editedJson["e"][0]["e"][0]["s"] = 3.25f;
            ExternalClipboardText = editedJson.ToString(2)
                .Insert(1, "\n  //+ Localized partial event box explanation");

            ReturnToChroMapperWindow();

            var importedGroup = SelectionController.CopiedObjects.OfType<BaseLightColorEventBoxGroup>().Single();
            var importedEvents = SelectionController.CopiedObjects.OfType<BaseLightColorBase>().ToList();
            Assert.That(importedGroup.JsonTime, Is.EqualTo(0));
            Assert.That(importedEvents.Count, Is.EqualTo(2));
            Assert.That(importedEvents[0].Brightness, Is.EqualTo(3.25f));
            Assert.That(importedEvents.All(evt => ReferenceEquals(evt.EventBoxGroupData, importedGroup)), Is.True);
        }

        [Test]
        public void PartialGlsMarkerAfterFirstPropertyDoesNotReplaceCopiedObjects()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var source = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            SelectionController.Select(source);
            selectionController.Copy();
            var copiedObject = SelectionController.CopiedObjects.Single();
            LeaveChroMapperWindow();
            ExternalClipboardText = @"{
  ""b"": 24,
  //+ This marker is intentionally after the first property
  ""g"": 7,
  ""e"": [
    {
      ""f"": { ""f"": 1, ""p"": 2, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 },
      ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
      ""e"": [{ ""b"": 0, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 }]
    }
  ]
}";

            ReturnToChroMapperWindow();

            Assert.That(SelectionController.CopiedObjects.Single(), Is.SameAs(copiedObject));
        }

        [Test]
        public void PastingEditedPartialGlsGroupInsideEventBoxMergesByFilterAndBeat()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.Boxes[0].Events[0]);
            SelectionController.Select(group.Boxes[1].Events[0], true);
            selectionController.Copy();
            LeaveChroMapperWindow();
            var editedJson = JSON.Parse(
                ExternalClipboardText.Replace(ClipboardJsonController.GetPartialEventBoxComment(), string.Empty));
            editedJson["e"][0]["w"] = 4;
            editedJson["e"][0]["e"][0]["s"] = 3.25f;
            editedJson["e"][0]["e"].Add(JSON.Parse(
                "{ \"b\": 1, \"c\": 1, \"s\": 4.25, \"i\": 0, \"f\": 0, \"sb\": 0, \"sf\": 0 }"));
            editedJson["e"][1]["e"].Add(JSON.Parse(
                "{ \"b\": 3, \"c\": 0, \"s\": 5.25, \"i\": 0, \"f\": 0, \"sb\": 0, \"sf\": 0 }"));
            ExternalClipboardText = editedJson.ToString(2)
                .Insert(1, $"\n  {ClipboardJsonController.GetPartialEventBoxComment()}");
            ReturnToChroMapperWindow();
            editModeContext.EditingMode = EditingMode.EventBox;
            Assert.That(Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext, Is.SameAs(group));

            try
            {
                selectionController.Paste();

                var mergedGroup = Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext as BaseLightColorEventBoxGroup;
                Assert.That(mergedGroup, Is.Not.Null);
                Assert.That(mergedGroup.Boxes.Count, Is.EqualTo(2));
                Assert.That(mergedGroup.Boxes[0].BeatDistribution, Is.EqualTo(4));
                Assert.That(mergedGroup.Boxes[0].Events.Length, Is.EqualTo(3));
                Assert.That(mergedGroup.Boxes[0].Events.Single(evt => evt.RelativeJsonTime == 0).Brightness,
                    Is.EqualTo(3.25f));
                Assert.That(mergedGroup.Boxes[0].Events.Single(evt => evt.RelativeJsonTime == 1).Brightness,
                    Is.EqualTo(4.25f));
                Assert.That(mergedGroup.Boxes[0].Events.Single(evt => evt.RelativeJsonTime == 2).Brightness,
                    Is.EqualTo(0.75f));
                Assert.That(mergedGroup.Boxes[1].Events.Length, Is.EqualTo(4));
                Assert.That(mergedGroup.Boxes[1].Events.Single(evt => evt.RelativeJsonTime == 3).Brightness,
                    Is.EqualTo(5.25f));
            }
            finally
            {
                editModeContext.EditingMode = originalMode;
            }
        }

        [Test]
        public void PastingPartialGlsGroupWithEditedFilterAddsANewLane()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            var originalMode = editModeContext.EditingMode;
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.Boxes[0].Events[0]);
            SelectionController.Select(group.Boxes[1].Events[0], true);
            selectionController.Copy();
            LeaveChroMapperWindow();
            var editedJson = JSON.Parse(
                ExternalClipboardText.Replace(ClipboardJsonController.GetPartialEventBoxComment(), string.Empty));
            editedJson["e"][1]["f"]["t"] = 9;
            ExternalClipboardText = editedJson.ToString(2)
                .Insert(1, $"\n  {ClipboardJsonController.GetPartialEventBoxComment()}");
            ReturnToChroMapperWindow();
            editModeContext.EditingMode = EditingMode.EventBox;

            try
            {
                selectionController.Paste();

                var mergedGroup = Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext as BaseLightColorEventBoxGroup;
                Assert.That(mergedGroup, Is.Not.Null);
                Assert.That(mergedGroup.Boxes.Count, Is.EqualTo(3));
                Assert.That(mergedGroup.Boxes[1].IndexFilter.Param1, Is.EqualTo(3));
                Assert.That(mergedGroup.Boxes[1].Events.Length, Is.EqualTo(3));
                Assert.That(mergedGroup.Boxes[2].IndexFilter.Param1, Is.EqualTo(9));
                Assert.That(mergedGroup.Boxes[2].Events.Length, Is.EqualTo(1));
            }
            finally
            {
                editModeContext.EditingMode = originalMode;
            }
        }

        [Test]
        public void CopyingMultipleInnerGlsLanesAlsoPreparesOuterGroupPaste()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.Boxes[0].Events[0]);
            SelectionController.Select(group.Boxes[1].Events[0], true);

            selectionController.Copy();

            var copiedGroup = SelectionController.CopiedObjects.OfType<BaseLightColorEventBoxGroup>().Single();
            Assert.That(copiedGroup.JsonTime, Is.EqualTo(0));
            Assert.That(copiedGroup.Boxes.Count, Is.EqualTo(2));
            Assert.That(copiedGroup.Boxes.All(box => box.Events.Length == 1), Is.True);
        }

        private static BaseLightColorEventBoxGroup CreateTwoLaneColorGroup()
        {
            var group = new BaseLightColorEventBoxGroup
            {
                JsonTime = 24,
                ID = 7,
                Boxes =
                {
                    new BaseLightColorEventBox
                    {
                        IndexFilter = new BaseIndexFilter { Type = 1, Param0 = 2 },
                        Events = new[]
                        {
                            new BaseLightColorBase { RelativeJsonTime = 0, Brightness = 0.25f },
                            new BaseLightColorBase { RelativeJsonTime = 1, Brightness = 0.5f },
                            new BaseLightColorBase { RelativeJsonTime = 2, Brightness = 0.75f }
                        }
                    },
                    new BaseLightColorEventBox
                    {
                        IndexFilter = new BaseIndexFilter { Type = 2, Param1 = 3 },
                        Events = new[]
                        {
                            new BaseLightColorBase { RelativeJsonTime = 0, Brightness = 1.25f },
                            new BaseLightColorBase { RelativeJsonTime = 1, Brightness = 1.5f },
                            new BaseLightColorBase { RelativeJsonTime = 2, Brightness = 1.75f }
                        }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        private static BaseVfxEventEventBoxGroup CreateTwoLaneFloatFxGroup()
        {
            var group = new BaseVfxEventEventBoxGroup
            {
                JsonTime = 24,
                ID = 7,
                Boxes =
                {
                    new BaseVfxEventEventBox
                    {
                        IndexFilter = new BaseIndexFilter { Type = 1, Param0 = 2 },
                        Events = new[]
                        {
                            new BaseFxEventFloat { RelativeJsonTime = 0, Value = 1 },
                            new BaseFxEventFloat { RelativeJsonTime = 1, Value = 2 }
                        }
                    },
                    new BaseVfxEventEventBox
                    {
                        IndexFilter = new BaseIndexFilter { Type = 2, Param1 = 3 },
                        Events = new[]
                        {
                            new BaseFxEventFloat { RelativeJsonTime = 2, Value = 3 }
                        }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        private static BaseEventBoxGroup CreateSingleLaneGlsGroup(string kind)
        {
            return kind switch
            {
                "color" => new BaseLightColorEventBoxGroup
                {
                    JsonTime = 24,
                    ID = 1,
                    Boxes = { new BaseLightColorEventBox() }
                },
                "rotation" => new BaseLightRotationEventBoxGroup
                {
                    JsonTime = 24,
                    ID = 1,
                    Boxes = { new BaseLightRotationEventBox() }
                },
                "translation" => new BaseLightTranslationEventBoxGroup
                {
                    JsonTime = 24,
                    ID = 1,
                    Boxes = { new BaseLightTranslationEventBox() }
                },
                "float" => new BaseVfxEventEventBoxGroup
                {
                    JsonTime = 24,
                    ID = 1,
                    Boxes = { new BaseVfxEventEventBox() }
                },
                _ => null
            };
        }

        private static TGroup PlaceGlsGroup<TGroup>(TGroup group)
            where TGroup : BaseEventBoxGroup
        {
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, false, false, true);
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
            return group;
        }
    }
}
