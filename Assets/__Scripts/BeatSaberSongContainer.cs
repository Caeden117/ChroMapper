using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatSaberSongContainer : MonoBehaviour {

    private static BeatSaberSongContainer _instance;
    public static BeatSaberSongContainer Instance => _instance;

    private void Awake() {
        if (_instance != null) {
            Destroy(_instance);
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public BeatSaberSong song;
    public BeatSaberSong.DifficultyBeatmap difficultyData;
    public AudioClip loadedSong;
    public BeatSaberMap map;

    public void SelectSongForEditing(BeatSaberSong song) {
        this.song = song;
        SceneTransitionManager.Instance.LoadScene(2);
    }

    public void SelectMapForEditing(BeatSaberMap map) {
        this.map = map;
        SceneTransitionManager.Instance.LoadScene(3);
    }

}
