using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// <see cref="ChainGridContainer"/> doesn't contain note(even the head note on the chain).
/// It only detects whether there is a note happening to be a head note
/// </summary>
public class ChainGridContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject chainPrefab;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private ChainAppearanceSO chainAppearanceSO;
    public const float ViewEpsilon = 0.1f; // original view is too small ?? sometimes cause error.
    public override ObjectType ContainerType => ObjectType.Chain;

    private bool isPlaying;

    public override ObjectContainer CreateContainer()
    {
        var con = ChainContainer.SpawnChain(null, ref chainPrefab);
        con.Animator.Atsc = AudioTimeSyncController;
        con.Animator.tracksManager = tracksManager;
        return con;
    }

    internal override void LateUpdate()
    {
        if (Settings.Instance.Load_MapV3)
        {
            base.LateUpdate();
        }
    }
    public void UpdateColor(Color red, Color blue) => chainAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var chain = con as ChainContainer;
        var chainData = obj as BaseChain;
        chain.ChainData = chainData;
        chainAppearanceSO.SetChainAppearance(chain);
        chain.Setup();

        if (!chain.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(chainData.SongBpmTime);
            track.AttachContainer(con);
        }
    }

    internal override void SubscribeToCallbacks()
    {
        var notesContainer = GetCollectionForType(ObjectType.Note) as NoteGridContainer;
        notesContainer.ContainerSpawnedEvent += CheckUpdatedNote;
        SpawnCallbackController.ChainPassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveChainCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.ChainPassedThreshold += DespawnCallback;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        var notesContainer = GetCollectionForType(ObjectType.Note) as NoteGridContainer;
        if (notesContainer != null)
            notesContainer.ContainerSpawnedEvent -= CheckUpdatedNote;
        SpawnCallbackController.ChainPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveChainCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.ChainPassedThreshold -= DespawnCallback;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
        this.isPlaying = isPlaying;

        foreach (ChainContainer obj in LoadedContainers.Values)
        {
            obj.SetIndicatorBlocksActive(!this.isPlaying);
        }
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    protected override void OnContainerSpawn(ObjectContainer container, BaseObject obj)
    {
        (container as ChainContainer).DetectHeadNote();
    }

    protected override void OnContainerDespawn(ObjectContainer container, BaseObject obj)
    {
        (container as ChainContainer).ResetHeadNoteScale();
    }

    private void CheckUpdatedNote(BaseObject obj)
    {
        var note = obj as BaseNote;
        if (note.Type == (int)NoteType.Bomb) return;
        var chains = GetBetween(note.JsonTime - ViewEpsilon, note.JsonTime + ViewEpsilon);
        foreach (BaseChain chain in chains)
        {
            LoadedContainers.TryGetValue(chain, out var con);
            var container = con as ChainContainer;
            if (container != null && container.IsHeadNote(note))
            {
                GetCollectionForType(ObjectType.Note).LoadedContainers.TryGetValue(note, out var noteContainer);
                container.AttachedHead = noteContainer as NoteContainer;
                container.DetectHeadNote(false);
                break;
            }
        }
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
}
