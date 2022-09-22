using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatSaberSongContainer : MonoBehaviour
{
    [FormerlySerializedAs("song")] public BeatSaberSong Song;
    [FormerlySerializedAs("difficultyData")] public BeatSaberSong.DifficultyBeatmap DifficultyData;
    [FormerlySerializedAs("loadedSong")] public AudioClip LoadedSong;
    [FormerlySerializedAs("map")] public BeatSaberMap Map;

    [NonSerialized] public MultiClientNetListener? MultiMapperConnection;

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

    public void ConnectToMultiSession(string ip, int port, MapperIdentityPacket identity)
    {
        MultiMapperConnection = new MultiClientNetListener(ip, port, identity);
        SceneTransitionManager.Instance.LoadScene("03_Mapper", DownloadAndLaunchMap());
    }

    public void ConnectToMultiSession(string roomCode, MapperIdentityPacket identity)
    {
        MultiMapperConnection = new MultiClientNetListener(roomCode, identity);
        SceneTransitionManager.Instance.LoadScene("03_Mapper", DownloadAndLaunchMap());
    }

    private IEnumerator DownloadAndLaunchMap()
    {
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        PersistentUI.Instance.LevelLoadSlider.value = 0;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "Downloading song... this can take several moments.";

        yield return new WaitUntil(() => MultiMapperConnection?.MapData != null);

        // Create the directory for our song to go to.
        // Path.GetTempPath() should be compatible with Windows and UNIX.
        // See Microsoft docs on it.
        var directory = Path.Combine(Path.GetTempPath(), "ChroMapper Multi Mapping", MultiMapperConnection?.MapData.GetHashCode().ToString());
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var stream = new MemoryStream(MultiMapperConnection.MapData.ZipBytes);
        var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // Extract our zipped file into this directory.
        archive.ExtractToDirectory(directory);

        // Try and get a BeatSaberSong out of what we've downloaded.
        var song = BeatSaberSong.GetSongFromFolder(directory);
        if (song != null)
        {
            PersistentUI.Instance.LevelLoadSliderLabel.text = "Loading song...";
            Song = song;

            // Find characteristic and difficulty
            DifficultyData = Song.DifficultyBeatmapSets
                .Find(set => set.BeatmapCharacteristicName == MultiMapperConnection.MapData.Characteristic)
                .DifficultyBeatmaps.Find(diff => diff.Difficulty == MultiMapperConnection.MapData.Diff);

            Map = song.GetMapFromDifficultyBeatmap(DifficultyData);

            yield return Song.LoadAudio((clip) => LoadedSong = clip, Song.SongTimeOffset, null);
        }

        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    private void Update() => MultiMapperConnection?.ManualUpdate();

    public class MultiMapperParams
    {
        public string IP;
        public int Port;
    }
}
