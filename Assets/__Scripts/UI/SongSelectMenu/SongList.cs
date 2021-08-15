using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongList : MonoBehaviour {

    private static readonly IComparer<BeatSaberSong> SortName =
        new WithFavouriteComparer((a, b) => string.Compare(a.songName, b.songName, StringComparison.InvariantCultureIgnoreCase));
    private static readonly IComparer<BeatSaberSong> SortModified =
        new WithFavouriteComparer((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
    private static readonly IComparer<BeatSaberSong> SortArtist =
        new WithFavouriteComparer((a, b) => string.Compare(a.songAuthorName, b.songAuthorName, StringComparison.InvariantCultureIgnoreCase));

    private static bool _lastVisitedWasWip = true;

    public event Action<SongSortType> OnSortTypeChanged;

    public SortedSet<BeatSaberSong> Songs = new SortedSet<BeatSaberSong>(SortName);
    public bool WipLevels = true;
    public bool FilteredBySearch = false;

    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Image sortImage;
    [SerializeField] private Sprite nameSortSprite;
    [SerializeField] private Sprite modifiedSortSprite;
    [SerializeField] private Sprite artistSortSprite;
    
    [SerializeField] private Color normalTabColor;
    [SerializeField] private Color selectedTabColor;
    [SerializeField] private Image wipTab;
    [SerializeField] private Image customTab;

    [SerializeField] private RecyclingListView newList;

    private List<BeatSaberSong> _filteredSongs = new List<BeatSaberSong>();
    private SongSortType _currentSort = SongSortType.Name;

    private void SwitchSort(IComparer<BeatSaberSong> newSort, Sprite sprite)
    {
        sortImage.sprite = sprite;
        Songs = new SortedSet<BeatSaberSong>(Songs, newSort);
        UpdateSongList();
    }

    public void NextSort()
    {
        switch (_currentSort)
        {
            case SongSortType.Name:
                _currentSort = SongSortType.Modified;
                break;
            case SongSortType.Modified:
                _currentSort = SongSortType.Artist;
                break;
            default:
                _currentSort = SongSortType.Name;
                break;
        }
        ApplySort(_currentSort);

        Settings.Instance.LastSongSortType = (int)_currentSort;

        OnSortTypeChanged?.Invoke(_currentSort);
    }

    public void ApplySort(SongSortType sortType)
    {
        switch (sortType)
        {
            case SongSortType.Name:
                SwitchSort(SortName, nameSortSprite);
                break;
            case SongSortType.Modified:
                SwitchSort(SortModified, modifiedSortSprite);
                break;
            default:
                SwitchSort(SortArtist, artistSortSprite);
                break;
        }
    }
    
    private void Start()
    {
        newList.ItemCallback = (item, index) =>
        {
            if (item is SongListItem child)
            {
                child.AssignSong(_filteredSongs[index], searchField.text);
            }
        };

        _currentSort = (SongSortType)Settings.Instance.LastSongSortType;
        ApplySort(_currentSort);
        
        OnSortTypeChanged?.Invoke(_currentSort);
        SetSongLocation(_lastVisitedWasWip);
    }

    public void ToggleSongLocation()
    {
        SetSongLocation(!WipLevels);
    }

    public void SetSongLocation(bool wip)
    {
        _lastVisitedWasWip = WipLevels = wip;
        wipTab.color = wip ? selectedTabColor : normalTabColor;
        customTab.color = !wip ? selectedTabColor : normalTabColor;
        TriggerRefresh();
    }

    public void TriggerRefresh()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshSongList());
    }

    public IEnumerator RefreshSongList() {
        var directories = Directory.GetDirectories(WipLevels ? Settings.Instance.CustomWIPSongsFolder : Settings.Instance.CustomSongsFolder);
        Songs.Clear();
        newList.Clear();
        var iterBeginTime = Time.realtimeSinceStartup;
        foreach (var dir in directories)
        {
            if (Time.realtimeSinceStartup - iterBeginTime > 0.02f)
            {
                UpdateSongList();
                yield return null;
                iterBeginTime = Time.realtimeSinceStartup;
            }

            var song = BeatSaberSong.GetSongFromFolder(dir);
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
                Songs.Add(song);
            }
        }
        UpdateSongList();
    }

    public void UpdateSongList()
    {
        FilteredBySearch = !string.IsNullOrEmpty(searchField.text);
        if (FilteredBySearch)
        {
            FilterBySearch();
        }
        else
        {
            _filteredSongs = Songs.ToList();
            ReloadListItems();
        }
    }
    
    public void FilterBySearch()
    {
        _filteredSongs = Songs.Where(x =>
            x.songName.IndexOf(searchField.text, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            x.songAuthorName.IndexOf(searchField.text, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
        ReloadListItems();
    }

    private void ReloadListItems() {
        if (newList.RowCount != _filteredSongs.Count)
        {
            newList.RowCount = _filteredSongs.Count;
        }
        else
        {
            newList.Refresh();
        }
    }

    public void RemoveSong(BeatSaberSong song)
    {
        Songs.Remove(song);
    }

    public void AddSong(BeatSaberSong song)
    {
        Songs.Add(song);
        UpdateSongList();
        /*if (song.IsFavourite)
        {
            newList.ScrollToRow(filteredSongs.IndexOf(song));
        }*/
    }

    public enum SongSortType
    {
        Name, Modified, Artist
    }

    private class FuncComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> _comparison;

        protected FuncComparer(Comparison<T> comparison)
        {
            this._comparison = comparison;
        }

        public virtual int Compare(T x, T y)
        {
            var result = _comparison(x, y);
            return result == 0 && x != null ? x.GetHashCode().CompareTo(y?.GetHashCode()) : result;
        }
    }

    private class WithFavouriteComparer : FuncComparer<BeatSaberSong>
    {
        public WithFavouriteComparer(Comparison<BeatSaberSong> comparison) : base(comparison) { }

        public override int Compare(BeatSaberSong a, BeatSaberSong b) =>
            a?.IsFavourite != b?.IsFavourite ? (a?.IsFavourite == true ? -1 : 1) : base.Compare(a, b);
    }
}
