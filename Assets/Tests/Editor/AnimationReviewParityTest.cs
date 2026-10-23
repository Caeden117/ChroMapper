using System.Collections;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class AnimationReviewParityTest : PreviewWorkflowTestBase
    {
        [UnityTest]
        public IEnumerator DeletingInactiveLegacyFogEventsPreservesTheAssignedTrack()
        {
            yield return TestUtils.ReloadMap(2, JSON.Parse(@"{
                ""_version"":""2.2.0"", ""_customData"":{""_customEvents"":[
                    {""_time"":0,""_type"":""AssignFogTrack"",""_data"":{""_track"":""reviewFogA""}},
                    {""_time"":0,""_type"":""AnimateTrack"",""_data"":{""_track"":""reviewFogA"",""_attenuation"":[0.001]}},
                    {""_time"":4,""_type"":""AnimateTrack"",""_data"":{""_track"":""reviewFogA"",""_attenuation"":[0.003]}},
                    {""_time"":8,""_type"":""AssignFogTrack"",""_data"":{""_track"":""reviewFogUnused""}},
                    {""_time"":8,""_type"":""AssignFogTrack"",""_data"":{""_track"":""reviewFogB""}},
                    {""_time"":8,""_type"":""AnimateTrack"",""_data"":{""_track"":""reviewFogB"",""_attenuation"":[0.002]}}
                ]}
            }"), environmentName: "BillieEnvironment");
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            atsc.MoveToJsonTime(4f);
            atsc.MoveToJsonTime(9f);
            Assert.That(context.Descriptor.BloomFogParams.Attenuation, Is.EqualTo(0.002f));
            atsc.MoveToJsonTime(0f);
            Assert.That(context.Descriptor.BloomFogParams.Attenuation, Is.EqualTo(0.001f));
            var ev = BeatSaberSongContainer.Instance.Map.CustomEvents
                .Single(item => item.Type == "AnimateTrack" && item.CustomTrack.Value == "reviewFogB");
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(ev, "Delete inactive fog animation"), true);
            Assert.That(context.Descriptor.BloomFogParams.Attenuation, Is.EqualTo(0.001f),
                "Deleting an inactive fog track's animation overwrote the active track's value.");
        }

        [UnityTest]
        public IEnumerator V2DisableNoteLookPreservesAuthoredFacing()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, JSON.Parse(@"{
                ""_version"":""2.2.0"", ""_notes"":[
                    {""_time"":20,""_lineIndex"":0,""_lineLayer"":0,""_type"":0,""_cutDirection"":8,
                     ""_customData"":{""_track"":""reviewNoLook"",""_fake"":true,""_disableNoteLook"":true,
                        ""_noteJumpStartBeatOffset"":10,""_animation"":{
                            ""_definitePosition"":[[10,0,12,0],[10,0,12,1]]}}},
                    {""_time"":20,""_lineIndex"":0,""_lineLayer"":0,""_type"":1,""_cutDirection"":8,
                     ""_customData"":{""_track"":""reviewLook"",""_fake"":true,
                        ""_noteJumpStartBeatOffset"":10,""_animation"":{
                            ""_definitePosition"":[[10,0,12,0],[10,0,12,1]]}}}
                ]
            }"));
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            var cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Playing);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(19.99f);
            yield return null;
            var notes = Object.FindObjectsByType<NoteContainer>(FindObjectsSortMode.None);
            var noLook = notes.Single(note => note.NoteData?.CustomTrack?.Value == "reviewNoLook");
            var look = notes.Single(note => note.NoteData?.CustomTrack?.Value == "reviewLook");
            noLook.Animator.LateUpdate();
            look.Animator.LateUpdate();
            Assert.That(Quaternion.Angle(noLook.DirectionTarget.rotation, Quaternion.identity),
                Is.LessThan(0.1f), "V2 _disableNoteLook still rotated the note toward the camera.");
            var ray = (look.DirectionTarget.position - cameras.SelectedCameraController.transform.position).normalized;
            Assert.That(Vector3.Dot(look.DirectionTarget.forward, ray), Is.GreaterThan(0.99f),
                "The control note must still face the player.");
        }

        [UnityTest]
        public IEnumerator ChainHeadsPreserveAuthoredFacingNearHitBeat()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(@"{
                ""version"":""3.2.0"", ""colorNotes"":[
                    {""b"":20,""x"":0,""y"":2,""c"":0,""d"":1,""a"":0},
                    {""b"":20,""x"":3,""y"":2,""c"":1,""d"":1,""a"":30},
                    {""b"":20,""x"":0,""y"":0,""c"":0,""d"":1,""a"":0},
                    {""b"":20,""x"":3,""y"":0,""c"":1,""d"":1,""a"":0}
                ], ""burstSliders"":[
                    {""b"":20,""x"":0,""y"":2,""c"":0,""d"":1,
                     ""tb"":20.5,""tx"":0,""ty"":1,""sc"":4,""s"":1},
                    {""b"":20,""x"":3,""y"":2,""c"":1,""d"":1,
                     ""tb"":20.5,""tx"":3,""ty"":1,""sc"":4,""s"":1}
                ]
            }"));
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var cameras = Object.FindAnyObjectByType<CameraManager>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            foreach (var mode in new[] { UIModeType.Preview, UIModeType.Playing })
            {
                uiMode.SetUIMode(mode, false);
                cameras.SelectCamera(
                    mode == UIModeType.Playing
                        ? CameraType.Playing
                        : CameraType.Editing);
                foreach (var beat in new[] { 19.9f, 19.99f, 19.999f })
                {
                    atsc.MoveToJsonTime(beat);
                    yield return null;
                    var notes = Object.FindObjectsByType<NoteContainer>(FindObjectsSortMode.None);
                    var heads = notes.Where(note => note.NoteData.Chains.Count > 0).ToArray();
                    var normalNotes = notes.Where(note => note.NoteData.Chains.Count == 0).ToArray();
                    Assert.That(heads.Length, Is.EqualTo(2));
                    Assert.That(normalNotes.Length, Is.EqualTo(2));
                    foreach (var normal in normalNotes)
                    {
                        normal.Animator.LateUpdate();
                        var authored = normal.DirectionTarget.parent.rotation
                            * Quaternion.Euler(normal.DirectionTargetEuler);
                        Assert.That(
                            Quaternion.Angle(normal.DirectionTarget.rotation, authored),
                            Is.GreaterThan(1f),
                            $"The normal-note control must still turn toward the player in {mode} at beat {beat}.");
                    }

                    foreach (var head in heads)
                    {
                        head.Animator.LateUpdate();
                        var authored = head.DirectionTarget.parent.rotation
                            * Quaternion.Euler(head.DirectionTargetEuler);
                        Assert.That(
                            Quaternion.Angle(head.DirectionTarget.rotation, authored),
                            Is.LessThan(0.1f),
                            $"Chain head at x={head.NoteData.PosX} must not turn toward the player in {mode} at beat {beat}.");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator SameEnvironmentDifficultySwitchReleasesOutgoingEnhancements()
        {
            yield return TestUtils.ReloadMap(3, JSON.Parse(@"{
                ""version"":""3.2.0"", ""customData"":{
                    ""customEvents"":[{""b"":0,""t"":""AssignTrackParent"",
                        ""d"":{""parentTrack"":""reviewParent"",""childrenTracks"":[""reviewChild""]}}],
                    ""environment"":[{""geometry"":{""type"":""Cube"",""material"":""standard""},
                        ""track"":""reviewChild""}]
                }
            }"), environmentName: "BillieEnvironment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var outgoingDescriptor = context.Descriptor;
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var outgoingTarget = geometry.LoadedContainers.Values.Cast<GeometryContainer>()
                .Single().GetComponent<Beatmap.Animations.ObjectAnimator>().LocalTarget;
            yield return TestUtils.SwitchToEmptyDifficulty();

            Assert.That(
                outgoingTarget == null,
                Is.True,
                "The real difficulty switch retained a target reparented out of the environment scene.");
            Assert.That(
                outgoingDescriptor == null,
                Is.True,
                "Reusing an enhanced environment retained the outgoing difficulty's native-object changes.");
            Assert.That(context.Descriptor, Is.Not.Null);
            Assert.That(geometry.LoadedContainers, Is.Empty);
            Assert.That(
                Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack("reviewChild").Children,
                Is.Empty);
        }

        [UnityTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
