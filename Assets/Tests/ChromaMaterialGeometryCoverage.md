# Chroma material and primitive request coverage

The request inventory uses published Chroma 2.9.22, commit `b38d924c0022d988e68cb28f1b0646856b546bc7` (release `v2026-02-23`), compared with selected Beat Saber 1.44.1 serialized materials and active D3D11 shader programs exported by AssetRipper 1.3.14. The initially inspected `06ac146` contents and current local contents are identical to that published pin for `MaterialsManager`, `EnvironmentMaterialsManager`, `GeometryFactory` and their environment enums, verified by scoped Git diffs. Tests exercise loaded objects, production geometry/material APIs, controller output and rendered pixels. Temporary native extraction files are independent evidence, not test dependencies.

## Presets and primitive types

| Authored shader preset | Numeric string | Runtime material family | Additional native behavior covered |
| --- | ---: | --- | --- |
| Standard | 0 | SimpleLit | Fresh generated defaults, omitted color, no generated dithering, empty-keywords Glowing upgrade |
| OpaqueLight | 1 | OpaqueNeonLight | Automatic physical light and bloom fog |
| TransparentLight | 2 | TransparentNeonLight | Automatic physical light and bloom fog; planar Quad/Triangle culling |
| BaseWater | 3 | Fresh WaterLit | Shader defaults and independent packed-probe numeric response |
| BillieWater | 4 | Billie WaterfallFalling / WaterLit | Automatic physical light and bloom fog; Gaga packed-probe tunnel contribution |
| BTSPillar | 5 | BTS SimpleLit | Serialized environment preset; empty-keywords Glowing upgrade |
| InterscopeConcrete | 6 | Interscope Concrete2 / SimpleLit | Serialized environment preset |
| InterscopeCar | 7 | Interscope Car / SimpleLit | Full-map and reduced rocket dark/on-phase reflections |
| WaterfallMirror | 9 | Billie WaterfallMirror / Mirror | Serialized planar-reflection preset |
| Glowing | 10 | Fresh Glowing | Omitted color and infinite fog offset; authored main-effect gate over a disabled global gate |

`Obstacle` (8) appears in Chroma's enum but has no working material branch or environment binding in the selected native implementation. CM retains its existing Obstacle extension; it is not counted as a working native preset. Undefined shader/type values do not gain another native route.

Native geometry types are `Sphere` (0), `Capsule` (1), `Cylinder` (2), `Cube` (3), `Plane` (4), `Quad` (5), and `Triangle` (6). Triangle uses the native three-vertex mesh and a matching physical mesh collider. The numeric Triangle path runs synchronously through `SpawnGeometry` as well as named requests during map loading.

## Request modes

| Request | Runtime coverage |
| --- | --- |
| V3 `materials` + named material and inline geometry material | All ten working presets |
| V2 `_materials`, `_shader`, `_shaderKeywords`, `_geometry`, `_material` | Equivalent named and inline coverage |
| Authored material named `standard` | Named dictionary definition takes precedence over CM's implicit standard extension |
| Omitted `shaderKeywords` | Native preset defaults retained |
| Explicit nonempty `shaderKeywords` | Authored set replaces defaults; known native aliases select their corresponding compiled branch |
| Explicit empty `shaderKeywords` | Clears defaults; Standard/BTSPillar alone use native Glowing upgrade and authored alpha 0 |
| Omitted color, authored HDR color and alpha | Generated clear defaults and effective MPB/controller color checked without assuming OpaqueLight has a material `_Color` property |
| `collision` omitted/false/true and V2 equivalent | Seven primitive colliders; real Playing light collision shortens only at the collidable shape |
| Light-preset geometry without a `components` object | Automatic ParametricBoxLight, bloom controller and bloom fog are wired before activation |
| Material track string/array | Companion enhancement/object-data tests cover binding and multiplicative animated color |

`ChromaMaterialRoutingParityTest` contains the route/default/type/component assertions. `ChromaGeometryPreviewParityTest` covers real Playing collision and generated Standard dither output. `ChromaBaseWaterRenderingParityTest`, `ChromaGlowingKeywordRenderingParityTest`, `ChromaOverdrawRenderingParityTest` and the Superluminal fixture classes cover distinct shader and map contributions.

These route and shader cases passed together in validation gate `213701`, after their expected unfixed failures. The complete and reduced Rocket/Surface cases also passed with exact source requirements in gate `204539`. Transparent overdraw uses explicit unit box widths and one Quad face to isolate the native per-fragment result; its normal visible-surface control passed before the corrected unfixed failure was counted.

## Active keyword mappings and rendering adapters

| Native authored keyword | Shader family | CM compiled keyword or behavior |
| --- | --- | --- |
| ENABLE_BLOOM_FOG | All preset shader families | BLOOM_FOG |
| MAIN_EFFECT_ENABLED | SimpleLit, opaque/transparent lights, Glowing | POST_BLOOM; authored material enable remains effective when the camera disables the global gate |
| ENABLE_HEIGHT_FOG | Opaque/transparent lights | HEIGHT_FOG; the old SimpleLit token is not remapped |
| ENABLE_WORLD_NOISE / ENABLE_WORLD_SPACE_FADE | Transparent light | WORLD_NOISE / WORLD_SPACE_FADE |
| ENABLE_DIRT | Mirror | DIRT |
| ENABLE_RIM_DIM | SimpleLit presets | RIM_DIM |
| ENABLE_EMISSION_ANGLE_DISAPPEAR | SimpleLit presets | EMISSION_ANGLE_DISAPPEAR |
| OVERDRAW_VIEW | WaterLit, Mirror, opaque/transparent lights | Dedicated native per-family debug output |

Keyword names are interpreted within their family, not by stripping a universal prefix. Other authored strings remain intact where the port accepts them. Compiled route coverage establishes the explicit mappings above; it does not prove pixels for every combination of arbitrary keywords.

The active native PC inventory was checked separately from the larger declared keyword list. WaterLit has no shipped active diffuse/specular/whiteboost permutations in the selected player shader. Its existing detail-normal and Z-fade branches are retained. Native OpaqueNeonLight's ACES gate produces identical program bytes across all eight fog/main-effect combinations. WaterLit's default main-effect gate likewise selects identical output. SimpleLit tags `_DISSOLVEALPHA_FADE` on 2048 selected programs, but none also has DISSOLVE and the inspected fragment has no alpha-fade controls or operation. These cases do not establish missing functional branches.

Unity instancing/stereo flags and engine lightmap state are renderer-owned inputs. The Glowing adapter omits legacy engine instancing/stereo strings, `_ENABLE_COLOR_INSTANCING` and the inert `_CUTOUT_NONE` token. Its older cutout/noise/Always whiteboost controls have no active selected native PC variants; only MainEffect whiteboost selects a native branch. CM's mirror uses its own per-camera reflection texture instead of the game's stereo reflection atlas. CM note deformation has its own note renderer streams. These adapters are not advertised as accepting native engine streams on arbitrary primitives. The tested presets retain their native material-facing light, normal, packed-probe and reflection inputs.

The overdraw oracle is independent native DXBC: WaterLit uses `.1 * TrueOverdrawOn * OpaqueOverdrawOn * OverdrawColor`; both parametric lights use `.1 * TransparentOverdrawOn * OverdrawColor`; Mirror adds the opaque term to its squared-intensity reflection texture. Glowing has no active native overdraw variant. No direction inversion or artistic intensity tuning is used.

## Superluminal source provenance

Superluminal's reduced Rocket and Surface fixtures preserve the source `Chroma` and `Noodle Extensions` requirements, source order and all referenced custom data. The complete 72,219,420-byte difficulty remains ignored and full tests skip only when that exact file is missing. Reduced fixtures always run.

The rocket fixture retains one Cube fin and one Cylinder from the complete 114-renderer group. It validates the dark phase at 184.062 and textured on phases at 188.0/188.5, including forward, reverse, boundary and deterministic playback samples. Exact 188.484 is an authored off phase 6.4 ms before the next on event; the supplied images have different timestamps. The tests retain that boundary without shifting the clock.

Final validation: the complete run `20261011-224129` passed 1,629 cases and exposed three obsolete test observers. Their test-only repairs preserve the behavior assertions and pass alongside neighbors in `20261011-230640` (43/43). All 1,632 regular cases are verified across those two runs; production code is unchanged after the full run. No build or deployment was performed.

