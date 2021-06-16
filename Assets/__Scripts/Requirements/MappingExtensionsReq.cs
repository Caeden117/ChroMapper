using System.Linq;

public class MappingExtensionsReq : RequirementCheck
{
    public override string Name => "Mapping Extensions";
    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;

        // idk why the customdata checks should be necessary, but they are.
        return map._notes.Any(note => (note._lineIndex < 0 || note._lineIndex > 3 || note._lineLayer < 0 || note._lineLayer > 2) && note._customData.Count <= 0) ||
               map._obstacles.Any(ob => (ob._lineIndex < 0 || ob._lineIndex > 3 || ob._type >= 2 || ob._width >= 1000) && ob._customData.Count <= 0) ||
               map._events.Any(ob => ob.IsRotationEvent && ob._value >= 1000 && ob._value <= 1720) ? RequirementType.Requirement : RequirementType.None;
    }
}