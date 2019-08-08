using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CountersPlusController : MonoBehaviour {

    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
    [SerializeField] private TextMeshProUGUI notesMesh;
    [SerializeField] private TextMeshProUGUI obstaclesMesh;
    [SerializeField] private TextMeshProUGUI eventsMesh;
    [SerializeField] private TextMeshProUGUI bpmMesh;

    private void Start()
    {
        StartCoroutine(DelayedUpdate());
    }
    
    private IEnumerator DelayedUpdate () {
        while (true)
        {
            yield return new WaitForSeconds(1); //I wouldn't want to update this every single frame.
            notesMesh.text = string.Format("Notes: {0}", notes.loadedNotes.Count);
            obstaclesMesh.text = string.Format("Obstacles: {0}", obstacles.loadedObstacles.Count);
            eventsMesh.text = string.Format("Events: {0}", events.loadedEvents.Count);
            bpmMesh.text = string.Format("BPM Changes: {0}", bpm.loadedBPMChanges.Count);
        }
	}

    public void ToggleCounters(bool enabled)
    {
        notesMesh.gameObject.SetActive(enabled);
        obstaclesMesh.gameObject.SetActive(enabled);
        eventsMesh.gameObject.SetActive(enabled);
        bpmMesh.gameObject.SetActive(enabled);
    }
}
