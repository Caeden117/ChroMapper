using System;
using UnityEngine;
using UnityEngine.Serialization;

public class LightingEvent : MonoBehaviour
{
    public bool OverrideLightGroup;
    public int OverrideLightGroupID;
    public bool UseInvertedPlatformColors;
    public bool CanBeTurnedOff = true;

    [SerializeField] private float multiplyAlpha = 1;

    [FormerlySerializedAs("lightID")] public int LightID;
    [FormerlySerializedAs("propGroup")] public int PropGroup;

    private float colorTime;

    private float startTime;
    private Color startColor = Color.white;

    [FormerlySerializedAs("currentAlpha")] [SerializeField]
    private float startAlpha;

    private float endTime;
    private float endAlpha;
    private Color endColor = Color.white;
    private Func<float, float> easing = Easing.ByName["easeLinear"];

    private MaterialPropertyBlock lightPropertyBlock;
    private Renderer lightRenderer;

    private BoostSprite boostSprite;

    private bool isLightEnabled = true;

    private static readonly int mainTex = Shader.PropertyToID("_MainTex");
    private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int baseColor = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        lightPropertyBlock = new MaterialPropertyBlock();
        lightRenderer = GetComponentInChildren<Renderer>();
        boostSprite = GetComponent<BoostSprite>();


        if (lightRenderer is SpriteRenderer spriteRenderer)
        {
            if (boostSprite != null) boostSprite.Setup(spriteRenderer.sprite);

            lightPropertyBlock.SetTexture(mainTex, spriteRenderer.sprite.texture);
        }

        if (!OverrideLightGroup) return;
        var descriptor = LoadInitialMap.Platform;

        // TODO: Add types?
        if (descriptor != null
            && OverrideLightGroupID >= 0
            && OverrideLightGroupID < descriptor.LightingManagers.Length)
        {
            var lm = descriptor.LightingManagers[OverrideLightGroupID];
            while (lm.LightIDPlacementMapReverse?.ContainsKey(LightID) ?? false)
            {
                ++LightID;
            }

            lm.ControllingLights.Add(this);
            lm.LoadOldLightOrder();
        }
    }

    private void OnDestroy()
    {
        if (OverrideLightGroup)
        {
            var descriptor = LoadInitialMap.Platform;

            if (descriptor != null
                && OverrideLightGroupID >= 0
                && OverrideLightGroupID < descriptor.LightingManagers.Length)
            {
                var lm = descriptor.LightingManagers[OverrideLightGroupID];
                lm.ControllingLights.Remove(this);
                lm.LightIDPlacementMapReverse?.Remove(LightID);
            }
        }
    }

    private void UpdateLighting(Color color, float alpha)
    {
        if (!isLightEnabled) return;
        lightPropertyBlock.SetColor(emissionColor, color);
        lightPropertyBlock.SetColor(baseColor, Color.white * alpha);
        lightRenderer.SetPropertyBlock(lightPropertyBlock);
    }

    public void UpdateTime(float time)
    {
        if (float.IsNaN(multiplyAlpha)) multiplyAlpha = 0;

        var normalizedTime = Mathf.Clamp01((time - startTime) / (endTime - startTime));
        var easeTime = easing(normalizedTime);
        var color = Color.Lerp(startColor, endColor, easeTime);
        var alpha = Mathf.Lerp(startAlpha, endAlpha, easeTime) * multiplyAlpha;

        SetEmission(alpha > 0);
        UpdateLighting(color, alpha);
    }

    public void UpdateEasing(string easingName) => easing = Easing.ByName[easingName];
    public void UpdateEasing(Func<float, float> _easing) => easing = _easing;
    public void UpdateStartAlpha(float target) => startAlpha = target;
    public void UpdateStartColor(Color color) => startColor = color;
    public void UpdateStartTime(float time) => startTime = time;
    public void UpdateEndTime(float time) => endTime = time;
    public void UpdateEndAlpha(float target) => endAlpha = target;
    public void UpdateEndColor(Color target) => endColor = target;
    public void UpdateMultiplyAlpha(float target = 1) => multiplyAlpha = Mathf.Clamp(target, 0f, 1.5f);

    public void UpdateBoostState(bool boost)
    {
        if (boostSprite != null) lightPropertyBlock.SetTexture(mainTex, boostSprite.GetSprite(boost).texture);
    }

    private void SetEmission(bool enabled)
    {
        if (isLightEnabled != enabled)
        {
            lightRenderer.enabled = isLightEnabled = enabled;
        }
    }
}
