using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class CensoredTextAlignmentTest : TestBase
    {
        private enum Scenario
        {
            SeekBeforePlaying, PlayingSeek, BackwardSeek, Playback, RestoredFirstFlash,
            RefreshPlayback, AuthoredMap, BasicEventWorkspace, ContinuousPlayback,
            PreviewCycle, InPlaceReload, StoppedInPlaceReload, RestoredAuthoredCursor
        }

        private int previousGridSnapping;
        private bool hadPreviousGridSnapping;
        private EditingMode previousEditingMode;
        private bool previousAnimations;
        private float previousFov;
        private float previousOffset;
        private bool previousAccurateScale;
        private float previousScale;
        private UIMode uiMode;
        private CameraManager cameraManager;

        protected override IEnumerator OnMapLoaded()
        {
            previousGridSnapping = Object.FindAnyObjectByType<AudioTimeSyncController>().GridMeasureSnapping;
            hadPreviousGridSnapping = Settings.NonPersistentSettings.ContainsKey(AudioTimeSyncController.PrecisionSnapName);
            previousEditingMode = Object.FindAnyObjectByType<EditModeContext>().EditingMode;
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            previousAccurateScale = Settings.Instance.NoteJumpSpeedForEditorScale;
            previousScale = Settings.Instance.EditorScale;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterPreviewSeekKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.SeekBeforePlaying);
        }

        [UnityTest]
        public IEnumerator FirstFlashFromPlayingModeKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.PlayingSeek);
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterBackwardSeekKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.BackwardSeek);
        }

        [UnityTest]
        public IEnumerator FirstFlashDuringPlaybackKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.Playback);
        }

        [UnityTest]
        public IEnumerator RestoredFirstFlashCursorKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.RestoredFirstFlash);
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterRefreshingDuringPlaybackKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.RefreshPlayback);
        }

        [UnityTest]
        [Explicit]
        public IEnumerator AuthoredMapFirstFlashKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.AuthoredMap);
        }

        [UnityTest]
        public IEnumerator FirstFlashFromBasicEventWorkspaceKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.BasicEventWorkspace);
        }

        [UnityTest]
        public IEnumerator FirstFlashFromContinuousPlaybackKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.ContinuousPlayback);
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterLeavingPreviewKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.PreviewCycle);
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterInPlaceReloadKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.InPlaceReload);
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterStoppedInPlaceReloadKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.StoppedInPlaceReload);
        }

        [UnityTest]
        public IEnumerator FirstFlashAfterRestoringAuthoredCursorKeepsTextOnBacking()
        {
            yield return CheckFirstFlash(Scenario.RestoredAuthoredCursor);
        }

        private IEnumerator CheckFirstFlash(Scenario scenario)
        {
            var playingBeforeSeek = scenario is not (Scenario.SeekBeforePlaying
                or Scenario.BasicEventWorkspace or Scenario.StoppedInPlaceReload);
            var rewind = scenario == Scenario.BackwardSeek;
            var playback = scenario is Scenario.Playback or Scenario.RefreshPlayback or Scenario.ContinuousPlayback;
            var refresh = scenario == Scenario.RefreshPlayback;
            var originalMap = scenario == Scenario.AuthoredMap;
            var workspace = scenario == Scenario.BasicEventWorkspace;
            var continuous = scenario == Scenario.ContinuousPlayback;
            var cyclePreview = scenario == Scenario.PreviewCycle;
            var reload = scenario is Scenario.InPlaceReload or Scenario.StoppedInPlaceReload;
            float? restoredCursor = scenario switch
            {
                Scenario.RestoredFirstFlash => 99.25f,
                Scenario.RestoredAuthoredCursor => 164.03125f,
                _ => null
            };
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90;
            Settings.Instance.PlayerCameraOffsetZ = 0;
            var fixture = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "CensoredFullMapFixture.json");
            if (originalMap)
                fixture = "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
                    "4b3da (CENSORED!! - Saltyfish)/ExpertPlusStandard.dat";

            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 16, NoteStartBeatOffset = 0,
                LightshowFileName = "MissingTestLightshow.dat"
            };
            JSONObject editorState = null;
            if (restoredCursor.HasValue)
            {
                editorState = new JSONObject
                {
                    ["components"] = new JSONObject
                    {
                        ["currentJsonTime"] = new JSONObject
                        {
                            ["value"] = restoredCursor.Value, ["gridMeasureSnapping"] = 64
                        }
                    }
                };
            }

            if (originalMap)
            {
                var infoPath = PathUtils.Combine(Path.GetDirectoryName(fixture), "Info.dat");
                var info = JSON.Parse(File.ReadAllText(infoPath));
                editorState = info["_customData"]["_editors"]["ChroMapper"]["editorState"].AsObject;
                restoredCursor = editorState["components"]["currentJsonTime"]["value"].AsFloat;
            }

            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(fixture)),
                beatsPerMinute: 195, environmentName: "FitBeatEnvironment", songLengthSeconds: 200,
                difficultyInfo: difficulty, forceSceneReload: true, editorState: editorState);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (restoredCursor.HasValue)
                Assert.That(atsc.CurrentJsonTime, Is.EqualTo(restoredCursor.Value).Within(0.001f),
                    "The regression must exercise the actual saved-cursor restoration path.");

            if (workspace)
            {
                Settings.Instance.NoteJumpSpeedForEditorScale = false;
                Settings.Instance.EditorScale = 3;
                Settings.ManuallyNotifySettingUpdatedEvent("NoteJumpSpeedForEditorScale", false);
                Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.BasicEvent;
            }

            if (playingBeforeSeek)
                uiMode.SetUIMode(UIModeType.Playing, false);

            if (reload)
            {
                atsc.MoveToJsonTime(164.03125f);
                yield return null;
                yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(fixture)),
                    beatsPerMinute: 195, environmentName: "FitBeatEnvironment", songLengthSeconds: 200,
                    difficultyInfo: difficulty);
            }

            if (rewind)
            {
                atsc.MoveToJsonTime(124f);
                yield return null;
            }

            if (!continuous)
                atsc.MoveToJsonTime(98.5f);
            if (!playingBeforeSeek)
                uiMode.SetUIMode(UIModeType.Playing, false);

            cameraManager.SelectCamera(CameraType.Playing);
            if (playback)
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, atsc.CurrentSongBpmTime);
                // Keep production playback callbacks active while the test supplies exact clock samples.
                atsc.enabled = false;
                if (refresh)
                    Object.FindAnyObjectByType<MapLoader>().HardRefreshBeforeEditorStateRestore(
                        Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor);

                var clock = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                var beats = continuous
                    ? Enumerable.Range(0, 398).Select(i => i * 0.25f)
                    : new[] { 98.75f, 99f, 99.25f }.AsEnumerable();
                foreach (var beat in beats)
                {
                    clock.SetValue(atsc, atsc.GetSecondsFromBeat(
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(beat)));
                    yield return null;
                }
            }
            else
                atsc.MoveToJsonTime(99.25f);
            yield return null;
            yield return null;

            if (cyclePreview)
            {
                for (var i = 0; i < 3; i++)
                {
                    uiMode.SetUIMode(UIModeType.Normal, false);
                    yield return null;
                    uiMode.SetUIMode(UIModeType.Playing, false);
                    atsc.MoveToJsonTime(99.25f);
                    yield return null;
                }
            }

            var geometry = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None);
            var text = geometry.Where(c => c.EnvironmentEnhancement.Track?.StartsWith("redcen_") == true)
                .SelectMany(c => c.MpbController.Renderers).ToArray();
            var backing = geometry.Single(c => c.EnvironmentEnhancement.Track == "theaaaa")
                .MpbController.Renderers.Single();
            Assert.That(text, Is.Not.Empty, "The authored text geometry must be loaded.");
            var bounds = text[0].bounds;
            foreach (var renderer in text)
                bounds.Encapsulate(renderer.bounds);

            var camera = cameraManager.CameraControllers[1].Camera;
            var textCenter = camera.WorldToViewportPoint(bounds.center);
            var backingCenter = camera.WorldToViewportPoint(backing.bounds.center);
            Debug.Log($"[CensoredAlignment] scenario={scenario} cursor={restoredCursor} " +
                $"textWorld={bounds.center} backingWorld={backing.bounds.center} " +
                $"textViewport={textCenter} backingViewport={backingCenter}");
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            foreach (var name in new[] { "redcen", "back", "censson", "redcen_10_9" })
            {
                var track = tracks.GetAnimationTrack(name);
                Debug.Log($"[CensoredAlignment] {name} selfLocal={track.Track.SelfTransform.localPosition} " +
                    $"parentLocal={track.Track.ObjectParentTransform.localPosition} " +
                    $"parentWorld={track.Track.ObjectParentTransform.position}");
            }

            var target = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                frame.Apply();
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "CensoredAlignment");
                Directory.CreateDirectory(directory);
                var capture = PathUtils.Combine(directory, NUnit.Framework.TestContext.CurrentContext.Test.Name + ".png");
                File.WriteAllBytes(capture, frame.EncodeToPNG());
                Debug.Log($"[CensoredAlignment] capture={capture}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(frame);
                Object.Destroy(target);
            }

            Assert.That(Mathf.Abs(textCenter.x - backingCenter.x), Is.LessThan(0.02f),
                "The CENSORED text must be horizontally centered on its black backing during the first flash.");
            Assert.That(Mathf.Abs(textCenter.y - backingCenter.y), Is.LessThan(0.02f),
                "The CENSORED text must be vertically centered on its black backing during the first flash.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                if (atsc.IsPlaying)
                    atsc.CancelPlaying();

                atsc.enabled = true;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            atsc.GridMeasureSnapping = previousGridSnapping;
            if (!hadPreviousGridSnapping)
                Settings.NonPersistentSettings.Remove(AudioTimeSyncController.PrecisionSnapName);

            Object.FindAnyObjectByType<EditModeContext>().EditingMode = previousEditingMode;
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            Settings.Instance.NoteJumpSpeedForEditorScale = previousAccurateScale;
            Settings.Instance.EditorScale = previousScale;
            Settings.ManuallyNotifySettingUpdatedEvent("NoteJumpSpeedForEditorScale", previousAccurateScale);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
