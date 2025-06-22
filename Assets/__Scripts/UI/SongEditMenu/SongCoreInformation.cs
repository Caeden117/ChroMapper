using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SongCoreInformation : MonoBehaviour
{
    [SerializeField] private GameObject listContainer;
    [SerializeField] private GameObject listItemPrefab;

    [SerializeField] private DifficultySelect difficultySelect;

    // SongCore warnings have the same structure as information so slap this on
    [SerializeField] private bool isWarning;

    private readonly List<SongCoreInformationListItem> songCoreInfoListItems = new();
    public List<string> InfoList => songCoreInfoListItems.Select(it => it.Value).ToList();

    public void AddItem()
    {
        AddItem("");
        UpdateSongCoreInfo();
        StartCoroutine(WaitToScroll());
    }

    private void AddItem(string text)
    {
        var obj = Instantiate(listItemPrefab, listContainer.transform).GetComponent<SongCoreInformationListItem>();
        obj.Setup(this, text);
        songCoreInfoListItems.Add(obj);
    }

    public void Remove(SongCoreInformationListItem listItem)
    {
        songCoreInfoListItems.Remove(listItem);
        Destroy(listItem.gameObject);
        UpdateSongCoreInfo();
    }

    public IEnumerator WaitToScroll(int y = 0)
    {
        yield return new WaitForEndOfFrame();
        listContainer.GetComponentInParent<ScrollRect>().normalizedPosition = new Vector2(0, y);
    }

    public void ClearList()
    {
        foreach (var o in songCoreInfoListItems) Destroy(o.gameObject);

        songCoreInfoListItems.Clear();
    }

    public void UpdateFromDiff(List<string> localSongCoreInfos)
    {
        ClearList();

        foreach (var ent in localSongCoreInfos) AddItem(ent);

        if (gameObject.activeInHierarchy)
            StartCoroutine(WaitToScroll(1));
    }

    public void UpdateSongCoreInfo()
    {
        if (isWarning)
        {
            difficultySelect.UpdateCustomWarnings();
        }
        else
        {
            difficultySelect.UpdateSongCoreInformation();
        }
    }
}
