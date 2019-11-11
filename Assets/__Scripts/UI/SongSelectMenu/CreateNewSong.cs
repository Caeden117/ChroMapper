using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CreateNewSong : MonoBehaviour {

    [SerializeField]
    private SongList list;

	public void CreateSong()
    {
        PersistentUI.Instance.ShowInputBox("Please enter the name for the new song.", HandleNewSongName, "New Song");
    }

    private void HandleNewSongName(string res)
    {
        if (res is null) return;
        BeatSaberSong songe = new BeatSaberSong(list.WIPLevels, res);
        BeatSaberSongContainer.Instance.SelectSongForEditing(songe);
        PersistentUI.Instance.DisplayMessage("Be sure to save info.dat before editing!", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
