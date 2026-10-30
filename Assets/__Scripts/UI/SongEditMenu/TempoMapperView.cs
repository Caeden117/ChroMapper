using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Info;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Audio-time tempo editing over the serialized Tempo Mapper workspace prefab.</summary>
public partial class TempoMapperView : MonoBehaviour, CMInput.ITimelineActions, CMInput.IActionsActions,
    CMInput.IPlaybackActions, CMInput.ISavingActions, CMInput.IMenusExtendedActions
{
    public TempoMap Map { get; private set; }
    public int Sample { get; private set; }
    public int SelectedRegion { get; private set; }
    public double ViewStart { get; private set; }
    public double ViewEnd { get; private set; }
    public int Subdivision { get; private set; }
    public bool ShiftTimeline => shift.isOn;
    public float PlaybackMarkerPulse => Settings.Instance.TempoPulsePlayhead && music != null && music.isPlaying && Map != null
        ? (float)Math.Exp(-(Map.BeatAt(Sample) - Math.Floor(Map.BeatAt(Sample))) * 4)
        : 0;
    public bool IsFollowingPlayback => music != null && music.isPlaying && Settings.Instance.TempoFollowPlayback;
    public bool PrecisionScrollActive => CMInputCallbackInstaller.InputInstance?.Timeline.ChangePrecisionModifier.IsPressed() == true;

    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource metronome;
    [SerializeField] private AudioClip tick;
    [SerializeField] private TempoGridOverlay grid;
    [SerializeField] private TempoGridOverlay overview;
    [SerializeField] private TempoGridOverlay waveformGrid;
    [SerializeField] private TempoWaveformGraphic waveform;
    [SerializeField] private TempoWaveformGraphic overviewWaveform;
    [SerializeField] private ComputeShader waveformShader;
    [SerializeField] private RawImage mainSpectrogram;
    [SerializeField] private RawImage overviewSpectrogram;
    [SerializeField] private LayoutElement spectrogramLayout;
    [SerializeField] private TempoBeatRuler beatRuler;
    [SerializeField] private TextComponent position;
    [SerializeField] private TextComponent status;
    [SerializeField] private TextBoxFloatComponent bpm;
    [SerializeField] private TextBoxFloatComponent beats;
    [SerializeField] private TextBoxIntComponent subdivisionField;
    [SerializeField] private Toggle shift;
    [SerializeField] private ButtonComponent play;
    [SerializeField] private ButtonComponent undoButton;
    [SerializeField] private ButtonComponent redoButton;
    [SerializeField] private ButtonComponent saveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Image playIcon;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private TempoMapperSlider zoom;
    [SerializeField] private TempoMapperSlider speed;
    [SerializeField] private TempoMapperSlider metronomeVolume;
    [SerializeField] private TempoMapperSlider volume;
    [SerializeField] private int tempoOptionsTab;

    private AudioPreviewGenerator preview;
    private DifficultySelect difficulties;
    private Material overviewMaterial;
    private Material scrollingSpectrogramMaterial;
    private TempoWaveformData waveformData;
    private IEnumerator waveformRoutine;
    private Coroutine waveformGeneration;
    private bool initialized;
    private bool confirmationOpen;
    private bool playbackClockRunning;
    private double playbackClockTime;
    private double playbackClockSample;
    private float playbackClockPitch = 1;
    private int audioDriftTolerance;
    private double lastReadoutTime = double.NegativeInfinity;
    private double previousZoomFactor = double.NaN;
    private int previousBeat = -1;
    private readonly Stack<TempoMap> undo = new();
    private readonly Stack<TempoMap> redo = new();
    private TempoMap gesture;
    private TempoMap saved;

    private static readonly string[] displaySettings =
    {
        nameof(Settings.TempoOverviewDisplay), nameof(Settings.TempoEditorDisplay),
        nameof(Settings.TempoWaveformRectifyPeaks), nameof(Settings.TempoWaveformMaxColumns),
        nameof(Settings.SpectrogramBilinearFiltering),
        nameof(Settings.SpectrogramShift)
    };

    private static readonly Type[] blockedInputs = typeof(CMInput).GetNestedTypes()
        .Where(type => type.IsInterface && type != typeof(CMInput.IUtilsActions)).ToArray();

    private static string Text(string key, params object[] args) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", "tempo." + key, args);

    public static TempoMapperView Open(AudioClip clip, AudioPreviewGenerator preview,
        DifficultySelect difficulties, TempoMapperView prefab)
    {
        TempoMapperView view = null;
        try
        {
            var map = TempoMapFileStore.Load(BeatSaberSongContainer.Instance.Info,
                difficulties.CurrentDiff, clip.samples - 1, clip.frequency);
            // A root canvas keeps this workspace independent of the persistent UI width-based scale.
            view = Instantiate(prefab);
            view.Initialize(clip, preview, difficulties, map);
            return view;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (view != null) Destroy(view.gameObject);
            PersistentUI.Instance.ShowDialogBox(Text("error", exception.Message), null, PersistentUI.DialogBoxPresetType.Ok);
            return null;
        }
    }

    private void Initialize(AudioClip clip, AudioPreviewGenerator audioPreview, DifficultySelect selector, TempoMap map)
    {
        preview = audioPreview;
        difficulties = selector;
        Map = map;
        saved = map.Copy();
        music.clip = clip;
        music.volume = Settings.Instance.SongVolume;
        metronome.volume = Settings.Instance.MetronomeVolume;
        ViewEnd = Math.Min(Map.EndSample, Map.Frequency * 12d);
        Subdivision = Math.Clamp(Settings.Instance.CursorPrecisionA, 1, 64);
        subdivisionField.SetValueWithoutNotify(Subdivision);

        // RawImage must not query _MainTex on the shared buffer-driven shader.
        mainSpectrogram.texture = overviewSpectrogram.texture = Texture2D.whiteTexture;
        scrollingSpectrogramMaterial = new Material(mainSpectrogram.material);
        mainSpectrogram.material = scrollingSpectrogramMaterial;
        overviewMaterial = new Material(overviewSpectrogram.material);
        overviewMaterial.SetFloat("_ViewStart", 0);
        overviewMaterial.SetFloat("_ViewEnd", clip.length);
        overviewSpectrogram.material = overviewMaterial;

        bpm.OnEndEdit(value => Edit(map => map.SetBpm(SelectedRegion, value)));
        beats.OnEndEdit(value => Edit(map => map.SetBeats(SelectedRegion, value)));
        subdivisionField.OnEndEdit(SetSubdivision);
        undoButton.OnClick(Undo);
        redoButton.OnClick(Redo);
        play.OnClick(TogglePlaying);
        saveButton.OnClick(Save);
        settingsButton.onClick.AddListener(OpenSettings);
        backButton.onClick.AddListener(RequestClose);

        zoom.Initialize((float)(Math.Log(Math.Max(1, Map.EndSample / (Map.Frequency * 12d)), 2) / 8),
            SetZoom, value => $"{Math.Pow(2, value * 8):F1}x");
        zoom.Slider.maxValue = Math.Max(1, (float)(Math.Log(Math.Max(1, Map.EndSample / (Map.Frequency * 0.1)), 2) / 8));
        speed.Initialize(1, SetPlaybackSpeed, value => $"{value:F2}x");
        metronomeVolume.Initialize(metronome.volume, value => metronome.volume = value, value => $"{value:P0}");
        volume.Initialize(music.volume, value => music.volume = value, value => $"{value:P0}");

        foreach (var setting in displaySettings) Settings.NotifyBySettingName(setting, DisplaySettingChanged);
        Settings.NotifyBySettingName(nameof(Settings.TempoWaveformSamplesPerPeak), WaveformResolutionChanged);
        initialized = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        InstallInput();
        ApplyDisplaySettings();
        RebuildWaveform();
    }

    private void DisplaySettingChanged(object value) => ApplyDisplaySettings();
    private void WaveformResolutionChanged(object value) => RebuildWaveform();

    private void RebuildWaveform()
    {
        StopWaveformGeneration();
        waveformRoutine = TempoWaveformData.Generate(music.clip, Settings.Instance.TempoWaveformSamplesPerPeak, waveformShader, data =>
        {
            var previous = waveformData;
            waveformData = data;
            waveform.Data = overviewWaveform.Data = data;
            previous?.Dispose();
        });
        waveformGeneration = StartCoroutine(waveformRoutine);
    }

    private void StopWaveformGeneration()
    {
        if (waveformGeneration != null) StopCoroutine(waveformGeneration);
        // Explicit disposal also releases an unfinished upload when Unity stops the coroutine.
        (waveformRoutine as IDisposable)?.Dispose();
        waveformRoutine = null;
        waveformGeneration = null;
    }

    private void ApplyDisplaySettings()
    {
        var settings = Settings.Instance;
        var waveformVisible = settings.TempoEditorDisplay != 1;
        var spectrumVisible = settings.TempoEditorDisplay != 0;
        waveform.transform.parent.gameObject.SetActive(waveformVisible);
        mainSpectrogram.transform.parent.gameObject.SetActive(spectrumVisible);
        var waveSize = waveform.transform.parent.GetComponent<LayoutElement>();
        waveSize.minHeight = spectrogramLayout.minHeight = 40;
        waveSize.preferredHeight = spectrogramLayout.preferredHeight = 112;
        waveSize.flexibleHeight = spectrogramLayout.flexibleHeight = 1;
        overviewWaveform.enabled = settings.TempoOverviewDisplay == 0;
        overviewSpectrogram.enabled = settings.TempoOverviewDisplay == 1;
        waveform.MaximumColumns = overviewWaveform.MaximumColumns = settings.TempoWaveformMaxColumns;
        waveform.RectifyPeaks = overviewWaveform.RectifyPeaks = settings.TempoWaveformRectifyPeaks;
        Shader.SetGlobalFloat("_Spectrogram_BilinearFiltering", settings.SpectrogramBilinearFiltering ? 1 : 0);
        Shader.SetGlobalFloat("_Spectrogram_Shift", settings.SpectrogramShift);
        if (spectrumVisible || settings.TempoOverviewDisplay == 1) preview.PrepareTempoMapper();
        Refresh();
    }


    private void OpenSettings()
    {
        if (OptionsController.IsActive) return;
        PausePlayback();
        OptionsController.ShowOptions(tempoOptionsTab);
    }

    private void SetSubdivision(int value)
    {
        Subdivision = Math.Clamp(value, 1, 64);
        Settings.Instance.CursorPrecisionA = Subdivision;
        Settings.ManuallyNotifySettingUpdatedEvent(AudioTimeSyncController.PrecisionSnapName, Subdivision);
        subdivisionField.SetValueWithoutNotify(Subdivision);
        Refresh();
    }

    internal static int StepSubdivision(int current, float scroll, bool precise)
    {
        if (scroll == 0) return current;
        var increase = (scroll > 0) != Settings.Instance.InvertPrecisionScroll;
        return Mathf.Clamp(precise ? current + (increase ? 1 : -1)
            : Mathf.RoundToInt(current * (increase ? 2f : 0.5f)), 1, 64);
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
        heldPlayback = false;
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
        if (playIcon != null)
            playIcon.sprite = music.isPlaying ? pauseSprite : playSprite;
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
                var editingGrid = waveformGrid.isActiveAndEnabled ? waveformGrid : grid;
                var pixels = Math.Max(1, editingGrid.rectTransform.rect.width * editingGrid.canvas.scaleFactor);
                var samplesPerPixel = span / pixels;
                // Translate every audio layer by the same whole-pixel step; fractional raster phase stays fixed.
                ViewStart = Math.Round((predicted - span / 2) / samplesPerPixel) * samplesPerPixel;
                ViewEnd = ViewStart + span;
            }
            else if (Sample < ViewStart || Sample >= ViewEnd)
            {
                // Page forward only on leaving the current window; do not continuously center playback.
                var span = ViewEnd - ViewStart;
                ViewStart = Math.Clamp(Sample, 0, Math.Max(0, Map.EndSample - span));
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
        if (zoom != null)
        {
            var factor = Map.EndSample / Math.Max(1, ViewEnd - ViewStart);
            if (factor != previousZoomFactor)
            {
                zoom.SetValueWithoutNotify((float)(Math.Log(factor, 2) / 8));

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
            if (playIcon != null)
                playIcon.sprite = music.isPlaying ? pauseSprite : playSprite;
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
        if (confirmationOpen || OptionsController.IsActive) return;
        if (gesture != null)
        {
            EndGesture(true);
            return;
        }
        PausePlayback();
        if (Map.Regions.SequenceEqual(saved.Regions))
        {
            Close();
            return;
        }

        confirmationOpen = true;
        var confirm = PersistentUI.Instance.CreateNewDialogBox().WithTitle("SongEditMenu", "tempo.unsaved");
        confirm.AddComponent<TextComponent>().WithInitialValue("SongEditMenu", "tempo.discard.prompt");
        confirm.AddFooterButton(Close, "SongEditMenu", "tempo.discard");
        confirm.AddFooterButton(() => { }, "SongEditMenu", "tempo.keep.editing");
        confirm.Open();
        StartCoroutine(WaitForConfirmation(confirm));
    }

    private IEnumerator WaitForConfirmation(DialogBox confirm)
    {
        yield return new WaitUntil(() => confirm == null || !confirm.gameObject.activeSelf);
        confirmationOpen = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Close()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StopWaveformGeneration();
        waveformData?.Dispose();
        if (overviewMaterial != null) Destroy(overviewMaterial);
        if (scrollingSpectrogramMaterial != null) Destroy(scrollingSpectrogramMaterial);
        if (!initialized) return;
        UninstallInput();
        foreach (var setting in displaySettings) Settings.StopNotifyingBySettingName(setting, DisplaySettingChanged);
        Settings.StopNotifyingBySettingName(nameof(Settings.TempoWaveformSamplesPerPeak), WaveformResolutionChanged);
        if (preview != null) preview.RestorePreviewBounds();
    }
}
