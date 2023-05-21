using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.Serialization;

public class NoteGridContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [FormerlySerializedAs("noteAppearanceSO")][SerializeField] private NoteAppearanceSO noteAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    [SerializeField] private CountersPlusController countersPlus;

    private readonly List<ObjectContainer> objectsAtSameTime = new List<ObjectContainer>();

    public static bool ShowArcVisualizer { get; private set; }

    public override ObjectType ContainerType => ObjectType.Note;

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
    public override IEnumerable<BaseObject> GrabSortedObjects()
    {
        var sorted = new List<BaseObject>();
        var grouping = LoadedObjects.GroupBy(x => x.JsonTime);
        foreach (var group in grouping)
        {
            sorted.AddRange(@group.OrderBy(x => ((BaseNote)x).PosX) //0 -> 3
                .ThenBy(x => ((BaseNote)x).PosY) //0 -> 2
                .ThenBy(x => ((BaseNote)x).Type));
        }

        return sorted;
    }

    //We don't need to check index as that's already done further up the chain
    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData)) RecycleContainer(objectData);
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    public void UpdateColor(Color red, Color blue) => noteAppearanceSo.UpdateColor(red, blue);

    public void UpdateSwingArcVisualizer()
    {
        ShowArcVisualizer = !ShowArcVisualizer;
        foreach (var note in LoadedContainers.Values.Cast<NoteContainer>())
            note.SetArcVisible(ShowArcVisualizer);
    }

    public override ObjectContainer CreateContainer()
    {
        ObjectContainer con = NoteContainer.SpawnBeatmapNote(null, ref notePrefab);
        con.Animator.Atsc = AudioTimeSyncController;
        con.Animator.tracksManager = tracksManager;
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var note = con as NoteContainer;
        var noteData = obj as BaseNote;
        noteAppearanceSo.SetNoteAppearance(note);
        note.Setup();
        note.SetBomb(noteData.Type == (int)NoteType.Bomb);
        note.directionTarget.transform.localEulerAngles = NoteContainer.Directionalize(noteData);

        if (!note.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.JsonTime);
            track.AttachContainer(con);
        }
    }

    protected override void OnObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);

    protected override void OnObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);

    // Here we check to see if any special angled notes are required.
    protected override void OnContainerSpawn(ObjectContainer container, BaseObject obj) =>
        RefreshSpecialAngles(obj, true, AudioTimeSyncController.IsPlaying);

    protected override void OnContainerDespawn(ObjectContainer container, BaseObject obj) =>
        RefreshSpecialAngles(obj, false, AudioTimeSyncController.IsPlaying);

    public void RefreshSpecialAngles(BaseObject obj, bool objectWasSpawned, bool isNatural)
    {
        // Do not bother refreshing if objects are despawning naturally (while playing back the song)
        if (!objectWasSpawned && isNatural) return;
        // Do not do special angles for bombs
        if ((obj as BaseNote).Type == (int)NoteType.Bomb) return;
        // Grab all objects with the same type, and time (within epsilon)

        objectsAtSameTime.Clear();
        foreach (var x in LoadedContainers)
        {
            if (!(x.Key.JsonTime - Epsilon <= obj.JsonTime && x.Key.JsonTime + Epsilon >= obj.JsonTime &&
                  (x.Key as BaseNote).Type == (obj as BaseNote).Type))
            {
                continue;
            }

            objectsAtSameTime.Add(x.Value);
        }

        // Only execute if we have exactly 2 notes with the same type
        if (objectsAtSameTime.Count == 2)
        {
            // Due to the potential for "obj" not having a container, we cannot reuse it as "a".
            var a = objectsAtSameTime.First().ObjectData as BaseNote;
            var b = objectsAtSameTime.Last().ObjectData as BaseNote;

            // Grab the containers we will be flipping
            var containerA = objectsAtSameTime.First() as NoteContainer;
            var containerB = objectsAtSameTime.Last() as NoteContainer;

            // Do not execute if cut directions are not the same (and both are not dot notes)
            if (a.CutDirection != b.CutDirection && a.CutDirection != (int)NoteCutDirection.Any &&
                b.CutDirection != (int)NoteCutDirection.Any)
            {
                return;
            }

            if (a.CutDirection == (int)NoteCutDirection.Any)
            {
                (a, b) = (b, a); // You can flip variables like this in C#. Who knew?
                (containerA, containerB) = (containerB, containerA);
            }

            Vector2 posA = containerA.GridPosition;
            Vector2 posB = containerB.GridPosition;
            var cutVector = a.CutDirection == (int)NoteCutDirection.Any ? Vector2.up : Direction(a);
            var line = posA - posB;
            var angle = SignedAngleToLine(cutVector, line);

            // if both notes are dots, line them up with each other by adding the signed angle.
            if (a.CutDirection == (int)NoteCutDirection.Any &&
                b.CutDirection == (int)NoteCutDirection.Any)
            {
                containerA.directionTarget.transform.localEulerAngles = Vector3.forward * angle;
                containerB.directionTarget.transform.localEulerAngles = Vector3.forward * angle;
            }
            else
            {
                var originalA = (a is V3ColorNote newA) ? NoteContainer.Directionalize(a) + new Vector3(0, 0, -newA.AngleOffset) : NoteContainer.Directionalize(a);
                var originalB = (b is V3ColorNote newB) ? NoteContainer.Directionalize(b) + new Vector3(0, 0, -newB.AngleOffset) : NoteContainer.Directionalize(b);
                // We restrict angles below 40 (For 45 just use diagonal notes KEKW)
                if (Mathf.Abs(angle) <= 40)
                {
                    containerA.directionTarget.transform.localEulerAngles = originalA + (Vector3.forward * angle);
                    if (b.CutDirection == (int)NoteCutDirection.Any && !a.IsMainDirection)
                        containerB.directionTarget.transform.localEulerAngles = originalB + (Vector3.forward * (angle + 45));
                    else
                        containerB.directionTarget.transform.localEulerAngles = originalB + (Vector3.forward * angle);
                }
            }
        }
        else
        {
            foreach (var toReset in objectsAtSameTime)
            {
                var direction = NoteContainer.Directionalize(toReset.ObjectData as BaseNote);
                (toReset as NoteContainer).directionTarget.transform.localEulerAngles = direction;
            }
        }
    }

    // Grab a Vector2 plane based on the cut direction
    public static Vector2 Direction(BaseNote obj)
    {
        return obj.CutDirection switch
        {
            (int)NoteCutDirection.Up => new Vector2(0f, 1f),
            (int)NoteCutDirection.Down => new Vector2(0f, -1f),
            (int)NoteCutDirection.Left => new Vector2(-1f, 0f),
            (int)NoteCutDirection.Right => new Vector2(1f, 0f),
            (int)NoteCutDirection.UpLeft => new Vector2(-0.7071f, 0.7071f),
            (int)NoteCutDirection.UpRight => new Vector2(0.7071f, 0.7071f),
            (int)NoteCutDirection.DownLeft => new Vector2(-0.7071f, -0.7071f),
            (int)NoteCutDirection.DownRight => new Vector2(0.7071f, -0.7071f),
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
