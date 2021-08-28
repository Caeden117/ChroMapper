using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Plugin
{
    private const BindingFlags bindingFlags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly List<Type> attributes = new List<Type>
    {
        typeof(InitAttribute),
        typeof(ObjectLoadedAttribute),
        typeof(EventPassedThresholdAttribute),
        typeof(NotePassedThresholdAttribute),
        typeof(ExitAttribute)
    };

    private readonly Dictionary<Type, MethodInfo> methods = new Dictionary<Type, MethodInfo>();

    private readonly object pluginInstance;

    public Plugin(string name, Version version, object pluginInstance)
    {
        Name = name;
        Version = version;
        this.pluginInstance = pluginInstance;
        foreach (var methodInfo in pluginInstance.GetType().GetMethods(bindingFlags))
        {
            foreach (var t in attributes)
            {
                if (methodInfo.GetCustomAttribute(t) != null)
                methods.Add(t, methodInfo);
            }
        }
    }

    public string Name { get; }
    public Version Version { get; }

    public void CallMethod<T>()
    {
        methods.TryGetValue(typeof(T), out var methodInfo);
        methodInfo?.Invoke(pluginInstance, new object[0]);
    }

    public void CallMethod<T, TS>(TS obj)
    {
        methods.TryGetValue(typeof(T), out var methodInfo);
        methodInfo?.Invoke(pluginInstance, new object[1] {obj});
    }

    public void Init()
    {
        CallMethod<InitAttribute>();
        Debug.Log($"Loaded Plugin: {Name} - v{Version}");
    }
}
