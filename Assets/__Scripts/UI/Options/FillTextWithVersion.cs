using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FillTextWithVersion : MonoBehaviour
{
    private void Start()
    {
        GetComponent<TextMeshProUGUI>().text = Application.isEditor ? $"Dev - v{Application.version}" : $"v{Application.version}";
    }
}
