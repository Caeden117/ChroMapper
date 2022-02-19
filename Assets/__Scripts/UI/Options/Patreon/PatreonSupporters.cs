using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Patreon Supporters", menuName = "Patreon Supporter List")]
public class PatreonSupporters : ScriptableObject
{
    public List<string> HighTierPatrons = new List<string>();
    public List<string> RegularPatrons = new List<string>();

    public void AddSupporter(string patron, bool isCmSupporter)
    {
        if (isCmSupporter)
        {
            if (HighTierPatrons.Contains(patron)) return;
            HighTierPatrons.Add(patron);
        }
        else
        {
            if (RegularPatrons.Contains(patron)) return;
            RegularPatrons.Add(patron);
        }
    }

    public IEnumerable<string> GetAllSupporters()
    {
        var supporters = new List<string>(HighTierPatrons);
        supporters.AddRange(RegularPatrons);
        return supporters.OrderBy(x => x);
    }
}
