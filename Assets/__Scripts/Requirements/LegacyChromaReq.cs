using System.Linq;
using Beatmap.Base;

public class LegacyChromaReq : RequirementCheck
{
    public override string Name => "Chroma Lighting Events";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, IDifficulty map)
    {
        if (mapInfo is null) return RequirementType.None;
        return map?.Events?.Any(e => e.Value > ColourManager.RgbintOffset) == true
            ? RequirementType.Suggestion
            : RequirementType.None;
    }
}
