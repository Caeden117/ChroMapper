using UnityEngine;

public class SpringJointData : EnvironmentComponentData<SpringJoint>
{
    public int ConnectedBody;
    public int ConnectedArticulationBody;
    public Vector3 Anchor;
    public bool AutoConfigureConnectedAnchor;
    public Vector3 ConnectedAnchor;
    public float Spring;
    public float Damper;
    public float MinDistance;
    public float MaxDistance;
    public float Tolerance;
    public string BreakForce;
    public string BreakTorque;
    public bool EnableCollision;
    public bool EnablePreprocessing;
    public float MassScale;
    public float ConnectedMassScale;

    public override void FillComponents(GameObject self, SpringJoint comp, CreateContainer container)
    {
        comp.connectedBody = container.GetComponentOrNull<Rigidbody>(ConnectedBody);
        comp.connectedArticulationBody = container.GetComponentOrNull<ArticulationBody>(ConnectedArticulationBody);
        comp.anchor = Anchor;
        comp.autoConfigureConnectedAnchor = AutoConfigureConnectedAnchor;
        comp.connectedAnchor = ConnectedAnchor;
        comp.spring = Spring;
        comp.damper = Damper;
        comp.minDistance = MinDistance;
        comp.maxDistance = MaxDistance;
        comp.tolerance = Tolerance;
        comp.breakForce = float.Parse(BreakForce);
        comp.breakTorque = float.Parse(BreakTorque);
        comp.enableCollision = EnableCollision;
        comp.enablePreprocessing = EnablePreprocessing;
        comp.massScale = MassScale;
        comp.connectedMassScale = ConnectedMassScale;
    }
}
