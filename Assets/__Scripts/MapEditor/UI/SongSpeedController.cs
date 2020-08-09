using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SongSpeedController : MonoBehaviour, CMInput.ISongSpeedActions
{
    public AudioSource source;

    [SerializeField] private TextMeshProUGUI songDisplayText;
    private float songSpeed = 10;

    private void Start()
    {
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
        if (Settings.NonPersistentSettings.ContainsKey("SongSpeed"))
        {
            Settings.NonPersistentSettings["SongSpeed"] = 10;
        }
        else
        {
            Settings.NonPersistentSettings.Add("SongSpeed", 10);
        }
    }

    private void Update()
    {
        if (songDisplayText.color.a <= 0) return;
        float alpha = songDisplayText.color.a - Time.deltaTime;
        songDisplayText.color = new Color(1, 1, 1, alpha);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("SongSpeed");
    }

    public void UpdateSongSpeed(object value)
    {
        float speedValue = (float)Convert.ChangeType(value, typeof(float));
        source.pitch = speedValue / 10;
        songSpeed = speedValue;

        songDisplayText.color = Color.white;
        songDisplayText.text = $"{songSpeed * 10}%";
    }

    public void OnDecreaseSongSpeed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        songSpeed--;
        if (songSpeed < 1) songSpeed = 1;
        Settings.ManuallyNotifySettingUpdatedEvent("SongSpeed", songSpeed);
        UpdateSongSpeed(songSpeed);
    }

    public void OnIncreaseSongSpeed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        songSpeed++;
        if (songSpeed > 20) songSpeed = 20;
        Settings.ManuallyNotifySettingUpdatedEvent("SongSpeed", songSpeed);
        UpdateSongSpeed(songSpeed);
    }
}
