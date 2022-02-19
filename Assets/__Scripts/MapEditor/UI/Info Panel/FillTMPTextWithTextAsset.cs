using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FillTMPTextWithTextAsset : MonoBehaviour
{
    [SerializeField] private TextAsset textAsset;

    // Start is called before the first frame update
    private void Start() => GetComponent<TextMeshProUGUI>().text = textAsset.text;
}
