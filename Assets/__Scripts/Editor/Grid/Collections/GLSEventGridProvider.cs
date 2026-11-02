using System;
using System.Collections.Generic;
using System.Text;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

public class GLSEventGridProvider : MonoBehaviour
{
    public event Action<BaseEventBoxGroup> OnGroupChanged;
    public event Action OnAutomaticLaneDefaultsChanged;
    // Retiring an outer group must clear inner node ownership without sending null to handlers that require a valid group.
    public event Action OnGroupRetired;

    [SerializeField] private EditModeContext editMode;
    [SerializeField] private GridLane gridLane;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    [Header("Prefab")] [SerializeField] private TextMeshProUGUI labelPrefab;
    [SerializeField] private RectTransform targetCanvas;

    private readonly Stack<TextMeshProUGUI> reuseLabels = new();
    private readonly Stack<TextMeshProUGUI> usedLabels = new();

    // Cache one synthetic XYZ set per transform group type so refreshes allocate neither rotation nor translation lanes.
    private readonly Dictionary<Type, BaseEventBox[]> unusedLanesByGroupType = new();
    private readonly List<BaseEventBox> axisUnusedLanes = new();
    // Axis-scroll ordering is rebuilt only when the open group changes, keeping label, node, hover, and selection lookups allocation-free.
    private readonly List<BaseEventBox> axisLaneOrder = new();
    private readonly List<int> axisLaneAuthoredIndexes = new();
    private readonly List<int> authoredLaneDisplayIndexes = new();
    private readonly HashSet<(Axis Axis, int Element)> claimedLights = new();
    private readonly HashSet<BaseEventBox> lanesOwningLights = new();
    private BaseEventBoxGroup groupContext;

    public GridLane RibbonGridLane => gridLane;
    public int DisplayedLaneCount => axisLaneOrder.Count;

    public BaseEventBoxGroup GroupContext
    {
        get => groupContext;
        set
        {
            if (groupContext == value) return;
            groupContext = value;
            RefreshTrack();

            OnGroupChanged?.Invoke(groupContext);
        }
    }

    private bool markRemove;
    public BaseEventBoxGroup LastContext;

    private void Awake()
    {
        Settings.NotifyBySettingName(nameof(Settings.DefaultGLSAffectFirst), HandleAutomaticLaneDefaultChanged);
    }

    private void OnDestroy()
    {
        Settings.StopNotifyingBySettingName(nameof(Settings.DefaultGLSAffectFirst), HandleAutomaticLaneDefaultChanged);
    }

    private void HandleAutomaticLaneDefaultChanged(object _)
    {
        // Cached automatic lanes describe future boxes. Saved boxes keep their own configured value.
        foreach (var lanes in unusedLanesByGroupType.Values)
        {
            for (var axis = 0; axis < lanes.Length; axis++)
            {
                lanes[axis].ApplyDefaultAffectFirst();
            }
        }
        OnAutomaticLaneDefaultsChanged?.Invoke();
    }

    public void MarkRemove()
    {
        LastContext ??= groupContext;
        // Notify only context-retirement consumers so event-box UI handlers never receive a null group.
        if (LastContext != null)
        {
            groupContext = null;
            OnGroupRetired?.Invoke();
        }
        markRemove = true;
        enabled = true;
    }

    private void LateUpdate()
    {
        if (markRemove)
        {
            if (groupContext == null && editMode.EditingMode.HasFlag(EditingMode.EventBox))
                editMode.EditingMode = EditingMode.GLS;
            else
                RefreshTrack();

            LastContext = null;
            markRemove = false;
        }

        enabled = false;
    }

    private void RefreshTrack()
    {
        while (usedLabels.TryPop(out var usedLabel))
        {
            usedLabel.enabled = false;
            reuseLabels.Push(usedLabel);
        }

        axisUnusedLanes.Clear();
        axisLaneOrder.Clear();
        axisLaneAuthoredIndexes.Clear();
        authoredLaneDisplayIndexes.Clear();
        if (groupContext == null)
        {
            gridLane.Lane = 0;
            atsc.VisualBeatOrigin = 0;
            return;
        }

        var boxes = groupContext.ReadOnlyBoxes;
        AppendMissingAxisLanes(groupContext, axisUnusedLanes);
        RebuildAxisLaneOrder(boxes);
        RebuildLaneOwnership(boxes, GetGroupSize(groupContext));
        gridLane.Lane = DisplayedLaneCount;

        for (var i = 0; i < DisplayedLaneCount; i++)
        {
            if (!reuseLabels.TryPop(out var label)) label = Instantiate(labelPrefab, targetCanvas);
            usedLabels.Push(label);

            var pos = label.rectTransform.localPosition;
            pos.x = i;
            label.rectTransform.localPosition = pos;

            var box = axisLaneOrder[i];
            var filter = box.IndexFilter;

            var sb = new StringBuilder();

            int p0;
            int p1;
            if (filter.Type == (int)IndexFilterType.Division)
            {
                p0 = box.IndexFilter.Param0;
                p1 = box.IndexFilter.Param1 + 1;
            }
            else
            {
                p0 = box.IndexFilter.Param0 + 1;
                p1 = box.IndexFilter.Param1;
            }

            sb.AppendLine($"[{i + 1}]");
            sb.AppendLine();
            sb.AppendLine(DistributionTypeToString(box.BeatDistributionType));
            sb.AppendLine($"[{box.BeatDistribution}]");
            sb.AppendLine();
            sb.AppendLine(FilterTypeToString(filter.Type));
            sb.AppendLine($"[{p0},{p1}]");
            if (box is BaseLightTransformEventBox transformBox)
            {
                sb.AppendLine();
                sb.Append(((Axis)transformBox.Axis).ToString());
            }

            label.SetText(sb.ToString());

            var labelColor = labelPrefab.color;
            if (!box.IsAutomaticAxisLane && !lanesOwningLights.Contains(box))
            {
                labelColor = Color.red;
            }
            else
            {
                labelColor.a *= box.IsAutomaticAxisLane && box.ReadOnlyEvents.Count == 0 ? 0.5f : 1f;
            }
            label.color = labelColor;
            label.enabled = true;
        }
    }

    // Match playback's first-valid-box ownership order. Rotation and translation axes claim lights independently.
    private void RebuildLaneOwnership(IReadOnlyList<BaseEventBox> boxes, int groupSize)
    {
        claimedLights.Clear();
        lanesOwningLights.Clear();
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var box = boxes[boxIndex];
            if (box.IsAutomaticAxisLane && box.ReadOnlyEvents.Count == 0)
            {
                continue;
            }

            var filter = IndexFilterHelper.Convert(box.IndexFilter, groupSize);
            if (filter == null)
            {
                continue;
            }

            foreach (var (element, _, _) in filter)
            {
                if (claimedLights.Add((box.GetAxis(), element)))
                {
                    lanesOwningLights.Add(box);
                }
            }
        }
    }

    // Use the manager for this group type because color, rotation, and translation groups can have different physical light counts.
    private int GetGroupSize(BaseEventBoxGroup group)
        => group switch
        {
            BaseLightColorEventBoxGroup => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager.IdToEffect
                .TryGetValue(group.ID, out var colorEffect)
                ? colorEffect.Count
                : 0,
            BaseLightRotationEventBoxGroup => beatmapRuntimeContext.Descriptor.LightRotationGroupEffectManager.IdToEffect
                .TryGetValue(group.ID, out var rotationEffect)
                ? rotationEffect.Count
                : 0,
            BaseLightTranslationEventBoxGroup => beatmapRuntimeContext.Descriptor.LightTranslationGroupEffectManager.IdToEffect
                .TryGetValue(group.ID, out var translationEffect)
                ? translationEffect.Count
                : 0,
            BaseVfxEventEventBoxGroup => beatmapRuntimeContext.Descriptor.FloatFxGroupEffectManager.IdToEffect
                .TryGetValue(group.ID, out var floatFxEffect)
                ? floatFxEffect.Count
                : 0,
            _ => 0
        };

    public bool TryGetDisplayedBox(int laneIndex, out BaseEventBox box)
    {
        if (laneIndex < 0 || laneIndex >= DisplayedLaneCount)
        {
            box = null;
            return false;
        }

        box = axisLaneOrder[laneIndex];
        return true;
    }

    public int GetAuthoredBoxIndex(int displayedLaneIndex) =>
        displayedLaneIndex >= 0 && displayedLaneIndex < axisLaneAuthoredIndexes.Count
            ? axisLaneAuthoredIndexes[displayedLaneIndex]
            : -1;

    public int GetDisplayedLaneIndex(int authoredBoxIndex) =>
        authoredBoxIndex >= 0 && authoredBoxIndex < authoredLaneDisplayIndexes.Count
            ? authoredLaneDisplayIndexes[authoredBoxIndex]
            : authoredBoxIndex;

    private void RebuildAxisLaneOrder(IReadOnlyList<BaseEventBox> authoredBoxes)
    {
        for (var boxIndex = 0; boxIndex < authoredBoxes.Count; boxIndex++)
        {
            authoredLaneDisplayIndexes.Add(-1);
        }

        if (axisUnusedLanes.Count == 0)
        {
            for (var boxIndex = 0; boxIndex < authoredBoxes.Count; boxIndex++)
            {
                AddAuthoredLane(authoredBoxes[boxIndex], boxIndex);
            }
            return;
        }

        for (var axis = 0; axis < 3; axis++)
        {
            for (var boxIndex = 0; boxIndex < authoredBoxes.Count; boxIndex++)
            {
                if ((int)authoredBoxes[boxIndex].GetAxis() == axis)
                {
                    AddAuthoredLane(authoredBoxes[boxIndex], boxIndex);
                }
            }

            for (var unusedIndex = 0; unusedIndex < axisUnusedLanes.Count; unusedIndex++)
            {
                var unusedLane = axisUnusedLanes[unusedIndex];
                if ((int)unusedLane.GetAxis() == axis)
                {
                    axisLaneOrder.Add(unusedLane);
                    axisLaneAuthoredIndexes.Add(-1);
                }
            }
        }

        // Preserve malformed or future-axis authored data after the known XYZ lanes instead of making it inaccessible.
        for (var boxIndex = 0; boxIndex < authoredBoxes.Count; boxIndex++)
        {
            if (authoredLaneDisplayIndexes[boxIndex] < 0)
            {
                AddAuthoredLane(authoredBoxes[boxIndex], boxIndex);
            }
        }
    }

    // Record both directions once so all later display/ownership conversions are constant-time.
    private void AddAuthoredLane(BaseEventBox box, int authoredBoxIndex)
    {
        authoredLaneDisplayIndexes[authoredBoxIndex] = axisLaneOrder.Count;
        axisLaneOrder.Add(box);
        axisLaneAuthoredIndexes.Add(authoredBoxIndex);
    }

    private void AppendMissingAxisLanes(BaseEventBoxGroup group, ICollection<BaseEventBox> lanes)
    {
        if (group is not ILightTransformEventBoxGroup transformGroup)
        {
            return;
        }

        var type = group.GetType();
        if (!unusedLanesByGroupType.TryGetValue(type, out var unusedLanes))
        {
            unusedLanes = new BaseEventBox[3];
            for (var axis = 0; axis < unusedLanes.Length; axis++)
            {
                var box = transformGroup.CreateTransformBox(axis);
                box.IsAutomaticAxisLane = true;
                unusedLanes[axis] = box;
            }

            unusedLanesByGroupType.Add(type, unusedLanes);
        }

        foreach (var box in group.ReadOnlyBoxes)
        {
            if (box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset
                && box.IndexFilter.Param1 == 0)
            {
                return;
            }
        }

        var presentAxes = 0;
        foreach (var box in group.ReadOnlyBoxes)
        {
            var axis = (int)box.GetAxis();
            if (axis >= 0 && axis < unusedLanes.Length)
            {
                presentAxes |= 1 << axis;
            }
        }

        var enabledAxes = beatmapRuntimeContext.TrackDefinitions.Gls.TryGetValue(group.ID, out var trackDefinition)
            ? transformGroup.GetEnabledAxes(trackDefinition)
            : null;
        for (var axis = 0; axis < unusedLanes.Length; axis++)
        {
            if ((presentAxes & (1 << axis)) == 0 && (enabledAxes == null || enabledAxes[axis]))
            {
                lanes.Add(unusedLanes[axis]);
            }
        }
    }

    private string FilterTypeToString(int t)
    {
        return t switch
        {
            (int)IndexFilterType.Division => "Section",
            (int)IndexFilterType.StepAndOffset => "Step",
            _ => "???"
        };
    }

    private string DistributionTypeToString(int t)
    {
        return t switch
        {
            (int)DistributionType.Wave => "Wave",
            (int)DistributionType.Step => "Step",
            _ => "???"
        };
    }
}
