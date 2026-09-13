# Shader Work

This file records the shader contracts used by this project. The shader files,
include files, runtime code, and material assets are the project authority.
external evidence and external source names are descriptive only.

## Scope

The work covers environment, object, particle, post-process, and helper shaders.
The target uses the built-in render pipeline and Unity editor adapters.

## Shader mapping

The project environment library stores the external source shader name and the
project shader reference.

| External source shader name | Project shader |
| --- | --- |
| `Custom/CloudsLitTransparent` | `ChroMapper/Clouds Lit Transparent` |
| `Custom/CloudsOpaque` | `ChroMapper/Clouds Opaque` |
| `Custom/CustomParticles` | `ChroMapper/Particles` |
| `Custom/Mirror` | `ChroMapper/Mirror` |
| `Custom/OpaqueNeonLight` | `ChroMapper/Parametric Box Opaque` |
| `Custom/Parametric3SliceSprite` | `ChroMapper/Parametric Slice Billboard` |
| `Custom/ParametricBoxFakeGlow` | `ChroMapper/Parametric Box Fake Glow` |
| `Custom/Rain` | `ChroMapper/Rain` |
| `Custom/SetDepthOnly` | `ChroMapper/Set Depth Only` |
| `Custom/SimpleLightning` | `ChroMapper/Lightning` |
| `Custom/SimpleLit` | `ChroMapper/Lit` |
| `Custom/SimpleStencil` | `Custom/Stencil` |
| `Custom/Spectrogram` | `ChroMapper/Spectrogram` |
| `Custom/TransparentNeonLight` | `ChroMapper/Parametric Box Transparent` |
| `Custom/UnlitSpectrogram` | `ChroMapper/Spectrogram Unlit` |
| `Custom/WaterLit` | `ChroMapper/Water Lit` |

Object and fallback mappings are:

| External source role | Project shader |
| --- | --- |
| `Custom/Note` | `ChroMapper/Object/Note` |
| `Custom/SliderNoteCrossedStrips` | `ChroMapper/Object/Arc` |
| `Custom/ScreenDisplacementHD` | `ChroMapper/Object/Obstacle Distortion` |
| `Custom/ParametricBoxFrameHD` | `ChroMapper/Object/Obstacle Outline` |
| Geometry fallback | `ChroMapper/Glowing` |

The `name` field preserves the external source lookup name. It can differ from
the `Shader` declaration in the project shader file.

## Include ownership

Keep feature order in the consuming shader when order affects the result.
Single-shader helpers stay inline unless they form an intentionally retained
domain library, such as easing or bloom filtering.

The current layout separates Core, Common, Families, shader-local implementations,
and root hold. The ownership records below describe the historical recovered
layout, not the current disk state:

- [`ShaderLibrary/README.md`](ShaderLibrary/README.md)
- [`Audit/Recovered/RecoveredShaderModuleOwnership.md`](Audit/Recovered/RecoveredShaderModuleOwnership.md)
- [`Audit/Recovered/RecoveredShaderModuleOwnership.json`](Audit/Recovered/RecoveredShaderModuleOwnership.json)

| Project include | Owner | Responsibility |
| --- | --- | --- |
| `ShaderLibrary/Core/Camera.hlsl` | Core | Stereo-aware camera position and screen coordinates |
| `ShaderLibrary/Core/Data.hlsl` | Core | Pipeline data types and surface defaults |
| `ShaderLibrary/Core/Tonemapping.hlsl` | Core | ACES tone mapping |
| `ShaderLibrary/Core/Easings.hlsl` | Core | Gradient easing curves selected by shader easing ID |
| `ShaderLibrary/Common/Bloom.hlsl` | Common | White boost and bloom composition |
| `ShaderLibrary/Common/Fog.hlsl` | Common | Distance fog, height fog, and color fog |
| `ShaderLibrary/Common/Lighting.hlsl` | Common | Shared diffuse, specular, and falloff lighting |
| `ShaderLibrary/Common/ObjectShared.hlsl` | Common | Shared object-rotation transforms |
| `ShaderLibrary/Common/PostProcess.hlsl` | Common | Blue-noise dither and screen-position helpers |
| `ShaderLibrary/Common/Reflection.hlsl` | Common | Reflection-probe decoding, sampling, and projection |
| `ShaderLibrary/Common/Time.hlsl` | Common | Standard, song, and frozen time vectors |
| `ShaderLibrary/Families/BloomFogComposition.hlsl` | BloomFog family | Prepass sampling and bloom-fog composition |
| `ShaderLibrary/Families/ParametricShared.hlsl` | Parametric family | Height, distance, noise, and fade calculations |
| `ShaderLibrary/Families/BloomShared.hlsl` | PostProcess family | Bloom pyramid filters, merge, exposure, and tone mapping |
| `ShaderLibrary/Families/SpectrogramShared.hlsl` | Spectrogram family | Spectrogram-data index helpers |
| `ShaderLibrary/Cutout.hlsl` | Root hold | Cutout helpers with UNVERIFIED provenance |
| `ShaderLibrary/Blurs.hlsl` | Root hold | Legacy sampler2D blur variants; no static project consumers are known |

The Fog split keeps `ApplyColorFog` in `ShaderLibrary/Common/Fog.hlsl`. It
places prepass globals and six composition functions in
`ShaderLibrary/Families/BloomFogComposition.hlsl`. The family file includes the
common Fog file.

The Fog include contract covers 18 consumers, all of which include the family
file directly. Four adapters do not call composition functions: ObjectArc,
ParametricBoxFakeGlow, ParametricBoxTransparent, and ParametricSliceBillboard.
They retain the family include because a
historical comparison found that a generic-only include changed 152 successful
D3D11 byte arrays. That result is historical audit evidence, not current
compiler-fidelity validation.

The particle implementation is inline in `Assets/_Graphics/Shaders/Particles.shader`.
Preserve its preprocessor section order because regions cross section boundaries.

The historical ownership records retain the former module layout for audit
purposes only.

## Keyword and property rules

- Use project names for project shader features.
- Keep Unity-owned names such as `INSTANCING_ON` and stereo keywords.
- Keep global `POST_BLOOM` and `BLOOM_FOG` routes under runtime ownership.
- Use independent keyword declarations for independent feature families.
- A parameterless `[Toggle]` controls an inspector property without declaring a
  keyword.
- Keep per-frame values in shader code or global state when runtime code owns
  those values.
- Keep property names, types, attributes, defaults, and runtime meaning stable.

The current external source aliases include:

| External source alias | Project route |
| --- | --- |
| `ENABLE_BLOOM_FOG` | `BLOOM_FOG` |
| `MAIN_EFFECT_ENABLED` | `POST_BLOOM` |
| `_WHITEBOOSTTYPE_MAINEFFECT` | `_BLOOMTYPE_DEFERRED` |
| `_WHITEBOOSTTYPE_ALWAYS` | `_BLOOMTYPE_MIXED` |
| `ENABLE_HEIGHT_FOG` | `HEIGHT_FOG` |
| `_FOGTYPE_ALPHA` | `_FOGTYPE_ALPHA` |
| `_FOGTYPE_COLOR` | `_FOGTYPE_COLOR` |
| `_FOGTYPE_LERP` | `_FOGTYPE_LERP` |
| `ENABLE_WORLD_NOISE` | `WORLD_NOISE` |
| `ENABLE_WORLD_SPACE_FADE` | `WORLD_SPACE_FADE` |
| `ENABLE_NOISE_DITHERING` | `NOISE_DITHERING` |
| `ENABLE_ANGLE_DISAPPEAR` | `ANGLE_DISAPPEAR` |
| `ENABLE_Y_AXIS_BILLBOARD` | `Y_AXIS_BILLBOARD` |
| `ENABLE_CUTOUT` | `CUTOUT` |
| `ENABLE_CLIPPING` | `CLIPPING` |
| `ENABLE_TARGET_POINT` | `TARGET_POINT` |
| `ENABLE_TIME_OFFSET` | `TIME_OFFSET` |
| `ENABLE_DIRT` | `DIRT` |
| `LIGHTMAP_ON` | `LIGHTMAP` |

Unsupported routes remain excluded. Do not add a route without project shader
support and a project runtime owner.

## Note and custom-note contract

`Assets/_Graphics/Shaders/Object/Note.shader` is the canonical note target.
It owns reflection, cutout, plane cut, rim dim, bloom type, fog, editor color,
timeline, translucency, and ordered dither behavior.

The editor selects the face color first. It selects strobe RGB for the diagonal
face when strobe mode is active. It then multiplies the selected RGB by the
instanced `_ColorMultiplier` MPB contract. Strobe alpha is not selected or
emitted.
`CM_PREVIEW_MODE` samples `_MainTex` after this color operation. Outside preview
mode, timeline whitening can replace RGB after the multiplier. Alpha remains
available to the recovered reflection and bloom equations.

Custom-note loading uses the canonical target when shader compatibility is on.
The adapter keeps supported source routes and drops unsupported routes. Its
supported aliases include `_WHITEBOOSTTYPE_ALWAYS` to `_BLOOMTYPE_MIXED`,
`ENABLE_HEIGHT_FOG` to `HEIGHT_FOG`, and the three fog aliases:
`_FOGTYPE_ALPHA`, `_FOGTYPE_COLOR`, and `_FOGTYPE_LERP`.

`_EnableFog` is the canonical built-in inspector control. It selects
`_FOGTYPE_LERP` for editor parity. The fragment shader declares all three fog
types as mutually exclusive variants. `BlendFogColor` keeps their output
channel behavior. The full bloom-fog route accepts all three types. The
height-only route keeps its no-bloom behavior.

`Note.mat` and `Bomb.mat` use the canonical note shader and keep render queues
`2004` and `2005`. Review keyword and property values only when a note contract
changes. Do not change prefab renderer overrides in this shader work.

## MIPMAP_BIAS and view-angle fade

`MIPMAP_BIAS` controls only biased texture sampling. It does not add view-angle fade or a normal payload. `VIEW_ALIGN_DISAPPEAR` owns angle fade.

## Bloom and post process

Main bloom uses the runtime-observed high-quality route with a 13-tap filter
and 928-pixel base width. `Bloom.shader` and `BloomShared.hlsl` serve
camera bloom and bloom fog. Runtime controllers own global values, temporary
targets, camera state, and keyword lifetime.

Settings expose independent Bloom, BloomFog, ChromaticAberration, and
ScreenDisplacement controls. Each controller restores its camera state, globals,
layers, and keywords when disabled.

## Environment shader audits

The consolidated audits cover 44 environments. They contain the durable route,
material, selector, stage, runtime, and regeneration contracts.

- [`LIT_REAUDIT.md`](LIT_REAUDIT.md): 290 Lit routes, 169 documented canonical states, 1,160 stage records, and 4,604 pass-0 DXBC blobs.
- [`PARTICLE_REAUDIT.md`](PARTICLE_REAUDIT.md): 345 particle routes, 103 canonical states, 1,380 stage records, 130 vertex binaries, and 204 fragment binaries.

## Maintenance checklist

1. Import changed project shader assets in Unity.
2. Read shader import and compile messages.
3. Inspect representative project materials and keyword values.
4. Make sure that source aliases use the project material projection path.
5. Render the affected family with instancing off and on.
6. Render available mono and stereo camera paths.
7. Render with `POST_BLOOM` and `BLOOM_FOG` off and on.
8. Render the relevant height, color, and alpha fog routes.
9. Test camera depth and screen displacement with MSAA off and on.
10. Toggle each independent post-process setting.
11. Inspect changed project textures and material property values.
12. Record project version, render API, color space, and fixture limits.
