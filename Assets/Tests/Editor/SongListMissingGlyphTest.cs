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
            Assert.IsTrue(title.enabled, "Fallback text must use TextMeshPro.");
            title.ForceMeshUpdate();
            Assert.AreEqual(0x25E0, title.textInfo.characterInfo[0].textElement.unicode,
                "The reported title must use the real U+25E0 glyph, not the missing-glyph box.");
            foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.transform.IsChildOf(title.transform))
                    graphic.enabled = false;
            }

            var symbolPixels = CountRenderedTextPixels();
            title.text = "";
            var emptyPixels = CountRenderedTextPixels();
            Assert.Greater(symbolPixels - emptyPixels, 5,
                "The Tanger title U+25E0 resolves but draws no visible pixels in the song-list mask.");
        }

        [UnityTest]
        public IEnumerator MetadataPreservesGeometricDingbatAndSupplementarySymbols()
        {
            var paths = new[] { "Text/Title", "Text/Artist", "Text/Folder" };
            foreach (var unicode in new[] { 0x25E0, 0x25E1, 0x2727, 0x2605, 0x1F0A1, 0x1FB00,
                         0x1F700, 0x1F701, 0x1F702 })
            {
                var symbol = char.ConvertFromUtf32(unicode);
                item.AssignSong(new BaseInfo
                {
                    SongName = symbol,
                    SongSubName = "",
                    SongAuthorName = symbol,
                    Directory = symbol
                }, "");
                yield return null;
                yield return null;

                foreach (var path in paths)
                {
                    var field = row.transform.Find(path).GetComponent<TextMeshProUGUI>();
                    field.ForceMeshUpdate();
                    Assert.Greater(field.textInfo.characterCount, 0, path + " omitted the symbol entirely.");
                    var character = field.textInfo.characterInfo[0];
                    Assert.AreEqual(TMP_TextElementType.Character, character.elementType,
                        path + " must use the symbol font, not a sprite substitute.");
                    Assert.IsTrue(character.isVisible, path + " has no symbol geometry.");
                    Assert.AreEqual(unicode, character.textElement.unicode,
                        path + $" replaced U+{unicode:X} with a missing-glyph box.");
                }
            }
        }

        [UnityTest]
        public IEnumerator MissingUnicodeScalarUsesOneBoxAndPreservesHighlightedTitle()
        {
            info.SongName = "Before \U0010FFFF After";
            item.AssignSong(info, "After");
            yield return null;
            title.ForceMeshUpdate();
            Assert.AreEqual("Before \U0010FFFF <color=#ff0000ff>After</color> <size=50%><i></i></size>", title.text,
                "Native font fallback must preserve rich-text highlighting and raw display text.");
            Assert.AreEqual("Before \U0010FFFF After", info.SongName, "Display substitution changed map metadata.");
            Assert.IsTrue(title.textInfo.characterInfo[7].isVisible, "The missing-glyph box has no geometry.");
            Assert.AreEqual(0x25A1, title.textInfo.characterInfo[7].textElement.unicode);
            Assert.AreEqual("Before □ After ", title.GetParsedText(),
                "An unsupported supplementary scalar must produce exactly one box.");
        }

        [UnityTest]
        public IEnumerator MissingCjkMetadataGlyphUsesBoxInTmpRenderer()
        {
            info.SongName = "漢 \U0010FFFF After";
            info.SongSubName = "\U0010FFFF";
            info.SongAuthorName = "漢 \U0010FFFF";
            info.Directory = "漢 \U0010FFFF";
            item.AssignSong(info, "After");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(title.enabled);
            Assert.AreEqual("漢 □ After □", title.GetParsedText());
            StringAssert.Contains("<color=#ff0000ff>After</color>", title.text);
            Assert.AreEqual("漢 □", row.transform.Find("Text/Artist").GetComponent<TextMeshProUGUI>().GetParsedText());
            Assert.AreEqual("漢 □", row.transform.Find("Text/Folder").GetComponent<TextMeshProUGUI>().GetParsedText());
        }

        [UnityTest]
        public IEnumerator RecycledRowsPreserveFontSizeAndHighlighting()
        {
            item.AssignSong(info, "");
            yield return null;
            Assert.IsTrue(title.enabled);
            Assert.AreEqual(16, title.fontSize, 0.001f,
                "A symbol must not shrink the whole title line.");

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
            Assert.AreEqual(16, title.fontSize, "Recycled Latin rows must preserve their normal font size.");
            Assert.AreEqual("Latin <color=#ff0000ff>Title</color> <size=50%><i>Subtitle</i></size>", title.text);

            item.AssignSong(new BaseInfo
            {
                SongName = "◠",
                SongSubName = "",
                SongAuthorName = "Tanger",
                Directory = "Symbol Folder"
            }, "");
            yield return null;
            Assert.AreEqual(16, title.fontSize, 0.001f,
                "Reusing a cached fallback glyph must preserve the normal title size.");
        }

        [UnityTest]
        public IEnumerator MetadataAssignmentPreservesAuthoredTextBounds()
        {
            // Negative vertical margins in SongListElement cover the taller fallback lines without
            // changing layout height. Their 10/8/4-unit allowances include Noto Sans Symbols' 2.05x line metrics.
            var paths = new[] { "Text/Title", "Text/Artist", "Text/Folder" };
            var margins = new Vector4[paths.Length];
            for (var i = 0; i < paths.Length; i++)
            {
                margins[i] = row.transform.Find(paths[i]).GetComponent<TextMeshProUGUI>().margin;
            }

            foreach (var text in new[] { "Latin gyp", "漢 gyp", "◠ \U0001F700 gyp" })
            {
                item.AssignSong(new BaseInfo
                {
                    SongName = text,
                    SongSubName = "Subtitle",
                    SongAuthorName = text,
                    Directory = text
                }, "");
                yield return null;
                yield return null;
                for (var i = 0; i < paths.Length; i++)
                {
                    var field = row.transform.Find(paths[i]).GetComponent<TextMeshProUGUI>();
                    Assert.AreEqual(margins[i], field.margin,
                        paths[i] + " must preserve its authored text bounds when metadata changes.");
                }
            }
        }

        [UnityTest]
        public IEnumerator FullSizeFallbackTitlePreservesPixelsInsideRow()
        {
            yield return CompareFullSizeMetadataPixels("Text/Title");
        }

        [UnityTest]
        public IEnumerator FullSizeFallbackArtistAndPathPreservePixelsInsideRow()
        {
            yield return CompareFullSizeMetadataPixels("Text/Artist");
            yield return CompareFullSizeMetadataPixels("Text/Folder");
        }

        private IEnumerator CompareFullSizeMetadataPixels(string path)
        {
            var field = row.transform.Find(path).GetComponent<TextMeshProUGUI>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/UI/SongListElement.prefab");
            var normalFontSize = prefab.transform.Find(path).GetComponent<TextMeshProUGUI>().fontSize;
            var viewport = row.transform.parent.GetComponent<RectMask2D>();
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.pivot = Vector2.one * 0.5f;
            viewport.rectTransform.sizeDelta = rowRect.sizeDelta;
            foreach (var songName in new[]
                     { "漢 gypgyp", "◠ gypgyp", "星光下◠的尾巴", "\U0001F700 gypgyp", "gypgyp" })
            {
                item.AssignSong(new BaseInfo
                {
                    SongName = songName,
                    SongSubName = "神聖的",
                    SongAuthorName = songName,
                    Directory = songName
                }, "");
                yield return null;
                yield return null;
                Assert.AreEqual(normalFontSize, field.fontSize,
                    "CJK or symbols must not shrink " + path + ": " + songName);
                foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = graphic.transform.IsChildOf(field.transform) || graphic == field
                        ? Color.white
                        : Color.clear;
                }

                var clippedPixels = CountRenderedTextPixels();
                field.overflowMode = TextOverflowModes.Overflow;
                viewport.enabled = false;
                var completePixels = CountRenderedTextPixels();
                Assert.Greater(completePixels, 20, path + " rendered no text for the clipping comparison.");
                Assert.AreEqual(completePixels, clippedPixels,
                    "Normal-size glyphs or descenders were clipped in " + path + ": " + songName);
                field.overflowMode = TextOverflowModes.Ellipsis;
                viewport.enabled = true;
            }
        }

        [UnityTest]
        public IEnumerator FullSizeFallbackTitleRetainsHorizontalEllipsis()
        {
            var normalFontSize = title.fontSize;
            item.AssignSong(new BaseInfo
            {
                SongName = "漢 ◠ gypgyp " + new string('漢', 40),
                SongSubName = "神聖的",
                SongAuthorName = "Matryoshka",
                Directory = "Latin Folder"
            }, "");
            yield return null;
            yield return null;
            title.ForceMeshUpdate();
            Assert.AreEqual(normalFontSize, title.fontSize);
            Assert.AreEqual(TextOverflowModes.Ellipsis, title.overflowMode);
            var lastVisibleUnicode = 0u;
            for (var i = 0; i < title.textInfo.characterCount; i++)
            {
                var character = title.textInfo.characterInfo[i];
                if (character.isVisible)
                {
                    lastVisibleUnicode = character.textElement.unicode;
                }
            }

            Assert.AreEqual(0x6F22, title.textInfo.characterInfo[0].textElement.unicode,
                "The leading CJK glyph must survive truncation.");
            Assert.IsTrue(title.textInfo.characterInfo[0].isVisible);
            Assert.AreEqual(0x2026, lastVisibleUnicode,
                "A long fallback title must end in a visible ellipsis.");
        }

        [UnityTest]
        public IEnumerator RecycledMetadataUsesTmpPreferredHeights()
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
                    Assert.AreEqual(field.preferredHeight, LayoutUtility.GetPreferredHeight(field.rectTransform), 0.01f,
                        paths[i] + " must let TMP determine its preferred layout height.");
                    fallbackRects[i] = new Rect(field.rectTransform.anchoredPosition, field.rectTransform.rect.size);
                }

                item.AssignSong(new BaseInfo
                {
                    SongName = "gypgyp",
                    SongSubName = "Subtitle",
                    SongAuthorName = "Matryoshka",
                    Directory = "Latin Folder"
                }, "");
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(row.transform.Find("Text").GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                foreach (var path in paths)
                {
                    var field = row.transform.Find(path).GetComponent<TextMeshProUGUI>();
                    Assert.AreEqual(field.preferredHeight, LayoutUtility.GetPreferredHeight(field.rectTransform), 0.01f,
                        path + " must retain automatic sizing after switching to Latin metadata.");
                }

                item.AssignSong(metadata, "");
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                for (var i = 0; i < paths.Length; i++)
                {
                    var field = row.transform.Find(paths[i]).GetComponent<TextMeshProUGUI>();
                    Assert.AreEqual(field.rectTransform.rect.height, fallbackRects[i].height, 0.01f,
                        paths[i] + " changed height after recycling back to the same fallback metadata.");
                    Assert.AreEqual(field.rectTransform.anchoredPosition.y, fallbackRects[i].y, 0.01f,
                        paths[i] + " moved after recycling back to the same fallback metadata.");
                }
            }
        }

        [UnityTest]
        public IEnumerator FallbackTitleMaskPreservesDescenderPixels()
        {
            info.SongName = "漢 gypgyp";
            info.SongSubName = "神聖的";
            info.SongAuthorName = "Matryoshka";
            item.AssignSong(info, "");
            yield return null;
            yield return null;
            Assert.IsTrue(title.enabled);
            foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
            {
                // Disabled TMP graphics stop contributing layout height, which would change the clipping reproduction.
                if (!graphic.transform.IsChildOf(title.transform) && graphic != title)
                {
                    graphic.color = Color.clear;
                }
            }

            var clippedPixels = CountRenderedTextPixels();
            title.overflowMode = TextOverflowModes.Overflow;
            var completePixels = CountRenderedTextPixels();
            Assert.Greater(completePixels, 20, "The descender comparison rendered no title text.");
            Assert.AreEqual(completePixels, clippedPixels,
                "The fallback title's normal bounds cut off glyph pixels, including g/p/y descenders.");
        }

        [UnityTest]
        public IEnumerator NativeTmpFallbackPresentsCjkAndSymbolPixels()
        {
            foreach (var text in new[] { "Hello", "漢", "◠", "漢 ◠ gyp", "あ カ 楽 中 汉 國 한 노" })
            {
                var metadata = new BaseInfo
                {
                    SongName = text,
                    SongSubName = "",
                    SongAuthorName = "Artist",
                    Directory = "Folder"
                };
                item.AssignSong(metadata, "");
                yield return null;
                yield return null;

                foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
                {
                    if (!graphic.transform.IsChildOf(title.transform) && graphic != title)
                    {
                        graphic.color = Color.clear;
                    }
                }

                Canvas.ForceUpdateCanvases();
                if (text == "あ カ 楽 中 汉 國 한 노")
                {
                    for (var i = 0; i < title.textInfo.characterCount; i++)
                    {
                        var ch = title.textInfo.characterInfo[i];
                        Assert.AreNotEqual('□', ch.character, "Supported CJK must retain its original glyph.");
                    }
                }

                var glyphPixels = CountRenderedTextPixels();
                title.text = "";
                var emptyPixels = CountRenderedTextPixels();
                Assert.Greater(glyphPixels - emptyPixels, 5,
                    "Native TMP fallback resolved but did not draw " + text);
            }
        }

        [UnityTest]
        public IEnumerator MetadataUsesTmpFallbackWithoutLegacyRenderer()
        {
            info.SongName = "漢 ◠ gyp";
            item.AssignSong(info, "");
            yield return null;
            yield return null;
            Assert.IsTrue(title.enabled, "CJK metadata must stay in TextMeshPro's font fallback chain.");
            Assert.IsEmpty(row.GetComponentsInChildren<Text>(true), "Obsolete Text renderers remain in the row.");
        }

        private int CountRenderedTextPixels()
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
                {
                    count++;
                }
            }

            Object.DestroyImmediate(image);
            return count;
        }
    }
}
