using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "CENSORED!!", mapped by Saltyfish (BeatSaver ID: 4b3da).
    public class CensoredLaserResetParityTest : TestBase
    {
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;
        private float previousPlayerFov;
        private float previousPlayerOffset;
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousPlayerFov = Settings.Instance.PlayerCameraFOV;
            previousPlayerOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator FullMapResetUsesEachPairsPositionAtTheCallback()
        {
            yield return CheckReset("CensoredFullMapFixture.json");
        }

        [UnityTest]
        public IEnumerator ReducedResetUsesEachPairsPositionAtTheCallback()
        {
            yield return CheckReset("CensoredLaserResetFixture.json");
        }

        [UnityTest]
        public IEnumerator ResetSamplesTranslatedRotatedAndScaledParent()
        {
            yield return CheckReset("CensoredLaserResetFixture.json", true);
        }

        [UnityTest]
        public IEnumerator FullMapPlaybackPreservesResetAndReverseSeek()
        {
            yield return CheckReset("CensoredFullMapFixture.json", playback: true);
        }

        [UnityTest]
        public IEnumerator ReducedPlaybackPreservesResetAndReverseSeek()
        {
            yield return CheckReset("CensoredLaserResetFixture.json", playback: true);
        }

        private IEnumerator CheckReset(string fixture, bool transformParent = false, bool playback = false)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90;
            Settings.Instance.PlayerCameraOffsetZ = 0;
            var data = JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fixture)));
            if (transformParent)
            {
                var restore = data["customData"]["customEvents"].Children.Last()["d"];
                restore["scale"] = JSON.Parse("[[2,2,2,0]]");
                restore["position"] = JSON.Parse("[[0,0,7,0]]");
                restore["localRotation"] = JSON.Parse("[[0,30,0,0]]");
            }

            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 16, NoteStartBeatOffset = 0,
                LightshowFileName = "MissingTestLightshow.dat"
            };
            // The loader consumes mutable custom-event data. Keep the authored oracle independent of it.
            var oracleData = data.Clone();
            yield return TestUtils.ReloadMap(3, data, beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", songLengthSeconds: 110, forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var pairs = Object.FindObjectsByType<LightPairRotation>(FindObjectsSortMode.None)
                .Where(pair => pair.name.StartsWith("RotatingLasersPair")).OrderBy(pair => pair.name).ToArray();
            Assert.That(pairs.Length, Is.EqualTo(8));
            if (playback)
            {
                playbackClock = atsc;
                previousClockEnabled = atsc.enabled;
                atsc.enabled = false;
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, 147.906f);
            }

            foreach (var beat in new[] { 147.906f, 159.8f, 159.9f, 159.96f, 163.9f, 164.031f, 147.906f, 164.031f })
            {
                if (playback)
                {
                    currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(beat));
                }
                else
                {
                    atsc.MoveToJsonTime(beat);
                }

                yield return null;
                yield return null;
                if (beat != 147.906f && beat != 163.9f && beat != 164.031f)
                    continue;

                // Native FitBeat pairs start at Z=44,40,...16 with offset scale 10.
                // The parent has returned to scale one at 163.875 before this unlocked reset.
                for (var i = 0; i < pairs.Length; i++)
                {
                    var pair = pairs[i];
                    var nativeZ = 44f - i * 4f;
                    if (transformParent && i < 2 && beat >= 163.875f)
                    {
                        nativeZ = (Quaternion.Euler(0, 30, 0) * new Vector3(0, -5, nativeZ) * 2f).z + 7f;
                    }

                    Assert.That(pair.transform.position.z, Is.EqualTo(nativeZ).Within(0.001f));
                    for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        var side = pair.Transforms[sideIndex];
                        var angle = GetNativeAngle(oracleData, sideIndex, beat, 44f - i * 4f,
                            fixture == "CensoredFullMapFixture.json" || i < 2);
                        var expected = side.Start * Quaternion.Euler(pair.RotationVector * angle);
                        var error = Quaternion.Angle(side.Transform.localRotation, expected);
                        Debug.Log($"[CensoredLaserReset] {fixture} beat={beat} pair={i} side={sideIndex} z={nativeZ} angle={angle} error={error}");
                        Assert.That(error, Is.LessThan(0.1f),
                            $"{fixture}: {pair.name} side {sideIndex} reset lost its callback-time Z offset.");
                    }
                }

                if (fixture == "CensoredFullMapFixture.json" && !playback)
                {
                    Capture(cameraManager.CameraControllers[1].Camera, beat);
                }
            }
        }

        private static float GetNativeAngle(JSONNode data, int sideIndex, float beat, float rawZ, bool enhanced)
        {
            var seconds = beat * 60f / 195f;
            var lastSeconds = 0f;
            var angle = 0f;
            var speed = 0f;
            var sign = sideIndex == 0 ? 1f : -1f;
            foreach (var evt in data["basicBeatmapEvents"].Children)
            {
                if (evt["et"].AsInt != 12 + sideIndex)
                    continue;

                var frame = Mathf.CeilToInt(evt["b"].AsFloat * 60f / 195f * 90f - 0.0001f);
                var callback = frame / 90f;
                if (callback > seconds)
                    break;

                angle += (callback - lastSeconds) * speed;
                lastSeconds = callback;
                var custom = evt["customData"];
                var value = evt["i"].AsInt;
                if (value == 0)
                {
                    speed = 0f;
                    if (!custom["lockRotation"].AsBool)
                        angle = 0f;

                    continue;
                }

                if (value < 0)
                    continue;

                if (!custom["lockRotation"].AsBool)
                {
                    angle = sign * (frame % 360 + GetNativeZ(data, rawZ, callback, enhanced) * 10f);
                }

                var direction = custom["direction"].IsNumber
                    ? (custom["direction"].AsInt == 0 ? -sign : sign)
                    : sign;
                speed = (custom["speed"].IsNumber ? custom["speed"].AsFloat : value) * 20f * direction;
            }

            return angle + (seconds - lastSeconds) * speed;
        }

        private static float GetNativeZ(JSONNode data, float rawZ, float seconds, bool enhanced)
        {
            if (!enhanced)
                return rawZ;

            var scale = Vector3.one;
            var rotation = Quaternion.identity;
            var positionZ = 0f;
            foreach (var evt in data["customData"]["customEvents"].Children)
            {
                if (evt["b"].AsFloat * 60f / 195f > seconds)
                    continue;

                var properties = evt["d"];
                if (evt["t"].Value != "AnimateTrack" || properties["track"].Value != "bruhaa")
                    continue;

                if (properties["scale"].IsArray)
                {
                    var point = properties["scale"][0];
                    scale = new Vector3(point[0].AsFloat, point[1].AsFloat, point[2].AsFloat);
                }

                if (properties["localRotation"].IsArray)
                {
                    var point = properties["localRotation"][0];
                    rotation = Quaternion.Euler(point[0].AsFloat, point[1].AsFloat, point[2].AsFloat);
                }

                if (properties["position"].IsArray)
                    positionZ = properties["position"][0][2].AsFloat;
            }

            return (rotation * Vector3.Scale(new Vector3(0, -5, rawZ), scale)).z + positionZ;
        }

        private static void Capture(Camera camera, float beat)
        {
            var target = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "CensoredLaserResetParityTest");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(PathUtils.Combine(directory, $"full-map-{beat.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}.png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        [UnityTearDown]
        public IEnumerator RestorePreview()
        {
            if (playbackClock != null)
            {
                if (playbackClock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(playbackClock);
                }

                playbackClock.enabled = previousClockEnabled;
                playbackClock = null;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousPlayerFov;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerOffset;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
        }
    }
}
