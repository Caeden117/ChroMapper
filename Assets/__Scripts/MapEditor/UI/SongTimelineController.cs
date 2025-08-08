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

    private string timeMeshFloatFormat = "F3";

    public static bool IsHovering { get; private set; }

    // Use this for initialization
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => mainAudioSource.clip != null);
        songLength = mainAudioSource.clip.length;
        slider.value = 0;

        timeMeshFloatFormat = $"F{Settings.Instance.TimeValueDecimalPrecision}";
        Settings.NotifyBySettingName(nameof(Settings.TimeValueDecimalPrecision), UpdateTimeMeshFloatFormat);
    }

    private void OnDestroy() => Settings.ClearSettingNotifications(nameof(Settings.TimeValueDecimalPrecision));

    private void UpdateTimeMeshFloatFormat(object value)
    {
        timeMeshFloatFormat = $"F{value}";
        currentBeatMesh.text = atsc.CurrentJsonTime.ToString(timeMeshFloatFormat);
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
        timeMesh.text = string.Format(
            "<mspace=0.4em>{3}{0:0}</mspace>:<mspace=0.4em>{1:00}</mspace><size=20>.<mspace=0.4em>{2:000}</mspace></size>",
            minutes, seconds, milliseconds,
            atsc.CurrentSeconds < 0 ? "-" : "");
        currentBeatMesh.text = atsc.CurrentJsonTime.ToString(timeMeshFloatFormat);
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
