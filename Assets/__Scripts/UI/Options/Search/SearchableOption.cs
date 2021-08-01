using System;
using System.Linq;
using UnityEngine;

public class SearchableOption : MonoBehaviour
{
    public string[] Keywords;

    private bool Matches(string text)
    {
        var parts = text.Split(' ');
        return text.Length == 0 || parts.All(part =>
            Keywords.Any(it => it.IndexOf(part, StringComparison.InvariantCultureIgnoreCase) >= 0)
        );
    }

    public bool UpdateSearch(string text)
    {
        var result = Matches(text);
        gameObject.SetActive(result);
        return result;
    }
}