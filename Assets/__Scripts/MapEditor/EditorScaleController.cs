using UnityEngine;

public class EditorScaleController : MonoBehaviour {

    public static int EditorScale = 4;
    private static int EditorStep = 2;

    private int PreviousEditorScale = -1;

    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Transform[] scalingOffsets;
    private BeatmapObjectContainerCollection[] collections;
    [SerializeField] private AudioTimeSyncController atsc;

    public void UpdateEditorScale(float value)
    {
        EditorStep = Mathf.RoundToInt(value);
        EditorScale = Mathf.RoundToInt(Mathf.Pow(2, EditorStep));
        if (PreviousEditorScale != EditorScale) Apply();
    }

    private void Apply()
    {
        foreach (BeatmapObjectContainerCollection collection in collections)
            foreach (BeatmapObjectContainer b in collection.LoadedContainers) b.UpdateGridPosition();
        atsc.MoveToTimeInSeconds(atsc.CurrentSeconds);
        PreviousEditorScale = EditorScale;
        foreach (Transform offset in scalingOffsets)
            offset.localScale = new Vector3(offset.localScale.x, offset.localScale.y, 8 * EditorScale);
    }

	// Use this for initialization
	void Start () {
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
        PreviousEditorScale = EditorScale;
        UpdateEditorScale(Settings.Instance.EditorScale);
        Apply();
	}
}
