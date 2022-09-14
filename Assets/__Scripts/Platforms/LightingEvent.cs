using System;
using UnityEngine;
using UnityEngine.Serialization;

public class LightingEvent : MonoBehaviour
{
    public bool OverrideLightGroup;
    public int OverrideLightGroupID;
    public bool UseInvertedPlatformColors;
    public bool CanBeTurnedOff = true;

    [SerializeField] private float currentAlpha;

    [SerializeField] private float multiplyAlpha = 1;

    [FormerlySerializedAs("lightID")] public int LightID;
    [FormerlySerializedAs("propGroup")] public int PropGroup;
    private float alphaTime;
    private float colorTime;

    private Color currentColor = Color.white;

    private MaterialPropertyBlock lightPropertyBlock;
    private Renderer lightRenderer;
    private float targetAlpha;

    private Color targetColor = Color.white;
    private float timeToTransitionAlpha;
    private float timeToTransitionColor;

    private BoostSprite boostSprite;

    private bool isLightEnabled = true;

    private Func<float, float> easing = Easing.ByName["easeLinear"];

    public int LightIdx = -1; // each light in the group has its unique LightIdx. This is used in LightsManagerV3
    private int currentNoteIdx = -1;
    public int TargetColor = 0;

    private void Start()
    {
        lightPropertyBlock = new MaterialPropertyBlock();
        lightRenderer = GetComponentInChildren<Renderer>();
        boostSprite = GetComponent<BoostSprite>();


        if (lightRenderer is SpriteRenderer spriteRenderer)
        {
            if (boostSprite != null)
                boostSprite.Setup(spriteRenderer.sprite);

            lightPropertyBlock.SetTexture("_MainTex", spriteRenderer.sprite.texture);
        }

        if (OverrideLightGroup)
        {
            var descriptor = GetComponentInParent<PlatformDescriptor>();

            if (descriptor != null)
            {
                descriptor.LightingManagers[OverrideLightGroupID].ControllingLights.Add(this);
            }
        }
    }

    private void Update()
    {
        if (multiplyAlpha == float.NaN) multiplyAlpha = 0;

        colorTime += Time.deltaTime;
        var color = Color.Lerp(currentColor, targetColor, easing(colorTime / timeToTransitionColor));

        if (!CanBeTurnedOff)
        {
            lightPropertyBlock.SetColor("_BaseColor", Color.white);
            lightPropertyBlock.SetColor("_EmissionColor", color);
            SetEmission(true);
            lightRenderer.SetPropertyBlock(lightPropertyBlock);
            return;
        }

        alphaTime += Time.deltaTime;
        var alpha = Mathf.Lerp(currentAlpha, targetAlpha, easing(alphaTime / timeToTransitionAlpha)) * multiplyAlpha;

        SetEmission(alpha > 0);

        if (isLightEnabled)
        {
            lightPropertyBlock.SetColor("_EmissionColor", color);
            lightPropertyBlock.SetColor("_BaseColor", Color.white * alpha);
            lightRenderer.SetPropertyBlock(lightPropertyBlock);
        }
    }

    public void UpdateEasing(string easingName) => easing = Easing.ByName[easingName];

    public void UpdateTargetColor(Color target, float timeToTransition)
    {
        // currentColor = targetColor;
        targetColor = target;
        timeToTransitionColor = timeToTransition;
        colorTime = 0;
        if (timeToTransition == 0) currentColor = target;
    }

    public void SetTargetColor(Color target) => targetColor = target;

    public void UpdateTargetAlpha(float target, float timeToTransition)
    {
        if (!CanBeTurnedOff) return;
        // currentAlpha = targetAlpha; //I do not believe this is needed, but will leave it just incase.
        targetAlpha = target;
        timeToTransitionAlpha = timeToTransition;
        alphaTime = 0;
        if (timeToTransition == 0) currentAlpha = target;
    }

    public void UpdateMultiplyAlpha(float target = 1)
    {
        if (!CanBeTurnedOff) return;
        multiplyAlpha = Mathf.Clamp(target, 0f, 1.5f);
    }

    public void UpdateBoostState(bool boost)
    {
        if (boostSprite != null)
          lightPropertyBlock.SetTexture("_MainTex", boostSprite.GetSprite(boost).texture);
    }

    public void UpdateCurrentColor(Color color) => currentColor = color;

    public void UpdateTargetAlpha(float target)
    {
        if (!CanBeTurnedOff) return;
        targetAlpha = target;
    }

    private void SetEmission(bool enabled)
    {
        if (isLightEnabled != enabled)
        {
            lightRenderer.enabled = isLightEnabled = enabled;
        }
    }


    public bool SetNoteIndex(int noteIdx, bool force = false)
    {
        if (!force && noteIdx < currentNoteIdx)
        {
            return false;
        }
        currentNoteIdx = noteIdx;
        return true;
    }
}
