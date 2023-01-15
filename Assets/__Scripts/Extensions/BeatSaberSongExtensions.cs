using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

public static class BeatSaberSongExtensions
{
    private static readonly Dictionary<string, AudioType> extensionToAudio = new Dictionary<string, AudioType>
    {
        {".ogg", AudioType.OGGVORBIS}, {".egg", AudioType.OGGVORBIS}, {".wav", AudioType.WAV}
    };

    /// <summary>
    ///     Try and load the song, this is used for the song preview as well as later
    ///     passed to the mapping scene
    /// </summary>
    /// <param name="useTemp">Should we load the song the user has updated in the UI or from the saved song data</param>
    /// <returns>Coroutine IEnumerator</returns>
    public static IEnumerator LoadAudio(this BeatSaberSong song, Action<AudioClip> onClipLoaded, float songTimeOffset = 0, string overrideLocalPath = null)
    {
        if (song.Directory == null) yield break;

        var fullPath = Path.Combine(song.Directory, overrideLocalPath ?? song.SongFilename);

        // Commented out since Song Time Offset changes need to reload the song, even if its the same file
        //if (fullPath == loadedSong)
        //{
        //    yield break;
        //}
        var audioType = extensionToAudio[Path.GetExtension(fullPath)];

        var www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{fullPath}")}", audioType);

        // Escaping should fix the issue where half the people can't open ChroMapper's editor (I believe this is caused by spaces in the directory, hence escaping)
        yield return www.SendWebRequest();

        Debug.Log("Song loaded!");
        var clip = DownloadHandlerAudioClip.GetContent(www);
        if (clip == null)
        {
            Debug.Log("Error getting Audio data!");
            SceneTransitionManager.Instance.CancelLoading("load.error.audio");
        }

        clip.name = "Song";

        if (songTimeOffset != 0)
        {
            // Take songTimeOffset into account by adjusting clip data forward/backward

            // Guaranteed to always be an integer multiple of the number of channels
            var songTimeOffsetSamples = Mathf.CeilToInt(songTimeOffset * clip.frequency) * clip.channels;
            var samples = new float[clip.samples * clip.channels];

            clip.GetData(samples, 0);

            // Negative offset: Shift existing data forward, fill in beginning blank with 0s
            if (songTimeOffsetSamples < 0)
            {
                Array.Resize(ref samples, samples.Length - songTimeOffsetSamples);

                for (var i = samples.Length - 1; i >= 0; i--)
                {
                    var shiftIndex = i + songTimeOffsetSamples;

                    samples[i] = shiftIndex < 0 ? 0 : samples[shiftIndex];
                }
            }
            // Positive offset: Shift existing data backward, cut off ending blank
            else
            {
                for (var i = 0; i < samples.Length; i++)
                {
                    var shiftIndex = i + songTimeOffsetSamples;

                    samples[i] = shiftIndex >= samples.Length ? 0 : samples[shiftIndex];
                }

                // Bit of a hacky workaround, since you can't create an AudioClip with 0 length,
                // and something in the spectrogram code doesn't like too short lengths either
                // This just sets a minimum of 4096 samples per channel
                Array.Resize(ref samples, Math.Max(samples.Length - songTimeOffsetSamples, clip.channels * 4096));
            }

            // Create a new AudioClip because apparently you can't change the length of an existing one
            clip = AudioClip.Create(clip.name, samples.Length / clip.channels, clip.channels, clip.frequency, false);
            clip.SetData(samples, 0);
        }

        onClipLoaded?.Invoke(clip);
    }

    [CanBeNull]
    public static Dictionary<string, string> GetFilesForArchiving(this BeatSaberSong song)
    {
        // path:entry_name
        var exportedFiles = new Dictionary<string, string>();

        var infoFileLocation = "";
        if (song.Directory != null)
        {
            infoFileLocation = Path.Combine(song.Directory, "Info.dat");
        }

        if (!File.Exists(infoFileLocation))
        {
            Debug.LogError(":hyperPepega: :mega: WHY TF ARE YOU TRYING TO PACKAGE A MAP WITH NO INFO.DAT FILE");
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "zip.warning", null, PersistentUI.DialogBoxPresetType.Ok);
            return null;
        }

        exportedFiles.Add(infoFileLocation, "Info.dat");
        AddToFileDictionary(exportedFiles, song.Directory, song.CoverImageFilename);
        AddToFileDictionary(exportedFiles, song.Directory, song.SongFilename);
        AddToFileDictionary(exportedFiles, song.Directory, "cinema-video.json");

        foreach (var contributor in song.Contributors.DistinctBy(it => it.LocalImageLocation))
        {
            var imageLocation = Path.Combine(song.Directory!, contributor.LocalImageLocation);
            if (contributor.LocalImageLocation != song.CoverImageFilename &&
                File.Exists(imageLocation) && !File.GetAttributes(imageLocation).HasFlag(FileAttributes.Directory))
            {
                exportedFiles.Add(imageLocation, contributor.LocalImageLocation);
            }
        }

        foreach (var map in song.DifficultyBeatmapSets.SelectMany(set => set.DifficultyBeatmaps))
        {
            AddToFileDictionary(exportedFiles, song.Directory, map.BeatmapFilename);
        }

        return exportedFiles;
    }

    private static void AddToFileDictionary(IDictionary<string, string> fileMap, string directory, string fileLocation)
    {
        var fullPath = Path.Combine(directory, fileLocation);

        if (File.Exists(fullPath))
        {
            fileMap.Add(fullPath, fileLocation);
        }
    }
}
