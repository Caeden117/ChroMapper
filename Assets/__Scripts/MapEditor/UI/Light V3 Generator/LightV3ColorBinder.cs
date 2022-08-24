using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightV3ColorBinder : MetaLightV3Binder<BeatmapLightColorEvent>
{
    public int DataIdx = 0;
    protected override void InitBindings()
    {
        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].BrightnessDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Color.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Brightness.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency.ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].BrightnessDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].TransitionType);

        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Section" : "Start");
        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Partition" : "Step");
        TextsDumpFn.Add(x => $"{DataIdx + 1}/{x.EventBoxes[0].EventDatas.Count}");

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);

        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Section = x.EventBoxes[0].Filter.FilterType == 1 ? int.Parse(s) - 1 : int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Partition = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Distribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].BrightnessDistribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Color = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Brightness = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency = int.Parse(s));

        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.FilterType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].BrightnessDistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].TransitionType = i);

        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.Reverse = b ? 1 : 0);
    }

    protected override void Dump(BeatmapLightColorEvent obj)
    {
        var col = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(obj.BeatmapType);
        if (!col.LoadedContainers.TryGetValue(obj, out var con))
        {
            Debug.LogError("cannot find container");
        }
        var colorCon = con as BeatmapLightColorEventContainer;
        DataIdx = colorCon.GetRaycastedIdx();
        base.Dump(obj);
    }
}
