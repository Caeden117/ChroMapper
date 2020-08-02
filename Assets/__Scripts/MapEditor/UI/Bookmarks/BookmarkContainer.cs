using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BookmarkContainer : MonoBehaviour, IPointerClickHandler
{
    public BeatmapBookmark data { get; private set; }
    private BookmarkManager manager;
    private float seconds;

    public void Init(BookmarkManager manager, BeatmapBookmark data)
    {
        if (this.data != null) return;
        this.data = data;
        this.manager = manager;
        GetComponent<Image>().color = Random.ColorHSV(0, 1, 0.75f, 0.75f, 1, 1);
        GetComponent<Tooltip>().tooltipOverride = data._name;
        seconds = data._time * (60 / BeatSaberSongContainer.Instance.song.beatsPerMinute);
        float modifiedBeat = manager.atsc.GetBeatFromSeconds(seconds);
        float unitsPerBeat = 780 / manager.atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length);
        ((RectTransform) transform).anchoredPosition = new Vector2(unitsPerBeat * modifiedBeat, 50);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                manager.atsc.MoveToTimeInSeconds(seconds);
                break;
            case PointerEventData.InputButton.Middle:
                PersistentUI.Instance.ShowDialogBox("Are you sure you want to delete this bookmark?", HandleDeleteBookmark, PersistentUI.DialogBoxPresetType.YesNo);
                break;
        }
    }

    private void HandleDeleteBookmark(int res)
    {
        if (res == 0)
        {
            manager.bookmarkContainers.Remove(this);
            Destroy(gameObject);
        }
    }
}
