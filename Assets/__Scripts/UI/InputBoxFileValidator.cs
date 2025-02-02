using System;
using System.Collections;
using System.Globalization;
using System.IO;
using SFB;
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

    [SerializeField] private bool enableValidation;
    [SerializeField] private bool forceStartupValidationAlign;

    private Vector2 startOffset;

    public void Awake()
    {
        var transform = inputMask.GetComponent<RectTransform>();
        startOffset = transform.offsetMax;
        // This will get un-done on start, but will stop negative text scroll
        // Shouldn't really be in awake but it needs to run before SongInfoEditUI sets the text value
        var info = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Info : null;

        if (forceStartupValidationAlign || (enableValidation && Directory.Exists(info?.Directory)))
            transform.offsetMax = new Vector2(startOffset.x - 36, startOffset.y);
    }

    public void Start() => OnUpdate();

    public void OnUpdate()
    {
        var info = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Info : null;

        var filename = input.text;
        if (!enableValidation || filename.Length == 0 || !Directory.Exists(info?.Directory))
        {
            if (!forceStartupValidationAlign) SetValidationState(false);

            return;
        }

        var path = Path.Combine(info.Directory, filename);
        SetValidationState(true, File.Exists(path));
    }

    public void SetValidationState(bool visible, bool state = false)
    {
        var transform = inputMask.GetComponent<RectTransform>();
        if (!visible)
        {
            transform.offsetMax = startOffset;
            validationImg.gameObject.SetActive(false);
            return;
        }

        transform.offsetMax = new Vector2(startOffset.x - 36, startOffset.y);
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
        var exts = new[] { new ExtensionFilter(filetypeName, extensions), new ExtensionFilter("All Files", "*") };

        var isSongDirectoryMissing = BeatSaberSongContainer.Instance.Info is null 
                                     || string.IsNullOrEmpty(BeatSaberSongContainer.Instance.Info.Directory)
                                     || !Directory.Exists(BeatSaberSongContainer.Instance.Info.Directory);
        if (isSongDirectoryMissing)
        {
            PersistentUI.Instance.ShowDialogBox("Cannot locate song directory. Did you forget to save your map?", null,
                PersistentUI.DialogBoxPresetType.Ok);
            OnUpdate();
            return;
        }

        var songDir = BeatSaberSongContainer.Instance.Info.Directory;
        CMInputCallbackInstaller.DisableActionMaps(typeof(InputBoxFileValidator),
            new[] { typeof(CMInput.IMenusExtendedActions) });
        string[] paths;
        try
        {
            paths = StandaloneFileBrowser.OpenFilePanel("Open File", songDir, exts, false);
        }
        catch (DllNotFoundException)
        {
            // This seems to be an apple silicon exclusive issue
            // Try updating package later
            PersistentUI.Instance.DisplayMessage("File browser not supported on this OS",
                PersistentUI.DisplayMessageType.Bottom);
            return;
        }
        StartCoroutine(ClearDisabledActionMaps());
        if (paths is { Length: > 0 })
        {
            var directory = new DirectoryInfo(songDir);
            var file = new FileInfo(paths[0]);

            var fullDirectory = directory.FullName;
            var fullFile = file.FullName;
#if UNITY_STANDALONE_WIN
            var ignoreCase = true;
#else
            var ignoreCase = false;
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
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(InputBoxFileValidator),
            new[] { typeof(CMInput.IMenusExtendedActions) });
    }

    private bool FileExistsAlready(string songDir, string fileName)
    {
        var newFile = Path.Combine(songDir, fileName);

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
