using System;
using System.Collections;
using System.Globalization;
using __Scripts;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace QuestDumper
{
    /// <summary>
    /// Utility class for doing UI with ABD.
    /// </summary>
    public static class AdbUI
    {
        public static void OnDownloadFail([CanBeNull] UnityWebRequest www, [CanBeNull] Exception e)
        {
            var message = !(e is null) ? e.Message : www?.error;
            OnDownloadFail(message);
        }

        public static void OnDownloadFail(string message)
        {
            PersistentUI.Instance.ShowDialogBox("Options", "quest.adb_error_download", null, PersistentUI.DialogBoxPresetType.Ok, new object[]{message});
        }

        // This UI code is probably the worse thing I could do and
        // makes everything spaghetti like, but hey! it works!
        // P.S I'm sorry
        public static IEnumerator DoDownload()
        {
            while (PersistentUI.Instance.DialogBox_Loading)
                yield return null;

            // TODO: Replace this with a real progress dialogue
            var dialog = PersistentUI.Instance.ShowUnManagedDialogBox("Options", "quest.downloading", null, PersistentUI.DialogBoxPresetType.UnDismissable, new object[]{0});

            void ONFail(UnityWebRequest www, Exception e)
            {
                if (dialog != null)
                    dialog.SendResult(0); // Act as if we clicked, so close the dialogue.

                dialog = null;

                OnDownloadFail(www, e);
            }

            var downloadCoro = Adb.DownloadADB(null, ONFail, (request, extracting) =>
            {
                // Progress bar how?
                Debug.Log($"Download at {(request.downloadProgress * 100).ToString(CultureInfo.InvariantCulture)}");

                // Progress bar this!
                if (PersistentUI.Instance.DialogBox_Loading)
                {
                    return;
                }

                if (!extracting)
                {
                    dialog.UpdateTextLocalized("Options", "quest.downloading",
                        new object[] { (request.downloadProgress * 100f).ToString(CultureInfo.CurrentUICulture) });
                }
                else
                {
                    dialog.UpdateTextLocalized("Options", "quest.extracting_download");
                }
            });

            yield return downloadCoro;

            Debug.Log("Finished extracting, starting ADB");
            try
            {
                // Initialize and dispose to make sure ADB works, catch any exceptions and notify the user.
                Adb.Initialize();
            }
            catch (AssertionException e)
            {
                dialog.SendResult(0); // Act as if we clicked, so close the dialogue.
                dialog = null;
                OnDownloadFail(null, e);
            }

            if (dialog != null)
                dialog.SendResult(0); // Act as if we clicked, so close the dialogue.

            yield return Adb.Dispose().AsCoroutine();
        }


    }
}