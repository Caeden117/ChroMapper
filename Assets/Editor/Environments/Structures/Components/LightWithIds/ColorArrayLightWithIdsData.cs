using System.Linq;
using UnityEngine;

public class ColorArrayLightWithIdsData : EnvironmentComponentData<ColorArrayLightsController>
{
    public ColorArrayLightWithIdData[] ColorArrayLightWithIds;
    public MaterialControllerData MaterialController;
    public int[] MaterialPropertyBlockControllers;
    public string ColorsArrayPropertyName = "_ColorsArray";
    public string ColorsArrayOffsetPropertyName = "_ColorsArrayOffset";

    public override void FillComponents(
        GameObject self,
        ColorArrayLightsController comp,
        CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.Material = container.GetMaterialSafe(MaterialController.Material);
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(container.GetComponentOrNull<MaterialPropertyBlockController>)
            .ToArray();
        comp.ColorArrayData = ColorArrayLightWithIds
            .Select(data =>
            {
                var d = comp.gameObject.AddComponent<ColorArrayData>();
                d.Index = data.Index;
                return d;
            })
            .ToArray();

        comp.ColorsArrayPropertyName = ColorsArrayPropertyName;
        comp.ColorsArrayOffsetPropertyName = ColorsArrayOffsetPropertyName;
    }

    public class MaterialControllerData
    {
        public string Material;
    }

    public class ColorArrayLightWithIdData
    {
        public int Index;
    }
}
