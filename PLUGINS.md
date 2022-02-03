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

## CMUI
CMUI is a plugin-friendly interface for creating UI, introduced in ChroMapper `0.8.459`. It has been retrofitted into the older Dialog Box APIs, and will see expanded rollout in the near future.

### Creating a Custom Dialog Box
For most developers, CMUI begins with the Dialog Box. The Dialog Box system has been completely revamped to use CMUI under the hood. While existing Dialog Box APIs still work, if you are using anything advanced, we recommend using the new Dialog Box API to squeeze everything into one box.

To create a new Dialog Box powered by CMUI, call `PersistentUI.Instance.CreateDialogBox()`. You are then given back the newly created `DialogBox` object, which will be used to manage the Dialog Box itself.

### Adding Components
CMUI works with *components*. In the context of CMUI, components wrap pre-created UI and expose easy-to-use APIs for that UI. Each CMUI component is either designed to handle a certain type (From primitive types like `int` and `strings` to larger structs and classes like `Color`s and `Progress<float>`s), or can exist without handling any type at all. Most components also include labels, which can be toggled on and off.

To add a component, call `dialogBox.AddComponent`. There are two overloads for this method: One with a generic type, and one without. In most cases, we recommend using the generic type version. Either way, you will need to define the CMUI component type you want to add.

CMUI components include (but are not limited to):
- `TextComponent` for a simple static label
- `TextBoxComponent` for exposing a text box
- `ToggleComponent` for a simple boolean toggle
- `SliderComponent` for exposing a slider with configurable min, max, and precision values
- `ButtonComponent` for executing actions on click
- `DropdownComponent` for selecting a list of items

Each CMUI component will have a few extension methods available for you to use. They will also have a individual set of methods that can chain together, so make sure to look for available methods.

For most cases, you will be using `component.WithInitialValue()` and `component.OnChanged` to define the starting value, and what will happen when that value is changed.

### Footer Buttons
When working with Dialog Boxes, you may also need to define some buttons on the footer of the box, either to submit user selections, or to simply close the dialog box entirely.

To add a footer button, simply use `dialogBox.AddFooterButton`. These will always close the dialog box when clicked. The provided `onClick` parameter will also be executed before the dialog box closes.

### Showing your Dialog Box
Unlike the previous Dialog Box APIs, when dealing with custom Dialog Boxes you are expected to explicitly open the box for the user. This is mostly so you can create the Dialog Box first, then show it to the user later.

To open the dialog box, simply call `dialogBox.Open()`. To close the dialog box at any time, call `dialogBox.Close()`.

**IMPORTANT!** By default, custom Dialog Boxes will *always* self-destruct when they are closed. To prevent this behavior, call `dialogBox.DontDestroyOnClose()`. If you do this however, you will be responsible for managing the lifetime of the Dialog Box.

### CMUI Outside of Dialog Boxes
While CMUI's primary use case is for creating advanced Dialog Boxes, CMUI itself is simple enough to use anywhere.

If you have an external piece of UI that you want to attach CMUI components to, you can still add components with `ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType`. You will be responsible for the lifetime of these components, so be sure to cache and/or destroy them appropriately.

If you don't particularly care which CMUI component you want, and only care about having a CMUI component that can handle a type, you can also use `ComponentStoreSO.Instance.InstantiateCMUIComponentForHandledType`.

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
