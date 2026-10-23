using UnityEditor;
using UnityEngine;

public static class EnvironmentBakedLightingAssets
{
    public static void BindLightmapData(GameObject environmentRoot, string environmentId)
    {
        var loader = environmentRoot.GetOrAddComponent<BakedLightDataLoader>();
        // Exports omit the native lightmap asset reference. Publish the imported pair after regeneration populates the loader.
        loader.LightmapData = AssetDatabase.LoadAssetAtPath<LightmapDataSO>(PathUtils.Combine(
            "Assets", "_Graphics", "Textures", "Environments", "BakedLighting",
            environmentId + "LightmapData.asset"));
        loader.SendDataToShaders();
    }
}
