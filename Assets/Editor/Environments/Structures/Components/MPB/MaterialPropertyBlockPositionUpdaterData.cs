using UnityEngine;

public class MaterialPropertyBlockPositionUpdaterData : EnvironmentComponentData<MaterialPropertyBlockPositionAnimator>
{
    public string Property;
    public int TargetTransform;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockPositionAnimator comp,
        CreateContainer container)
    {
        comp.Controller = self.GetComponent<MaterialPropertyBlockController>();
        comp.TargetTransform = container.GetComponentOrNull<Transform>(TargetTransform);
        comp.TargetTransform.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.Property = Property;
    }
}
