using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

public class ContributorsController : MonoBehaviour
{
    [SerializeField] private ContributorListItem[] items;

    private List<MapContributor> contributors;

    // Start is called before the first frame update
    void Start()
    {
        contributors = new List<MapContributor>(BeatSaberSongContainer.Instance.song.contributors);
        RefreshContributorList();
    }

    public void RefreshContributorList()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (i < contributors.Count)
            {
                items[i].SetContributorData(contributors[i]);
            }
            else
            {
                items[i].SetContributorData(null);
            }
        }
    }

    public void RefreshContributors()
    {
        contributors = new List<MapContributor>();
        foreach (ContributorListItem item in items)
        {
            if (item.Contributor != null) contributors.Add(item.Contributor);
        }
        RefreshContributorList();
    }

    public void RemoveContributor(MapContributor contributor)
    {
        contributors.Remove(contributor);
        RefreshContributorList();
    }

    public void RemoveAllContributors()
    {
        PersistentUI.Instance.ShowDialogBox("Are you sure you want to remove all contributors?", HandleRemoveAllContributors,
            PersistentUI.DialogBoxPresetType.YesNo);
    }

    public void AddNewContributor()
    {
        contributors.Add(new MapContributor("New Contributor", "", ""));
        RefreshContributorList();
    }

    public void ExitContributorsScreen()
    {
        SceneTransitionManager.Instance.LoadScene(2);
    }

    public void SaveAndExit()
    {
        RefreshContributors();
        BeatSaberSongContainer.Instance.song.contributors = contributors;
        BeatSaberSongContainer.Instance.song.SaveSong();
        ExitContributorsScreen();
    }

    private void HandleRemoveAllContributors(int res)
    {
        if (res > 0) return;
        contributors = new List<MapContributor>();
        RefreshContributorList();
    }
}
