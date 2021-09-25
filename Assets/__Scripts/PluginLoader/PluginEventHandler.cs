using System.Diagnostics.CodeAnalysis;
using UnityEngine;

internal class PluginEventHandler : MonoBehaviour
{
    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;

    private void Awake()
    {
        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
        interfaceCallback.NotePassedThreshold += NotePassedThreshold;
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
        interfaceCallback.NotePassedThreshold -= NotePassedThreshold;
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void EventPassedThreshold(bool _, int __, BeatmapObject newlyAdded) =>
        PluginLoader.BroadcastEvent<EventPassedThresholdAttribute, BeatmapObject>(newlyAdded);

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void NotePassedThreshold(bool _, int __, BeatmapObject newlyAdded) =>
        PluginLoader.BroadcastEvent<NotePassedThresholdAttribute, BeatmapObject>(newlyAdded);
}
