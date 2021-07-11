using UnityEngine;
using UnityEngine.Analytics;

public class ForceDisableUnityAnalytics : MonoBehaviour
{
    private void Awake()
    {
        Analytics.enabled = false;
        Analytics.deviceStatsEnabled = false;
        PerformanceReporting.enabled = false;
    }
}
