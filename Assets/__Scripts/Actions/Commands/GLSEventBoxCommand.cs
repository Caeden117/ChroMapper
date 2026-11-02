using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class GLSEventBoxCommand
{
    public static bool CanSplitSections(BaseEventBoxGroup group, BaseEventBox box)
    {
        return CanSplitSections(group, box, box?.IndexFilter.Param0 ?? 0);
    }

    public static bool CanSplitSections(BaseEventBoxGroup group, BaseEventBox box, int sectionCount)
    {
        var presentSections = GetPresentSectionIds(group, box, sectionCount);
        return presentSections != null && presentSections.Count < sectionCount;
    }

    public static BaseEventBoxGroup SplitSections(BaseEventBoxGroup group, int boxIndex)
    {
        var source = group.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        var presentSections = GetPresentSectionIds(group, source, source?.IndexFilter.Param0 ?? 0);
        if (presentSections == null || presentSections.Count == source.IndexFilter.Param0)
        {
            return group;
        }

        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup color:
                InsertMissingSections(color, boxIndex, presentSections);
                break;
            case BaseLightRotationEventBoxGroup rotation:
                InsertMissingSections(rotation, boxIndex, presentSections);
                break;
            case BaseLightTranslationEventBoxGroup translation:
                InsertMissingSections(translation, boxIndex, presentSections);
                break;
            case BaseVfxEventEventBoxGroup fx:
                InsertMissingSections(fx, boxIndex, presentSections);
                break;
        }

        GLSCommonCommand.RebindGroup(newGroup);
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.None);
    }

    private static HashSet<int> GetPresentSectionIds(BaseEventBoxGroup group, BaseEventBox source, int sectionCount)
    {
        if (source == null
            || source.IndexFilter.Type != (int)IndexFilterType.Division
            || sectionCount <= 1)
        {
            return null;
        }

        var axis = source.GetAxis();
        var presentSections = new HashSet<int>();
        // The selected box will use the draft count when the Sections field commits on deselection.
        if (source.IndexFilter.Param1 >= 0 && source.IndexFilter.Param1 < sectionCount)
        {
            presentSections.Add(source.IndexFilter.Param1);
        }
        foreach (var box in group.ReadOnlyBoxes)
        {
            var filter = box.IndexFilter;
            if (box != source
                && box.GetAxis() == axis
                && filter.Type == (int)IndexFilterType.Division
                && filter.Param0 == sectionCount
                && filter.Param1 >= 0
                && filter.Param1 < sectionCount)
            {
                presentSections.Add(filter.Param1);
            }
        }
        return presentSections;
    }

    private static void InsertMissingSections<TBox>(
        BaseEventBoxGroup<TBox> group,
        int sourceIndex,
        HashSet<int> presentSections)
        where TBox : BaseEventBox
    {
        var source = group.Boxes[sourceIndex];
        var splitBoxes = new List<TBox>(source.IndexFilter.Param0 - presentSections.Count);
        for (var section = 0; section < source.IndexFilter.Param0; section++)
        {
            if (presentSections.Contains(section))
            {
                continue;
            }

            var box = BeatmapFactory.Clone(source);
            box.IndexFilter.Param1 = section;
            box.IsAutomaticAxisLane = false;
            splitBoxes.Add(box);
        }
        group.Boxes.InsertRange(sourceIndex + 1, splitBoxes);
    }

    public static BaseEventBoxGroup AddEventBox(BaseEventBoxGroup group, int targetIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.Insert(targetIndex, new());
                break;
            case ILightTransformEventBoxGroup transformGroup:
                // Insert the new box after the selected lane and keep its axis.
                var selectedAxis = targetIndex > 0 && targetIndex <= transformGroup.TransformBoxes.Count
                    ? (int)transformGroup.TransformBoxes[targetIndex - 1].GetAxis()
                    : (int)Axis.X;
                transformGroup.InsertTransformBox(targetIndex, selectedAxis);
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
            case ILightTransformEventBoxGroup transformGroup:
                RebuildTransformIdLanes(newGroup, transformGroup, count, null);
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

        return GLSCommonCommand.TriggerPlaceAction(group, newGroup);
    }

    public static BaseEventBoxGroup AddAllAxesEventBox(BaseEventBoxGroup group, TrackDefinitionGLS td)
    {
        var newGroup = BeatmapFactory.Clone(group);
        if (newGroup is not ILightTransformEventBoxGroup transformGroup)
        {
            return null;
        }

        AddMissingAxes(newGroup, transformGroup, transformGroup.GetEnabledAxes(td));
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

    private static void RebuildTransformIdLanes(
        BaseEventBoxGroup groupData,
        ILightTransformEventBoxGroup group,
        int count,
        bool[] includedAxes)
    {
        if (count <= 0)
        {
            return;
        }

        const int axisCount = 3;
        var generatedAxes = new bool[axisCount];
        if (includedAxes != null)
        {
            var includedAxisCount = System.Math.Min(axisCount, includedAxes.Length);
            for (var axis = 0; axis < includedAxisCount; axis++)
            {
                generatedAxes[axis] = includedAxes[axis];
            }
        }

        var sourceEvents = new System.Collections.Generic.List<BaseGLSEvent>[axisCount];
        for (var axis = 0; axis < sourceEvents.Length; axis++)
        {
            sourceEvents[axis] = new System.Collections.Generic.List<BaseGLSEvent>();
        }

        var boxes = group.TransformBoxes;
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var box = boxes[boxIndex];
            if (box.Axis < 0 || box.Axis >= axisCount)
            {
                continue;
            }

            generatedAxes[box.Axis] = true;
            for (var eventIndex = 0; eventIndex < box.ReadOnlyEvents.Count; eventIndex++)
            {
                sourceEvents[box.Axis].Add(box.ReadOnlyEvents[eventIndex]);
            }
        }

        group.ClearTransformBoxes();
        for (var axis = 0; axis < generatedAxes.Length; axis++)
        {
            if (!generatedAxes[axis])
            {
                continue;
            }

            for (var id = 0; id < count; id++)
            {
                var targetBox = group.CreateTransformBox(axis);
                targetBox.IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.StepAndOffset,
                    Param0 = id,
                    Param1 = 0
                };
                var clonedEvents = new BaseGLSEvent[sourceEvents[axis].Count];
                for (var eventIndex = 0; eventIndex < sourceEvents[axis].Count; eventIndex++)
                {
                    clonedEvents[eventIndex] = BeatmapFactory.Clone(sourceEvents[axis][eventIndex]);
                }

                targetBox.SetEvents(clonedEvents);
                targetBox.IsAutomaticAxisLane = false;
                group.TryAddTransformBox(targetBox);
            }
        }

        GLSCommonCommand.RebindGroup(groupData);
    }

    private static void AddMissingAxes(
        BaseEventBoxGroup groupData,
        ILightTransformEventBoxGroup group,
        bool[] enabledAxes)
    {
        var boxes = group.TransformBoxes;
        var presentAxes = new bool[3];
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var box = boxes[boxIndex];
            if (box.Axis < 0 || box.Axis >= presentAxes.Length)
            {
                continue;
            }

            presentAxes[box.Axis] = true;
            if (box.Axis < enabledAxes.Length && enabledAxes[box.Axis])
            {
                box.IsAutomaticAxisLane = false;
            }
        }

        var supportedAxisCount = System.Math.Min(presentAxes.Length, enabledAxes.Length);
        for (var axis = 0; axis < supportedAxisCount; axis++)
        {
            if (enabledAxes[axis] && !presentAxes[axis])
            {
                group.TryAddTransformBox(group.CreateTransformBox(axis));
            }
        }

        group.SortAxisTracks();
        GLSCommonCommand.RebindGroup(groupData);
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
            // +Axes IDs uses each transform group's own enabled-track family while sharing partition and clone behavior.
            case ILightTransformEventBoxGroup transformGroup:
                RebuildTransformIdLanes(newGroup, transformGroup, count, transformGroup.GetEnabledAxes(td));
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
            case ILightTransformEventBoxGroup transformGroup:
                transformGroup.RemoveTransformBoxAt(targetIndex);
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
            case ILightTransformEventBoxGroup transformGroup:
                transformGroup.RemoveEmptyTransformBoxes();
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
            case ILightTransformEventBoxGroup transformGroup:
                transformGroup.SortTransformBoxesByIds();
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
        if (newGroup is not ILightTransformEventBoxGroup transformGroup)
        {
            return null;
        }

        transformGroup.SortAxesNumerically();
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
            case ILightTransformEventBoxGroup transformGroup:
                transformGroup.SwapTransformBoxes(originalIndex, targetIndex);
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
            case ILightTransformEventBoxGroup transformGroup:
                transformGroup.DuplicateTransformBox(boxIndex);
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.Insert(boxIndex + 1, BeatmapFactory.Clone(ffebg.Boxes[boxIndex]));
                ffebg.Boxes[boxIndex + 1].ClearEvents();
                break;
        }

        return GLSCommonCommand.TriggerPlaceAction(group, BeatmapFactory.Clone(newGroup));
    }

    public static BaseEventBoxGroup MaterializeAutomaticLane(
        BaseEventBoxGroup group,
        BaseEventBox displayBox)
    {
        var newGroup = CloneForBoxEdit(group, -1, displayBox, out var newBox);
        return newBox == null
            ? null
            : GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.None);
    }

    private static BaseEventBoxGroup CloneForBoxEdit(
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox,
        out BaseEventBox newBox)
    {
        var newGroup = BeatmapFactory.Clone(group);
        newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null && displayBox != null)
        {
            GLSCommonCommand.TryMaterializeAutomaticAxisLane(newGroup, displayBox, out newBox, out _);
            GLSCommonCommand.RebindGroup(newGroup);
        }

        if (newBox != null)
        {
            // A configured empty box must survive automatic-lane pruning and serialization.
            newBox.IsAutomaticAxisLane = false;
        }
        return newGroup;
    }

    public static BaseEventBoxGroup SetType(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.IndexFilter.Type == value)
            return null;

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

    public static BaseEventBoxGroup SetParam0(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null)
            return null;

        var newValue = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value : value - 1;
        if (newBox.IndexFilter.Param0 == newValue)
            return null;
        newBox.IndexFilter.Param0 = newValue;

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterParam0);
    }

    public static BaseEventBoxGroup SetParam1(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null)
            return null;

        var newValue = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value - 1 : value;
        if (newBox.IndexFilter.Param1 == newValue)
            return null;
        newBox.IndexFilter.Param1 = newValue;

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterParam1);
    }

    public static BaseEventBoxGroup SetReverse(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.IndexFilter.Reverse == value)
            return null;
        newBox.IndexFilter.Reverse = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterReverse);
    }

    public static BaseEventBoxGroup SetChunk(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.IndexFilter.Chunks == value)
            return null;
        newBox.IndexFilter.Chunks = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterChunk);
    }

    public static BaseEventBoxGroup SetRandom(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.IndexFilter.Random == value)
            return null;
        newBox.IndexFilter.Random = value;
        if (newBox.IndexFilter.Seed == 0) newBox.IndexFilter.Seed = -211754377;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterRandom);
    }

    public static BaseEventBoxGroup SetSeed(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.IndexFilter.Seed == value)
            return null;
        newBox.IndexFilter.Seed = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterSeed);
    }

    public static BaseEventBoxGroup SetLimit(
        float value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || Mathf.Approximately(newBox.IndexFilter.Limit, value))
            return group;
        newBox.IndexFilter.Limit = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxFilterLimit);
    }

    public static BaseEventBoxGroup SetLimitAffectsType(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.IndexFilter.LimitAffectsType == value)
            return null;
        newBox.IndexFilter.LimitAffectsType = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFilterLimitAffectsType);
    }

    public static BaseEventBoxGroup SetBeatDistributionType(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.BeatDistributionType == value)
            return null;
        newBox.BeatDistributionType = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxBeatDistributionType);
    }

    public static BaseEventBoxGroup SetBeatDistribution(
        float value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || Mathf.Approximately(newBox.BeatDistribution, value))
            return null;
        newBox.BeatDistribution = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxBeatDistribution);
    }

    public static BaseEventBoxGroup SetAxis(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var box);
        // Axis edits apply only to the common transform-box contract and retain the existing action boundary.
        if (box is not BaseLightTransformEventBox newBox
            || newBox.Axis == value)
        {
            return null;
        }

        newBox.Axis = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxAxis);
    }

    public static BaseEventBoxGroup SetFlip(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var box);
        if (box is not BaseLightTransformEventBox newBox
            || newBox.Flip == value)
        {
            return null;
        }

        newBox.Flip = value;
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxFlip);
    }

    public static BaseEventBoxGroup SetValueDistribution(
        float value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null)
            return null;
        float newValue;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                newValue = value / 100f;
                if (Mathf.Approximately(lceb.BrightnessDistribution, newValue))
                    return null;
                lceb.BrightnessDistribution = newValue;
                break;
            // Rotation retains raw degrees while translation retains its percentage display conversion through the shared scale.
            case BaseLightTransformEventBox transformBox:
                newValue = value / transformBox.ValueDistributionDisplayScale;
                if (Mathf.Approximately(transformBox.ValueDistribution, newValue))
                    return null;
                transformBox.ValueDistribution = newValue;
                break;
            case BaseVfxEventEventBox ffeb:
                newValue = value / 100f;
                if (Mathf.Approximately(ffeb.VfxDistribution, newValue))
                    return null;
                ffeb.VfxDistribution = newValue;
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxValueDistribution);
    }

    public static BaseEventBoxGroup SetValueDistributionType(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null)
            return null;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                if (lceb.BrightnessDistributionType == value)
                    return null;
                lceb.BrightnessDistributionType = value;
                break;
            case BaseLightTransformEventBox transformBox:
                if (transformBox.ValueDistributionType == value)
                    return null;
                transformBox.ValueDistributionType = value;
                break;
            case BaseVfxEventEventBox ffeb:
                if (ffeb.VfxDistributionType == value)
                    return null;
                ffeb.VfxDistributionType = value;
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            ActionMergeType.ModifyEventBoxValueDistributionType);
    }

    public static BaseEventBoxGroup SetAffectFirst(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null)
            return null;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                if (lceb.BrightnessAffectFirst == value)
                    return null;
                lceb.BrightnessAffectFirst = value;
                break;
            case BaseLightTransformEventBox transformBox:
                if (transformBox.AffectFirst == value)
                    return null;
                transformBox.AffectFirst = value;
                break;
            case BaseVfxEventEventBox ffeb:
                if (ffeb.VfxAffectFirst == value)
                    return null;
                ffeb.VfxAffectFirst = value;
                break;
        }

        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxAffectFirst);
    }

    public static BaseEventBoxGroup SetEasing(
        int value,
        BaseEventBoxGroup group,
        int boxIndex,
        BaseEventBox displayBox = null)
    {
        var newGroup = CloneForBoxEdit(group, boxIndex, displayBox, out var newBox);
        if (newBox == null || newBox.Easing == value)
            return null;
        newBox.Easing = value;

        return GLSCommonCommand.TriggerModifyEventBoxAction(group, newGroup, ActionMergeType.ModifyEventBoxEasing);
    }

    public static BaseEventBoxGroup SetColorDistributions(
        string[] value,
        bool strobe,
        BaseEventBoxGroup group,
        int boxIndex)
    {
        value ??= Array.Empty<string>();
        if (group.ReadOnlyBoxes.ElementAtOrDefault(boxIndex) is not BaseLightColorEventBox originalBox)
        {
            return null;
        }

        var existing = strobe
            ? originalBox.StrobeColorDistributions
            : originalBox.ColorDistributions;
        if (existing.SequenceEqual(value))
        {
            return null;
        }

        var newGroup = BeatmapFactory.Clone(group);
        var newBox = (BaseLightColorEventBox)newGroup.ReadOnlyBoxes.ElementAt(boxIndex);
        if (strobe)
        {
            newBox.StrobeColorDistributions = value;
        }
        else
        {
            newBox.ColorDistributions = value;
        }

        newBox.SaveCustom();
        return GLSCommonCommand.TriggerModifyEventBoxAction(
            group,
            newGroup,
            strobe
                ? ActionMergeType.ModifyEventBoxStrobeColorDistributions
                : ActionMergeType.ModifyEventBoxColorDistributions);
    }
}
