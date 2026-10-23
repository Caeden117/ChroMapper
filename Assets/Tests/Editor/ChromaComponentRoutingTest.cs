using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaComponentRoutingTest : TestBase
    {
        private const string RootId = "BTSEnvironment.[0]Environment";
        private const string TubeId = "BTSEnvironment.[0]Environment.[22]PillarPair (3).[1]PillarR.[2]LaserR";
        private const string TrackName = "componentRouting";
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameras;
        private AudioTimeSyncController clock;
        private bool previousClockEnabled;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator ComponentPropertiesApplyBeforeNullCancellation()
        {
            yield return Load(ComponentMap(false));
            yield return CheckPlayback(
                new[] { 1f, 2f, 3f, 4f, 5f, 8f },
                new[] { 1f, 2f, 6f, 6f, 6f, 6f });
        }

        // Null stops writes without erasing earlier completed component animation.
        [UnityTest]
        public IEnumerator NullComponentPropertiesRetainEarlierCompletedWrites()
        {
            yield return Load(ComponentMap());
            yield return CheckPlayback(
                new[] { 1f, 2f, 3f, 4f, 5f, 8f },
                new[] { 1f, 2f, 6f, 6f, 6f, 6f });
        }

        private IEnumerator Load(JSONNode data)
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3, data, environmentName: "BTSEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        private IEnumerator CheckPlayback(float[] beats, float[] expected)
        {
            var failures = new List<string>();
            clock.MoveToJsonTime(0f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 0f);
            var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            for (var i = 0; i < beats.Length; i++)
            {
                seconds.SetValue(clock, clock.GetSecondsFromBeat(beats[i]));
                yield return null;
                yield return null;
                CheckValues(failures, beats[i], expected[i]);
            }

            TestUtils.PauseDeterministicPlayback(clock);
            for (var i = beats.Length - 1; i >= 0; i--)
            {
                clock.MoveToJsonTime(beats[i]);
                yield return null;
                yield return null;
                CheckValues(failures, beats[i], expected[i]);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void CheckValues(List<string> failures, float beat, float expected)
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var fog = descriptor.BloomFogParams;
            var tubeTarget = descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == TubeId);
            var tube = tubeTarget.GetComponentInChildren<ParametricBloomFogLightController>(true);
            Assert.That(tube, Is.Not.Null, "The canonical OEM request needs an existing tube component.");
            var values = new[]
            {
                (Name: "attenuation", Value: fog.Attenuation), (Name: "offset", Value: fog.Offset),
                (Name: "startY", Value: fog.StartY), (Name: "height", Value: fog.Height),
                (Name: "colorAlphaMultiplier", Value: tube.ColorAlphaMultiplier),
                (Name: "bloomFogIntensityMultiplier", Value: tube.BloomFogIntensityMultiplier)
            };
            foreach (var value in values)
            {
                if (Mathf.Abs(value.Value - expected) > 0.0001f)
                    failures.Add($"b{beat}: component {value.Name}={value.Value}, native={expected}.");

            }
        }

        private static JSONNode ComponentMap(bool includeNull = true)
        {
            var environment = new JSONArray();
            environment.Add(
                new JSONObject
                {
                    ["id"] = RootId, ["lookupMethod"] = "Exact", ["track"] = TrackName,
                    ["components"] = new JSONObject { ["BloomFogEnvironment"] = Properties(false, "1") }
                });
            environment.Add(
                new JSONObject
                {
                    ["id"] = TubeId, ["lookupMethod"] = "Exact", ["track"] = TrackName,
                    ["components"] = new JSONObject
                    {
                        ["TubeBloomPrePassLight"] = new JSONObject
                        {
                            ["colorAlphaMultiplier"] = 1f, ["bloomFogIntensityMultiplier"] = 1f
                        }
                    }
                });
            var events = new JSONArray();
            var data = new JSONObject
            {
                ["track"] = TrackName, ["duration"] = 1f,
                ["BloomFogEnvironment"] = Properties(false, "[[2,0],[6,1]]"),
                ["TubeBloomPrePassLight"] = Properties(true, "[[2,0],[6,1]]")
            };
            events.Add(new JSONObject { ["b"] = 2f, ["t"] = "AnimateComponent", ["d"] = data });
            if (includeNull)
                events.Add(
                    new JSONObject
                    {
                        ["b"] = 4f, ["t"] = "AnimateComponent",
                        ["d"] = new JSONObject
                        {
                            ["track"] = TrackName,
                            ["BloomFogEnvironment"] = Properties(false, "null"),
                            ["TubeBloomPrePassLight"] = Properties(true, "null")
                        }
                    });

            return new JSONObject
            {
                ["version"] = "3.2.0",
                ["customData"] = new JSONObject { ["environment"] = environment, ["customEvents"] = events }
            };
        }

        private static JSONObject Properties(bool tube, string points)
        {
            var data = new JSONObject();
            foreach (var key in tube
                ? new[] { "colorAlphaMultiplier", "bloomFogIntensityMultiplier" }
                : new[] { "attenuation", "offset", "startY", "height" })
            {
                data[key] = JSON.Parse(points);
            }

            return data;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
