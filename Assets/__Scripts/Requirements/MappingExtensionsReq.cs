using System.Linq;

public class MappingExtensionsReq : RequirementCheck
{
    public override string Name => "Mapping Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;

        if (map is BeatSaberMapV3 mapv3)
        {
            if (mapv3.Arcs.Any(arc =>
                arc.X < 0 || arc.X > 3 || arc.Y < 0 || arc.Y > 2 ||
                arc.TailX < 0 || arc.TailX > 3 || arc.TailY < 0 || arc.TailY > 2) ||
                mapv3.Chains.Any(chain =>
                chain.X < 0 || chain.X > 3 || chain.Y < 0 || chain.Y > 2 ||
                chain.TailX < 0 || chain.TailX > 3 || chain.TailY < 0 || chain.TailY > 2))
                return RequirementType.Requirement;
        }

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
