using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SongSpeedController : MonoBehaviour, CMInput.ISongSpeedActions
{
    public AudioSource source;
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

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("SongSpeed");
    }

    public void UpdateSongSpeed(object value)
    {
        float speedValue = (float)Convert.ChangeType(value, typeof(float));
        source.pitch = speedValue / 10;
        songSpeed = speedValue;
    }

    public void OnDecreaseSongSpeed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        songSpeed--;
        if (songSpeed < 1) songSpeed = 1;
        Settings.NonPersistentSettings["SongSpeed"] = songSpeed;
        UpdateSongSpeed(songSpeed);
    }

    public void OnIncreaseSongSpeed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        songSpeed++;
        if (songSpeed > 20) songSpeed = 20;
        Settings.NonPersistentSettings["SongSpeed"] = songSpeed;
        UpdateSongSpeed(songSpeed);
    }
}
