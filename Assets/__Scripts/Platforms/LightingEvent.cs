using UnityEngine;

public class LightingEvent : MonoBehaviour {

    private static readonly int LightingEventRenderQueue = 2925;

    [HideInInspector] public Material LightMaterial;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;
    public bool UseInvertedPlatformColors = false;
    public bool CanBeTurnedOff = true;
    public Color TargetColor { get; private set; } = Color.white;
    public float TargetAlpha { get; private set; } = 0;

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
        Color color = Color.Lerp(currentColor, TargetColor, colorTime / timeToTransitionColor);
        LightMaterial.SetColor("_EmissionColor", color);

        if (!CanBeTurnedOff)
        {
            LightMaterial.SetColor("_BaseColor", Color.white);
            SetEmission(true);
            return;
        }

        alphaTime += Time.deltaTime;
        float alpha = Mathf.Lerp(currentAlpha, TargetAlpha, alphaTime / timeToTransitionAlpha) * multiplyAlpha;
        LightMaterial.SetColor("_BaseColor", Color.white * alpha);

        SetEmission(alpha > 0);
    }

    public void UpdateTargetColor(Color target, float timeToTransition)
    {
        currentColor = TargetColor;
        TargetColor = target;
        timeToTransitionColor = timeToTransition;
        colorTime = 0;
        if (timeToTransition == 0) currentColor = target;
    }

    public void UpdateTargetAlpha(float target, float timeToTransition)
    {
        if (!CanBeTurnedOff) return;
        currentAlpha = TargetAlpha; //I do not believe this is needed, but will leave it just incase.
        TargetAlpha = target;
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
        TargetAlpha = target;
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
