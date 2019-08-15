using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class AutoSaveController : MonoBehaviour {
    private float t = 0;
    public bool AutoSaveEnabled { get; private set; } = true;
    public float AutoSaveIntervalMinutes { get; private set; } = 5;

    public void ToggleAutoSave(bool enabled)
    {
        AutoSaveEnabled = enabled;
    }

    public void UpdateAutoSaveInterval(string value)
    {
        int interval = 0;
        if (int.TryParse(value, out interval) && interval > 0)
            AutoSaveIntervalMinutes = (float)interval;
    }

	// Use this for initialization
	void Start () {
        t = 0;
	}
	
	// Update is called once per frame
	void Update () {
        if (!AutoSaveEnabled) return;
        t += Time.deltaTime;
        if (t > (AutoSaveIntervalMinutes * 60))
        {
            t = 0;
            PersistentUI.Instance.DisplayMessage("Auto Saving...", PersistentUI.DisplayMessageType.BOTTOM);
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                BeatSaberSongContainer.Instance.map.Save();
                Debug.Log("Auto save!");
            }).Start();
        }
	}
}
