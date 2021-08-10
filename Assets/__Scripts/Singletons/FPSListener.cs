using UnityEngine;

public class FPSListener : MonoBehaviour
{
    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.MaximumFPS), UpdateFPS);
    }

    private void UpdateFPS(object _)
    {
        Application.targetFrameRate = Settings.Instance.MaximumFPS;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.MaximumFPS));
    }
}
