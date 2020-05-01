# Plugin Guide

ChroMapper has the ability to load plugins from the `Plugins` folder at the root of it's directory.

## Plugin Project Setup
* Reference `Assembly-CSharp.dll` from `ChroMapper_Data/Managed`, you'll also find Unity's assemblies and some other libraries here you may want to reference
* Create a class that ChroMapper will load that looks like the following: 

```csharp
    [Plugin("Plugin Name")]
    public class Plugin
    {
        [Init]
        private void Init()
        {
            Debug.Log("Plugin has loaded!");
        }
        [Exit]
        private void Exit()
        {
            Debug.Log("Application has closed!");
        }
    }
```
For now the plugin interface is very barebones, it may be expanded upon in the future.