using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ChainsContainer"/> doesn't contain note(even the head note on the chain). 
/// It only detects whether there is a note happening to be a head note
/// </summary>
public class ChainsContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject chainPrefab;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private ChainAppearanceSO chainAppearanceSO;
    public const float ViewEpsilon = 0.1f; // original view is too small ?? sometimes cause error.
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Chain;

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapChainContainer.SpawnChain(null, ref chainPrefab);
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

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var chain = con as BeatmapChainContainer;
        var chainData = obj as BeatmapChain;
        chain.ChainData = chainData;
        chainAppearanceSO.SetChainAppearance(chain);
        chain.Setup();
        var track = tracksManager.GetTrackAtTime(chainData.Time);
        track.AttachContainer(con);
    }

    internal override void SubscribeToCallbacks() 
    {
        var notesContainer = GetCollectionForType(BeatmapObject.ObjectType.Note) as NotesContainer;
        notesContainer.ContainerSpawnedEvent += CheckUpdatedNote;

    }
    internal override void UnsubscribeToCallbacks() 
    {
        var notesContainer = GetCollectionForType(BeatmapObject.ObjectType.Note) as NotesContainer;
        if (notesContainer != null)
            notesContainer.ContainerSpawnedEvent -= CheckUpdatedNote;
    }

    protected override void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj)
    {
        (container as BeatmapChainContainer).DetectHeadNote();
    }

    protected override void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj)
    {
        (container as BeatmapChainContainer).ResetHeadNoteScale();
    }

    private void CheckUpdatedNote(BeatmapObject obj)
    {
        var note = obj as BeatmapNote;
        if (note.Type == BeatmapNote.NoteTypeBomb) return;
        var chains = GetBetween(note.Time - ViewEpsilon, note.Time + ViewEpsilon);
        foreach (BeatmapChain chain in chains)
        {
            LoadedContainers.TryGetValue(chain, out var con);
            var container = con as BeatmapChainContainer;
            if (container != null && container.IsHeadNote(note))
            {
                GetCollectionForType(BeatmapObject.ObjectType.Note).LoadedContainers.TryGetValue(note, out var noteContainer);
                container.AttachedHead = noteContainer as BeatmapNoteContainer;
                container.DetectHeadNote(false);
                break;
            }
        }
    }
}
