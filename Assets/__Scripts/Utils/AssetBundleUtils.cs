using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZLinq;

public static class AssetBundleUtils
{
    /// <summary>Opens a bundle synchronously. The caller owns the returned bundle and its loaded assets.</summary>
    /// <returns>The loaded bundle, or null when Unity cannot read it.</returns>
    public static AssetBundle LoadFromFile(string filePath) => AssetBundle.LoadFromFile(filePath);

    /// <summary>Loads each named asset lazily, preserving prefab components without sanitization.</summary>
    /// <remarks>
    /// Keep the caller-owned bundle loaded throughout enumeration. This method does not unload it on failure.
    /// Assets that Unity cannot load are skipped; subassets are not enumerated separately.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The bundle is null when enumeration begins.</exception>
    public static IEnumerable<(string AssetPath, UnityEngine.Object Asset)> EnumerateAssets(AssetBundle assetBundle)
    {
        if (assetBundle == null) throw new ArgumentNullException(nameof(assetBundle));

        foreach (var assetPath in assetBundle.GetAllAssetNames())
        {
            var asset = assetBundle.LoadAsset(assetPath);
            if (asset != null) yield return (assetPath, asset);
        }
    }

    public static IEnumerator LoadAssetFromFileAsync<T>(
        string filePath,
        string assetName,
        Action<AssetBundle, T> onLoaded,
        Action<string> onFailed,
        params Type[] supportedTypes) where T : UnityEngine.Object
    {
        if (onLoaded == null) throw new ArgumentNullException(nameof(onLoaded));
        if (onFailed == null) throw new ArgumentNullException(nameof(onFailed));

        AssetBundleCreateRequest bundleRequest;
        try
        {
            bundleRequest = AssetBundle.LoadFromFileAsync(filePath);
        }
        catch (Exception exception)
        {
            onFailed($"Unity could not start loading the bundle ({exception.GetType().Name})");
            yield break;
        }

        yield return bundleRequest;
        var assetBundle = bundleRequest.assetBundle;
        if (assetBundle == null)
        {
            onFailed("Unity could not read the AssetBundle");
            yield break;
        }

        AssetBundleRequest assetRequest = null;
        Exception assetLoadException = null;
        try
        {
            assetRequest = assetBundle.LoadAssetAsync<T>(assetName);
        }
        catch (Exception exception)
        {
            assetLoadException = exception;
        }

        if (assetRequest == null)
        {
            yield return UnloadAsync(assetBundle);
            onFailed($"Unity could not start loading the asset ({assetLoadException?.GetType().Name ?? "Unknown"})");
            yield break;
        }

        yield return assetRequest;
        var asset = assetRequest.asset as T;
        if (asset == null)
        {
            yield return UnloadAsync(assetBundle);
            onFailed($"the bundle does not contain {assetName}");
            yield break;
        }

        Exception preparationException = null;
        try
        {
            if (asset is GameObject gameObject) SanitizeGameObject(gameObject, supportedTypes);
        }
        catch (Exception exception)
        {
            preparationException = exception;
        }

        if (preparationException != null)
        {
            yield return UnloadAsync(assetBundle);
            onFailed($"the asset could not be prepared ({preparationException.GetType().Name})");
            yield break;
        }

        onLoaded(assetBundle, asset);
    }

    public static T[] LoadAssetsWithSubAssetsFromFile<T>(
        string filePath,
        string assetName,
        params Type[] supportedTypes) where T : UnityEngine.Object
    {
        var assetBundle = LoadFromFile(filePath);
        if (assetBundle == null) throw new InvalidOperationException("Unity could not read the AssetBundle");

        try
        {
            var assets = assetBundle.LoadAssetWithSubAssets<T>(assetName);
            foreach (var asset in assets)
            {
                if (asset is GameObject gameObject) SanitizeGameObject(gameObject, supportedTypes);
            }

            return assets;
        }
        finally
        {
            Unload(assetBundle, false);
        }
    }

    public static T LoadAsset<T>(
        AssetBundle assetBundle,
        string assetName,
        params Type[] supportedTypes) where T : UnityEngine.Object
    {
        if (assetBundle == null) throw new ArgumentNullException(nameof(assetBundle));

        var asset = assetBundle.LoadAsset<T>(assetName);
        if (asset is GameObject gameObject) SanitizeGameObject(gameObject, supportedTypes);
        return asset;
    }

    public static void Unload(AssetBundle assetBundle, bool unloadAllLoadedObjects = true) =>
        assetBundle.Unload(unloadAllLoadedObjects);

    public static AssetBundleUnloadOperation UnloadAsync(
        AssetBundle assetBundle,
        bool unloadAllLoadedObjects = true) =>
        assetBundle.UnloadAsync(unloadAllLoadedObjects);

    public static void SanitizeGameObject(GameObject prefab, params Type[] supportedTypes)
    {
        if (prefab == null) return;
        if (supportedTypes == null) throw new ArgumentNullException(nameof(supportedTypes));

        var supportedTypeSet = new HashSet<Type>(supportedTypes);
        var unsupportedBehaviours = prefab
            .GetComponentsInChildren<MonoBehaviour>(true)
            .AsValueEnumerable()
            .Where(behaviour => behaviour != null && !supportedTypeSet.Contains(behaviour.GetType()))
            .ToArray();

        foreach (var behaviour in unsupportedBehaviours) UnityEngine.Object.DestroyImmediate(behaviour, true);
    }
}
