using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateChecker : MonoBehaviour {

    private static DateTime lastCheck = default;
    private static int latestVersion = -1;
    private static readonly string DEFAULT_CDN = "https://cm.topc.at";
    public GameObject showWhenUpdateIsAvailable;

    private void Awake()
    {
        StartCoroutine(CheckForUpdates());
    }

    private IEnumerator CheckForUpdates()
    {
        var channel = Settings.Instance.ReleaseChannel == 1 ? "dev" : "stable";

        int ourVersion = int.Parse(Application.version.Split('.').Last());

        // Don't check unless using a jenkins build
        // Limit checks to once per hour
        if (ourVersion != 0 && (latestVersion < 0 || DateTime.Now.Subtract(lastCheck).TotalHours > 1))
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{DEFAULT_CDN}/{channel}")) {
                yield return request.SendWebRequest();
                latestVersion = int.Parse(request.downloadHandler.text);
                lastCheck = DateTime.Now;
            }
        }

        // Show when using a jenkins build that is not the latest version
        showWhenUpdateIsAvailable.SetActive(ourVersion != 0 && ourVersion < latestVersion);
    }
}
