using UnityEngine;

public class LightPairSinMoveEventEffectData : EnvironmentComponentData<LightPairSinMove>
{
    public EnvironmentEventType EventTypeL;
    public int TransformL;
    public EnvironmentEventType EventTypeR;
    public int TransformR;
    public EnvironmentEventType SwitchOverrideRandomValuesEvent;
    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    public override void FillComponents(GameObject self, LightPairSinMove comp, CreateContainer container)
    {
        comp.enabled = true;
        var effect = self.AddComponent<LightPairSinMoveEffect>();
        effect.Visual = comp;
        if (EventTypeL != -1)
        {
            effect.LeftEventType = EventTypeL;
            container.Descriptor.BasicEventEffectManager.Register(EventTypeL, effect);
        }
        if (EventTypeR != -1)
        {
            effect.RightEventType = EventTypeR;
            container.Descriptor.BasicEventEffectManager.Register(EventTypeR, effect);
        }
        if (SwitchOverrideRandomValuesEvent != -1)
        {
            effect.SwitchEventType = SwitchOverrideRandomValuesEvent;
            container.Descriptor.BasicEventEffectManager.Register(SwitchOverrideRandomValuesEvent, effect);
        }

        var lT = container.GetComponentOrNull<Transform>(TransformL);
        lT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        var rT = container.GetComponentOrNull<Transform>(TransformR);
        rT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.Transforms =
            new LightPairSinMove.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
        comp.OverrideRandomValues = OverrideRandomValues;
        comp.StartValueOffset = StartValueOffset;
        comp.StartPositionOffset = StartPositionOffset;
        comp.EndPositionOffset = EndPositionOffset;
    }
}
