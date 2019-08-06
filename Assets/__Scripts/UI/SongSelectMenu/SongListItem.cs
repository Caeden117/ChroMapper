using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongListItem : MonoBehaviour {

    [SerializeField]
    TMP_Text text;

    [SerializeField]
    Button button;

    private BeatSaberSong song;

    public void SetDisplayName(string title) {
        text.text = title;
    }

    public void AssignButton(BeatSaberSong song) {
        this.song = song;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ButtonClicked);
    }

    void ButtonClicked() {
        Debug.Log("Edit button for song " + song.songName);
        if (BeatSaberSongContainer.Instance != null && song != null) BeatSaberSongContainer.Instance.SelectSongForEditing(song);
    }

}
