using System;
using Beatmap.Animations;
using UnityEngine;

public class BeatmapRuntimeContext : MonoBehaviour
{
    public AudioTimeSyncController Atsc;
    public AudioLink.AudioLink AudioLink;
    public EnvironmentListSO EnvironmentList;

    [Header("Runtime")] public EnvironmentDescriptor Descriptor;
    public ColorSchemeSO ColorScheme;
    public TrackDefinitionsSO TrackDefinitions;

    public event Action OnEnvironmentUnloaded;
    public event Action<EnvironmentDescriptor> OnEnvironmentLoaded;
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
        if (Descriptor != null)
        {
            var listing = EnvironmentList.GetEnvironmentOrDefault(descriptor.ID);
            SetColorScheme(listing.ColorScheme);
            SetTrackDefinitions(listing.TrackDefinitions);
            Descriptor.Initialize(this);
            // TODO: also move this elsewhere
            if (BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData["_environmentRemoval"] != null)
            {
                var envRemoval = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData["_environmentRemoval"]
                    .AsArray;
                foreach (var marker in Descriptor.ChromaIDMarkers)
                {
                    foreach (var (_, id) in envRemoval)
                    {
                        if (!marker.ChromaID.Contains(id)) continue;
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

    public void SetColorScheme(ColorSchemeSO colorScheme)
    {
        ColorScheme.Copy(colorScheme);
        // TODO: make a class that handles no event class that require direct assignment
        PointDataParsers.ColorScheme = colorScheme;
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
