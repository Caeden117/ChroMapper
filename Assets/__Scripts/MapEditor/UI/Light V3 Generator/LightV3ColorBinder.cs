using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightV3ColorBinder : MetaLightV3Binder<BeatmapLightColorEvent>
{
    protected override void InitBindings()
    {
        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].BrightnessDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[0].AddedBeat.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[0].Color.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[0].Brightness.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[0].FlickerFrequency.ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].BrightnessDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[0].TransitionType);

        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Section" : "Start");
        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Partition" : "Step");
        TextsDumpFn.Add(x => $"1/{x.EventBoxes[0].EventDatas.Count}");

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);
    }
}
