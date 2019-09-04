using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;
using System;

public class AutoSaveController : MonoBehaviour {
    private float t = 0;
    public bool AutoSaveEnabled { get; private set; } = true;
    public float AutoSaveIntervalMinutes { get; private set; } = 5f;

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
            Save(true);
        }
	}

    public void Save(bool auto = false)
    {
        PersistentUI.Instance.DisplayMessage($"{(auto ? "Auto " : "")}Saving...", PersistentUI.DisplayMessageType.BOTTOM);
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            string original = BeatSaberSongContainer.Instance.map.directoryAndFile;
            if (auto) {
                List<string> directory = original.Split('/').ToList();
                directory.Insert(directory.Count - 1, $"autosave-{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}");
                string tempDirectory = string.Join("/", directory.ToArray());
                Debug.Log($"Auto saved to: {tempDirectory}");
                //We need to create the autosave directory before we can save the .dat difficulty into it.
                System.IO.Directory.CreateDirectory(string.Join("/", directory.Where(x => x != directory.Last()).ToArray()));
                BeatSaberSongContainer.Instance.map.directoryAndFile = tempDirectory;
            }
            BeatSaberSongContainer.Instance.map.Save();
            BeatSaberSongContainer.Instance.map.directoryAndFile = original;
        }).Start();
    }
}
