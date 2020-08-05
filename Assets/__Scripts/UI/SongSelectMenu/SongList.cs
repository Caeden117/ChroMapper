using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class SongList : MonoBehaviour {

    private static int lastVisitedPage = 0;
    private static bool lastVisited_WasWIP = true;

    [SerializeField]
    InputField searchField;

    [SerializeField]
    SongListItem[] items;

    [SerializeField]
    LocalizeStringEvent pageTextString;

    [SerializeField]
    int currentPage = 0;

    [SerializeField]
    int maxPage = 0;

    // For localization
    public int CurrentPage { get { return currentPage + 1; } }
    public int MaxPage { get { return maxPage + 1; } }

    [SerializeField]
    public List<BeatSaberSong> songs = new List<BeatSaberSong>();

    [SerializeField]
    Button firstButton;
    [SerializeField]
    Button prevButton;
    [SerializeField]
    Button nextButton;
    [SerializeField]
    Button lastButton;
    [SerializeField]
    LocalizeStringEvent songLocationToggleText;

    public bool WIPLevels = true;
    public bool FilteredBySearch = false;

    private void Start()
    {
        WIPLevels = lastVisited_WasWIP;
        RefreshSongList(false);
    }

    public void ToggleSongLocation()
    {
        WIPLevels = !WIPLevels;
        lastVisited_WasWIP = WIPLevels;
        RefreshSongList(true);
    }

    public void RefreshSongList(bool search) {
        songLocationToggleText.StringReference.TableEntryReference = WIPLevels ? "custom" : "wip";

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
        songs = songs.OrderBy(x => x.songName).ToList();
        maxPage = Mathf.Max(0, Mathf.CeilToInt((songs.Count - 1) / items.Length));
        SetPage(lastVisitedPage);
    }

    public void SetPage(int page) {
        if (page < 0 || page > maxPage)
        {
            page = 0;
        }
        lastVisitedPage = page;
        currentPage = page;
        LoadPage();
        pageTextString.StringReference.RefreshString();


        firstButton.interactable = currentPage != 0;
        prevButton.interactable = currentPage - 1 >= 0;
        nextButton.interactable = currentPage + 1 <= maxPage;
        lastButton.interactable = currentPage != maxPage;
    }

    public void LoadPage() {
        int offset = currentPage * items.Length;
        for (int i = 0; i < 10; i++) { // && (i + offset) < songs.Count; i++) {
            if (items[i] == null) continue;
            if (i + offset < songs.Count) {
                string name = songs[i + offset].songName;
                if (searchField.text != "" && FilteredBySearch)
                {
                    List<int> index = name.AllIndexOf(searchField.text).ToList();
                    if (index.Any())
                    {
                        string newName = name.Substring(0, index.First());
                        int length = searchField.text.Length;
                        for (int j = 0; j < index.Count(); j++)
                        {
                            newName += $"<u>{name.Substring(index[j], length)}</u>";
                            if (j != index.Count() - 1)
                                newName += $"{name.Substring(index[j] + length, index[j + 1] - (index[j] + length))}";
                            else break;
                        }
                        int lastIndex = index.Last() + (length - 1);
                        name = newName + name.Substring(lastIndex + 1, name.Length - lastIndex - 1);
                    }
                }
                items[i].gameObject.SetActive(true);
                items[i].AssignSong(songs[i + offset]);
            } else items[i].gameObject.SetActive(false);
        }
    }

    public void FirstPage() {
        SetPage(0);
    }

    public void PrevPage() {
        SetPage(currentPage - 1);
    }

    public void NextPage() {
        SetPage(currentPage + 1);
    }

    public void LastPage() {
        SetPage(maxPage);
    }

}
