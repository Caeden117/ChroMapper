using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class LaneCountEditorStateTest : TestBase
    {
        [UnityTest]
        public IEnumerator LaneCountSurvivesSaveAndReloadWithV2Info()
        {
            yield return SaveAndReloadLaneCount(V2Info.Version, 6);
        }

        [UnityTest]
        public IEnumerator LaneCountSurvivesSaveAndReloadWithV4Info()
        {
            yield return SaveAndReloadLaneCount(V4Info.Version, 7);
        }

        [UnityTest]
        public IEnumerator LoadedGridEdgesMatchGridAfterChangingLaneCount([Values(12, 13)] int laneCount)
        {
            yield return SaveAndReloadLaneCount(V2Info.Version, laneCount);
            var laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            var laneField = typeof(PlacementLaneController).GetField("lane", BindingFlags.Instance | BindingFlags.NonPublic);
            var gridLane = (GridLane)laneField.GetValue(laneController);
            var loadedXY = RenderGrid(gridLane.XY.Grid);
            var loadedXZ = RenderGrid(gridLane.XZ.Grid);

            var laneCountInput = GetLaneCountInput();
            laneCountInput.Value = laneCount + 1;
            laneCountInput.Value = laneCount;
            yield return null;

            var editedXY = RenderGrid(gridLane.XY.Grid);
            var editedXZ = RenderGrid(gridLane.XZ.Grid);
            var changedPixels = new[]
            {
                CountChangedPixels(loadedXY, editedXY),
                CountChangedPixels(loadedXZ, editedXZ)
            };
            Assert.That(
                changedPixels,
                Is.All.EqualTo(0),
                "Reloading must render the same XY and XZ grid edges as editing the lane count and changing it back.");
        }

        [UnityTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            GetLaneCountInput().Value = 4;
        }

        private static IEnumerator SaveAndReloadLaneCount(string infoVersion, int laneCount)
        {
            var laneCountInput = GetLaneCountInput();
            var textInput = laneCountInput.GetComponentInChildren<TMP_InputField>(true);
            Assert.That(textInput, Is.Not.Null);
            var laneCountText = laneCount.ToString(CultureInfo.InvariantCulture);
            textInput.text = laneCountText;
            textInput.onEndEdit.Invoke(laneCountText);

            var laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            Assert.That(laneController.LaneCount, Is.EqualTo(laneCount), "The top-bar edit must change the note lanes before saving.");

            var songContainer = BeatSaberSongContainer.Instance;
            var info = songContainer.Info;
            var map = songContainer.Map;
            var originalDirectory = info.Directory;
            var originalVersion = info.Version;
            var originalMapPath = map.DirectoryAndFile;
            var saveDirectory = PathUtils.Combine(Path.GetTempPath(), "ChroMapper Lane Count Tests", Guid.NewGuid().ToString("N"));
            JSONObject savedEditorState;
            JSONNode savedMap;

            try
            {
                Directory.CreateDirectory(saveDirectory);
                info.Directory = saveDirectory;
                info.Version = infoVersion;
                map.DirectoryAndFile = PathUtils.Combine(saveDirectory, "ExpertPlus.dat");

                var saveController = Object.FindAnyObjectByType<AutoSaveController>();
                Assert.That(saveController, Is.Not.Null);
                saveController.Save();
                yield return new WaitUntil(() => !saveController.IsSaving);

                var savedInfo = BeatSaberSongUtils.GetInfoFromFolder(saveDirectory);
                Assert.That(savedInfo, Is.Not.Null);
                Assert.That(savedInfo.Version, Is.EqualTo(infoVersion));
                savedEditorState = savedInfo.CustomEditorsData.GetEditorData("editorState").AsObject;
                Assert.That(savedEditorState, Is.Not.Null);
                savedMap = BeatSaberSongUtils.GetNodeFromFile(map.DirectoryAndFile);
                Assert.That(savedMap, Is.Not.Null);
            }
            finally
            {
                info.Directory = originalDirectory;
                info.Version = originalVersion;
                map.DirectoryAndFile = originalMapPath;
                if (Directory.Exists(saveDirectory))
                    Directory.Delete(saveDirectory, true);
            }

            // Recreate the mapper scene so a surviving controller cannot hide a missing saved value.
            yield return TestUtils.ReloadMap(3, savedMap, savedEditorState, forceSceneReload: true);
            yield return null;

            laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            Assert.That(laneController.LaneCount, Is.EqualTo(laneCount), "Reloading the saved map must restore its note lane count.");
            laneCountInput = GetLaneCountInput();
            Assert.That(laneCountInput.Value, Is.EqualTo(laneCount), "The top bar must display the restored lane count.");
            textInput = laneCountInput.GetComponentInChildren<TMP_InputField>(true);
            Assert.That(textInput.text, Is.EqualTo(laneCountText));

            var laneField = typeof(PlacementLaneController).GetField("lane", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(laneField, Is.Not.Null);
            var gridLane = (GridLane)laneField.GetValue(laneController);
            Assert.That(gridLane.Lane, Is.EqualTo(laneCount), "The gameplay grid must use the restored lane count.");
        }

        private static TextBoxIntComponent GetLaneCountInput()
        {
            var view = Object.FindAnyObjectByType<GameplayViewController>(FindObjectsInactive.Include);
            Assert.That(view, Is.Not.Null);
            var inputField = typeof(GameplayViewController).GetField("laneCountInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(inputField, Is.Not.Null);
            return (TextBoxIntComponent)inputField.GetValue(view);
        }

        private static Color32[] RenderGrid(Renderer renderer)
        {
            const int width = 512;
            const int height = 256;
            const int renderLayer = 31;
            var previousLayer = renderer.gameObject.layer;
            var previousTarget = RenderTexture.active;
            var cameraObject = new GameObject("Lane grid regression camera");
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                renderer.gameObject.layer = renderLayer;
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 1 << renderLayer;
                camera.orthographic = true;
                camera.aspect = (float)width / height;
                camera.orthographicSize = renderer.transform.TransformVector(Vector3.right).magnitude
                    / (2f * camera.aspect) * 1.1f;
                camera.transform.SetPositionAndRotation(
                    renderer.bounds.center - renderer.transform.forward * 10f,
                    renderer.transform.rotation);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                var visiblePixels = 0;
                foreach (var pixel in pixels)
                {
                    if (Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > 8)
                        visiblePixels++;
                }

                Assert.That(visiblePixels, Is.GreaterThan(100), "The comparison must render visible grid lines.");
                return pixels;
            }
            finally
            {
                renderer.gameObject.layer = previousLayer;
                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
            }
        }

        private static int CountChangedPixels(Color32[] loaded, Color32[] edited)
        {
            var changed = 0;
            for (var i = 0; i < loaded.Length; i++)
            {
                if (Mathf.Abs(loaded[i].r - edited[i].r) > 2
                    || Mathf.Abs(loaded[i].g - edited[i].g) > 2
                    || Mathf.Abs(loaded[i].b - edited[i].b) > 2)
                {
                    changed++;
                }
            }

            return changed;
        }
    }
}
