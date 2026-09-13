using System;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectIntSwitch : MonoBehaviour
{
    public GenericCallbackEventEffect Effect;
    public GameObjectsValueContainer[] GameObjectsValueContainers;
    public int DefaultValue;

    private int lastActiveValue;
    private readonly Dictionary<int, GameObjectsValueContainer> valueToContainer = new();

    private void Start()
    {
        foreach (var container in GameObjectsValueContainers)
        {
            SetContainerActive(container, container.Value == DefaultValue);
            valueToContainer.Add(container.Value, container);
        }

        lastActiveValue = DefaultValue;
        Effect.OnStateChanged += HandleStateChanged;
        var p = Effect.GetCurrentState();
        if (p.index != -1) HandleStateChanged(p);
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged((int index, BasicEventStateData state) data)
    {
        var evt = data.state.Base;
        if (evt.Value == lastActiveValue) return;

        if (valueToContainer.TryGetValue(lastActiveValue, out var container) && container.IsActive)
            SetContainerActive(container, false);

        if (valueToContainer.TryGetValue(evt.Value, out container))
        {
            SetContainerActive(container, true);
            lastActiveValue = evt.Value;
        }
    }

    private static void SetContainerActive(GameObjectsValueContainer container, bool active)
    {
        if (container.IsActive != active)
            foreach (var go in container.GameObjects)
                go.SetActive(active);

        container.IsActive = active;
    }

    [Serializable]
    public class GameObjectsValueContainer
    {
        public int Value;
        public GameObject[] GameObjects;
        [NonSerialized] public bool IsActive;
    }
}
