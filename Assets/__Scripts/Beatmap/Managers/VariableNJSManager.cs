using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class VariableNJSManager : BeatmapObjectManager<BaseNJSEvent>
{
    [SerializeField] private VariableNJSProvider provider;

    protected override void Awake()
    {
        base.Awake();
        LoadInitialMap.OnLevelLoaded += Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += Refresh;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= Refresh;
        Context.Atsc.OnTimeChangedEarly -= UpdateTime;
    }

    public override void Refresh()
    {
        provider.Initialize();
        BeatSaberSongContainer.Instance.Map.NJSEvents.ForEach(provider.InsertData);
        // Publish the new movement values after its complete source index is ready, including unchanged NJS reloads.
        provider.Refresh();

        Context.Atsc.OnTimeChangedEarly += UpdateTime;
    }

    public override void UpdateTime() => UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    public override void UpdateTime(bool isPlaying, float beatTime) => provider.UpdateTime(isPlaying, beatTime);

    protected override bool AddData(IEnumerable<BaseNJSEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            provider.InsertData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseNJSEvent reference, BaseNJSEvent original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            provider.RemoveData(reference, original);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseNJSEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            provider.RemoveData(d, d);
            mark = true;
        }

        return mark;
    }

}
