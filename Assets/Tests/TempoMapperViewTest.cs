using System.Collections;
using System.Linq;
using Beatmap.Info;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class TempoMapperViewTest
    {
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator TempoWorkspaceBorrowsAndRestoresTheExistingSpectrogram()
        {
            yield return TestUtils.LoadMap(3);
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "02_SongEditMenu"
                && !SceneTransitionManager.IsLoading);
            yield return null;

            var preview = Object.FindAnyObjectByType<AudioPreviewGenerator>();
            var selector = Object.FindAnyObjectByType<DifficultySelect>();
            var spectrogram = preview.Spectrogram;
            var parent = spectrogram.parent;
            var originalSize = spectrogram.sizeDelta;
            var clip = AudioClip.Create("Tempo mapping smoke test", 480000, 1, 48000, false);
            var container = BeatSaberSongContainer.Instance;
            var oldClip = container.LoadedSong;
            container.LoadedSong = clip;
            try
            {
                TempoMapperView.Open(clip, preview, selector, null);
                yield return null;
                var view = Object.FindAnyObjectByType<TempoMapperView>();
                Assert.That(view, Is.Not.Null);
                var sourcePrecision = CMInputCallbackInstaller.InputInstance.asset.FindAction("Timeline/Change Precision Modifier", true);
                var previousBindings = sourcePrecision.SaveBindingOverridesAsJson();
                var sourceEnabled = sourcePrecision.enabled;
                var installPrecision = typeof(TempoMapperView).GetMethod("InstallPrecisionInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                try
                {
                    sourcePrecision.ApplyBindingOverride(0, "<Keyboard>/leftAlt");
                    installPrecision.Invoke(view, null);
                    var clonedPrecision = (InputAction)typeof(TempoMapperView)
                        .GetField("precisionModifier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view);
                    Assert.That(clonedPrecision.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/leftAlt"),
                        "Tempo Mapper must inherit the configured Timeline precision modifier.");
                    Assert.That(sourcePrecision.enabled, Is.EqualTo(sourceEnabled),
                        "Tempo Mapper must not enable the 3D timeline's source action.");
                }
                finally
                {
                    sourcePrecision.LoadBindingOverridesFromJson(previousBindings);
                    installPrecision.Invoke(view, null);
                }

                Assert.That(preview.Spectrogram, Is.SameAs(spectrogram), "Reuse the existing spectrogram instance.");
                Assert.That(spectrogram.parent, Is.Not.SameAs(parent));
                Assert.That(view.GetComponentsInChildren<TempoGridOverlay>(), Has.Length.EqualTo(3));
                Canvas.ForceUpdateCanvases();
                if (Settings.Instance.TempoEditorDisplay == 2)
                {
                    var waveBounds = (RectTransform)view.GetComponentsInChildren<TempoWaveformGraphic>()
                        .Single(wave => wave.transform.parent.name == "Editing waveform").transform.parent;
                    Assert.That(waveBounds.rect.height, Is.EqualTo(spectrogram.rect.height).Within(0.1),
                        "Both must give waveform and spectrogram equal editing heights.");
                }
                var canvas = view.GetComponentInParent<Canvas>();
                var canvasBounds = ((RectTransform)canvas.transform).rect;
                var corners = new Vector3[4];
                foreach (var button in view.GetComponentsInChildren<ButtonComponent>())
                {
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var local = canvas.transform.InverseTransformPoint(corner);
                        Assert.That(local.y, Is.InRange(canvasBounds.yMin - 1, canvasBounds.yMax + 1),
                            "Tempo controls must fit inside the canvas.");
                    }
                }
                var synthetic = new TempoMap(new[] { new BpmInfoBpmRegion { StartSampleIndex = 0, EndSampleIndex = 479999, StartBeat = 0, EndBeat = 20 } }, 48000);
                typeof(TempoMapperView).GetProperty("Map").SetValue(view, synthetic);
                typeof(TempoMapperView).GetProperty("Subdivision").SetValue(view, 1);
                view.Seek(0);
                view.Zoom(100, synthetic.EndSample / 2);
                Canvas.ForceUpdateCanvases();
                var editingGrid = view.GetComponentsInChildren<TempoGridOverlay>().First(grid => !grid.Overview);
                var rect = editingGrid.rectTransform.rect;
                var point = editingGrid.transform.TransformPoint(new Vector3(rect.xMin + rect.width * 0.25f, rect.center.y));
                var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, point), button = PointerEventData.InputButton.Left };
                editingGrid.OnPointerDown(pointer);
                editingGrid.OnPointerUp(pointer);
                Assert.That(view.Sample, Is.EqualTo(0), "Creating a split must not seek the audio cursor.");
                Assert.That(view.Map.Regions, Has.Count.EqualTo(2));
                var previousRectify = Settings.Instance.TempoWaveformRectifyPeaks;
                var previousInstantTransitions = Settings.Instance.InstantEscapeMenuTransitions;
                Settings.Instance.InstantEscapeMenuTransitions = true;
                var settingsButton = view.GetComponentsInChildren<ButtonComponent>()
                    .Single(button => button.GetComponentsInChildren<UnityEngine.UI.Image>().Any(image => image.name == "settings icon"));
                settingsButton.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
                yield return new WaitUntil(() => SceneManager.GetSceneByName("04_Options").isLoaded);
                yield return null;
                yield return null;
                var manager = Object.FindAnyObjectByType<TabManager>();
                Assert.That(manager.SelectedTab.name, Is.EqualTo("Tempo Mapper Tab"));
                var bindings = Object.FindObjectsByType<TempoMapperSettingBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(bindings.All(binding => binding.Caption != null), Is.True, "Every setting must have a visible label reference.");
                Canvas.ForceUpdateCanvases();
                var hitSounds = Object.FindObjectsByType<SettingsBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(binder => binder.BindedSetting == nameof(Settings.NoteHitSound));
                var sourceRow = hitSounds.GetComponentInParent<SearchableOption>();
                var sourceCaption = sourceRow.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(text => text.name == "Description");
                var sourceDropdown = sourceRow.GetComponentInChildren<TMPro.TMP_Dropdown>(true);
                foreach (var binding in bindings.Where(binding => binding.Dropdown != null))
                {
                    Assert.That(binding.Caption.color, Is.EqualTo(sourceCaption.color));
                    Assert.That(binding.Caption.fontSize, Is.EqualTo(sourceCaption.fontSize));
                    Assert.That(binding.Caption.rectTransform.anchoredPosition, Is.EqualTo(sourceCaption.rectTransform.anchoredPosition));
                    Assert.That(((RectTransform)binding.Dropdown.transform).anchoredPosition,
                        Is.EqualTo(((RectTransform)sourceDropdown.transform).anchoredPosition));
                    CollectionAssert.AreEqual(binding.Values.Length == 2 ? new[] { "Waveform", "Spectograph" } :
                        new[] { "Waveform", "Spectograph", "Both" }, binding.Dropdown.options.Select(option => option.text).ToArray());
                }
                var rectify = bindings.Single(binding => binding.SettingName == nameof(Settings.TempoWaveformRectifyPeaks));
                var follow = bindings.Single(binding => binding.SettingName == nameof(Settings.TempoFollowPlayback));
                foreach (var binding in new[] { rectify, follow })
                {
                    Assert.That(binding.Toggle.isActiveAndEnabled, Is.True, binding.SettingName + " must be visible and interactive.");
                    Assert.That(binding.Caption.isActiveAndEnabled, Is.True);
                    Assert.That(((RectTransform)binding.transform).rect.height, Is.GreaterThan(0));
                }
                var previousFollow = Settings.Instance.TempoFollowPlayback;
                follow.Toggle.SetUiOn(!previousFollow);
                Assert.That(Settings.Instance.TempoFollowPlayback, Is.EqualTo(!previousFollow));
                follow.Toggle.SetUiOn(previousFollow);

                rectify.Toggle.SetUiOn(!previousRectify);
                Assert.That(Settings.Instance.TempoWaveformRectifyPeaks, Is.EqualTo(!previousRectify));
                var options = Object.FindAnyObjectByType<OptionsController>();
                Assert.That(options.GetComponentInParent<Canvas>().rootCanvas.sortingOrder, Is.GreaterThan(canvas.rootCanvas.sortingOrder));
                options.Close();
                yield return new WaitUntil(() => !SceneManager.GetSceneByName("04_Options").isLoaded);
                Assert.That(OptionsController.IsActive, Is.False);
                Assert.That(view.GetComponentsInChildren<TempoWaveformGraphic>().All(wave => wave.RectifyPeaks == !previousRectify), Is.True);
                Settings.Instance.TempoWaveformRectifyPeaks = previousRectify;
                Settings.Instance.InstantEscapeMenuTransitions = previousInstantTransitions;
                view.Seek(120000);
                view.Split(120000);
                Assert.That(view.Map.Regions, Has.Count.GreaterThanOrEqualTo(2));
                view.BeginGesture();
                var before = view.Map.Copy();
                view.Drag(1, 0, 0, 130000);
                view.EndGesture(true);
                Assert.That(view.Map.Regions, Is.EqualTo(before.Regions), "Cancel restores the complete gesture snapshot.");
                view.GetComponent<DialogBox>().Close();
                yield return null;
                Assert.That(spectrogram.parent, Is.SameAs(parent));
                Assert.That(spectrogram.sizeDelta, Is.EqualTo(originalSize));
                Assert.That(Object.FindAnyObjectByType<TempoMapperView>(), Is.Null);
            }
            finally
            {
                container.LoadedSong = oldClip;
                Object.Destroy(clip);
            }
        }
    }
}
