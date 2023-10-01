using System.Diagnostics.CodeAnalysis;
using Beatmap.Base;
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
    private void EventPassedThreshold(bool _, int __, BaseObject newlyAdded) =>
        PluginLoader.BroadcastEvent<EventPassedThresholdAttribute, BaseObject>(newlyAdded);

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void NotePassedThreshold(bool _, int __, BaseObject newlyAdded) =>
        PluginLoader.BroadcastEvent<NotePassedThresholdAttribute, BaseObject>(newlyAdded);
}
