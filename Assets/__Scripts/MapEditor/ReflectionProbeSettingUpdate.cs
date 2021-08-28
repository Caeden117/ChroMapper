using UnityEngine;

public class ReflectionProbeSettingUpdate : MonoBehaviour
{
    [SerializeField] private ReflectionProbe probe;

    private void Start()
    {
        Settings.NotifyBySettingName("Reflections", UpdateReflectionSetting);
        UpdateReflectionSetting(Settings.Instance.Reflections);
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("Reflections");

    private void UpdateReflectionSetting(object obj) => probe.enabled = (bool)obj;
}
