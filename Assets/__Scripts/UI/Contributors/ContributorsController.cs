using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

[ExecuteAlways]
public class ContributorsController : MonoBehaviour
{
    [SerializeField] private GameObject listContainer;
    [SerializeField] private GameObject listItemPrefab;

    private readonly List<ContributorListItem> items = new List<ContributorListItem>();
    public readonly List<MapContributor> contributors = new List<MapContributor>();

    // Start is called before the first frame update
    void Start()
    {
        if (!Application.IsPlaying(gameObject))
        {
            // Render 6 example objects in the editor
            for (int i = 0; i < 6; i++)
            {
                GameObject listItem = Instantiate(listItemPrefab, listContainer.transform);
                listItem.hideFlags = HideFlags.HideAndDontSave;
            }
            return;
        }
        else
        {
            transform.parent.gameObject.SetActive(false);
        }

        UndoChanges();
    }

    public void UndoChanges()
    {
        HandleRemoveAllContributors(0);

        foreach (MapContributor item in BeatSaberSongContainer.Instance.song.contributors)
        {
            ContributorListItem listItem = Instantiate(listItemPrefab, listContainer.transform).GetComponent<ContributorListItem>();
            listItem.Setup(item, this);
            contributors.Add(item);
            items.Add(listItem);
        }
    }

    public void RemoveContributor(ContributorListItem item)
    {
        items.Remove(item);
        Destroy(item.gameObject);
        contributors.Remove(item.Contributor);
    }

    public void RemoveAllContributors()
    {
        PersistentUI.Instance.ShowDialogBox("Contributors", "removeall", HandleRemoveAllContributors,
            PersistentUI.DialogBoxPresetType.YesNo);
    }

    public void AddNewContributor()
    {
        MapContributor contributor = new MapContributor("", "", "");
        ContributorListItem listItem = Instantiate(listItemPrefab, listContainer.transform).GetComponent<ContributorListItem>();
        listItem.Setup(contributor, this, true);
        contributors.Add(contributor);
        items.Add(listItem);
        StartCoroutine(WaitToScroll());
    }

    public System.Collections.IEnumerator WaitToScroll()
    {
        yield return new WaitForEndOfFrame();
        listContainer.GetComponentInParent<ScrollRect>().normalizedPosition = new Vector2(0, 0);
    }

    private void HandleRemoveAllContributors(int res)
    {
        if (res > 0) return;

        foreach (ContributorListItem item in items) {
            Destroy(item.gameObject);
        }
        items.Clear();
        contributors.Clear();
    }

    public bool IsDirty()
    {
        return items.Any(it => it.Dirty);
    }

    public void Commit()
    {
        foreach (var i in items)
        {
            i.Commit();
        }
    }
}
