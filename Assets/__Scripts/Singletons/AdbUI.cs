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

        public static void OnDownloadFail(string message) => PersistentUI.Instance.ShowDialogBox("Options", "quest.adb_error_download", null, PersistentUI.DialogBoxPresetType.Ok, new object[]{message});


        // YES THIS CODE WAS THE FINAL STRAW 
        // WHICH FINALLY PUSHED FOR A UI DIALOG REWRITE
        // YES
        // I DESERVE CREDIT FOR THIS UI REWRITE
        public static IEnumerator DoDownload()
        {
            var dialog = PersistentUI.Instance.CreateNewDialogBox();
            dialog.WithTitle("Options", "quest.downloading");
            var progressBarComponent = dialog.AddComponent<ProgressBarComponent>();

            progressBarComponent.WithCustomLabelFormatter(f =>
            {
                return LocalizationSettings.StringDatabase.GetLocalizedString("Options",  "quest.downloading_progress", new object[] { f * 100 });
            });

            dialog.Open();

            var downloadCoro = Adb.DownloadADB(null, (www, e) =>
            {
                dialog.Close();

                OnDownloadFail(www, e);
            }, (request, extracting) =>
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
                    progressBarComponent.WithCustomLabelFormatter(f => LocalizationSettings.StringDatabase.GetLocalizedString("Options", "quest.extracting_download"));
                    progressBarComponent.UpdateProgressBar(request.downloadProgress);
                }
            });

            yield return downloadCoro;

            Debug.Log("Finished extracting, starting ADB");

            // Initialize and dispose to make sure ADB works, catch any exceptions and notify the user.
            var initialize = Adb.Initialize();
            yield return initialize.AsCoroutine();

            string error = initialize.Result.ErrorOut?.Trim()
                .Replace("* daemon not running; starting now at tcp:5037\n* daemon started successfully", "");
            
            if (!initialize.IsCompleted || initialize.Exception != null || !string.IsNullOrEmpty(error))
            {
                // close before opening new dialog
                dialog.Close();
                OnDownloadFail(null, initialize.Exception);
                yield break;
            }


            
            dialog.Clear();
            
            // Notify the user the task finished
            dialog.WithTitle("Options", "quest.adb_finished_downloading");
            dialog.AddFooterButton(null, "Ok");
            
            // var okButton = dialog.AddComponent<ButtonComponent>();
            // okButton.OnClick(() => dialog.Close());
            // okButton.WithLabel("Ok");
        }

        public static IEnumerator DoRemove()
        {
            var dialog = PersistentUI.Instance.CreateNewDialogBox();
            dialog.WithTitle("Options", "quest.uninstalling_adb");
            dialog.Open();

            yield return Adb.RemoveADB();
            
            dialog.Close();
            
        }

    }
}
