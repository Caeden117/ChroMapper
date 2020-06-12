using System.Collections.Generic;
using System.Linq;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> conflictingData;

    public StrobeGeneratorGenerationAction(IEnumerable<BeatmapObject> generated, IEnumerable<BeatmapObject> conflicting) : base(generated)
    {
        conflictingData = conflicting;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false);
        }
        foreach (BeatmapObject obj in conflictingData)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType)?.SpawnObject(obj);
            SelectionController.Select(obj, true, false);
        }
        SelectionController.RefreshSelectionMaterial(false);
        RefreshPools(conflictingData);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (BeatmapObject obj in conflictingData)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false);
        }
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).SpawnObject(obj);
            SelectionController.Select(obj, true, false);
        }
        SelectionController.RefreshSelectionMaterial(false);
        RefreshPools(Data);
    }
}
