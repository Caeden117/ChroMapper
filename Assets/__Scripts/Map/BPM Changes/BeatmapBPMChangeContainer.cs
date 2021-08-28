using System.Globalization;
using TMPro;
using UnityEngine;

public class BeatmapBPMChangeContainer : BeatmapObjectContainer
{
    [SerializeField] private TextMeshProUGUI bpmText;

    public BeatmapBPMChange BpmData;

    public override BeatmapObject ObjectData { get => BpmData; set => BpmData = (BeatmapBPMChange)value; }

    public static BeatmapBPMChangeContainer SpawnBpmChange(BeatmapBPMChange data, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapBPMChangeContainer>();
        container.BpmData = data;
        return container;
    }

    public void UpdateBpmText() => bpmText.text = BpmData.Bpm.ToString(CultureInfo.InvariantCulture);

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(0.5f, 0.5f, BpmData.Time * EditorScaleController.EditorScale);
        bpmText.text = BpmData.Bpm.ToString(CultureInfo.InvariantCulture);
        UpdateCollisionGroups();
    }
}
