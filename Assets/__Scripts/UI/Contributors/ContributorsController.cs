using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

[ExecuteAlways]
public class ContributorsController : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset oddFontColor;
    [SerializeField] private TMP_FontAsset evenFontColor;

    [SerializeField] private GameObject listContainer;
    [SerializeField] private GameObject listItemPrefab;
    [SerializeField] private ContributorsEditController editController;

    private readonly List<ContributorListItem> items = new List<ContributorListItem>();
    private readonly List<MapContributor> contributors = new List<MapContributor>();

    // Start is called before the first frame update
    void Start()
    {
        if (!Application.IsPlaying(gameObject))
        {
            // Render 12 example objects in the editor
            for (int i = 0; i < 12; i++)
            {
                GameObject listItem = Instantiate(listItemPrefab, listContainer.transform);
                listItem.hideFlags = HideFlags.HideAndDontSave;
            }
            return;
        }

        foreach (MapContributor item in BeatSaberSongContainer.Instance.song.contributors)
        {
            ContributorListItem listItem = Instantiate(listItemPrefab, listContainer.transform).GetComponent<ContributorListItem>();
            listItem.Setup(item, this, editController);
            contributors.Add(item);
            items.Add(listItem);
        }
        items[0].SelectContributorForEditing();
        UpdateColors();
    }

    public void RemoveContributor(ContributorListItem item)
    {
        items.Remove(item);
        Destroy(item.gameObject);
        contributors.Remove(item.Contributor);
        editController.SelectContributorForEditing(null);
        UpdateColors();
    }

    private void UpdateColors()
    {
        if (Settings.Instance.DarkTheme) return;

        bool even = false;
        foreach (ContributorListItem item in items)
        {
            TextMeshProUGUI[] textObjects = item.gameObject.GetComponentsInChildren<TextMeshProUGUI>();
            textObjects.First(it => it.gameObject.name.Equals("Title")).font = even ? evenFontColor : oddFontColor;
            even = !even;
        }
    }

    public void RemoveAllContributors()
    {
        PersistentUI.Instance.ShowDialogBox("Are you sure you want to remove all contributors?", HandleRemoveAllContributors,
            PersistentUI.DialogBoxPresetType.YesNo);
    }

    public void AddNewContributor()
    {
        MapContributor contributor = new MapContributor("New Contributor", "", "");
        ContributorListItem listItem = Instantiate(listItemPrefab, listContainer.transform).GetComponent<ContributorListItem>();
        listItem.Setup(contributor, this, editController);
        contributors.Add(contributor);
        items.Add(listItem);
        listItem.SelectContributorForEditing();
        UpdateColors();
    }

    public void ExitContributorsScreen()
    {
        SceneTransitionManager.Instance.LoadScene(2);
    }

    public void SaveAndExit()
    {
        BeatSaberSongContainer.Instance.song.contributors = contributors;
        BeatSaberSongContainer.Instance.song.SaveSong();
        ExitContributorsScreen();
    }

    private void HandleRemoveAllContributors(int res)
    {
        if (res > 0) return;

        foreach (ContributorListItem item in items) {
            Destroy(item.gameObject);
        }
        items.Clear();
        contributors.Clear();
        editController.SelectContributorForEditing(null);
    }
}
