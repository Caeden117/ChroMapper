using UnityEngine;

public class MaterialPropertyBlockPositionAnimator : MonoBehaviour
{
    public MaterialPropertyBlockController Controller;
    public string Property;
    protected int PropertyId;

    public Transform TargetTransform;

    private void Awake()
    {
        PropertyId = Shader.PropertyToID(Property);
        enabled = Controller != null;
    }

    protected void Update()
    {
        SetProperty();
        Controller.ApplyChanges();
    }

    protected void SetProperty()
    {
        if (TargetTransform != null) Controller.Mpb.SetVector(PropertyId, TargetTransform.position);
    }
}
