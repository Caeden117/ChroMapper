using System.Linq;
using UnityEngine;

public class MaterialPropertyBlockControllerData : EnvironmentComponentData<MaterialPropertyBlockController>
{
    public int[] Renderers;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockController comp,
        CreateContainer container)
    {
        comp.Renderers = Renderers
            .Select(container.GetComponentOrNull<Renderer>)
            .Where(y => y != null)
            .Select(g =>
            {
                g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToList();
    }
}
