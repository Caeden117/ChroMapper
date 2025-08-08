using System.Linq;
using Beatmap.Base;
using Beatmap.Info;

public class VivifyReq : RequirementCheck
{
    public override string Name => "Vivify";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        MapHasVivifyBundles(infoDifficulty) || MapHasVivifyEvents(map) 
            ? RequirementType.Requirement
            : RequirementType.None;
    
    private bool MapHasVivifyEvents(BaseDifficulty map) =>
        map.CustomEvents.Any(ev => vivifyEventTypes.Contains(ev.Type));

    private bool MapHasVivifyBundles(InfoDifficulty infoDifficulty) =>
        infoDifficulty.CustomData != null &&
        infoDifficulty.CustomData.HasKey("_assetBundle");
    
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
        "SetRenderingSettings"
    };
}
