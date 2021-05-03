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
    public readonly string assetPath;
    public readonly GameObject prefabRoot;

    public EditPrefabAssetScope(string assetPath)
    {
        this.assetPath = assetPath;
        prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
    }

    public void Dispose()
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
