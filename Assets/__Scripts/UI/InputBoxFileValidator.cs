using SFB;
using System.Collections;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputBoxFileValidator : MonoBehaviour
{
    [SerializeField] private GameObject inputMask;
    [SerializeField] private Image validationImg;
    [SerializeField] private TMP_InputField input;

    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Color goodColor;
    [SerializeField] private Sprite badSprite;
    [SerializeField] private Color badColor;

    [SerializeField] private string filetypeName;
    [SerializeField] private string[] extensions;

    [SerializeField] private bool enableValidation = false;

    private Vector2 startOffset;

    public void Awake()
    {
        var transform = inputMask.GetComponent<RectTransform>();
        startOffset = transform.offsetMax;
        // This will get un-done on start, but will stop negative text scroll
        // Shouldn't really be in awake but it needs to run before SongInfoEditUI sets the text value
        transform.offsetMax = new Vector2(startOffset.x - 36, startOffset.y);
    }

    public void Start()
    {
        OnUpdate();
    }

    public void OnUpdate()
    {
        BeatSaberSong song = BeatSaberSongContainer.Instance?.song;

        string filename = input.text;
        if (!enableValidation || filename.Length == 0 || song?.directory == null)
        {
            SetValidationState(false);
            return;
        }

        string path = Path.Combine(song.directory, filename);
        SetValidationState(true, File.Exists(path));
    }

    public void SetValidationState(bool visible, bool state = false)
    {
        if (!visible)
        {
            validationImg.gameObject.SetActive(false);
            return;
        }

        validationImg.gameObject.SetActive(true);

        if (state)
        {
            validationImg.sprite = goodSprite;
            validationImg.color = goodColor;
            return;
        }

        validationImg.sprite = badSprite;
        validationImg.color = badColor;
    }

    public void BrowserForFile()
    {
        var exts = new[] {
            new ExtensionFilter(filetypeName, extensions),
            new ExtensionFilter("All Files", "*"),
        };

        if (BeatSaberSongContainer.Instance.song is null || BeatSaberSongContainer.Instance.song.directory is null)
        {
            PersistentUI.Instance.ShowDialogBox("Cannot locate song directory. Did you forget to save your map?", null, PersistentUI.DialogBoxPresetType.Ok);
            OnUpdate();
            return;
        }

        string songDir = BeatSaberSongContainer.Instance.song.directory;
        CMInputCallbackInstaller.DisableActionMaps(typeof(InputBoxFileValidator), new[] { typeof(CMInput.IMenusExtendedActions) });
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", songDir, exts, false);
        StartCoroutine(ClearDisabledActionMaps());
        if (paths.Length > 0)
        {
            DirectoryInfo directory = new DirectoryInfo(songDir);
            FileInfo file = new FileInfo(paths[0]);

            string fullDirectory = directory.FullName;
            string fullFile = file.FullName;
#if UNITY_STANDALONE_WIN
            bool ignoreCase = true;
#else
            bool ignoreCase = false;
#endif

            if (!fullFile.StartsWith(fullDirectory, ignoreCase, CultureInfo.InvariantCulture))
            {
                if (FileExistsAlready(songDir, file.Name)) return;

                PersistentUI.Instance.ShowDialogBox("SongEditMenu", "files.badpath", result =>
                {
                    if (FileExistsAlready(songDir, file.Name)) return;

                    if (result == 0)
                    {
                        File.Copy(fullFile, Path.Combine(songDir, file.Name));
                        input.text = file.Name;
                        OnUpdate();
                    }
                }, PersistentUI.DialogBoxPresetType.YesNo);
            }
            else
            {
                input.text = fullFile.Substring(fullDirectory.Length + 1);
                OnUpdate();
            }
        }
    }

    private IEnumerator ClearDisabledActionMaps()
    {
        yield return new WaitForEndOfFrame();
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(InputBoxFileValidator), new[] { typeof(CMInput.IMenusExtendedActions) });
    }

    private bool FileExistsAlready(string songDir, string fileName)
    {
        string newFile = Path.Combine(songDir, fileName);

        if (!File.Exists(newFile)) return false;

        PersistentUI.Instance.ShowDialogBox("SongEditMenu", "files.conflict", result =>
        {
            if (result == 0)
            {
                input.text = fileName;
                OnUpdate();
            }
        }, PersistentUI.DialogBoxPresetType.YesNo);

        return true;
    }
}
