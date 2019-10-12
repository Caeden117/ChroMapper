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
    }

    internal override void UnsubscribeToCallbacks() {
        SpawnCallbackController.NotePassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold -= DespawnCallback;
    }

    public override void SortObjects() {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        uint id = 0;
        for (int i = 0; i < LoadedContainers.Count; i++) {
            if (LoadedContainers[i].objectData is BeatmapNote noteData) {
                noteData.id = id;
                LoadedContainers[i].gameObject.name = "Note " + id;
                id++;
            }
        }
        UseChunkLoading = true;
    }

    //We don't need to check index as that's already done further up the chain
    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        BeatmapObjectContainer e = LoadedContainers[index];
        if (e.PreviousActiveState != true)
        {
            e.gameObject.SetActive(true);
            e.SafeSetActive(true);
        }
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        BeatmapObjectContainer e = LoadedContainers[index];
        e.SafeSetActive(false);
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex) {
        for (int i = 0; i < LoadedContainers.Count; i++)
            LoadedContainers[i].SafeSetActive(i < SpawnCallbackController.NextNoteIndex && i >= DespawnCallbackController.NextNoteIndex);
    }

    public void UpdateColor(Color red, Color blue)
    {
        noteAppearanceSO.UpdateColor(red, blue);
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting)
    {
        conflicting = LoadedContainers.FirstOrDefault(x => x.objectData._time == obj._time &&
            (obj as BeatmapNote)._lineLayer == (x.objectData as BeatmapNote)._lineLayer &&
            (obj as BeatmapNote)._lineIndex == (x.objectData as BeatmapNote)._lineIndex
        );
        if (conflicting != null) DeleteObject(conflicting);
        BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote(obj as BeatmapNote, ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
        beatmapNote.transform.SetParent(GridTransform);
        beatmapNote.UpdateGridPosition();
        LoadedContainers.Add(beatmapNote);
        SelectionController.RefreshMap();
        return beatmapNote;
    }
}
