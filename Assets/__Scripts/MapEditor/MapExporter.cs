using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using QuestDumper;
using UnityEngine.Localization.Settings;
using Debug = UnityEngine.Debug;
using Task = System.Threading.Tasks.Task;

// This is a struct because it doesn't really manage data
// Should this just be a static singleton since `song` is always passed from BeatSaberSongContainer?
public struct MapExporter
{
    // TODO: Move constants
    public const string QUEST_CUSTOM_SONGS_LOCATION =
        "sdcard/ModData/com.beatgames.beatsaber/Mods/SongLoader/CustomLevels";

    // I added this so the non-quest maintainers can use it as a reference for adding WIP uploads
    // this does indeed exist and work, please don't refrain from asking me. 
    public const string QUEST_CUSTOM_SONGS_WIP_LOCATION =
        "sdcard/ModData/com.beatgames.beatsaber/Mods/SongLoader/CustomWIPLevels";


    private readonly BeatSaberSong song;

    public MapExporter(BeatSaberSong song) => this.song = song;

    /// <summary>
    /// Exports the files to the Quest using adb
    /// </summary>
    public async Task ExportToQuest()
    {
        var (devices, output) = await Adb.GetDevices();

        if (devices == null || !string.IsNullOrEmpty(output.ErrorOut))
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "quest.no-devices", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        // Run async
        var questCandidatesTask =
            await Task.WhenAll(devices.Select(async device => (device, quest: (await Adb.IsQuest(device)).Item1)));
        var questCandidates = questCandidatesTask.Where(s => s.quest).Select(s => s.device).ToList();

        if (questCandidates.Count == 0)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "quest.no-quest", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var dialog = PersistentUI.Instance.CreateNewDialogBox();
        dialog.WithTitle("SongEditMenu", "quest.exporting");

        var progressBar = dialog.AddComponent<ProgressBarComponent>();
        progressBar.WithCustomLabelFormatter(f =>
            LocalizationSettings.StringDatabase
                .GetLocalizedString("SongEditMenu", "quest.exporting_progress",
                    new object[] { f }));

        dialog.Open();

        // We should always be exporting to WIP Levels. CustomLevels are for downloaded BeatSaver songs.
        var songExportPath = Path.Combine(QUEST_CUSTOM_SONGS_WIP_LOCATION, song.CleanSongName).Replace("\\", @"/");
        var exportedFiles = song.GetFilesForArchiving();

        if (exportedFiles == null) return;

        Debug.Log($"Creating folder if needed at {songExportPath}");

        var totalFiles = questCandidates.Count * exportedFiles.Count;
        var fCount = 0;

        foreach (var questCandidate in questCandidates)
        {
            var createDir = await Adb.Mkdir(songExportPath, questCandidate);
            Debug.Log($"ADB Create dir: {createDir}");


            foreach (var fileNamePair in exportedFiles)
            {
                var locationRelativeToSongDir = fileNamePair.Value;

                var questPath = Path.Combine(songExportPath, locationRelativeToSongDir).Replace("\\", @"/");

                Debug.Log($"Pushing {questPath} from {fileNamePair.Key}");

                var log = await Adb.Push(fileNamePair.Key, questPath, questCandidate);
                Debug.Log(log.ToString());

                fCount++;
                progressBar.UpdateProgressBar((float)fCount / totalFiles);
            }
        }

        dialog.Clear();

        Debug.Log("EXPORTED TO QUEST SUCCESSFULLY YAYAAYAYA");

        dialog.WithTitle("Options", "quest.success");
        dialog.AddFooterButton(null, "PersistentUI", "ok");
    }

    /// <summary>
    ///     Create a zip for sharing the map
    /// </summary>
    public bool PackageZip()
    {
        var infoFileLocation = "";
        var zipPath = "";
        if (Directory.Exists(song.Directory))
        {
            zipPath = Path.Combine(song.Directory, song.CleanSongName + ".zip");
            // Mac doesn't seem to like overwriting existing zips, so delete the old one first
            File.Delete(zipPath);

            infoFileLocation = Path.Combine(song.Directory, "Info.dat");
        }

        if (!File.Exists(infoFileLocation))
        {
            Debug.LogError(":hyperPepega: :mega: WHY TF ARE YOU TRYING TO PACKAGE A MAP WITH NO INFO.DAT FILE");
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "zip.warning", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return false;
        }

        var exportedFiles = song.GetFilesForArchiving();
        if (exportedFiles == null)
        {
            return false;
        }

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var pathFileEntryPair in exportedFiles)
            {
                archive.CreateEntryFromFile(pathFileEntryPair.Key, pathFileEntryPair.Value);
            }
        }

        return true;
    }

    /// <summary>
    ///     Open the folder containing the map's files in a native file browser
    /// </summary>
    public void OpenSelectedMapInFileBrowser()
    {
        if (!Directory.Exists(song.Directory))
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "explorer.warning", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var path = song.Directory;
#if UNITY_STANDALONE_WIN
        path = path.Replace("/", "\\").Replace("\\\\", "\\");
#else
        path = path.Replace("\\", "/").Replace("//", "/");
#endif
        if (!path.StartsWith("\"")) path = "\"" + path;
        if (!path.EndsWith("\"")) path += "\"";

#if UNITY_STANDALONE_WIN
        Debug.Log($"Opening song directory ({path}) with Windows...");
        Process.Start("explorer.exe", path);
#elif UNITY_STANDALONE_OSX
        Debug.Log($"Opening song directory ({path}) with Mac...");
        Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
        Debug.Log($"Opening song directory ({path}) with Linux...");
        Process.Start("xdg-open", path);
#else
        Debug.Log("What is this, some UNIX bullshit?");
        PersistentUI.Instance.ShowDialogBox(
            "Unrecognized OS!\n\nIf you happen to know this OS and would like to contribute," +
            " please contact me on Discord: Caeden117#0117", null, PersistentUI.DialogBoxPresetType.Ok);
#endif
    }
}
