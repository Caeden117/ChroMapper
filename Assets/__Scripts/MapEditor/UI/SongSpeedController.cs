using System;
using UnityEngine;

public class SongSpeedController : MonoBehaviour {

    public AudioSource source;

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
    }
}
