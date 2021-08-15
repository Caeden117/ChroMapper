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
                StartCoroutine(AdbUI.DoDownload());
            }
            else
            {
                _betterToggle.isOn = true;
            }
        }
    }
}