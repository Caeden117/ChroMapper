using System;
using System.Reflection;
using UnityEngine;

internal class Plugin
{
    private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private string name;
    private Version version;
    private object pluginInstance;

    private MethodInfo initMethod;
    private MethodInfo exitMethod;

    public Plugin(string name, Version version, object pluginInstance)
    {
        this.name = name;
        this.version = version;
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
        Debug.Log($"Loaded Plugin: {name} - v{version}");
    }

    public void Exit()
    {
        exitMethod?.Invoke(pluginInstance, new object[0]);
    }
}
