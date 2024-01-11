using UnityEngine;

public class AlternateLightingToggle : MonoBehaviour
{
    [Tooltip("This GameObject will be disabled if the AlternateLighting setting does not match this value.")]
    [SerializeField] private bool alternateLighting = false;

    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.AlternateLighting), OnAlternateLightingChanged);
        OnAlternateLightingChanged(Settings.Instance.AlternateLighting);
    }

    private void OnAlternateLightingChanged(object obj) => gameObject.SetActive(Settings.Instance.AlternateLighting == alternateLighting);

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.AlternateLighting));
    }
}
