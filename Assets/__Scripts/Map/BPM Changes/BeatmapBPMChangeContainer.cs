using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BeatmapBPMChangeContainer : BeatmapObjectContainer {
    public override BeatmapObject objectData
    {
        get
        {
            return bpmData;
        }
    }

    public BeatmapBPMChange bpmData;

    public static BeatmapBPMChangeContainer SpawnBPMChange(BeatmapBPMChange data, ref GameObject prefab)
    {
        BeatmapBPMChangeContainer container = Instantiate(prefab).GetComponent<BeatmapBPMChangeContainer>();
        container.bpmData = data;
        container.GetComponentInChildren<TextMeshProUGUI>().text = data._BPM.ToString();
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(15.5f, 0.5f, bpmData._time * EditorScaleController.EditorScale);
    }
}
