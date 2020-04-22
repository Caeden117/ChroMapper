using UnityEngine;

public class LightingEvent : MonoBehaviour {

    private static readonly int LightingEventRenderQueue = 2925;

    [HideInInspector] public Material LightMaterial;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;
    public bool UseInvertedPlatformColors = false;
    public bool CanBeTurnedOff = true;

    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    private float currentAlpha = 0;
    private float targetAlpha = 0;
    private float alphaTime = 0;
    private float colorTime = 0;
    private float timeToTransitionColor = 0;
    private float timeToTransitionAlpha = 0;

	// Use this for initialization
	void Start () {
        LightMaterial = GetComponentInChildren<Renderer>().material;
        LightMaterial.renderQueue = LightingEventRenderQueue;
        if (OverrideLightGroup)
        {
            PlatformDescriptor descriptor = GetComponentInParent<PlatformDescriptor>();
            descriptor?.LightingManagers[OverrideLightGroupID].ControllingLights.Add(this);
        }
    }

    private void Update()
    {
        colorTime += Time.deltaTime;
        LightMaterial.SetColor("_EmissionColor", Color.Lerp(currentColor, targetColor, colorTime / timeToTransitionColor));
        SetEmission(currentColor.a > 0 || currentAlpha > 0);
        if (!CanBeTurnedOff)
        {
            LightMaterial.SetColor("_BaseColor", Color.white);
            return;
        }
        alphaTime += Time.deltaTime;
        LightMaterial.SetColor("_BaseColor", Color.white * Mathf.Lerp(currentAlpha, targetAlpha, alphaTime / timeToTransitionAlpha));
    }

    public void UpdateTargetColor(Color target, float timeToTransition)
    {
        currentColor = targetColor;
        targetColor = target;
        timeToTransitionColor = timeToTransition;
        colorTime = 0;
        if (timeToTransition == 0) currentColor = target;
    }

    public void UpdateTargetAlpha(float target, float timeToTransition)
    {
        if (!CanBeTurnedOff) return;
        currentAlpha = targetAlpha;
        targetAlpha = target;
        timeToTransitionAlpha = timeToTransition;
        alphaTime = 0;
        if (timeToTransition == 0) currentAlpha = target;
    }

    private void SetEmission(bool enabled)
    {
        if (enabled)
        {
            LightMaterial.EnableKeyword("_EMISSION");
            LightMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
        }
        else
        {
            LightMaterial.DisableKeyword("_EMISSION");
            LightMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
    }
}
