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

    private Dictionary<int, TextMeshProUGUI> measureTextsByBeat = new Dictionary<int, TextMeshProUGUI>();

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); //wait for ATSC to get song info, so we can get beats in the song VVV
        measureTextsByBeat.Add(0, measureLinePrefab);
        for (int i = 1; i <= Mathf.FloorToInt(atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length)); i++)
        {
            TextMeshProUGUI instantiate = Instantiate(measureLinePrefab, parent);
            instantiate.text = $"{i}";
            measureTextsByBeat.Add(i, instantiate);
        }
    }

    void Update()
    {
        float beatsAhead = (noteGridScalingOffset.transform.localScale.z / 4) + 1;
        parent.transform.localPosition = new Vector3(7.5f * noteGridScalingOffset.localScale.x, atsc.gridStartPosition, 0);
        foreach(KeyValuePair<int, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            kvp.Value.transform.localPosition = new Vector3(0, kvp.Key * EditorScaleController.EditorScale, 0);
            kvp.Value.gameObject.SetActive(kvp.Key >= atsc.CurrentBeat && kvp.Key <= atsc.CurrentBeat + beatsAhead);
        }
    }
}
