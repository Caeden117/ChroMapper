using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Info;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Dedicated audio-time workspace borrowing the song menu's existing spectrogram and CMUI controls.</summary>
public class TempoMapperView : MonoBehaviour
{
    public TempoMap Map { get; private set; }
    public int Sample { get; private set; }
    public int SelectedRegion { get; private set; }
    public double ViewStart { get; private set; }
    public double ViewEnd { get; private set; }

    private DialogBox dialog;
    private AudioPreviewGenerator preview;
    private DifficultySelect difficulties;
    private AudioSource music;
    private AudioSource metronome;
    private AudioClip tick;
    private TempoGridOverlay grid;
    private TextComponent position;
    private TextComponent status;
    private TextBoxFloatComponent bpm;
    private TextBoxFloatComponent beats;
    private TextBoxIntComponent subdivisionField;
    private InputAction precisionModifier;
    private InputAction preciseModifier;
    private InputAction precisionScroll;
    private bool playbackClockRunning;
    private double playbackClockTime;
    private double playbackClockSample;
    private float playbackClockPitch = 1;
    private int audioDriftTolerance;
    private double lastReadoutTime = double.NegativeInfinity;
    private double previousZoomFactor = double.NaN;
    public bool ShiftTimeline { get; private set; }
    public bool IsFollowingPlayback => music != null && music.isPlaying && Settings.Instance.TempoFollowPlayback;
    public int Subdivision { get; private set; }
    private TempoGridOverlay overview;
    private TempoWaveformGraphic overviewWaveform;
    private TempoWaveformGraphic waveform;
    private TempoGridOverlay waveformGrid;
    private Sprite resetIcon;
    private Sprite[] toolIcons;
    private RawImage overviewSpectrogram;
    private Material overviewMaterial;
    private RawImage mainSpectrogram;
    private Material originalSpectrogramMaterial;
    private Material scrollingSpectrogramMaterial;
    private Slider zoomSlider;
    private TextComponent zoomReadout;
    private Coroutine waveformGeneration;
    private CanvasGroup spectrumVisibility;
    private bool ownsSpectrumVisibility;
    private float spectrumAlpha;
    private bool spectrumRaycasts;
    private RectTransform workspacePanel;
    private ButtonComponent play;
    private Image playIcon;
    private TempoBeatRuler beatRuler;
    private readonly Stack<TempoMap> undo = new();
    private readonly Stack<TempoMap> redo = new();
    private TempoMap gesture;
    private TempoMap saved;
    private Slider sliderTemplate;
    private Button backTemplate;
    private Button saveTemplate;
    private int previousBeat = -1;
    private RectTransform spectrogram;
    private Transform originalParent;
    private int originalSibling;
    private Vector2 originalMin, originalMax, originalPosition, originalSize, originalPivot;
    private Vector3 originalScale;
    private bool selectionVisible;
    private LayoutElement spectrogramLayout;
    private bool ownsSpectrogramLayout;
    private bool restored;
    private bool confirmationOpen;
    private readonly Dictionary<ImageWithRoundedCorners, float> originalRadii = new();
    private readonly Dictionary<ImageWithIndependentRoundedCorners, Vector4> originalIndependentRadii = new();
    private readonly Dictionary<Image, (Sprite sprite, Image.Type type)> originalMaskImages = new();
    private float workspaceScale = 1;

    private static string Text(string key, params object[] args) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", "tempo." + key, args);

    public static void Open(AudioClip clip, AudioPreviewGenerator preview, DifficultySelect difficulties, AudioClip tick, Slider sliderTemplate = null, Button backTemplate = null, Button saveTemplate = null, Sprite resetIcon = null, Sprite[] toolIcons = null)
    {
        DialogBox dialog = null;
        try
        {
            var info = BeatSaberSongContainer.Instance.Info;
            var map = TempoMapFileStore.Load(info, difficulties.CurrentDiff, clip.samples - 1, clip.frequency);
            dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("SongEditMenu", "tempo.title");
            var view = dialog.gameObject.AddComponent<TempoMapperView>();
            view.dialog = dialog;
            view.preview = preview;
            view.difficulties = difficulties;
            view.tick = tick;
            view.sliderTemplate = sliderTemplate;
            view.backTemplate = backTemplate;
            view.saveTemplate = saveTemplate;
            view.resetIcon = resetIcon;
            view.toolIcons = toolIcons;
            view.Map = map;
            view.saved = map.Copy();
            view.Build(clip);
            dialog.OnCloseRequested(view.RequestClose).Open();
            view.FitToCanvas();
            view.ApplyDisplaySettings();
            view.RebuildWaveform();
            view.InstallPrecisionInput();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (dialog != null) dialog.Close();
            PersistentUI.Instance.ShowDialogBox(Text("error", exception.Message), null, PersistentUI.DialogBoxPresetType.Ok);
        }
    }

    private void Build(AudioClip clip)
    {
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.clip = clip;
        music.volume = Settings.Instance.SongVolume;
        metronome = gameObject.AddComponent<AudioSource>();
        metronome.playOnAwake = false;
        metronome.volume = Settings.Instance.MetronomeVolume;

        var anchor = dialog.AddComponent<TextComponent>();
        var body = anchor.transform.parent;
        workspacePanel = (RectTransform)body.parent;
        workspacePanel.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
        anchor.gameObject.SetActive(false);
        Destroy(anchor.gameObject);
        var bodyGroup = body.GetComponent<VerticalLayoutGroup>();
        bodyGroup.padding.bottom = 4;
        bodyGroup.childAlignment = TextAnchor.UpperLeft;
        var bodySize = body.GetComponent<LayoutElement>();
        if (bodySize == null) bodySize = body.gameObject.AddComponent<LayoutElement>();
        bodySize.layoutPriority = 100;
        bodySize.minHeight = bodySize.preferredHeight = 0;
        bodySize.flexibleHeight = 1;
        var workspace = Row(body, "Tempo workspace");
        SetHeight(workspace, 390);
        workspace.GetComponent<LayoutElement>().minHeight = 180;
        workspace.GetComponent<LayoutElement>().flexibleHeight = 1;
        workspace.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        workspace.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        var sidebar = new GameObject("Controls", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        sidebar.transform.SetParent(workspace, false);
        var sideLayout = sidebar.GetComponent<LayoutElement>();
        sideLayout.layoutPriority = 100;
        sideLayout.minWidth = sideLayout.preferredWidth = 56;
        sideLayout.flexibleWidth = 0;
        var sideGroup = sidebar.GetComponent<VerticalLayoutGroup>();
        sideGroup.spacing = 10;
        sideGroup.childAlignment = TextAnchor.UpperCenter;
        sideGroup.padding = new RectOffset(0, 0, 6, 0);
        sideGroup.childControlHeight = sideGroup.childControlWidth = true;
        sideGroup.childForceExpandHeight = false;
        sideGroup.childForceExpandWidth = false;
        ToolButton(sidebar.transform, "undo", Undo, 0);
        ToolButton(sidebar.transform, "redo", Redo, 1);
        var shiftLabel = dialog.AddComponent<TextComponent>().WithInitialValue("SongEditMenu", "tempo.shift");
        shiftLabel.transform.SetParent(sidebar.transform, false);
        SetHeight(shiftLabel, 20);
        SetWidth(shiftLabel, 56, 0);
        shiftLabel.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Center;
        var shift = dialog.AddComponent<ToggleComponent>().WithNoLabel()
            .OnChanged<ToggleComponent, bool>(value => ShiftTimeline = value);
        shift.transform.SetParent(sidebar.transform, false);
        shift.gameObject.AddComponent<Tooltip>().TooltipOverride = "Drag to shift all tempo boundaries together. The song origin stays fixed.";
        CompactControl(shift, 56);
        var checkbox = shift.GetComponentInChildren<Toggle>(true);
        var checkboxRect = (RectTransform)checkbox.transform;
        checkboxRect.anchorMin = checkboxRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkboxRect.anchoredPosition = Vector2.zero;
        var center = new GameObject("Timeline", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        center.transform.SetParent(workspace, false);
        var centerSize = center.GetComponent<LayoutElement>();
        centerSize.layoutPriority = 100;
        centerSize.minWidth = centerSize.preferredWidth = 0;
        centerSize.flexibleWidth = 1;
        var centerLayout = center.GetComponent<VerticalLayoutGroup>();
        centerLayout.spacing = 4;
        centerLayout.childControlHeight = centerLayout.childControlWidth = true;
        centerLayout.childForceExpandHeight = false;

        spectrogram = preview.Spectrogram;
        foreach (var corner in spectrogram.GetComponentsInChildren<ImageWithRoundedCorners>(true))
        {
            originalRadii[corner] = corner.radius;
            corner.radius = 0;
            corner.Refresh();
        }
        foreach (var corner in spectrogram.GetComponentsInChildren<ImageWithIndependentRoundedCorners>(true))
        {
            originalIndependentRadii[corner] = corner.r;
            corner.r = Vector4.zero;
            corner.Refresh();
        }
        foreach (var mask in spectrogram.GetComponentsInChildren<Mask>(true))
        {
            var image = mask.GetComponent<Image>();
            if (image == null) continue;
            originalMaskImages[image] = (image.sprite, image.type);
            image.sprite = null;
            image.type = Image.Type.Simple;
        }
        originalParent = spectrogram.parent;
        originalSibling = spectrogram.GetSiblingIndex();
        originalMin = spectrogram.anchorMin;
        originalMax = spectrogram.anchorMax;
        originalPosition = spectrogram.anchoredPosition;
        originalSize = spectrogram.sizeDelta;
        originalPivot = spectrogram.pivot;
        originalScale = spectrogram.localScale;
        selectionVisible = preview.PreviewSelection.activeSelf;
        preview.PreviewSelection.SetActive(false);
        var overviewObject = new GameObject("Song overview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        overviewObject.transform.SetParent(center.transform, false);
        overviewObject.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.09f);
        SetHeight(overviewObject.transform, 54);
        var source = spectrogram.GetComponentInChildren<RawImage>(true);
        if (source != null)
        {
            mainSpectrogram = source;
            originalSpectrogramMaterial = source.material;
            scrollingSpectrogramMaterial = new Material(source.material);
            source.material = scrollingSpectrogramMaterial;
            var imageObject = new GameObject("Overview spectrogram", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(LayoutElement));
            imageObject.transform.SetParent(overviewObject.transform, false);
            imageObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;
            overviewSpectrogram = imageObject.GetComponent<RawImage>();
            overviewSpectrogram.raycastTarget = false;
            overviewSpectrogram.texture = source.texture;
            overviewMaterial = new Material(source.material);
            overviewMaterial.SetInteger("_Stencil", 0);
            overviewMaterial.SetFloat("_TempoOverview", 1);
            overviewMaterial.SetFloat("_TempoSongDuration", clip.length);
            overviewSpectrogram.material = overviewMaterial;
        }
        overviewWaveform = AddWaveform(overviewObject.transform);
        overview = AddGrid(overviewObject.transform, true);

        var inspector = Row(center.transform, "Statistics and playback");
        bpm = dialog.AddComponent<TextBoxFloatComponent>().WithLabel("SongEditMenu", "tempo.bpm");
        bpm.transform.SetParent(inspector, false);
        bpm.OnEndEdit(value => Edit(m => m.SetBpm(SelectedRegion, value)));
        CompactControl(bpm, 160);
        beats = dialog.AddComponent<TextBoxFloatComponent>().WithLabel("SongEditMenu", "tempo.beats");
        beats.transform.SetParent(inspector, false);
        beats.OnEndEdit(value => Edit(m => m.SetBeats(SelectedRegion, value)));
        CompactControl(beats, 150);
        play = ToolButton(inspector, "play", TogglePlaying, 3);
        SetHeight(play, 32);
        SetHeight(inspector, 32);
        inspector.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        foreach (var control in new Component[] { bpm, beats, play })
        {
            foreach (var size in control.GetComponentsInChildren<LayoutElement>(true))
            {
                size.layoutPriority = 100;
                size.minHeight = 0;
                size.preferredHeight = 32;
            }
            SetHeight(control, 32);
            foreach (var layout in control.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                layout.childControlHeight = layout.childForceExpandHeight = true;
                layout.padding.top = layout.padding.bottom = 0;
            }
        }
        position = dialog.AddComponent<TextComponent>();
        position.transform.SetParent(inspector, false);
        SetWidth(position, 150, 1);

        var rulerObject = new GameObject("Beat numbers", typeof(RectTransform), typeof(LayoutElement), typeof(TempoBeatRuler));
        rulerObject.transform.SetParent(center.transform, false);
        SetHeight(rulerObject.transform, 20);
        beatRuler = rulerObject.GetComponent<TempoBeatRuler>();
        beatRuler.View = this;
        beatRuler.Font = position.GetComponentInChildren<TMP_Text>().font;
        var waveformObject = new GameObject("Editing waveform", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        waveformObject.transform.SetParent(center.transform, false);
        waveformObject.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.09f);
        SetHeight(waveformObject.transform, 64);
        waveform = AddWaveform(waveformObject.transform);
        waveformGrid = AddGrid(waveformObject.transform, false);

        spectrogram.SetParent(center.transform, false);
        spectrogram.localScale = Vector3.one;
        spectrogramLayout = spectrogram.GetComponent<LayoutElement>();
        ownsSpectrogramLayout = spectrogramLayout == null;
        if (ownsSpectrogramLayout) spectrogramLayout = spectrogram.gameObject.AddComponent<LayoutElement>();
        spectrumVisibility = spectrogram.GetComponent<CanvasGroup>();
        ownsSpectrumVisibility = spectrumVisibility == null;
        if (ownsSpectrumVisibility) spectrumVisibility = spectrogram.gameObject.AddComponent<CanvasGroup>();
        spectrumAlpha = spectrumVisibility.alpha;
        spectrumRaycasts = spectrumVisibility.blocksRaycasts;
        spectrogramLayout.preferredHeight = 160;
        spectrogramLayout.minHeight = 120;
        spectrogramLayout.flexibleHeight = 1;
        grid = AddGrid(spectrogram, false);

        var options = Row(body, "Settings");
        SetHeight(options, 60);
        var defaultZoom = (float)(Math.Log(Math.Max(1, Map.EndSample / (Map.Frequency * 12d)), 2) / 8);
        AddSettingSlider(options, "zoom", 0, 1, defaultZoom, SetZoom, value => $"{Math.Pow(2, value * 8):F1}x");
        AddSettingSlider(options, "speed", 0.25f, 1.5f, 1, SetPlaybackSpeed, value => $"{value:F2}x");
        Subdivision = Math.Clamp(Settings.Instance.CursorPrecisionA, 1, 64);
        var subdivision = SettingColumn(options, "precision");
        subdivision.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
        var precision = dialog.AddComponent<TextBoxIntComponent>().WithNoLabel();
        subdivisionField = precision;
        if (precision.ScrollableInput != null) precision.ScrollableInput.enabled = false;
        precision.transform.SetParent(subdivision, false);
        SetWidth(precision, 44, 0);
        SetHeight(precision, 26);
        // CMUI's numeric field expects dialog-sized minimum heights. This compact input needs explicit bounds.
        foreach (var element in precision.GetComponentsInChildren<LayoutElement>(true))
        {
            element.layoutPriority = 100;
            element.minHeight = 0;
            element.preferredHeight = 26;
        }
        foreach (var group in precision.GetComponentsInChildren<HorizontalLayoutGroup>(true))
        {
            group.padding = new RectOffset();
            group.childForceExpandWidth = group.childForceExpandHeight = true;
        }
        precision.SetValueWithoutNotify(Subdivision);
        precision.OnEndEdit(value =>
        {
            SetSubdivision(value);
        });
        AddSettingSlider(options, "metronome", 0, 1, Settings.Instance.MetronomeVolume,
            value => metronome.volume = value, value => $"{value:P0}");
        AddSettingSlider(options, "volume", 0, 1, Settings.Instance.SongVolume,
            value => music.volume = value, value => $"{value:P0}");
        status = dialog.AddComponent<TextComponent>();
        SetHeight(status, 20);
        var header = workspacePanel.GetChild(0);
        var save = Button(header, "save", Save);
        PlaceHeaderControl(save, 6, 70);
        var settingsButton = ToolButton(header, "settings", OpenSettings, 2);
        PlaceHeaderControl(settingsButton, 84, 28);
        if (saveTemplate != null)
        {
            save.WithBackgroundColor(saveTemplate.targetGraphic.color);
            var target = save.GetComponentInChildren<Button>();
            target.colors = saveTemplate.colors;
        }
        else save.WithBackgroundColor(new Color(0.313025f, 0.3572f, 0.475f));
        AddBackButton();
        foreach (var text in dialog.GetComponentsInChildren<TMP_Text>(true))
        {
            text.fontSize = 14;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = 14;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
        ViewEnd = Math.Min(Map.EndSample, Map.Frequency * 12d);
        Refresh();
    }

    private ButtonComponent ToolButton(Transform parent, string key, Action action, int iconIndex)
    {
        var button = Button(parent, key, action);
        SetWidth(button, 32, 0);
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        var iconObject = new GameObject(key + " icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(button.transform, false);
        iconObject.GetComponent<LayoutElement>().ignoreLayout = true;
        var image = iconObject.GetComponent<Image>();
        image.sprite = toolIcons != null && iconIndex < toolIcons.Length ? toolIcons[iconIndex] : resetIcon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var rect = (RectTransform)iconObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6, 4);
        rect.offsetMax = new Vector2(-6, -4);
        var tooltip = button.GetComponent<Tooltip>();
        if (tooltip == null) tooltip = button.gameObject.AddComponent<Tooltip>();
        tooltip.enabled = true;
        tooltip.TooltipOverride = Text(key);
        if (key == "play") playIcon = image;
        return button;
    }

    private void RebuildWaveform()
    {
        if (waveformGeneration != null) StopCoroutine(waveformGeneration);
        waveformGeneration = StartCoroutine(TempoWaveformData.Generate(music.clip, Settings.Instance.TempoWaveformSamplesPerPeak, data =>
        {
            waveform.Data = overviewWaveform.Data = data;
            waveform.SetVerticesDirty();
            overviewWaveform.SetVerticesDirty();
            waveformGeneration = null;
        }));
    }

    private void ApplyDisplaySettings()
    {
        var settings = Settings.Instance;
        var waveformVisible = settings.TempoEditorDisplay != 1;
        var spectrumVisible = settings.TempoEditorDisplay != 0;
        waveform.transform.parent.gameObject.SetActive(waveformVisible);
        spectrumVisibility.alpha = spectrumVisible ? 1 : 0;
        spectrumVisibility.blocksRaycasts = spectrumVisible;
        spectrogramLayout.ignoreLayout = !spectrumVisible;
        var bothVisible = waveformVisible && spectrumVisible;
        SetHeight(waveform.transform.parent, (bothVisible ? 112 : 230) * workspaceScale);
        var waveSize = waveform.transform.parent.GetComponent<LayoutElement>();
        // Equal minimum, preferred and flexible sizes keep Both evenly split at every window size.
        waveSize.minHeight = bothVisible ? 50 : 40;
        waveSize.flexibleHeight = bothVisible ? 1 : 3;
        spectrogramLayout.preferredHeight = (bothVisible ? 112 : 230) * workspaceScale;
        spectrogramLayout.minHeight = bothVisible ? 50 : 60;
        spectrogramLayout.flexibleHeight = bothVisible ? 1 : 2;
        overviewWaveform.enabled = settings.TempoOverviewDisplay == 0;
        if (overviewSpectrogram != null) overviewSpectrogram.enabled = settings.TempoOverviewDisplay == 1;
        foreach (var graphic in new[] { waveform, overviewWaveform })
        {
            graphic.MaximumColumns = settings.TempoWaveformMaxColumns;
            graphic.RectifyPeaks = settings.TempoWaveformRectifyPeaks;
            graphic.SetVerticesDirty();
        }
        if (scrollingSpectrogramMaterial != null) scrollingSpectrogramMaterial.SetFloat("_TempoAntialias", settings.TempoSpectrogramAntialias ? 1 : 0);
        if (overviewMaterial != null) overviewMaterial.SetFloat("_TempoAntialias", settings.TempoSpectrogramAntialias ? 1 : 0);
        Shader.SetGlobalFloat("_Spectrogram_BilinearFiltering", settings.SpectrogramBilinearFiltering ? 1 : 0);
        Shader.SetGlobalFloat("_Spectrogram_Shift", settings.SpectrogramShift);
        // FFT generation lives on the borrowed preview. Keep it active if only the overview needs its buffers.
        if (spectrumVisible || settings.TempoOverviewDisplay == 1) preview.PrepareTempoMapper();
        Refresh();
    }

    private void OpenSettings()
    {
        if (OptionsController.IsActive) return;
        PausePlayback();
        var previousResolution = Settings.Instance.TempoWaveformSamplesPerPeak;
        try
        {
            OptionsController.ShowOptions(OptionsController.TempoMapperGroup, closed: () =>
        {
            if (this == null) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            ApplyDisplaySettings();
            if (previousResolution != Settings.Instance.TempoWaveformSamplesPerPeak) RebuildWaveform();
            InstallPrecisionInput();
        });
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void SetSubdivision(int value)
    {
        Subdivision = Math.Clamp(value, 1, 64);
        Settings.Instance.CursorPrecisionA = Subdivision;
        Settings.NonPersistentSettings[AudioTimeSyncController.PrecisionSnapName] = Subdivision;
        subdivisionField.SetValueWithoutNotify(Subdivision);
        Refresh();
    }

    public bool PrecisionScrollActive => precisionModifier != null && precisionModifier.IsPressed();

    private void InstallPrecisionInput()
    {
        precisionModifier?.Dispose();
        preciseModifier?.Dispose();
        precisionScroll?.Dispose();
        var asset = CMInputCallbackInstaller.InputInstance?.asset;
        if (asset == null) return;
        // Clone only these actions so the modal can keep the 3D timeline disabled. Cloning retains binding overrides.
        InputAction Clone(string path)
        {
            var source = asset.FindAction(path, true);
            var action = source.Clone();
            action.bindingMask = source.bindingMask ?? source.actionMap?.bindingMask ?? asset.bindingMask;
            return action;
        }
        precisionModifier = Clone("Timeline/Change Precision Modifier");
        preciseModifier = Clone("Timeline/Precise Snap Modification");
        precisionScroll = Clone("Timeline/+Change Time and Precision");
        precisionModifier.wantsInitialStateCheck = preciseModifier.wantsInitialStateCheck = true;
        precisionScroll.performed += context =>
        {
            if (!Application.isFocused || OptionsController.IsActive || confirmationOpen || !PrecisionScrollActive) return;
            var scroll = context.ReadValue<float>();
            if (scroll != 0) SetSubdivision(TempoCursorPrecision.Step(Subdivision, scroll, preciseModifier.IsPressed()));
        };
        precisionModifier.Enable();
        preciseModifier.Enable();
        precisionScroll.Enable();
    }

    private void ResetPlaybackClock(double sample)
    {
        playbackClockTime = Time.realtimeSinceStartupAsDouble;
        playbackClockSample = sample;
        playbackClockPitch = music.pitch;
        playbackClockRunning = music.isPlaying;
        AudioSettings.GetDSPBufferSize(out var size, out _);
        audioDriftTolerance = Math.Max(size * 2, music.clip.frequency / 10);
    }

    private double PlaybackSampleNow() => playbackClockSample
        + (Time.realtimeSinceStartupAsDouble - playbackClockTime) * music.clip.frequency * playbackClockPitch;

    private void SetPlaybackSpeed(float value)
    {
        var sample = playbackClockRunning ? PlaybackSampleNow() : Sample;
        music.pitch = value;
        ResetPlaybackClock(sample);
    }

    private void PausePlayback()
    {
        music.Pause();
        playbackClockRunning = false;
        Sample = Math.Clamp(music.timeSamples, 0, Map.EndSample);
        SelectedRegion = Map.RegionAt(Sample);
        Refresh(false);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) playbackClockRunning = false;
    }

    private Transform SettingColumn(Transform parent, string key)
    {
        var column = new GameObject(key, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        column.transform.SetParent(parent, false);
        SetWidth(column.transform, key == "precision" ? 110 : 130, key == "precision" ? 0 : 1);
        SetHeight(column.transform, 56);
        var group = column.GetComponent<VerticalLayoutGroup>();
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
        group.spacing = 6;
        var caption = dialog.AddComponent<TextComponent>().WithInitialValue("SongEditMenu", "tempo." + key);
        caption.transform.SetParent(column.transform, false);
        SetHeight(caption, 20);
        SetWidth(caption, key == "precision" ? 110 : 130, 1);
        return column.transform;
    }

    private void AddSettingSlider(Transform parent, string key, float minimum, float maximum, float defaultValue,
        Action<float> changed, Func<float, string> format)
    {
        var column = SettingColumn(parent, key);
        var controls = Row(column, key + " slider");
        SetHeight(controls, 26);
        SetWidth(controls, 130, 1);
        var readout = dialog.AddComponent<TextComponent>();
        readout.transform.SetParent(controls, false);
        SetWidth(readout, 32, 0);
        SetHeight(readout, 26);
        // Reuse the settings screen's horizontal slider, including its fill and handle graphics.
        Slider slider;
        if (sliderTemplate != null)
        {
            slider = Instantiate(sliderTemplate, controls, false);
            slider.onValueChanged = new Slider.SliderEvent();
            foreach (var label in slider.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        }
        else
        {
            var fallback = dialog.AddComponent<SliderComponent>().WithNoLabel().WithDisplay(false);
            fallback.transform.SetParent(controls, false);
            slider = fallback.GetComponentInChildren<Slider>(true);
        }
        slider.minValue = minimum;
        slider.maxValue = key == "zoom"
            ? Math.Max(1, (float)(Math.Log(Math.Max(1, Map.EndSample / (Map.Frequency * 0.1)), 2) / 8)) : maximum;
        slider.wholeNumbers = false;
        SetWidth(slider, 50, 1);
        SetHeight(slider, 26);
        slider.onValueChanged.AddListener(value => { changed(value); readout.Value = format(value); });
        if (key == "zoom") { zoomSlider = slider; zoomReadout = readout; }
        var reset = Button(controls, "reset", () =>
        {
            slider.SetValueWithoutNotify(defaultValue);
            changed(defaultValue);
            readout.Value = format(defaultValue);
        });
        foreach (var label in reset.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        var iconObject = new GameObject("Reset icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(reset.transform, false);
        iconObject.GetComponent<LayoutElement>().ignoreLayout = true;
        var icon = iconObject.GetComponent<Image>();
        icon.sprite = resetIcon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var rect = (RectTransform)icon.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(5, 5);
        rect.offsetMax = new Vector2(-5, -5);
        if (resetIcon == null)
        {
            iconObject.SetActive(false);
            var fallback = reset.GetComponentInChildren<TMP_Text>(true);
            fallback.gameObject.SetActive(true);
            fallback.text = "\u21BA";
        }
        var tooltip = reset.GetComponent<Tooltip>();
        if (tooltip == null) tooltip = reset.gameObject.AddComponent<Tooltip>();
        tooltip.enabled = true;
        tooltip.TooltipOverride = Text("reset") + " " + Text(key);
        SetWidth(reset, 26, 0);
        SetHeight(reset, 26);
        slider.SetValueWithoutNotify(defaultValue);
        // Initial values are already applied by Build. In particular, zoom must not refresh a half-built view.
        readout.Value = format(defaultValue);
    }

    private static void PlaceHeaderControl(Component control, float right, float width)
    {
        var size = control.GetComponent<LayoutElement>();
        if (size == null) size = control.gameObject.AddComponent<LayoutElement>();
        size.ignoreLayout = true;
        var rect = (RectTransform)control.transform;
        rect.anchorMin = new Vector2(1, 0.15f);
        rect.anchorMax = new Vector2(1, 0.85f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.sizeDelta = new Vector2(width, 0);
        rect.anchoredPosition = new Vector2(-right, 0);
    }

    private void AddBackButton()
    {
        var body = workspacePanel;
        var header = body.GetChild(0);
        Button back;
        if (backTemplate != null)
        {
            back = Instantiate(backTemplate, header, false);
            back.onClick = new Button.ButtonClickedEvent();
            back.onClick.AddListener(RequestClose);
        }
        else
        {
            var component = dialog.AddComponent<ButtonComponent>().OnClick(RequestClose).WithLabel("<");
            component.transform.SetParent(header, false);
            back = component.GetComponentInChildren<Button>();
        }
        back.gameObject.SetActive(true);
        var rect = (RectTransform)back.transform;
        var layout = back.GetComponent<LayoutElement>();
        if (layout == null) layout = back.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        rect.anchorMin = new Vector2(0, 0.15f);
        rect.anchorMax = new Vector2(0, 0.85f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(22, 0);
        rect.anchoredPosition = new Vector2(6, 0);
        foreach (RectTransform child in rect)
        {
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = new Vector2(3, 3);
            child.offsetMax = new Vector2(-3, -3);
        }
        var tooltip = back.GetComponent<Tooltip>();
        if (tooltip == null) tooltip = back.gameObject.AddComponent<Tooltip>();
        tooltip.TooltipOverride = Text("close");
    }

    private static Transform Row(Transform body, string name)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(body, false);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.GetComponent<LayoutElement>().preferredHeight = 32;
        return row.transform;
    }

    private static void SetWidth(Component component, float width, float flexible)
    {
        var element = component.GetComponent<LayoutElement>();
        if (element == null) element = component.gameObject.AddComponent<LayoutElement>();
        element.layoutPriority = 100;
        element.minWidth = width;
        element.preferredWidth = width;
        element.flexibleWidth = flexible;
    }

    /// <summary>Keep CMUI controls, but arrange their existing label/value containers for a compact workspace.</summary>
    private static void CompactControl(Component control, float width)
    {
        SetWidth(control, width, 0);
        SetHeight(control, 32);
        var group = control.GetComponent<HorizontalLayoutGroup>();
        if (group != null)
        {
            group.padding = new RectOffset(4, 4, 0, 0);
            group.spacing = 4;
            group.childForceExpandWidth = false;
        }
        var label = control.transform.Find("Label Container");
        if (label != null) SetWidth(label, width * 0.6f, 0);
        foreach (Transform child in control.transform)
            if (child != label) SetWidth(child, 24, 1);
    }

    private static void SetHeight(Component component, float height)
    {
        var layout = component.GetComponent<LayoutElement>();
        if (layout == null) layout = component.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = height;
        layout.flexibleHeight = 0;
    }

    private void FitToCanvas()
    {
        Canvas.ForceUpdateCanvases();
        var panel = workspacePanel;
        var available = ((RectTransform)dialog.transform).rect.height - 16;
        var preferred = LayoutUtility.GetPreferredHeight(panel);
        if (preferred <= available || preferred <= 0) return;
        var scale = available / preferred;
        workspaceScale = scale;
        foreach (var text in dialog.GetComponentsInChildren<TMP_Text>(true)) text.fontSize *= scale;
        foreach (var layout in dialog.GetComponentsInChildren<LayoutElement>(true))
        {
            if (layout.minHeight > 0) layout.minHeight *= scale;
            if (layout.preferredHeight > 0) layout.preferredHeight *= scale;
        }
        foreach (var layout in dialog.GetComponentsInChildren<VerticalLayoutGroup>(true))
        {
            layout.spacing *= scale;
            layout.padding.top = Mathf.FloorToInt(layout.padding.top * scale);
            layout.padding.bottom = Mathf.FloorToInt(layout.padding.bottom * scale);
        }
        Canvas.ForceUpdateCanvases();
    }

    private ButtonComponent Button(Transform row, string key, Action action)
    {
        var button = dialog.AddComponent<ButtonComponent>().WithLabel("SongEditMenu", "tempo." + key).OnClick(action);
        button.transform.SetParent(row, false);
        SetHeight(button, 28);
        SetWidth(button, 76, 0);
        var layout = button.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.childControlHeight = layout.childForceExpandHeight = true;
        return button;
    }

    private TempoWaveformGraphic AddWaveform(Transform parent)
    {
        var graphic = new GameObject("Waveform peaks", typeof(RectTransform), typeof(CanvasRenderer), typeof(TempoWaveformGraphic), typeof(LayoutElement));
        graphic.transform.SetParent(parent, false);
        graphic.GetComponent<LayoutElement>().ignoreLayout = true;
        var rect = (RectTransform)graphic.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var result = graphic.GetComponent<TempoWaveformGraphic>();
        result.raycastTarget = false;
        result.color = new Color(0.1f, 0.85f, 0.75f, 1);
        result.MaximumColumns = Settings.Instance.TempoWaveformMaxColumns;
        return result;
    }

    private TempoGridOverlay AddGrid(Transform parent, bool isOverview)
    {
        var overlay = new GameObject("Tempo grid", typeof(RectTransform), typeof(CanvasRenderer), typeof(TempoGridOverlay), typeof(LayoutElement));
        overlay.transform.SetParent(parent, false);
        overlay.layer = parent.gameObject.layer;
        overlay.GetComponent<LayoutElement>().ignoreLayout = true;
        var rect = (RectTransform)overlay.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var result = overlay.GetComponent<TempoGridOverlay>();
        result.View = this;
        result.Overview = isOverview;
        return result;
    }

    private void SetZoom(float value)
    {
        var target = Map.EndSample / Math.Pow(2, value * 8);
        Zoom(target / (ViewEnd - ViewStart), Sample);
    }

    public void ShiftGesture(int delta)
    {
        if (gesture == null) return;
        var next = gesture.Copy();
        if (!next.ShiftTimeline(delta)) return;
        Map = next;
        Refresh();
    }

    public void Seek(int sample)
    {
        Sample = Mathf.Clamp(sample, 0, Map.EndSample);
        SelectedRegion = Map.RegionAt(Sample);
        music.timeSamples = Math.Min(Sample, music.clip.samples - 1);
        ResetPlaybackClock(Sample);
        previousBeat = -1;
        EnsureVisible();
        Refresh();
    }

    public void Zoom(double factor, int center)
    {
        var span = Math.Clamp((ViewEnd - ViewStart) * factor, Map.Frequency * 0.1, Map.EndSample);
        ViewStart = Math.Clamp(center - span / 2, 0, Map.EndSample - span);
        ViewEnd = ViewStart + span;
        Refresh();
    }

    public void Pan(double samples)
    {
        var span = ViewEnd - ViewStart;
        ViewStart = Math.Clamp(ViewStart + samples, 0, Map.EndSample - span);
        ViewEnd = ViewStart + span;
        Refresh();
    }

    public void Split(int sample) => Edit(m => m.Split(sample));
    public void MergeBoundary(int right) => Edit(m => m.MergeNext(right - 1));

    private void Edit(Func<TempoMap, bool> operation)
    {
        var next = Map.Copy();
        if (!operation(next)) { status.gameObject.SetActive(true); status.Value = Text("invalid"); return; }
        try { next.Validate(); }
        catch (ArgumentException) { status.gameObject.SetActive(true); status.Value = Text("invalid"); return; }
        undo.Push(Map);
        redo.Clear();
        Map = next;
        SelectedRegion = Map.RegionAt(Sample);
        Refresh();
    }

    public void BeginGesture() => gesture = Map.Copy();

    public void Drag(int boundary, int region, double beat, int sample)
    {
        if (gesture == null) return;
        var next = gesture.Copy();
        var changed = boundary > 0 ? next.MoveBoundary(boundary, sample) : next.MoveBeat(region, beat, sample);
        if (!changed) return;
        Map = next;
        Refresh();
    }

    public void EndGesture(bool cancel = false)
    {
        if (gesture == null) return;
        if (cancel) Map = gesture;
        else if (!Map.Regions.SequenceEqual(gesture.Regions)) { undo.Push(gesture); redo.Clear(); }
        gesture = null;
        Refresh();
    }

    private void Undo()
    {
        if (undo.Count == 0) return;
        redo.Push(Map);
        Map = undo.Pop();
        Refresh();
    }

    private void Redo()
    {
        if (redo.Count == 0) return;
        undo.Push(Map);
        Map = redo.Pop();
        Refresh();
    }

    private void TogglePlaying()
    {
        if (music.isPlaying) PausePlayback();
        else { if (Sample >= Map.EndSample) Seek(0); music.Play(); ResetPlaybackClock(Sample); }
        if (playIcon != null && toolIcons != null && toolIcons.Length > 4)
            playIcon.sprite = toolIcons[music.isPlaying ? 4 : 3];
        var playTooltip = play.GetComponent<Tooltip>();
        if (playTooltip != null) playTooltip.TooltipOverride = Text(music.isPlaying ? "pause" : "play");
    }

    private void Update()
    {
        if (music == null || grid == null) return;
        if (music.isPlaying)
        {
            if (!playbackClockRunning) ResetPlaybackClock(music.timeSamples);
            var predicted = PlaybackSampleNow();
            // AudioSource.timeSamples advances in mixer blocks; use it for large-drift recovery, not frame positions.
            if (Math.Abs(predicted - music.timeSamples) > audioDriftTolerance)
            {
                var correction = Math.Clamp(music.timeSamples - predicted,
                    -music.clip.frequency * Time.unscaledDeltaTime * 0.02,
                    music.clip.frequency * Time.unscaledDeltaTime * 0.02);
                playbackClockSample += correction;
                predicted = PlaybackSampleNow();
            }
            Sample = (int)Math.Clamp(Math.Round(predicted), 0, Map.EndSample);
            SelectedRegion = Map.RegionAt(Sample);
            var beat = (int)Math.Floor(Map.BeatAt(Sample));
            if (beat != previousBeat && metronome.volume > 0 && tick != null) metronome.PlayOneShot(tick);
            previousBeat = beat;
            if (Settings.Instance.TempoFollowPlayback)
            {
                var span = ViewEnd - ViewStart;
                var pixels = Math.Max(1, grid.rectTransform.rect.width * grid.canvas.scaleFactor);
                var samplesPerPixel = span / pixels;
                // Translate every audio layer by the same whole-pixel step; fractional raster phase stays fixed.
                ViewStart = Math.Round((predicted - span / 2) / samplesPerPixel) * samplesPerPixel;
                ViewEnd = ViewStart + span;
            }
            Refresh(false);
        }
        else if (playbackClockRunning)
        {
            playbackClockRunning = false;
            Sample = Map.EndSample;
            Refresh();
        }
        var keyboard = Keyboard.current;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (confirmationOpen || OptionsController.IsActive || keyboard == null || (selected != null && selected.GetComponent<TMP_InputField>() is { isFocused: true })) return;
        if (keyboard.spaceKey.wasPressedThisFrame) TogglePlaying();
        if (keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed)
        {
            if (keyboard.zKey.wasPressedThisFrame) { if (keyboard.shiftKey.isPressed) Redo(); else Undo(); }
            if (keyboard.yKey.wasPressedThisFrame) Redo();
            if (keyboard.sKey.wasPressedThisFrame) Save();
        }
    }

    private void EnsureVisible()
    {
        var span = ViewEnd - ViewStart;
        if (Sample < ViewStart || Sample > ViewEnd)
        {
            ViewStart = Math.Clamp(Sample - span / 2, 0, Map.EndSample - span);
            ViewEnd = ViewStart + span;
        }
    }

    private void Refresh(bool inspector = true)
    {
        SelectedRegion = Math.Clamp(SelectedRegion, 0, Map.Regions.Count - 1);
        Shader.SetGlobalFloat("_ViewStart", (float)(ViewStart / Map.Frequency));
        Shader.SetGlobalFloat("_ViewEnd", (float)(ViewEnd / Map.Frequency));
        grid.SetVerticesDirty();
        overview.SetVerticesDirty();
        waveformGrid.SetVerticesDirty();
        if (beatRuler != null) beatRuler.Refresh();
        if (zoomSlider != null)
        {
            var factor = Map.EndSample / Math.Max(1, ViewEnd - ViewStart);
            if (factor != previousZoomFactor)
            {
                zoomSlider.SetValueWithoutNotify((float)(Math.Log(factor, 2) / 8));
                zoomReadout.Value = $"{factor:F1}x";
                previousZoomFactor = factor;
            }
        }
        waveform.SetWindow(ViewStart, ViewEnd);
        overviewWaveform.SetWindow(0, Map.EndSample);
        var r = Map.Regions[SelectedRegion];
        if (inspector || Time.realtimeSinceStartupAsDouble - lastReadoutTime >= 0.05)
        {
            lastReadoutTime = Time.realtimeSinceStartupAsDouble;
            var seconds = Sample / (double)Map.Frequency;
            position.Value = Text("statistics", (int)(seconds / 60), seconds % 60, Map.BeatAt(Sample), SelectedRegion + 1, Map.Regions.Count);
            if (playIcon != null && toolIcons != null && toolIcons.Length > 4)
                playIcon.sprite = toolIcons[music.isPlaying ? 4 : 3];
            var playTooltip = play.GetComponent<Tooltip>();
            if (playTooltip != null) playTooltip.TooltipOverride = Text(music.isPlaying ? "pause" : "play");
        }
        if (inspector)
        {
            bpm.SetValueWithoutNotify((float)Map.Bpm(SelectedRegion));
            beats.SetValueWithoutNotify(r.EndBeat - r.StartBeat);
            status.Value = "";
            status.gameObject.SetActive(false);
        }
    }

    private void Save()
    {
        try
        {
            EndGesture();
            TempoMapFileStore.Save(BeatSaberSongContainer.Instance.Info, Map,
                difficulties.Characteristics.Values.SelectMany(c => c.Values));
            saved = Map.Copy();
            Refresh();
        }
        catch (Exception exception) { Debug.LogException(exception); status.gameObject.SetActive(true); status.Value = Text("error", exception.Message); }
    }

    private void RequestClose()
    {
        if (gesture != null) { EndGesture(true); return; }
        PausePlayback();
        if (Map.Regions.SequenceEqual(saved.Regions)) { Close(); return; }
        confirmationOpen = true;
        var confirm = PersistentUI.Instance.CreateNewDialogBox().WithTitle("SongEditMenu", "tempo.unsaved");
        confirm.AddComponent<TextComponent>().WithInitialValue("SongEditMenu", "tempo.discard.prompt");
        confirm.AddFooterButton(() => { confirmationOpen = false; Close(); }, "SongEditMenu", "tempo.discard");
        confirm.AddFooterButton(() => confirmationOpen = false, "SongEditMenu", "tempo.keep.editing");
        confirm.OnCloseRequested(() => { confirmationOpen = false; confirm.Close(); });
        confirm.Open(dialog);
    }

    private void Close()
    {
        RestoreSpectrogram();
        dialog.Close();
    }

    private void RestoreSpectrogram()
    {
        if (restored || spectrogram == null || originalParent == null) return;
        restored = true;
        music.Stop();
        foreach (var item in originalRadii) if (item.Key != null) { item.Key.radius = item.Value; item.Key.Refresh(); }
        foreach (var item in originalIndependentRadii) if (item.Key != null) { item.Key.r = item.Value; item.Key.Refresh(); }
        foreach (var item in originalMaskImages) if (item.Key != null) { item.Key.sprite = item.Value.sprite; item.Key.type = item.Value.type; }
        if (mainSpectrogram != null) mainSpectrogram.material = originalSpectrogramMaterial;
        if (scrollingSpectrogramMaterial != null) Destroy(scrollingSpectrogramMaterial);
        if (grid != null) Destroy(grid.gameObject);
        if (ownsSpectrogramLayout) Destroy(spectrogramLayout);
        else spectrogramLayout.ignoreLayout = false;
        if (ownsSpectrumVisibility) Destroy(spectrumVisibility);
        else if (spectrumVisibility != null)
        {
            spectrumVisibility.alpha = spectrumAlpha;
            spectrumVisibility.blocksRaycasts = spectrumRaycasts;
        }
        spectrogram.SetParent(originalParent, false);
        spectrogram.SetSiblingIndex(originalSibling);
        spectrogram.anchorMin = originalMin;
        spectrogram.anchorMax = originalMax;
        spectrogram.anchoredPosition = originalPosition;
        spectrogram.sizeDelta = originalSize;
        spectrogram.pivot = originalPivot;
        spectrogram.localScale = originalScale;
        preview.PreviewSelection.SetActive(selectionVisible);
        preview.RestorePreviewBounds();
    }

    private void OnDestroy()
    {
        precisionModifier?.Dispose();
        preciseModifier?.Dispose();
        precisionScroll?.Dispose();
        RestoreSpectrogram();
        if (overviewMaterial != null) Destroy(overviewMaterial);
    }
}

