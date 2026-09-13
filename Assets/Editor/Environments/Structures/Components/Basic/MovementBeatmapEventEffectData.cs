using System.Linq;
using UnityEngine;

public class MovementBeatmapEventEffectData : EnvironmentComponentData<Movement>
{
    public EnvironmentEventType EventType;
    public float TransitionSpeed;
    public MovementDataComponent[] MovementData;
    public string[] Transforms;

    public class MovementDataComponent
    {
        public Vector3 LocalPositionOffset;
    }

    public override void FillComponents(GameObject self, Movement comp, CreateContainer container)
    {
        comp.enabled = true;
        
        comp.Transforms = Transforms
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.transform : null)
            .Where(y => y != null)
            .ToArray();
        foreach (var t in comp.Transforms) t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.TransitionSpeed = TransitionSpeed;
        comp.MovementData = MovementData.Select(x => x.LocalPositionOffset).ToArray();

        var effect = self.AddComponent<MovementEffect>();
        effect.Visual = comp;
        container.Descriptor.BasicEventEffectManager.Register(EventType, effect);
    }
}
