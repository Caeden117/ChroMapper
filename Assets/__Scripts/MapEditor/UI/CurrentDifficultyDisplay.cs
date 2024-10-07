using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrentDifficultyDisplay : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        var info = BeatSaberSongContainer.Instance.Info;

        string songStr = (info.SongSubName != "")
            ? $"{info.SongAuthorName} - {info.SongName} {info.SongSubName}\n"
            : $"{info.SongAuthorName} - {info.SongName}\n";

        string diffStr = (!string.IsNullOrWhiteSpace(infoDifficulty.CustomLabel))
            ? $"{infoDifficulty.CustomData["_difficultyLabel"].Value} - [{infoDifficulty.Difficulty}]"
            : $"[{infoDifficulty.Difficulty}]";

        string display = "";

        if (Settings.Instance.DisplaySongDetailsInEditor)
            display += songStr;
        if (Settings.Instance.DisplayDiffDetailsInEditor)
            display += diffStr;

        textMesh.text = display;
    }
}
