using System.Collections;
using System.Linq;
using System.Reflection;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class AnimationBpmTimingRefreshTest : PreviewWorkflowTestBase
    {
        private static readonly PropertyInfo playbackSeconds = typeof(AudioTimeSyncController)
            .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private AudioTimeSyncController atsc;

        [UnityTest]
        public IEnumerator BpmEditRetimesPathAnimationWithoutLeavingPreview()
        {
            yield return LoadFixture();
            yield return AssertPathScaleAtSongTime(12f, 2f);
            StopPlayback();
            ChangeBpm();
            yield return AssertPathScaleAtSongTime(8f, 2f);
        }

        [UnityTest]
        public IEnumerator BpmEditRetimesPathAnimationWhenReenteringPreview()
        {
            yield return LoadFixture();
            yield return AssertPathScaleAtSongTime(12f, 2f);
            StopPlayback();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            ChangeBpm();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            yield return AssertPathScaleAtSongTime(8f, 2f);
        }

        [UnityTest]
        public IEnumerator BpmEditUndoAndRedoRetimesPathAnimation()
        {
            yield return LoadFixture();
            ChangeBpm();
            yield return AssertPathScaleAtSongTime(8f, 2f);
            StopPlayback();
            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            Assert.That(actions.Undo(), Is.Not.Null);
            yield return AssertPathScaleAtSongTime(12f, 2f);
            StopPlayback();
            Assert.That(actions.Redo(), Is.Not.Null);
            yield return AssertPathScaleAtSongTime(8f, 2f);
        }

        [UnityTest]
        public IEnumerator V2BpmEditRetimesPathAnimationAndKeepsLegacyFogOnBeat()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, JSON.Parse(@"{
                ""_version"":""2.6.0"",
                ""_events"":[{""_time"":0,""_type"":100,""_floatValue"":100},
                    {""_time"":4,""_type"":100,""_floatValue"":100}],
                ""_notes"":[{""_time"":16,""_lineIndex"":0,""_lineLayer"":0,""_type"":0,""_cutDirection"":8,
                    ""_customData"":{""_track"":""bpmPath"",""_fake"":true,""_noteJumpStartBeatOffset"":10}}],
                ""_customData"":{""_customEvents"":[
                    {""_time"":0,""_type"":""AssignPathAnimation"",""_data"":{""_track"":""bpmPath"",""_scale"":[1,1,1]}},
                    {""_time"":8,""_type"":""AssignPathAnimation"",""_data"":{""_track"":""bpmPath"",""_duration"":8,""_scale"":[3,3,3]}},
                    {""_time"":0,""_type"":""AssignFogTrack"",""_data"":{""_track"":""bpmFog""}},
                    {""_time"":0,""_type"":""AnimateTrack"",""_data"":{""_track"":""bpmFog"",""_duration"":16,
                        ""_attenuation"":[[0.001,0],[0.005,1]]}}
                ]}
            }"), beatsPerMinute: 100, environmentName: "BillieEnvironment");
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            atsc.MoveToJsonTime(9f);
            yield return AssertPathScaleAtSongTime(12f, 2f);
            StopPlayback();
            ChangeBpm();
            yield return AssertPathScaleAtSongTime(8f, 2f);
            Assert.That(Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams.Attenuation,
                Is.EqualTo(0.001f + atsc.CurrentJsonTime * 0.00025f).Within(0.00004f));
        }

        [UnityTest]
        public IEnumerator BpmEditRetimesWallLifetimeSpanningTheChange()
        {
            yield return LoadFixture();
            yield return AssertWallScaleAtSongTime(9f);
            StopPlayback();
            ChangeBpm();
            yield return AssertWallScaleAtSongTime(8f);
        }

        [UnityTest]
        public IEnumerator BpmEditPreservesBeatTimedTracksFogAndTubeBloomDuringPlayback()
        {
            yield return LoadFixture();
            yield return AssertComponentOutputAtSongTime(12f, 12f);
            StopPlayback();
            ChangeBpm();
            yield return AssertComponentOutputAtSongTime(8f, 12f);
        }

        [UnityTest]
        public IEnumerator BpmEditExtendsReachableTrackFogAndTubeBloomRepeats()
        {
            yield return LoadFixture(repeating: true);
            yield return AssertComponentOutputAtSongTime(86f, 86f, repeating: true);
            StopPlayback();
            atsc.MoveToJsonTime(4f);
            yield return null;
            ChangeBpm();
            yield return AssertComponentOutputAtSongTime(77f, 150f, repeating: true);
            StopPlayback();
            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            Assert.That(actions.Undo(), Is.Not.Null);
            yield return AssertComponentOutputAtSongTime(86f, 86f, repeating: true);
            StopPlayback();
            Assert.That(actions.Redo(), Is.Not.Null);
            yield return AssertComponentOutputAtSongTime(77f, 150f, repeating: true);
        }

        private IEnumerator LoadFixture(bool repeating = false)
        {
            Settings.Instance.Animations = true;
            var difficulty = JSON.Parse(@"{
                ""version"":""3.3.0"",
                ""bpmEvents"":[{""b"":0,""m"":100},{""b"":4,""m"":100}],
                ""colorNotes"":[{""b"":16,""x"":0,""y"":0,""c"":0,""d"":8,
                    ""customData"":{""track"":""bpmPath"",""fake"":true,
                        ""noteJumpStartBeatOffset"":10}}],
                ""obstacles"":[{""b"":2,""x"":0,""y"":0,""d"":20,""w"":1,""h"":1,
                    ""customData"":{""fake"":true,""noteJumpStartBeatOffset"":10,
                        ""animation"":{""scale"":[[1,1,1,0],[3,3,3,1]]}}}],
                ""customData"":{
                    ""environment"":[
                        {""id"":""[0]Environment"",""lookupMethod"":""EndsWith"",""track"":""bpmFog""},
                        {""geometry"":{""type"":""Cube"",""material"":""standard""},
                            ""track"":""bpmLight"",""components"":{
                                ""ILightWithId"":{""type"":1,""lightID"":9999},
                                ""TubeBloomPrePassLight"":{""colorAlphaMultiplier"":1,""bloomFogIntensityMultiplier"":1}}}
                    ],
                    ""customEvents"":[
                        {""b"":0,""t"":""AssignPathAnimation"",""d"":{""track"":""bpmPath"",""scale"":[1,1,1]}},
                        {""b"":8,""t"":""AssignPathAnimation"",""d"":{""track"":""bpmPath"",""duration"":8,""scale"":[3,3,3]}},
                        {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""bpmLight"",""duration"":16,
                            ""position"":[[0,0,0,0],[16,0,0,1]]}},
                        {""b"":0,""t"":""AnimateComponent"",""d"":{""track"":""bpmLight"",""duration"":16,
                            ""TubeBloomPrePassLight"":{""colorAlphaMultiplier"":[[1,0],[5,1]],
                                ""bloomFogIntensityMultiplier"":[[1,0],[9,1]]}}},
                        {""b"":0,""t"":""AnimateComponent"",""d"":{""track"":""bpmFog"",""duration"":16,
                            ""BloomFogEnvironment"":{""attenuation"":[[0.001,0],[0.005,1]]}}}
                    ]
                }
            }");
            if (repeating)
            {
                foreach (var ev in difficulty["customData"]["customEvents"].Children)
                {
                    if (ev["t"].Value is "AnimateTrack" or "AnimateComponent")
                    {
                        ev["d"]["repeat"] = 20;
                    }
                }
            }

            yield return TestUtils.ReloadMap(3, difficulty, beatsPerMinute: 100, environmentName: "BillieEnvironment");
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(9f);
            yield return null;
        }

        private void ChangeBpm()
        {
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);
            var ev = collection.MapObjects.Single(item => item.JsonTime == 4f);
            collection.RefreshPool();
            Assert.That(collection.LoadedContainers.ContainsKey(ev), Is.True);
            var container = (BpmEventContainer)collection.LoadedContainers[ev];
            BeatmapBPMChangeInputController.ChangeBpm(container, "200");
        }

        // Keep production playback callbacks active while using a clock independent of native audio.
        private IEnumerator AdvancePlayback(float songBpmTime)
        {
            if (!atsc.IsPlaying)
            {
                atsc.MoveToSongBpmTime(songBpmTime);
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
            }

            playbackSeconds.SetValue(atsc, atsc.GetSecondsFromBeat(songBpmTime));
            yield return null;
            yield return null;
            Assert.That(atsc.IsPlaying, Is.True);
            Assert.That(atsc.SongAudioSource.isPlaying, Is.False);
        }

        private IEnumerator AssertPathScaleAtSongTime(float songBpmTime, float expectedScale)
        {
            yield return AdvancePlayback(songBpmTime);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var data = collection.MapObjects.Single(item => item.CustomTrack?.Value == "bpmPath");
            Assert.That(collection.LoadedContainers.TryGetValue(data, out var loaded), Is.True,
                $"The path note is absent at song time {atsc.CurrentSongBpmTime}, authored beat {atsc.CurrentJsonTime}. "
                + $"Its song-time window is {data.SpawnSongBpmTime}..{data.DespawnSongBpmTime}.");
            var note = (NoteContainer)loaded;
            Assert.That(note.Animator.LocalTarget.localScale.x, Is.EqualTo(expectedScale).Within(0.03f),
                "The cached AssignPathAnimation transition still uses the pre-edit song-time window.");
        }

        private IEnumerator AssertWallScaleAtSongTime(float songBpmTime)
        {
            yield return AdvancePlayback(songBpmTime);
            var wall = Object.FindObjectsByType<ObstacleContainer>(FindObjectsSortMode.None).Single();
            var data = wall.ObstacleData;
            var end = data.SongBpmTime + data.DurationSongBpmTime + data.HalfJumpDuration;
            var progress = (atsc.CurrentSongBpmTime - data.SpawnSongBpmTime) / (end - data.SpawnSongBpmTime);
            Assert.That(wall.Animator.LocalTarget.localScale.x, Is.EqualTo(1f + 2f * progress).Within(0.01f),
                "The cached obstacle animation lifetime still uses its pre-edit endpoint.");
        }

        private IEnumerator AssertComponentOutputAtSongTime(float songBpmTime, float expectedJsonTime,
            bool repeating = false)
        {
            yield return AdvancePlayback(songBpmTime);
            Assert.That(atsc.CurrentJsonTime, Is.EqualTo(expectedJsonTime).Within(0.15f));
            var light = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Single(item => item.EnvironmentEnhancement?.Track == "bpmLight");
            var controller = light.GetComponentInChildren<ParametricBloomFogLightController>(true);
            var jsonTime = atsc.CurrentJsonTime;
            var animationTime = repeating ? jsonTime % 16f : jsonTime;
            Assert.That(light.GetComponent<Beatmap.Animations.ObjectAnimator>().LocalTarget.position.x,
                Is.EqualTo(animationTime).Within(0.15f));
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(1f + animationTime / 4f).Within(0.04f));
            Assert.That(controller.BloomFog.IntensityMultiplier, Is.EqualTo(1f + animationTime / 2f).Within(0.08f));
            Assert.That(Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams.Attenuation,
                Is.EqualTo(0.001f + animationTime * 0.00025f).Within(0.00004f));
        }

        private void StopPlayback()
        {
            if (atsc != null && atsc.IsPlaying)
            {
                atsc.TogglePlaying();
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            StopPlayback();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
