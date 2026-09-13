# Build guide

ChroMapper is developed with Unity **6000.3.13f1** in C#.

This GitHub repository comes with the assets and scripts you need to easily open it up in Unity, no dependency bullshit required.

## Development Environment Setup
* Clone the project from GitHub to your local work folder.
* Download and install [Unity Hub](https://unity3d.com/get-unity/download).
* Activate your license within Unity Hub. Most people should be eligible for a free Personal license.
* Use Unity's [build archive](https://unity.com/releases/editor/archive) to locate and install ChroMapper's version of Unity (see above).
* Add the project in the "Projects" section. Select your main folder you cloned from GitHub.
* Open the project. Project dependencies should download automatically.

## Running the project
* Open scene `00 Bootup` from the Project window before running or building.
  * Hitting the **Play button** in Unity on this scene will launch ChroMapper directly in the editor — much faster to iterate than a full build.
* Select "File" -> "Build and Run" within Unity for a standalone build.
  * It is recommended to always build with Mono; building with IL2CPP will cause issues in areas that utilize [Harmony](https://github.com/pardeike/Harmony) patches, including post processing and input.
* Most errors, including "Missing Project ID" and "Discord RPC error", can be ignored.

## Localizing UI text and tooltips

All user-facing ChroMapper text should use Unity Localization rather than a literal string.

### Add a string-table entry

1. In the Unity Project window, open the **base table asset** in `Assets/Locales` for the relevant area—for example, open `Mapper.asset`, not a locale-specific file such as `Mapper_en.asset` or `Mapper_de.asset`.
2. Use the Unity Localization table editor to add a descriptive, dot-separated key and its English text.
3. Provide that English text in every locale column as a fallback. Missing entries produce localization errors for users of that language; translators can replace the fallback later.

Do not hand-edit the generated locale-specific `.asset` files. Manage the table through the base asset's editor UI so Unity keeps the collection in sync.

### Wire the text into the UI

For a `TextMeshProUGUI` object in a scene or prefab, add a `LocalizeStringEvent` component and select the table and key you added. The component refreshes the text when the active locale changes.

For an existing `Tooltip` component, set its `LocalizedTooltip` field to the table and key instead of using `TooltipOverride`. The tooltip component resolves that `LocalizedString` when it is shown, and then appends any configured hotkey hint.

For UI created in code, fetch the string with `LocalizationSettings.StringDatabase.GetLocalizedString(table, key)` or use an existing localized helper such as `ButtonComponent.WithLabel(table, key)`. Keep the table/key in the same collection that owns the surrounding UI strings.

## Environment Branch Setup

> Make sure scene `00 Bootup` is open in the Unity editor before running the steps below.

1. Extract the environment assets ZIP (usually from Discord) to `_Scenes/Environments/Data`
2. In Unity, run `Environment/Populate Build Data`
3. In Unity, run `Environment/Create All from Data` (this may take a while and may show some errors, which is fine)
4. In Unity, run `Environment/Update Environment List` (registers scenes and color schemes in `EnvironmentListSO`)
5. Never commit scenes and materials until final commit

## Contributing
Please follow the [Contributing guidelines](CONTRIBUTING.md) as you are making contributions to ChroMapper.
