# Plugin Guide

ChroMapper has it's own included basic Plugin loader, and can load plugins from the `Plugins` folder at the root of it's directory.

These Plugins can be used to add more functionality to the editor, or perhaps customize and edit the look and feel of the program, or to simply develop code that would not make sense to be contained to a separate fork of ChroMapper.

## Plugin Project Setup
* In Visual Studio, create a new Class Library that targets .NET Framework (.NET Standard and .NET Core are untested, use at your own risk).
* Reference `Assembly-CSharp.dll` from `ChroMapper_Data/Managed`. You'll also find Unity's assemblies and some other libraries here you may want to reference.
* Create a class that ChroMapper will load that looks like the example below.
* Build your project, and throw it into your `Plugins` folder.

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

## Legal

Some users might be thinking: What about GPLv2? Wouldn't these plugins be forced to be distributed and released under GPLv2?

After carefully reviewing the GPLv2 license, I have decided that these plugins would fall under this clause:

```
If identifiable sections of that work are not derived from the Program, and can be reasonably considered independent and separate works in themselves, then this License, and its terms, do not apply to those sections when you distribute them as separate works.
```

Essentially: No, GPLv2's infamous "viral" effect will not apply to plugins you create for ChroMapper.

In a little more detail: I will not consider plugins for ChroMapper to be derived from it, and will instead consider them as independent and separate works, thereby making GPLv2 not apply Sections 2 and 3, which talks about making source code and modifications available under the same license.