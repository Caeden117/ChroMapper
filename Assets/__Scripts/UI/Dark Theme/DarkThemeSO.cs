using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "DarkThemeSO", menuName = "Map/Dark Theme SO")]
public class DarkThemeSO : ScriptableObject
{
    [FormerlySerializedAs("BeonReplacement")] [SerializeField] private TMP_FontAsset beonReplacement;
    public TMP_FontAsset TekoReplacement;
    [FormerlySerializedAs("BeonUnityReplacement")] [SerializeField] private Font beonUnityReplacement;
    [FormerlySerializedAs("TekoUnityReplacement")] [SerializeField] private Font tekoUnityReplacement;

    public void DarkThemeifyUI()
    {
        if (!Settings.Instance.DarkTheme) return;
        foreach (var jankCodeMate in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (jankCodeMate == null || jankCodeMate.font == null) continue;

            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = beonReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = TekoReplacement;
        }

        foreach (var jankCodeMate in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (jankCodeMate == null || jankCodeMate.font == null) continue;

            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = beonUnityReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = tekoUnityReplacement;
        }
    }
}
