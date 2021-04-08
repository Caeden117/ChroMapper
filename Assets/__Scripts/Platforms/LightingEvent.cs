using UnityEngine;

public class LightingEvent : MonoBehaviour {

    private static readonly int LightingEventRenderQueue = 2925;

    [HideInInspector] public Material LightMaterial;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;
    public bool UseInvertedPlatformColors = false;
    public bool CanBeTurnedOff = true;

    private Color targetColor = Color.white;
    private float targetAlpha = 0;

    private Color currentColor = Color.white;
    [SerializeField]
    private float currentAlpha = 0;
    [SerializeField]
    private float multiplyAlpha = 1;
    private float alphaTime = 0;
    private float colorTime = 0;
    private float timeToTransitionColor = 0;
    private float timeToTransitionAlpha = 0;
    public int lightID;
    public int propGroup;

    // Use this for initialization
    void Start ()
    {
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
        if (multiplyAlpha == float.NaN)
        {
            multiplyAlpha = 0;
        }

        colorTime += Time.deltaTime;
        Color color = Color.Lerp(currentColor, targetColor, colorTime / timeToTransitionColor);
        LightMaterial.SetColor("_EmissionColor", color);

        if (!CanBeTurnedOff)
        {
            LightMaterial.SetColor("_BaseColor", Color.white);
            SetEmission(true);
            return;
        }

        alphaTime += Time.deltaTime;
        float alpha = Mathf.Lerp(currentAlpha, targetAlpha, alphaTime / timeToTransitionAlpha) * multiplyAlpha;
        LightMaterial.SetColor("_BaseColor", Color.white * alpha);

        SetEmission(alpha > 0);
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
        currentAlpha = targetAlpha; //I do not believe this is needed, but will leave it just incase.
        targetAlpha = target;
        timeToTransitionAlpha = timeToTransition;
        alphaTime = 0;
        if (timeToTransition == 0) currentAlpha = target;
    }

    public void UpdateMultiplyAlpha(float target = 1)
    {
        if (!CanBeTurnedOff) return;
        multiplyAlpha = target;
    }

    public void UpdateCurrentColor(Color color)
    {
        currentColor = color;
    }

    public void UpdateTargetAlpha(float target)
    {
        if (!CanBeTurnedOff) return;
        targetAlpha = target;
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
