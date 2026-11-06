using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class GLSInitialPlaybackTest : PreviewWorkflowTestBase
    {
        private DeterministicPlaybackClock clock;

        [UnityTest]
        public IEnumerator FirstPlaybackLightsUpWithoutPausing([Values(false, true)] bool savedCursorAtEnd)
        {
            // An on/off pair and a saved end-of-song cursor reproduce Nutella's first-playback sequence.
            var difficulty = JSON.Parse(@"{
                ""version"":""3.3.0"",
                ""lightColorEventBoxGroups"":[{
                    ""b"":1,""g"":23,""e"":[{
                        ""f"":{""f"":1,""p"":1},
                        ""e"":[
                            {""b"":0,""c"":1,""s"":2.1,""i"":0},
                            {""b"":3,""c"":1,""s"":0,""i"":0}
                        ]
                    }]
                }]
            }");
            var editorState = new JSONObject
            {
                ["components"] = new JSONObject
                {
                    ["editingMode"] = new JSONObject { ["mode"] = (int)EditingMode.GLS },
                    ["currentJsonTime"] = new JSONObject { ["value"] = savedCursorAtEnd ? 126f : 0f }
                }
            };
            yield return TestUtils.ReloadMap(
                3,
                difficulty,
                editorState,
                beatsPerMinute: 126f,
                environmentName: "ColliderEnvironment",
                forceSceneReload: true);

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var lights = context.Descriptor.GetComponentsInChildren<LightController>(true)
                .Where(light => light.Kind == LightController.LightKind.Group && light.Type == 23)
                .ToArray();
            Assert.That(lights, Is.Not.Empty, "The fixture must observe Collider's physical group 23 lights.");
            Assert.That(Object.FindAnyObjectByType<LightshowController>().Mode, Is.EqualTo(LightshowMode.Full));
            InitializeVirtualInput(includeKeyboard: false);
            clock = new DeterministicPlaybackClock(context.Atsc);

            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            // Resume() restores the pre-toggle time, which would hide TogglePlaying's end-of-song rewind.
            context.Atsc.TogglePlaying();
            context.Atsc.SongAudioSource.Stop();
            context.Atsc.StopScheduled = true;
            Assert.That(context.Atsc.CurrentJsonTime, Is.EqualTo(0f).Within(0.001f));
            clock.AdvanceToJsonTime(1.125f);
            yield return null;
            Assert.That(context.Atsc.IsPlaying, Is.True);
            var playingBrightness = lights.Max(light => light.Color.a);

            clock.Pause();
            var pausedBrightness = lights.Max(light => light.Color.a);

            Assert.That(pausedBrightness, Is.EqualTo(2.1f).Within(0.001f),
                "The on/off fixture must light up when paused.");
            Assert.That(playingBrightness, Is.EqualTo(pausedBrightness).Within(0.001f),
                "First playback stayed dark until pausing refreshed the lights.");
        }

        [UnityTearDown]
        public IEnumerator RestorePlaybackMap()
        {
            if (clock != null)
            {
                clock.Dispose();
                clock = null;
            }

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            var cameras = Object.FindAnyObjectByType<CameraManager>();
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            TearDownVirtualInput();
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
        }
    }
}
