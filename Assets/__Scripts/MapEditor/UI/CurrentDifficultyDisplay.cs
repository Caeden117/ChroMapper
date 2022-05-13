using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrentDifficultyDisplay : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        BeatSaberSong.DifficultyBeatmap data = BeatSaberSongContainer.Instance.DifficultyData;
        string diffStr = data.Difficulty;
        textMesh.text = (data.CustomData != null && data.CustomData.HasKey("_difficultyLabel"))
            ? $"{data.CustomData["_difficultyLabel"].Value} - [{diffStr}]"
            : $"[{diffStr}]";
    }
}
