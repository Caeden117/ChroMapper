using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class SongList : MonoBehaviour
{
    public enum SongSortType
    {
        Name, Modified, Artist
    }

    private static readonly IComparer<BeatSaberSong> sortName =
        new WithFavouriteComparer((a, b) =>
            string.Compare(a.SongName, b.SongName, StringComparison.InvariantCultureIgnoreCase));

    private static readonly IComparer<BeatSaberSong> sortModified =
        new WithFavouriteComparer((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));

    private static readonly IComparer<BeatSaberSong> sortArtist =
        new WithFavouriteComparer((a, b) =>
            string.Compare(a.SongAuthorName, b.SongAuthorName, StringComparison.InvariantCultureIgnoreCase));

    public SortedSet<BeatSaberSong> Songs = new(sortName);
    public bool FilteredBySearch;

    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Image sortImage;
    [SerializeField] private Sprite nameSortSprite;
    [SerializeField] private Sprite modifiedSortSprite;
    [SerializeField] private Sprite artistSortSprite;

    [SerializeField] private Color normalTabColor;
    [SerializeField] private Color selectedTabColor;

    [SerializeField] private RecyclingListView newList;
    private SongSortType currentSort = SongSortType.Name;

    private List<BeatSaberSong> filteredSongs = new();

    public string SelectedFolderPath => songFolderPaths[selectedFolder];
    private static int selectedFolder;

    [SerializeField] private GameObject songFolderPrefab;
    private readonly List<GameObject> songFolderObjects = new();
    private readonly List<string> songFolderPaths = new();

    private void Start()
    {
        newList.ItemCallback = (item, index) =>
        {
            if (item is SongListItem child) child.AssignSong(filteredSongs[index], searchField.text);
        };

        AddDefaultFolders();
        AddSongCoreFolders();

        currentSort = (SongSortType)Settings.Instance.LastSongSortType;
        ApplySort(currentSort);

        SortTypeChanged?.Invoke(currentSort);
        SetSongLocation(selectedFolder);
    }

    private void AddDefaultFolders()
    {
        InitFolderObject("WIP Levels", Settings.Instance.CustomWIPSongsFolder);
        InitFolderObject("Custom Levels", Settings.Instance.CustomSongsFolder);
    }

    /*
     * SongCore folder structure is:
     * <folders>
     *   <folder>
     *     <Name></Name>
     *     <Path></Path>
     *   <folder>
     * </folders>
     */
    private void AddSongCoreFolders()
    {
        var songCoreFolder = Path.Combine(Settings.Instance.BeatSaberInstallation, "UserData", "SongCore");
        var foldersXml = Path.Combine(songCoreFolder, "folders.xml");

        if (!File.Exists(foldersXml)) return;

        var xml = XDocument.Load(foldersXml);

        foreach (var folder in xml.Descendants("folder"))
        {
            var tabName = folder.Element("Name")?.Value;
            var path = folder.Element("Path")?.Value;

            if (tabName == null || path == null || !Directory.Exists(path)) continue;

            InitFolderObject(tabName, path);
        }
    }

    private void InitFolderObject(string tabName, string folderPath)
    {
        var prefab = Instantiate(songFolderPrefab, songFolderPrefab.transform.parent, true);
        var button = prefab.GetComponent<Button>();
        var count = songFolderObjects.Count;
        button.onClick.AddListener(() => SetSongLocation(count));

        var childObject = prefab.transform.GetChild(0).gameObject;
        var text = childObject.GetComponent<TextMeshProUGUI>();
        text.text = tabName;

        songFolderObjects.Add(prefab);
        songFolderPaths.Add(folderPath);

        prefab.SetActive(true);

        // Use localised strings for default folders
        if (folderPath == Settings.Instance.CustomWIPSongsFolder || folderPath == Settings.Instance.CustomSongsFolder)
        {
            var localizeStringComponent = childObject.GetComponent<LocalizeStringEvent>();
            localizeStringComponent.StringReference.TableEntryReference =
                folderPath == Settings.Instance.CustomWIPSongsFolder
                    ? "wip"
                    : "custom";
            localizeStringComponent.enabled = true;
        }
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
                SwitchSort(sortName, nameSortSprite);
                break;
            case SongSortType.Modified:
                SwitchSort(sortModified, modifiedSortSprite);
                break;
            default:
                SwitchSort(sortArtist, artistSortSprite);
                break;
        }
    }

    public void SetSongLocation(int index)
    {
        selectedFolder = index;
        
        for (var i = 0; i < songFolderObjects.Count; i++)
        {
            var image = songFolderObjects[i].gameObject.GetComponent<Image>();
            image.color = i == selectedFolder ? selectedTabColor : normalTabColor;
        }
        
        TriggerRefresh();
    }

    public void TriggerRefresh()
    {
        StopAllCoroutines();
        RefreshSongList().Forget();
    }

    public async UniTask RefreshSongList()
    {
        Songs.Clear();
        newList.Clear();

        await UniTask.SwitchToThreadPool();

        var directories = new DirectoryInfo(songFolderPaths[selectedFolder]).GetDirectories();
        var songQueue = new List<UniTask>(directories.Length);
        var loadedSongs = new ConcurrentQueue<BeatSaberSong>();

        foreach (var dir in directories)
        {
            if (dir.Attributes.HasFlag(FileAttributes.Hidden)) continue;

            songQueue.Add(LoadSongInternal(dir, loadedSongs));
        }

        var task = UniTask.WhenAll(songQueue);
        task.Forget();

        await UniTask.SwitchToMainThread();

        // We only want to update our song list whenever new songs make it into the queue
        while (task.Status is UniTaskStatus.Pending || !loadedSongs.IsEmpty)
        {
            if (loadedSongs.TryDequeue(out var queuedSong))
            {
                Songs.Add(queuedSong);

                UpdateSongList();
            }

            await UniTask.Yield();
        }
    }

    private async UniTask LoadSongInternal(DirectoryInfo dir, ConcurrentQueue<BeatSaberSong> songs)
    {
        var song = await BeatSaberSong.GetSongFromFolderAsync(dir.FullName);

        if (song == null)
        {
            Debug.LogWarning($"No song at location {dir} exists! Is it in a subfolder?");
            return;
        }

        songs.Enqueue(song);
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
