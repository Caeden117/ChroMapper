# Recovered Shader Evidence Gaps

## Interpretation

A gap is an unverified contract, not a defect. A defect candidate records evidence but does not establish a fix.

No artifact proves complete visual or runtime parity. Static source, DXBC, compilation, material, and mesh evidence have different scopes.

## Cross-phase blockers

1. No RenderDoc, PIX, screenshot pair, or named visual comparison provides frame authority.
2. The exported historical shader source contains a `DummyShaderTextExporter` stub.
3. Reconstructed HLSL is derived evidence. Exact DXBC ASM has higher authority.
4. Sixteen current replacement routes lack complete recovered fragment-binary equivalence evidence.
5. No recovered full-spectrogram route exists.
6. The route-to-hash audit output is absent. Only prior report counts remain.
7. Runtime MPB values, global values, selected variants, and material reachability remain unverified.
8. External user bundles remain outside the repository evidence.
9. Beat Saber mesh channels and runtime packing values remain unavailable.
10. Stereo paths, mesh stripping, compression masks, Mirror batching, and Rain or Unlit stereo behavior need runtime evidence.
11. No shader variant collection or shader-stripper hook exists.
12. Five Lit audit tools remain only as CPython 3.14 bytecode.
13. Dynamic property names can fail after a rename without an error.
14. Dead setters and bake-only properties need a runtime verdict.

## Cross-snapshot source hashes

The catalog ABI and behavior matrix have different source hashes for 12 catalog shaders. The ordered behavior records can describe a different audit snapshot.

A hash difference does not prove a semantic difference. The semantic delta remains unverified for these joins:

- `ChroMapper/Glowing`: catalog `474c58f22ab992d71c614ca2c4e890cccc148018fce95b1c253b357ba163f74d`; behavior matrix `f4557f5acf5ca0c4db452786c5b372c6ad7b150cdd59b95550f9c762637bbdb3`.
- `ChroMapper/Lightning`: catalog `71fc66b5dbbf52e9dbaafe719024294e35e552299a48aa30e00fd5b024ed61a9`; behavior matrix `b5fa1d06f28807918e6c10af5c41115707ca1ee4f1c190fe0780eb904c2d58a1`.
- `ChroMapper/Lit`: catalog `a6867bcb0e3cac93e336d453312ede5bf988c0544aff1c0051e02c9bfe7a1152`; behavior matrix `a8afd7b2974244691b41d93e2a151734487d55a7ac0fc0e5fd3fca4895fac1f6`.
- `ChroMapper/Mirror`: catalog `4fd36c4518a0a74b6520325b9a71a2a51e6967c5aeb18a549631c35510242f3f`; behavior matrix `7e8a97d5a039abfd28037f21c96a849be286cf836b53ccd83bbef4a16e57f3ab`.
- `ChroMapper/Object/Arc`: catalog `cc5aaa365ab6dc778b11981fbe91f3e6f3d7f80dde0ea5d9c25abf1814927153`; behavior matrix `737aed8b0feb8dd270c547238f874d33b3c62fbc0b73cec5ddb878df6808b09b`.
- `ChroMapper/Object/Note`: catalog `14f58d505830cedbd830b4ba62837516c046d35b70e6dfcfd0327dd45697dcfe`; behavior matrix `2352f8cc9028ace1a6ee45524b5f206ce6b5a6b040dffbcf5aad18f87955b17d`.
- `ChroMapper/Parametric Box Fake Glow`: catalog `e1e4d80c037f9ec1cfa7a5b1a8f97b428601beedffc6de19f15b5cb171b6fb0e`; behavior matrix `03aa4522c7601f047b9b6d8d8738fe977af8f0b242958f34fe65cc743bc9e46c`.
- `ChroMapper/Parametric Slice Billboard`: catalog `f6eb849feff3fe7e0493d282db11647411da225390cccb63f9881bbda5ad7c24`; behavior matrix `307621ff1a7e89d8236830e61a1a3bd0fb04bc5d3920f12a865a7d4368b30e4b`.
- `ChroMapper/Particles`: catalog `e9d1ba767ac448cf9f400bbb72c86c3caffc385e57e2d2f34b5475250b2c3f96`; behavior matrix `30967d086b93f2ce0f71d5f952b0591c5ef69c7725137b420faf289e5426b219`.
- `ChroMapper/Spectrogram`: catalog `8218a94fa5266cf3b2d38ef1f4cda990588dc46d88ee2b73a87562459ebb4436`; behavior matrix `213deadf948fda29c2fb0b8f6d94884eccb108a529621908e3338ed536db0d51`.
- `ChroMapper/Spectrogram Unlit`: catalog `87df7741ec2e3c9cbffe6a28770cd2bc4b002506b0f501928a7bbe5764deb497`; behavior matrix `2630113e6763c5f09ca34745fae5e0a30a957cb7f6e87a65bc7eb4f05ae46b56`.
- `ChroMapper/Water Lit`: catalog `6ca64b42647c56a9fa89d3d013b69bc7ea3cc381303948368d96e95f4eb3e370`; behavior matrix `d1c617d56d680d6b7ff2a355590ee8812ffc6277e462dc4f9dc6a8a3b65bf44a`.

## Catalog gap register

### `ChroMapper/BloomfogMesh`

- Exact original ShaderLab identity is unrecorded; no standalone audit/capture.

### `ChroMapper/BloomfogSkybox`

- Original identity is only suspected from inline comparison; no standalone audit/capture.

### `ChroMapper/Sky Gradient`

- Exact original ShaderLab identity is unrecorded; no standalone audit/capture.

### `ChroMapper/Clouds Lit Transparent`

- No committed visual parity capture was found.

### `ChroMapper/Clouds Opaque`

- No committed visual parity capture was found.

### `ChroMapper/Glowing`

- No committed visual parity capture was found.

### `ChroMapper/Lightning`

- Frame capture required; eight Metallica origin/target transforms are missing in current data.

### `ChroMapper/Lit`

- Sixteen executable routes lack recovered fragment binaries; runtime globals/MPBs and visual parity are not proven; pass 1 is unproven.

### `ChroMapper/Mirror`

- No standalone audit; stereo reflection-atlas behavior and omitted routes need runtime/visual validation.

### `ChroMapper/Object/Arc`

- Mapping/formula comments only; no dedicated versioned binary audit or visual capture.

### `ChroMapper/Object/Note`

- No dedicated Note audit or committed visual parity fixture.

### `ChroMapper/Object/Obstacle Distortion`

- No standalone audit; runtime grab/depth texture and renderer-property ownership need capture.

### `ChroMapper/Object/Obstacle Outline`

- Not a binary-proven complete FrameHD replacement; source bindings and layout remain unproven.

### `ChroMapper/Parametric Box Fake Glow`

- No committed visual parity capture was found.

### `ChroMapper/Parametric Box Opaque`

- No committed visual parity capture was found.

### `ChroMapper/Parametric Box Transparent`

- No committed visual parity capture was found.

### `ChroMapper/Parametric Slice Billboard`

- No committed visual parity capture was found.

### `ChroMapper/Particles`

- Runtime/mesh parity and visual comparison remain unproven; unsupported zero-row selectors have no invented formula.

### `ChroMapper/Post Process/Bloom`

- Exact original ShaderLab identity is unrecorded; no committed visual capture.

### `ChroMapper/Post Process/Post Bloom`

- No standalone audit or committed frame/reference capture.

### `ChroMapper/Rain`

- No dedicated audit, corpus counts, version, binary hashes, capture, or screenshot.

### `ChroMapper/Set Depth Only`

- No standalone audit or visual capture; ShaderLab state cannot be proven from stage ASM alone.

### `ChroMapper/Spectrogram`

- No committed visual parity capture was found.

### `ChroMapper/Spectrogram Unlit`

- No committed visual parity capture was found.

### `ChroMapper/Stencil`

- No standalone audit; render-state parity relies on source shell/established state, not stage ASM.

### `ChroMapper/Water Lit`

- Packed dual-cubemap reflection route is not implemented because required probe assets/data are null.

## Behavior scan gap register

## 1. Record 1

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not unify ordinary (non-bloom) fog formulas: CloudsOpaque lerps toward 0.1 with (1-hFade) while Spectrogram lerps (0.1 -> rgb, heightRetained); WaterLit/Glowing/Mirror have no ordinary-fog route (bloom-only or unconditional dither). Unification would change output.

## 2. Record 2

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not unify alpha contracts: CloudsOpaque/Spectrogram force alpha=0; CloudsLitTransparent computes base.a*runway*bottom*tint.a; SpectrogramUnlit preserves visibility*_Color.a; Rain outputs premultiplied (color*alpha, alpha); Mirror retains reflection alpha; SetDepthOnly returns saturated vertex COLOR; Stencil returns float4(0,0,0,0). Blend state depends on this.

## 3. Record 3

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not unify normal spaces: WaterLit builds TBN mapped world normal with _NormalScale + _NormalScaleVertical falloff; Mirror does screen-space bump distortion (red*=alpha, XY unpack, z=sqrt, reflectionUV offset by viewY); CloudsOpaque uses -normalize(world) with optional INVERT; CloudsLitTransparent uses world-origin XZ normal with ALIGN_NORMALS_TO_WORLD_ORIGIN variant. Same helper name would be unsafe.

## 4. Record 4

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not infer ShaderLab render state (queue/cull/depth/blend/stencil) from DXBC arithmetic per WATER_SPECTROGRAM_REAUDIT Limits and per-shader D-findings. Established ChroMapper pass state is parity state, not binary-proven.

## 5. Record 5

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not add OVERDRAW_VIEW, Meta pass, debug/white-boost, or speculative keyword routes. Omitted intentionally in every audit.

## 6. Record 6

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not compensate in-shader for data defects: Metallica 4+4 lightning origins/targets are (0,0,0) (LIGHTNING_REAUDIT); Billie/Gaga ReflectionProbeData refs are null so packed-cube reflection falls back to Unity spec-cube (WATER_SPECTROGRAM_REAUDIT W5); opaque fog needs valid custom height globals cleared when bloom fog disabled (CLOUD_REAUDIT); Billie material has stale FOG keyword - fix via Populate Build Data, not shader edit.

## 7. Record 7

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not unify _TimeHelperOffset slots: CloudsOpaque swirl uses (_Time.y+helper.y) and scroll uses (_Time.x+helper.x); CloudsLitTransparent rotation/scroll use x-slot and wave uses z-slot; Lightning non-TIME_OFFSET route uses x-slot while TIME_OFFSET route uses objectTime+_TimeOffset; Mirror/WaterLit normal scroll uses x-slot. Same uniform name, different lanes.

## 8. Record 8

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Do not unify _Color semantics: CloudsLitTransparent instance-aware tint (CloudProps); Lightning per-renderer instanced; WaterLit instanced base albedo overwritten by lighting (albedo.rgb=lighting); Spectrogram/SpectrogramUnlit/Glowing non-instanced uniform; Rain MPB with _Intensity scaling; Glowing alpha modulates fog offsets. Same name, different ownership/role.

## 9. Record 9

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: LIGHTMAP constant mismatch must be resolved before sharing lightmap helper: WaterLit uses 4.59479332, Mirror uses 4.594793 (truncated). Treat as DEFECT_CANDIDATE pending binary authority check.

## 10. Record 10

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Partial CloudsLitTransparent keyword bundles have no recovered fragment row and intentionally fall back to generic _Color route; do not invent hybrid formulas. FOG/HEIGHT_FOG properties in that shader are hidden serialization-only (no fragment route).

## 11. Record 11

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: Rain active-route coverage is narrower than its Properties block: frag implements only TEXTURE_COLOR, _ALPHACHANNEL_RED, MASK_RED_IS_ALPHA, VERTEX_COLOR, VERTEX_SQUARE_ALPHA, _FOGTYPE_COLOR+BLOOM_FOG. COLOR_GRADIENT/MASK/MASK2/SOFT_PARTICLES/CLOSE_TO_CAMERA/VIEW_ALIGN/HOLOGRAM/SQUARE_ALPHA/NOISE_DITHERING/white-boost declare properties/keywords but have no code in the active vert/frag shown. Any sharing claim for those features is UNVERIFIED.

## 12. Record 12

- Classification: `UNVERIFIED`
- Source: environment.json
- Text: WaterLit production route set excludes DIFFUSE/SPECULAR/white-boost/emission/decal/vertex-color per W2 despite Properties exposing them; Mirror SPECULAR property has no 1.44 binary per M5; Glowing _ENABLE_COLOR_INSTANCING/_CUTOUT*/_NOISE_DITHERING/_WHITEBOOSTTYPE_ALWAYS properties are intentionally inert per G6. Do not treat exposed-but-inert properties as behavior.

## 13. Record 13

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Arc.shader, Unlit.shader, Rain.shader, ObstacleOutline.shader call ComputeScreenPosCustom but declare no STEREO_INSTANCING_ON pragma (Arc/Unlit/Rain: 0 hits; ObstacleOutline: 0 hits), so the unity_StereoEyeIndex eye-offset branch is dead there by construction. Unifying or removing the branch changes nothing for them today but silently changes output the day a pragma is added.

## 14. Record 14

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Consumers must keep UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO + UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX + multi_compile_instancing discipline: Arc vert transfers instance ID without UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO and its frag calls only UNITY_SETUP_INSTANCE_ID (no POST_VERTEX eye setup). Same partial pattern in Unlit (transfer, no INIT_STEREO; frag no POST_VERTEX). Do not assume eye index is valid in those shaders.

## 15. Record 15

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: _StereoCameraEyeOffsets is a ChroMapper-authored global with no game-binary provenance cited in-tree; its values are written by C# at runtime. Do not replace with engine defaults.

## 16. Record 16

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Bloom.hlsl declares camera-global uniforms (comment: material Properties must NOT redeclare them). Verified: no consumer redeclares _BaseColorBoost/_BaseColorBoostThreshold in Properties. Any refactor must preserve exactly one declaration site.

## 17. Record 17

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Call-site argument conventions differ BY DESIGN and must not be normalized: Arc passes bloomValue=albedo.a*fogTransmission^2 with premultiply=albedo.a; ObstacleOutline passes premultiply=1 with bloomValue=color.a; Particles passes bloomValue/boostInput/whiteboostMultiplier remaps; ParametricSliceBillboard passes bloomValue=alpha*alpha with _BloomWhiteMultiplier. Same function, intentionally different inputs.

## 18. Record 18

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not merge with Blurs.hlsl: signatures are incompatible (sampler2D+radius vs TEXTURE2D_ARGS+texelSize) and the tent-filter radius convention differs (Blurs upsampleTent applies *radius*0.5; BloomUpsampleTent applies *sampleScale with no 0.5). Merging changes blur output.

## 19. Record 19

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not lift the TEXTURE2D_*/SAMPLE_TEXTURE2D/BLOOM_SAMPLE_UV fallback macros to a wider scope: Post Process/ChromaticAberration.shader:26-30 defines its own overlapping TEXTURE2D_ARGS/TEXTURE2D_PARAM/TEXTURE2D_SAMPLER2D/SAMPLE_TEXTURE2D fallbacks. All definitions are #ifndef-guarded so co-inclusion is safe today, but no shader includes both files — joint-inclusion behavior is UNVERIFIED.

## 20. Record 20

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not drop the abs() on texel size at FragDownsample4Alpha/FragDownsample4 call sites: abs(_BloomTexelSize.xy) is a call-site choice, not library behavior.

## 21. Record 21

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Zero consumers: safe to leave untouched, unsafe to reference as precedent. Any new #include of Blurs.hlsl must adopt legacy sampler2D + explicit radius convention, which no current shader uses.

## 22. Record 22

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: `BLURS_INCLUDED` guard is not `CHROMAPPER_`-namespaced; a future same-named guard in third-party code would silently skip the include. Renaming is a no-behavior-change edit but out of scope for this read-only assignment.

## 23. Record 23

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not rename distanceSquared(): Lit.shader declares block-scope locals named `distanceSquared` (lines 1952, 2308) and Note.shader:379 declares frag-scope `float distanceSquared`. All current collisions are scope-safe (no post-declaration call to distanceSquared() in the same scope), but any refactor that moves those locals or adds a call after them breaks compilation. This is the highest-risk name in the library.

## 24. Record 24

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not unify CloudsOpaque's inline distFade (lines 217-218) with CalculateCustomFogFactor: operation order differs (attenuation applied before _FogStartOffset subtraction, extra * _FogScale outside the reciprocal). Same inputs, different output.

## 25. Record 25

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not unify BloomfogMesh's alpha-modulated u0 fog with CalculateCustomFogFactor: it multiplies dir2 by 1/max(color.a,1) first — a family-specific (mesh-line) contract with no library equivalent.

## 26. Record 26

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: ApplyColorFog's BLOOM_FOG&&FOG early-out vs height-blend branches depend on consumer keyword sets (Lit gates COLOR_BY_FOG with !(BLOOM_FOG&&FOG); Particles calls it unconditionally in its color-fog route). Moving branches changes gated outputs.

## 27. Record 27

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: _CustomFogTextureToScreenRatio centering math ((uv-0.5)*ratio+0.5) is load-bearing for BLOOM_FOG prepass sampling; CloudsOpaque:24-25 documents direct screenPos.xy/screenPos.w sampling as the in-fragment equivalent — keep both, do not 'simplify' one to the other without DXBC proof.

## 28. Record 28

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: _DirectionalLightPositions/Radii/Directions/Colors[5] + _PrivatePointLight* are engine-written uniforms. No consumer redeclares them; keep the single declaration site in Lighting.hlsl.

## 29. Record 29

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: CloudsOpaque negates normals (INVERT_DIFFUSE_NORMAL) BEFORE calling CalculateLightDiffuse — the inversion is call-site convention, invisible to the library. Do not 'fix' by adding inversion inside the shared function.

## 30. Record 30

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: CloudsLitTransparent's front+reversed diffuse sum is unnormalized-input, BACK_LIGHTING-gated family behavior; the shared BOTH_SIDES_DIFFUSE term is a different formulation. Unifying them changes cloud output.

## 31. Record 31

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Lit:1952-1960 private-point-light block and Lit:2308-2309 dissolve point-distance block are Lit-local light math that deliberately does not call CalculateLightFalloff (different denominators: /distanceSquared intensity vs 1/(d/r^2*25+1)).

## 32. Record 32

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not unify the three reflection encodings: (1) packed 6-channel light-bake-ID probe pair (Reflection.hlsl, Lit + ParametricBoxTransparent), (2) engine unity_SpecCube0 + DecodeHDR (WaterLit), (3) plain _EnvironmentReflectionCube texCUBElod (Note REFLECTION_MAP family, Lit REFLECTION_TEXTURE path). Same helper names at call sites do not imply same data.

## 33. Record 33

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: WaterLit includes Reflection.hlsl but never calls SampleReflectionProbePair* — it uses only BoxProjectReflectionDirection + CalculateViewReflectionDirection (via nesting). Removing the include is a no-behavior-change edit but out of scope; removing the header's probe-pair functions would break Lit/ParametricBoxTransparent.

## 34. Record 34

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Lit REFLECTION_STATIC branch (reflectionDirection = worldPosition + normal, line ~2082) bypasses CalculateViewReflectionDirection deliberately; do not route it through the shared function.

## 35. Record 35

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: The 1.7/0.7/6.0 LOD polynomial is triplicated (Reflection.hlsl SampleReflectionProbePair, Lit:2029 REFLECTION_TEXTURE block, WaterLit:369, Note:350 variant with x=w+1-smoothness+saturate(dist*0.01-0.3)). Unification is blocked: the inputs (smoothness vs x) and samplers/encodings differ per family.

## 36. Record 36

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Only Lit and Particles declare the _Custom_Time KeywordEnum (_CUSTOM_TIME_FREEZE/_CUSTOM_TIME_SONG_TIME) that GetTime dispatches on. Routing any manual time site through GetTime silently changes output unless that shader also adopts the keywords + _SongTime/_TimeHelperOffset plumbing. This is the #1 adoption blocker for Time.hlsl.

## 37. Record 37

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Lit:1516-1534 displacement-time branches handle _CUSTOM_TIME_FREEZE but NOT _CUSTOM_TIME_SONG_TIME (they read _Time.y + offset in the #else). If GetTime is the contract authority, those two branches are SONG_TIME-divergent by construction — flagged SEMANTICALLY_SIMILAR_NOT_SAFE, needs owner decision (read-only: no fix).

## 38. Record 38

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Rain.shader:107 declares its own uniform float4 _TimeOffset (material/vertex input, NOT instanced Props) — same name, different contract vs Props._TimeOffset used with GetTime in Lit/Particles. Do not conflate.

## 39. Record 39

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: CloudsLitTransparent reads _Time.z (+helper.z) for vertex wave and _Time.x (+helper.x) for rotation/scroll — component selection is family behavior GetTime's float4 preserves (components exist) but the FREEZE/SONG_TIME dispatch would alter it.

## 40. Record 40

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: GetParametricCameraPosition() is a one-line alias of GetStereoAwareCameraPosition with no added semantics; ParametricBoxOpaque bypasses it and calls GetStereoAwareCameraPosition directly (line 138). Do not 'fix' the inconsistency by mass-rewriting call sites — the alias may exist for future parametric-specific behavior (UNVERIFIED).

## 41. Record 41

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: CalculateParametricDistanceTransmission returns TRANSMISSION (rcp(...), 1 = no fog) while Fog.hlsl CalculateCustomFogFactor returns FOG FACTOR (1 = full fog). Same density shape, inverted contract, plus an extra alphaDivisor input. Substituting one for the other inverts fog.

## 42. Record 42

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: The height-ramp smoothstep appears in three spellings: ParametricShared (saturate+max(eps) guard), Fog CalculateCustomHeightFogFactor (clamp), Particles PRECISE_FOG (saturate, no eps guard). Outputs agree except at globalHeight==0 (division guard differs: max(globalHeight,1e-5) vs none). Edge-case behavior at zero band height is UNVERIFIED.

## 43. Record 43

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Lit:2687 ApplyNoiseDither(albedo, 0.0, _GlobalBlueNoiseTex) — scalar 0.0 splats to float4(0,0,0,0), so noiseUv = 0/0 = NaN and tex2D samples with a NaN UV (undefined result). Gated behind #if USE_NOISE_SCREEN_POSITION/#else, so it only executes when the screen-position path is compiled out. Whether NaN-UV tex2D is benign on all target GPUs is UNVERIFIED — flagged DEFECT_CANDIDATE, needs runtime proof, not a refactor.

## 44. Record 44

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: BuildNoiseScreenPosition's objectTranslation/randomValue plumbing differs per consumer (Lit:1725, Mirror:182, Spectrogram:134, Particles:959, etc.); the function only concatenates inputs — input provenance stays a consumer responsibility.

## 45. Record 45

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: WaterLit declares its own _ReflectionProbeIntensity/_ReflectionProbeBoxProjection* uniforms (WaterLit:217-219) while including Reflection.hlsl whose functions take intensity as an argument — no collision today because the header declares no uniforms, but any edit that adds header-level uniforms risks silent shadowing. Keep the header argument-passing contract.

## 46. Record 46

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Lit declares samplerCUBE _ReflectionProbeTexture1/2 + six _LightProbeLightBakeId* uniforms locally (Lit:568+); ParametricBoxTransparent declares its own pair (PBT:140-141). Same names, separate shader scopes — safe, but a merged 'common uniforms' header would collide with WaterLit's unity_SpecCube0 path.

## 47. Record 47

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Single consumer (BasicGradient). Widening use is safe (zero coupling) but each new consumer pays compile cost for ~30 functions; prefer per-shader need, not blanket inclusion.

## 48. Record 48

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Easings.hlsl declares file-scope `const float s/s2` (Back easing). A consumer that also declares s/s2 at file scope would collide — no current consumer does. Keep the narrow names inside the header (observation, not a fix request).

## 49. Record 49

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: None for the shared functions. Note: ObstacleOutline/ObstacleDistortion/ParametricBoxFakeGlow each gate cutout under their own keywords and feed different textureOffset/scale sources — input provenance is consumer-side; the shared math must stay provenance-agnostic.

## 50. Record 50

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: CloudsLitTransparent includes ObjectShared.hlsl but only calls RotateObjectPositionY (vertex layer rotation at line 228); it never calls CalculateRotatedObjectPosition. The include is justified (shared rotation primitive) but the song-time packing half of the header is unused there — splitting is out of scope.

## 51. Record 51

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: CalculateRotatedObjectPosition packs `objectTime + 0.001 - songTime` into .w with a hardcoded 0.001 bias. Arc:123 and Note:239 both consume .w downstream under family-specific semantics — the bias is load-bearing recovered behavior, do not touch.

## 52. Record 52

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not generalize the 63.0-bin assumption: i.uv.x*63 spans 64 bins (0..63) and Particles indexes _SpectrogramData[] with the same helper. Changing the constant desynchronizes mesh UVs from audio data on both shaders.

## 53. Record 53

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Spectrogram.shader:101-123 vs SpectrogramUnlit.shader:74-94 duplicate the instancing/stereo prologue around the shared call; only the index helper itself is shared. Unifying the prologues is out of scope (different varyings).

## 54. Record 54

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: ApplyAcesTonemapping's saturate() wrapper means sub-zero and >1 inputs clamp — callers passing already-saturated vs HDR-linear inputs get different (intended, per-family) results. CloudsOpaque:241 wraps float4(color,0.0) and takes .rgb; Mirror:254 tones only the lighting term, not the reflection composite. These are call-site composition choices, not library defects — do not normalize.

## 55. Record 55

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: BloomShared BloomApplyKneeAndAces chains into ApplyAcesTonemapping; Bloom.shader:128 FragUpsampleAces calls it directly. ACES is applied exactly once per route — adding a second application (e.g., 'ensuring' tonemapping at a shared choke point) would double-tonemap.

## 56. Record 56

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: SkyGradient gates ACES behind USE_TONE_MAPPING/ACES_TONE_MAPPING multi_compiles (SkyGradient:26-27); most others use #if defined(ACES_TONE_MAPPING). Keyword spelling/compile mix differs per shader — the function is shared, the gating is not.

## 57. Record 57

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not touch Fog.hlsl's distanceSquared() name or its shadowing locals in Note.shader:379 / Lit.shader:1952,2308 without a compile check — scope-safe today, fragile on any edit.

## 58. Record 58

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not route manual _Time readers through GetTime without also adopting the _Custom_Time KeywordEnum + _SongTime plumbing in that shader — output changes under FREEZE/SONG_TIME materials.

## 59. Record 59

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not unify the three reflection encodings (packed bake-ID pair vs unity_SpecCube0+DecodeHDR vs plain _EnvironmentReflectionCube) — same helper names, different data contracts.

## 60. Record 60

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not merge Blurs.hlsl into BloomShared.hlsl (incompatible sampler/radius contracts; tent-scale 0.5 discrepancy).

## 61. Record 61

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Do not normalize CalculateBloomComposition call-site arguments (premultiply/bloomValue conventions differ by design per family).

## 62. Record 62

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Lit.shader:2687 ApplyNoiseDither(albedo, 0.0, ...) NaN-UV fallback is the single DEFECT_CANDIDATE — needs runtime proof on target GPUs before any change.

## 63. Record 63

- Classification: `UNVERIFIED`
- Source: includes.json
- Text: Parametric transmission vs Fog factor inversion (rcp vs 1-x): substituting one for the other inverts fog — keep both spellings with their contracts documented.

## 64. Record 64

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: STEREO_HLSL_GAP: 255 unique stereo vertex DXBC blobs have no recovered vertex HLSL (LIT_REAUDIT.md Limits). Stereo plumbing is source-observed only; do not treat ComputeScreenPosCustom eye-offset path as binary-proven.

## 65. Record 65

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: RUNTIME_OWNERSHIP_GAP: MPB/live streams, imported FBX secondary-UV retention, LightProbe cubemaps, _BloomPrePassTexture, _GlobalBlueNoiseTex, _SongTime/_TimeHelperOffset producers, and 16 executable replacement routes with no recovered fragment binary are unproven at runtime.

## 66. Record 66

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: PASS1_GAP: pass 1 (Meta-pattern arithmetic) metadata/draw route unknown; Lit ships pass 0 only. Do not invent utility pass.

## 67. Record 67

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: DECLARED_WITHOUT_BODY_CONSUMER: _MpmMipBias/MPM_CUSTOM_MIP, FLIPBOOK_BLENDING_OFF, _PARALLAX_PROJECTION_WARPED/_Parallax_Projection, _EmissionFogSuppression/_MainEffectFogSuppression/USE_FOG_SUPPRESSION, _SecondaryUVsDiffuse/_SecondaryUVsNormal, _DISSOLVEALPHA_FADE are declared/pragma-guarded but have no consumer in the Lit.shader body observed. _DIFFUSE_TEXTURE_SOURCE_TEXTURE is documented compatibility-inert; _SECONDARY_UVS_ADDITIVE_OFFSET inert on represented routes; orphan DISSOLVE_TEXTURE/DISSOLVE_COLOR inert without DISSOLVE parent. These block EXACT sharing claims for those selectors.

## 68. Record 68

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: UNRESOLVED_NAMES: _Intensity, _UVColors/_UVRimColors, _ColorsArrayOffset have no proven source name/producer; formulas use active recovered routes (anonymous registers do not prove names). WATERLIT_REFLECTION_OUT_OF_SCOPE: WaterLit reflection consumer outside SimpleLit audit; do not merge without its reaudit.

## 69. Record 69

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: LIGHTMAP_ON_GLOBAL_GAP: global LIGHTMAP_ON changes binaries but is not owned/selected; Lit owns local LIGHTMAP route only. Do not add LIGHTMAP_ON.

## 70. Record 70

- Classification: `UNVERIFIED`
- Source: lit.json
- Text: DEFECT_CANDIDATE_ASSERTION: none asserted. The 13 binary-proven fixes (rim-dim fragment distance, Grid exp2 mapping, secondary-source gating, additive-offset inert, packing cull, precise double-normalize, dissolve-after-dither, iridescence/non-iridescence epilogues, import order, antiflicker payload/centroid, mask/detail fallback intensities, SDF fourth-root, TEXTURE compat gate) are recorded applied with 0 unresolved records; labeling any current behavior DEFECT_CANDIDATE would require new binary evidence, which this read-only inventory does not propose.

## 71. Record 71

- Classification: `UNVERIFIED`
- Source: particles.json
- Text: No shader compilation or variant-count validation performed

## 72. Record 72

- Classification: `UNVERIFIED`
- Source: particles.json
- Text: No runtime MPB/stream/visual-parity validation (explicitly required by reaudit)

## 73. Record 73

- Classification: `UNVERIFIED`
- Source: particles.json
- Text: No verification of 6v3+123 asset/scene drift beyond reaudit text

## 74. Record 74

- Classification: `UNVERIFIED`
- Source: particles.json
- Text: Cross-shader formula diffs summarized from targeted greps, not full pairwise diffs

## 75. Record 75

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B1 runtime-vs-bake pass divergence: BloomRenderer uses passes (0,2,5) only; ReflectionProbeBakePipeline.ApplyBloom uses (first 0 else 1) + (2) final — do not unify pass tables without bake-pipeline owner sign-off.

## 76. Record 76

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B2 sampler-type split: BloomShared.hlsl uses Texture2D+SamplerState+SAMPLE_TEXTURE2D+BLOOM_SAMPLE_UV(saturate); Blurs.hlsl uses legacy sampler2D+tex2D with no clamp — mechanical merge changes edge sampling.

## 77. Record 77

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B3 stereo split in PostBloom.shader: Texture2DArray+eye-index layer under STEREO_INSTANCING_ON vs Texture2D otherwise; shared helper must preserve layer semantics.

## 78. Record 78

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B4 blend-state boundary: BlitBlendColor.shader relies on fixed-function Blend SrcAlpha OneMinusSrcAlpha + ColorMask RGB; BloomfogMesh.shader relies on material-driven Blend [_BlendSrcFactor] [_BlendDstFactor],... + BlendOp; SkyGradient.shader relies on Blend One One, Zero Zero — none of this state is expressible inside shared HLSL.

## 79. Record 79

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B5 GrabPassTexture1.shader is legacy CGPROGRAM/appdata_base + GrabPass _GrabTexture1 + ColorMask 0; no HLSL shared helper applies.

## 80. Record 80

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B6 PostBloom blue-noise contract: _GlobalBlueNoiseTex sampled with SampleLevel(...,0.0) at (uv+(0.1,0.2))*params+random and added to bloom BEFORE scene+fade; PostProcess.hlsl helper divides noiseScreenPos.xy/ww and uses tex2D — not interchangeable.

## 81. Record 81

- Classification: `UNVERIFIED`
- Source: post_bloom.json
- Text: B7 _BloomParams slot meaning differs by consumer: runtime BloomRenderer sets (0,0,threshold,0) with x=autoExposureLimit=0, w=legacy=0; pass-13 auto-exposure path therefore depends on _GlobalIntensityTex probe value — do not repurpose x/w without pyramid-driver change.

## 82. Record 82

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B1; area: runtime-visual; detail: No RenderDoc/PIX/generic frame capture or named comparison image was found; static evidence does not prove live MPBs, generated streams, imported mesh semantics, or visual parity.; evidence: /tmp/opencode/lit_modularization_20260903/evidence_audit/report.md:66-78

## 83. Record 83

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B2; area: object-arc; detail: Arc uses a LineRenderer-compatible TEXCOORD0/uv.y edge adapter while recovered SliderNoteCrossedStrips uses a mesh edge channel; exact ABI equivalence is not proven.; evidence: Assets/_Graphics/Shaders/Object/Arc.shader:135-150

## 84. Record 84

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B3; area: object-outline; detail: ObstacleOutline is an editor adapter, not an ObstacleCore replacement; FrameHD/FrameLW anonymous uniform bindings and current environment reachability remain unresolved.; evidence: Assets/_Graphics/Shaders/Object/ObstacleOutline.shader:1-15; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:37-50

## 85. Record 85

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B4; area: post-process-chromatic; detail: ChromaticAberration is split from a related combined PPv2 Uber/Bloom source; no standalone recovered shader metadata joins the split pass to this asset.; evidence: Assets/_Graphics/Shaders/Post Process/ChromaticAberration.shader:1-7

## 86. Record 86

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B5; area: lit; detail: Lit audit leaves sixteen executable replacement routes without recovered fragment binaries and does not establish pass-1 draw role/state.; evidence: Assets/_Graphics/Shaders/LIT_REAUDIT.md:65-78

## 87. Record 87

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B6; area: particles; detail: Particles omits OVERDRAW_VIEW/zero-row selectors and unsupported VERTEX_DISPLACEMENT routes; seven current materials request unsupported VERTEX_DISPLACEMENT.; evidence: Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:103-115; Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:162-170

## 88. Record 88

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B7; area: water-reflection; detail: Packed WaterLit reflection route cannot replace the Unity fallback until Billie/Gaga provide packed probe assets; current ReflectionProbeData references are null.; evidence: Assets/_Graphics/Shaders/WaterLit.shader:22-25; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:13-25

## 89. Record 89

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B8; area: parametric; detail: FrameHD and FrameLW have no current environment library/material mapping and anonymous uniform bindings; separate replacements are blocked.; evidence: Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:37-50

## 90. Record 90

- Classification: `UNVERIFIED`
- Source: scope_remaining.json
- Text: id: B9; area: known-source-discrepancies; detail: ParametricSlice cap UV uses 0.36 in source versus audited exact 0.25; ParametricBoxFakeGlow guards a denominator that recovered DXBC divides directly. Both are recorded as defect candidates only.; evidence: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader:265-270; Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl:11-19; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:11-18; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:32-35

## Consumer gap register

1. **Dead-or-unproven setters need a runtime verdict before any rename.**
   `_OpaqueAlpha` (MPB writes in `Arc/ChainIndicatorContainer`, zero shader
   decls), `_GlobalLightTintColor` (`GlobalShaderColorLightsController`,
   zero decls), `_FrustumPlanes` (`PerCameraShaderSetupController`, zero
   decls), `_ChroMapperPostProcessSource` / `_ChroMapperPostBloomOutput` /
   `_ChroMapperPostBloomTexture` (buffer ids, zero decls). Each is either dead
   code to delete or a missing shader input to add (the latter costs
   variants). Decide per item with frame-debugger evidence, not by reading
   code. (`property_setters.csv`, `scripts/property_shader_hit.log`.)
2. **No validation that scene-bound dynamic names exist.**
   ~7,700 MPB bindings (`MaterialPropertyBlockColorSetter`,
   `MaterialPropertyValuesSetter`, `SpectrogramRowPropertyAnimator`,
   random-value/position animators, `MaterialLightsController`,
   FX `Property`/`PropertyName` fields) push strings into whatever material
   the renderer holds. A typo'd or renamed property fails silently every
   frame. Recommend an editor-time check that walks env scenes + target
   materials and reports unresolvable names. Data:
   `scripts/dynamic_serialized_values.log`, `scripts/env_components.log`.
3. **Bake-pipeline-only property names are unverified against shaders.**
   `_ProbeRawTex`/`_ProbeBloomTex`/`_ProbeSourceA-C`/`_ProbeBlend`/
   `_ProbeSourceTexelOffset`/`_BloomThreshold` exist only as transient
   material properties in `ReflectionProbeBakePipeline` (expected: no repo
   shader declares them). Confirm the bake shaders declare them (they load
   by asset path) before touching that pipeline.
4. **Chroma map-data contract pins keyword names externally.**
   `CanonicalizeGlowingKeyword` (`GeometryAppearanceSO`) and the `NoteModelSO`
   remap translate stored map keywords. Renaming `CUTOUT`, `RIM_DIM`,
   `HEIGHT_FOG`, `_FOGTYPE_LERP`, `_WHITEBOOSTTYPE_*`, `NOISE_DITHERING`,
   `PLANE_CUT` breaks already-shipped custom maps, not just repo assets.
   Treat these as public API.
5. **Stale `AlwaysIncludedShaders` entry + dead `BsStandardEditor` ref.**
   Guid `346f9fe3…` resolves to no file; `Legacy/Standard.shader` names a
   `CustomEditor` class that does not exist. Both harmless today, both worth
   one cleanup commit with a build check — not bundled with a shader refactor.
6. **No automated consumer coverage.**
   `Assets/Tests*` reference no shaders; the only compile probe is temporary
   audit tooling (`Assets/Editor/__LitShaderAuditTemp/`); shader breakage
   renders as Diffuse fallback or magenta rather than failing a check.
   Minimum useful gate: `Shader.Find` each of the 16 mapped shaders +
   `keywordSpace.FindKeyword` for every name in `keyword_setters.csv` +
   `HasProperty` for every `property_setters.csv` confirmed row.
7. **Known schema gaps are out of scope but adjacent.**
   CloudsLitTransparent omits 106/137 source props; Particles omits 8;
   `_VertexDisplacementMode`→`_VertexDisplacement` unmapped (mode vs toggle
   contract unproven) — all per `SHADER_PROPERTY_MAPPING_AUDIT.md`. The
   dynamic-setter inventory here adds one more: confirm no scene binds the
   omitted names before closing.

## Mesh and platform gap register

1. Beat Saber source-mesh channels. Gap: all packing/spectrogram/color-array UV claims beyond the 54 repo meshes depend on imported Beat Saber environment meshes (`EnvironmentMeshSO` 389 hashes) not present as decodable bytes here. Proven only: repo meshes (17 uv1, 1 uv2, 0 uv3). Close: at runtime in Unity 6000.3.13f1, iterate `EnvironmentMeshSO.Lookup` values and log `mesh.uv2/uv3/colors` min/max/counts per hash, plus which `ChromaID` uses `MESH_PACKING/COLOR_ARRAY`.
2. Runtime `_MeshPackingId/_ColorsArrayOffset/IDVector` maxima. Gap: no C# setter for `_MeshPackingId` found; `_ColorsArrayOffset=i*num` and `IDVector` depend on per-prefab `ColorArrayData.Length/MpbControllers` and scene anchors. Proven only: material ids `{0,1,2,4,6}`, mesh uv1.y `0..6`. Close: log MPB values per draw (`Renderers[].GetPropertyBlock`) in a loaded environment + dump `ColorArrayData.Length`/`MpbControllers.Length` per `ColorArrayLightsController`.
3. `UV_COLOR_SEGMENTS` provider. Gap: zero library variants enable it; `_UVColors/_UVRimColors` unserialized. Close: confirm whether any Beat Saber map/manifest sets `UV_COLOR_SEGMENTS` at runtime via `BaseMaterial.ShaderKeywords`, or mark dead for Lit modularization.
4. `StripUnusedMeshComponents=1` interaction. Gap: player builds may strip `uv1/uv2/uv3/color` not referenced by the compiled variant; `USE_MESH_PACKING_UV1` switches between `TEXCOORD1/3`. Close: build `StandaloneWindows64` (per `SimpleEditorUtils`) and inspect built mesh strides + a `MESH_PACKING` variant dump.
5. Serialized channel encoding. Gap: normal channel `format:1 dimension:52` and `VertexChannelCompressionMask=214` bit order are inferred from offsets/stride, not Unity source. Close: verify against Unity 6000.3 `Mesh` YAML docs or `VertexAttributeDescriptor` serialization; re-emit `mesh_channels_raw.csv` with format-aware dims.
6. Graphics API / color-space / path enum names. Gap: raw hex/ints and flags are proven; names (`D3D11/12, Vulkan, Metal, Linear, Forward, Multi-Pass`) come from external docs and conflict across sources. Close: open project in 6000.3.13f1 and read PlayerSettings UI + `Editor.log` device list.
7. Shader OOB semantics. Gap: `_ColorsArray[_caIdx]` (200/150), `_UVColors[(uint)(x*10)]` (10), `_SpectrogramData` values have no bounds/value clamps (spectrogram bin index is clamped, values are not). `round(8.5)` half-way, negative-uv `max`, and `uv1.x=1.0→index 10` behavior are HLSL/driver-defined. Close: D3D11/Vulkan GPU capture or CPU mirror of formulas with proven mesh values.
8. Variant explosion/preload. Gap: no `ShaderVariantCollection`, no `IPreprocessShaders`, Lit not in `AlwaysIncluded/Preloaded`; actual built variant set is unproven. Close: run a build with shader-variant logging (`m_LogWhenShaderIsCompiled` is currently 0) and capture `variants.sqlite` provenance (repo-root `variants.sqlite` was not inspected here).
9. FBX importer channel contributions. Gap: `.fbx` files listed in `mesh_files.log` were not decoded; their importer settings could add/strip UV2/colors. Close: inspect `.fbx.meta` `additionalUVs/vectorBlendShapes` and re-run channel survey post-import.

## Evidence authority gap register

1. **No frame authority:** no RenderDoc/PIX capture, screenshot pair, or named visual comparison output exists in the assigned roots.
2. **Exported source is a stub:** `SimpleLit.shader` has a complete property table but a `DummyShaderTextExporter` implementation. It cannot establish original formulas.
3. **Recovered HLSL is derived:** 4,352 HLSL files are reconstructed through SPIR-V; 255 stereo vertex hashes have ASM only. Exact ASM must override HLSL interpretation.
4. **Route-level audit outputs are absent:** `LIT_REAUDIT.md` records 1,160 stage records and 52 hash reconciliations, but no corresponding route-to-hash CSV/JSON/database was found in the repository.
5. **Audit source is absent:** five ChroMapper Lit audit tools survive only as CPython 3.14 `.pyc`; their `.py` source files are missing.
6. **Repository database is empty:** `/mnt/programs/Code/GitRepository/ChroMapper/variants.sqlite` is zero bytes.
7. **Pass 1 remains weakly identified:** compiled evidence exists, but pass metadata/name and live draw reachability are not proven.
8. **Runtime state is not captured:** static scenes/materials do not prove material property blocks, generated vertex streams, imported UV semantics, camera globals, or per-frame keyword ownership.
9. **Replacement gaps remain documented:** `LIT_REAUDIT.md` records 16 executable replacement routes without a recovered fragment binary and no recovered full-spectrogram variant.
10. **Toolchain gap:** `dxc`, `fxc`, `vkd3d-compiler`, and `spirv-cross` are not on PATH. Existing exact ASM can be audited; regenerating all derived outputs needs the original recovery toolchain.
11. **Audit-output discrepancy:** `README.md` says `official_shader_audit.json` covers 74 records/72 shader names, but the current JSON contains only nine LIV shader records and does not include `Custom/SimpleLit`.
12. **Source bundle not independently checked:** `_shader.json` names a Steam installation bundle outside the two assigned evidence paths. This audit used the extracted DXBC corpus and did not inspect that external source root.
