using Newtonsoft.Json;
using UnityEngine;

public class SwitchGameObjectEffectTargetData : EnvironmentComponentData<SwitchGameObjectFx>
{
    [JsonProperty("gameObject01")] public string GameObjectA;
    [JsonProperty("gameObject02")] public string GameObjectB;

    public override void FillComponents(GameObject self, SwitchGameObjectFx comp, CreateContainer container)
    {
        comp.GameObjectA = container.GetGameObjectOrNull(GameObjectA, self);
        comp.GameObjectB = container.GetGameObjectOrNull(GameObjectB, self);

        comp.GameObjectA.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.GameObjectA.GetComponent<ChromaIDMarker>().MarkActivator = true;
        comp.GameObjectB.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.GameObjectB.GetComponent<ChromaIDMarker>().MarkActivator = true;
    }
}
