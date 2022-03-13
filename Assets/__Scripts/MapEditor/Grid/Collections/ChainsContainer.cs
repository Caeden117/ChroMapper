using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainsContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject chainPrefab;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private ChainAppearanceSO chainAppearanceSO;
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

    public void UpdateColor(Color red, Color blue) => chainAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var chain = con as BeatmapChainContainer;
        var chainData = obj as BeatmapChain;
        chain.ChainData = chainData;
        chainAppearanceSO.SetChainAppearance(chain);
        chain.Setup();
        var track = tracksManager.GetTrackAtTime(chainData.B);
        track.AttachContainer(con);
    }

    internal override void SubscribeToCallbacks() { }
    internal override void UnsubscribeToCallbacks() { }
}
