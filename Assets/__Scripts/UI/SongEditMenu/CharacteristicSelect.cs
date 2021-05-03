using System;
using System.Collections.Generic;
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

            if (selected == null || (Settings.Instance.LastLoadedMap.Equals(Song.directory) && Settings.Instance.LastLoadedChar.Equals(child.name)))
            {
                OnClick(child, true);
            }
        }
    }

    private void OnClick(Transform obj, bool firstLoad = false)
    {
        if (selected != null)
        {
            var selectedImage = selected.GetComponent<Image>();
            selectedImage.color = normalColor;
        }

        selected = obj;
        var image = selected.GetComponent<Image>();
        image.color = selectedColor;
        difficultySelect.SetCharacteristic(obj.name, firstLoad);
    }

    private void Recalculate(Transform transform)
    {
        difficultySelect.Characteristics.TryGetValue(transform.name, out Dictionary<string, DifficultySettings> diff);
        var count = diff != null ? diff.Count : 0;

        var diffCountText = transform.Find("Difficulty Count").GetComponent<TMP_Text>();
        diffCountText.text = count.ToString();
    }

    public void Recalculate()
    {
        Recalculate(selected);
    }
}