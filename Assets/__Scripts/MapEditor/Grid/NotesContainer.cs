using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotesContainer : BeatmapObjectContainerCollection {

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;

    internal override void SubscribeToCallbacks() {
        SpawnCallbackController.NotePassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold += DespawnCallback;
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks() {
        SpawnCallbackController.NotePassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold -= DespawnCallback;
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    public override void SortObjects() {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        uint id = 0;
        for (int i = 0; i < LoadedContainers.Count; i++) {
            if (LoadedContainers[i].objectData is BeatmapNote) {
                BeatmapNote noteData = (BeatmapNote)LoadedContainers[i].objectData;
                noteData.id = id;
                LoadedContainers[i].gameObject.name = "Note " + id;
                id++;
            }
        }
    }

    //We don't need to check index as that's already done further up the chain
    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            LoadedContainers[index].gameObject.SetActive(true);
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            LoadedContainers[index].gameObject.SetActive(false);
    }
    
    void OnPlayToggle(bool playing) {
        if (playing)
            for (int i = 0; i < LoadedContainers.Count; i++)
                LoadedContainers[i].gameObject.SetActive(i < SpawnCallbackController.NextNoteIndex && i >= DespawnCallbackController.NextNoteIndex);
        else
            for (int i = 0; i < LoadedContainers.Count; i++)
                LoadedContainers[i].gameObject.SetActive(true);
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex) {
        OnPlayToggle(AudioTimeSyncController.IsPlaying);
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj)
    {
        BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote(obj as BeatmapNote, ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
        beatmapNote.transform.SetParent(GridTransform);
        beatmapNote.UpdateGridPosition();
        LoadedContainers.Add(beatmapNote);
        return beatmapNote;
    }
}
