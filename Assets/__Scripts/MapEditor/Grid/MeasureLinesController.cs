using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RectTransform parent;
    [SerializeField] private Transform noteGridScalingOffset;

    private float previousATSCBeat = -1;
    private int previousEditorScale = EditorScaleController.EditorScale;
    private float previousNodeGridX = -1;
    private Dictionary<int, TextMeshProUGUI> measureTextsByBeat = new Dictionary<int, TextMeshProUGUI>();
    private Dictionary<int, bool> previousEnabledByBeat = new Dictionary<int, bool>();

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); //wait for ATSC to get song info, so we can get beats in the song VVV
        measureTextsByBeat.Add(0, measureLinePrefab);
        previousEnabledByBeat.Add(0, true);
        for (int i = 1; i <= Mathf.FloorToInt(atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length)); i++)
        {
            TextMeshProUGUI instantiate = Instantiate(measureLinePrefab, parent);
            instantiate.text = $"{i}";
            instantiate.transform.localPosition = new Vector3(0, i * EditorScaleController.EditorScale, 0);
            measureTextsByBeat.Add(i, instantiate);
            previousEnabledByBeat.Add(i, true);
        }
    }

    void Update()
    {
        if (atsc.CurrentBeat == previousATSCBeat) return;
        previousATSCBeat = atsc.CurrentBeat;
        float offsetBeat = atsc.CurrentBeat - atsc.offsetBeat;
        float beatsAhead = (noteGridScalingOffset.transform.localScale.z / 4);
        if (noteGridScalingOffset.localScale.x != previousNodeGridX)
        {
            parent.transform.localPosition = new Vector3(7.5f * noteGridScalingOffset.localScale.x, atsc.gridStartPosition, 0);
            previousNodeGridX = noteGridScalingOffset.localScale.x;
        }
        foreach(KeyValuePair<int, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            bool enabled = kvp.Key >= offsetBeat && kvp.Key <= offsetBeat + beatsAhead;
            if (EditorScaleController.EditorScale != previousEditorScale)
            {
                kvp.Value.transform.localPosition = new Vector3(0, kvp.Key * EditorScaleController.EditorScale, 0);
                previousEditorScale = EditorScaleController.EditorScale;
            }
            if (previousEnabledByBeat[kvp.Key] != enabled)
                kvp.Value.gameObject.SetActive(enabled);
            previousEnabledByBeat[kvp.Key] = enabled;
        }
    }
}
