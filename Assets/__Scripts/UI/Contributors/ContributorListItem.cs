using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Beatmap.Base.Customs;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ContributorListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameText;
    [SerializeField] private TMP_InputField roleText;
    [SerializeField] private Image contributorImage;
    public BaseContributor Contributor;
    private bool dirty;
    private ContributorsController controller;
    private string imagePath = "";

    public bool Dirty
    {
        get => dirty || nameText.text != Contributor.Name || Contributor.Role != roleText.text ||
               Contributor.LocalImageLocation != imagePath;
        set => dirty = value;
    }

    public void Awake() => CheckLoadImage();

    public void Setup(BaseContributor contributor, ContributorsController contributorsControllerNew, bool dirty = false)
    {
        Contributor = contributor;
        controller = contributorsControllerNew;
        this.dirty = dirty;

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

    private void UpdateName() => nameText.text = Contributor.Name;

    public void Commit()
    {
        Contributor.Name = nameText.text;
        Contributor.Role = roleText.text;
        Contributor.LocalImageLocation = imagePath;
        Dirty = false;
    }

    public void BrowseForImage()
    {
        var extensions = new[]
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"), new ExtensionFilter("All Files", "*")
        };

        var songDir = BeatSaberSongContainer.Instance.Song.Directory;
        CMInputCallbackInstaller.DisableActionMaps(typeof(ContributorListItem),
            new[] { typeof(CMInput.IMenusExtendedActions) });
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", songDir, extensions, false);
        StartCoroutine(ClearDisabledActionMaps());
        if (paths.Length > 0)
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
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(ContributorListItem),
            new[] { typeof(CMInput.IMenusExtendedActions) });
    }

    private bool FileExistsAlready(string songDir, string fileName)
    {
        var newFile = Path.Combine(songDir, fileName);

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
        var location = Path.Combine(BeatSaberSongContainer.Instance.Song.Directory, imagePath);
        var request = UnityWebRequestTexture.GetTexture($"file:///{Uri.EscapeDataString(location)}");
        yield return request.SendWebRequest();
        var tex = DownloadHandlerTexture.GetContent(request);
        contributorImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);
    }


    public void Delete() => controller.RemoveContributor(this);
}
