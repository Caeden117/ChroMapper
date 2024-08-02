using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
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

    private readonly List<EnvRemovalListItem> envRemovalList = new List<EnvRemovalListItem>();
    public List<BaseEnvironmentEnhancement> EnvRemovalList => envRemovalList.Select(it => it.Value).ToList();

    public void ToggleEnvRemoval()
    {
        var oldActive = envRemovalContainer.activeSelf;
        envRemovalContainer.SetActive(!oldActive);
        diffInfoContainer.SetActive(oldActive);
        envRemovalToggle.enabled = !oldActive;
        envRemovalToggle.transform.localPosition += (oldActive)
            ? new Vector3(-152, 0, 0)
            : new Vector3(152, 0, 0);
    }

    public void AddItem()
    {
        var newItem = Settings.Instance.MapVersion == 3
            ? (BaseEnvironmentEnhancement)new V3EnvironmentEnhancement("")
            : new V2EnvironmentEnhancement("");
        newItem.Active = false;
        AddItem(newItem);
        UpdateEnvRemoval();
        StartCoroutine(WaitToScroll());
    }

    public void AddItem(BaseEnvironmentEnhancement text)
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

    public IEnumerator WaitToScroll(int y = 0)
    {
        yield return new WaitForEndOfFrame();
        listContainer.GetComponentInParent<ScrollRect>().normalizedPosition = new Vector2(0, y);
    }

    public void ClearList()
    {
        foreach (var o in envRemovalList) Destroy(o.gameObject);

        envRemovalList.Clear();
    }

    public void UpdateFromDiff(List<BaseEnvironmentEnhancement> localEnvRemoval)
    {
        ClearList();

        foreach (var ent in localEnvRemoval) AddItem(ent);

        if (gameObject.activeInHierarchy)
            StartCoroutine(WaitToScroll(1));
    }

    public void UpdateEnvRemoval() => difficultySelect.UpdateEnvRemoval();
}
