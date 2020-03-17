using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class BeatmapBPMChangeContainer : BeatmapObjectContainer {

    public override BeatmapObject objectData { get => bpmData; set => bpmData = (BeatmapBPMChange)value; }

    public BeatmapBPMChange bpmData;

    public static BeatmapBPMChangeContainer SpawnBPMChange(BeatmapBPMChange data, ref GameObject prefab)
    {
        BeatmapBPMChangeContainer container = Instantiate(prefab).GetComponent<BeatmapBPMChangeContainer>();
        container.bpmData = data;
        container.GetComponentInChildren<TextMeshProUGUI>().text = data._BPM.ToString(CultureInfo.InvariantCulture);
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(16.5f, 0.5f, bpmData._time * EditorScaleController.EditorScale);
        chunkID = (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
            MidpointRounding.AwayFromZero);
    }
}
