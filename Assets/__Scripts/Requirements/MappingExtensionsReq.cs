using System.Linq;

public class MappingExtensionsReq : RequirementCheck
{
    public override string Name => "Mapping Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;

        // idk why the customdata checks should be necessary, but they are.
        return map.Notes.Any(note =>
                   (note.LineIndex < 0 || note.LineIndex > 3 || note.LineLayer < 0 || note.LineLayer > 2) &&
                   note.CustomData.Count <= 0) ||
               map.Obstacles.Any(ob =>
                   (!(ob is BeatmapObstacleV3) && (ob.LineIndex < 0 || ob.LineIndex > 3)
                   || (ob is BeatmapObstacleV3) && (ob.LineIndex <= -1000 || ob.LineIndex >= 1000)
                   || ob.Type >= 2 || ob.Width >= 1000) &&
                   ob.CustomData.Count <= 0) ||
               map.Events.Any(ob => ob.IsRotationEvent && !(ob is RotationEvent) // V3 rotation
                    && ob.Value >= 1000 && ob.Value <= 1720)
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}
