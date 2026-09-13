using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombineGroupIdToVector4FloatFxGroupEffectTargetData : EnvironmentComponentData<CombineGroupIdToVector4Fx>
{
    public string PropertyName;
    public int MaterialPropertyBlockController;
    public Vector4 DefaultValue;
    public Dictionary<string, int> GroupIdToIndex;

    public override void FillComponents(GameObject self, CombineGroupIdToVector4Fx comp, CreateContainer container)
    {
        comp.PropertyName = PropertyName;
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.DefaultValue = DefaultValue;
        comp.LightGroupsToIndices =
            GroupIdToIndex
                .Select(x =>
                    new CombineGroupIdToVector4Fx.LightGroupToIndex { GroupId = int.Parse(x.Key), Index = x.Value })
                .ToArray();
    }
}
