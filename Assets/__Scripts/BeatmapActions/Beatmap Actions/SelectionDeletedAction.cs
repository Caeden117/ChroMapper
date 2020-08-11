﻿using System.Linq;
using System.Collections.Generic;

public class SelectionDeletedAction : BeatmapAction
{
    public SelectionDeletedAction(IEnumerable<BeatmapObject> deletedData) : base(deletedData)
    {
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach(BeatmapObject data in Data.ToArray())
        {
            BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType)?.SpawnObject(data);
            SelectionController.Select(data, true, false);
        }
        SelectionController.RefreshSelectionMaterial(false);
        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.selection.Delete(false);
        RefreshPools(Data);
    }
}
