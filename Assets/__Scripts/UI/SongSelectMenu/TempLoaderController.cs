using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class TempLoaderController : MonoBehaviour
{
    // Location to the API endpoint to download a map from its BeatSaver ID
    private const string beatSaverDownloadUrl = "https://beatsaver.com/api/download/key/";

    public void OpenTempLoader()
    {
        PersistentUI.Instance.ShowInputBox(
            "Please enter the BeatSaver ID to a map, or a direct link to a zipped beatmap.",
            TryOpenTempLoader, "ff9"
            );
    }

    private void TryOpenTempLoader(string location)
    {
        if (string.IsNullOrEmpty(location) || string.IsNullOrWhiteSpace(location)) return;
        // Trim whitespace from the beginning and end of our location
        location = location.Trim();
        // Check if it is a valid BeatSaver ID
        if (location.ToCharArray().All(c => c.IsHex()))
        {
            string escaped = $"{beatSaverDownloadUrl}{location}";
            Uri uri = new Uri(escaped, UriKind.Absolute);
            SceneTransitionManager.Instance.LoadScene(2, GetBeatmapFromLocation(uri));
        }
        else // If not, handle it as a direct link to a zip file.
        {
            if (location.ToLower().EndsWith(".zip"))
            {
                // This is definitely more open so let's see if we can even create a Uri out of this.
                if (Uri.TryCreate(location, UriKind.Absolute, out Uri uri))
                {
                    SceneTransitionManager.Instance.LoadScene(2, GetBeatmapFromLocation(uri));
                }
                else
                {
                    CancelTempLoader("Could not retrieve a proper location to download from.");
                }
            }
            else
            {
                CancelTempLoader("Provided URL does not point to a zipped file.");
            }
        }
    }

    private IEnumerator GetBeatmapFromLocation(Uri uri)
    {
        // We will extract the contents of the zip to the temp directory, so we will save the zip in memory.
        DownloadHandlerBuffer downloadHandler = new DownloadHandlerBuffer();

        // Create our web request, and set our handler.
        UnityWebRequest request = UnityWebRequest.Get(uri);
        request.downloadHandler = downloadHandler;
        // Change our User-Agent so BeatSaver can download our map
        request.SetRequestHeader("User-Agent", $"{Application.productName}/{Application.version}");

        // Set progress bar state.
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        PersistentUI.Instance.LevelLoadSlider.value = 0;
        PersistentUI.Instance.LevelLoadSliderLabel.text = $"Downloading file... this could take a while!";

        var operation = request.SendWebRequest();
        while (operation.progress < 1f)
        {
            // Lissen. The progress value Unity gives me is not representative of the download state at all.
            // So I'll just use some funny sine waves to make it look like ChroMapper is doing something.
            float funnyProgressValue = (Mathf.Sin(Time.time) / 2f) + 0.5f;
            PersistentUI.Instance.LevelLoadSlider.value = funnyProgressValue;

            // Cancel loading if an error has occurred.
            if (request.isHttpError || request.isNetworkError)
            {
                CancelTempLoader(request.error);
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }
        // Check one more time to be safe.
        if (request.isHttpError || request.isNetworkError)
        {
            CancelTempLoader(request.error);
            yield break;
        }
        PersistentUI.Instance.LevelLoadSlider.value = 1;
        yield return new WaitUntil(() => request.isDone);
        
        // Wahoo! We are done. Let's grab our downloaded data.
        byte[] downloaded = downloadHandler.data;

        // If the request failed, our downloaded bytes will be null. Let's check that.
        if (downloaded != null)
        {
            PersistentUI.Instance.LevelLoadSlider.value = 1;
            PersistentUI.Instance.LevelLoadSliderLabel.text = "Extracting contents...";

            try
            {
                // Slap our downloaded bytes into a memory stream and slap that into a ZipArchive.
                MemoryStream stream = new MemoryStream(downloaded);
                ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);

                // Create the directory for our song to go to.
                // Path.GetTempPath() should be compatible with Windows and UNIX.
                // See Microsoft docs on it.
                string directory = $"{Path.GetTempPath()}ChroMapper Temp Loader\\{request.GetHashCode()}";
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                // Extract our zipped file into this directory.
                archive.ExtractToDirectory(directory);

                // Dispose our downloaded bytes, we don't need them.
                downloadHandler.Dispose();

                // Try and get a BeatSaberSong out of what we've downloaded.
                BeatSaberSong song = BeatSaberSong.GetSongFromFolder(directory);
                if (song != null)
                {
                    PersistentUI.Instance.LevelLoadSliderLabel.text = "Loading song...";
                    BeatSaberSongContainer.Instance.song = song;
                }
                else
                {
                    CancelTempLoader("Could not obtain a valid Beatmap from the downloaded content.");
                }
            }
            catch(Exception e)
            {
                // Uh oh, an error occurred.
                // Let's see if it is due to user error, or a genuine error on ChroMapper's part.
                switch (e.GetType().Name)
                {
                    // InvalidDataException means that the ZipArchive cannot be created.
                    // ChroMapper tried to download something that is not a zip.
                    case nameof(InvalidDataException):
                        CancelTempLoader($"Downloaded content was not a valid zip.");
                        break;
                    // Default case is a genuine error, let's print what it has to say.
                    default:
                        CancelTempLoader($"An unknown error ({e.GetType().Name}) has occurred:\n\n{e.Message}");
                        break;
                }
            }
        }
        else
        {
            CancelTempLoader("Downloaded bytes is somehow null, yet the request was successfully completed. WTF!?");
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
