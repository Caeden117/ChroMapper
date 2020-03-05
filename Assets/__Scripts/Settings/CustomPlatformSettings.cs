using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class CustomPlatformSettings
{

    private static CustomPlatformSettings _instance;
    public static CustomPlatformSettings Instance => _instance ?? (_instance = Load());

    public Dictionary<string, string> CustomPlatformsDictionary = new Dictionary<string, string>();
    public List<String> CustomEnvironmentsList = new List<String>();

    public GameObject[] LoadPlatform(string platformName)
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath,"CustomPlatforms",platformName + ".plat"));

        GameObject[] platformPrefab = bundle.LoadAssetWithSubAssets<GameObject>("_CustomPlatform");

        bundle.Unload(false);
        //UnityEngine.Object customPlatform = AssetDatabase.LoadAssetAtPath("Assets/CustomPlatforms\\" + platformName+".plat", typeof(UnityEngine.Object)) as UnityEngine.Object;
        Debug.Log("Load platform/s: " + platformName + " " + platformPrefab.Length);
        return platformPrefab;
    }

    private void loadCustomEnvironments()
    {
        var customPlatformsDirectory = Application.dataPath + "/CustomPlatforms";

        //First create CustomPlatforms in project folder if it does not exist
        Directory.CreateDirectory(customPlatformsDirectory);

        //Then copy all not yet existing files over to this (Replace existing as they may have been updated?)
        string beatSaberCustomPlatforms = Settings.Instance.CustomPlatformsFolder;

        if (Directory.Exists(beatSaberCustomPlatforms))
        {
            FileUtil.ReplaceDirectory(beatSaberCustomPlatforms, customPlatformsDirectory);

            //Then import these platforms from the AssetDirectory
            CustomPlatformsDictionary.Clear();
            foreach (var file in Directory.GetFiles(customPlatformsDirectory))
            {
                AssetDatabase.ImportAsset(file.Replace(Application.dataPath, "Assets"));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                UnityEngine.Object customPlatform = AssetDatabase.LoadAssetAtPath(file.Replace(Application.dataPath, "Assets"), typeof(UnityEngine.Object)) as UnityEngine.Object;
                CustomEnvironmentsList.Add(file.Replace(customPlatformsDirectory, "").Replace(".plat", "").Replace("\\", "").Replace("/", ""));
                CustomPlatformsDictionary.Add(file.Replace(customPlatformsDirectory, "").Replace(".plat", "").Replace("\\", "").Replace("/", ""), customPlatform.GetHashCode().ToString());
            }
            
            AssetDatabase.Refresh();
        }
    }

    private static CustomPlatformSettings Load()
    {
        CustomPlatformSettings cpSettings = new CustomPlatformSettings();

        cpSettings.loadCustomEnvironments();

        return cpSettings;
    }
}