using System;
using System.Reflection;
using UnityEngine;

public class Plugin
{
    private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public string Name { get; private set; }
    public Version Version { get; private set; }

    private object pluginInstance;

    private MethodInfo initMethod;
    private MethodInfo exitMethod;

    public Plugin(string name, Version version, object pluginInstance)
    {
        Name = name;
        Version = version;
        this.pluginInstance = pluginInstance;
        foreach(MethodInfo methodInfo in pluginInstance.GetType().GetMethods(BINDING_FLAGS))
        {
            if (methodInfo.GetCustomAttribute<InitAttribute>() != null)
                initMethod = methodInfo;
            if (methodInfo.GetCustomAttribute<ExitAttribute>() != null)
                exitMethod = methodInfo;
        }
    }

    public void Init()
    {
        initMethod?.Invoke(pluginInstance, new object[0]);
        Debug.Log($"Loaded Plugin: {Name} - v{Version}");
    }

    public void Exit()
    {
        exitMethod?.Invoke(pluginInstance, new object[0]);
    }
}
