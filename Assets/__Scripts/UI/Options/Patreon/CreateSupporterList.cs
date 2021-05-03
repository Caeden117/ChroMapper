using TMPro;
using UnityEngine;

public class CreateSupporterList : MonoBehaviour
{
    [SerializeField] private PatreonSupporters supporters;
    [SerializeField] private TextMeshProUGUI prefab;

    // Start is called before the first frame update
    void Start()
    {
        foreach (string supporter in supporters.GetAllSupporters())
        {
            TextMeshProUGUI instantiate = Instantiate(prefab.gameObject, transform).GetComponent<TextMeshProUGUI>();
            instantiate.text = supporter;
            if (supporters.HighTierPatrons.Contains(supporter)) instantiate.color = Color.cyan;
        }
        prefab.gameObject.SetActive(false);
    }
}
