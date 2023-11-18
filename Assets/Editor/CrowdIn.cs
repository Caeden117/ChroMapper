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
    private static readonly string projectIdentifier = "chromapper";
    
    // I don't want to write code to parse all the steps so I'm skipping some steps and putting the values here
    private static readonly int projectId = 414106;
    private static readonly Dictionary<string, int> tableNameIdMap = new Dictionary<string, int>
    {
        { "FirstBoot", 20 },
        { "SongEditMenu", 22 },
        { "SongSelectMenu", 24 },
        { "Contributors", 26 },
        { "Options", 28 },
        { "Mapper", 30 },
        { "PersistentUI", 32 },
        { "MultiMapping", 33 }
    };

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
            var infoUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/info?key={apiKey}&json=true";
            var actionUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/update-file?key={apiKey}";
            var addUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/add-file?key={apiKey}";

            using (var formData = new MultipartFormDataContent())
            using (var formData2 = new MultipartFormDataContent())
            using (var client = new HttpClient())
            {
                var infoTask = client.GetStringAsync(infoUrl);
                infoTask.Wait();
                var info = JSON.Parse(infoTask.Result);

                var files = fileData.ToLookup(it => info["files"].AsArray.Children.Any(a => a["name"].Equals(it.Key)));

                if (files[false].Any(_ => true))
                {
                    foreach (var file in files[false])
                    {
                        Debug.Log($"Adding {file.Key}");
                        formData2.Add(file.Value, $"files[{file.Key}]", $"files[{file.Key}]");
                    }

                    var addTask = client.PostAsync(addUrl, formData2);
                    addTask.Wait();
                    var add = addTask.Result;

                    if (!add.IsSuccessStatusCode)
                    {
                        var why = add.Content.ReadAsStringAsync();
                        why.Wait();

                        Debug.Log("Failed to add files to crowdin");
                        Debug.Log(why.Result);
                        return;
                    }
                }

                foreach (var file in files[true])
                {
                    Debug.Log($"Updating {file.Key}");
                    formData.Add(file.Value, $"files[{file.Key}]", $"files[{file.Key}]");
                }

                formData.Add(new StringContent("update_as_unapproved"), "update_option");
                var responseTask = client.PostAsync(actionUrl, formData);
                responseTask.Wait();
                var response = responseTask.Result;

                if (!response.IsSuccessStatusCode)
                {
                    var why = response.Content.ReadAsStringAsync();
                    why.Wait();

                    Debug.Log("Failed to update crowdin");
                    Debug.Log(why.Result);
                }
                else
                {
                    Debug.Log("Uploaded files to crowdin");
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
                    if (tableNameIdMap.TryGetValue(collectionName, out var id))
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
                        using var client = new HttpClient();
                        var translationFileTask = client.GetAsync(fileUrl);
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
