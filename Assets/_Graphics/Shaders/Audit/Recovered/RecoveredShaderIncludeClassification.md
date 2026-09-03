# Include classification

## Scope

This register classifies all 16 shared includes in the behavior scan. Include placement does not establish recovered catalog membership or behavior equivalence.

## 1. ShaderLibrary/Bloom.hlsl

- Classification: `recovered common`
- Include: ShaderLibrary/Bloom.hlsl
- Placement Classification: recovered common
- Consumers: Glowing.shader; Lit.shader; Object/Arc.shader; Object/Note.shader; Object/ObstacleOutline.shader; ParametricBoxFakeGlow.shader; ParametricBoxOpaque.shader; ParametricBoxTransparent.shader; ParametricSliceBillboard.shader; Particles.shader; Post Process/PostBloom.shader; Unlit.shader
- Consumer Count: 12
- Functions: CalculateWhiteBoost; CalculateBloomComposition; CalculateBloomPostComposition
- Guards: CHROMAPPER_BLOOM_INCLUDED
- Uniforms: _BaseColorBoost; _BaseColorBoostThreshold
- Lexical Hash: 87f85e9a405d1ac34091f28c8f932317e539be8c69bb1b6caf4aa4dd0ef756c6
- Operation Hash: 52af26f4c8abbab72bf915ec09c1adc2d5a3d977e8b727c4b5a978ef22c97c46
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Game-recovered white-boost composition shared verbatim across 12 consumers in ≥5 families (Lit, Particles, Unlit/Glowing, Note/Arc/Obstacle, Parametric). No family keywords inside the header.
- Blockers: Bloom.hlsl declares camera-global uniforms (comment: material Properties must NOT redeclare them). Verified: no consumer redeclares _BaseColorBoost/_BaseColorBoostThreshold in Properties. Any refactor must preserve exactly one declaration site.; Call-site argument conventions differ BY DESIGN and must not be normalized: Arc passes bloomValue=albedo.a*fogTransmission^2 with premultiply=albedo.a; ObstacleOutline passes premultiply=1 with bloomValue=color.a; Particles passes bloomValue/boostInput/whiteboostMultiplier remaps; ParametricSliceBillboard passes bloomValue=alpha*alpha with _BloomWhiteMultiplier. Same function, intentionally different inputs.

## 2. ShaderLibrary/BloomShared.hlsl

- Classification: `correctly located`
- Include: ShaderLibrary/BloomShared.hlsl
- Placement Classification: correctly located
- Consumers: Post Process/Bloom.shader
- Consumer Count: 1
- Functions: BloomDownsample4; BloomDownsample13Classic; BloomUpsampleTent; BloomUpsampleBox; BloomWeightedCombine; BloomAlphaGate; BloomRec601AutoExposureKnee; BloomApplyKneeAndAces; BloomApplyGamma
- Guards: BLOOM_SAMPLE_UV; CHROMAPPER_BLOOM_SHARED_INCLUDED; SAMPLER; SAMPLE_TEXTURE2D; TEXTURE2D; TEXTURE2D_ARGS; TEXTURE2D_PARAM; TEXTURE2D_SAMPLER2D
- Lexical Hash: 6246d945e26a4bbf74c90bf5bad053da1fb9e0300d9a336ce5fdb3ce654e9bb0
- Operation Hash: 39ab6bc97b0a2824a3365fbad3d35520b33d1b87b52fc3a72ca90dd6373a3a08
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Single-consumer-by-design: the only post-process bloom shader funnels all 14 passes through it. Content is the family's shared implementation (shared across passes), not cross-family code, and it is already in a *Shared-named file. Narrowing further gains nothing.
- Blockers: Do not merge with Blurs.hlsl: signatures are incompatible (sampler2D+radius vs TEXTURE2D_ARGS+texelSize) and the tent-filter radius convention differs (Blurs upsampleTent applies *radius*0.5; BloomUpsampleTent applies *sampleScale with no 0.5). Merging changes blur output.; Do not lift the TEXTURE2D_*/SAMPLE_TEXTURE2D/BLOOM_SAMPLE_UV fallback macros to a wider scope: Post Process/ChromaticAberration.shader:26-30 defines its own overlapping TEXTURE2D_ARGS/TEXTURE2D_PARAM/TEXTURE2D_SAMPLER2D/SAMPLE_TEXTURE2D fallbacks. All definitions are #ifndef-guarded so co-inclusion is safe today, but no shader includes both files — joint-inclusion behavior is UNVERIFIED.; Do not drop the abs() on texel size at FragDownsample4Alpha/FragDownsample4 call sites: abs(_BloomTexelSize.xy) is a call-site choice, not library behavior.

## 3. ShaderLibrary/Blurs.hlsl

- Classification: `family-specific too broad`
- Include: ShaderLibrary/Blurs.hlsl
- Placement Classification: family-specific too broad
- Consumer Count: 0
- Functions: downsample4; upsampleTent; kawase; box
- Guards: BLURS_INCLUDED
- Lexical Hash: b2d18a3d0c89e49956dfe3dd501aee79543ab08b16db11c8c48e08fab4f348be
- Operation Hash: a8c156565c1d527c945e53a8c1d67e3749b6e5cbd9cf5a0c4b49bffaff315519
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Legacy post-process-family blur code with zero consumers, kept at shared-library scope. Superseded by BloomShared.hlsl for the only bloom implementation in-tree. Must not be widened or merged without re-verification.
- Contract Mismatches: downsample4 vs BloomDownsample4: same 4-tap box math but incompatible contract (sampler2D+radius vs TEXTURE2D_ARGS+texelSize+BLOOM_SAMPLE_UV clamp).; upsampleTent vs BloomUpsampleTent: offset scale differs (Blurs multiplies radius*0.5; BloomShared multiplies sampleScale with no 0.5). Same 9-tap tent weights, different sampling grid — outputs differ.
- Blockers: Zero consumers: safe to leave untouched, unsafe to reference as precedent. Any new #include of Blurs.hlsl must adopt legacy sampler2D + explicit radius convention, which no current shader uses.; `BLURS_INCLUDED` guard is not `CHROMAPPER_`-namespaced; a future same-named guard in third-party code would silently skip the include. Renaming is a no-behavior-change edit but out of scope for this read-only assignment.

## 4. ShaderLibrary/Camera.hlsl

- Classification: `authored Core`
- Include: ShaderLibrary/Camera.hlsl
- Placement Classification: authored Core
- Consumers: CloudsOpaque.shader; Glowing.shader; Lit.shader; Object/Arc.shader; Object/Note.shader; Object/ObstacleDistortion.shader; ParametricBoxOpaque.shader; ParametricSliceBillboard.shader; Rain.shader; ShaderLibrary/Fog.hlsl; ShaderLibrary/Lighting.hlsl; ShaderLibrary/ParametricShared.hlsl; Unlit.shader
- Consumer Count: 13
- Functions: GetStereoAwareCameraPosition; ComputeScreenPosCustom
- Guards: !UNITY_UV_STARTS_AT_TOP; CHROMAPPER_CAMERA_INCLUDED; defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON)
- Uniforms: _StereoCameraEyeOffsets
- Lexical Hash: 5cd4cce5f8997f9807934621fee46d618f2e84291c0b9922fd4643612bcdd43b
- Operation Hash: c4d8a60cff8a4d35a90c201e8cd1485cbf6683a437f255dff166564c40f22903
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: ChroMapper-authored stereo abstraction over UnityCG (custom _StereoCameraEyeOffsets uniform, no game-binary provenance cited). Consumed directly by 10 shaders and transitively by ~all fog/lighting users. No family-specific keywords or material uniforms.
- Contract Mismatches: CloudsOpaque.shader:209-211 inlines its own stereo eye select (_WorldSpaceCameraPos base, same three-keyword guard) instead of calling GetStereoAwareCameraPosition — duplicated contract, not a call.; Mirror.shader:224-226 inlines eye select under a DIFFERENT guard (USING_STEREO_MATRICES) with _WorldSpaceCameraPos base. Guard mismatch vs library (UNITY_SINGLE_PASS_STEREO||STEREO_INSTANCING_ON||STEREO_MULTIVIEW_ON): the two agree only when both/neither keyword sets are defined.; Arc/Unlit/Rain/ObstacleOutline lack the STEREO_INSTANCING_ON pragma, so ComputeScreenPosCustom silently degrades to ComputeNonStereoScreenPos for them.
- Blockers: Arc.shader, Unlit.shader, Rain.shader, ObstacleOutline.shader call ComputeScreenPosCustom but declare no STEREO_INSTANCING_ON pragma (Arc/Unlit/Rain: 0 hits; ObstacleOutline: 0 hits), so the unity_StereoEyeIndex eye-offset branch is dead there by construction. Unifying or removing the branch changes nothing for them today but silently changes output the day a pragma is added.; Consumers must keep UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO + UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX + multi_compile_instancing discipline: Arc vert transfers instance ID without UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO and its frag calls only UNITY_SETUP_INSTANCE_ID (no POST_VERTEX eye setup). Same partial pattern in Unlit (transfer, no INIT_STEREO; frag no POST_VERTEX). Do not assume eye index is valid in those shaders.; _StereoCameraEyeOffsets is a ChroMapper-authored global with no game-binary provenance cited in-tree; its values are written by C# at runtime. Do not replace with engine defaults.

## 5. ShaderLibrary/Cutout.hlsl

- Classification: `correctly located`
- Include: ShaderLibrary/Cutout.hlsl
- Placement Classification: correctly located
- Consumers: Object/ObstacleDistortion.shader; Object/ObstacleOutline.shader; ParametricBoxFakeGlow.shader
- Consumer Count: 3
- Functions: CalculateObjectSpaceCutoutPosition; ApplyCutoutNoise
- Guards: CHROMAPPER_CUTOUT_INCLUDED
- Lexical Hash: d823f6e268aba4a4b58cdee6df58b2496573b7e799a15aff25b240ecd69d1134
- Operation Hash: ff0f94d873cbcf880ae1bcb736e7f1d2678a26a7a62dac24608a7f1fe7623e16
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Two-function, zero-coupling helper shared verbatim by 3 consumers across 2 families (Obstacle + Parametric). Narrow and correctly placed.
- Blockers: None for the shared functions. Note: ObstacleOutline/ObstacleDistortion/ParametricBoxFakeGlow each gate cutout under their own keywords and feed different textureOffset/scale sources — input provenance is consumer-side; the shared math must stay provenance-agnostic.

## 6. ShaderLibrary/Data.hlsl

- Classification: `authored Core`
- Include: ShaderLibrary/Data.hlsl
- Placement Classification: authored Core
- Consumers: Lit.shader; ShaderLibrary/Lighting.hlsl; ShaderLibrary/Reflection.hlsl
- Consumer Count: 3
- Functions: InitializeSurfaceData
- Guards: CHROMAPPER_DATA_INCLUDED
- Lexical Hash: ce20cd957f152ecb23ae631f509198b9bf32649fbe73085ad86982d8367f03d3
- Operation Hash: c5245f19fb56f7753f963afd5c012da67f569265df59dde293d6dfe250f16de3
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: ChroMapper-authored pipeline data vocabulary (Surface/Lighting/Emission) with zero keywords/uniforms. Directly included only by Lit; WaterLit reaches it via Reflection nesting. Core data definitions belong at core scope.
- Contract Mismatches: InitializeEmissionData + all EmissionData plumbing is Lit-local (Lit:1106+) although EmissionData is declared in Data.hlsl — the struct lives in core, its constructor does not.; WaterLit constructs SurfaceData with (worldPos, worldNormal, i.uv, i.uv, albedo, _Metallic, _Smoothness) — uv1 duplicates uv, secondary wrapping/mpm/occlusion stay defaults; a narrower constructor would suffice but none exists (observation only).
- Blockers: WaterLit declares its own _ReflectionProbeIntensity/_ReflectionProbeBoxProjection* uniforms (WaterLit:217-219) while including Reflection.hlsl whose functions take intensity as an argument — no collision today because the header declares no uniforms, but any edit that adds header-level uniforms risks silent shadowing. Keep the header argument-passing contract.; Lit declares samplerCUBE _ReflectionProbeTexture1/2 + six _LightProbeLightBakeId* uniforms locally (Lit:568+); ParametricBoxTransparent declares its own pair (PBT:140-141). Same names, separate shader scopes — safe, but a merged 'common uniforms' header would collide with WaterLit's unity_SpecCube0 path.

## 7. ShaderLibrary/Easings.hlsl

- Classification: `correctly located`
- Include: ShaderLibrary/Easings.hlsl
- Placement Classification: correctly located
- Consumers: Object/BasicGradient.shader
- Consumer Count: 1
- Functions: Step; Quadratic_In; Quadratic_Out; Quadratic_InOut; Cubic_In; Cubic_Out; Cubic_InOut; Quartic_In; Quartic_Out; Quartic_InOut; Quintic_In; Quintic_Out; Quintic_InOut; Sinusoidal_In; Sinusoidal_Out; Sinusoidal_InOut; Exponential_In; Exponential_Out; Exponential_InOut; Circular_In; Circular_Out; Circular_InOut; Elastic_In; Elastic_Out; Elastic_InOut; Back_In; Back_Out; Back_InOut; Bounce_Out; Bounce_In; Bounce_InOut
- Guards: CHROMAPPER_EASINGS_INCLUDED
- Lexical Hash: 8ff9d1d332fe5900e45dcfbe5f6212ad2d2720d7121541ad7759a96a51da38d5
- Operation Hash: 56c518135e2dacfd6fb2f1aee87f37af580ec8ea395bbcc0e3c3a1ef47126dcd
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Pure-function math library with zero uniforms, zero keywords, zero engine coupling. Single consumer today, but generic content with no family concepts — shared scope is harmless and correct.
- Blockers: Single consumer (BasicGradient). Widening use is safe (zero coupling) but each new consumer pays compile cost for ~30 functions; prefer per-shader need, not blanket inclusion.; Easings.hlsl declares file-scope `const float s/s2` (Back easing). A consumer that also declares s/s2 at file scope would collide — no current consumer does. Keep the narrow names inside the header (observation, not a fix request).

## 8. ShaderLibrary/Fog.hlsl

- Classification: `mixed`
- Include: ShaderLibrary/Fog.hlsl
- Placement Classification: mixed
- Consumers: CloudsOpaque.shader; Glowing.shader; Lit.shader; Mirror.shader; Object/Arc.shader; Object/Note.shader; Object/ObstacleDistortion.shader; Object/ObstacleOutline.shader; ParametricBoxFakeGlow.shader; ParametricBoxOpaque.shader; ParametricBoxTransparent.shader; ParametricSliceBillboard.shader; Particles.shader; Rain.shader; Spectrogram.shader; SpectrogramUnlit.shader; Unlit.shader; WaterLit.shader
- Consumer Count: 18
- Functions: distanceSquared; CalculateCustomFogFactor; CalculateCustomHeightFogFactor; CalculateHeightFogFactor; ApplyColorFog; SampleBloomPrePass; BlendFogColor; ApplyBloomFogCalculatedFactor; ApplyBloomFog; ApplyBloomHeightFogCalculatedFactor; ApplyBloomHeightFog
- Guards: CHROMAPPER_FOG_INCLUDED; CUSTOM_FOG_ATTENUATION_NAME; CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME; CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME; CUSTOM_FOG_OFFSET_NAME; defined(BLOOM_FOG); defined(BLOOM_FOG) && defined(FOG); defined(FOG_COLOR_HIGHLIGHT); defined(_FOGTYPE_ALPHA); defined(_FOGTYPE_COLOR)
- Uniforms: _CustomFogTextureToScreenRatio; _BloomPrePassTexture
- Lexical Hash: 996f7dfdd5c9a62f9802233f033a42e7f8e42488eaeab7aecb47b93cfe067a51
- Operation Hash: 09e30df6c7751e9ecaa218a133499b7d827fdcb29c3cbfd173163dc62c87e3be
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: One header bundles (a) genuinely cross-family fog math (distance/height factors, prepass sampling, ~18 consumers) with (b) BloomFog-family prepass globals and (c) _FOGTYPE_*/FOG_COLOR_HIGHLIGHT keyword dispatch. The (b)/(c) coupling is why consumers like Note hand-roll blends around the shared calls instead of calling the top-level Apply* wrappers.
- Contract Mismatches: Note.shader:379 shadows the library function with frag-scope `float distanceSquared`; subsequent line 382 uses the variable (correct today, fragile).; Lit.shader:1952,2308 shadow with block-scope `float distanceSquared`; line 2614 calls distanceSquared(worldPos) at function scope (correct today, fragile).; CloudsOpaque.shader:217-218 inline distFade reorders attenuation vs offsets (SEMANTICALLY_SIMILAR_NOT_SAFE).; BloomfogMesh.shader:90-93 alpha-modulated u0 has no library equivalent (family-specific).; Note.shader:393-397 hand-composes SampleBloomPrePass+BlendFogColor with sourceAlpha preservation under _FOGTYPE_*/HEIGHT_FOG guards the library does not know.; Particles CalculateParticleHeightFogClearFactor adds a PRECISE_FOG branch that duplicates CalculateHeightFogFactor inline instead of calling it in both branches.
- Blockers: Do not rename distanceSquared(): Lit.shader declares block-scope locals named `distanceSquared` (lines 1952, 2308) and Note.shader:379 declares frag-scope `float distanceSquared`. All current collisions are scope-safe (no post-declaration call to distanceSquared() in the same scope), but any refactor that moves those locals or adds a call after them breaks compilation. This is the highest-risk name in the library.; Do not unify CloudsOpaque's inline distFade (lines 217-218) with CalculateCustomFogFactor: operation order differs (attenuation applied before _FogStartOffset subtraction, extra * _FogScale outside the reciprocal). Same inputs, different output.; Do not unify BloomfogMesh's alpha-modulated u0 fog with CalculateCustomFogFactor: it multiplies dir2 by 1/max(color.a,1) first — a family-specific (mesh-line) contract with no library equivalent.; ApplyColorFog's BLOOM_FOG&&FOG early-out vs height-blend branches depend on consumer keyword sets (Lit gates COLOR_BY_FOG with !(BLOOM_FOG&&FOG); Particles calls it unconditionally in its color-fog route). Moving branches changes gated outputs.; _CustomFogTextureToScreenRatio centering math ((uv-0.5)*ratio+0.5) is load-bearing for BLOOM_FOG prepass sampling; CloudsOpaque:24-25 documents direct screenPos.xy/screenPos.w sampling as the in-fragment equivalent — keep both, do not 'simplify' one to the other without DXBC proof.

## 9. ShaderLibrary/Lighting.hlsl

- Classification: `recovered common`
- Include: ShaderLibrary/Lighting.hlsl
- Placement Classification: recovered common
- Consumers: CloudsLitTransparent.shader; CloudsOpaque.shader; Lit.shader; Mirror.shader; ShaderLibrary/Reflection.hlsl; Spectrogram.shader
- Consumer Count: 6
- Functions: CalculateLightFalloff; CalculateDirectionalDiffuseTerm; CalculateLightDiffuseAccumulation; CalculateLightDiffuse; CalculateViewReflectionDirection; CalculateSpecularLobeFactor; CalculateLightSpecularLobe; CalculateSpecularReflectionDirection; CalculateLightSpecular; CalculateLightFalloffDiffuse; CalculateLightFalloffSpecularLobe; CalculateLightFalloffSpecular
- Guards: CHROMAPPER_LIGHTING_INCLUDED; defined(BOTH_SIDES_DIFFUSE)
- Uniforms: _DirectionalLightColors; _DirectionalLightDirections; _DirectionalLightPositions; _DirectionalLightRadii; _PrivatePointLightIntensity; _PrivatePointLightPosition
- Lexical Hash: 327db59d82360049c1e79372e153eb118f73c6ef2d30bff4c72a4da36b9f27d6
- Operation Hash: 1233aa5a68cc26d4af3fa63cf166cd8d7ab6d2c620b6137bb4d19ded2a68b6bf
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Five-light rig + falloff + specular-lobe math shared verbatim across Clouds/Mirror/Spectrogram/Lit via direct and Reflection-nested inclusion. BOTH_SIDES_DIFFUSE is the only internal keyword and is a genuine cross-family material option.
- Contract Mismatches: CloudsLitTransparent:276-277 sums CalculateLightDiffuse(n)+CalculateLightDiffuse(-n)*boost instead of BOTH_SIDES_DIFFUSE (different both-sides formulation).; Mirror:189 CalculateMirrorDiffuseLighting is a LIGHT_FALLOFF dispatcher unknown to the library.; Note.shader does not include Lighting.hlsl at all (custom REFLECTION_MAP route) — any lighting-unification scope must exclude Note.; Spectrogram:150-164 selects falloff vs plain variants under its own gates; WaterLit uses only CalculateViewReflectionDirection from this header (never the 5-light accumulators).
- Blockers: _DirectionalLightPositions/Radii/Directions/Colors[5] + _PrivatePointLight* are engine-written uniforms. No consumer redeclares them; keep the single declaration site in Lighting.hlsl.; CloudsOpaque negates normals (INVERT_DIFFUSE_NORMAL) BEFORE calling CalculateLightDiffuse — the inversion is call-site convention, invisible to the library. Do not 'fix' by adding inversion inside the shared function.; CloudsLitTransparent's front+reversed diffuse sum is unnormalized-input, BACK_LIGHTING-gated family behavior; the shared BOTH_SIDES_DIFFUSE term is a different formulation. Unifying them changes cloud output.; Lit:1952-1960 private-point-light block and Lit:2308-2309 dissolve point-distance block are Lit-local light math that deliberately does not call CalculateLightFalloff (different denominators: /distanceSquared intensity vs 1/(d/r^2*25+1)).

## 10. ShaderLibrary/ObjectShared.hlsl

- Classification: `recovered common`
- Include: ShaderLibrary/ObjectShared.hlsl
- Placement Classification: recovered common
- Consumers: CloudsLitTransparent.shader; Object/Arc.shader; Object/Note.shader
- Consumer Count: 3
- Functions: RotateObjectPositionY; CalculateRotatedObjectPosition
- Guards: CHROMAPPER_OBJECT_SHARED_INCLUDED
- Lexical Hash: e4b3586e8f5a9c3946be98a976b7312cf5ec5867bd005fffb2403349b75c7d13
- Operation Hash: a0caa20d462ff9d61fe6ea3c2a2c9054f5a4bee2af906d9148ef525033fab2d9
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Object-family rotation/time packing shared verbatim across Note/Arc/Clouds call sites. No keywords, no uniforms; the 0.001-bias packing is recovered game behavior used by ≥2 families.
- Contract Mismatches: Note:198 applies RotateObjectPositionY to a reflection DIRECTION vector, not a position — safe only because the function is rotation-only (offset must be ~0 there); worth a comment at the call site, not a library change. (Observation; needs owner confirmation — UNVERIFIED.)
- Blockers: CloudsLitTransparent includes ObjectShared.hlsl but only calls RotateObjectPositionY (vertex layer rotation at line 228); it never calls CalculateRotatedObjectPosition. The include is justified (shared rotation primitive) but the song-time packing half of the header is unused there — splitting is out of scope.; CalculateRotatedObjectPosition packs `objectTime + 0.001 - songTime` into .w with a hardcoded 0.001 bias. Arc:123 and Note:239 both consume .w downstream under family-specific semantics — the bias is load-bearing recovered behavior, do not touch.

## 11. ShaderLibrary/ParametricShared.hlsl

- Classification: `family-specific too broad`
- Include: ShaderLibrary/ParametricShared.hlsl
- Placement Classification: family-specific too broad
- Consumers: ParametricBoxFakeGlow.shader; ParametricBoxOpaque.shader; ParametricBoxTransparent.shader; ParametricSliceBillboard.shader
- Consumer Count: 4
- Functions: GetParametricCameraPosition; CalculateParametricHeightRamp; CalculateParametricDistanceTransmission; WarpParametricNoisePosition; CalculateParametricNoiseUv; CalculateParametricWorldFade; SampleParametricWorldNoise
- Guards: CHROMAPPER_PARAMETRIC_SHARED_INCLUDED; defined(WORLD_NOISE_WARP); defined(WORLD_SPACE_FADE)
- Lexical Hash: f4131df98f7c5ca577ffd922b377dd889ec6f9d3727b4a7689239c7838bcfb08
- Operation Hash: d4ceeeb9334513a0e996abb37b462833cee3f33b4271055931221598be6d590b
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: All 4 consumers are the Parametric family; no other family includes it. Content (height ramp, transmission, noise warp) is parametric-material behavior, not cross-family engine utility. Sitting at ShaderLibrary root overstates its scope — family consumers only. (No move proposed; read-only.)
- Contract Mismatches: ParametricBoxOpaque:138 calls GetStereoAwareCameraPosition directly while its 3 siblings call GetParametricCameraPosition (intra-family inconsistency).; ParametricBoxFakeGlow:160 computes viewDirection inline (normalize(worldPos - GetParametricCameraPosition())) instead of sharing a helper — duplicates Lighting's view-direction math without calling it.
- Blockers: GetParametricCameraPosition() is a one-line alias of GetStereoAwareCameraPosition with no added semantics; ParametricBoxOpaque bypasses it and calls GetStereoAwareCameraPosition directly (line 138). Do not 'fix' the inconsistency by mass-rewriting call sites — the alias may exist for future parametric-specific behavior (UNVERIFIED).; CalculateParametricDistanceTransmission returns TRANSMISSION (rcp(...), 1 = no fog) while Fog.hlsl CalculateCustomFogFactor returns FOG FACTOR (1 = full fog). Same density shape, inverted contract, plus an extra alphaDivisor input. Substituting one for the other inverts fog.; The height-ramp smoothstep appears in three spellings: ParametricShared (saturate+max(eps) guard), Fog CalculateCustomHeightFogFactor (clamp), Particles PRECISE_FOG (saturate, no eps guard). Outputs agree except at globalHeight==0 (division guard differs: max(globalHeight,1e-5) vs none). Edge-case behavior at zero band height is UNVERIFIED.

## 12. ShaderLibrary/PostProcess.hlsl

- Classification: `recovered common`
- Include: ShaderLibrary/PostProcess.hlsl
- Placement Classification: recovered common
- Consumers: BloomFog/BloomfogSkybox.shader; CloudsOpaque.shader; Lit.shader; Mirror.shader; ParametricBoxOpaque.shader; ParametricSliceBillboard.shader; Particles.shader; Spectrogram.shader; WaterLit.shader
- Consumer Count: 9
- Functions: ApplyNoiseDither; BuildNoiseScreenPosition
- Guards: CHROMAPPER_POST_PROCESS_INCLUDED
- Lexical Hash: 447e3b90e989341d0304eb6bd877b02dfc99ddb5600457b6a49ea99baeff7982
- Operation Hash: abfff63308a6209bfc160815524bafc3f1218336a4afeb597656dff04af340fd
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Blue-noise dither pair shared verbatim across 9 consumers in 6+ families with zero keywords and zero globals inside. Textbook recovered-common content.
- Contract Mismatches: Lit:2687 scalar-0.0 noiseScreenPosition (see blockers).; CloudsOpaque:21 documents _GlobalBlueNoiseParams vertex scaling before BuildNoiseScreenPosition — a consumer-side input convention the header does not (and should not) enforce.
- Blockers: Lit:2687 ApplyNoiseDither(albedo, 0.0, _GlobalBlueNoiseTex) — scalar 0.0 splats to float4(0,0,0,0), so noiseUv = 0/0 = NaN and tex2D samples with a NaN UV (undefined result). Gated behind #if USE_NOISE_SCREEN_POSITION/#else, so it only executes when the screen-position path is compiled out. Whether NaN-UV tex2D is benign on all target GPUs is UNVERIFIED — flagged DEFECT_CANDIDATE, needs runtime proof, not a refactor.; BuildNoiseScreenPosition's objectTranslation/randomValue plumbing differs per consumer (Lit:1725, Mirror:182, Spectrogram:134, Particles:959, etc.); the function only concatenates inputs — input provenance stays a consumer responsibility.

## 13. ShaderLibrary/Reflection.hlsl

- Classification: `recovered common`
- Include: ShaderLibrary/Reflection.hlsl
- Placement Classification: recovered common
- Consumers: Lit.shader; ParametricBoxTransparent.shader; WaterLit.shader
- Consumer Count: 3
- Functions: DecodeReflectionProbeChannel; DecodeReflectionProbePair; SampleReflectionProbePairLod; SampleReflectionProbePair; BoxProjectReflectionDirection
- Guards: CHROMAPPER_REFLECTION_INCLUDED
- Lexical Hash: a66a8da92dbf0f0d6edbbf2d327f96987bc648d5def6140d5d3017d5ee53869f
- Operation Hash: f88eee5bb5caaaa1e2a26c1596df5f3b4a4c9f090e1173f4027f0c2cb3db8ce0
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Packed probe-pair decode + LOD polynomial + box projection shared verbatim across Lit and Parametric families; WaterLit reuses box projection. Zero uniforms/keywords inside the header (pure argument-passing contract) keeps it safe at shared scope.
- Contract Mismatches: WaterLit:371-374 samples unity_SpecCube0 with UNITY_SAMPLE_TEXCUBE_LOD + DecodeHDR instead of SampleReflectionProbePairLod — same box-projection, different decode contract.; ParametricBoxTransparent:244 builds incident=normalize(worldPos-camera) and calls reflect() instead of CalculateViewReflectionDirection — algebraically identical output for the same camera, but the entry contract differs (caller-supplied GetParametricCameraPosition vs lib-internal stereo camera).; Note.shader:351-356 REFLECTION_MAP path (front/back F0, _FinalColorMul scaling, cutout mixing) shares only the texCUBElod instruction with the library.; Lit:2030-2044 REFLECTION_TEXTURE path (_EnvironmentReflectionCube * _ReflectionTexIntensity * metallic/smoothness/rim terms) is a Lit-only composition, not the probe-pair contract.
- Blockers: Do not unify the three reflection encodings: (1) packed 6-channel light-bake-ID probe pair (Reflection.hlsl, Lit + ParametricBoxTransparent), (2) engine unity_SpecCube0 + DecodeHDR (WaterLit), (3) plain _EnvironmentReflectionCube texCUBElod (Note REFLECTION_MAP family, Lit REFLECTION_TEXTURE path). Same helper names at call sites do not imply same data.; WaterLit includes Reflection.hlsl but never calls SampleReflectionProbePair* — it uses only BoxProjectReflectionDirection + CalculateViewReflectionDirection (via nesting). Removing the include is a no-behavior-change edit but out of scope; removing the header's probe-pair functions would break Lit/ParametricBoxTransparent.; Lit REFLECTION_STATIC branch (reflectionDirection = worldPosition + normal, line ~2082) bypasses CalculateViewReflectionDirection deliberately; do not route it through the shared function.; The 1.7/0.7/6.0 LOD polynomial is triplicated (Reflection.hlsl SampleReflectionProbePair, Lit:2029 REFLECTION_TEXTURE block, WaterLit:369, Note:350 variant with x=w+1-smoothness+saturate(dist*0.01-0.3)). Unification is blocked: the inputs (smoothness vs x) and samplers/encodings differ per family.

## 14. ShaderLibrary/SpectrogramShared.hlsl

- Classification: `correctly located`
- Include: ShaderLibrary/SpectrogramShared.hlsl
- Placement Classification: correctly located
- Consumers: Particles.shader; Spectrogram.shader; SpectrogramUnlit.shader
- Consumer Count: 3
- Functions: CalculateSpectrogramIndex
- Guards: CHROMAPPER_SPECTROGRAM_SHARED_INCLUDED
- Lexical Hash: 6241999744be89ce9a4b2f069ca522306ee2b8a888d816cef369619ea2470341
- Operation Hash: addfe252e492d64a358d9b82e992cc7a8986cfc7f7dd7b0682c6ab9c245c1205
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Single-function, zero-coupling helper shared verbatim by all 3 spectrogram-data consumers. Narrow and correctly placed.
- Blockers: Do not generalize the 63.0-bin assumption: i.uv.x*63 spans 64 bins (0..63) and Particles indexes _SpectrogramData[] with the same helper. Changing the constant desynchronizes mesh UVs from audio data on both shaders.; Spectrogram.shader:101-123 vs SpectrogramUnlit.shader:74-94 duplicate the instancing/stereo prologue around the shared call; only the index helper itself is shared. Unifying the prologues is out of scope (different varyings).

## 15. ShaderLibrary/Time.hlsl

- Classification: `recovered common`
- Include: ShaderLibrary/Time.hlsl
- Placement Classification: recovered common
- Consumers: Lit.shader; Particles.shader
- Consumer Count: 2
- Functions: GetTimeOffsetVector; GetTime
- Guards: CHROMAPPER_TIME_INCLUDED; defined(_CUSTOM_TIME_FREEZE); defined(_CUSTOM_TIME_SONG_TIME)
- Uniforms: _SongTime; _TimeHelperOffset
- Lexical Hash: a45c39af9884ca8dc1bf1a43f0bb6b3d5c226e4be9fa4bd9e16dab56322e3452
- Operation Hash: 10175d8d97deca4e31ef13aec85377fef4bf86da4b1c068278f0113867d02f1a
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Two-consumer shared contract today, but the contract (vector convention + FREEZE/SONG_TIME dispatch) is the documented authority for time behavior and 7+ shaders hand-roll its standard-branch subset. Under-adopted, not misplaced.
- Contract Mismatches: CloudsLitTransparent:227,234,265; CloudsOpaque:173,190; Mirror:202; WaterLit:321; ParametricBoxTransparent:210; ParametricSliceBillboard:335; Lightning:144 — all scalar _Time+_TimeHelperOffset reads bypassing FREEZE/SONG_TIME dispatch.; Lit:1519,1534 FREEZE-checked but SONG_TIME-ignorant displacement branches.; Rain:143-145 _Time+_TimeOffset (non-Props uniform) phase math.; Arc:142 (_Time.y scroll), ObstacleDistortion:211 (_Time.y scroll) — raw _Time with no helper offset at all.
- Blockers: Only Lit and Particles declare the _Custom_Time KeywordEnum (_CUSTOM_TIME_FREEZE/_CUSTOM_TIME_SONG_TIME) that GetTime dispatches on. Routing any manual time site through GetTime silently changes output unless that shader also adopts the keywords + _SongTime/_TimeHelperOffset plumbing. This is the #1 adoption blocker for Time.hlsl.; Lit:1516-1534 displacement-time branches handle _CUSTOM_TIME_FREEZE but NOT _CUSTOM_TIME_SONG_TIME (they read _Time.y + offset in the #else). If GetTime is the contract authority, those two branches are SONG_TIME-divergent by construction — flagged SEMANTICALLY_SIMILAR_NOT_SAFE, needs owner decision (read-only: no fix).; Rain.shader:107 declares its own uniform float4 _TimeOffset (material/vertex input, NOT instanced Props) — same name, different contract vs Props._TimeOffset used with GetTime in Lit/Particles. Do not conflate.; CloudsLitTransparent reads _Time.z (+helper.z) for vertex wave and _Time.x (+helper.x) for rotation/scroll — component selection is family behavior GetTime's float4 preserves (components exist) but the FREEZE/SONG_TIME dispatch would alter it.

## 16. ShaderLibrary/Tonemapping.hlsl

- Classification: `authored Core`
- Include: ShaderLibrary/Tonemapping.hlsl
- Placement Classification: authored Core
- Consumers: BloomFog/SkyGradient.shader; CloudsLitTransparent.shader; CloudsOpaque.shader; Lit.shader; Mirror.shader; Object/BasicGradient.shader; Object/Event.shader; Object/Note.shader; Object/ObstacleSimple.shader; ShaderLibrary/BloomShared.hlsl; Spectrogram.shader; Unlit.shader; WaterLit.shader
- Consumer Count: 13
- Functions: ApplyAcesTonemapping
- Guards: CHROMAPPER_TONEMAPPING_INCLUDED
- Lexical Hash: ef83ad565b67a748f2c531178397cb655a3d3d4ccd8ec7dc16a112babc8171b6
- Operation Hash: ebddd639a93ebfc0b0303a768f408a0220f011c4356243d9b6dbe6e9102b672d
- Evidence: Actual #include graph and current source
- Confidence: MEDIUM
- Notes: Standard filmic curve (Knarkowicz fit, ETAN-attributed comment), not game-binary recovery. Widest-shared function in the library (13 consumers). Hand-authored core utility, correctly placed.
- Blockers: ApplyAcesTonemapping's saturate() wrapper means sub-zero and >1 inputs clamp — callers passing already-saturated vs HDR-linear inputs get different (intended, per-family) results. CloudsOpaque:241 wraps float4(color,0.0) and takes .rgb; Mirror:254 tones only the lighting term, not the reflection composite. These are call-site composition choices, not library defects — do not normalize.; BloomShared BloomApplyKneeAndAces chains into ApplyAcesTonemapping; Bloom.shader:128 FragUpsampleAces calls it directly. ACES is applied exactly once per route — adding a second application (e.g., 'ensuring' tonemapping at a shared choke point) would double-tonemap.; SkyGradient gates ACES behind USE_TONE_MAPPING/ACES_TONE_MAPPING multi_compiles (SkyGradient:26-27); most others use #if defined(ACES_TONE_MAPPING). Keyword spelling/compile mix differs per shader — the function is shared, the gating is not.
