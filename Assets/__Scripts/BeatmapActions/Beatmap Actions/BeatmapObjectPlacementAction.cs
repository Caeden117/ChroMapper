using System.Collections.Generic;
using System.Linq;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    internal List<BeatmapObjectContainer> removedConflictObjects = new List<BeatmapObjectContainer>();
    internal List<BeatmapObject> removedConflictObjectsData = new List<BeatmapObject>();

    public BeatmapObjectPlacementAction(IEnumerable<BeatmapObjectContainer> conflictingObjects, 
        IEnumerable<BeatmapObjectContainer> placedContainers) : base(placedContainers) {
        foreach (BeatmapObjectContainer con in conflictingObjects)
        {
            if (con is null) continue;
            removedConflictObjects.Add(con);
            removedConflictObjectsData.Add(con.objectData);
        }
    }

    public BeatmapObjectPlacementAction(BeatmapObjectContainer placedContainer,
       BeatmapObjectContainer conflictingObject) : base(new List<BeatmapObjectContainer>() { placedContainer })
    {
        removedConflictObjects.Add(conflictingObject);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in containers) param.collections.ForEach(x => x.DeleteObject(obj));
        removedConflictObjects.Clear();
        foreach (BeatmapObject data in removedConflictObjectsData)
            removedConflictObjects.Add(param.collections.Where(x => x.ContainerType == data.beatmapType).FirstOrDefault()?.SpawnObject(
                BeatmapObject.GenerateCopy(data), out _));
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in removedConflictObjects) param.collections.ForEach(x => x.DeleteObject(obj));
        containers.Clear();
        foreach (BeatmapObject con in data)
            containers.Add(param.collections.Where(x => x.ContainerType == con.beatmapType).FirstOrDefault()?.SpawnObject(
                BeatmapObject.GenerateCopy(con), out _));
    }
}
