using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Beatmap.Containers;
using Beatmap.Info;
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
            var animator = Object.FindAnyObjectByType<TracksManager>()
                .GetAnimationTrack("reviewFogB").GetComponent<Beatmap.Animations.FogAnimator>();
            var ev = BeatSaberSongContainer.Instance.Map.CustomEvents
                .Single(item => item.Type == "AnimateTrack" && item.CustomTrack.Value == "reviewFogB");
            animator.RemoveEvent(ev);
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
            var song = BeatSaberSongContainer.Instance;
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var outgoingDescriptor = context.Descriptor;
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var outgoingTarget = geometry.LoadedContainers.Values.Cast<GeometryContainer>()
                .Single().GetComponent<Beatmap.Animations.ObjectAnimator>().LocalTarget;
            var selector = Object.FindAnyObjectByType<LoadedDifficultySelectController>(FindObjectsInactive.Include);
            var field = typeof(LoadedDifficultySelectController)
                .GetField("setDifficulties", BindingFlags.Instance | BindingFlags.NonPublic);
            var previousDifficulties = field.GetValue(selector);
            var previousDirectory = song.Info.Directory;
            var previousDifficulty = song.MapDifficultyInfo;
            var directory = PathUtils.Combine(Application.temporaryCachePath, $"cm-review-switch-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(PathUtils.Combine(directory, "Review.dat"), "{\"version\":\"3.2.0\"}");
                song.Info.Directory = directory;
                var incoming = new InfoDifficulty(previousDifficulty.ParentSet)
                {
                    BeatmapFileName = "Review.dat",
                    Difficulty = "Expert",
                    EnvironmentNameIndex = previousDifficulty.EnvironmentNameIndex,
                    ColorSchemeIndex = previousDifficulty.ColorSchemeIndex
                };
                field.SetValue(selector, new List<InfoDifficulty> { incoming });
                var switchRoutine = (IEnumerator)typeof(LoadedDifficultySelectController)
                    .GetMethod("SelectDifficulty", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(selector, new object[] { 0 });
                yield return switchRoutine;
                Assert.That(outgoingTarget == null, Is.True,
                    "The real difficulty switch retained a target reparented out of the environment scene.");
                Assert.That(outgoingDescriptor == null, Is.True,
                    "Reusing an enhanced environment retained the outgoing difficulty's native-object changes.");
                Assert.That(context.Descriptor, Is.Not.Null);
                Assert.That(geometry.LoadedContainers, Is.Empty);
                Assert.That(Object.FindAnyObjectByType<TracksManager>()
                    .GetAnimationTrack("reviewChild").Children, Is.Empty);
            }
            finally
            {
                field.SetValue(selector, previousDifficulties);
                song.Info.Directory = previousDirectory;
                song.MapDifficultyInfo = previousDifficulty;
                Directory.Delete(directory, true);
            }
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
