using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

/// <summary>
/// Manages GLS (Grouped Light System) event box groups in the renderer.
/// IMPORTANT: GLS event groups are cached in StateChunksContainer buckets based on their StartTime (SongBpmTime).
/// When an event group's time changes (e.g., when moved via cut/paste), the cached state must be properly updated
/// to reflect the new time, otherwise the renderer will continue showing lights at the old location.
/// 
/// CRITICAL: Any modification to an event group's JsonTime must go through the StateManager's RemoveData/InsertData
/// mechanism to ensure the state cache is properly updated. The StateChunksContainer uses bucket indices based on
/// StartTime for performance, so time changes require the state to be removed from the old bucket and re-inserted
/// into the new bucket.
/// </summary>
public class GLSManager : BeatmapObjectManager<BaseEventBoxGroup>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    [SerializeField] private LightshowController lightshowController;
    [SerializeField] private GLSEventGridProvider eventGridProvider;

    protected override void Awake()
    {
        base.Awake();
        Context.OnBeatmapTimingChanged += HandleBeatmapTimingChanged;
    }

    protected override void OnDestroy()
    {
        Context.OnBeatmapTimingChanged -= HandleBeatmapTimingChanged;
        base.OnDestroy();
    }

    private void HandleBeatmapTimingChanged(float jsonTime)
    {
        if (!AllowAction)
            return;

        var descriptor = Context.Descriptor;
        var map = BeatSaberSongContainer.Instance.Map;
        foreach (var effect in descriptor.LightColorGroupEffectManager.IdToEffect.Values)
            effect.RecomputeSongBpmTimes(jsonTime, map.LightColorEventBoxGroups.Count > 0);

        foreach (var effect in descriptor.LightRotationGroupEffectManager.IdToEffect.Values)
            effect.RecomputeSongBpmTimes(jsonTime, map.LightRotationEventBoxGroups.Count > 0);

        foreach (var effect in descriptor.LightTranslationGroupEffectManager.IdToEffect.Values)
            effect.RecomputeSongBpmTimes(jsonTime, map.LightTranslationEventBoxGroups.Count > 0);

        foreach (var effect in descriptor.FloatFxGroupEffectManager.IdToEffect.Values)
            effect.RecomputeSongBpmTimes(jsonTime, map.VfxEventBoxGroups.Count > 0);

        if (lightshowController.Mode == LightshowMode.None)
            UpdateTime(false, -1f);
        else
            UpdateTime();
    }

    public override void Refresh()
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null ? songContainer.Map : null;
        if (map == null)
            return;

        var descriptor = Context.Descriptor;
        descriptor.LightColorGroupEffectManager.Reinitialize();
        descriptor.LightRotationGroupEffectManager.Reinitialize();
        descriptor.LightTranslationGroupEffectManager.Reinitialize();
        descriptor.FloatFxGroupEffectManager.Reinitialize();

        foreach (var group in map.LightColorEventBoxGroups)
        {
            if (descriptor.LightColorGroupEffectManager.IdToEffect.TryGetValue(group.ID, out var colorEffect))
                colorEffect.InsertData(group);
        }

        foreach (var group in map.LightRotationEventBoxGroups)
        {
            if (descriptor.LightRotationGroupEffectManager.IdToEffect.TryGetValue(group.ID, out var rotationEffect))
                rotationEffect.InsertData(group);
        }

        foreach (var group in map.LightTranslationEventBoxGroups)
        {
            if (descriptor.LightTranslationGroupEffectManager.IdToEffect.TryGetValue(group.ID, out var translationEffect))
                translationEffect.InsertData(group);
        }

        foreach (var group in map.VfxEventBoxGroups)
        {
            if (descriptor.FloatFxGroupEffectManager.IdToEffect.TryGetValue(group.ID, out var floatFxEffect))
                floatFxEffect.InsertData(group);
        }

        descriptor.LightColorGroupEffectManager.Refresh();
        descriptor.LightRotationGroupEffectManager.Refresh();
        descriptor.LightTranslationGroupEffectManager.Refresh();
        descriptor.FloatFxGroupEffectManager.Refresh();
    }

    public override void UpdateTime()
    {
        if (lightshowController.Mode != LightshowMode.Full) return;
        UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        foreach (var effect in Context.Descriptor.LightColorGroupEffectManager.IdToEffect.Values)
        {
            effect.UpdateTime(isPlaying, time);
        }
        foreach (var effect in Context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Values)
        {
            effect.UpdateTime(isPlaying, time);
        }
        foreach (var effect in Context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Values)
        {
            effect.UpdateTime(isPlaying, time);
        }
        foreach (var effect in Context.Descriptor.FloatFxGroupEffectManager.IdToEffect.Values)
        {
            effect.UpdateTime(isPlaying, time);
        }
    }

    // TODO: probably do more generic on descriptor side
    protected override bool AddData(IEnumerable<BaseEventBoxGroup> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (eventGridProvider.LastContext != null && eventGridProvider.LastContext.IsConflictingWith(d))
                eventGridProvider.GroupContext = d;
            switch (d)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.InsertData(lcebg);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.InsertData(lrebg);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.InsertData(ltebg);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.InsertData(ffebg);
                    break;
            }
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseEventBoxGroup reference, BaseEventBoxGroup original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            if (eventGridProvider.GroupContext == reference) eventGridProvider.MarkRemove();

            switch (reference)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.RemoveData(
                        lcebg,
                        original as BaseLightColorEventBoxGroup);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.RemoveData(
                        lrebg,
                        original as BaseLightRotationEventBoxGroup);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.RemoveData(
                        ltebg,
                        original as BaseLightTranslationEventBoxGroup);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.RemoveData(
                        ffebg,
                        original as BaseVfxEventEventBoxGroup);
                    break;
            }
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEventBoxGroup> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (eventGridProvider.GroupContext == d) eventGridProvider.MarkRemove();
            switch (d)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.RemoveData(lcebg, lcebg);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.RemoveData(lrebg, lrebg);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.RemoveData(ltebg, ltebg);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.RemoveData(ffebg, ffebg);
                    break;
            }
        }

        return mark;
    }

}
