using System;
using System.IO;
using System.Linq;
using Beatmap.Info;
using UnityEngine;

public class CreateNewSong : MonoBehaviour
{
    [SerializeField] private SongList list;

    public void CreateSong() => PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog", HandleNewSongName,
        "newmap.dialog.default");

    private void HandleNewSongName(string res)
    {
        if (string.IsNullOrWhiteSpace(res)) return;

        var song = new BaseInfo { SongName = res };

        if (string.IsNullOrWhiteSpace(song.CleanSongName))
        {
            PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog.invalid", HandleNewSongName,
                "newmap.dialog.default");
            return;
        }

        var songDirectory = Path.Combine(list.SelectedFolderPath, song.CleanSongName);
        if (list.SongInfos.Any(x => Path.GetFullPath(x.Directory).Equals(
                Path.GetFullPath(Path.Combine(songDirectory)),
                StringComparison.CurrentCultureIgnoreCase)))
        {
            PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName,
                "newmap.dialog.default");
            return;
        }

        song.Directory = songDirectory;

        var standardSet = new InfoDifficultySet { Characteristic = "Standard" };
        song.DifficultySets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(song);
        PersistentUI.Instance.ShowDialogBox("SongSelectMenu", "newmap.message", null,
            PersistentUI.DialogBoxPresetType.Ok);
    }
}
