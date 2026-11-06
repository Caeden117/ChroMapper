using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class LightColorStaticGroupEffect : LightColorGroupEffect
{
    public Color StaticColor;

    protected override void HandleBoostChange(bool boost)
    {
        // no op because boost dont exist here
    }

    protected override void UpdateObject(LightColorGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var start = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        var end = (LightColorEventStateData)(state.Next.UsePrevious ? start : state.Next);
        var darkHead = IsPreFirstEventSentinel(state);
        ConfigureTween(
            container.Tween,
            state,
            ResolveStaticColor(start),
            ResolveStaticColor(end),
            ResolveStaticStrobeColor(start),
            ResolveStaticStrobeColor(end),
            BeatSaberSongContainer.Instance.Map,
            darkHead);
    }

    private Color ResolveBaseColor(LightColorEventStateData state) =>
        state.Base.CustomColor
        ?? (state.Base.Color == (int)LightColor.White
            ? ColorSchemeProvider.ColorScheme.EnvironmentWhiteColor
            : StaticColor);

    private Color ResolveStaticColor(LightColorEventStateData state) =>
        GLSColorDistribution.ApplyNormal(
            ResolveBaseColor(state),
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);

    private Color ResolveStaticStrobeColor(LightColorEventStateData state) =>
        GLSColorDistribution.ApplyStrobe(
            ResolveBaseColor(state),
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);
}
