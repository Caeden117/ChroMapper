using TMPro;
using UnityEngine;

public class CreateSupporterList : MonoBehaviour
{
    [SerializeField] private PatreonSupporters supporters;
    [SerializeField] private TextMeshProUGUI prefab;

    // Start is called before the first frame update
    private void Start()
    {
        foreach (var supporter in supporters.GetAllSupporters())
        {
            var instantiate = Instantiate(prefab.gameObject, transform).GetComponent<TextMeshProUGUI>();
            instantiate.text = supporter;
            if (supporters.HighTierPatrons.Contains(supporter)) instantiate.color = Color.cyan;
        }

        prefab.gameObject.SetActive(false);
    }
}
