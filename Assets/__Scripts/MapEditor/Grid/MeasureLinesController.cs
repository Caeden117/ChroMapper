using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RectTransform parent;
    [SerializeField] private Transform noteGrid;
    [SerializeField] private Transform frontNoteGridScaling;
    [SerializeField] private Transform measureLineGrid;
    [SerializeField] private BPMChangesContainer bpmChangesContainer;
    [SerializeField] private GridChild measureLinesGridChild;

    private float previousATSCBeat = -1;
    private List<(float, TextMeshProUGUI)> measureTextsByBeat = new List<(float, TextMeshProUGUI)>();
    private Dictionary<float, bool> previousEnabledByBeat = new Dictionary<float, bool>();

    private bool init = false;

    private void Start()
    {
        if (!measureTextsByBeat.Any())
        {
            measureTextsByBeat.Add((0, measureLinePrefab));
            previousEnabledByBeat.Add(0, true);
        }
        EditorScaleController.EditorScaleChangedEvent += EditorScaleUpdated;
    }

    private void OnDestroy()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleUpdated;
    }

    private void EditorScaleUpdated(float obj)
    {
        RefreshPositions();
    }

    public void RefreshMeasureLines()
    {
        Debug.Log("Refreshing measure lines...");
        init = false;
        Queue<TextMeshProUGUI> existing = new Queue<TextMeshProUGUI>(measureTextsByBeat.Select(x => x.Item2));
        measureTextsByBeat.Clear();
        previousEnabledByBeat.Clear();

        int rawBeatsInSong = Mathf.FloorToInt(atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length));
        float jsonBeat = 0;
        int modifiedBeats = 0;
        float songBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;

        List<BeatmapBPMChange> allBPMChanges = new List<BeatmapBPMChange>()
        {
            new BeatmapBPMChange(songBPM, 0)
        };
        allBPMChanges.AddRange(bpmChangesContainer.LoadedObjects.Cast<BeatmapBPMChange>());

        while (jsonBeat <= rawBeatsInSong)
        {
            TextMeshProUGUI text = existing.Count > 0 ? existing.Dequeue() : Instantiate(measureLinePrefab, parent);
            text.text = $"{modifiedBeats}";
            text.transform.localPosition = new Vector3(0, jsonBeat * EditorScaleController.EditorScale, 0);
            measureTextsByBeat.Add((jsonBeat, text));
            previousEnabledByBeat.Add(jsonBeat, true);

            modifiedBeats++;
            BeatmapBPMChange last = allBPMChanges.Last(x => x._Beat <= modifiedBeats);
            jsonBeat = ((modifiedBeats - last._Beat) / last._BPM * songBPM) + last._time;
        }

        // Set proper spacing between Notes grid, Measure lines, and Events grid
        measureLinesGridChild.Size = modifiedBeats > 1000 ? 1 : 0;
        foreach (TextMeshProUGUI leftovers in existing)
        {
            Destroy(leftovers.gameObject);
        }
        init = true;
        RefreshVisibility();
        RefreshPositions();
    }

    void LateUpdate()
    {
        if (atsc.CurrentBeat == previousATSCBeat || !init) return;
        previousATSCBeat = atsc.CurrentBeat;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        float offsetBeat = atsc.CurrentBeat - atsc.offsetBeat;
        float beatsAhead = frontNoteGridScaling.localScale.z / EditorScaleController.EditorScale;
        float beatsBehind = beatsAhead / 4f;

        foreach (var kvp in measureTextsByBeat)
        {
            var time = kvp.Item1;
            var text = kvp.Item2;
            var enabled = time >= offsetBeat - beatsBehind && time <= offsetBeat + beatsAhead;
            
            if (previousEnabledByBeat[time] != enabled)
            {
                text.gameObject.SetActive(enabled);
                previousEnabledByBeat[time] = enabled;
            }
        }
    }

    private void RefreshPositions()
    {
        foreach (var kvp in measureTextsByBeat)
        {
            kvp.Item2.transform.localPosition = new Vector3(0, kvp.Item1 * EditorScaleController.EditorScale, 0);
        }
    }
}
