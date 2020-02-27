using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotesContainer : BeatmapObjectContainerCollection {

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;

    public static bool ShowArcVisualizer { get; private set; } = false;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.NOTE;

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

    private void OnPlayToggle(bool isPlaying)
    {
        foreach (BeatmapObjectContainer obj in LoadedContainers)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) //Welcome to Python.
                foreach (Material mat in renderer.materials)
                    if (mat.HasProperty("_Editor_IsPlaying"))
                        mat.SetFloat("_Editor_IsPlaying", isPlaying ? 1 : 0);
        }
    }

    public override void SortObjects() {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time) //0 -> end of map
            .ThenBy(x => ((BeatmapNote) x.objectData)._lineLayer) //0 -> 2
            .ThenBy(x => ((BeatmapNote)x.objectData)._lineIndex) //0 -> 3
            .ThenBy(x => ((BeatmapNote) x.objectData)._type) //Red -> Blue -> Bomb
            .ToList();
        uint id = 0;
        foreach (var t in LoadedContainers)
        {
            if (t.objectData is BeatmapNote noteData) {
                noteData.id = id;
                t.gameObject.name = "Note " + id;
                id++;
            }
        }
        UseChunkLoading = true;
    }

    //We don't need to check index as that's already done further up the chain
    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        BeatmapObjectContainer e = LoadedContainers[index];
        e.SafeSetActive(true);
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

    public void UpdateSwingArcVisualizer()
    {
        ShowArcVisualizer = !ShowArcVisualizer;
        foreach (BeatmapNoteContainer note in LoadedContainers.Cast<BeatmapNoteContainer>())
            note.SetArcVisible(ShowArcVisualizer);
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true)
    {
        conflicting = null;
        if (removeConflicting)
        {
            conflicting = LoadedContainers.FirstOrDefault(x => x.objectData._time == obj._time &&
                ((BeatmapNote)obj)._lineLayer == ((BeatmapNote)x.objectData)._lineLayer &&
                ((BeatmapNote)obj)._lineIndex == ((BeatmapNote)x.objectData)._lineIndex &&
                ((BeatmapNote)obj)._type == ((BeatmapNote)x.objectData)._type &&
                ConflictingByTrackIDs(obj, x.objectData)
            );
            if (conflicting != null) DeleteObject(conflicting, true, $"Conflicted with a newer object at time {obj._time}");
        }
        BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote(obj as BeatmapNote, ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
        beatmapNote.transform.SetParent(GridTransform);
        beatmapNote.UpdateGridPosition();
        LoadedContainers.Add(beatmapNote);
        foreach (BeatmapObjectContainer container in LoadedContainers)
        {
            if (container.OutlineVisible && !SelectionController.IsObjectSelected(container)) container.OutlineVisible = false;
        }
        if (Settings.Instance.HighlightLastPlacedNotes) beatmapNote.SetOutlineColor(Color.magenta);
        if (refreshMap) SelectionController.RefreshMap();
        return beatmapNote;
    }
}
