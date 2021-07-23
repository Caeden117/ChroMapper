using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.__Scripts.MapEditor.Hit_Sounds
{
    class HitSoundVolumeController : MonoBehaviour, CMInput.IAudioActions
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
    }
}
