using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongTimelineController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI timeMesh;
    [SerializeField] private AudioSource mainAudioSource;

    private float songLength = 0;
    private float lastSongTime = 0;

    public static bool IsHovering { get; private set; } = false;
    public bool IsClicked = false;

	// Use this for initialization
	IEnumerator Start () {
        yield return new WaitUntil(() => mainAudioSource.clip != null);
        songLength = mainAudioSource.clip.length;
        slider.value = 0;
	}
	
	// Update is called once per frame
	private void Update () {
        if (atsc.CurrentSeconds == lastSongTime) return;
        if (!IsClicked)
        {
            lastSongTime = atsc.CurrentSeconds;
            slider.value = lastSongTime / songLength;
        }
        int seconds = Mathf.Abs(Mathf.FloorToInt(atsc.CurrentSeconds % 60));
        float rawMins = atsc.CurrentSeconds / 60;
        int minutes = Mathf.Abs(atsc.CurrentSeconds > 0 ? Mathf.FloorToInt(rawMins) : Mathf.CeilToInt(rawMins));
        int milliseconds = Mathf.FloorToInt((atsc.CurrentSeconds - Mathf.FloorToInt(atsc.CurrentSeconds)) * 100);
        timeMesh.text = string.Format("{3}{0:0}:{1:00}<size=20>.{2:00}</size>", minutes, seconds, milliseconds,
            atsc.CurrentSeconds < 0 ? "-" : "");
	}

    public void UpdateSongTimelineSlider(float sliderValue)
    {
        if (atsc.IsPlaying || Input.GetAxis("Mouse ScrollWheel") != 0 || NodeEditorController.IsActive || !IsClicked)
            return; //Don't modify ATSC if some other things are happening.
        atsc.MoveToTimeInSeconds(sliderValue * songLength);
        atsc.SnapToGrid();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHovering = false;
    }
}
