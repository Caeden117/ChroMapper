using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RectTransform parent;
    [SerializeField] private Transform noteGrid;
    [SerializeField] private Transform frontNoteGridScaling;
    [SerializeField] private Transform measureLineGrid;
    [SerializeField] private UIWorkflowToggle workflowToggle;
    [SerializeField] private BPMChangesContainer bpmChangesContainer;
    [SerializeField] private GridChild measureLinesGridChild;

    private float previousATSCBeat = -1;
    private Dictionary<float, TextMeshProUGUI> measureTextsByBeat = new Dictionary<float, TextMeshProUGUI>();
    private Dictionary<float, bool> previousEnabledByBeat = new Dictionary<float, bool>();

    private bool init = false;

    private void Start()
    {
        EditorScaleController.EditorScaleChangedEvent += EditorScaleUpdated;
        LoadInitialMap.LevelLoadedEvent += LevelLoaded;
    }

    private void OnDestroy()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleUpdated;
        LoadInitialMap.LevelLoadedEvent -= LevelLoaded;
    }

    private void EditorScaleUpdated(float obj)
    {
        RefreshPositions();
    }

    private void LevelLoaded()
    {
        RefreshMeasureLines();
    }

    public void RefreshMeasureLines()
    {
        init = false;
        Queue<TextMeshProUGUI> existing = new Queue<TextMeshProUGUI>(measureTextsByBeat.Values);
        measureTextsByBeat.Clear();
        previousEnabledByBeat.Clear();
        int rawBeatsInSong = Mathf.FloorToInt(atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length));
        float beatsProcessed = 1;
        float rawBPMtoChangedBPMRatio = 1;
        int modifiedBeats = 1;
        BeatmapBPMChange lastBPMChange = null;
        while (beatsProcessed <= rawBeatsInSong)
        {
            TextMeshProUGUI text = existing.Count > 0 ? existing.Dequeue() : Instantiate(measureLinePrefab, parent);
            text.gameObject.SetActive(true);
            text.text = $"{modifiedBeats}";
            text.transform.localPosition = new Vector3(0, beatsProcessed * EditorScaleController.EditorScale, 0);
            measureTextsByBeat.Add(beatsProcessed, text);
            previousEnabledByBeat.Add(beatsProcessed, true);

            modifiedBeats++;
            BeatmapBPMChange last = bpmChangesContainer.FindLastBPM(beatsProcessed + rawBPMtoChangedBPMRatio, true);
            if (last != lastBPMChange && last?._BPM > 0)
            {
                lastBPMChange = last;
                beatsProcessed = last._time;
                rawBPMtoChangedBPMRatio = BeatSaberSongContainer.Instance.song.beatsPerMinute / last._BPM;
            }
            else
            {
                beatsProcessed += rawBPMtoChangedBPMRatio;
            }
        }
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
        foreach (KeyValuePair<float, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            bool enabled = kvp.Key >= offsetBeat - beatsBehind && kvp.Key <= offsetBeat + beatsAhead;
            if (previousEnabledByBeat[kvp.Key] != enabled)
            {
                kvp.Value.gameObject.SetActive(enabled);
                previousEnabledByBeat[kvp.Key] = enabled;
            }
        }
    }

    private void RefreshPositions()
    {
        foreach (KeyValuePair<float, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            kvp.Value.transform.localPosition = new Vector3(0, kvp.Key * EditorScaleController.EditorScale, 0);
        }
    }
}
