using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SimpleJSON;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public class LocalizationWindow : EditorWindow
{
    private const int projectId = 414106;

    private string apiKey = "";

    private void OnGUI()
    {
        maxSize = new Vector2(250f, 150f);
        minSize = maxSize;
        titleContent = new GUIContent("CrowdIn");
        if (GUILayout.Button("Export to JSON")) ToJson();
        if (GUILayout.Button("Import from JSON")) FromJson();
        if (GUILayout.Button("Fix Orphans")) FixOrphans();
        GUILayout.Label("API Key:");
        apiKey = GUILayout.TextField(apiKey);
        if (GUILayout.Button("Upload to CrowdIn")) ToJson(apiKey, true);
        if (GUILayout.Button("Import from CrowdIn")) FromJson(apiKey, true);
    }

    [MenuItem("Edit/CrowdIn")]
    public static void ShowWindow() => GetWindow(typeof(LocalizationWindow));

    private static List<LocaleIdentifier> GetCulturesInfo(StringTableCollection collection)
    {
        var cultures = new List<LocaleIdentifier>();
        foreach (var table in collection.StringTables) cultures.Add(table.LocaleIdentifier);
        return cultures;
    }

    private static Dictionary<string, int> GetTableNameIdMap(HttpClient client)
    {
        var filesUrl = $"https://api.crowdin.com/api/v2/projects/{projectId}/files";

        var filesTask = client.GetStringAsync(filesUrl);
        filesTask.Wait();
        var filesInfo = JSON.Parse(filesTask.Result);

        var fileData = filesInfo["data"].AsArray.Children.Select(x => x["data"]);
        return fileData.ToDictionary<JSONNode, string, int>(data => data["name"], data => data["id"]);
    }

    public static void ToJson(string apiKey = "", bool upload = false)
    {
        var fileData = new Dictionary<string, StringContent>();
        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            var collectionName = collection.TableCollectionName;

            // Only export english values
            //List<CultureInfo> cultures = GetCulturesInfo(collection);
            //foreach (var culture in cultures)
            //{

            var culture = CultureInfo.GetCultureInfo("en");
            var folder = Path.Combine(Application.dataPath, $"Locales/{culture.TwoLetterISOLanguageName}");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"{collectionName}.json");
            var json = new JSONObject();

            var table = (StringTable)collection.GetTable(culture);

            foreach (var entry in table.SharedData.Entries)
            {
                var key = entry.Key;

                if (table.GetEntry(key) == null || string.IsNullOrEmpty(table.GetEntry(key).Value)) continue;

                json[key] = table.GetEntry(key).Value;
            }

            if (upload)
            {
                fileData.Add($"{collectionName}.json", new StringContent(json.ToString(2)));
            }
            else
            {
                using (var writer = new StreamWriter(path, false))
                {
                    writer.Write(json.ToString(2));
                }

                Debug.Log($"Wrote: {path}");
            }

            //}
        }
        
        if (upload)
        {
            // Get existing file names
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
            var filesUrl = $"https://api.crowdin.com/api/v2/projects/{projectId}/files";
            var filesTask = client.GetStringAsync(filesUrl);
            filesTask.Wait();
            var filesInfo = JSON.Parse(filesTask.Result);
            
            var existingFileNames = filesInfo["data"].AsArray.Children.Select(x => x["data"]["name"].Value);
            var files = fileData.ToLookup(it => existingFileNames.Contains(it.Key));

            // Add new files
            foreach (var file in files[false])
            {
                if (TryAddStorage(client, file.Key, file.Value, out var storageId))
                {
                    var fileName = file.Key;
                    AddFile(client, fileName, storageId);
                }
            }

            // Update existing files
            var tableNameIdMap = GetTableNameIdMap(client);
            foreach (var file in files[true])
            {
                if (TryAddStorage(client, file.Key, file.Value, out var storageId))
                {
                    var fileId = tableNameIdMap[file.Key];
                    var fileName = file.Key;
                    UpdateFile(client, fileName, fileId, storageId);
                }
            }
        }
    }

    public static void FixOrphans()
    {
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections()
            .ToLookup(it => it.TableCollectionName);
        var allTables = Resources.FindObjectsOfTypeAll<StringTable>();
        foreach (var table in allTables)
        {
            var collection = stringTableCollections[table.TableCollectionName].First();
            if (!collection.ContainsTable(table))
            {
                // Orphaned table, why does it do this?
                collection.AddTable(table);
                EditorUtility.SetDirty(collection);
                Debug.Log(table + " fixed");
            }
            else
            {
                Debug.Log(table + " looks ok");
            }
        }

        AssetDatabase.SaveAssets();
    }

    public static void FromJson(string apiKey = "", bool download = false)
    {
        var translationsExportsUrl = $"https://api.crowdin.com/api/v2/projects/{projectId}/translations/exports";

        using var unAuthedClient = new HttpClient();
        using var tableInfoClient = new HttpClient();
        tableInfoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var tableNameIdMap = download ? GetTableNameIdMap(tableInfoClient): new Dictionary<string, int>();

        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            var collectionName = collection.TableCollectionName;

            var cultures = GetCulturesInfo(collection);
            foreach (var culture in cultures)
            {
                if (culture.Code.Equals("en")) continue;

                JSONNode json = new JSONObject();
                if (download)
                {
                    
                    // First we need to get export the translation and get the url to it
                    var fileUrl = "";
                    if (tableNameIdMap.TryGetValue($"{collectionName}.json", out var id))
                    {
                        using var client = new HttpClient();
                        var stringContent =
                            new StringContent(
                                new JSONObject
                                {
                                    ["targetLanguageId"] = culture.Code,
                                    ["fileIds"] = new JSONArray { [0] = id }
                                }.ToString(), Encoding.UTF8, "application/json");
                        
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        var translationExportTask = client.PostAsync(translationsExportsUrl, stringContent);

                        try
                        {
                            translationExportTask.Wait();
                            var fileUrlTask = translationExportTask.Result.Content.ReadAsStringAsync();
                            fileUrlTask.Wait();

                            fileUrl = JSON.Parse(fileUrlTask.Result)["data"]["url"];
                        }
                        catch (Exception)
                        {
                            // 404 = Import empty, anything else = skip
                            if (translationExportTask.Result.StatusCode != HttpStatusCode.NotFound) continue;
                        }
                    }

                    // Now get retrieve the exported translation file from that url
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        // The url contains the auth
                        var translationFileTask = unAuthedClient.GetAsync(fileUrl);
                        try
                        {
                            translationFileTask.Wait();
                            var stringTask = translationFileTask.Result.Content.ReadAsStringAsync();
                            stringTask.Wait();
                            json = JSON.Parse(stringTask.Result);
                        }
                        catch (Exception)
                        {
                            // 404 = Import empty, anything else = skip
                            if (translationFileTask.Result.StatusCode != HttpStatusCode.NotFound) continue;
                        }
                    }
                }
                else
                {
                    var folder = Path.Combine(Application.dataPath, $"Locales/{culture.Code}");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    var path = Path.Combine(folder, $"{collectionName}.json");

                    json = GetNodeFromFile(path);
                }

                var table = (StringTable)collection.GetTable(culture);
                table.MetadataEntries.Clear();
                var tableEnglish = (StringTable)collection.GetTable(CultureInfo.GetCultureInfo("en"));

                foreach (var entry in table.SharedData.Entries)
                {
                    var key = entry.Key;

                    if (json.HasKey(key)) table.AddEntry(key, json[key]);

                    if (table.GetEntry(key) == null || string.IsNullOrEmpty(table.GetEntry(key).Value))
                        // Add english as default
                        table.AddEntry(key, tableEnglish.GetEntry(key).Value);

                    table.GetEntry(key).IsSmart = false;
                    table.GetEntry(key).MetadataEntries.Clear();
                    table.GetEntry(key).IsSmart = tableEnglish.GetEntry(key).IsSmart;
                }

                EditorUtility.SetDirty(table);
                if (download)
                    Debug.Log($"Downloaded: {culture.Code} - {collectionName}.json");
                else
                    Debug.Log("Loaded from file");
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static bool TryAddStorage(HttpClient client, string fileName, HttpContent fileContent, out long id)
    {
        const string storagesUrl = "https://api.crowdin.com/api/v2/storages";
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = fileContent,
            RequestUri = new Uri(storagesUrl),
        };
        request.Headers.Add("Crowdin-API-FileName", fileName);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    
        var storageResponseTask = client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        storageResponseTask.Wait();
        
        if (!storageResponseTask.Result.IsSuccessStatusCode)
        {
            Debug.LogWarning($"Failed to add {fileName} to storage");
            id = 0;
            return false;
        }
        
        request.Headers.Remove("Crowdin-API-FileName");
        
        var stringTask = storageResponseTask.Result.Content.ReadAsStringAsync();
        stringTask.Wait();

        var json = JSON.Parse(stringTask.Result);

        id = json["data"]["id"].AsLong;
        return true;
    }

    private static void UpdateFile(HttpClient client, string fileName, long fileId, long storageId)
    {
        var filesUrl = $"https://api.crowdin.com/api/v2/projects/{projectId}/files/{fileId}";

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            Content = new StringContent(
                new JSONObject { ["storageId"] = storageId, ["updateOption"] = "keep_translations" }.ToString(),
                Encoding.UTF8,
                "application/json"),
            RequestUri = new Uri(filesUrl),
        };
                    
        var updateFileTask = client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        updateFileTask.Wait();

        if (updateFileTask.Result.IsSuccessStatusCode)
        {
            Debug.Log($"Updated file ({fileName})");
        }
        else
        {
            var errorTask = updateFileTask.Result.Content.ReadAsStringAsync();
            errorTask.Wait();
            Debug.LogWarning($"Failed to update file (${fileName}): {errorTask.Result}");
        }
    }
    
    private static void AddFile(HttpClient client, string fileName, long storageId)
    {
        var filesUrl = $"https://api.crowdin.com/api/v2/projects/{projectId}/files/";

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            Content = new StringContent(
                new JSONObject { ["storageId"] = storageId, ["name"] = fileName }.ToString(), Encoding.UTF8,
                "application/json"),
            RequestUri = new Uri(filesUrl),
        };
                    
        var addFileTask = client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        addFileTask.Wait();

        if (addFileTask.Result.IsSuccessStatusCode)
        {
            Debug.Log($"Added file (${fileName})");
        }
        else
        {
            var errorTask = addFileTask.Result.Content.ReadAsStringAsync();
            errorTask.Wait();
            Debug.LogWarning($"Failed to add file (${fileName}): {errorTask.Result}");
        }
    }
    
    private static JSONNode GetNodeFromFile(string file)
    {
        if (!File.Exists(file)) return new JSONObject();
        try
        {
            using (var reader = new StreamReader(file))
            {
                var node = JSON.Parse(reader.ReadToEnd());
                return node;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error trying to read from file {file}\n{e}");
        }

        return new JSONObject();
    }
}
