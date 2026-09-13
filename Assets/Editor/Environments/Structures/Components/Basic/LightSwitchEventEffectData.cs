using UnityEngine;

public class LightSwitchEventEffectData
{
    public EnvironmentEventType EventType;
    public float OffColorIntensity;
    public bool LightOnStart;
    public int LightsId;

    public void FillComponents(GameObject self, BasicLightEffect comp, CreateContainer container)
    {
        comp.OffIntensity = OffColorIntensity;
        comp.LightOnStart = LightOnStart;
    }
}
