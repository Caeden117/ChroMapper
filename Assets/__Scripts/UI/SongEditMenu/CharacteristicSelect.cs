using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacteristicSelect : MonoBehaviour
{
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private DifficultySelect difficultySelect;
    private Transform selected;

    BeatSaberSong Song
    {
        get { return BeatSaberSongContainer.Instance?.song; }
    }

    public void Start()
    {
        foreach (Transform child in transform)
        {
            Recalculate(child);

            var button = child.GetComponent<Button>();
            button.onClick.AddListener(() => OnClick(child));

            if (selected == null)
            {
                OnClick(child);
            }
        }
    }

    private void OnClick(Transform obj)
    {
        if (selected != null)
        {
            var selectedImage = selected.GetComponent<Image>();
            selectedImage.color = normalColor;
        }

        selected = obj;
        var image = selected.GetComponent<Image>();
        image.color = selectedColor;
        difficultySelect.SetCharacteristic(obj.name);
    }

    private void Recalculate(Transform transform)
    {
        var diff = Song?.difficultyBeatmapSets?.Find(it => it.beatmapCharacteristicName.Equals(transform.name, StringComparison.InvariantCultureIgnoreCase));
        var count = diff != null ? diff.difficultyBeatmaps.Count : 0;

        var diffCountText = transform.Find("Difficulty Count").GetComponent<TMP_Text>();
        diffCountText.text = count.ToString();
    }

    public void Recalculate()
    {
        Recalculate(selected);
    }
}