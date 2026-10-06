using System.Collections;
using System.IO;
using Beatmap.Info;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.Editor
{
    public class SongListMissingGlyphTest
    {
        private GameObject canvasObject;
        private GameObject cameraObject;
        private GameObject row;
        private RenderTexture target;
        private SongListItem item;
        private TextMeshProUGUI title;
        private BaseInfo info;

        [SetUp]
        public void SetUp()
        {
            target = new RenderTexture(512, 128, 24, RenderTextureFormat.ARGB32);
            cameraObject = new GameObject("Symbol Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.cullingMask = 1 << 5;
            camera.targetTexture = target;

            canvasObject = new GameObject("Symbol Canvas", typeof(Canvas));
            canvasObject.layer = 5;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.layer = 5;
            viewport.transform.SetParent(canvas.transform, false);
            viewport.GetComponent<RectTransform>().sizeDelta = new Vector2(1024, 512);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/UI/SongListElement.prefab");
            row = Object.Instantiate(prefab, viewport.transform);
            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchoredPosition = Vector2.zero;
            item = row.GetComponent<SongListItem>();
            title = row.transform.Find("Text/Title").GetComponent<TextMeshProUGUI>();
            info = BeatSaberSongUtils.GetInfoFromFolder(Path.GetFullPath(
                "Assets/Tests/Fixtures/51eb1 (星光下的尾巴 - 匿名の音楽家)"));
            info.SongName = "◠";
            info.SongSubName = "";
            info.SongAuthorName = "Tanger";
            info.Directory = "Missing Glyph Fixture";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
            target.Release();
            Object.DestroyImmediate(target);
        }

        [UnityTest]
        public IEnumerator TangerTitlePresentsPixelsInsideSongListMask()
        {
            item.AssignSong(info, "");
            yield return null;
            yield return null;
            var sourceTitle = title.GetComponentInChildren<Text>(true);
            Debug.Log($"[SongListGlyph] U+25E0 sourceActive={sourceTitle.gameObject.activeSelf} " +
                $"sourceHasGlyph={sourceTitle.font.HasCharacter('◠')} sourceHasBox={sourceTitle.font.HasCharacter('□')}");
            Assert.IsTrue(sourceTitle.gameObject.activeSelf,
                "U+25E0 still uses the TMP fallback submesh path that produces invisible song-list symbols in the player.");
            foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.transform.IsChildOf(title.transform))
                    graphic.enabled = false;
            }

            var symbolPixels = CountTitlePixels();
            title.text = "";
            title.GetComponentInChildren<Text>(true).text = "";
            var emptyPixels = CountTitlePixels();
            Assert.Greater(symbolPixels - emptyPixels, 5,
                "The Tanger title U+25E0 resolves but draws no visible pixels in the song-list mask.");
        }

        [UnityTest]
        public IEnumerator MissingUnicodeScalarUsesOneBoxAndPreservesHighlightedTitle()
        {
            info.SongName = "Before \U0010FFFF After";
            item.AssignSong(info, "After");
            yield return null;
            title.ForceMeshUpdate();
            Assert.AreEqual("Before □ <color=#ff0000ff>After</color> <size=50%><i></i></size>", title.text,
                "Missing characters must become one U+25A1 box before rich-text formatting.");
            Assert.AreEqual("Before \U0010FFFF After", info.SongName, "Display substitution changed map metadata.");
            var sourceTitle = title.GetComponentInChildren<Text>(true);
            if (title.enabled)
            {
                Assert.IsTrue(title.textInfo.characterInfo[7].isVisible, "The missing-glyph box has no geometry.");
                Assert.AreEqual(0x25A1, title.textInfo.characterInfo[7].textElement.unicode);
            }
            else
            {
                Assert.IsTrue(sourceTitle.gameObject.activeSelf);
                Assert.IsTrue(sourceTitle.font.HasCharacter('□'), "The source font cannot draw the missing-glyph box.");
            }
        }

        [UnityTest]
        public IEnumerator MissingCjkMetadataGlyphUsesBoxInSourceFontRenderer()
        {
            info.SongName = "漢 \U0010FFFF After";
            info.SongSubName = "\U0010FFFF";
            info.SongAuthorName = "漢 \U0010FFFF";
            info.Directory = "漢 \U0010FFFF";
            item.AssignSong(info, "After");
            yield return null;
            var sourceTitle = title.GetComponentInChildren<Text>(true);
            Assert.IsTrue(sourceTitle.gameObject.activeSelf);
            StringAssert.StartsWith("漢 □ <color=#ff0000ff>After</color>", sourceTitle.text);
            StringAssert.Contains("<i>□</i>", sourceTitle.text);
            Assert.AreEqual("漢 □", row.transform.Find("Text/Artist").GetComponentInChildren<Text>(true).text);
            Assert.AreEqual("漢 □", row.transform.Find("Text/Folder").GetComponentInChildren<Text>(true).text);
        }

        [UnityTest]
        public IEnumerator RecycledSymbolRowReturnsToTmpForLatinMetadata()
        {
            item.AssignSong(info, "");
            yield return null;
            var sourceTitle = title.GetComponentInChildren<Text>(true);
            Assert.IsTrue(sourceTitle.gameObject.activeSelf);

            var latinInfo = new BaseInfo
            {
                SongName = "Latin Title",
                SongSubName = "Subtitle",
                SongAuthorName = "Artist",
                Directory = "Latin Folder"
            };
            item.AssignSong(latinInfo, "Title");
            yield return null;
            Assert.IsTrue(title.enabled);
            Assert.IsFalse(sourceTitle.gameObject.activeSelf);
            Assert.AreEqual("Latin <color=#ff0000ff>Title</color> <size=50%><i>Subtitle</i></size>", title.text);
        }

        [UnityTest]
        public IEnumerator FallbackMetadataKeepsNormalLayoutSlots()
        {
            var paths = new[] { "Text/Title", "Text/Artist", "Text/Folder" };
            for (var scenario = 0; scenario < 3; scenario++)
            {
                var metadata = new BaseInfo
                {
                    SongName = "gypgyp",
                    SongSubName = scenario != 1 ? "神聖的" : "",
                    SongAuthorName = scenario != 0 ? "Matryoshka 漢" : "Matryoshka",
                    Directory = scenario == 2 ? "漢 Folder" : "Latin Folder"
                };
                item.AssignSong(metadata, "");
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();

                var fallbackRects = new Rect[paths.Length];
                for (var i = 0; i < paths.Length; i++)
                {
                    var field = row.transform.Find(paths[i]).GetComponent<TextMeshProUGUI>();
                    fallbackRects[i] = new Rect(field.rectTransform.anchoredPosition, field.rectTransform.rect.size);
                    field.enabled = true;
                    field.GetComponentInChildren<Text>(true).gameObject.SetActive(false);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(row.transform.Find("Text").GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                for (var i = 0; i < paths.Length; i++)
                {
                    var field = row.transform.Find(paths[i]).GetComponent<TextMeshProUGUI>();
                    Debug.Log($"[SongListLayout] scenario={scenario} {paths[i]} " +
                        $"fallbackHeight={fallbackRects[i].height} tmpHeight={field.rectTransform.rect.height} " +
                        $"fallbackY={fallbackRects[i].y} tmpY={field.rectTransform.anchoredPosition.y}");
                    Assert.AreEqual(field.rectTransform.rect.height, fallbackRects[i].height, 0.01f,
                        paths[i] + " lost its normal layout height when metadata switched to fallback rendering");
                    Assert.AreEqual(field.rectTransform.anchoredPosition.y, fallbackRects[i].y, 0.01f,
                        paths[i] + " moved vertically when metadata switched to fallback rendering");
                }
            }
        }

        [UnityTest]
        public IEnumerator FallbackTitleMaskPreservesDescenderPixels()
        {
            info.SongName = "gypgyp";
            info.SongSubName = "神聖的";
            info.SongAuthorName = "Matryoshka";
            item.AssignSong(info, "");
            yield return null;
            yield return null;
            var sourceTitle = title.GetComponentInChildren<Text>(true);
            Assert.IsTrue(sourceTitle.gameObject.activeSelf);
            foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
            {
                // Disabled TMP graphics stop contributing layout height, which would change the clipping reproduction.
                if (graphic != sourceTitle)
                    graphic.color = Color.clear;
            }

            var clippedPixels = CountTitlePixels();
            var clip = sourceTitle.GetComponentInParent<RectMask2D>();
            clip.enabled = false;
            var completePixels = CountTitlePixels();
            Debug.Log($"[SongListLayout] titleHeight={title.rectTransform.rect.height} " +
                $"clipHeight={clip.rectTransform.rect.height} clippedPixels={clippedPixels} completePixels={completePixels}");
            Assert.Greater(completePixels, 20, "The descender comparison rendered no title text.");
            Assert.AreEqual(completePixels, clippedPixels,
                "The fallback title's local mask cuts off glyph pixels, including g/p/y descenders.");
        }

        private int CountTitlePixels()
        {
            Canvas.ForceUpdateCanvases();
            cameraObject.GetComponent<Camera>().Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            var count = 0;
            foreach (var pixel in image.GetPixels32())
            {
                if (pixel.a > 16 && pixel.r > 32)
                    count++;
            }
            Object.DestroyImmediate(image);
            return count;
        }
    }
}
