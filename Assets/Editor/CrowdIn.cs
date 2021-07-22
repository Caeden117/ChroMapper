using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public class LocalizationWindow : EditorWindow
{
    [MenuItem("Edit/CrowdIn")]
    public static void ShowWindow()
    {
        GetWindow(typeof(LocalizationWindow));
    }

    string apiKey = "";
    static string projectIdentifier = "chromapper";

    void OnGUI()
    {
        maxSize = new Vector2(250f, 150f);
        minSize = maxSize;
        titleContent = new GUIContent("CrowdIn");
        if (GUILayout.Button("Export to JSON"))
        {
            ToJSON();
        }
        if (GUILayout.Button("Import from JSON"))
        {
            FromJSON();
        }
        if (GUILayout.Button("Fix Orphans"))
        {
            FixOrphans();
        }
        GUILayout.Label("API Key:");
        apiKey = GUILayout.TextField(apiKey);
        if (GUILayout.Button("Upload to CrowdIn"))
        {
            ToJSON(apiKey, true);
        }
        if (GUILayout.Button("Import from CrowdIn"))
        {
            FromJSON(apiKey, true);
        }
    }

    private static List<LocaleIdentifier> GetCulturesInfo(StringTableCollection collection)
    {
        List<LocaleIdentifier> cultures = new List<LocaleIdentifier>();
        foreach (StringTable table in collection.StringTables)
        {
            cultures.Add(table.LocaleIdentifier);
        }
        return cultures;
    }

    public static void ToJSON(string apiKey = "", bool upload = false)
    {
        var fileData = new Dictionary<string, StringContent>();
        foreach (StringTableCollection collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            string collectionName = collection.TableCollectionName;

            // Only export english values
            //List<CultureInfo> cultures = GetCulturesInfo(collection);
            //foreach (var culture in cultures)
            //{

            var culture = CultureInfo.GetCultureInfo("en");
            string folder = Path.Combine(Application.dataPath, $"Locales/{culture.TwoLetterISOLanguageName}");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"{collectionName}.json");
            var json = new JSONObject();

            StringTable table = (StringTable)collection.GetTable(culture);

            foreach (SharedTableData.SharedTableEntry entry in table.SharedData.Entries)
            {
                string key = entry.Key;

                if (table.GetEntry(key) == null || string.IsNullOrEmpty(table.GetEntry(key).Value)) continue;

                json[key] = table.GetEntry(key).Value;
            }

            if (upload)
            {
                fileData.Add($"{collectionName}.json", new StringContent(json.ToString(2)));
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(path, false))
                    writer.Write(json.ToString(2));

                Debug.Log($"Wrote: {path}");
            }

            //}
        }

        if (upload)
        {
            string infoUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/info?key={apiKey}&json=true";
            string actionUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/update-file?key={apiKey}";
            string addUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/add-file?key={apiKey}";

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

                        Debug.Log($"Failed to add files to crowdin");
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

                    Debug.Log($"Failed to update crowdin");
                    Debug.Log(why.Result);
                }
                else
                {
                    Debug.Log($"Uploaded files to crowdin");
                }
            }
        }
    }

    public static void FixOrphans()
    {
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections().ToLookup(it => it.TableCollectionName);
        var allTables = Resources.FindObjectsOfTypeAll<StringTable>();
        foreach (StringTable table in allTables)
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

    public static void FromJSON(string apiKey = "", bool download = false)
    {
        string downloadUrl = $"https://api.crowdin.com/api/project/{projectIdentifier}/export-file?key={apiKey}";

        foreach (StringTableCollection collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            string collectionName = collection.TableCollectionName;

            List<LocaleIdentifier> cultures = GetCulturesInfo(collection);
            foreach (var culture in cultures)
            {
                if (culture.Code.Equals("en")) continue;

                JSONNode json;
                if (download)
                {
                    using (var client = new HttpClient())
                    {
                        var downloadTask = client.GetAsync($"{downloadUrl}&file={collectionName}.json&language={culture.Code}");
                        try
                        {
                            downloadTask.Wait();
                            var stringTask = downloadTask.Result.Content.ReadAsStringAsync();
                            stringTask.Wait();

                            if (stringTask.Result.Contains("Language was not found"))
                            {
                                Debug.LogError($"Language with code {culture.Code} was not found on CrowdIn");
                            }

                            json = JSON.Parse(stringTask.Result);
                        }
                        catch (Exception)
                        {
                            // 404 = Import empty, anything else = skip
                            if (downloadTask.Result.StatusCode != System.Net.HttpStatusCode.NotFound) continue;
                            json = new JSONObject();
                        }
                    }
                }
                else
                {
                    string folder = Path.Combine(Application.dataPath, $"Locales/{culture.Code}");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string path = Path.Combine(folder, $"{collectionName}.json");

                    json = GetNodeFromFile(path);
                }

                StringTable table = (StringTable)collection.GetTable(culture);
                table.MetadataEntries.Clear();
                StringTable tableEnglish = (StringTable)collection.GetTable(CultureInfo.GetCultureInfo("en"));

                foreach (SharedTableData.SharedTableEntry entry in table.SharedData.Entries)
                {
                    string key = entry.Key;

                    if (json.HasKey(key))
                    {
                        table.AddEntry(key, json[key]);
                    }

                    if (table.GetEntry(key) == null || string.IsNullOrEmpty(table.GetEntry(key).Value))
                    {
                        // Add english as default
                        table.AddEntry(key, tableEnglish.GetEntry(key).Value);
                    }

                    table.GetEntry(key).IsSmart = false;
                    table.GetEntry(key).MetadataEntries.Clear();
                    table.GetEntry(key).IsSmart = tableEnglish.GetEntry(key).IsSmart;
                }

                EditorUtility.SetDirty(table);
                if (download)
                {
                    Debug.Log($"Downloaded: {culture.Code} - {collectionName}.json");
                }
                else
                {
                    Debug.Log($"Loaded from file");
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static JSONNode GetNodeFromFile(string file)
    {
        if (!File.Exists(file)) return new JSONObject();
        try
        {
            using (StreamReader reader = new StreamReader(file))
            {
                JSONNode node = JSON.Parse(reader.ReadToEnd());
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