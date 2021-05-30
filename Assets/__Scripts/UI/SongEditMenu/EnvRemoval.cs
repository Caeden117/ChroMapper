using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnvRemoval : MonoBehaviour
{
    [SerializeField] private GameObject diffInfoContainer;
    [SerializeField] private GameObject envRemovalContainer;

    [SerializeField] private GameObject listContainer;
    [SerializeField] private GameObject listItemPrefab;

    [SerializeField] private DifficultySelect difficultySelect;
    [SerializeField] private Image envRemovalToggle;

    private List<EnvRemovalListItem> envRemovalList = new List<EnvRemovalListItem>();
    public List<EnvEnhancement> EnvRemovalList => envRemovalList.Select(it => it.Value).ToList();

    public void ToggleEnvRemoval()
    {
        var oldActive = envRemovalContainer.activeSelf;
        envRemovalContainer.SetActive(!oldActive);
        diffInfoContainer.SetActive(oldActive);
        envRemovalToggle.enabled = !oldActive;
    }

    public void AddItem()
    {
        AddItem(new EnvEnhancement(""));
        UpdateEnvRemoval();
        StartCoroutine(WaitToScroll());
    }

    public void AddItem(EnvEnhancement text)
    {
        var obj = Instantiate(listItemPrefab, listContainer.transform).GetComponent<EnvRemovalListItem>();
        obj.Setup(this, text);
        envRemovalList.Add(obj);
    }

    public void Remove(EnvRemovalListItem item)
    {
        envRemovalList.Remove(item);
        Destroy(item.gameObject);
        UpdateEnvRemoval();
    }

    public System.Collections.IEnumerator WaitToScroll(int y = 0)
    {
        yield return new WaitForEndOfFrame();
        listContainer.GetComponentInParent<ScrollRect>().normalizedPosition = new Vector2(0, y);
    }

    public void ClearList()
    {
        foreach (var o in envRemovalList)
        {
            Destroy(o.gameObject);
        }

        envRemovalList.Clear();
    }

    public void UpdateFromDiff(List<EnvEnhancement> localEnvRemoval)
    {
        ClearList();

        foreach (var ent in localEnvRemoval)
        {
            AddItem(ent);
        }

        if (gameObject.activeInHierarchy)
            StartCoroutine(WaitToScroll(1));
    }

    public void UpdateEnvRemoval()
    {
        difficultySelect.UpdateEnvRemoval();
    }
}