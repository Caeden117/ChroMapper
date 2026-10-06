using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.Editor
{
    public class SongInfoCjkFontTest
    {
        // The SongInfo edit fields instantiate this prefab; its text component's font must resolve
        // every script mappers can type into song metadata, through the font's own fallback chain.
        private const string InputFieldPrefabPath = "Assets/_Prefabs/UI/InputField Container.prefab";

        // Header/title labels on the same screen are wired to Teko directly.
        private const string TekoFontPath = "Assets/_Graphics/Materials/Font/Teko.asset";

        // Teko's CJK coverage rides on this dynamic fallback; the song list exercises it at scale.
        private const string NotoSansCjkFontPath = "Assets/_Graphics/Materials/Font/NotoSansCJKjp.asset";

        // Representative codepoints spanning the scripts Beat Saber maps carry in metadata.
        private static readonly uint[] CjkCodepoints =
        {
            0x3042u, // あ hiragana
            0x30ABu, // カ katakana
            0x697Du, // 楽 kanji
            0x4E2Du, // 中 hanzi
            0x6C49u, // 汉 simplified
            0x570Bu, // 國 traditional
            0xD55Cu, // 한 hangul
            0xB178u // 노 hangul
        };

        [Test]
        public void InputFieldTextResolvesCjkCharacters()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InputFieldPrefabPath);
            Assert.NotNull(prefab, "InputField Container prefab missing");

            var inputField = prefab.GetComponentInChildren<TMP_InputField>(true);
            Assert.NotNull(inputField, "Prefab has no TMP_InputField");
            var font = inputField.textComponent.font;
            Assert.NotNull(font, "Input field text component has no font asset");

            foreach (var unicode in CjkCodepoints)
            {
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(
                    unicode, font, true, FontStyles.Normal, FontWeight.Regular, out _);
                Assert.NotNull(
                    character,
                    $"U+{unicode:X4} does not resolve through {font.name}'s fallback chain; " +
                    "CJK song metadata will render as missing glyphs in the SongInfo fields");
            }
        }

        // SongListCjkNamesKeepResolvingPastAtlasCapacity: the song select screen resolves a fresh
        // glyph per new CJK character it lists, so the dynamic atlas must keep accepting additions —
        // a single near-full atlas with multi-atlas disabled silently drops every later character,
        // which is why CJK map names rendered nothing at all.
        [Test]
        public void DynamicCjkAtlasAcceptsNewGlyphsAtScale()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSansCjkFontPath);
            Assert.NotNull(font, "NotoSansCJKjp font asset missing");

            // U+9F40..U+9FA3 is a populated CJK-unified range absent from the baked glyph table.
            var failures = 0;
            for (uint unicode = 0x9F40; unicode <= 0x9FA3; unicode++)
            {
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(
                    unicode, font, true, FontStyles.Normal, FontWeight.Regular, out _);
                if (character == null) failures++;
            }

            Assert.Zero(
                failures,
                "dynamic CJK atlas stopped accepting new glyphs; song-list names beyond capacity render nothing");
        }

        // The song-list row prefab AssignSong writes to; its Title/Artist/Folder fields use Teko,
        // which resolves CJK through the dynamic NotoSansCJKjp fallback at runtime.
        private const string SongListElementPrefabPath = "Assets/_Prefabs/UI/SongListElement.prefab";

        // This depersonalized map fixture preserves the reported mix of Han and Japanese glyphs in
        // both its directory and Info.dat, so the regression covers the production metadata loader.
        private const string CjkSongFixturePath =
            "Assets/Tests/Fixtures/51eb1 (星光下的尾巴 - 匿名の音楽家)";

        // End-to-end regression for blank CJK map names in the song list: drives the real prefab
        // through AssignSong with the metadata of the map that reported the failure, then verifies
        // every non-ASCII character resolved to its own glyph — a substitution with the baked
        // missing-glyph char (U+25A1) or a null element means the dynamic atlas dropped it.
        [UnityTest]
        public IEnumerator SongListElementResolvesAllCjkGlyphsInMapMetadata()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongListElementPrefabPath);
            Assert.NotNull(prefab, "SongListElement prefab missing");

            // A real canvas is required for TMP's sub-mesh render path to engage.
            var canvasGo = new GameObject("TestCanvas", typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var go = Object.Instantiate(prefab, canvas.transform);
            var item = go.GetComponent<SongListItem>();
            Assert.NotNull(item, "SongListElement has no SongListItem");

            var info = LoadCjkSongFixture();
            item.AssignSong(info, "");
            yield return null;
            yield return null;

            var unresolved = 0;
            foreach (var field in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                field.ForceMeshUpdate();
                foreach (var ci in field.textInfo.characterInfo)
                {
                    if (ci.character <= 0x7F || ci.elementType != TMP_TextElementType.Character) continue;
                    if (ci.textElement == null || ci.textElement.unicode != ci.character)
                    {
                        unresolved++;
                    }
                }

            }

            Object.Destroy(go);
            Object.Destroy(canvasGo);
            Assert.Zero(unresolved, "song-list fields contain characters with no resolved glyph");
        }

        // A separate fallback-material regression logged mat=null on submeshes that still carried
        // geometry. TMP's runtime fallback materials are referenced
        // only by managed caches in TMP_MaterialManager, so once the last sub-mesh drops its
        // reference the next UnloadUnusedAssets (which Unity runs on every scene load) destroys the
        // material while the cache keeps returning the corpse — and GetFallbackMaterial never
        // null-checks it, so every later row gets a dead material permanently. SongListItem pins
        // each live fallback material on a persistent holder so it can never be collected.
        [UnityTest]
        public IEnumerator SongListSubMeshMaterialSurvivesFallbackCleanup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongListElementPrefabPath);
            Assert.NotNull(prefab, "SongListElement prefab missing");

            var canvasGo = new GameObject("TestCanvas", typeof(Canvas));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = Object.Instantiate(prefab, canvasGo.transform);
            var item = go.GetComponent<SongListItem>();
            Assert.NotNull(item, "SongListElement has no SongListItem");

            // Force the legacy TMP route so this lifetime regression still exercises the holder
            // when metadata assignment routes fallback symbols through the source-font renderer.
            var info = LoadCjkSongFixture();
            info.SongName = "Fallback ✧";
            info.SongSubName = "";
            info.SongAuthorName = "Artist";
            item.AssignSong(info, "");
            var title = go.transform.Find("Text/Title").GetComponent<TextMeshProUGUI>();
            title.enabled = true;
            title.GetComponentInChildren<Text>(true).gameObject.SetActive(false);
            title.text = "Fallback ✧";
            yield return null;
            yield return null;

            var subMesh = go.GetComponentsInChildren<TMP_SubMeshUI>(true)
                .FirstOrDefault(sm => sm.mesh != null && sm.mesh.vertexCount > 0 && sm.sharedMaterial != null);
            Assert.NotNull(subMesh, "no fallback sub-mesh with live geometry was created");
            var fallbackMaterial = subMesh.sharedMaterial;

            // Drop every Unity reference TMP holds, the same way a scene unload does: if nothing
            // else references the material, the next asset collection turns it into a corpse that
            // TMP's cache keeps returning to future rows.
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(canvasGo);
            yield return Resources.UnloadUnusedAssets();

            Assert.IsTrue(
                fallbackMaterial != null,
                "fallback material was collected; TMP's cache will keep returning the corpse, " +
                "leaving materialForRendering null and CJK text invisible");
            Assert.IsTrue(
                TMPFallbackMaterialHolder.IsPinned(fallbackMaterial),
                "fallback material was never pinned to the persistent holder");
        }

        // The earlier regressions only inspected resolved glyphs and geometry, which looked healthy
        // while the player drew nothing. Render the real source-font path through RectMask2D and
        // assert on presented pixels rather than internal text state.
        [UnityTest]
        public IEnumerator SongListElementPresentsCjkPixelsInsideMask()
        {
            const int renderWidth = 512;
            const int renderHeight = 128;
            var renderTexture = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
            var cameraGo = new GameObject("CJK Render Camera", typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.cullingMask = 1 << 5;
            camera.targetTexture = renderTexture;

            var canvasGo = new GameObject("CJK Render Canvas", typeof(Canvas));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;

            var maskGo = new GameObject("Song List Viewport", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(RectMask2D));
            maskGo.layer = 5;
            maskGo.transform.SetParent(canvasGo.transform, false);
            var maskRect = maskGo.GetComponent<RectTransform>();
            maskRect.anchorMin = new Vector2(0.5f, 0.5f);
            maskRect.anchorMax = new Vector2(0.5f, 0.5f);
            maskRect.sizeDelta = new Vector2(1024, 512);
            maskGo.GetComponent<Image>().enabled = false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongListElementPrefabPath);
            Assert.NotNull(prefab, "SongListElement prefab missing");
            var row = Object.Instantiate(prefab, maskGo.transform);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = Vector2.zero;
            var item = row.GetComponent<SongListItem>();
            item.AssignSong(LoadCjkSongFixture(), "");

            yield return null;
            yield return null;

            // Isolate the title and its generated fallback submeshes so cover art and row chrome
            // cannot make a blank text render look successful.
            var title = row.transform.Find("Text/Title").GetComponent<TextMeshProUGUI>();
            var cjkTitle = title.GetComponentInChildren<Text>(true);
            Assert.NotNull(cjkTitle, "song-list title has no source-font renderer");
            Assert.IsTrue(cjkTitle.gameObject.activeSelf, "source-font title is inactive for CJK metadata");
            foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
            {
                graphic.enabled = graphic == cjkTitle;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var cjkPixels = CountVisiblePixels(renderTexture, renderWidth, renderHeight);

            // The hidden mask graphic can still affect the render target. Subtract an otherwise
            // identical empty-title frame so only pixels contributed by CJK glyphs satisfy the test.
            cjkTitle.text = "";
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var emptyPixels = CountVisiblePixels(renderTexture, renderWidth, renderHeight);
            var visiblePixels = cjkPixels - emptyPixels;

            Object.DestroyImmediate(row);
            Object.DestroyImmediate(maskGo);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(cameraGo);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);

            Assert.Greater(visiblePixels, 20,
                "CJK song-list title resolved internally but presented no visible pixels inside the list mask");
        }

        // Both fallback and direct multi-atlas TMP paths create player-invisible submeshes. Require
        // one source-font uGUI renderer per affected field so CJK never enters that render path.
        [UnityTest]
        public IEnumerator SongListCjkMetadataUsesSourceFontRenderer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongListElementPrefabPath);
            Assert.NotNull(prefab, "SongListElement prefab missing");
            var canvasGo = new GameObject("TestCanvas", typeof(Canvas));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var maskGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            maskGo.transform.SetParent(canvasGo.transform, false);
            var row = Object.Instantiate(prefab, maskGo.transform);
            var item = row.GetComponent<SongListItem>();
            item.AssignSong(LoadCjkSongFixture(), "");

            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            foreach (var path in new[] { "Text/Title", "Text/Artist", "Text/Folder" })
            {
                var field = row.transform.Find(path).GetComponent<TextMeshProUGUI>();
                var sourceRenderer = field.GetComponentInChildren<Text>(true);
                Assert.NotNull(sourceRenderer,
                    path + " has no independent source-font renderer");
                Assert.NotNull(sourceRenderer.font,
                    path + " source-font renderer has no bundled font");
                Assert.NotNull(sourceRenderer.GetComponentInParent<RectMask2D>(),
                    path + " has no field-local clipping for long metadata");
                Assert.IsTrue(sourceRenderer.gameObject.activeSelf,
                    path + " source-font renderer is inactive for CJK metadata");
                Assert.IsFalse(field.enabled,
                    path + " still sends CJK through TMP's multi-atlas render path");
            }

            Object.DestroyImmediate(row);
            Object.DestroyImmediate(maskGo);
            Object.DestroyImmediate(canvasGo);
        }

        // SongListCjkMetadataRasterizesAboveDisplayResolution: Unity UI.Text rasterized at the
        // final display size is visibly softer than adjacent TMP SDF text, including Latin glyphs
        // in a mixed CJK path. Keep supersampling when shrinking glyphs to fit each metadata field.
        [UnityTest]
        public IEnumerator SongListCjkMetadataRasterizesAboveDisplayResolution()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongListElementPrefabPath);
            Assert.NotNull(prefab, "SongListElement prefab missing");
            var canvasGo = new GameObject("TestCanvas", typeof(Canvas));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var row = Object.Instantiate(prefab, canvasGo.transform);
            var item = row.GetComponent<SongListItem>();
            item.AssignSong(LoadCjkSongFixture(), "");

            yield return null;

            foreach (var path in new[] { "Text/Title", "Text/Artist", "Text/Folder" })
            {
                var field = row.transform.Find(path).GetComponent<TextMeshProUGUI>();
                var sourceRenderer = field.GetComponentInChildren<Text>(true);
                var displayScale = path switch
                {
                    "Text/Title" => 0.76f,
                    "Text/Artist" => 0.8f,
                    _ => 0.9f
                };

                Assert.GreaterOrEqual(sourceRenderer.fontSize, Mathf.RoundToInt(field.fontSize * 2),
                    path + " source font is rasterized at display resolution and will look blurry");
                Assert.AreEqual(field.fontSize * displayScale / sourceRenderer.fontSize,
                    sourceRenderer.rectTransform.localScale.x, 0.001f,
                    path + " source renderer does not apply the requested fallback font reduction");
                // SongListCjkMetadataRasterizesAboveDisplayResolution: the supersampled child must
                // retain the authored displayed width or UI.Text wraps mixed metadata vertically.
                Assert.AreEqual(field.rectTransform.rect.width,
                    sourceRenderer.rectTransform.rect.width * sourceRenderer.rectTransform.localScale.x,
                    0.01f, path + " supersampled renderer does not preserve the authored field width");
                // SongListCjkMetadataRasterizesAboveDisplayResolution: metadata remains on one
                // line and relies on RectMask2D clipping, matching the authored TMP fields.
                Assert.AreEqual(HorizontalWrapMode.Overflow, sourceRenderer.horizontalOverflow,
                    path + " CJK metadata wraps instead of remaining on one clipped line");
                // SongListCjkTitleMaskAllowsDescenders: the 16-point source font is taller than
                // the authored TMP title field, so its clip wrapper must allow vertical bleed.
                if (path == "Text/Title")
                {
                    var clip = sourceRenderer.GetComponentInParent<RectMask2D>();
                    Assert.Greater(clip.rectTransform.rect.height, field.rectTransform.rect.height,
                        "CJK title clip cuts off the bottom of full-height glyphs");
                }
            }

            Object.DestroyImmediate(row);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void TekoFontResolvesCjkCharacters()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TekoFontPath);
            Assert.NotNull(font, "Teko font asset missing");

            foreach (var unicode in CjkCodepoints)
            {
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(
                    unicode, font, true, FontStyles.Normal, FontWeight.Regular, out _);
                Assert.NotNull(
                    character,
                    $"U+{unicode:X4} does not resolve through Teko's fallback chain");
            }
        }

        // Song-list coverage must begin with the same file-backed load used by map discovery; a
        // hand-built BaseInfo would miss Unicode path or metadata decoding regressions.
        private static BaseInfo LoadCjkSongFixture()
        {
            var fixtureDirectory = Path.GetFullPath(CjkSongFixturePath);
            var info = BeatSaberSongUtils.GetInfoFromFolder(fixtureDirectory);
            Assert.NotNull(info, "CJK Info.dat fixture did not load");
            Assert.AreEqual("星光下的尾巴", info.SongName);
            Assert.AreEqual("匿名の音楽家", info.SongAuthorName);
            Assert.AreEqual(fixtureDirectory, info.Directory);
            return info;
        }

        // SongListElementPresentsCjkPixelsInsideMask reads the presented render target rather than
        // trusting TMP mesh state, which remained healthy during the reported blank-row failure.
        private static int CountVisiblePixels(RenderTexture renderTexture, int width, int height)
        {
            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            pixels.Apply(false);
            RenderTexture.active = previousActive;
            var count = pixels.GetPixels32().Count(pixel => pixel.a > 16 && pixel.r > 32);
            Object.DestroyImmediate(pixels);
            return count;
        }
    }
}
