using System.Linq;
using UnityEngine;

public class MeshRendererSwitchEventEffectData : EnvironmentComponentData<MeshRendererSwitch>
{
    public EnvironmentEventType EventType;
    public int[] ActivateOnBoostRenderers;
    public int[] DeactivateOnBoostRenderers;

    public override void FillComponents(GameObject self, MeshRendererSwitch comp, CreateContainer container)
    {
        comp.Effect = container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
            EventType);

        comp.NormalRenderers = DeactivateOnBoostRenderers
            .Select(container.GetComponentOrNull<Renderer>)
            .Where(y => y != null)
            .Select(g =>
            {
                g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToArray();
        comp.BoostRenderers = ActivateOnBoostRenderers
            .Select(container.GetComponentOrNull<Renderer>)
            .Where(y => y != null)
            .Select(g =>
            {
                g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToArray();
    }
}
