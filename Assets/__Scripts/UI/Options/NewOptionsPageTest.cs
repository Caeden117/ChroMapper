using UnityEngine;
using TMPro;

public class NewOptionsPageTest : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset asset;
    [SerializeField] private OptionsController controller;
    void Start()
    {
        controller.AddSettingsPage("Mr Kiwi Man Please Consider", asset);
    }
}
