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
        if (list.songs.Any(x => x.songName == res))
        {
            PersistentUI.Instance.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName, "newmap.dialog.default");
            return;
        }
        BeatSaberSong song = new BeatSaberSong(list.WIPLevels, res);
        BeatSaberSong.DifficultyBeatmapSet standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        song.difficultyBeatmapSets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(song);
        PersistentUI.Instance.DisplayMessage("Be sure to save info.dat before editing!", PersistentUI.DisplayMessageType.BOTTOM); // newmap.message
    }
}
