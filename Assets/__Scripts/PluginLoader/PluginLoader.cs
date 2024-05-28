using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

internal class PluginLoader : MonoBehaviour
{
    private const string pluginDir = "Plugins";
    private const bool loadPluginsInEditor = false;

    //there shouldn't be any issues with making this static, but if there are let me know
    private static readonly List<Plugin> plugins = new List<Plugin>();

    /// <summary>
    ///     Get all currently loaded ChroMapper plugins.
    ///     This does NOT include plugins added by external mod loaders (BepinEx, IPA, BSIPA, etc.)
    /// </summary>
    public static IReadOnlyList<Plugin> LoadedPlugins => plugins.AsReadOnly();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (!Application.isEditor || loadPluginsInEditor)
            LoadAssemblies();
    }

    private void OnDestroy() => BroadcastEvent<ExitAttribute>();

    private void LoadAssemblies()
    {
        if (!Directory.Exists(pluginDir))
            Directory.CreateDirectory(pluginDir);
        foreach (var file in Directory.GetFiles(pluginDir, "*.dll", SearchOption.AllDirectories))
        {
            var assembly = Assembly.LoadFile(Path.GetFullPath(file));
            foreach (var type in assembly.GetExportedTypes())
            {
                PluginAttribute pluginAttribute = null;
                try
                {
                    pluginAttribute = type.GetCustomAttribute<PluginAttribute>();
                }
                catch (Exception) { }

                ;

                if (pluginAttribute == null) continue;
                try {
                    var plugin = new Plugin(pluginAttribute.Name, assembly.GetName().Version, Activator.CreateInstance(type));
                    plugins.Add(plugin);
                }
                catch (Exception e) {
                    Debug.LogError($"Incompatible plugin {pluginAttribute.Name}, please check for an update or remove it!");
                    Debug.LogException(e);
                }
            }
        }

        foreach (var plugin in plugins)
            plugin.Init();
    }

    public static void BroadcastEvent<T>() where T : Attribute
    {
        foreach (var plugin in plugins)
            plugin.CallMethod<T>();
    }

    public static void BroadcastEvent<T, TS>(TS obj) where T : Attribute
    {
        foreach (var plugin in plugins)
            plugin.CallMethod<T, TS>(obj);
    }
}
