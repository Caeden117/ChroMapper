using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CustomPlatformSettings
{

    private static CustomPlatformSettings _instance;
    public static CustomPlatformSettings Instance => _instance ?? (_instance = Load());

    public Dictionary<string, PlatformInfo> CustomPlatformsDictionary = new Dictionary<string, PlatformInfo>();
    public List<String> CustomEnvironmentsList = new List<String>();

    public GameObject[] LoadPlatform(string name)
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(CustomPlatformsDictionary[name].Info.FullName);

        GameObject[] platformPrefab = bundle.LoadAssetWithSubAssets<GameObject>("_CustomPlatform");

        bundle.Unload(false);

        Debug.Log("Load platform/s: " + name + " " + platformPrefab.Length);
        return platformPrefab;
    }

    private void loadCustomEnvironments()
    {
        string beatSaberCustomPlatforms = Settings.Instance.CustomPlatformsFolder;

        if (Directory.Exists(beatSaberCustomPlatforms))
        {
            //FileUtil.ReplaceDirectory(beatSaberCustomPlatforms, customPlatformsDirectory);

            //Then import these platforms from the AssetDirectory
            CustomPlatformsDictionary.Clear();
            foreach (var file in Directory.GetFiles(beatSaberCustomPlatforms))
            {
                FileInfo info = new FileInfo(file);

                //Use AssetBundle. Not AssetDatabase.
                string name = info.Name.Split('.')[0];
                CustomEnvironmentsList.Add(name);
                PlatformInfo platInfo = new PlatformInfo();
                platInfo.Info = info;
                using (MD5 md5 = MD5.Create())
                using (Stream stream = File.OpenRead(info.FullName))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2").ToLower());
                    }
                    platInfo.Md5Hash = sb.ToString();
                    CustomPlatformsDictionary.Add(name, platInfo);
                }
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
public struct PlatformInfo
{
    public FileInfo Info;
    public string Md5Hash;
}