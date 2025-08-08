using System;
using System.Collections;
using System.Globalization;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace __Scripts.UI.SongEditMenu
{
    public class ImageBrowser : MonoBehaviour
    {
        private static IEnumerator ClearDisabledActionMaps()
        {
            yield return new WaitForEndOfFrame();
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(ImageBrowser),
                new[] { typeof(CMInput.IMenusExtendedActions) });
        }

        public void BrowseForImage(Action<string> callback)
        {
            var extensions = new[]
            {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
                new ExtensionFilter("All Files", "*")
            };

            var songDir = BeatSaberSongContainer.Instance.Info.Directory;
            CMInputCallbackInstaller.DisableActionMaps(typeof(ImageBrowser),
                new[] { typeof(CMInput.IMenusExtendedActions) });

            string[] paths;
            try
            {
                paths = StandaloneFileBrowser.OpenFilePanel("Open File", songDir, extensions, false);
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
                    if (FileExistsAlready(callback, songDir, file.Name)) return;

                    PersistentUI.Instance.ShowDialogBox("SongEditMenu", "files.badpath", result =>
                    {
                        if (FileExistsAlready(callback, songDir, file.Name)) return;

                        if (result == 0)
                        {
                            File.Copy(fullFile, Path.Combine(songDir, file.Name));
                            callback(file.Name);
                        }
                    }, PersistentUI.DialogBoxPresetType.YesNo);
                }
                else
                {
                    callback(fullFile.Substring(fullDirectory.Length + 1));
                }
            }
        }
        
        private bool FileExistsAlready(Action<string> callback, string songDir, string fileName)
        {
            var newFile = Path.Combine(songDir, fileName);

            if (!File.Exists(newFile)) return false;

            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "files.conflict", result =>
            {
                if (result == 0) callback(fileName);
            }, PersistentUI.DialogBoxPresetType.YesNo);

            return true;
        }

        public IEnumerator LoadImageIntoSprite(string relativeImagePath, Image image, bool isOverride)
        {
            var location = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, relativeImagePath);

            var uriPath = Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                ? Uri.EscapeDataString(location)
                : Uri.EscapeUriString(location);

            var request = UnityWebRequestTexture.GetTexture($"file:///{uriPath}");

            yield return request.SendWebRequest();

            var tex = DownloadHandlerTexture.GetContent(request);

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);

            if (isOverride)
                image.overrideSprite = sprite;
            else
                image.sprite = sprite;
        }
    }
}
