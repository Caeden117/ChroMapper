using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class LogLineUI : MonoBehaviour
{
    private const string bugReportsSubfolder = "Bug Reports";
    private const int lastLinesCount = 20;

    public TextMeshProUGUI TextMesh;
    [SerializeField] [FormerlySerializedAs("ReportButton")] private Button reportButton;

    private string previousMessages = "";
    private DevConsole.Logline logline;
    private bool sentReport;

    private static readonly string seperator = new string('-', 50);

    internal void SetupReport(DevConsole.Logline logline, List<string> lines)
    {
        this.logline = logline;
        reportButton.gameObject.SetActive(logline.Type == LogType.Exception);
        reportButton.image.color = Color.cyan;
        previousMessages = logline.Type == LogType.Exception ? string.Join("\n", lines.Skip(lines.Count - lastLinesCount)) : "";
        sentReport = false;
    }

    public void SendReport()
    {
        if (!sentReport)
        {
            sentReport = true;
            StartCoroutine(GenerateBugReport());
        }

        DevConsole.OpenFolder(bugReportsSubfolder);
    }

    private static string GenerateSystemInfo()
    {
        return "APP: ChroMapper " + Application.version + ", Unity " + Application.unityVersion + " (" + Environment.CommandLine + ")\n" +
               "CPU: " + SystemInfo.processorType + " (" + SystemInfo.processorCount + " cores)\n" +
               "GPU: " + SystemInfo.graphicsDeviceName + "\n" +
               "RAM: " + SystemInfo.systemMemorySize + " MB\n" +
               "OS: " + SystemInfo.operatingSystem;
    }

    // This could be made more useful with a proper manifest file
    private static string GeneratePluginList()
    {
        var stringBuilder = new StringBuilder();

        foreach (var plugin in PluginLoader.LoadedPlugins)
        {
            stringBuilder.AppendLine($"{plugin.Name} - {plugin.Version}");
        }
        
        return stringBuilder.ToString();
    }

    private string Heading(string text, bool first = false) => (first ? "" : "\n\n\n") + $"{seperator}\n{text}\n{seperator}\n";

    public IEnumerator GenerateBugReport()
    {
        yield return CreateAsync(
            Heading("System information:", true) +
            GenerateSystemInfo() +

            Heading("Installed plugins:") +
            GeneratePluginList() +
            
            Heading("Exception:") +
            logline.Message + "\n" + logline.StackTrace +

            Heading("Recent log messages before error:") +
            previousMessages,
            "ChroMapper " + Application.version + " bug report info"
        );
        reportButton.image.color = Color.green;
    }

    private IEnumerator WriteErrorToFile(string text)
    {
        var path = Path.Combine(Application.persistentDataPath, bugReportsSubfolder);
        
        Directory.CreateDirectory(path);

        var fileName = Path.Combine(path, $"{DateTime.Now:yyyy_MM_dd-HH_mm_ss}.txt");

        yield return File.WriteAllTextAsync(fileName, text);
    }

    private IEnumerator CreateAsync(string text, string title = "Untitled", string language = "csharp", int visibility = 1, string expiration = "N")
    {
        yield return WriteErrorToFile(text);
    }
}
