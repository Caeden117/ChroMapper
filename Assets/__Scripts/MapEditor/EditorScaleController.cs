using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorScaleController : MonoBehaviour {

    public static int EditorScale = 4;
    private static int EditorStep = 2;

    private int PreviousEditorScale;

    [SerializeField] private CanvasGroup PauseMenuCanvasGroup;
    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
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
        foreach (BeatmapNoteContainer n in notes.loadedNotes) n.UpdateGridPosition();
        foreach (BeatmapObstacleContainer o in obstacles.loadedObstacles) o.UpdateGridPosition();
        foreach (BeatmapEventContainer e in events.loadedEvents) e.UpdateGridPosition();
        foreach (BeatmapBPMChangeContainer b in bpm.loadedBPMChanges) b.UpdateGridPosition();
        atsc.MoveToTimeInSeconds(atsc.CurrentSeconds);
        PreviousEditorScale = EditorScale;
    }

	// Use this for initialization
	void Start () {
        PreviousEditorScale = EditorScale;
        UpdateEditorScale(2);
        Apply();
	}
}
