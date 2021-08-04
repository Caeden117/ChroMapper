using UnityEngine;
using UnityEngine.InputSystem;

public class HitSoundVolumeController : MonoBehaviour, CMInput.IAudioActions
{
    [SerializeField] private float lastVolume = 0f;

    public void OnToggleHitsoundMute(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        float currentVolume = Settings.Instance.NoteHitVolume;
        if (currentVolume == 0f)
        {
            Settings.Instance.NoteHitVolume = lastVolume;
        }
        else
        {
            lastVolume = currentVolume;
            Settings.Instance.NoteHitVolume = 0f;
        }
    }

    private void UpdateLastVolume(object obj)
    {
        lastVolume = (float)obj;
    }

    private void OnEnable()
    {
        lastVolume = Settings.Instance.NoteHitVolume;
        Settings.NotifyBySettingName("NoteHitVolume", UpdateLastVolume);
    }

    private void OnDestroy()
    {
        Settings.Instance.NoteHitVolume = lastVolume;
        Settings.ClearSettingNotifications("NoteHitVolume");
    }
}
