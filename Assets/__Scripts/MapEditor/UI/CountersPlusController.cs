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
    [SerializeField] private TextMeshProUGUI selectionMesh;
    [SerializeField] private TextMeshProUGUI timeMappingMesh;

    public static bool IsActive { get; private set; } //todo: is this needed

    private void Start()
    {
        Settings.NotifyBySettingName("CountersPlus", ToggleCounters);
        ToggleCounters(Settings.Instance.CountersPlus);
        StartCoroutine(DelayedUpdate());
    }
    
    private IEnumerator DelayedUpdate () {
        while (true)
        {
            yield return new WaitForSeconds(1); //I wouldn't want to update this every single frame.
            List<BeatmapObjectContainer> sel = SelectionController.SelectedObjects.OrderBy(x => x.objectData._time).ToList();
            int notesel = SelectionController.SelectedObjects.Where(x => x is BeatmapNoteContainer).Count(); // only active when notes are selected
            if (SelectionController.HasSelectedObjects() && notesel > 0) {
                notesMesh.text = $"Selected Notes: {notesel}";
                float beatTimeDiff = sel.Last().objectData._time - sel.First().objectData._time;
                float secDiff = atsc.GetSecondsFromBeat(beatTimeDiff);
                notesPSMesh.text = $"Selected NPS: {(notesel / secDiff).ToString("F2")}";
            }
            else {
                notesMesh.text = $"Notes: {notes.LoadedContainers.Count}";
                notesPSMesh.text = $"Notes Per Second: {(notes.LoadedContainers.Count / cameraAudioSource.clip.length).ToString("F2")}";
            }
            obstaclesMesh.text = $"Obstacles: {obstacles.LoadedContainers.Count}";
            eventsMesh.text = $"Events: {events.LoadedContainers.Count}";
            bpmMesh.text = $"BPM Changes: {BeatSaberSongContainer.Instance.map._BPMChanges.Count}";

            float timeMapping = BeatSaberSongContainer.Instance.map._time;
            int seconds = Mathf.Abs(Mathf.FloorToInt(timeMapping * 60 % 60));
            int minutes = Mathf.FloorToInt(timeMapping % 60);
            int hours = Mathf.FloorToInt(timeMapping / 60);
            timeMappingMesh.text = string.Format("Time Mapping: {0:0}:{1:00}:{2:00}", hours, minutes, seconds);
        }
	}

    private void Update() // i do want to update this every single frame
    {
        if (Application.isFocused) BeatSaberSongContainer.Instance.map._time += Time.deltaTime / 60; // only tick while application is focused
        if (SelectionController.HasSelectedObjects()) // selected counter; does not rely on counters+ option
        {
            selectionMesh.text = $"Selected: {SelectionController.SelectedObjects.Count()}";
        }
        selectionMesh.gameObject.SetActive(SelectionController.HasSelectedObjects());
    }

    public void ToggleCounters(object value)
    {
        bool enabled = (bool)value;
        IsActive = enabled;
        foreach (Transform child in transform) child.gameObject.SetActive(enabled);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("CountersPlus");
    }
}
