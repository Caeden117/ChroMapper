using UnityEngine;

public class CollectionFx : FxTarget
{
    [SerializeField] public FxTarget[] Targets;

    public override void SetValue(int group, int id, float value)
    {
        for (var i = 0; i < Targets.Length; i++) Targets[i].SetValue(group, id, value);
    }

    public override void TriggerValue(int group, int id, float value)
    {
        for (var i = 0; i < Targets.Length; i++) Targets[i].TriggerValue(group, id, value);
    }
}
