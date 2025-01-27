using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VivifyReq : RequirementCheck
{
    public override string Name => "Vivify";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map)
        => MapHasVivifyBundles() || MapHasVivifyEvents(map) ? RequirementType.Requirement : RequirementType.None;
    
    private bool MapHasVivifyEvents(BaseDifficulty map) =>
        map.CustomEvents.Any(ev => vivifyEventTypes.Contains(ev.Type));

    private bool MapHasVivifyBundles() =>
        BeatSaberSongContainer.Instance.Song.CustomData != null &&
        BeatSaberSongContainer.Instance.Song.CustomData.HasKey("_assetBundle");
    
    private static readonly string[] vivifyEventTypes = new[]
    {
        "SetMaterialProperty",
        "SetGlobalProperty",
        "Blit",
        "CreateCamera",
        "CreateScreenTexture",
        "InstantiatePrefab",
        "DestroyObject",
        "SetAnimatorProperty",
        "SetCameraProperty",
        "AssignObjectPrefab",
        "SetRenderSettings"
    };
}
