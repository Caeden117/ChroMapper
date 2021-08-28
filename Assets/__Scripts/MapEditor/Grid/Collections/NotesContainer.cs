using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class NotesContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [FormerlySerializedAs("noteAppearanceSO")] [SerializeField] private NoteAppearanceSO noteAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    [SerializeField] private CountersPlusController countersPlus;

    private readonly List<BeatmapObjectContainer> objectsAtSameTime = new List<BeatmapObjectContainer>();

    public static bool ShowArcVisualizer { get; private set; }

    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Note;

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.NotePassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold += DespawnCallback;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.NotePassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold -= DespawnCallback;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }

    // This should hopefully return a sorted list of notes to prevent flipped stack notes when playing in game.
    // (I'm done with note sorting; if you don't like it, go fix it yourself.)
    public override IEnumerable<BeatmapObject> GrabSortedObjects()
    {
        var sorted = new List<BeatmapObject>();
        var grouping = LoadedObjects.GroupBy(x => x.Time);
        foreach (var group in grouping)
        {
            sorted.AddRange(@group.OrderBy(x => ((BeatmapNote)x).LineIndex) //0 -> 3
                .ThenBy(x => ((BeatmapNote)x).LineLayer) //0 -> 2
                .ThenBy(x => ((BeatmapNote)x).Type));
        }

        return sorted;
    }

    //We don't need to check index as that's already done further up the chain
    private void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData)) RecycleContainer(objectData);
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    public void UpdateColor(Color red, Color blue) => noteAppearanceSo.UpdateColor(red, blue);

    public void UpdateSwingArcVisualizer()
    {
        ShowArcVisualizer = !ShowArcVisualizer;
        foreach (var note in LoadedContainers.Values.Cast<BeatmapNoteContainer>())
            note.SetArcVisible(ShowArcVisualizer);
    }

    public override BeatmapObjectContainer CreateContainer()
    {
        BeatmapObjectContainer con = BeatmapNoteContainer.SpawnBeatmapNote(null, ref notePrefab);
        return con;
    }

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var note = con as BeatmapNoteContainer;
        var noteData = obj as BeatmapNote;
        noteAppearanceSo.SetNoteAppearance(note);
        note.Setup();
        note.SetBomb(noteData.Type == BeatmapNote.NoteTypeBomb);
        note.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(noteData);

        var track = tracksManager.GetTrackAtTime(obj.Time);
        track.AttachContainer(con);
    }

    protected override void OnObjectSpawned(BeatmapObject _) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);

    protected override void OnObjectDelete(BeatmapObject _) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);

    // Here we check to see if any special angled notes are required.
    protected override void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj) =>
        RefreshSpecialAngles(obj, true, AudioTimeSyncController.IsPlaying);

    protected override void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj) =>
        RefreshSpecialAngles(obj, false, AudioTimeSyncController.IsPlaying);

    public void RefreshSpecialAngles(BeatmapObject obj, bool objectWasSpawned, bool isNatural)
    {
        // Do not bother refreshing if objects are despawning naturally (while playing back the song)
        if (!objectWasSpawned && isNatural) return;
        // Do not do special angles for bombs
        if ((obj as BeatmapNote).Type == BeatmapNote.NoteTypeBomb) return;
        // Grab all objects with the same type, and time (within epsilon)

        objectsAtSameTime.Clear();
        foreach (var x in LoadedContainers)
        {
            if (!(x.Key.Time - Epsilon <= obj.Time && x.Key.Time + Epsilon >= obj.Time &&
                  (x.Key as BeatmapNote).Type == (obj as BeatmapNote).Type))
            {
                continue;
            }

            objectsAtSameTime.Add(x.Value);
        }

        // Only execute if we have exactly 2 notes with the same type
        if (objectsAtSameTime.Count == 2)
        {
            // Due to the potential for "obj" not having a container, we cannot reuse it as "a".
            var a = objectsAtSameTime.First().ObjectData as BeatmapNote;
            var b = objectsAtSameTime.Last().ObjectData as BeatmapNote;

            // Grab the containers we will be flipping
            var containerA = objectsAtSameTime.First();
            var containerB = objectsAtSameTime.Last();

            // Do not execute if cut directions are not the same (and both are not dot notes)
            if (a.CutDirection != b.CutDirection && a.CutDirection != BeatmapNote.NoteCutDirectionAny &&
                b.CutDirection != BeatmapNote.NoteCutDirectionAny)
            {
                return;
            }

            if (a.CutDirection == BeatmapNote.NoteCutDirectionAny)
            {
                (a, b) = (b, a); // You can flip variables like this in C#. Who knew?
                (containerA, containerB) = (containerB, containerA);
            }

            Vector2 posA = containerA.transform.localPosition;
            Vector2 posB = containerB.transform.localPosition;
            var cutVector = a.CutDirection == BeatmapNote.NoteCutDirectionAny ? Vector2.up : Direction(a);
            var line = posA - posB;
            var angle = SignedAngleToLine(cutVector, line);

            // if both notes are dots, line them up with each other by adding the signed angle.
            if (a.CutDirection == BeatmapNote.NoteCutDirectionAny &&
                b.CutDirection == BeatmapNote.NoteCutDirectionAny)
            {
                containerA.transform.localEulerAngles = Vector3.forward * angle;
                containerB.transform.localEulerAngles = Vector3.forward * angle;
            }
            else
            {
                var originalA = BeatmapNoteContainer.Directionalize(a);
                var originalB = BeatmapNoteContainer.Directionalize(b);
                // We restrict angles below 40 (For 45 just use diagonal notes KEKW)
                if (Mathf.Abs(angle) <= 40)
                {
                    containerA.transform.localEulerAngles = originalA + (Vector3.forward * angle);
                    if (b.CutDirection == BeatmapNote.NoteCutDirectionAny && !a.IsMainDirection)
                        containerB.transform.localEulerAngles = originalB + (Vector3.forward * (angle + 45));
                    else
                        containerB.transform.localEulerAngles = originalB + (Vector3.forward * angle);
                }
            }
        }
        else
        {
            foreach (var toReset in objectsAtSameTime)
            {
                var direction = BeatmapNoteContainer.Directionalize(toReset.ObjectData as BeatmapNote);
                toReset.transform.localEulerAngles = direction;
            }
        }
    }

    // Grab a Vector2 plane based on the cut direction
    private Vector2 Direction(BeatmapNote obj)
    {
        return obj.CutDirection switch
        {
            BeatmapNote.NoteCutDirectionUp => new Vector2(0f, 1f),
            BeatmapNote.NoteCutDirectionDown => new Vector2(0f, -1f),
            BeatmapNote.NoteCutDirectionLeft => new Vector2(-1f, 0f),
            BeatmapNote.NoteCutDirectionRight => new Vector2(1f, 0f),
            BeatmapNote.NoteCutDirectionUpLeft => new Vector2(-0.7071f, 0.7071f),
            BeatmapNote.NoteCutDirectionUpRight => new Vector2(0.7071f, 0.7071f),
            BeatmapNote.NoteCutDirectionDownLeft => new Vector2(-0.7071f, -0.7071f),
            BeatmapNote.NoteCutDirectionDownRight => new Vector2(0.7071f, -0.7071f),
            _ => new Vector2(0f, 0f),
        };
    }

    // Totally not ripped from Beat Saber (jaroslav beck plz dont hurt me)
    private float SignedAngleToLine(Vector2 vec, Vector2 line)
    {
        var positive = Vector2.SignedAngle(vec, line);
        var negative = Vector2.SignedAngle(vec, -line);
        if (Mathf.Abs(positive) >= Mathf.Abs(negative)) return negative;
        return positive;
    }
}
