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
    private const string devKey = "zx634zuwuNuzKCocnPpTY99c2uhDJxr3";
    private const string pasteeeKey = "uYsOA14Wo2JBxzQBgMHNfWOi6Mlbghchc8B86IwG6";
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
    }
    
    private static string GenerateSystemInfo()
    {
        return "APP: ChroMapper " + Application.version + ", Unity " + Application.unityVersion + " (" + Environment.CommandLine + ")\n" +
               "CPU: " + SystemInfo.processorType + " (" + SystemInfo.processorCount + " cores)\n" +
               "GPU: " + SystemInfo.graphicsDeviceName + "\n" +
               "RAM: " + SystemInfo.systemMemorySize + " MB\n" +
               "OS: " + SystemInfo.operatingSystem;
    }

    private string Heading(string text, bool first = false)
    {
        return (first ? "" : "\n\n\n") + $"{seperator}\n{text}\n{seperator}\n";
    }

    public IEnumerator GenerateBugReport()
    {
        yield return CreateAsync(
            Heading("System information:", true) +
            GenerateSystemInfo() + 

            Heading("Exception:") +
            logline.Message + "\n" + logline.StackTrace +

            Heading("Recent log messages before error:") + 
            previousMessages,
            "ChroMapper " + Application.version + " bug report info"
        );
        reportButton.image.color = Color.green;
    }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
        Justification = "Maybe soon™")]
    private static IEnumerator WriteErrorToFile(string text)
    {
        File.WriteAllText("error.txt", text);
        yield break;
    }

    private static IEnumerator UploadToPasteee(string text, string title = "Untitled")
    {
        var requestBody = new JSONObject();
        requestBody["key"] = pasteeeKey;
        requestBody["description"] = title;

        var sections = new JSONArray();
        var section = new JSONObject();
        section["name"] = "Main";
        section["contents"] = text;
        
        sections.Add(section);
        requestBody["sections"] = sections;

        using (var www = UnityWebRequest.Post("https://api.paste.ee/v1/pastes", ""))
        {
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes( requestBody.ToString()))
            {
                contentType = "application/json"
            };
            yield return www.SendWebRequest();
            var result = JSON.Parse(www.downloadHandler.text);

            if (result.HasKey("link"))
            {
                Application.OpenURL(result["link"]);
            }
            else
            {
                Debug.LogError("Failed to upload bug report!");
            }
        }
    }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
        Justification = "Maybe soon™")]
    private static IEnumerator UploadToPastebin(string text, string title = "Untitled")
    {
        var form = new WWWForm();
        form.AddField("api_dev_key", devKey);
        form.AddField("api_option", "paste");
        form.AddField("api_paste_code", text);
        form.AddField("api_paste_name", title);
        form.AddField("api_paste_format", "csharp");
        form.AddField("api_paste_private", 1); // Unlisted
        form.AddField("api_paste_expire_date", "N"); // Never

        using (var www = UnityWebRequest.Post("https://pastebin.com/api/api_post.php", form))
        {
            yield return www.SendWebRequest();
            var result = www.downloadHandler.text;

            if (result.Contains("Bad API request"))
            {
                Debug.LogError("Failed to upload bug report! " + result);
            }
            else
            {
                Application.OpenURL(result);
            }
        }
    }
    
    private static IEnumerator CreateAsync(string text, string title = "Untitled", string language = "csharp", int visibility = 1, string expiration = "N")
    {
        //yield return WriteErrorToFile(text);
        yield return UploadToPasteee(text, title);
        //yield return UploadToPastebin(text, title);
    }
}
