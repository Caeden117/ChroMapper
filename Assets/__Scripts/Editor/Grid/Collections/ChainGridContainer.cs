using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// <see cref="ChainGridContainer"/> doesn't contain note(even the head note on the chain).
/// It only detects whether there is a note happening to be a head note
/// </summary>
public class ChainGridContainer : BeatmapObjectContainerCollection<BaseChain>
{
    [SerializeField] private GameObject chainPrefab;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private ChainAppearanceSO chainAppearanceSO;

    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.Chain;

    private bool isPlaying;

    public override ObjectContainer CreateContainer()
    {
        var con = ChainContainer.SpawnChain(null, ref chainPrefab);
        con.Animator.Context = BeatmapContext;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    public void UpdateColor(Color red, Color blue) => chainAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var chain = con as ChainContainer;
        var chainData = obj as BaseChain;
        chain.ChainData = chainData;
        chainAppearanceSO.SetChainAppearance(chain);
        chain.Setup();
        chain.SetIndicators(!isPlaying);

        if (!chain.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(chainData.SongBpmTime, chainData.Rotation);
            track.AttachContainer(con);
        }

        if (UIMode.AnimationMode && obj.CustomTrack != null && chain.Animator.enabled)
        {
            tracksManager.PushHeldValuesToChild(obj.CustomTrack, chain.Animator);
            chain.Animator.LateUpdate();
        }
    }

    protected override void HandleObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Chains);

    protected override void HandleObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Chains);

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.OnChainPassedThreshold += SpawnCallback;
        SpawnCallbackController.OnRecursiveChainCheckFinished += OnRecursiveCheckFinished;
        DespawnCallbackController.OnChainPassedThreshold += DespawnCallback;
        BeatmapContext.Atsc.OnPlayToggled += OnPlayToggle;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;

        Settings.NotifyBySettingName(nameof(Settings.NoteColorMultiplier), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.ArrowColorMultiplier), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.ArrowColorWhiteBlend), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.AccurateNoteSize), AppearanceChanged);
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.OnChainPassedThreshold -= SpawnCallback;
        SpawnCallbackController.OnRecursiveChainCheckFinished -= OnRecursiveCheckFinished;
        DespawnCallbackController.OnChainPassedThreshold -= DespawnCallback;
        BeatmapContext.Atsc.OnPlayToggled -= OnPlayToggle;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;

        Settings.ClearSettingNotifications(nameof(Settings.NoteColorMultiplier));
        Settings.ClearSettingNotifications(nameof(Settings.ArrowColorMultiplier));
        Settings.ClearSettingNotifications(nameof(Settings.ArrowColorWhiteBlend));
        Settings.ClearSettingNotifications(nameof(Settings.AccurateNoteSize));
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
        this.isPlaying = isPlaying;

        foreach (ChainContainer obj in LoadedContainers.Values)
        {
            obj.SetIndicators(!this.isPlaying);
        }
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    private void OnRecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    private void AppearanceChanged(object _) => RefreshPool(true);

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

    // TODO: not my proudest
    public IEnumerable<BaseChain> GetBetweenTail(float jsonTime, float jsonTime2) =>
        MapObjects.Where(x => jsonTime < x.TailJsonTime && x.TailJsonTime < jsonTime2);
}
