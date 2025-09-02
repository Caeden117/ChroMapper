using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class NoteGridContainer : BeatmapObjectContainerCollection<BaseNote>
{
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [FormerlySerializedAs("noteAppearanceSO")] [SerializeField] private NoteAppearanceSO noteAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    [SerializeField] private CountersPlusController countersPlus;

    private readonly List<ObjectContainer> objectsAtSameTime = new();

    public static bool ShowArcVisualizer { get; private set; }

    public override ObjectType ContainerType => ObjectType.Note;

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.NotePassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold += DespawnCallback;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        UIMode.PreviewModeSwitched += OnUIPreviewModeSwitch;

        Settings.NotifyBySettingName(nameof(Settings.NoteColorMultiplier), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.ArrowColorMultiplier), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.ArrowColorWhiteBlend), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.AccurateNoteSize), AppearanceChanged);
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.NotePassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished -= RecursiveCheckFinished;
        DespawnCallbackController.NotePassedThreshold -= DespawnCallback;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        UIMode.PreviewModeSwitched -= OnUIPreviewModeSwitch;

        Settings.ClearSettingNotifications(nameof(Settings.NoteColorMultiplier));
        Settings.ClearSettingNotifications(nameof(Settings.ArrowColorMultiplier));
        Settings.ClearSettingNotifications(nameof(Settings.ArrowColorWhiteBlend));
        Settings.ClearSettingNotifications(nameof(Settings.AccurateNoteSize));
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    private void AppearanceChanged(object _) => RefreshPool(true);

    //We don't need to check index as that's already done further up the chain
    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            if (!LoadedContainers[objectData].Animator.AnimatedLife)
                RecycleContainer(objectData);
            else
                LoadedContainers[objectData].Animator.ShouldRecycle = true;
        }
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    public void UpdateColor(Color red, Color blue) => noteAppearanceSo.UpdateColor(red, blue);

    public override ObjectContainer CreateContainer()
    {
        ObjectContainer con = NoteContainer.SpawnBeatmapNote(null, ref notePrefab);
        con.Animator.Atsc = AudioTimeSyncController;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var note = con as NoteContainer;
        var noteData = obj as BaseNote;
        noteAppearanceSo.SetNoteAppearance(note);
        note.Setup();
        note.DirectionTargetEuler = NoteContainer.Directionalize(noteData);

        if (!note.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.SongBpmTime);
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

        // Do not do special angles for bombs and fakes
        var note = obj as BaseNote;
        if (note.Type == (int)NoteType.Bomb || note.CustomFake) return;

        // Grab all objects with the same type, and time (within epsilon)
        PopulateObjectsAtSameTime(note);

        // Early return if we don't have exactly 2 notes to snap
        if (objectsAtSameTime.Count != 2)
        {
            ClearSpecialAnglesFromObjectsAtSameTime();
            return;
        }

        // Due to the potential for "obj" not having a container, we cannot reuse it as "a".
        var a = objectsAtSameTime[0].ObjectData as BaseNote;
        var b = objectsAtSameTime[^1].ObjectData as BaseNote;

        // Grab the containers we will be flipping
        var containerA = objectsAtSameTime[0] as NoteContainer;
        var containerB = objectsAtSameTime[^1] as NoteContainer;

        // Clear angles if directions are not the same (and both are not dot notes) or is precision placed
        var hasNEcoordinates = a.CustomCoordinate != null || b.CustomCoordinate != null;
        var hasMEprecision = a.CutDirection >= 1000 || a.CutDirection <= -1000 ||
                             b.CutDirection >= 1000 || b.CutDirection <= -1000;

        if (a.CutDirection != b.CutDirection && a.CutDirection != (int)NoteCutDirection.Any &&
            b.CutDirection != (int)NoteCutDirection.Any && !hasMEprecision && !hasNEcoordinates)
        {
            var directionA = NoteContainer.Directionalize(a);
            var directionB = NoteContainer.Directionalize(b);

            containerA.DirectionTarget.localEulerAngles = containerA.DirectionTargetEuler = directionA;
            containerB.DirectionTarget.localEulerAngles = containerB.DirectionTargetEuler = directionB;
            return;
        }

        // Swap references if our first note is a dot note
        if (a.CutDirection == (int)NoteCutDirection.Any)
        {
            (a, b) = (b, a);
            (containerA, containerB) = (containerB, containerA);
        }

        // Note jump animation broke when we used container local position. Use position from note data instead
        var posA = a.GetPosition();
        var posB = b.GetPosition();

        var cutVector = a.CutDirection == (int)NoteCutDirection.Any ? Vector2.up : Direction(a);
        var line = posA - posB;
        var angle = SignedAngleToLine(cutVector, line);

        // if both notes are dots, line them up with each other by adding the signed angle.
        if (a.CutDirection == (int)NoteCutDirection.Any &&
            b.CutDirection == (int)NoteCutDirection.Any)
        {
            containerA.DirectionTargetEuler = Vector3.forward * angle;
            containerB.DirectionTargetEuler = Vector3.forward * angle;
        }
        // We restrict angles below 40 otherwise display their normal direction
        else if (Mathf.Abs(angle) <= 40)
        {
            var originalA = NoteContainer.Directionalize(a) + new Vector3(0, 0, -a.AngleOffset);
            var originalB = NoteContainer.Directionalize(b) + new Vector3(0, 0, -b.AngleOffset);

            containerA.DirectionTargetEuler = originalA + (Vector3.forward * angle);
            if (b.CutDirection == (int)NoteCutDirection.Any && !a.IsMainDirection)
                containerB.DirectionTargetEuler = originalB + (Vector3.forward * (angle + 45));
            else
                containerB.DirectionTargetEuler = originalB + (Vector3.forward * angle);
        }
        // These notes do not snap so lets reset their angle
        else
        {
            var directionA = NoteContainer.Directionalize(a);
            var directionB = NoteContainer.Directionalize(b);

            containerA.DirectionTargetEuler = directionA;
            containerB.DirectionTargetEuler = directionB;
        }

        // Immediately update direction target
        containerA.DirectionTarget.localEulerAngles = containerA.DirectionTargetEuler;
        containerB.DirectionTarget.localEulerAngles = containerB.DirectionTargetEuler;
    }

    public void ClearSpecialAngles(BaseObject obj)
    {
        var note = obj as BaseNote;

        PopulateObjectsAtSameTime(note);
        ClearSpecialAnglesFromObjectsAtSameTime();
    }

    // Grab all objects with the same type, and time (within epsilon)
    private void PopulateObjectsAtSameTime(BaseNote note)
    {
        objectsAtSameTime.Clear();
        foreach (var x in LoadedContainers)
        {
            if (note.CustomFake
                || !(x.Key.JsonTime - Epsilon <= note.JsonTime && x.Key.JsonTime + Epsilon >= note.JsonTime
                                                               && (x.Key as BaseNote).Type == note.Type))
            {
                continue;
            }

            objectsAtSameTime.Add(x.Value);
        }
    }

    private void ClearSpecialAnglesFromObjectsAtSameTime()
    {
        foreach (var toReset in objectsAtSameTime)
        {
            var direction = NoteContainer.Directionalize(toReset.ObjectData as BaseNote);
            (toReset as NoteContainer).DirectionTarget.localEulerAngles = direction;
            (toReset as NoteContainer).DirectionTargetEuler = direction;
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
