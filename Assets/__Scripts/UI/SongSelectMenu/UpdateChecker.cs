using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateChecker : MonoBehaviour {

    private static DateTime lastCheck = default;
    private static int latestVersion = -1;
    private static readonly string DEFAULT_CDN = "https://cm.topc.at";

    private readonly string ParentDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName;
    public GameObject showWhenUpdateIsAvailable;

    private void Awake()
    {
        StartCoroutine(CheckForUpdates());
    }

    public void LaunchUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        var startInfo = new ProcessStartInfo("CM Launcher.exe")
        {
            WorkingDirectory = ParentDir
        };

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--launcher")
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(args[i + 1]);
                startInfo.FileName = Path.GetFileName(args[i + 1]);
            }
        }

        Process.Start(startInfo);

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private IEnumerator CheckForUpdates()
    {
        if (!File.Exists(Path.Combine(ParentDir, "CM Launcher.exe")))
        {
            showWhenUpdateIsAvailable.SetActive(false);
            yield break;
        }

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
