using UnityEngine;

public class FPSListener : MonoBehaviour
{
    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.MaximumFPS), UpdateFPS);
        Settings.NotifyBySettingName(nameof(Settings.VSync), UpdateFPS);
    }

    private void UpdateFPS(object _)
    {
        QualitySettings.vSyncCount = Settings.Instance.VSync ? 1 : 0;
        Application.targetFrameRate = Settings.Instance.MaximumFPS;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.MaximumFPS));
        Settings.ClearSettingNotifications(nameof(Settings.VSync));
    }
}
