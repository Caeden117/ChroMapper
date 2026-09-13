using System.Linq;
using UnityEngine;

public class GameObjectSwitchEventEffectData : EnvironmentComponentData<GameObjectSwitch>
{
    public string[] ActivateOnBoostObjects;
    public string[] DeactivateOnBoostObjects;

    public override void FillComponents(GameObject self, GameObjectSwitch comp, CreateContainer container)
    {
        comp.Effect = container.Descriptor.BasicEventEffectManager.GetEffect<ColorBoostEffect>(5);

        comp.NormalGameObjects = DeactivateOnBoostObjects
            .Select(x => container.GetGameObjectOrNull(x, self))
            .Where(y => y != null)
            .Select(g =>
            {
                g.GetComponent<ChromaIDMarker>().MarkUse = true;
                g.GetComponent<ChromaIDMarker>().MarkActivator = true;
                return g;
            })
            .ToArray();
        comp.BoostGameObjects = ActivateOnBoostObjects
            .Select(x => container.GetGameObjectOrNull(x, self))
            .Where(y => y != null)
            .Select(g =>
            {
                g.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToArray();
    }
}
