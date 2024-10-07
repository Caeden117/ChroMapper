using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;
using Beatmap.Base;
using Beatmap.Info;

public class BeatSaberSongContainer : MonoBehaviour
{
    [Obsolete("Use Info",true)]
    [FormerlySerializedAs("song")] public BeatSaberSong Song;
    [Obsolete("Use MapDifficultyInfo")]
    [FormerlySerializedAs("difficultyData")] public BeatSaberSong.DifficultyBeatmap DifficultyData;
    [FormerlySerializedAs("loadedSong")] public AudioClip LoadedSong;

    public BaseInfo Info;
    public InfoDifficulty MapDifficultyInfo;
    [FormerlySerializedAs("map")] public BaseDifficulty Map;

    [NonSerialized] public MultiClientNetListener? MultiMapperConnection;

    // Because Unity API is not thread safe, these are stored for later when saving difficulty file on separate thread
    [HideInInspector] public int LoadedSongSamples;
    [HideInInspector] public int LoadedSongFrequency;
    [HideInInspector] public float LoadedSongLength;

    public static BeatSaberSongContainer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) Destroy(Instance.gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SelectSongForEditing(BaseInfo info)
    {
        Info = info;
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
        PersistentUI.Instance.LevelLoadSliderLabel.text =
            LocalizationSettings.StringDatabase.GetLocalizedString("MultiMapping", "multi.session.downloading");

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
        var mapInfo = BeatSaberSong.GetInfoFromFolder(directory);
        if (mapInfo != null)
        {
            PersistentUI.Instance.LevelLoadSliderLabel.text =
                LocalizationSettings.StringDatabase.GetLocalizedString("MultiMapping", "multi.session.loading");
            Info = mapInfo;

            // Find characteristic and difficulty
            MapDifficultyInfo = mapInfo.DifficultySets
                .Find(set => set.Characteristic == MultiMapperConnection.MapData.Characteristic)
                .Difficulties.Find(diff => diff.Difficulty == MultiMapperConnection.MapData.Diff);
            
            Map = BeatSaberSong.GetMapFromInfoFiles(mapInfo, MapDifficultyInfo);
            Settings.Instance.MapVersion = Map.Version[0] == '3' ? 3 : 2;

            yield return BeatSaberSongExtensions.LoadAudio(mapInfo, (clip) =>
            {
                LoadedSong = clip;
                LoadedSongSamples = clip.samples;
                LoadedSongFrequency = clip.frequency;
                LoadedSongLength = clip.length;
            }, mapInfo.SongTimeOffset, null);
        }

        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    private void Update() => MultiMapperConnection?.ManualUpdate();
}
