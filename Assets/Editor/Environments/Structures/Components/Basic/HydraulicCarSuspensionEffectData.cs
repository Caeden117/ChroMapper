using UnityEngine;

public class HydraulicCarSuspensionEffectData : EnvironmentComponentData<HydraulicCarSuspension>
{
    public EnvironmentEventType ContractEventType;
    public int[] EventValues;
    public EnvironmentEventType ExpandEventType;
    public int Joint;
    public float ContractDistance = 0.3f;
    public float ExpandDistance = 0.4f;
    public int Rigidbody;

    public override void FillComponents(
        GameObject self,
        HydraulicCarSuspension comp,
        CreateContainer container)
    {
        comp.ContractEffect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                ContractEventType);
        comp.ExpandEffect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                ExpandEventType);

        comp.Rigidbody = container.GetComponentOrNull<Rigidbody>(Rigidbody);
        comp.Joint = container.GetComponentOrNull<ConfigurableJoint>(Joint);
        comp.EventValues = EventValues;
        comp.ContractDistance = ContractDistance;
        comp.ExpandDistance = ExpandDistance;
    }
}
