using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshColliderData : EnvironmentComponentData<MeshCollider>
{
    public string Mesh;

    public override void FillComponents(GameObject self, MeshCollider comp, CreateContainer container) =>
        comp.sharedMesh = container.Library.Meshes.GetSafe(Mesh);
}
