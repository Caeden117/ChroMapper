using System;
using System.Collections;
using System.Globalization;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization.Settings;
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


        // YES THIS CODE WAS THE FINAL STRAW 
        // WHICH FINALLY PUSHED FOR A UI DIALOG REWRITE
        // YES
        // I DESERVE CREDIT FOR THIS UI REWRITE
        public static IEnumerator DoDownload()
        {
            var dialog = PersistentUI.Instance.CreateNewDialogBox();
            dialog.WithTitle("Options", "quest.downloading");
            var progressBarComponent = dialog.AddComponent<ProgressBarComponent>();

            void Fail(UnityWebRequest www, Exception e)
            {
                dialog.Close(); // Act as if we clicked, so close the dialogue.

                OnDownloadFail(www, e);
            }

            void UpdateLabel(bool extracting)
            {
                progressBarComponent.WithCustomLabelFormatter(f =>
                {
                    return LocalizationSettings.StringDatabase.GetLocalizedString("Options",
                        extracting ? "quest.extracting_download" : "quest.downloading_progress", new object[] { f * 100 });
                });
            }
            
            UpdateLabel(false);
            
            dialog.Open();

            var downloadCoro = Adb.DownloadADB(null, Fail, (request, extracting) =>
            {
                // Progress bar how?
                Debug.Log($"Download at {(request.downloadProgress * 100).ToString(CultureInfo.InvariantCulture)}");

                // Progress bar this!
                if (!extracting)
                {
                    progressBarComponent.UpdateProgressBar(request.downloadProgress);
                }
                else
                {
                    UpdateLabel(true);
                    progressBarComponent.UpdateProgressBar(request.downloadProgress);
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
                // close before opening new dialog
                dialog.Close();
                OnDownloadFail(null, e);
            }

            dialog.Close();

            yield return Adb.Dispose().AsCoroutine();
        }


    }
}
