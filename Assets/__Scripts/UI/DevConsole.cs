﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class DevConsole : MonoBehaviour, ILogHandler, CMInput.IDebugActions
{
    private const bool devConsoleInEditor = false;
    private const int maxLines = 500;

    [SerializeField] private LogLineUI logRow;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Transform rowParent;
    private readonly List<string> lines = new List<string>();
    private readonly List<LogLineUI> uiElements = new List<LogLineUI>();
    private readonly ConcurrentQueue<Logline> backlog = new ConcurrentQueue<Logline>();
    private StreamWriter writer;

    internal class Logline
    {
        public readonly string StackTrace;
        public readonly LogType Type;
        public readonly string Message;

        public Logline(LogType type, string message, string stackTrace)
        {
            Type = type;
            Message = message;
            StackTrace = stackTrace;
        }
    }

    public void LogFormat(LogType logType, Object context, string format, params object[] args) =>
        // This will not always be called from the main thread
        backlog.Enqueue(new Logline(logType, string.Format(format, args), null));

    public void LogException(Exception exception, Object context) => backlog.Enqueue(new Logline(LogType.Exception, "[" + exception.GetType() + "] " + exception.Message, exception.StackTrace));

    public void OnEnable()
    {
        Hide();

        if (Application.isEditor && !devConsoleInEditor) return;

        var logFile = Path.Combine(Application.persistentDataPath, "ChroMapper.log");
        writer = new StreamWriter(logFile);

        Debug.unityLogger.logHandler = this;
        Application.logMessageReceived += LogCallback;

        SceneManager.sceneLoaded += SceneLoaded;
    }

    public void OnDisable()
    {
        Application.logMessageReceived -= LogCallback;
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        var yPos = arg0.name.Contains("Mapper") ? 30 : 10;
        GetComponent<RectTransform>().anchoredPosition = new Vector3(10, yPos, 0);
    }

    public void Update()
    {
        while (backlog.TryDequeue(out var logline)) ShowLogline(logline);
    }

    private void FixedUpdate() => writer?.Flush();

    private readonly Dictionary<LogType, string> logColors = new Dictionary<LogType, string>()
    {
        {LogType.Log, "#FFFFFF"},
        {LogType.Assert, "#32AD10"},
        {LogType.Error, "#F02B2B"},
        {LogType.Exception, "#AF3DFF"},
        {LogType.Warning, "#EBCF34"}
    };

    private void LogCallback(string condition, string stackTrace, LogType type) => ShowLogline(new Logline(type, condition, stackTrace));

    private void ShowLogline(Logline logline)
    {
        if (logline.Type == LogType.Error || logline.Type == LogType.Exception)
        {
            scrollRect.gameObject.SetActive(true);
        }

        Debug.developerConsoleVisible = false;

        lines.Add(logline.Message);
        writer.WriteLine("[" + logline.Type + "] " + logline.Message);
        if (!string.IsNullOrWhiteSpace(logline.StackTrace))
        {
            writer.WriteLine(logline.StackTrace);
        }

        LogLineUI newElement;
        if (uiElements.Count >= maxLines)
        {
            newElement = uiElements[0];
            uiElements.RemoveAt(0);
            newElement.transform.SetAsLastSibling();
        }
        else
        {
            newElement = Instantiate(logRow, rowParent);
        }
        uiElements.Add(newElement);

        newElement.gameObject.SetActive(true);
        newElement.SetupReport(logline, lines);
        newElement.TextMesh.text = $"<color={logColors[logline.Type]}>" + logline.Message + "</color>\n";
        StopCoroutine(nameof(ScrollToBottom));
        StartCoroutine(nameof(ScrollToBottom));
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void Clear()
    {
        lines.Clear();
        foreach (var textMeshProUGUI in uiElements)
        {
            textMeshProUGUI.gameObject.SetActive(false);
        }
        StopCoroutine(nameof(ScrollToBottom));
        StartCoroutine(nameof(ScrollToBottom));
    }

    public void OpenFolder()
    {
        try
        {
            var path = Application.persistentDataPath;
#if UNITY_STANDALONE_WIN
            path = path.Replace("/", "\\").Replace("\\\\", "\\");
            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
#elif UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
            System.Diagnostics.Process.Start("open", path);
#endif
        }
        catch
        {
            Debug.LogWarning("Failed to open log directory");
        }
    }

    public void Hide() => scrollRect.gameObject.SetActive(false);

    public void OnToggleDebugConsole(InputAction.CallbackContext context)
    {
        scrollRect.gameObject.SetActive(!scrollRect.gameObject.activeSelf);
        if (scrollRect.gameObject.activeSelf && toggle.isOn)
        {
            StopCoroutine(nameof(ScrollToBottom));
            StartCoroutine(nameof(ScrollToBottom));
        }
    }
}
