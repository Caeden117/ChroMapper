using UnityEngine;

public class LightingEvent : MonoBehaviour {

    private static readonly int LightingEventRenderQueue = 2925;

    [HideInInspector] public Material LightMaterial;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;

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
}
