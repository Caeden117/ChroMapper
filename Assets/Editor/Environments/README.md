# ChroMapper environment generation

The JSON files under `Assets/__Scenes/Environments/Data/` are UGEcko exports of Beat Saber's environment data. Keep them faithful to the game dump; ChroMapper-specific corrections do not belong in the source exports.

## Generation workflow

The editor commands have separate responsibilities:

- `Environment/Populate Build Data` rebuilds the shared mesh, material, sprite, shader, and layer libraries used by generated environments.
- `Environment/Update Environment List` refreshes the environment list and generated `TracksDefinitionSO` assets from the export metadata.
- `Environment/Create All from Data` rebuilds the environment scenes from the exports and populated libraries.

`EnvironmentBuildPopulate.cs` owns the shared build libraries. `EnvironmentListUpdate.cs` owns environment-list and track-definition refreshes. `EnvironmentSceneCreator` and the scripts under `Create/` own scene construction.

## Import responsibilities

`Structures/EnvDataInfo.cs` converts exported metadata into ChroMapper track definitions. Environment-specific corrective mappings for inaccurate or incomplete game-export metadata belong there so every regeneration reproduces them without modifying the UGEcko source files.

Generated scenes and track-definition assets should reflect these import and construction paths. Avoid hand-editing generated output without making the equivalent generator change, because the next refresh will overwrite it.
