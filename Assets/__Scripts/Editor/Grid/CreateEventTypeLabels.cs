using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using TMPro;
using UnityEngine;

public class CreateEventTypeLabels : MonoBehaviour
{
    public GameObject LabelPrefab;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private Transform target;

    // The event grid supplies its map-scoped filter index before requesting a label refresh.
    public BasicEventNameFilterIndex NameFilterIndex { private get; set; }

    private readonly List<(int id, int type, string nameFilter)> laneObjs = new();

    private Dictionary<int, BasicLightEffect> typeToManager = new();

    // Use this for initialization
    private void Start() => context.OnEnvironmentLoaded += HandleEnvironmentLoaded;
    private void OnDestroy() => context.OnEnvironmentLoaded -= HandleEnvironmentLoaded;

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        // Build the effect lookup without allocating a LINQ iterator and intermediate dictionary.
        typeToManager.Clear();
        foreach (var (type, effect) in descriptor.BasicEventEffectManager.GetEffects<BasicLightEffect>())
            typeToManager[type] = effect;
    }

    /// <summary>
    ///     Rebuilds visible lane labels from track definitions and the event grid's incremental filter index.
    /// </summary>
    /// <remarks>
    ///     Propagation-off refreshes are now <c>O(D + L)</c> instead of scanning <c>E</c> basic events first, so adding
    ///     or deleting one named ring event on a large map only renders the visible definitions and filter lanes.
    /// </remarks>
    public void UpdateLabels(EventGridContainer.PropMode propMode, int eventType, int lanes)
    {
        foreach (Transform children in target)
        {
            if (children.gameObject.activeSelf) Destroy(children.gameObject);
        }

        laneObjs.Clear();

        if (propMode == EventGridContainer.PropMode.Off)
        {
            var lane = 0;
            // The event grid owns the incremental filter index, so label refreshes only enumerate visible definitions.
            AddBasicLabels(BasicEventKind.Lights, true, ref lane);
            AddBasicLabels(BasicEventKind.Lights, false, ref lane);
        }
        else
        {
            for (var i = 0; i < lanes; i++)
            {
                var instantiate = Instantiate(LabelPrefab, target);
                var laneInfo = (i, i, (string)null);
                instantiate.SetActive(true);
                instantiate.transform.localPosition =
                    new Vector3(i, 0, 0);
                laneObjs.Add(laneInfo);

                try
                {
                    var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                    textMesh.text = i == 0
                        ? "All Lights"
                        : $"{context.TrackDefinitions.GetBasicOrDefault(eventType).Name} ID {LaneToLightID(eventType, i - 1)}";
                }
                catch { }
            }
        }
    }

    private void AddBasicLabels(
        BasicEventKind selectedKind,
        bool matchingKind,
        ref int lane)
    {
        // SkrillexBasicEventLanesUseEnvironmentPresentationOrder relies on generated track-definition order so the
        // runtime label builder remains environment-agnostic and newly generated assets reproduce the same lanes.
        foreach (var entry in context.TrackDefinitions.Basic)
        {
            var definition = entry.Value;
            if ((definition.Kind == selectedKind) != matchingKind)
                continue;

            AddBasicLabel(definition, ref lane);
        }
    }

    private void AddBasicLabel(TrackDefinitionBasic definition, ref int lane)
    {
        AddLabel(lane++, definition.Type, null, definition.Name);
        // Name filters remain adjacent to their reordered base lane so event-to-lane mappings retain their identities.
        if (!definition.Components.HasFlag(BasicEventComponent.RingRotation)
            || NameFilterIndex == null
            || !NameFilterIndex.TryGetFilters(definition.Type, out var filters))
        {
            return;
        }

        foreach (var filter in filters.Keys)
        {
            AddLabel(lane++, definition.Type, filter, filter);
        }
    }

    private void AddLabel(int lane, int eventType, string nameFilter, string label)
    {
        var instantiate = Instantiate(LabelPrefab, target);
        instantiate.SetActive(true);
        instantiate.transform.localPosition = new Vector3(lane, 0, 0);
        laneObjs.Add((lane, eventType, nameFilter));

        try
        {
            var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
            textMesh.text = label;
        }
        catch { }
    }

    public int LaneIdToEventType(int laneId)
    {
        if (laneId < 0 || laneId >= laneObjs.Count) return -1;
        return laneObjs[laneId].type;
    }

    public int EventToLaneId(BaseEvent data)
    {
        foreach (var (id, type, nameFilter) in laneObjs)
        {
            if (type != data.Type) continue;
            if (nameFilter == data.CustomNameFilter) return id;
        }

        return EventTypeToLaneId(data.Type);
    }

    public int EventTypeToLaneId(int eventType)
    {
        foreach (var (id, type, _) in laneObjs)
        {
            if (type != eventType) continue;
            return id;
        }

        return -1;
    }

    // Expose the visible basic-event lane mirror so the mirror command can move ordinary light events between displayed lanes.
    public int MirroredEventType(BaseEvent data)
    {
        // Mirror only among distinct visible light lanes without allocating a LINQ pipeline.
        var lightTypes = new List<int>();
        var seenTypes = new HashSet<int>();
        foreach (var entry in laneObjs)
        {
            if (context.TrackDefinitions.GetBasicOrDefault(entry.type).Kind != BasicEventKind.Lights
                || !seenTypes.Add(entry.type))
                continue;
            lightTypes.Add(entry.type);
        }

        var index = lightTypes.IndexOf(data.Type);
        return index >= 0 ? lightTypes[lightTypes.Count - 1 - index] : data.Type;
    }

    public int? LightIdsToPropId(int type, int[] lightID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return null;

        var id = manager.LaneToLightIDs.FindIndex(x => Array.Exists(
            x,
            y => Array.Exists(lightID, z => z == y)));

        return id != -1 ? id : null;
    }

    public int[] PropIdToLightIds(int type, int propID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return Array.Empty<int>();

        return 0 <= propID && propID < manager.LaneToLightIDs.Count
            ? manager.LaneToLightIDs[propID]
            : Array.Empty<int>();
    }

    public JSONArray PropIdToLightIdsJ(int type, int propID)
    {
        var result = new JSONArray();
        foreach (var id in PropIdToLightIds(type, propID)) result.Add(id);
        return result;
    }

    public int LaneToLightID(int type, int lightID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return -1;
        return lightID >= 0 && lightID < manager.LaneToLightID.Count ? manager.LaneToLightID[lightID] : -1;
    }

    public int LightIDToLane(int type, int lightID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return -1;
        return manager.LightIDToLane.GetValueOrDefault(lightID, -1);
    }

    // Resolve multi-ID events through displayed physical lanes so hidden IDs cannot become an anchor.
    public int LightIDsToVisibleLane(int type, IEnumerable<int> lightIDs)
    {
        if (lightIDs == null) return -1;
        // Resolve the minimum visible lane in one pass without iterator allocations.
        var minimum = int.MaxValue;
        foreach (var lightID in lightIDs)
        {
            var lane = LightIDToLane(type, lightID);
            if (lane >= 0 && lane < minimum) minimum = lane;
        }

        return minimum == int.MaxValue ? -1 : minimum;
    }

    public int LightIDsToPropID(int type, int[] lightIDs)
    {
        if (!typeToManager.TryGetValue(type, out var manager) || lightIDs == null) return -1;
        foreach (var lightID in lightIDs)
        {
            for (var index = 0; index < manager.LaneToLightIDs.Count; index++)
            {
                var id = manager.LaneToLightIDs[index];
                // Match a physical light ID without relying on LINQ's array extension.
                if (!Array.Exists(id, candidate => candidate == lightID)) continue;
                return index;
            }
        }

        return -1;
    }

    public int MaxLaneId() => laneObjs.Count - 1;

    public int LaneCount => laneObjs.Count;
}
