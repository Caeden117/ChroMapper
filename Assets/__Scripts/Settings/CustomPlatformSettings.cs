using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class CustomPlatformSettings
{
    private static CustomPlatformSettings instance;

    public Dictionary<string, PlatformInfo> CustomPlatformsDictionary = new Dictionary<string, PlatformInfo>();
    public static CustomPlatformSettings Instance => instance ??= Load();

    public GameObject[] LoadPlatform(string name)
    {
        var bundle = AssetBundle.LoadFromFile(CustomPlatformsDictionary[name].Info.FullName);

        var platformPrefab = bundle.LoadAssetWithSubAssets<GameObject>("_CustomPlatform");

        bundle.Unload(false);

        Debug.Log("Load platform/s: " + name + " " + platformPrefab.Length);
        return platformPrefab;
    }

    private void LoadCustomEnvironments()
    {
        var beatSaberCustomPlatforms = Settings.Instance.CustomPlatformsFolder;

        if (Directory.Exists(beatSaberCustomPlatforms))
        {
            //FileUtil.ReplaceDirectory(beatSaberCustomPlatforms, customPlatformsDirectory);

            //Then import these platforms from the AssetDirectory
            CustomPlatformsDictionary.Clear();
            foreach (var file in Directory.GetFiles(beatSaberCustomPlatforms))
            {
                var info = new FileInfo(file);
                if (!info.Extension.ToUpper().Contains("PLAT")) continue;
                //Use AssetBundle. Not AssetDatabase.
                var name = info.Name.Split('.')[0];
                if (CustomPlatformsDictionary.ContainsKey(name))
                {
                    Debug.LogError(":hyperPepega: :mega: YOU HAVE TWO PLATFORMS WITH THE SAME FILE NAME");
                }
                else
                {
                    var platInfo = new PlatformInfo {Info = info};
                    using (var md5 = MD5.Create())
                    using (Stream stream = File.OpenRead(info.FullName))
                    {
                        var hashBytes = md5.ComputeHash(stream);
                        var sb = new StringBuilder();
                        for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2").ToLower());
                        platInfo.Md5Hash = sb.ToString();
                        CustomPlatformsDictionary.Add(name, platInfo);
                    }
                }
            }
        }
    }

    private static CustomPlatformSettings Load()
    {
        var cpSettings = new CustomPlatformSettings();

        cpSettings.LoadCustomEnvironments();

        return cpSettings;
    }
}

public struct PlatformInfo
{
    public FileInfo Info;
    public string Md5Hash;
}
