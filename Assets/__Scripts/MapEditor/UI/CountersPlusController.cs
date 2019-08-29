using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class CountersPlusController : MonoBehaviour {

    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
    [SerializeField] private AudioSource cameraAudioSource;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TextMeshProUGUI notesMesh;
    [SerializeField] private TextMeshProUGUI notesPSMesh;
    [SerializeField] private TextMeshProUGUI obstaclesMesh;
    [SerializeField] private TextMeshProUGUI eventsMesh;
    [SerializeField] private TextMeshProUGUI bpmMesh;

    public static bool IsActive { get; private set; } = false;

    private void Start()
    {
        StartCoroutine(DelayedUpdate());
    }
    
    private IEnumerator DelayedUpdate () {
        while (true)
        {
            yield return new WaitForSeconds(1); //I wouldn't want to update this every single frame.
            if (SelectionController.HasSelectedObjects()) {
                List<BeatmapObjectContainer> sel = SelectionController.SelectedObjects.OrderBy(x => x.objectData._time).ToList();
                int notes = SelectionController.SelectedObjects.Where(x => x is BeatmapNoteContainer).Count();
                notesMesh.text = $"Selected Notes: {notes}";
                float beatTimeDiff = sel.Last().objectData._time - sel.First().objectData._time;
                float secDiff = atsc.GetSecondsFromBeat(beatTimeDiff);
                notesPSMesh.text = $"Selected NPS: {(notes / secDiff).ToString("F2")}";
            }
            else {
                notesMesh.text = $"Notes: {notes.LoadedContainers.Count}";
                notesPSMesh.text = $"Notes Per Second: {(notes.LoadedContainers.Count / cameraAudioSource.clip.length).ToString("F2")}";
            }
            obstaclesMesh.text = $"Obstacles: {obstacles.LoadedContainers.Count}";
            eventsMesh.text = $"Events: {events.LoadedContainers.Count}";
            bpmMesh.text = $"BPM Changes: {bpm.LoadedContainers.Count}";
        }
	}

    public void ToggleCounters(bool enabled)
    {
        IsActive = enabled;
        notesMesh.gameObject.SetActive(enabled);
        notesPSMesh.gameObject.SetActive(enabled);
        obstaclesMesh.gameObject.SetActive(enabled);
        eventsMesh.gameObject.SetActive(enabled);
        bpmMesh.gameObject.SetActive(enabled);
    }
}
