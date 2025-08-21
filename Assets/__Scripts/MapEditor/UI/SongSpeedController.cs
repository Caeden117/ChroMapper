using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class SongSpeedController : MonoBehaviour, CMInput.ISongSpeedActions
{
    [FormerlySerializedAs("source")] public AudioSource Source;

    [SerializeField] private TextMeshProUGUI songDisplayText;
    private float songSpeed = 10;

    private void Start()
    {
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
        Settings.NonPersistentSettings["SongSpeed"] = 10;
    }

    private void Update()
    {
        if (songDisplayText.color.a <= 0) return;
        var alpha = songDisplayText.color.a - Time.deltaTime;
        songDisplayText.color = new Color(1, 1, 1, alpha);
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("SongSpeed");

    public void OnDecreaseSongSpeed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        songSpeed -= 0.5f;
        if (songSpeed < 0.5f) songSpeed = 0.5f;
        Settings.ManuallyNotifySettingUpdatedEvent("SongSpeed", songSpeed);
        UpdateSongSpeed(songSpeed);
    }

    public void OnIncreaseSongSpeed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        songSpeed += 0.5f;
        if (songSpeed > 30) songSpeed = 30;
        Settings.ManuallyNotifySettingUpdatedEvent("SongSpeed", songSpeed);
        UpdateSongSpeed(songSpeed);
    }

    public void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        Source.pitch = speedValue / 10;
        songSpeed = speedValue;

        songDisplayText.color = Color.white;
        songDisplayText.text = $"{songSpeed * 10}%";
    }
}
