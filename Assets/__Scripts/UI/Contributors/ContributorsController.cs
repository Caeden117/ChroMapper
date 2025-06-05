using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Info;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ContributorsController : MonoBehaviour
{
    [SerializeField] private GameObject listContainer;
    [SerializeField] private GameObject listItemPrefab;
    public readonly List<BaseContributor> Contributors = new List<BaseContributor>();

    private readonly List<ContributorListItem> items = new List<ContributorListItem>();

    // Start is called before the first frame update
    private void Start()
    {
    #if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            // Render 6 example objects in the editor
            for (var i = 0; i < 6; i++)
            {
                var listItem = Instantiate(listItemPrefab, listContainer.transform);
                listItem.hideFlags = HideFlags.HideAndDontSave;
            }

            return;
        }
    #endif

        transform.parent.gameObject.SetActive(false);

        UndoChanges();
    }

    public void UndoChanges()
    {
        HandleRemoveAllContributors(0);

        foreach (var item in BeatSaberSongContainer.Instance.Info.CustomContributors)
        {
            var listItem = Instantiate(listItemPrefab, listContainer.transform).GetComponent<ContributorListItem>();
            listItem.Setup(item, this);
            Contributors.Add(item);
            items.Add(listItem);
        }
    }

    public void RemoveContributor(ContributorListItem item)
    {
        items.Remove(item);
        Destroy(item.gameObject);
        Contributors.Remove(item.Contributor);
    }

    public void RemoveAllContributors() =>
        PersistentUI.Instance.ShowDialogBox("Contributors", "removeall", HandleRemoveAllContributors,
            PersistentUI.DialogBoxPresetType.YesNo);

    public void AddNewContributor()
    {
        var contributor = new BaseContributor("", "", "");
        var listItem = Instantiate(listItemPrefab, listContainer.transform).GetComponent<ContributorListItem>();
        listItem.Setup(contributor, this, true);
        Contributors.Add(contributor);
        items.Add(listItem);
        StartCoroutine(WaitToScroll());
    }

    public IEnumerator WaitToScroll()
    {
        yield return new WaitForEndOfFrame();
        listContainer.GetComponentInParent<ScrollRect>().normalizedPosition = new Vector2(0, 0);
    }

    private void HandleRemoveAllContributors(int res)
    {
        if (res > 0) return;

        foreach (var item in items) Destroy(item.gameObject);
        items.Clear();
        Contributors.Clear();
    }

    public bool IsDirty() => items.Any(it => it.Dirty);

    public void Commit()
    {
        foreach (var i in items) i.Commit();
    }
}
