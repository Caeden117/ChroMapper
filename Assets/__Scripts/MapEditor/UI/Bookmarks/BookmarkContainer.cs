using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookmarkContainer : MonoBehaviour
{
    private BeatmapBookmark data;
    private BookmarkManager manager;
    private float seconds;

    public void Init(BookmarkManager manager, BeatmapBookmark data)
    {
        if (this.data != null) return;
        this.data = data;
        this.manager = manager;
        GetComponent<Image>().color = Random.ColorHSV(0, 1, 0.75f, 0.75f, 1, 1);
        GetComponent<Tooltip>().tooltip = data._name;
        seconds = data._time * (60 / BeatSaberSongContainer.Instance.song.beatsPerMinute);
        float modifiedBeat = manager.atsc.GetBeatFromSeconds(seconds);
        float unitsPerBeat = 780 / manager.atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length);
        (transform as RectTransform).anchoredPosition = new Vector2(unitsPerBeat * modifiedBeat, 50);
    }

    public void JumpToBookmark()
    {
        manager.atsc.MoveToTimeInSeconds(seconds);
    }
}
