using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RectTransform parent;
    [SerializeField] private Transform noteGrid;
    [SerializeField] private Transform frontNoteGridScaling;
    [SerializeField] private Transform measureLineGrid;
    [FormerlySerializedAs("bpmChangesContainer")] [SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [SerializeField] private GridChild measureLinesGridChild;
    [SerializeField] private BookmarkRenderingController bookmarkRenderingController;
    private readonly List<(float, TextMeshProUGUI)> measureTextsByBeat = new List<(float, TextMeshProUGUI)>();
    private readonly Dictionary<float, bool> previousEnabledByBeat = new Dictionary<float, bool>();

    private bool init;

    private float previousAtscBeat = -1;

    private void Start()
    {
        if (!measureTextsByBeat.Any())
        {
            measureTextsByBeat.Add((0, measureLinePrefab));
            previousEnabledByBeat.Add(0, true);
        }

        EditorScaleController.EditorScaleChangedEvent += EditorScaleUpdated;
    }

    private void LateUpdate()
    {
        if (atsc.CurrentBeat == previousAtscBeat || !init) return;
        previousAtscBeat = atsc.CurrentBeat;
        RefreshVisibility();
    }

    private void OnDestroy() => EditorScaleController.EditorScaleChangedEvent -= EditorScaleUpdated;

    private void EditorScaleUpdated(float obj) => RefreshPositions();

    public void RefreshMeasureLines()
    {
        Debug.Log("Refreshing measure lines...");
        init = false;
        var existing = new Queue<TextMeshProUGUI>(measureTextsByBeat.Select(x => x.Item2));
        measureTextsByBeat.Clear();
        previousEnabledByBeat.Clear();

        var rawBeatsInSong =
            Mathf.FloorToInt(atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.LoadedSong.length));
        float jsonBeat = 0;
        var modifiedBeats = 0;
        var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

        var allBpmChanges = new List<BaseBpmEvent> { BeatSaberSongContainer.Instance.Map.GetVersion() == 3 ? (BaseBpmEvent)new V3BpmChange(songBpm, 0) : new V2BpmChange(songBpm, 0) };
        allBpmChanges.AddRange(bpmChangeGridContainer.LoadedObjects.Cast<BaseBpmEvent>());

        while (jsonBeat <= rawBeatsInSong)
        {
            var text = existing.Count > 0 ? existing.Dequeue() : Instantiate(measureLinePrefab, parent);
            text.text = $"{modifiedBeats}";
            text.transform.localPosition = new Vector3(0, jsonBeat * EditorScaleController.EditorScale, 0);
            measureTextsByBeat.Add((jsonBeat, text));
            previousEnabledByBeat.Add(jsonBeat, true);

            modifiedBeats++;
            var last = allBpmChanges.Last(x => x.Beat <= modifiedBeats);
            jsonBeat = ((modifiedBeats - last.Beat) / last.Bpm * songBpm) + last.Time;
        }

        // Set proper spacing between Notes grid, Measure lines, and Events grid
        measureLinesGridChild.Size = modifiedBeats > 1000 ? 1 : 0;
        foreach (var leftovers in existing) Destroy(leftovers.gameObject);
        init = true;
        RefreshVisibility();
        RefreshPositions();
    }

    private void RefreshVisibility()
    {
        var currentBeat = atsc.CurrentBeat;
        var beatsAhead = frontNoteGridScaling.localScale.z / EditorScaleController.EditorScale;
        var beatsBehind = beatsAhead / 4f;

        foreach (var kvp in measureTextsByBeat)
        {
            var time = kvp.Item1;
            var text = kvp.Item2;
            var enabled = time >= currentBeat - beatsBehind && time <= currentBeat + beatsAhead;

            if (previousEnabledByBeat[time] != enabled)
            {
                text.gameObject.SetActive(enabled);
                previousEnabledByBeat[time] = enabled;
            }
        }

        bookmarkRenderingController.RefreshVisibility(currentBeat, beatsAhead, beatsBehind);
    }

    private void RefreshPositions()
    {
        foreach (var kvp in measureTextsByBeat)
            kvp.Item2.transform.localPosition = new Vector3(0, kvp.Item1 * EditorScaleController.EditorScale, 0);
    }
}
