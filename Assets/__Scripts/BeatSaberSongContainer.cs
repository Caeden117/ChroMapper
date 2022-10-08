using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatSaberSongContainer : MonoBehaviour
{
    [FormerlySerializedAs("song")] public BeatSaberSong Song;
    [FormerlySerializedAs("difficultyData")] public BeatSaberSong.DifficultyBeatmap DifficultyData;
    [FormerlySerializedAs("loadedSong")] public AudioClip LoadedSong;
    [FormerlySerializedAs("map")] public IDifficulty Map;
    public static BeatSaberSongContainer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) Destroy(Instance.gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectSongForEditing(BeatSaberSong song)
    {
        Song = song;
        SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
    }
}
