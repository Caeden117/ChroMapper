using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SearchableSection : MonoBehaviour
{
    [SerializeField] private List<SearchableOption> options;

    public void RegisterOption(SearchableOption option)
    {
        options.Add(option);
    }

    public bool UpdateSearch(string text)
    {
        var result = options.Select(it => it.UpdateSearch(text)).ToList().Any(it => it);
        gameObject.SetActive(result);
        return result;
    }
}