using System.Linq;
using UnityEngine;

public class GameObjectIntSwitchEventEffectData : EnvironmentComponentData<GameObjectIntSwitch>
{
    public EnvironmentEventType EventType;
    public int DefaultValue;
    public GameObjectsValue[] GameObjectsValueLists;

    public class GameObjectsValue
    {
        public int Value;
        public string[] GameObjectIds;
    }

    public override void FillComponents(GameObject self, GameObjectIntSwitch comp, CreateContainer container)
    {
        comp.Effect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                EventType);

        comp.GameObjectsValueContainers =
            GameObjectsValueLists
                .Select(x => new GameObjectIntSwitch.GameObjectsValueContainer
                {
                    Value = x.Value,
                    GameObjects =
                        x
                            .GameObjectIds.Select(y => container.GetGameObjectOrNull(y, self))
                            .Where(y => y != null)
                            .Select(g =>
                            {
                                g.GetComponent<ChromaIDMarker>().MarkUse = true;
                                g.GetComponent<ChromaIDMarker>().MarkActivator = true;
                                return g;
                            })
                            .ToArray()
                })
                .ToArray();
        comp.DefaultValue = DefaultValue;
    }
}
