using UnityEngine;
using UnityEngine.UI;
using TMPro;


[CreateAssetMenu(fileName = "DarkThemeSO", menuName = "Map/Dark Theme SO")]
public class DarkThemeSO : ScriptableObject
{
    [SerializeField] private TMP_FontAsset BeonReplacement;
    public TMP_FontAsset TekoReplacement;
    [SerializeField] private Font BeonUnityReplacement;
    [SerializeField] private Font TekoUnityReplacement;

    public void DarkThemeifyUI()
    {
        if (!Settings.Instance.DarkTheme) return;
        foreach (TextMeshProUGUI jankCodeMate in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()) {
            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = BeonReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = TekoReplacement;
        }
        foreach (Text jankCodeMate in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = BeonUnityReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = TekoUnityReplacement;
        }
    }
}
