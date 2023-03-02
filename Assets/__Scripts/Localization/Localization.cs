using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Localization", menuName = "Localization")]
public class Localization : ScriptableObject
{
    public bool OverwriteLocalizationText;
    public int OverwriteLocalizationTextID;

    [FormerlySerializedAs("loadingMessages")] [TextArea(3, 10)] public string[] LoadingMessages;

    public Sprite[] WaifuSprites;

    public string GetRandomLoadingMessage()
    {
        if (!Settings.Instance.HelpfulLoadingMessages) return string.Empty;
        return OverwriteLocalizationText
            ? LoadingMessages[OverwriteLocalizationTextID]
            : LoadingMessages[Random.Range(0, LoadingMessages.Length)];
    }

    public Sprite GetRandomWaifuSprite() => WaifuSprites[Random.Range(0, WaifuSprites.Length)];
}
