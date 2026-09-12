using UnityEngine;

public class LightPairRotationEventEffectData : EnvironmentComponentData<LightPairRotation>
{
    public EnvironmentEventType EventTypeL;
    public int TransformL;
    public EnvironmentEventType EventTypeR;
    public int TransformR;
    public EnvironmentEventType SwitchOverrideRandomValuesEvent;
    public Vector3 RotationVector;
    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;
    public float StartRotation;

    public override void FillComponents(GameObject self, LightPairRotation comp, CreateContainer container)
    {
        comp.enabled = true;
        var effect = self.AddComponent<LightPairRotationEffect>();
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
            new LightPairRotation.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
        comp.RotationVector = RotationVector;
        comp.OverrideRandomValues = OverrideRandomValues;
        comp.UseZPositionForAngleOffset = UseZPositionForAngleOffset;
        comp.ZPositionAngleOffsetScale = ZPositionAngleOffsetScale;
        comp.StartRotation = StartRotation;
    }
}
