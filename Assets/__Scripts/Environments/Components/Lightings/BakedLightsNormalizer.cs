using System.Collections.Generic;
using UnityEngine;

public class BakedLightsNormalizer : MonoBehaviour
{
    [SerializeField] public float MaxTotalIntensity = 1f;

    private readonly Dictionary<LightConstants.BakeId, LightmapLightsController> lightmapLightDict = new();

    private bool lightmapDictInitialized;
    private float grayscaleTotal;
    private int lastCalculatedOnFrame;
    private bool grayscaleCalculatedOnce;
    private bool newUpdates = true;

    private const int maxFramesWithoutUpdate = 5;

    protected void LateUpdate()
    {
        if (!newUpdates || Time.frameCount - lastCalculatedOnFrame <= maxFramesWithoutUpdate) return;
        UpdateGrayscaleTotal();
        newUpdates = false;
    }

    private void GetLightmapLights()
    {
        lightmapLightDict.Clear();
        foreach (var controller in FindObjectsByType<LightmapLightsController>(FindObjectsSortMode.None))
            lightmapLightDict[controller.BakeId] = controller;

        lightmapDictInitialized = true;
    }

    private void UpdateGrayscaleTotal()
    {
        if (lightmapLightDict.Count == 0 && !lightmapDictInitialized) GetLightmapLights();

        if (Time.frameCount == lastCalculatedOnFrame && grayscaleCalculatedOnce) return;

        grayscaleTotal = 0f;
        foreach (var value in lightmapLightDict.Values)
            grayscaleTotal += value.CalculatedColorPreNormalization.grayscale * value.NormalizerWeight;

        lastCalculatedOnFrame = Time.frameCount;
        grayscaleCalculatedOnce = true;
    }

    public float GetNormalizationMultiplier()
    {
        UpdateGrayscaleTotal();
        newUpdates = true;
        if (!lightmapDictInitialized || !(grayscaleTotal > MaxTotalIntensity)) return 1f;

        return Mathf.LinearToGammaSpace(MaxTotalIntensity / grayscaleTotal);
    }
}
