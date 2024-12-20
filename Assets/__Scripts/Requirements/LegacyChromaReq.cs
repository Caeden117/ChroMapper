using System.Linq;
using Beatmap.Base;
using Beatmap.Info;

public class LegacyChromaReq : RequirementCheck
{
    public override string Name => "Chroma Lighting Events";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        if (infoDifficulty is null) return RequirementType.None;
        return map?.Events?.Any(e => e.Value > ColourManager.RgbintOffset) == true
            ? RequirementType.Suggestion
            : RequirementType.None;
    }
}
