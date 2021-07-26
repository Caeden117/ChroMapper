using System;
using System.Collections;
using System.Globalization;
using QuestDumper;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace __Scripts.UI.Options
{
    public class AdbHandler : MonoBehaviour
    {
        private BetterToggle _betterToggle;

        private void Start()
        {
            _betterToggle = GetComponent<BetterToggle>();
            // Set toggle
            SetBetterToggleValue(Adb.IsAdbInstalled(out _));
        }


        private void SetBetterToggleValue(bool val)
        {
            // Update the toggle manually
            if (_betterToggle.isOn == val) return;

            _betterToggle.OnPointerClick(null);
        }

        /// <summary>
        /// Toggles ADB installation
        ///
        /// This in reality is just for downloading ADB,
        /// would rather it be a button and hidden when it is installed
        ///
        /// </summary>
        public void ToggleADB()
        {
            if (!Adb.IsAdbInstalled(out _))
            {
                // TODO: This needs a dialogue or else the coroutine can be cancelled when the object is no longer active
                StartCoroutine(AdbInstallDialogue());
            }
            else
            {
                _betterToggle.isOn = true;
            }
        }

        private static void OnDownloadFail(UnityWebRequest www, Exception e)
        {
            var message = !(e is null) ? e.Message : www.error;

            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "quest.adb_error_download", null, PersistentUI.DialogBoxPresetType.Ok, new object[]{message});
        }

        public IEnumerator AdbInstallDialogue()
        {
            // TODO: Progress bar dialogue?
            var downloadCoro = Adb.DownloadADB(null, OnDownloadFail, request =>
            {
                // Progress bar how?
                Debug.Log($"Download at {(request.downloadProgress * 100).ToString(CultureInfo.InvariantCulture)}");
            });

            yield return downloadCoro;

            bool failed = false;

            Debug.Log("Finished extracting, starting ADB");
            try
            {
                Adb.Initialize();
                SetBetterToggleValue(true);
            }
            catch (AssertionException)
            {
                Debug.Log("No ADB installed");
                SetBetterToggleValue(false);

                failed = true;
            }

            if (!failed) yield break;

            while (PersistentUI.Instance.DialogBox_IsEnabled)
                yield return null;

            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "quest.adb_error_download", null, PersistentUI.DialogBoxPresetType.Ok, new object[]{"ADB did not download successfully"});
        }
    }
}