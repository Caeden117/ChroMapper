using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongTimelineController : MonoBehaviour {

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI timeMesh;
    [SerializeField] private AudioSource mainAudioSource;

    private float songLength = 0;
    private float lastSongTime = 0;

	// Use this for initialization
	IEnumerator Start () {
        yield return new WaitUntil(() => mainAudioSource.clip != null);
        songLength = mainAudioSource.clip.length;
	}
	
	// Update is called once per frame
	private void Update () {
        if (atsc.CurrentSeconds == lastSongTime) return;
        lastSongTime = atsc.CurrentSeconds;
        slider.value = lastSongTime / songLength;
        int seconds = Mathf.FloorToInt(atsc.CurrentSeconds % 60);
        int minutes = Mathf.FloorToInt(atsc.CurrentSeconds / 60);
        int milliseconds = Mathf.FloorToInt((atsc.CurrentSeconds - Mathf.FloorToInt(atsc.CurrentSeconds)) * 100);
        timeMesh.text = string.Format("{0:0}:{1:00}<size=20>.{2:00}</size>", minutes, seconds, milliseconds);
	}

    public void UpdateSongTimelineSlider(float sliderValue)
    {
        if (atsc.IsPlaying) return; //Don't modify ATSC if it's already playing.
        atsc.MoveToTimeInSeconds(sliderValue * songLength);
        atsc.SnapToGrid();
    }
}
