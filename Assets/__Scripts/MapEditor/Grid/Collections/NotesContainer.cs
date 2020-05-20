using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotesContainer : BeatmapObjectContainerCollection {

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    [SerializeField] private TracksManager tracksManager;

    private List<Material> allNoteRenderers = new List<Material>();

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
        foreach (Material mat in allNoteRenderers)
        {
            mat?.SetFloat("_Editor_IsPlaying", isPlaying ? 1 : 0);
        }
        if (!isPlaying)
        {
            RefreshPool();
        }
    }

    public override void SortObjects() {
        LoadedObjects = new SortedSet<BeatmapObject>(
            LoadedObjects.OrderBy(x => ((BeatmapNote)x)._lineIndex) //0 -> 3
            .ThenBy(x => ((BeatmapNote)x)._lineLayer) //0 -> 2
            .ThenBy(x => ((BeatmapNote)x)._type), new BeatmapObjectComparer()); //Red -> Blue -> Bomb
        UseChunkLoading = true;
    }

    //We don't need to check index as that's already done further up the chain
    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData))
        {
            CreateContainerFromPool(objectData);
        }
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            RecycleContainer(objectData);
        }
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        RefreshPool();
    }

    public void UpdateColor(Color red, Color blue)
    {
        noteAppearanceSO.UpdateColor(red, blue);
    }

    public void UpdateSwingArcVisualizer()
    {
        ShowArcVisualizer = !ShowArcVisualizer;
        foreach (BeatmapNoteContainer note in LoadedObjects.Cast<BeatmapNoteContainer>())
            note.SetArcVisible(ShowArcVisualizer);
    }

    protected override bool AreObjectsAtSameTimeConflicting(BeatmapObject a, BeatmapObject b)
    {
        BeatmapNote noteA = a as BeatmapNote;
        BeatmapNote noteB = b as BeatmapNote;
        return noteA._lineIndex == noteB._lineIndex && noteA._lineLayer == noteB._lineLayer;
    }

    public override BeatmapObjectContainer CreateContainer()
    {
        BeatmapObjectContainer con = BeatmapNoteContainer.SpawnBeatmapNote(null, ref notePrefab);
        foreach (Renderer renderer in con.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in renderer.materials.Where(x => x?.HasProperty("_Editor_IsPlaying") ?? false))
            {
                allNoteRenderers.Add(mat);
            }
        }
        return con;
    }

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        BeatmapNoteContainer note = con as BeatmapNoteContainer;
        BeatmapNote noteData = obj as BeatmapNote;
        note.SetBomb(noteData._type == BeatmapNote.NOTE_TYPE_BOMB);
        noteAppearanceSO.SetNoteAppearance(note);
        note.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(noteData);
        Track track = tracksManager.GetTrackAtTime(obj._time);
        track.AttachContainer(con);
    }
}
