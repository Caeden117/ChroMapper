using System;
using System.Linq;
using Beatmap.Animations;
using SimpleJSON;
using UnityEngine;

public class BeatmapRuntimeContext : MonoBehaviour
{
    public AudioTimeSyncController Atsc;
    public AudioLink.AudioLink AudioLink;
    public EnvironmentListSO EnvironmentList;

    [Header("Runtime")] public EnvironmentDescriptor Descriptor;
    public ColorSchemeSO ColorScheme;
    public TrackDefinitionsSO TrackDefinitions;
    private const string LegacyEnvironmentRemovalKey = "_environmentRemoval";
    public bool HasLegacyEnvironmentRemoval { get; private set; }

    public event Action OnEnvironmentUnloaded;
    public event Action<EnvironmentDescriptor> OnEnvironmentLoaded;
    // Environment overrides arrive after OnEnvironmentLoaded. Publish their final values separately so
    // renderers update without repeating the full environment-load lifecycle.
    public event Action<BloomFogParams> OnBloomFogParamsChanged;
    public event Action<ColorSchemeSO> OnColorSchemeChanged;
    public event Action<TrackDefinitionsSO> OnTrackDefinitionsChanged;

    public void Start()
    {
        ColorScheme = ScriptableObject.CreateInstance<ColorSchemeSO>();
        TrackDefinitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
    }

    public void SetEnvironment(EnvironmentDescriptor descriptor)
    {
        Descriptor = descriptor;
        HasLegacyEnvironmentRemoval = false;
        if (Descriptor != null)
        {
            var listing = EnvironmentList.GetEnvironmentOrDefault(descriptor.ID);
            SetColorScheme(listing.ColorScheme);
            SetTrackDefinitions(listing.TrackDefinitions);
            Descriptor.Initialize(this);
            // Chroma's EnvironmentEnhancementManager permanently deactivates /Environment/GradientBackground
            // on every map load, and ChromaGLS mirrors that when the map declares it without requiring
            // Chroma, so any difficulty listing either mod never renders the prepass gradient in-game.
            // Maps without either mod keep it as the vanilla game renders it.
            if (MapDeclaresChromaFamily())
            {
                var gradientBackground = Descriptor.transform.Find("GradientBackground");
                if (gradientBackground != null)
                    gradientBackground.gameObject.SetActive(false);
            }
            var song = BeatSaberSongContainer.Instance;
            if (song.MapDifficultyInfo.CustomData.GetValueOrDefault(LegacyEnvironmentRemovalKey, null) is JSONArray removals)
            {
                HasLegacyEnvironmentRemoval = song.Map.MajorVersion == 2;
                foreach (var marker in Descriptor.ChromaIDMarkers)
                {
                    foreach (var removal in removals.Children)
                    {
                        var name = removal.Value;
                        var matches = HasLegacyEnvironmentRemoval
                            ? marker.name.Contains(name) && !(name == "TrackLaneRing" && marker.name.Contains("Big"))
                            : marker.ChromaID.Contains(name);
                        if (!matches)
                        {
                            continue;
                        }
                        marker.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        NotifyEnvironment();
    }

    public void NotifyEnvironment()
    {
        if (Descriptor != null)
            OnEnvironmentLoaded?.Invoke(Descriptor);
        else
            OnEnvironmentUnloaded?.Invoke();
    }

    internal static bool MapDeclaresBasicChroma() => MapDeclaresCapability(
        name =>
        name.Equals("Chroma", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Chroma Lighting Events", StringComparison.OrdinalIgnoreCase));

    private static bool MapDeclaresChromaFamily() => MapDeclaresCapability(
        name =>
        name.Equals("Chroma", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ChromaGLS", StringComparison.OrdinalIgnoreCase));

    private static bool MapDeclaresCapability(Func<string, bool> matches)
    {
        var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        return info.CustomRequirements.Any(matches) || info.CustomSuggestions.Any(matches);
    }

    /// <summary>Publishes fog overrides after enhancement loading or a custom-event sample.</summary>
    public void NotifyBloomFogParamsChanged()
    {
        if (Descriptor != null && Descriptor.BloomFogParams != null)
            OnBloomFogParamsChanged?.Invoke(Descriptor.BloomFogParams);
    }

    public void SetColorScheme(ColorSchemeSO colorScheme)
    {
        ColorScheme.Copy(colorScheme);
        // TODO: make a class that handles no event class that require direct assignment
        PointDataParsers.ColorScheme = ColorScheme;
        NotifyColorScheme();
    }

    public void NotifyColorScheme() => OnColorSchemeChanged?.Invoke(ColorScheme);

    public void SetTrackDefinitions(TrackDefinitionsSO trackDefinitions)
    {
        TrackDefinitions.Copy(trackDefinitions);
        // Share the active definition by reference so requirement checks can identify component-specific Basic Events.
        BeatSaberSongContainer.Instance.Map.RuntimeTrackDefinitions = TrackDefinitions;
        PaintSelectedObjects.TrackDefinitions = trackDefinitions;
        NotifyTrackDefinitions();
    }

    public void NotifyTrackDefinitions() => OnTrackDefinitionsChanged?.Invoke(TrackDefinitions);
}
