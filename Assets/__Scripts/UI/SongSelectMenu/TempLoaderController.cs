using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class TempLoaderController : MonoBehaviour
{
    // Location to the API endpoint to download a map from its BeatSaver ID
    private const string beatSaverDownloadURL = "https://beatsaver.com/api/download/key/";

    private readonly Regex bsrBeatSaverIdRegex =
        new Regex(@"(!bsr )?(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase); 

    public void OpenTempLoader() =>
        PersistentUI.Instance.ShowInputBox(
            "SongSelectMenu", "temploader.dialog",
            TryOpenTempLoader, "temploader.dialog.default"
        );

    private void TryOpenTempLoader(string location)
    {
        if (string.IsNullOrEmpty(location) || string.IsNullOrWhiteSpace(location)) return;
        // Trim whitespace from the beginning and end of our location
        location = location.Trim();

        // Check if it is a valid BeatSaver ID
        var beatSaverId = bsrBeatSaverIdRegex.Matches(location)[0].Groups[2].Value;
        if (beatSaverId.ToCharArray().All(c => c.IsHex()))
        {
            var escaped = $"{beatSaverDownloadURL}{beatSaverId}";
            var uri = new Uri(escaped, UriKind.Absolute);
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu", GetBeatmapFromLocation(uri));
        }
        else // If not, handle it as a direct link to a zip file.
        {
            if (!Uri.TryCreate(location, UriKind.Absolute, out var uri))
            {
                CancelTempLoader("Could not retrieve a proper location to download from.");
                return;
            }

            if (!uri.AbsolutePath.ToLower().EndsWith("zip"))
            {
                CancelTempLoader("Provided URL does not point to a zipped file.");
                return;
            }

            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu", GetBeatmapFromLocation(uri));
        }
    }

    private async UniTask GetBeatmapFromLocation(Uri uri)
    {
        // We will extract the contents of the zip to the temp directory, so we will save the zip in memory.
        var downloadHandler = new DownloadHandlerBuffer();

        // Create our web request, and set our handler.
        var request = UnityWebRequest.Get(uri);
        request.downloadHandler = downloadHandler;
        // Change our User-Agent so BeatSaver can download our map
        request.SetRequestHeader("User-Agent", $"{Application.productName}/{Application.version}");

        // Set progress bar state.
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        PersistentUI.Instance.LevelLoadSlider.value = 0;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "Downloading file... Starting download...";

        var operation = request.SendWebRequest();
        while (!request.isDone)
        {
            // Grab Content-Length, which is the length of the downloading file, to use for progress bar.
            if (int.TryParse(request.GetResponseHeader("Content-Length"), out var length))
            {
                var progress = downloadHandler.data.Length / (float)length;
                PersistentUI.Instance.LevelLoadSlider.value = progress;
                var percent = progress * 100;
                PersistentUI.Instance.LevelLoadSliderLabel.text = $"Downloading file... {percent:F2}% complete.";
            }
            else
            {
                // Just gives the bar something to do until we get the content length.
                PersistentUI.Instance.LevelLoadSlider.value = (Mathf.Sin(Time.time) / 2) + 0.5f;
            }

            // Cancel loading if an error has occurred.
            if (request.result > UnityWebRequest.Result.Success)
            {
                CancelTempLoader(request.error);
                return;
            }

            await UniTask.Yield();
        }

        // Check one more time to be safe.
        if (request.result > UnityWebRequest.Result.Success)
        {
            CancelTempLoader(request.error);
            return;
        }

        // Wahoo! We are done. Let's grab our downloaded data.
        var downloaded = downloadHandler.data;

        // If the request failed, our downloaded bytes will be null. Let's check that.
        if (downloaded == null)
        {
            CancelTempLoader("Downloaded bytes is somehow null, yet the request was successfully completed. WTF!?");
            return;
        }

        PersistentUI.Instance.LevelLoadSlider.value = 1;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "Extracting contents...";

        try
        {
            await UniTask.SwitchToThreadPool();

            // Slap our downloaded bytes into a memory stream and slap that into a ZipArchive.
            var stream = new MemoryStream(downloaded);
            var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            // Create the directory for our song to go to.
            // Path.GetTempPath() should be compatible with Windows and UNIX.
            // See Microsoft docs on it.
            var directory = Path.Combine(Path.GetTempPath(), "ChroMapper Temp Loader", request.GetHashCode().ToString());
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // Extract our zipped file into this directory.
            archive.ExtractToDirectory(directory);

            // Dispose our downloaded bytes, we don't need them.
            downloadHandler.Dispose();

            // Try and get a BeatSaberSong out of what we've downloaded.
            var song = await BeatSaberSong.GetSongFromFolderAsync(directory);
            await UniTask.SwitchToMainThread();
            if (song == null)
            {
                CancelTempLoader("Could not obtain a valid Beatmap from the downloaded content.");
                return;
            }

            BeatSaberSongContainer.Instance.Song = song;
            PersistentUI.Instance.LevelLoadSliderLabel.text = "Loading song...";
        }
        catch (Exception e)
        {
            await UniTask.SwitchToMainThread();

            // Uh oh, an error occurred.
            // Let's see if it is due to user error, or a genuine error on ChroMapper's part.
            switch (e.GetType().Name)
            {
                // InvalidDataException means that the ZipArchive cannot be created.
                // ChroMapper tried to download something that is not a zip.
                case nameof(InvalidDataException):
                    CancelTempLoader("Downloaded content was not a valid zip.");
                    break;
                // Default case is a genuine error, let's print what it has to say.
                default:
                    CancelTempLoader($"An unknown error ({e.GetType().Name}) has occurred:\n\n{e.Message}");
                    break;
            }
        }
    }

    private void CancelTempLoader(string error)
    {
        PersistentUI.Instance.ShowDialogBox(
            $"Temp Loader failed with the following message:\n\n{error}",
            null,
            PersistentUI.DialogBoxPresetType.Ok);
        SceneTransitionManager.Instance.CancelLoading("");
        ResetProgressBar();
    }

    private void ResetProgressBar()
    {
        PersistentUI.Instance.LevelLoadSlider.value = 0;
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
    }
}
