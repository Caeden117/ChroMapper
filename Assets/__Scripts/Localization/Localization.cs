using UnityEngine;

[CreateAssetMenu(fileName = "Localization", menuName = "Localization")]
public class Localization : ScriptableObject {

    [SerializeField]
    public bool OverwriteLocalizationText = false;

    [SerializeField]
    public int OverwriteLocalizationTextID = 0;

    [SerializeField]
    [TextArea(3, 10)]
    public string[] loadingMessages;

    public string GetRandomLoadingMessage() {
        if (!Settings.Instance.HelpfulLoadingMessages) return string.Empty;
        if (OverwriteLocalizationText)
        {
            return loadingMessages[OverwriteLocalizationTextID];
        }
        return loadingMessages[Random.Range(0, loadingMessages.Length)];
    }

}
