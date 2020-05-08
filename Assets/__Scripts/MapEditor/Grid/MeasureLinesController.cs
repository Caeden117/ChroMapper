using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    private float previousATSCBeat = -1;
    private Dictionary<float, TextMeshProUGUI> measureTextsByBeat = new Dictionary<float, TextMeshProUGUI>();
    private Dictionary<float, bool> previousEnabledByBeat = new Dictionary<float, bool>();

    private bool init = false;

    public void RefreshMeasureLines()
    {
        init = false;
        previousATSCBeat = -1;
        Queue<TextMeshProUGUI> existing = new Queue<TextMeshProUGUI>(measureTextsByBeat.Values);
        measureTextsByBeat.Clear();
        previousEnabledByBeat.Clear();
        int rawBeatsInSong = Mathf.FloorToInt(atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length));
        float beatsProcessed = 1;
        float rawBPMtoChangedBPMRatio = 1;
        int modifiedBeats = 0;
        BeatmapBPMChange lastBPMChange = null;
        while (beatsProcessed <= rawBeatsInSong)
        {
            //yield return new WaitForEndOfFrame();
            TextMeshProUGUI text = existing.Count > 0 ? existing.Dequeue() : Instantiate(measureLinePrefab, parent);
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
        foreach (TextMeshProUGUI leftovers in existing)
        {
            Destroy(leftovers.gameObject);
        }
        init = true;
    }

    //TODO switch to pool system and also not do this every update you knob
    void Update()
    {
        if (atsc.CurrentBeat == previousATSCBeat || !init) return;
        previousATSCBeat = atsc.CurrentBeat;
        float offsetBeat = atsc.CurrentBeat - atsc.offsetBeat;
        float beatsAhead = frontNoteGridScaling.localScale.z / EditorScaleController.EditorScale;
        float beatsBehind = beatsAhead / 4f;
        foreach (KeyValuePair<float, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            bool enabled = kvp.Key >= offsetBeat - beatsBehind && kvp.Key <= offsetBeat + beatsAhead;
            kvp.Value.transform.localPosition = new Vector3(0, kvp.Key * EditorScaleController.EditorScale, 0);
            if (previousEnabledByBeat[kvp.Key] != enabled)
            {
                kvp.Value.gameObject.SetActive(enabled);
                previousEnabledByBeat[kvp.Key] = enabled;
            }
        }
    }
}
