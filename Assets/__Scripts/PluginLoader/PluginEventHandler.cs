using UnityEngine;

class PluginEventHandler : MonoBehaviour
{

    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;

    private void Awake()
    {
        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
    }

    private void EventPassedThreshold(bool _, int __, BeatmapObject newlyAdded)
    {
        PluginLoader.BroadcastEvent<EventPassedThresholdAttribute, BeatmapObject>(newlyAdded);
    }

}
