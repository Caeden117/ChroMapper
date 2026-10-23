using System;
using System.Collections.Generic;
using System.IO;
using Beatmap.Animations;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class VivifyAssetBundleManager : MonoBehaviour
{
    public VisualRepositorySO VisualRepository;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    public AssetBundle Bundle;
    public readonly Dictionary<string, VivifyObject> AssetPathToPrefab = new();
    public readonly Dictionary<string, Material> AssetPathToMaterial = new();
    public readonly Dictionary<string, Texture> AssetPathToTexture = new();
    public readonly Dictionary<string, UnityEngine.Object> AssetPathToObject = new();
    private readonly Dictionary<string, VisualModelSO> assetPathToVisualModel = new();

    public void OnDestroy() => UnloadAssetBundle();

    public void LoadAssetBundle()
    {
        if (Bundle != null) return;

        if (AssetPathToPrefab.Count > 0
            || AssetPathToMaterial.Count > 0
            || AssetPathToTexture.Count > 0
            || AssetPathToObject.Count > 0)
            UnloadAssetBundle();

        if (!BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements.Contains("Vivify")) return;

        var vivifyBundlePath = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, "bundleWindows2021.vivify");
        if (!File.Exists(vivifyBundlePath))
            vivifyBundlePath = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, "bundleWindows2019.vivify");
        if (!File.Exists(vivifyBundlePath)) return;

        Bundle = AssetBundleUtils.LoadFromFile(vivifyBundlePath);
        if (Bundle == null) return;

        try
        {
            foreach (var (assetPath, asset) in AssetBundleUtils.EnumerateAssets(Bundle))
            {
                AssetPathToObject.Add(assetPath, asset);

                switch (asset)
                {
                    case Material material:
                        VivifyTextMeshProCompatibility.PrepareMaterial(material);
                        AssetPathToMaterial.Add(assetPath, material);
                        continue;
                    case Texture texture:
                        AssetPathToTexture.Add(assetPath, texture);
                        continue;
                }

                if (asset is not GameObject prefab) continue;

                prefab.SetActive(false);
                var go = Instantiate(prefab);
                go.name = asset.name;

                go
                    .GetComponentsInChildren<Component>(true)
                    .Do(c =>
                    {
                        switch (c)
                        {
                            case TMP_Text text:
                                VivifyTextMeshProCompatibility.Prepare(text);
                                break;
                            case Animator:
                                var asc = c.gameObject.AddComponent<AnimatorSyncController>();
                                asc.Atsc = atsc;
                                asc.SetStartTime(0f);
                                break;
                            case ParticleSystem particleSystem:
                                // Initialize the clock rate before playback; synchronization advances time directly.
                                var particleMain = particleSystem.main;
                                particleMain.simulationSpeed = 1f;
                                var pssc = c.gameObject.AddComponent<ParticleSystemSyncController>();
                                pssc.Atsc = atsc;
                                pssc.SetStartTime(0f);
                                break;
                            case VideoPlayer vp:
                                if (!vp.playOnAwake) return;
                                var vpsc = c.gameObject.AddComponent<VideoPlayerSyncController>();
                                vpsc.Atsc = atsc;
                                vpsc.SetStartTime(0f);
                                break;
                        }
                    });
                // this is more of a patch, but is it needed?
                // go
                //     .GetComponentsInChildren<TextMeshPro>(true)
                //     .Do(t => Destroy(t.GetComponent<CanvasRenderer>()));

                var vivifyObject = go.AddComponent<VivifyObject>();
                vivifyObject.AssetPath = assetPath;
                vivifyObject.Atsc = atsc;
                vivifyObject.Animators = go.GetComponentsInChildren<Animator>(true);
                vivifyObject.SetDefault();
                vivifyObject.SongSynchronize(0);

                // Replacement models use their note container's animator. Enable this controller only on track attachment.
                var objectAnimator = go.AddComponent<ObjectAnimator>();
                objectAnimator.enabled = false;
                vivifyObject.Animator = objectAnimator;
                objectAnimator.AnimationThis = go;
                objectAnimator.LocalTarget = go.transform;
                objectAnimator.WorldTarget = go.transform;
                objectAnimator.TracksManager = tracksManager;
                objectAnimator.Context = beatmapRuntimeContext;
                vivifyObject.SetAnimatorDefault();

                try
                {
                    if (VisualRepository.ModelsByName.ContainsKey(assetPath))
                        throw new InvalidOperationException(
                            $"A visual model with asset path '{assetPath}' already exists.");

                    VisualRepository.AddWithFallback(go, assetPath);
                    assetPathToVisualModel.Add(assetPath, VisualRepository.ModelsByName[assetPath]);
                    AssetPathToPrefab.Add(assetPath, vivifyObject);
                }
                catch
                {
                    Destroy(go);
                    throw;
                }
            }
        }
        catch
        {
            UnloadAssetBundle();
            throw;
        }
    }

    /// <summary>Detaches bundle render targets and releases this manager's models, prefabs and loaded assets.</summary>
    public void UnloadAssetBundle()
    {
        DetachBundleRenderTargets();

        foreach (var (key, model) in assetPathToVisualModel)
        {
            if (VisualRepository != null
                && VisualRepository.ModelsByName != null
                && VisualRepository.ModelsByName.TryGetValue(key, out var currentModel)
                && currentModel == model)
                VisualRepository.ModelsByName.Remove(key);
            if (model != null) Destroy(model);
        }

        foreach (var prefab in AssetPathToPrefab.Values)
        {
            if (prefab != null) Destroy(prefab.gameObject);
        }

        assetPathToVisualModel.Clear();
        AssetPathToPrefab.Clear();
        AssetPathToMaterial.Clear();
        AssetPathToTexture.Clear();
        AssetPathToObject.Clear();
        var bundle = Bundle;
        Bundle = null;
        if (bundle != null) AssetBundleUtils.Unload(bundle);
    }

    private void DetachBundleRenderTargets()
    {
        var renderTextures = new HashSet<RenderTexture>();
        foreach (var texture in AssetPathToTexture.Values)
            if (texture is RenderTexture renderTexture) renderTextures.Add(renderTexture);

        foreach (var asset in AssetPathToObject.Values)
        {
            if (asset is not GameObject prefab) continue;
            foreach (var camera in prefab.GetComponentsInChildren<Camera>(true))
                if (camera.targetTexture != null) renderTextures.Add(camera.targetTexture);
        }

        if (renderTextures.Count == 0) return;

        // Include prefab assets, inactive caches and instances queued for deferred destruction.
        // Unload(true) releases their shared textures before those cameras are destroyed.
        foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>())
            if (camera.targetTexture != null && renderTextures.Contains(camera.targetTexture))
                camera.targetTexture = null;
    }
}
