using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputBoxFileValidator : MonoBehaviour
{
    [SerializeField] private GameObject inputMask;
    [SerializeField] private GameObject validationImg;

    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Color goodColor;
    [SerializeField] private Sprite badSprite;
    [SerializeField] private Color badColor;

    private Vector2 startOffset;

    public void Awake()
    {
        var transform = inputMask.GetComponent<RectTransform>();
        startOffset = transform.offsetMax;
        // This will get un-done on start, but will stop negative text scroll
        // Shouldn't really be in awake but it needs to run before SongInfoEditUI sets the text value
        transform.offsetMax = new Vector2(startOffset.x - 16, startOffset.y);
    }

    public void Start()
    {
        OnUpdate();
    }

    public void OnUpdate()
    {
        BeatSaberSong song = BeatSaberSongContainer.Instance?.song;

        var input = GetComponent<TMP_InputField>();
        string filename = input.text;
        if (filename.Length == 0 || song?.directory == null)
        {
            validationImg.SetActive(false);
            return;
        }

        validationImg.SetActive(true);

        var image = validationImg.GetComponent<Image>();

        string path = Path.Combine(song.directory, filename);
        if (File.Exists(path))
        {
            image.sprite = goodSprite;
            image.color = goodColor;
        }
        else
        {
            image.sprite = badSprite;
            image.color = badColor;
        }
    }
}
