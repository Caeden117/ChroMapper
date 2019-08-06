using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CreateNewSong : MonoBehaviour {

    [SerializeField]
    private SongList list;

	public void CreateSong()
    {
        BeatSaberSong songe = new BeatSaberSong(list.WIPLevels);
        BeatSaberSongContainer.Instance.SelectSongForEditing(songe);
        PersistentUI.Instance.DisplayMessage("Be sure to save info.dat before editing!", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
