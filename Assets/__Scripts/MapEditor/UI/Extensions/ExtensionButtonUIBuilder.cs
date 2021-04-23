using UnityEngine;

public class ExtensionButtonUIBuilder : MonoBehaviour
{
    [SerializeField]
    private ExtensionButtonUI buttonPrefab;

    void Awake()
    {
        ExtensionButtons.ForEachButton(BuildButton);
    }

    private void BuildButton(ExtensionButton button)
    {
        ExtensionButtonUI buttonUI = Instantiate(buttonPrefab, transform);
        buttonUI.Init(button);
    }
}
