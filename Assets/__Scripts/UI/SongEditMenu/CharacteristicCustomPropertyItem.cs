using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Beatmap.Info;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CharacteristicCustomPropertyItem : MonoBehaviour
{
    private CharacteristicCustomPropertyController controller;
    private string characteristic;
    
    [SerializeField] public Image Image;
    [SerializeField] public TMP_InputField CustomNameField;
    public string iconImageFileName = "";
    
    private string initialCustomName;
    private string initialImageFileName;
    
    public void Setup(CharacteristicCustomPropertyController controller, string characteristic, Sprite originalIconSprite)
    {
        this.controller = controller;
        
        // Setup placeholder text to match the characteristic name
        this.characteristic = characteristic;
        CustomNameField.placeholder.GetComponent<TMP_Text>().text = characteristic;

        // Setup default sprite to be the characteristic icon
        Image.sprite = originalIconSprite;
        
        // Fill in custom characteristic data if present
        var difficultySet = BeatSaberSongContainer.Instance.Info.DifficultySets.Find(x => x.Characteristic == this.characteristic);
        initialCustomName = CustomNameField.text = difficultySet?.CustomCharacteristicLabel ?? "";
        initialImageFileName = iconImageFileName = difficultySet?.CustomCharacteristicIconImageFileName ?? "";
    }

    public bool IsDirty() => CustomNameField.text != initialCustomName || iconImageFileName != initialImageFileName;
    
    public void CommitToInfo()
    {
        var difficultySet = BeatSaberSongContainer.Instance.Info.DifficultySets.Find(x => x.Characteristic == characteristic);
        if (difficultySet == null)
        {
            difficultySet = new InfoDifficultySet { Characteristic = characteristic };
            BeatSaberSongContainer.Instance.Info.DifficultySets.Add(difficultySet);
        }
        
        initialCustomName = difficultySet.CustomCharacteristicLabel = CustomNameField.text;
        initialImageFileName = difficultySet.CustomCharacteristicIconImageFileName = iconImageFileName;
    }

    public void Clear()
    {
        CustomNameField.text = null;
        iconImageFileName = "";
        Image.overrideSprite = null;
        
        controller.ReplaceCharacteristicIcon(characteristic);
    }
    
    
    public void BrowseForImage()
    {
        var extensions = new[]
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"), new ExtensionFilter("All Files", "*")
        };

        var songDir = BeatSaberSongContainer.Instance.Info.Directory;
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
        iconImageFileName = path;
        StartCoroutine(LoadImage());
    }

    private IEnumerator LoadImage()
    {
        var location = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, iconImageFileName);

        var uriPath = Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
            ? Uri.EscapeDataString(location)
            : Uri.EscapeUriString(location);
        
        var request = UnityWebRequestTexture.GetTexture($"file:///{uriPath}");
        
        yield return request.SendWebRequest();
        
        var tex = DownloadHandlerTexture.GetContent(request);
        Image.overrideSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);

        controller.ReplaceCharacteristicIcon(characteristic);
    }
}
