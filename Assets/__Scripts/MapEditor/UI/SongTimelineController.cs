using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SongTimelineController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI timeMesh;
    [SerializeField] private TextMeshProUGUI currentBeatMesh;
    [SerializeField] private AudioSource mainAudioSource;
    public bool IsClicked;
    private float lastSongTime;

    private float songLength;

    private const string beatFormat = "F3";

    private const string timeFormat =
        "<mspace=0.4em>{3}{0:0}</mspace>:<mspace=0.4em>{1:00}</mspace><size=20>.<mspace=0.4em>{2:000}</mspace></size>";

    public static bool IsHovering { get; private set; }

    // Use this for initialization
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => mainAudioSource.clip != null);
        songLength = mainAudioSource.clip.length;
        slider.value = 0;
    }

    // Update is called once per frame
    private void Update()
    {
        if (atsc.CurrentSeconds == lastSongTime) return;
        if (!IsClicked)
        {
            lastSongTime = atsc.CurrentSeconds;
            slider.value = lastSongTime / songLength;
        }

        var seconds = Mathf.Abs(Mathf.FloorToInt(atsc.CurrentSeconds % 60));
        var rawMins = atsc.CurrentSeconds / 60;
        var minutes = Mathf.Abs(atsc.CurrentSeconds > 0 ? Mathf.FloorToInt(rawMins) : Mathf.CeilToInt(rawMins));
        var milliseconds = Mathf.FloorToInt((atsc.CurrentSeconds - Mathf.FloorToInt(atsc.CurrentSeconds)) * 1000);
        timeMesh.text = string.Format(timeFormat,
            minutes, seconds, milliseconds,
            atsc.CurrentSeconds < 0 ? "-" : "");

        currentBeatMesh.text = atsc.CurrentJsonTime.ToString(beatFormat);
    }

    public void OnPointerEnter(PointerEventData eventData) => IsHovering = true;

    public void OnPointerExit(PointerEventData eventData) => IsHovering = false;

    public void TriggerUpdate() => UpdateSongTimelineSlider(slider.value);

    public void UpdateSongTimelineSlider(float sliderValue)
    {
        if (atsc.IsPlaying || Input.GetAxis("Mouse ScrollWheel") != 0 || !IsClicked)
            return; //Don't modify ATSC if some other things are happening.

        if (NodeEditorController.IsActive)
        {
            slider.value = lastSongTime / songLength;
            return;
        }

        atsc.SnapToGrid(sliderValue * songLength);
    }
}
