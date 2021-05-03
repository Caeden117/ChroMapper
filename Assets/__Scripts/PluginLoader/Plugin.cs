using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Plugin
{
    private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public string Name { get; private set; }
    public Version Version { get; private set; }

    private object pluginInstance;

    private Dictionary<Type, MethodInfo> methods = new Dictionary<Type, MethodInfo>();
    private List<Type> attributes = new List<Type>()
    {
        typeof(InitAttribute),
        typeof(ObjectLoadedAttribute),
        typeof(EventPassedThresholdAttribute),
        typeof(NotePassedThresholdAttribute),
        typeof(ExitAttribute)
    };

    public Plugin(string name, Version version, object pluginInstance)
    {
        Name = name;
        Version = version;
        this.pluginInstance = pluginInstance;
        foreach(MethodInfo methodInfo in pluginInstance.GetType().GetMethods(BINDING_FLAGS))
        {
            foreach (Type t in attributes)
            {
                if (methodInfo.GetCustomAttribute(t) != null)
                    methods.Add(t, methodInfo);
            }
        }
    }

    public void CallMethod<T>()
    {
        methods.TryGetValue(typeof(T), out var methodInfo);
        methodInfo?.Invoke(pluginInstance, new object[0]);
    }

    public void CallMethod<T, S>(S obj)
    {
        methods.TryGetValue(typeof(T), out var methodInfo);
        methodInfo?.Invoke(pluginInstance, new object[1] { obj });
    }

    public void Init()
    {
        CallMethod<InitAttribute>();
        Debug.Log($"Loaded Plugin: {Name} - v{Version}");
    }
}
