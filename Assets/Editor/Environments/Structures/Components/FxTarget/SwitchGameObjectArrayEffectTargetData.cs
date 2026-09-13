using System.Linq;
using UnityEngine;

public class SwitchGameObjectArrayEffectTargetData : EnvironmentComponentData<SwitchGameObjectArrayFx>
{
    public GameObjectActivation[] GameObjects;

    public struct GameObjectActivation
    {
        public float Threshold;
        public string GameObject;
    }

    public override void FillComponents(GameObject self, SwitchGameObjectArrayFx comp, CreateContainer container)
    {
        comp.GameObjects = GameObjects
            .Select(x => (container.GetGameObjectOrNull(x.GameObject, self), x.Threshold))
            .Where(x => x.Item1 != null)
            .Select(x =>
            {
                x.Item1.GetComponent<ChromaIDMarker>().MarkUse = true;
                x.Item1.GetComponent<ChromaIDMarker>().MarkActivator = true;
                return new SwitchGameObjectArrayFx.GameObjectActivation
                {
                    GameObject = x.Item1, Threshold = x.Threshold
                };
            })
            .ToArray();
    }
}
