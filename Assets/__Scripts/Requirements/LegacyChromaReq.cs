using System.Linq;

public class LegacyChromaReq : RequirementCheck
{
    public override string Name => "Chroma Lighting Events";
    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;
        return map?._events?.Any(e => e._value > ColourManager.RGB_INT_OFFSET) == true ? RequirementType.Suggestion : RequirementType.None;
    }
}