# Plugin Guide

ChroMapper has it's own included basic Plugin loader, and can load plugins from the `Plugins` folder at the root of it's directory.

These Plugins can be used to add more functionality to the editor, or perhaps customize and edit the look and feel of the program, or to simply develop code that would not make sense to be contained to a separate fork of ChroMapper.

## Plugin Project Setup
* In Visual Studio, create a new Class Library that targets .NET Framework. .NET Standard and .NET Core are untested, use at your own risk.
* Add a reference to the core ChroMapper file, `Main.dll`. It can be found in the `ChroMapper_Data/Managed` folder.
* Add references to any other ChroMapper or Unity library you may need. These can also be found in the `ChroMapper_Data/Managed` folder:
    * `UnityEngine.CoreModule.dll` - The core of Unity. Reference this if you need access to the Unity engine.
    * `UnityEngine.UI.dll` - Unity's UI system. Reference this if you want to create custom UI.
    * `Unity.TextMeshPro.dll` - The text system used by ChroMapper.
    * `Unity.InputSystem.dll` - The new Input System, heavily used by ChroMapper.
    * `UnityEngine.InputLegacyModule.dll` - The legacy Input System, which is a bit simpler to use.
    * `FileBrowser.dll` - The cross-platform File Browser system used by ChroMapper.
    * `Intersections.dll` - ChroMapper's custom made, fast raycasting system.
    * `Plugins.dll` - Contains ChroMapper's JSON library, if you don't want to import your own.
* Create a class that ChroMapper will load that looks like the example below.
* Build your project, and put the built `.dll` file into your `Plugins` folder.

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

## ExtensionButtons
You can register your own buttons to appear on the in editor right side pop out panel. You should do this in your plugin's, `Init` function or some time before the editor scene starts.

To register a button simply call
```csharp
ExtensionButton button = ExtensionButtons.AddButton(iconSprite, "This is the tooltip", OnButtonClick);
```
AddButton returns an `ExtensionButton` which you can use if you wish to change the buttons properties after you've registered it.

## Requirement Checks
As of ChroMapper `0.7.386`, a new modular system for map requirements and suggestions was added. With this new system, any external plugin can now register their own checks for having maps suggest or require a certain mod or feature.

ChroMapper currently comes with 6 built-in requirement checks for the following mods:
- Chroma 2.0
- Chroma 1.0
- Noodle Extensions
- Mapping Extensions
- Cinema
- Sound Extensions

### Custom Requirement Checks
To write your own requirement check, create a new C# class file that inherits ChroMapper's abstract `RequirementCheck` class, and implement what's required.

```csharp
public class MyCustomModCheck : RequirementCheck
{
    public override string Name => "Mod or Feature Name";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        return RequirementType.None;
    }
}
```

To register the requirement check into ChroMapper, call the static `RequirementCheck.RegisterRequirement` method, and pass an instance to your custom requirement check.

```csharp
[Plugin("My Plugin")]
public class MyPlugin
{
    [Init]
    private void Init()
    {
        RequirementCheck.RegisterRequirement(new MyCustomModCheck());
    }
}
```

#### `Name`
This `Name` property is what will go inside the `_requirements` or `_suggestions` array. Make sure it matches what's expected of the mod you want ChroMapper to detect.

#### `IsRequiredOrSuggested`
This method processes the map and difficulty info, and determines whether or not a requirement or suggestion should be added. It returns a `RequirementType`, which is an enum with 3 possible values:
- `RequirementType.Requirement`: When returned, ChroMapper will add the `Name` property as a requirement for the difficulty.
- `RequirementType.Suggestion`: When returned, ChroMapper will add the `Name` property as a suggestion for the difficulty.
- `RequirementType.None`: When returned, ChroMapper will not do anything. This should almost always be the default value.

## Legal

Some users might be thinking: What about GPLv2? Wouldn't these plugins be forced to be distributed and released under GPLv2?

After carefully reviewing the GPLv2 license, I have decided that these plugins would fall under this clause:

```
If identifiable sections of that work are not derived from the Program, and can be reasonably considered independent and separate works in themselves, then this License, and its terms, do not apply to those sections when you distribute them as separate works.
```

Essentially: No, GPLv2's infamous "viral" effect will not apply to plugins you create for ChroMapper.

In a little more detail: I will not consider plugins for ChroMapper to be derived from it, and will instead consider them as independent and separate works, thereby making GPLv2 not apply Sections 2 and 3, which talks about making source code and modifications available under the same license.
