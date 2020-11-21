using SFB;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ContributorListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameText;
    [SerializeField] private TMP_InputField roleText;
    [SerializeField] private Image contributorImage;
    public MapContributor Contributor = null;
    private ContributorsController controller;
    private string imagePath = "";
    private bool _dirty = false;
    public bool Dirty
    {
        get
        {
            return _dirty || nameText.text != Contributor.Name || Contributor.Role != roleText.text || Contributor.LocalImageLocation != imagePath;
        }
        set
        {
            _dirty = value;
        }
    }

    public void Awake()
    {
        CheckLoadImage();
    }

    public void Setup(MapContributor contributor, ContributorsController contributorsControllerNew, bool dirty = false)
    {
        Contributor = contributor;
        controller = contributorsControllerNew;
        _dirty = dirty;

        nameText.text = Contributor.Name;
        roleText.text = Contributor.Role;
        imagePath = Contributor.LocalImageLocation;

        if (gameObject.activeInHierarchy)
            CheckLoadImage();

        UpdateName();
    }

    private void CheckLoadImage()
    {
        if (!string.IsNullOrWhiteSpace(imagePath))
            StartCoroutine(LoadImage());
    }

    private void UpdateName()
    {
        nameText.text = Contributor.Name;
    }

    public void Commit()
    {
        Contributor.Name = nameText.text;
        Contributor.Role = roleText.text;
        Contributor.LocalImageLocation = imagePath;
        Dirty = false;
    }

    public void BrowseForImage()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
            new ExtensionFilter("All Files", "*" ),
        };

        string songDir = BeatSaberSongContainer.Instance.song.directory;
        CMInputCallbackInstaller.DisableActionMaps(new[] { typeof(CMInput.IMenusExtendedActions) });
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", songDir, extensions, false);
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
                        SetImageLocation(file.Name);
                    }
                }, PersistentUI.DialogBoxPresetType.YesNo);
            }
            else
            {
                SetImageLocation(fullFile.Substring(fullDirectory.Length + 1));
            }
        }
    }

    private IEnumerator ClearDisabledActionMaps()
    {
        yield return new WaitForEndOfFrame();
        CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.IMenusExtendedActions) });
    }

    private bool FileExistsAlready(string songDir, string fileName)
    {
        string newFile = Path.Combine(songDir, fileName);

        if (!File.Exists(newFile)) return false;

        PersistentUI.Instance.ShowDialogBox("SongEditMenu", "files.conflict", result =>
        {
            if (result == 0) SetImageLocation(fileName);
        }, PersistentUI.DialogBoxPresetType.YesNo);

        return true;
    }

    private void SetImageLocation(string path)
    {
        imagePath = path;
        StartCoroutine(LoadImage());
    }

    private IEnumerator LoadImage()
    {
        string location = Path.Combine(BeatSaberSongContainer.Instance.song.directory, imagePath);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture($"file:///{Uri.EscapeDataString(location)}");
        yield return request.SendWebRequest();
        Texture2D tex = DownloadHandlerTexture.GetContent(request);
        contributorImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);
    }


    public void Delete()
    {
        controller.RemoveContributor(this);
    }
}
