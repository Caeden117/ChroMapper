using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongList : MonoBehaviour
{
    public enum SongSortType
    {
        Name, Modified, Artist
    }

    private static readonly IComparer<BeatSaberSong> SortName =
        new WithFavouriteComparer((a, b) =>
            string.Compare(a.SongName, b.SongName, StringComparison.InvariantCultureIgnoreCase));

    private static readonly IComparer<BeatSaberSong> SortModified =
        new WithFavouriteComparer((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));

    private static readonly IComparer<BeatSaberSong> SortArtist =
        new WithFavouriteComparer((a, b) =>
            string.Compare(a.SongAuthorName, b.SongAuthorName, StringComparison.InvariantCultureIgnoreCase));

    private static bool lastVisitedWasWip = true;

    public SortedSet<BeatSaberSong> Songs = new SortedSet<BeatSaberSong>(SortName);
    public bool WipLevels = true;
    public bool FilteredBySearch;

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
    private SongSortType currentSort = SongSortType.Name;

    private List<BeatSaberSong> filteredSongs = new List<BeatSaberSong>();

    private void Start()
    {
        newList.ItemCallback = (item, index) =>
        {
            if (item is SongListItem child) child.AssignSong(filteredSongs[index], searchField.text);
        };

        currentSort = (SongSortType)Settings.Instance.LastSongSortType;
        ApplySort(currentSort);

        SortTypeChanged?.Invoke(currentSort);
        SetSongLocation(lastVisitedWasWip);
    }

    public event Action<SongSortType> SortTypeChanged;

    private void SwitchSort(IComparer<BeatSaberSong> newSort, Sprite sprite)
    {
        sortImage.sprite = sprite;
        Songs = new SortedSet<BeatSaberSong>(Songs, newSort);
        UpdateSongList();
    }

    public void NextSort()
    {
        currentSort = currentSort switch
        {
            SongSortType.Name => SongSortType.Modified,
            SongSortType.Modified => SongSortType.Artist,
            _ => SongSortType.Name,
        };
        ApplySort(currentSort);

        Settings.Instance.LastSongSortType = (int)currentSort;

        SortTypeChanged?.Invoke(currentSort);
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

    public void ToggleSongLocation() => SetSongLocation(!WipLevels);

    public void SetSongLocation(bool wip)
    {
        lastVisitedWasWip = WipLevels = wip;
        wipTab.color = wip ? selectedTabColor : normalTabColor;
        customTab.color = !wip ? selectedTabColor : normalTabColor;
        TriggerRefresh();
    }

    public void TriggerRefresh()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshSongList());
    }

    public IEnumerator RefreshSongList()
    {
        var directories =
            Directory.GetDirectories(WipLevels
                ? Settings.Instance.CustomWIPSongsFolder
                : Settings.Instance.CustomSongsFolder);
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
            else
                Songs.Add(song);
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
            filteredSongs = Songs.ToList();
            ReloadListItems();
        }
    }

    public void FilterBySearch()
    {
        filteredSongs = Songs.Where(x =>
            x.SongName.IndexOf(searchField.text, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            x.SongAuthorName.IndexOf(searchField.text, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
        ReloadListItems();
    }

    private void ReloadListItems()
    {
        if (newList.RowCount != filteredSongs.Count)
            newList.RowCount = filteredSongs.Count;
        else
            newList.Refresh();
    }

    public void RemoveSong(BeatSaberSong song) => Songs.Remove(song);

    public void AddSong(BeatSaberSong song)
    {
        Songs.Add(song);
        UpdateSongList();
        /*if (song.IsFavourite)
        {
            newList.ScrollToRow(filteredSongs.IndexOf(song));
        }*/
    }

    private class FuncComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        protected FuncComparer(Comparison<T> comparison) => this.comparison = comparison;

        public virtual int Compare(T x, T y)
        {
            var result = comparison(x, y);
            return result == 0 && x != null ? x.GetHashCode().CompareTo(y?.GetHashCode()) : result;
        }
    }

    private class WithFavouriteComparer : FuncComparer<BeatSaberSong>
    {
        public WithFavouriteComparer(Comparison<BeatSaberSong> comparison) : base(comparison) { }

        public override int Compare(BeatSaberSong a, BeatSaberSong b) =>
            a?.IsFavourite != b?.IsFavourite ? a?.IsFavourite == true ? -1 : 1 : base.Compare(a, b);
    }
}
