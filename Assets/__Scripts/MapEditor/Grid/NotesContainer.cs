using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotesContainer : MonoBehaviour {

    [SerializeField] AudioTimeSyncController audioTimeSyncController;

    //An easy way to edit notes
    [SerializeField] public List<BeatmapObjectContainer> loadedNotes = new List<BeatmapObjectContainer>();
    
    [SerializeField] BeatmapObjectCallbackController spawnCallbackController;
    [SerializeField] BeatmapObjectCallbackController despawnCallbackController;

    private void OnEnable() {
        //audioTimeSyncController.OnPlayToggle += OnPlayToggle;

        spawnCallbackController.NotePassedThreshold += SpawnCallback;
        spawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        despawnCallbackController.NotePassedThreshold += DespawnCallback;
    }

    private void OnDisable() {
        //audioTimeSyncController.OnPlayToggle -= OnPlayToggle;

        spawnCallbackController.NotePassedThreshold -= SpawnCallback;
        spawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        despawnCallbackController.NotePassedThreshold -= DespawnCallback;
    }

    public void SortNotes() {
        loadedNotes = loadedNotes.OrderBy(x => x.objectData._time).ToList();
        uint id = 0;
        for (int i = 0; i < loadedNotes.Count; i++) {
            if (loadedNotes[i].objectData is BeatmapNote) {
                BeatmapNote noteData = (BeatmapNote)loadedNotes[i].objectData;
                noteData.id = id;
                loadedNotes[i].gameObject.name = "Note " + id;
                id++;
            }
        }
    }

    //We don't need to check index as that's already done further up the chain
    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            loadedNotes[index].gameObject.SetActive(true);
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            loadedNotes[index].gameObject.SetActive(false);
    }
    
    void OnPlayToggle(bool playing) {
        if (playing) {
            for (int i = 0; i < loadedNotes.Count; i++) {
                loadedNotes[i].gameObject.SetActive(i < spawnCallbackController.NextNoteIndex && i >= despawnCallbackController.NextNoteIndex);
            }
        } else {
            for (int i = 0; i < loadedNotes.Count; i++) {
                loadedNotes[i].gameObject.SetActive(true);
            }
        }
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex) {
        if (audioTimeSyncController.IsPlaying) {
            for (int i = 0; i < loadedNotes.Count; i++) {
                loadedNotes[i].gameObject.SetActive(i < spawnCallbackController.NextNoteIndex && i >= despawnCallbackController.NextNoteIndex);
            }
        } else {
            for (int i = 0; i < loadedNotes.Count; i++) {
                loadedNotes[i].gameObject.SetActive(true);
            }
        }
    }

}
