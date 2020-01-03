using UnityEngine;

[CreateAssetMenu(fileName = "Localization", menuName = "Localization")]
public class Localization : ScriptableObject {

    [SerializeField]
    [TextArea(3, 10)]
    public string[] loadingMessages;

    public string GetRandomLoadingMessage() {
        return loadingMessages[Random.Range(0, loadingMessages.Length)];
    }

}
