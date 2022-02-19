using System;
using UnityEditor;
using UnityEngine;

/*
 * Credits to Baste from the Unity Forums for this wrapper for editing prefabs via script.
 * 
 * This is officially implemented in Unity 2020.1, however we're unfortunately on 2019.3.
 */
public class EditPrefabAssetScope : IDisposable
{
    public readonly string AssetPath;
    public readonly GameObject PrefabRoot;

    public EditPrefabAssetScope(string assetPath)
    {
        AssetPath = assetPath;
        PrefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
    }

    public void Dispose()
    {
        PrefabUtility.SaveAsPrefabAsset(PrefabRoot, AssetPath);
        PrefabUtility.UnloadPrefabContents(PrefabRoot);
    }
}
