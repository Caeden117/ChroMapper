using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorScaleController : MonoBehaviour {

    public static int EditorScale = 4;
    private static int EditorStep = 2;

    private int PreviousEditorScale;

    [SerializeField] private CanvasGroup PauseMenuCanvasGroup;
    [SerializeField] private Transform moveableGridTransform;
    private BeatmapObjectContainerCollection[] collections;
    [SerializeField] private AudioTimeSyncController atsc;

    public void UpdateEditorScale(float value)
    {
        EditorStep = Mathf.RoundToInt(value);
        EditorScale = Mathf.RoundToInt(Mathf.Pow(2, EditorStep));
    }

    public void ApplyEditorScaleChanges()
    {
        if (PauseMenuCanvasGroup.alpha > 0.5f && PreviousEditorScale != EditorScale) Apply();
    }

    private void Apply()
    {
        foreach (BeatmapObjectContainerCollection collection in collections)
            foreach (BeatmapObjectContainer b in collection.LoadedContainers) b.UpdateGridPosition();
        atsc.MoveToTimeInSeconds(atsc.CurrentSeconds);
        PreviousEditorScale = EditorScale;
    }

	// Use this for initialization
	void Start () {
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
        PreviousEditorScale = EditorScale;
        UpdateEditorScale(2);
        Apply();
	}
}
