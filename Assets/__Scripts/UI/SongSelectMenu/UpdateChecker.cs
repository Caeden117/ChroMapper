using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class UpdateChecker : MonoBehaviour
{
    private static DateTime lastCheck;
    private static int latestVersion = -1;
    [FormerlySerializedAs("showWhenUpdateIsAvailable")] public GameObject ShowWhenUpdateIsAvailable;

    private readonly string parentDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName;

    private ProcessStartInfo startInfo;

    private void Awake() => StartCoroutine(CheckForUpdates());

    public void LaunchUpdate()
    {
        Process.Start(startInfo);

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private IEnumerator CheckForUpdates()
    {
        var args = Environment.GetCommandLineArgs();

        startInfo = new ProcessStartInfo("CML.exe") { WorkingDirectory = parentDir };

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--launcher")
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(args[i + 1]);
                startInfo.FileName = Path.GetFileName(args[i + 1]);
            }
        }

        if (!File.Exists(Path.Combine(startInfo.WorkingDirectory, startInfo.FileName)))
        {
            ShowWhenUpdateIsAvailable.SetActive(false);
            yield break;
        }

        var channel = Settings.Instance.ReleaseChannel == 1 ? "dev" : "stable";

        var ourVersion = int.Parse(Application.version.Split('.').Last());

        // Don't check unless using a jenkins build
        // Limit checks to once per hour
        if (ourVersion != 0 && (latestVersion < 0 || DateTime.Now.Subtract(lastCheck).TotalHours > 1))
            StartCoroutine(GetLatestVersion(Settings.Instance.ReleaseServer, channel, VersionCheckCb));
        else
            VersionCheckCb(latestVersion);
    }

    private void VersionCheckCb(int v)
    {
        var ourVersion = int.Parse(Application.version.Split('.').Last());
        latestVersion = v;
        lastCheck = DateTime.Now;

        // Show when using a jenkins build that is not the latest version
        ShowWhenUpdateIsAvailable.SetActive(ourVersion != 0 && ourVersion < latestVersion);
    }

    public static IEnumerator GetLatestVersion(string server, string channel, Action<int> callback)
    {
        var latestVersion = 0;
        using (var request = UnityWebRequest.Get($"{server}/{channel}"))
        {
            yield return request.SendWebRequest();
            int.TryParse(request.downloadHandler.text, out latestVersion);
        }

        callback(latestVersion);
    }
}
