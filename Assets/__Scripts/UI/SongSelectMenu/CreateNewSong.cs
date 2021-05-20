using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class CreateNewSong : MonoBehaviour {

    [SerializeField]
    private SongList list;

	public void CreateSong()
    {
        PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog", HandleNewSongName, "newmap.dialog.default");
    }

    private void HandleNewSongName(string res)
    {
        if (res is null) return;

        var song = new BeatSaberSong(list.WipLevels, res);

        if (list.Songs.Any(x => Path.GetFullPath(x.directory).Equals(
            Path.GetFullPath(Path.Combine(list.WipLevels ? Settings.Instance.CustomWIPSongsFolder : Settings.Instance.CustomSongsFolder, song.cleanSongName)),
            StringComparison.CurrentCultureIgnoreCase
        )))
        {
            PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName, "newmap.dialog.default");
            return;
        }

        var standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        song.difficultyBeatmapSets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(song);
        PersistentUI.Instance.DisplayMessage("SongSelectMenu", "newmap.message", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
