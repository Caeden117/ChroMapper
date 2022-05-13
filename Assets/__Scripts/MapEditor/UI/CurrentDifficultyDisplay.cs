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
        BeatSaberSong song = BeatSaberSongContainer.Instance.Song;

        string songStr = (song.SongSubName != "")
            ? $"{song.SongAuthorName} - {song.SongName} {song.SongSubName}\n"
            : $"{song.SongAuthorName} - {song.SongName}\n";

        string diffStr = (data.CustomData != null && data.CustomData.HasKey("_difficultyLabel"))
            ? $"{data.CustomData["_difficultyLabel"].Value} - [{data.Difficulty}]"
            : $"[{data.Difficulty}]";

        string display = "";

        if (Settings.Instance.DisplaySongDetailsInEditor)
            display += songStr;
        if (Settings.Instance.DisplayDiffDetailsInEditor)
            display += diffStr;

        textMesh.text = display;
    }
}
