using UnityEngine;
using UnityEngine.Analytics;

public class ForceDisableUnityAnalytics : MonoBehaviour
{
    private void Awake()
    {
        Analytics.initializeOnStartup = false;
        Analytics.enabled = false;
        Analytics.deviceStatsEnabled = false;
        PerformanceReporting.enabled = false;
    }
}
