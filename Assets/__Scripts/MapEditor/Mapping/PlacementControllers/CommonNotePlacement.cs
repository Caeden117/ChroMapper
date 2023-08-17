using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class CommonNotePlacement
{
    public static void UpdateAttachedSlidersDirection(BaseNote noteData, ICollection<BeatmapAction> actions)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var arcData = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(arcData.JsonTime - noteData.JsonTime) < epsilon && arcData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(arcData.TailJsonTime - noteData.JsonTime) < epsilon && arcData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var arcOriginal = BeatmapFactory.Clone(arcData);
                arcData.CutDirection = noteData.CutDirection;
                (arcContainer.Value as ArcContainer).NotifySplineChanged();

                actions.Add(new BeatmapObjectModifiedAction(arcData, arcData, arcOriginal, keepSelection: true));
            }
            else if (isConnectedToTail)
            {
                var arcOriginal = BeatmapFactory.Clone(arcData);
                arcData.TailCutDirection = noteData.CutDirection;
                (arcContainer.Value as ArcContainer).NotifySplineChanged();

                actions.Add(new BeatmapObjectModifiedAction(arcData, arcData, arcOriginal, keepSelection: true));
            }
        }

        var chainCollection = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var chainData = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(chainData.JsonTime - noteData.JsonTime) < epsilon && chainData.GetPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var chainOriginal = BeatmapFactory.Clone(chainData);
                chainData.CutDirection = noteData.CutDirection;
                (chainContainer.Value as ChainContainer).GenerateChain();

                actions.Add(new BeatmapObjectModifiedAction(chainData, chainData, chainOriginal, keepSelection: true));
            }
        }
    }
}