using System.Linq;
using UnityEngine;

public class CreateNewSong : MonoBehaviour {

    [SerializeField]
    private SongList list;

	public void CreateSong()
    {
        PersistentUI.Instance.ShowInputBox("Please enter the name for the new beatmap.", HandleNewSongName, "New Beatmap");
    }

    private void HandleNewSongName(string res)
    {
        if (res is null) return;
        if (list.songs.Any(x => x.songName == res))
        {
            PersistentUI.Instance.ShowInputBox("There already exists a beatmap with that name.\n\n" + 
                "Please enter the name for the new beatmap.", HandleNewSongName, "New Beatmap");
            return;
        }
        BeatSaberSong songe = new BeatSaberSong(list.WIPLevels, res);
        BeatSaberSong.DifficultyBeatmapSet standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        songe.difficultyBeatmapSets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(songe);
        PersistentUI.Instance.DisplayMessage("Be sure to save info.dat before editing!", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
