using System.Linq;
using UnityEngine;

public class SDFArrayManagerData : EnvironmentComponentData<SDFArrayManager>
{
    public int[] SDFPointArray;

    public override void FillComponents(GameObject self, SDFArrayManager comp, CreateContainer container)
    {
        if (SDFPointArray == null)
            throw new System.InvalidOperationException("SDF array manager requires point references.");

        comp.SetSdfPoints(SDFPointArray.Select(container.GetComponentOrNull<SDFPoint>).ToArray());
    }
}
