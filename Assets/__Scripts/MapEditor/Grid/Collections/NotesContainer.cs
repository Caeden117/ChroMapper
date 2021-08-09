using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotesContainer : BeatmapObjectContainerCollection {

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    [SerializeField] private TracksManager tracksManager;

    [SerializeField] private CountersPlusController countersPlus;

    private HashSet<Material> allNoteRenderers = new HashSet<Material>();

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
        if (!isPlaying)
        {
            RefreshPool();
        }
    }

    // This should hopefully return a sorted list of notes to prevent flipped stack notes when playing in game.
    // (I'm done with note sorting; if you don't like it, go fix it yourself.)
    public override IEnumerable<BeatmapObject> GrabSortedObjects()
    {
        List<BeatmapObject> sorted = new List<BeatmapObject>();
        var grouping = LoadedObjects.GroupBy(x => x._time);
        foreach (var group in grouping)
        {
            sorted.AddRange(group.OrderBy(x => ((BeatmapNote)x)._lineIndex) //0 -> 3
            .ThenBy(x => ((BeatmapNote)x)._lineLayer) //0 -> 2
            .ThenBy(x => ((BeatmapNote)x)._type));
        }
        return sorted;
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
        foreach (BeatmapNoteContainer note in LoadedContainers.Values.Cast<BeatmapNoteContainer>())
            note.SetArcVisible(ShowArcVisualizer);
    }

    public override BeatmapObjectContainer CreateContainer()
    {
        BeatmapObjectContainer con = BeatmapNoteContainer.SpawnBeatmapNote(null, ref notePrefab);
        return con;
    }

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        BeatmapNoteContainer note = con as BeatmapNoteContainer;
        BeatmapNote noteData = obj as BeatmapNote;
        noteAppearanceSO.SetNoteAppearance(note);
        note.Setup();
        note.SetBomb(noteData._type == BeatmapNote.NOTE_TYPE_BOMB);
        note.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(noteData);

        Track track = tracksManager.GetTrackAtTime(obj._time);
        track.AttachContainer(con);
    }

    protected override void OnObjectSpawned(BeatmapObject _)
    {
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);
    }

    protected override void OnObjectDelete(BeatmapObject _)
    {
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);
    }

    // Here we check to see if any special angled notes are required.
    protected override void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj)
    {
        RefreshSpecialAngles(obj, true, AudioTimeSyncController.IsPlaying);
    }

    protected override void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj)
    {
        RefreshSpecialAngles(obj, false, AudioTimeSyncController.IsPlaying);
    }

    private List<BeatmapObjectContainer> objectsAtSameTime = new List<BeatmapObjectContainer>();

    public void RefreshSpecialAngles(BeatmapObject obj, bool objectWasSpawned, bool isNatural)
    {
        // Do not bother refreshing if objects are despawning naturally (while playing back the song)
        if (!objectWasSpawned && isNatural) return;
        // Do not do special angles for bombs
        if ((obj as BeatmapNote)._type == BeatmapNote.NOTE_TYPE_BOMB) return;
        // Grab all objects with the same type, and time (within epsilon)

        objectsAtSameTime.Clear();
        foreach (var x in LoadedContainers)
        {
            if (!(x.Key._time - Epsilon <= obj._time && x.Key._time + Epsilon >= obj._time &&
            (x.Key as BeatmapNote)._type == (obj as BeatmapNote)._type)) continue;

            objectsAtSameTime.Add(x.Value);
        }

        // Only execute if we have exactly 2 notes with the same type
        if (objectsAtSameTime.Count == 2)
        {
            // Due to the potential for "obj" not having a container, we cannot reuse it as "a".
            BeatmapNote a = objectsAtSameTime.First().objectData as BeatmapNote;
            BeatmapNote b = objectsAtSameTime.Last().objectData as BeatmapNote;

            // Grab the containers we will be flipping
            BeatmapObjectContainer containerA = objectsAtSameTime.First();
            BeatmapObjectContainer containerB = objectsAtSameTime.Last();

            // Do not execute if cut directions are not the same (and both are not dot notes)
            if (a._cutDirection != b._cutDirection && a._cutDirection != BeatmapNote.NOTE_CUT_DIRECTION_ANY &&
                b._cutDirection != BeatmapNote.NOTE_CUT_DIRECTION_ANY)
            {
                return;
            }
            if (a._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY)
            {
                (a, b) = (b, a); // You can flip variables like this in C#. Who knew?
                (containerA, containerB) = (containerB, containerA);
            }
            Vector2 posA = containerA.transform.localPosition;
            Vector2 posB = containerB.transform.localPosition;
            Vector2 cutVector = a._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY ? Vector2.up : Direction(a);
            Vector2 line = posA - posB;
            float angle = SignedAngleToLine(cutVector, line);

            // if both notes are dots, line them up with each other by adding the signed angle.
            if (a._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY && b._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY)
            {
                containerA.transform.localEulerAngles = Vector3.forward * angle;
                containerB.transform.localEulerAngles = Vector3.forward * angle;
            }
            else
            {
                Vector3 originalA = BeatmapNoteContainer.Directionalize(a);
                Vector3 originalB = BeatmapNoteContainer.Directionalize(b);
                // We restrict angles below 40 (For 45 just use diagonal notes KEKW)
                if (Mathf.Abs(angle) <= 40)
                {
                    containerA.transform.localEulerAngles = originalA + (Vector3.forward * angle);
                    if (b._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY && !a.IsMainDirection)
                    {
                        containerB.transform.localEulerAngles = originalB + (Vector3.forward * (angle + 45));
                    }
                    else
                    {
                        containerB.transform.localEulerAngles = originalB + (Vector3.forward * angle);
                    }
                }
            }
        }
        else
        {
            foreach (var toReset in objectsAtSameTime)
            {
                Vector3 direction = BeatmapNoteContainer.Directionalize(toReset.objectData as BeatmapNote);
                toReset.transform.localEulerAngles = direction;
            }
        }
    }

    // Grab a Vector2 plane based on the cut direction
    private Vector2 Direction(BeatmapNote obj)
    {
        switch (obj._cutDirection)
        {
            case BeatmapNote.NOTE_CUT_DIRECTION_UP: return new Vector2(0f, 1f);
            case BeatmapNote.NOTE_CUT_DIRECTION_DOWN: return new Vector2(0f, -1f);
            case BeatmapNote.NOTE_CUT_DIRECTION_LEFT: return new Vector2(-1f, 0f);
            case BeatmapNote.NOTE_CUT_DIRECTION_RIGHT: return new Vector2(1f, 0f);
            case BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT: return new Vector2(-0.7071f, 0.7071f);
            case BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT: return new Vector2(0.7071f, 0.7071f);
            case BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT: return new Vector2(-0.7071f, -0.7071f);
            case BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT: return new Vector2(0.7071f, -0.7071f);
            default: return new Vector2(0f, 0f);
        }
    }

    // Totally not ripped from Beat Saber (jaroslav beck plz dont hurt me)
    private float SignedAngleToLine(Vector2 vec, Vector2 line)
    {
        float positive = Vector2.SignedAngle(vec, line);
        float negative = Vector2.SignedAngle(vec, -line);
        if (Mathf.Abs(positive) >= Mathf.Abs(negative)) return negative;
        return positive;
    }
}
