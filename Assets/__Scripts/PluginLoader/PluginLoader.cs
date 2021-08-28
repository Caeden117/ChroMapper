using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

internal class PluginLoader : MonoBehaviour
{
    private const string PLUGIN_DIR = "Plugins";
    private const bool LOAD_PLUGINS_IN_EDITOR = false;

    //there shouldn't be any issues with making this static, but if there are let me know
    private static readonly List<Plugin> Plugins = new List<Plugin>();

    /// <summary>
    ///     Get all currently loaded ChroMapper plugins.
    ///     This does NOT include plugins added by external mod loaders (BepinEx, IPA, BSIPA, etc.)
    /// </summary>
    public static IReadOnlyList<Plugin> LoadedPlugins => Plugins.AsReadOnly();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (!Application.isEditor || LOAD_PLUGINS_IN_EDITOR)
            LoadAssemblies();
    }

    private void OnDestroy() => BroadcastEvent<ExitAttribute>();

    private void LoadAssemblies()
    {
        if (!Directory.Exists(PLUGIN_DIR))
            Directory.CreateDirectory(PLUGIN_DIR);
        foreach (var file in Directory.GetFiles(PLUGIN_DIR, "*.dll", SearchOption.AllDirectories))
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
                Plugins.Add(
                    new Plugin(pluginAttribute.Name, assembly.GetName().Version, Activator.CreateInstance(type)));
            }
        }

        foreach (var plugin in Plugins)
            plugin.Init();
    }

    public static void BroadcastEvent<T>() where T : Attribute
    {
        foreach (var plugin in Plugins)
            plugin.CallMethod<T>();
    }

    public static void BroadcastEvent<T, TS>(TS obj) where T : Attribute
    {
        foreach (var plugin in Plugins)
            plugin.CallMethod<T, TS>(obj);
    }
}
