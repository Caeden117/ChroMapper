using System;
using System.Collections;
using System.IO;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public static class ChroMapTogetherApi
{
    public static void TryRoomCode(string code, Action<string, int> onSuccess, Action<int, string> onFail)
        => PersistentUI.Instance.StartCoroutine(AttemptRoomCode(code, onSuccess, onFail));

    private static IEnumerator AttemptRoomCode(string code, Action<string, int> onSuccess, Action<int, string> onFail)
    {
        var url = Path.Combine(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl, $"JoinServer?code={code}")
            .Replace('\\', '/');

        using (var request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var json = JSONNode.Parse(request.downloadHandler.text);

                onSuccess?.Invoke(json["ip"], json["port"]);
            }
            else
            {
                onFail?.Invoke((int)request.responseCode, request.error);
            }
        }
    }

    public static void TryHost(Action<Guid, int, string> onSuccess, Action<int, string> onFail)
        => PersistentUI.Instance.StartCoroutine(AttemptHost(onSuccess, onFail));

    private static IEnumerator AttemptHost(Action<Guid, int, string> onSuccess, Action<int, string> onFail)
    {
        var url = Path.Combine(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl, "CreateServer")
            .Replace('\\', '/');

        var form = new WWWForm();
        form.AddField("appVersion", Application.version);
        using (var request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var json = JSONNode.Parse(request.downloadHandler.text);

                onSuccess?.Invoke(Guid.Parse(json["guid"]), json["port"], json["code"]);
            }
            else
            {
                onFail?.Invoke((int)request.responseCode, request.error);
            }
        }
    }

    public static void TryKeepAlive(Guid guid, Action<int, string> onFail)
        => PersistentUI.Instance.StartCoroutine(AttemptKeepAlive(guid, onFail));

    private static IEnumerator AttemptKeepAlive(Guid guid, Action<int, string> onFail)
    {
        var url = Path.Combine(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl, $"KeepServerAlive?guid={guid}")
            .Replace('\\', '/');

        using (var request = UnityWebRequest.Put(url, string.Empty))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onFail?.Invoke((int)request.responseCode, request.error);
            }
        }
    }
}
