using System;
using UnityEngine;

public abstract class EnvironmentComponentData
{
    [NonSerialized] public Component Instance;
    public virtual int Priority => 0;

    public bool IsEnabled;
    public int InstanceId;

    public virtual bool AllowNew => true;

    public abstract void Apply(CreateContainer container);
    public abstract void SpawnComponent(GameObject self);
}

/// <summary>
/// Base class for a single component of an environment object. The class itself is simply a data type used for deserialization, however provides a method to apply its properties to a Unity / ChroMapper component.
/// </summary>
/// <typeparam name="T">Unity / ChroMapper component to copy data to.</typeparam>
public abstract class EnvironmentComponentData<T> : EnvironmentComponentData where T : Component
{
    public override void SpawnComponent(GameObject self)
    {
        if (Instance != null) return;
        if (!AllowNew)
        {
            Instance = self.GetComponent<T>();
            return;
        }

        var comp = self.AddComponent<T>();
        if (comp is Behaviour b) b.enabled = IsEnabled;
        Instance = comp;
    }

    public override void Apply(CreateContainer container)
    {
        var comp = Instance as T;
        if (comp == null)
        {
            Debug.LogError($"{this} missing");
        }

        var self = comp.gameObject;
        FillComponents(self, comp, container);
    }

    public T GetComponent() => Instance as T;

    public abstract void FillComponents(GameObject self, T comp, CreateContainer container);
}
