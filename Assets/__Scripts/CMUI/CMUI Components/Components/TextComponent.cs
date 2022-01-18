using TMPro;
using UnityEngine;

public class TextComponent : CMUIComponent<string>
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    private void Start() => OnValueUpdated(Value);

    protected override void OnValueUpdated(string updatedValue) => textMeshProUGUI.text = updatedValue;
}
