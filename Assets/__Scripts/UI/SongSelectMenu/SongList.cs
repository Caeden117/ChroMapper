using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using static EnumPicker;

public class SongList : MonoBehaviour
{

    public enum SortingOption
    {
        [PickerChoice("SongSelectMenu", "sortbysong")]
        SONG,
        [PickerChoice("SongSelectMenu", "sortbyartist")]
        ARTIST,
        [PickerChoice("SongSelectMenu", "sortbymodified")]
        MODIFIED
    }
    private SortingOption lastSortingOption = SortingOption.SONG;

    [SerializeField]
    TMP_InputField searchField;

    [SerializeField]
    EnumPicker sortingOptions;

    [SerializeField]
    RecyclingListView listView;

    [SerializeField]
    public List<BeatSaberSong> songs = new List<BeatSaberSong>();

    public bool WIPLevels = true;
    public bool FilteredBySearch = false;

    private void Start()
    {
        listView.ItemCallback = SetupCell;
        RefreshSongList(false);
        sortingOptions.Initialize(typeof(SortingOption));
        sortingOptions.onClick += SortBy;
    }

    public void SetSongLocation(bool wipLevels)
    {
        WIPLevels = wipLevels;
        RefreshSongList(true);
    }

    public void RefreshSongList(bool search)
    {
        FilteredBySearch = search;
        string[] directories;
        directories = Directory.GetDirectories(WIPLevels ? Settings.Instance.CustomWIPSongsFolder : Settings.Instance.CustomSongsFolder);
        songs.Clear();
        foreach (var dir in directories)
        {
            BeatSaberSong song = BeatSaberSong.GetSongFromFolder(dir);
            if (song == null)
            {
                Debug.LogWarning($"No song at location {dir} exists! Is it in a subfolder?");
                /*
                 * Subfolder loading support has been removed for the following:
                 * A) SongCore does not natively support loading from subfolders, only through editing a config file
                 * B) OneClick no longer saves to a subfolder
                 */
                /*if (dir.ToUpper() == "CACHE") continue; //Ignore the cache folder
                //Get songs from subdirectories
                string[] subDirectories = Directory.GetDirectories(dir);
                foreach (var subDir in subDirectories)
                {
                    song = BeatSaberSong.GetSongFromFolder(subDir);
                    if (song != null) songs.Add(song);
                }*/
            }
            else
            {
                songs.Add(song);
            }
        }
        //Sort by song name, and filter by search text.
        if (FilteredBySearch)
            songs = songs.Where(x => searchField.text != "" ? x.songName.AllIndexOf(searchField.text).Any() : true).ToList();
        SortBy(lastSortingOption);
    }

    public void SortBy(Enum sortingOption)
    {
        lastSortingOption = (SortingOption)sortingOption;
        switch (lastSortingOption)
        {
            case SortingOption.SONG:
                songs = songs.OrderBy(x => x.songName).ToList();
                break;
            case SortingOption.ARTIST:
                songs = songs.OrderBy(x => x.songAuthorName).ToList();
                break;
            case SortingOption.MODIFIED:
                songs = songs.OrderByDescending(x => Directory.GetLastWriteTime(x.directory)).ToList();
                break;
        }
        UpdateList();
    }

    public void UpdateList()
    {
        if (listView.RowCount != songs.Count)
            listView.RowCount = songs.Count;
        else
            listView.Refresh();
    }

    private void SetupCell(RecyclingListViewItem item, int rowIndex)
    {
        item.GetComponent<SongListItem>().AssignSong(songs[rowIndex]);
    }
}
