using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrentDifficultyDisplay : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        var info = BeatSaberSongContainer.Instance.Info;

        string songStr = (info.SongSubName != "")
            ? $"{info.SongAuthorName} - {info.SongName} {info.SongSubName}\n"
            : $"{info.SongAuthorName} - {info.SongName}\n";

        string display = "";

        if (Settings.Instance.DisplaySongDetailsInEditor)
            display += songStr;

        textMesh.text = display;
    }
}
