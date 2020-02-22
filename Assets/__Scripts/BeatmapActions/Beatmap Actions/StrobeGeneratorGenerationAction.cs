using System.Collections.Generic;
using System.Linq;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private List<BeatmapObject> conflictingData = new List<BeatmapObject>();
    private List<BeatmapObjectContainer> conflictingContainers = new List<BeatmapObjectContainer>();

    public StrobeGeneratorGenerationAction(List<BeatmapObjectContainer> generated, List<BeatmapObject> notGenerated) : base(generated)
    {
        conflictingData = notGenerated;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (BeatmapObjectContainer obj in containers)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType).DeleteObject(obj, false);
        }
        foreach (BeatmapObject obj in conflictingData)
        {
            conflictingContainers.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT)?.SpawnObject(BeatmapObject.GenerateCopy(obj), out _));
        }
        foreach (BeatmapObjectContainer obj in conflictingContainers) SelectionController.Select(obj, true, false);
        SelectionController.RefreshSelectionMaterial(false);
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (BeatmapObjectContainer obj in conflictingContainers) BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType).DeleteObject(obj, false);
        foreach (BeatmapObject obj in data)
        {
            containers.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT)?.SpawnObject(BeatmapObject.GenerateCopy(obj), out _));
        }
        foreach (BeatmapObjectContainer obj in containers) SelectionController.Select(obj, true, false);
        SelectionController.RefreshSelectionMaterial(false);
        param.tracksManager.RefreshTracks();
    }
}
