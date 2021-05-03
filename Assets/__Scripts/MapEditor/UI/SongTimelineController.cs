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

    private float songLength;
    private float lastSongTime;

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
        int milliseconds = Mathf.FloorToInt((atsc.CurrentSeconds - Mathf.FloorToInt(atsc.CurrentSeconds)) * 1000);
        timeMesh.text = string.Format("<mspace=0.4em>{3}{0:0}</mspace>:<mspace=0.4em>{1:00}</mspace><size=20>.<mspace=0.4em>{2:000}</mspace></size>", minutes, seconds, milliseconds,
            atsc.CurrentSeconds < 0 ? "-" : "");
	}

    public void TriggerUpdate()
    {
        UpdateSongTimelineSlider(slider.value);
    }

    public void UpdateSongTimelineSlider(float sliderValue)
    {
        if (atsc.IsPlaying || Input.GetAxis("Mouse ScrollWheel") != 0 || !IsClicked)
        {
            return; //Don't modify ATSC if some other things are happening.
        }
        else if (NodeEditorController.IsActive)
        {
            slider.value = lastSongTime / songLength;
            return;
        }
        atsc.SnapToGrid(sliderValue * songLength);
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
