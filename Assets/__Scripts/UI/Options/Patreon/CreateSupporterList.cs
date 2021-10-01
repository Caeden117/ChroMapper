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

            if (supporter.Contains("Zyxi"))
            {
                SneakButton(instantiate.gameObject, 1);
            }
        }

        prefab.gameObject.SetActive(false);
    }

    private void SneakButton(GameObject gameObject, int bongoId)
    {
        var child = new GameObject("button", typeof(RectTransform));
        child.transform.SetParent(gameObject.transform);

        var childTransform = child.transform as RectTransform;
        childTransform.localScale = Vector3.one;
        childTransform.anchoredPosition = Vector2.zero;
        childTransform.anchorMin = Vector2.zero;
        childTransform.anchorMax = Vector2.one;

        var image = child.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1, 1, 1, 0);

        var button = child.AddComponent<UnityEngine.UI.Button>();

        var nav = button.navigation;
        nav.mode = UnityEngine.UI.Navigation.Mode.None;
        button.navigation = nav;

        button.onClick.AddListener(() => button.gameObject.GetComponentInParent<OptionsController>().ToggleBongo(bongoId));
    }
}
