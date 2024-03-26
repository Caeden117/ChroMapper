using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class CreateNewSong : MonoBehaviour
{
    [SerializeField] private SongList list;

    public void CreateSong() => PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog", HandleNewSongName,
        "newmap.dialog.default");

    private void HandleNewSongName(string res)
    {
        if (string.IsNullOrWhiteSpace(res)) return;

        var song = new BeatSaberSong(list.SelectedFolderPath, res);
        
        if (string.IsNullOrWhiteSpace(song.CleanSongName))
        {
            PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog.invalid", HandleNewSongName,
                "newmap.dialog.default");
            return;
        }
        
        if (list.Songs.Any(x => Path.GetFullPath(x.Directory).Equals(
            Path.GetFullPath(Path.Combine(
                list.SelectedFolderPath,
                song.CleanSongName)),
            StringComparison.CurrentCultureIgnoreCase
        )))
        {
            PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName,
                "newmap.dialog.default");
            return;
        }

        var standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        song.DifficultyBeatmapSets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(song);
        PersistentUI.Instance.ShowDialogBox("SongSelectMenu", "newmap.message", null,
            PersistentUI.DialogBoxPresetType.Ok);
    }
}
