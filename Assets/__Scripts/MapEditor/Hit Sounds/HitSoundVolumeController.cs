using UnityEngine;
using UnityEngine.InputSystem;

public class HitSoundVolumeController : MonoBehaviour, CMInput.IAudioActions
{
    [SerializeField] private float lastHitsoundVolume;
    [SerializeField] private float lastMetronomeVolume;

    private void OnEnable()
    {
        lastHitsoundVolume = Settings.Instance.NoteHitVolume;
        Settings.NotifyBySettingName("NoteHitVolume", UpdateLastVolume);

        lastMetronomeVolume = Settings.Instance.MetronomeVolume;
        Settings.NotifyBySettingName("MetronomeVolume", UpdateMetronomeVolume);
    }

    private void OnDestroy()
    {
        Settings.Instance.NoteHitVolume = lastHitsoundVolume;
        Settings.ClearSettingNotifications("NoteHitVolume");

        Settings.Instance.MetronomeVolume = lastMetronomeVolume;
        Settings.ClearSettingNotifications("MetronomeVolume");
    }

    public void OnToggleHitsoundMute(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        var currentVolume = Settings.Instance.NoteHitVolume;
        if (currentVolume == 0f)
        {
            Settings.Instance.NoteHitVolume = lastHitsoundVolume;
        }
        else
        {
            lastHitsoundVolume = currentVolume;
            Settings.Instance.NoteHitVolume = 0f;
        }
    }

    private void UpdateLastVolume(object obj) => lastHitsoundVolume = (float)obj;

    public void OnToggleMetronomeMute(InputAction.CallbackContext context)
    {

        if (!context.performed) return;

        var currentVolume = Settings.Instance.MetronomeVolume;
        if (currentVolume == 0f)
        {
            Settings.Instance.MetronomeVolume = lastMetronomeVolume;
        }
        else
        {
            lastMetronomeVolume = currentVolume;
            Settings.Instance.MetronomeVolume = 0f;
        }
    }

    private void UpdateMetronomeVolume(object obj) => lastMetronomeVolume = (float)obj;
}
