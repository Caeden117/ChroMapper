using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

internal class PluginLoader : MonoBehaviour
{
    private const string PLUGIN_DIR = "Plugins";
    private const bool LOAD_PLUGINS_IN_EDITOR = false;

    /// <summary>
    /// Get all currently loaded ChroMapper plugins.
    /// This does NOT include plugins added by external mod loaders (BepinEx, IPA, BSIPA, etc.)
    /// </summary>
    public static IReadOnlyList<Plugin> LoadedPlugins => plugins.AsReadOnly();

    //there shouldn't be any issues with making this static, but if there are let me know
    private static List<Plugin> plugins = new List<Plugin>(); 

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        if(!Application.isEditor || LOAD_PLUGINS_IN_EDITOR)
            LoadAssemblies();
    }

    void OnDestroy()
    {
        BroadcastEvent<ExitAttribute>();
    }

    private void LoadAssemblies()
    {
        if (!Directory.Exists(PLUGIN_DIR))
            Directory.CreateDirectory(PLUGIN_DIR);
        foreach(string file in Directory.GetFiles(PLUGIN_DIR, "*.dll", SearchOption.AllDirectories))
        {
            Assembly assembly = Assembly.LoadFile(Path.GetFullPath(file));
            foreach (Type type in assembly.GetExportedTypes())
            {
                PluginAttribute pluginAttribute = null;
                try
                {
                    pluginAttribute = type.GetCustomAttribute<PluginAttribute>();
                } catch (Exception) { };

                if (pluginAttribute == null) continue;
                plugins.Add(new Plugin(pluginAttribute.name, assembly.GetName().Version, Activator.CreateInstance(type)));
            }
        }

        foreach (Plugin plugin in plugins)
            plugin.Init();
    }

    public static void BroadcastEvent<T>() where T : Attribute
    {
        foreach (Plugin plugin in plugins)
            plugin.CallMethod<T>();
    }

    public static void BroadcastEvent<T, S>(S obj) where T : Attribute
    {
        foreach (Plugin plugin in plugins)
            plugin.CallMethod<T, S>(obj);
    }
}
