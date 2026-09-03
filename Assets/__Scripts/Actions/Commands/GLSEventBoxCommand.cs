using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class GLSEventBoxCommand
{
    public static BaseEventBoxGroup AddEventBox(BaseEventBoxGroup group, int targetIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.Insert(targetIndex, new());
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.Insert(targetIndex, new());
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.Insert(targetIndex, new());
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.Insert(targetIndex, new());
                break;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup AddAllIdsEventBox(BaseEventBoxGroup group, int count)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                RebuildIdLanes(
                    lcebg,
                    count,
                    1,
                    null,
                    static _ => 0,
                    static _ => new BaseLightColorEventBox());
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                RebuildIdLanes(
                    lrebg,
                    count,
                    3,
                    null,
                    static box => box.Axis,
                    static axis => new BaseLightRotationEventBox { Axis = axis });
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                RebuildIdLanes(
                    ltebg,
                    count,
                    3,
                    null,
                    static box => box.Axis,
                    static axis => new BaseLightTranslationEventBox { Axis = axis });
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                RebuildIdLanes(
                    ffebg,
                    count,
                    1,
                    null,
                    static _ => 0,
                    static _ => new BaseVfxEventEventBox());
                break;
        }

        // RebuildIdLanes already produced an independently cloned group; cloning the expanded ID lanes again doubled peak allocations.
        return GLSCommonCommand.TriggerPlaceAction(group, newGroup);
    }

    public static BaseEventBoxGroup AddAllAxesEventBox(BaseEventBoxGroup group, TrackDefinitionGLS td)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup:
                return null;
            case BaseLightRotationEventBoxGroup lrebg:
                // AddAxesPreservesMultipleExistingRotationAxisNodes adds only absent enabled axes and authors existing auto lanes.
                AddMissingAxes(
                    lrebg,
                    td.RotationTracks,
                    static box => box.Axis,
                    static axis => new BaseLightRotationEventBox { Axis = axis });
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                // AddAxesPreservesTranslationNodeAndUsesTranslationTracks retains nodes and reads the correct track family.
                AddMissingAxes(
                    ltebg,
                    td.TranslationTracks,
                    static box => box.Axis,
                    static axis => new BaseLightTranslationEventBox { Axis = axis });
                break;
            case BaseVfxEventEventBoxGroup:
                return null;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, newGroup);
    }

    // +Ids snapshots nodes by axis, removes all non-ID source boxes, and independently clones each snapshot into every ID lane.
    private static void RebuildIdLanes<TBox>(
        BaseEventBoxGroup<TBox> group,
        int count,
        int partitionCount,
        bool[] includedPartitions,
        System.Func<TBox, int> getPartition,
        System.Func<int, TBox> createBox)
        where TBox : BaseEventBox
    {
        if (count <= 0)
        {
            return;
        }

        var boxes = group.Boxes;
        var generatedPartitions = new bool[partitionCount];
        if (includedPartitions != null)
        {
            var includedPartitionCount = System.Math.Min(generatedPartitions.Length, includedPartitions.Length);
            for (var partition = 0; partition < includedPartitionCount; partition++)
            {
                generatedPartitions[partition] = includedPartitions[partition];
            }
        }

        var sourceEvents = new System.Collections.Generic.List<BaseGLSEvent>[generatedPartitions.Length];
        for (var partition = 0; partition < sourceEvents.Length; partition++)
        {
            sourceEvents[partition] = new System.Collections.Generic.List<BaseGLSEvent>();
        }

        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var box = boxes[boxIndex];
            var partition = getPartition(box);
            if (partition < 0 || partition >= generatedPartitions.Length)
            {
                continue;
            }

            // Existing partitions join generated layouts even when track metadata is stale, preventing node loss during conversion.
            generatedPartitions[partition] = true;
            for (var eventIndex = 0; eventIndex < box.ReadOnlyEvents.Count; eventIndex++)
            {
                sourceEvents[partition].Add(box.ReadOnlyEvents[eventIndex]);
            }
        }

        boxes.Clear();
        for (var partition = 0; partition < generatedPartitions.Length; partition++)
        {
            if (!generatedPartitions[partition])
            {
                continue;
            }

            for (var id = 0; id < count; id++)
            {
                var targetBox = createBox(partition);
                targetBox.IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.StepAndOffset,
                    Param0 = id,
                    Param1 = 0
                };
                var clonedEvents = new BaseGLSEvent[sourceEvents[partition].Count];
                for (var eventIndex = 0; eventIndex < sourceEvents[partition].Count; eventIndex++)
                {
                    clonedEvents[eventIndex] = BeatmapFactory.Clone(sourceEvents[partition][eventIndex]);
                }

                targetBox.SetEvents(clonedEvents);
                targetBox.IsAutomaticAxisLane = false;
                boxes.Add(targetBox);
            }
        }

        GLSCommonCommand.RebindGroup(group);
    }

    // Add Axes is additive: preserve every authored filter lane and create one permanent lane only for a missing enabled axis.
    private static void AddMissingAxes<TBox>(
        BaseEventBoxGroup<TBox> group,
        bool[] enabledAxes,
        System.Func<TBox, int> getAxis,
        System.Func<int, TBox> createBox)
        where TBox : BaseEventBox
    {
        var boxes = group.Boxes;
        var presentAxes = new bool[3];
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var axis = getAxis(boxes[boxIndex]);
            if (axis < 0 || axis >= presentAxes.Length)
            {
                continue;
            }

            presentAxes[axis] = true;
            if (axis < enabledAxes.Length && enabledAxes[axis])
            {
                boxes[boxIndex].IsAutomaticAxisLane = false;
            }
        }

        var supportedAxisCount = System.Math.Min(presentAxes.Length, enabledAxes.Length);
        for (var axis = 0; axis < supportedAxisCount; axis++)
        {
            if (enabledAxes[axis] && !presentAxes[axis])
            {
                boxes.Add(createBox(axis));
            }
        }

        GLSCommonCommand.SortAxisTracks(boxes, getAxis);
        GLSCommonCommand.RebindGroup(group);
    }

    public static BaseEventBoxGroup AddAllAxesAndIdsEventBox(BaseEventBoxGroup group, TrackDefinitionGLS td, int count)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                // Non-axis +Axes IDs calls retain the same node-copy behavior if invoked outside the disabled UI action.
                RebuildIdLanes(
                    lcebg,
                    count,
                    1,
                    null,
                    static _ => 0,
                    static _ => new BaseLightColorEventBox());
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                // AddAxesAndIdsCopiesNodesAndCreatesEmptyIdLanesForMissingAxes includes enabled empty rotation axes too.
                RebuildIdLanes(
                    lrebg,
                    count,
                    3,
                    td.RotationTracks,
                    static box => box.Axis,
                    static axis => new BaseLightRotationEventBox { Axis = axis });
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                // Translation +Axes IDs reads translation availability and clones each source axis into every generated lane.
                RebuildIdLanes(
                    ltebg,
                    count,
                    3,
                    td.TranslationTracks,
                    static box => box.Axis,
                    static axis => new BaseLightTranslationEventBox { Axis = axis });
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                // FloatFX follows the same single-partition fallback when +Axes IDs is invoked programmatically.
                RebuildIdLanes(
                    ffebg,
                    count,
                    1,
                    null,
                    static _ => 0,
                    static _ => new BaseVfxEventEventBox());
                break;
        }

        // RebuildIdLanes already produced an independently cloned group; avoid deep-copying every expanded axis/ID event a second time.
        return GLSCommonCommand.TriggerPlaceAction(group, newGroup);
    }

    public static BaseEventBoxGroup DeleteEventBox(BaseEventBoxGroup group, int targetIndex)
    {
        if (targetIndex < 0 || group.ReadOnlyBoxes.Count <= targetIndex) return null;
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.RemoveAt(targetIndex);
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.RemoveAt(targetIndex);
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.RemoveAt(targetIndex);
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.RemoveAt(targetIndex);
                break;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup DeletePruneEventBox(BaseEventBoxGroup group)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes = lcebg.Boxes.Where(x => x.Events.Length != 0).ToList();
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes = lrebg.Boxes.Where(x => x.Events.Length != 0).ToList();
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes = ltebg.Boxes.Where(x => x.Events.Length != 0).ToList();
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes = ffebg.Boxes.Where(x => x.Events.Length != 0).ToList();
                break;
        }

        return group.ReadOnlyBoxes.Count == newGroup.ReadOnlyBoxes.Count
            ? group
            : GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup SortIdsEventBox(BaseEventBoxGroup group)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes = lcebg
                    .Boxes
                    .OrderByDescending(eventBox =>
                        eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                            ? eventBox.IndexFilter.Param0
                            : eventBox.IndexFilter.Param1)
                    .ThenBy(eventBox => eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                        ? eventBox.IndexFilter.Param1
                        : eventBox.IndexFilter.Param0)
                    .ToList();
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes = lrebg
                    .Boxes
                    .OrderByDescending(eventBox =>
                        eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                            ? eventBox.IndexFilter.Param0
                            : eventBox.IndexFilter.Param1)
                    .ThenBy(eventBox => eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                        ? eventBox.IndexFilter.Param1
                        : eventBox.IndexFilter.Param0)
                    .ToList();
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes = ltebg
                    .Boxes
                    .OrderByDescending(eventBox =>
                        eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                            ? eventBox.IndexFilter.Param0
                            : eventBox.IndexFilter.Param1)
                    .ThenBy(eventBox => eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                        ? eventBox.IndexFilter.Param1
                        : eventBox.IndexFilter.Param0)
                    .ToList();
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes = ffebg
                    .Boxes
                    .OrderByDescending(eventBox =>
                        eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                            ? eventBox.IndexFilter.Param0
                            : eventBox.IndexFilter.Param1)
                    .ThenBy(eventBox => eventBox.IndexFilter.Type == (int)IndexFilterType.Division
                        ? eventBox.IndexFilter.Param1
                        : eventBox.IndexFilter.Param0)
                    .ToList();
                break;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup SortAxesEventBox(BaseEventBoxGroup group)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup:
                return null;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes = lrebg
                    .Boxes
                    .OrderBy(eventBox => eventBox.Axis)
                    .ToList();
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes = ltebg
                    .Boxes
                    .OrderBy(eventBox => eventBox.Axis)
                    .ToList();
                break;
            case BaseVfxEventEventBoxGroup:
                return null;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup MoveDownEventBox(BaseEventBoxGroup group, int boxIndex) =>
        ReorderEventBox(group, boxIndex, boxIndex + 1);

    public static BaseEventBoxGroup MoveUpEventBox(BaseEventBoxGroup group, int boxIndex) =>
        ReorderEventBox(group, boxIndex, boxIndex - 1);

    public static BaseEventBoxGroup ReorderEventBox(BaseEventBoxGroup group, int originalIndex, int targetIndex)
    {
        if (originalIndex == targetIndex
            || originalIndex < 0
            || originalIndex >= group.ReadOnlyBoxes.Count
            || targetIndex < 0
            || targetIndex >= group.ReadOnlyBoxes.Count)
            return null;

        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                (lcebg.Boxes[originalIndex], lcebg.Boxes[targetIndex]) =
                    (lcebg.Boxes[targetIndex], lcebg.Boxes[originalIndex]);
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                (lrebg.Boxes[originalIndex], lrebg.Boxes[targetIndex]) =
                    (lrebg.Boxes[targetIndex], lrebg.Boxes[originalIndex]);
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                (ltebg.Boxes[originalIndex], ltebg.Boxes[targetIndex]) =
                    (ltebg.Boxes[targetIndex], ltebg.Boxes[originalIndex]);
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                (ffebg.Boxes[originalIndex], ffebg.Boxes[targetIndex]) =
                    (ffebg.Boxes[targetIndex], ffebg.Boxes[originalIndex]);
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            BeatmapFactory.Clone(newGroup),
            ActionMergeType.ReorderEventBox);
    }

    public static BaseEventBoxGroup DuplicateEventBox(BaseEventBoxGroup group, int boxIndex)
    {
        if (boxIndex < 0 || boxIndex >= group.ReadOnlyBoxes.Count) return null;

        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.Insert(boxIndex + 1, BeatmapFactory.Clone(lcebg.Boxes[boxIndex]));
                lcebg.Boxes[boxIndex + 1].ClearEvents();
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.Insert(boxIndex + 1, BeatmapFactory.Clone(lrebg.Boxes[boxIndex]));
                lrebg.Boxes[boxIndex + 1].ClearEvents();
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.Insert(boxIndex + 1, BeatmapFactory.Clone(ltebg.Boxes[boxIndex]));
                ltebg.Boxes[boxIndex + 1].ClearEvents();
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.Insert(boxIndex + 1, BeatmapFactory.Clone(ffebg.Boxes[boxIndex]));
                ffebg.Boxes[boxIndex + 1].ClearEvents();
                break;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup SetType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Type == value) return null;

        switch (value)
        {
            case (int)IndexFilterType.Division:
                newBox.IndexFilter.Param0 = 1;
                newBox.IndexFilter.Param1 = 0;
                break;
            case (int)IndexFilterType.StepAndOffset:
                newBox.IndexFilter.Param0 = 0;
                newBox.IndexFilter.Param1 = 1;
                break;
        }

        newBox.IndexFilter.Type = value;

        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterType);
    }

    public static BaseEventBoxGroup SetParam0(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;

        var newValue = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value : value - 1;
        if (newBox.IndexFilter.Param0 == newValue) return null;
        newBox.IndexFilter.Param0 = newValue;

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterParam0);
    }

    public static BaseEventBoxGroup SetParam1(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;

        var newValue = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value - 1 : value;
        if (newBox.IndexFilter.Param1 == newValue) return null;
        newBox.IndexFilter.Param1 = newValue;

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterParam1);
    }

    public static BaseEventBoxGroup SetReverse(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Reverse == value) return null;
        newBox.IndexFilter.Reverse = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterReverse);
    }

    public static BaseEventBoxGroup SetChunk(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Chunks == value) return null;
        newBox.IndexFilter.Chunks = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterChunk);
    }

    public static BaseEventBoxGroup SetRandom(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Random == value) return null;
        newBox.IndexFilter.Random = value;
        if (newBox.IndexFilter.Seed == 0) newBox.IndexFilter.Seed = -211754377;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterRandom);
    }

    public static BaseEventBoxGroup SetSeed(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Seed == value) return null;
        newBox.IndexFilter.Seed = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterSeed);
    }

    public static BaseEventBoxGroup SetLimit(float value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || Mathf.Approximately(newBox.IndexFilter.Limit, value)) return group;
        newBox.IndexFilter.Limit = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterLimit);
    }

    public static BaseEventBoxGroup SetLimitAffectsType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.LimitAffectsType == value) return null;
        newBox.IndexFilter.LimitAffectsType = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterLimitAffectsType);
    }

    public static BaseEventBoxGroup SetBeatDistributionType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.BeatDistributionType == value) return null;
        newBox.BeatDistributionType = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxBeatDistributionType);
    }

    public static BaseEventBoxGroup SetBeatDistribution(float value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || Mathf.Approximately(newBox.BeatDistribution, value)) return null;
        newBox.BeatDistribution = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxBeatDistribution);
    }

    public static BaseEventBoxGroup SetAxis(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;
        switch (newBox)
        {
            case BaseLightRotationEventBox lreb:
                if (lreb.Axis == value) return null;
                lreb.Axis = value;
                return GLSCommonCommand.TriggerModifyEventBoxAction(
                    group,
                    newGroup,
                    ActionMergeType.ModifyEventBoxAxis);
            case BaseLightTranslationEventBox lteb:
                if (lteb.Axis == value) return null;
                lteb.Axis = value;
                return GLSCommonCommand.TriggerModifyEventBoxAction(
                    group,
                    newGroup,
                    ActionMergeType.ModifyEventBoxAxis);
        }

        return null;
    }

    public static BaseEventBoxGroup SetFlip(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;
        switch (newBox)
        {
            case BaseLightRotationEventBox lreb:
                if (lreb.Flip == value) return null;
                lreb.Flip = value;
                return GLSCommonCommand.TriggerModifyEventBoxAction(
                    group,
                    newGroup,
                    ActionMergeType.ModifyEventBoxFlip);
            case BaseLightTranslationEventBox lteb:
                if (lteb.Flip == value) return null;
                lteb.Flip = value;
                return GLSCommonCommand.TriggerModifyEventBoxAction(
                    group,
                    newGroup,
                    ActionMergeType.ModifyEventBoxFlip);
        }

        return null;
    }

    public static BaseEventBoxGroup SetValueDistribution(float value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;
        float newValue;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                newValue = value / 100f;
                if (Mathf.Approximately(lceb.BrightnessDistribution, newValue)) return null;
                lceb.BrightnessDistribution = newValue;
                break;
            case BaseLightRotationEventBox lreb:
                newValue = value;
                if (Mathf.Approximately(lreb.RotationDistribution, newValue)) return null;
                lreb.RotationDistribution = newValue;
                break;
            case BaseLightTranslationEventBox lteb:
                newValue = value / 100f;
                if (Mathf.Approximately(lteb.TranslationDistribution, newValue)) return null;
                lteb.TranslationDistribution = newValue;
                break;
            case BaseVfxEventEventBox ffeb:
                newValue = value / 100f;
                if (Mathf.Approximately(ffeb.VfxDistribution, newValue)) return null;
                ffeb.VfxDistribution = newValue;
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxValueDistribution);
    }

    public static BaseEventBoxGroup SetValueDistributionType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                if (lceb.BrightnessDistributionType == value) return null;
                lceb.BrightnessDistributionType = value;
                break;
            case BaseLightRotationEventBox lreb:
                if (lreb.RotationDistributionType == value) return null;
                lreb.RotationDistributionType = value;
                break;
            case BaseLightTranslationEventBox lteb:
                if (lteb.TranslationDistributionType == value) return null;
                lteb.TranslationDistributionType = value;
                break;
            case BaseVfxEventEventBox ffeb:
                if (ffeb.VfxDistributionType == value) return null;
                ffeb.VfxDistributionType = value;
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxValueDistributionType);
    }

    public static BaseEventBoxGroup SetAffectFirst(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return null;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                if (lceb.BrightnessAffectFirst == value) return null;
                lceb.BrightnessAffectFirst = value;
                break;
            case BaseLightRotationEventBox lreb:
                if (lreb.RotationAffectFirst == value) return null;
                lreb.RotationAffectFirst = value;
                break;
            case BaseLightTranslationEventBox lteb:
                if (lteb.TranslationAffectFirst == value) return null;
                lteb.TranslationAffectFirst = value;
                break;
            case BaseVfxEventEventBox ffeb:
                if (ffeb.VfxAffectFirst == value) return null;
                ffeb.VfxAffectFirst = value;
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxAffectFirst);
    }

    public static BaseEventBoxGroup SetEasing(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.Easing == value) return null;
        newBox.Easing = value;

        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxEasing);
    }
}
