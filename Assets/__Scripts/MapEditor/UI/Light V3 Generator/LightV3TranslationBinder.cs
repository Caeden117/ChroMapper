using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightV3TranslationBinder : MetaLightV3Binder<BeatmapLightTranslationEvent>
{
    public int DataIdx = 0;
    [SerializeField] private LightTranslationEventPlacement lightTranslationEventPlacement;

    protected override void InitBindings()
    {
        ObjectData = new BeatmapLightTranslationEvent();

        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].TranslationDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat.ToString());
        InputDumpFn.Add(x => Mathf.RoundToInt(x.EventBoxes[0].EventDatas[DataIdx].TranslateValue * 100).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Chunk.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.RandomSeed.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Limit.ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].TranslationDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].Axis);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].EaseType + 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.RandomType);

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].TranslationAffectFirst == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].Flip == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].UsePrevious == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.TimeLimited);
        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.DataLimited);

        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Section" : "Step");
        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Partition" : "Start");
        TextsDumpFn.Add(x => $"{DataIdx + 1}/{x.EventBoxes[0].EventDatas.Count}");

        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Section = x.EventBoxes[0].Filter.FilterType == 1 ? int.Parse(s) - 1 : int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Partition = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Distribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].TranslationDistribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].TranslateValue = float.Parse(s) / 100.0f);
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Chunk = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.RandomSeed = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Limit = int.Parse(s));

        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.FilterType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].TranslationDistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Axis = i);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].EaseType = i - 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.RandomType = i);

        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.Reverse = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].TranslationAffectFirst = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Flip = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].EventDatas[DataIdx].UsePrevious = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.TimeLimited = b);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.DataLimited = b);
    }

    public override void Dump(BeatmapLightTranslationEvent obj)
    {
        var col = BeatmapObjectContainerCollection.GetCollectionForType<LightTranslationEventsContainer>(obj.BeatmapType);
        if (col.LoadedContainers.TryGetValue(obj, out var con))
        {
            var rotCon = con as BeatmapLightTranslationEventContainer;
            DataIdx = rotCon.GetRaycastedIdx();
        }
        else
        {
            DataIdx = 0;
        }
        base.Dump(obj);
    }

    public override void UpdateToPlacement()
    {
        lightTranslationEventPlacement.UpdateData(ObjectData);
    }
}
