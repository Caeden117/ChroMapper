using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Beatmap.Base;
using NetDataReader = LiteNetLib.Utils.NetDataReader;

/*
 * Alternative to ActionCollectionAction that removes all objects and then re adds them on undo/redo.
 * No modifying in-place should ensure map objects don't run into weird stacked/ghost issues. 
 */
public class BeatmapObjectModifiedCollectionAction : BeatmapAction
{
    private List<BaseObject> editedObjects;
    private List<BaseObject> originalObjects;
    
    private readonly float firstBpmEventJsonTime;

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedCollectionAction() : base() { }

    public BeatmapObjectModifiedCollectionAction(List<BaseObject> editedObjects, List<BaseObject> originalObjects,
        string comment = "No comment.") : base(editedObjects.Concat(originalObjects), comment)
    {
        this.editedObjects = editedObjects;
        this.originalObjects = originalObjects;

        firstBpmEventJsonTime = Data.OfType<BaseBpmEvent>().DefaultIfEmpty().Min(x => x?.JsonTime ?? -1f);
    }

    public override BaseObject DoesInvolveObject(BaseObject obj)
    {
        var involvedObject = editedObjects.Find(x => x == obj);
        involvedObject ??= originalObjects.Find(x => x == obj);
        
        return involvedObject;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in editedObjects)
        {
            DeleteObject(obj, false);
        }

        foreach (var obj in originalObjects)
        {
            SpawnObject(obj, false, false);
            
            if (!Networked)
            {
                SelectionController.Select(obj, true, true, false);
            }
        }

        if (firstBpmEventJsonTime >= 0)
        {
            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(firstBpmEventJsonTime);
        }
        
        RefreshPools(Data);
        RefreshEventAppearance();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in originalObjects)
        {
            DeleteObject(obj, false);
        }

        foreach (var obj in editedObjects)
        {
            SpawnObject(obj, false, false);
            
            if (!Networked)
            {
                SelectionController.Select(obj, true, true, false);
            }
        }

        if (firstBpmEventJsonTime >= 0)
        {
            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(firstBpmEventJsonTime);
        }
        
        RefreshPools(Data);
        RefreshEventAppearance();
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, editedObjects);
        SerializeBeatmapObjectList(writer, originalObjects);
    }

    public override void Deserialize(NetDataReader reader)
    {
        editedObjects = DeserializeBeatmapObjectList(reader).ToList();
        originalObjects = DeserializeBeatmapObjectList(reader).ToList();

        Data = editedObjects.Concat(originalObjects);
    }
}
