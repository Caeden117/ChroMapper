using System.Collections;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Visual
{
    public class VNJSPreviewTest : TestBase
    {
        // OEM 1.44.1 VariableMovementDataProvider moves a jumping note by VNJS multiplied by elapsed song seconds.
        private const float ExpectedActiveVNJS = 20f;
        private const float ExpectedTravelPerBeat = 12f;
        private const float Delta = 0.001f;

        // V3VNJSPreviewMovesNoteLikeOem proves the BeatToTheFuture flat extension reaches the same live preview path as OEM V4 data.
        [UnityTest]
        public IEnumerator V3VNJSPreviewMovesNoteLikeOem()
        {
            yield return TestUtils.ReloadMap(3, CreateV3Difficulty());
            yield return AssertLoadedVNJSMovesNote(3);
        }

        // V4VNJSPreviewMovesNoteLikeOem proves indexed OEM VNJS data changes note movement rather than only populating its editor lane.
        [UnityTest]
        public IEnumerator V4VNJSPreviewMovesNoteLikeOem()
        {
            yield return TestUtils.ReloadMap(3, CreateV4Difficulty());
            yield return AssertLoadedVNJSMovesNote(4);
        }

        // VNJSPreviewTest replaces the mapper scene twice, so restore the canonical empty V3 scene for later fixtures.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // Both format cases use an identical note and event so any difference is isolated to deserialization and preview wiring.
        private static IEnumerator AssertLoadedVNJSMovesNote(int expectedMajorVersion)
        {
            var songContainer = BeatSaberSongContainer.Instance;
            Assert.AreEqual(expectedMajorVersion, songContainer.Map.MajorVersion);
            Assert.AreEqual(1, songContainer.Map.NJSEvents.Count);

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var provider = Object.FindAnyObjectByType<VariableNJSProvider>();
            var noteGrid = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            uiMode.SetUIMode(UIModeType.Preview, false);
            yield return null;

            try
            {
                atsc.MoveToJsonTime(6f);
                yield return null;

                Assert.AreEqual(ExpectedActiveVNJS, provider.NoteJumpSpeed, Delta);
                var note = songContainer.Map.Notes[0];
                Assert.IsTrue(noteGrid.LoadedContainers.ContainsKey(note));
                var noteContainer = (NoteContainer)noteGrid.LoadedContainers[note];
                var positionAtBeatSix = noteContainer.transform.position.z;

                atsc.MoveToJsonTime(7f);
                yield return null;

                Assert.AreEqual(ExpectedActiveVNJS, provider.NoteJumpSpeed, Delta);
                Assert.IsTrue(noteGrid.LoadedContainers.ContainsKey(note));
                noteContainer = (NoteContainer)noteGrid.LoadedContainers[note];
                var actualTravel = positionAtBeatSix - noteContainer.transform.position.z;
                Assert.AreEqual(
                    ExpectedTravelPerBeat,
                    actualTravel,
                    Delta,
                    "VNJS preview note travel diverged from OEM VNJS * elapsed-song-seconds movement.");
            }
            finally
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // V3 stores BeatToTheFuture VNJS as one flat BeatSaver-safe array under customData.
        private static JSONNode CreateV3Difficulty() => JSON.Parse(@"
        {
          ""version"": ""3.3.0"",
          ""colorNotes"": [ { ""b"": 8, ""x"": 1, ""y"": 1, ""a"": 0, ""c"": 0, ""d"": 1 } ],
          ""customData"": {
            ""njsEvents"": [ { ""b"": 4, ""d"": 10, ""p"": 0, ""e"": 0 } ]
          }
        }");

        // V4 keeps the same logical event in its OEM beat-index and common-data arrays.
        private static JSONNode CreateV4Difficulty() => JSON.Parse(@"
        {
          ""version"": ""4.1.0"",
          ""colorNotes"": [ { ""b"": 8, ""r"": 0, ""i"": 0 } ],
          ""colorNotesData"": [ { ""x"": 1, ""y"": 1, ""c"": 0, ""d"": 1, ""a"": 0 } ],
          ""njsEvents"": [ { ""b"": 4, ""i"": 0 } ],
          ""njsEventData"": [ { ""p"": 0, ""e"": 0, ""d"": 10 } ]
        }");
    }
}
