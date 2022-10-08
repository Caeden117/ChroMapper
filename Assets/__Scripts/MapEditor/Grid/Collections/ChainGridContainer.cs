using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
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

    public override ObjectContainer CreateContainer()
    {
        return ChainContainer.SpawnChain(null, ref chainPrefab);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal override void LateUpdate()
    {
        if (Settings.Instance.Load_MapV3)
        {
            base.LateUpdate();
        }
    }
    public void UpdateColor(Color red, Color blue) => chainAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(ObjectContainer con, IObject obj)
    {
        var chain = con as ChainContainer;
        var chainData = obj as IChain;
        chain.ChainData = chainData;
        chainAppearanceSO.SetChainAppearance(chain);
        chain.Setup();
        var track = tracksManager.GetTrackAtTime(chainData.Time);
        track.AttachContainer(con);
    }

    internal override void SubscribeToCallbacks() 
    {
        var notesContainer = GetCollectionForType(ObjectType.Note) as NoteGridContainer;
        notesContainer.ContainerSpawnedEvent += CheckUpdatedNote;

    }
    internal override void UnsubscribeToCallbacks() 
    {
        var notesContainer = GetCollectionForType(ObjectType.Note) as NoteGridContainer;
        if (notesContainer != null)
            notesContainer.ContainerSpawnedEvent -= CheckUpdatedNote;
    }

    protected override void OnContainerSpawn(ObjectContainer container, IObject obj)
    {
        (container as ChainContainer).DetectHeadNote();
    }

    protected override void OnContainerDespawn(ObjectContainer container, IObject obj)
    {
        (container as ChainContainer).ResetHeadNoteScale();
    }

    private void CheckUpdatedNote(IObject obj)
    {
        var note = obj as INote;
        if (note.Type == (int)NoteType.Bomb) return;
        var chains = GetBetween(note.Time - ViewEpsilon, note.Time + ViewEpsilon);
        foreach (IChain chain in chains)
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
}
