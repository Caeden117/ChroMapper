# Shared candidates

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their behavior records remain available with an `adjacent_unverified` scope status.

## 1. /cross_shader_classification/0

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: environment.json
- Blocker Or Note: Different fogFactor inputs and different BlendFogColor gating (_FOGTYPE_* vs unconditional). Ordinary-fog branches are NOT interchangeable.
- Confidence: high
- Evidence: Fog.hlsl:115-143 signatures; call sites: CloudsOpaque frag (SampleBloomPrePass direct + lerp), WaterLit:401-411, Spectrogram:180-181, SpectrogramUnlit:114-116, Mirror:269-271, Glowing:132-133, Rain:191-197 direct sample. README: ENABLE_BLOOM_FOG->BLOOM_FOG alias; POST_BLOOM/BLOOM_FOG runtime-owned.
- Helpers: SampleBloomPrePass; ApplyBloomFogCalculatedFactor; ApplyBloomFog; ApplyBloomHeightFog; ApplyBloomHeightFogCalculatedFactor; BlendFogColor
- Regions: fragment fog/bloom-fog composition

## 2. /cross_shader_classification/2

- Classification: `EXACT_SHARED`
- Source: environment.json
- Confidence: high
- Evidence: Tonemapping.hlsl:7-16 constants a=2.51 b=0.03 c=2.43 d=0.59 e=0.14 + saturate; identical call ApplyAcesTonemapping(float4) in CloudsOpaque:240-242, CloudsLitTransparent:293-295, WaterLit:397-399, Spectrogram:171-173, Mirror:252-256 (gated DIFFUSE||LIGHTMAP). Absent in Lightning/Rain/SetDepthOnly/Stencil/SpectrogramUnlit/Glowing by design.
- Helpers: ApplyAcesTonemapping
- Regions: fragment tone-map stage; order: after lighting saturation, before fog+dither (CloudsOpaque O10; CloudsLitTransparent bottom-fade before ACES, runway fade after ACES)

## 3. /cross_shader_classification/3

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: environment.json
- Confidence: high
- Evidence: PostProcess.hlsl:7-14 (noise-0.5)/255; call sites CloudsOpaque:255-262 (NOISE_DITHERING-gated), WaterLit:413-415 (NOISE_DITHERING-gated), Spectrogram:186 unconditional, Mirror:273 unconditional. Audit S5/W6/O7 confirm same constant.
- Helpers: ApplyNoiseDither
- Regions: fragment dither stage

## 4. /cross_shader_classification/4

- Classification: `EXACT_SHARED`
- Source: environment.json
- Confidence: high
- Evidence: PostProcess.hlsl:16-24 signature; WaterLit:297-299, Spectrogram:134-136, Mirror:182-184 identical argument shape (screenPos, clipPos, _GlobalBlueNoiseParams, _GlobalRandomValue, objectTranslation). CloudsOpaque builds noiseScreenPos by vertex multiply only (198-199) - excluded.
- Helpers: BuildNoiseScreenPosition
- Regions: vertex noise-screen-pos construction (WaterLit/Spectrogram/Mirror)

## 5. /cross_shader_classification/6

- Classification: `EXACT_SHARED`
- Source: environment.json
- Confidence: high
- Evidence: Camera.hlsl:15-25; used CloudsOpaque:197, WaterLit:295, Spectrogram:133, SpectrogramUnlit:99, Rain:149, Mirror:181, Glowing:109. Lightning/SetDepthOnly/Stencil do not use it (UnityObjectToClipPos/manual only).
- Helpers: ComputeScreenPosCustom; GetStereoAwareCameraPosition
- Regions: vertex screen-pos + fragment camera-pos (stereo-aware)

## 6. /cross_shader_classification/7

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: environment.json
- Confidence: high
- Evidence: Lighting.hlsl:21-56 BOTH_SIDES_DIFFUSE compile branch; five-light accumulation order 1,0,2,3,4; call sites CloudsOpaque:236 (+saturate), CloudsLitTransparent:276-278 (front lobe + reversed back lobe * base.g * _BackLightingBoost, trusted vector no normalize per LT14), Spectrogram:150-165 (falloff vs non-falloff select), Mirror:189-196 wrapper CalculateMirrorDiffuseLighting. WaterLit production has no DIFFUSE per W2.
- Helpers: CalculateLightDiffuse; CalculateLightDiffuseAccumulation; CalculateDirectionalDiffuseTerm
- Regions: fragment five-light diffuse

## 7. /cross_shader_classification/8

- Classification: `FAMILY_SHARED`
- Source: environment.json
- Confidence: medium-high
- Evidence: Lighting.hlsl:65-169 specular lobe (1-dot(diff,diff)*scale*0.5)^4 chain, scale=smoothness^4*500; Spectrogram:151/157 unusual specular color metallic*(diffuseColor-0.04)+0.04 and 0.96*(1-metallic) diffuse term (S3); Mirror SPECULAR property exposed but no 1.44 binary (M5) and no specular call in frag; WaterLit production has no SPECULAR per W2. LIGHT_FALLOFF selects falloff variants.
- Helpers: CalculateLightSpecular; CalculateLightFalloffDiffuse; CalculateLightFalloffSpecular; CalculateSpecularReflectionDirection
- Regions: fragment specular / falloff lighting

## 8. /cross_shader_classification/9

- Classification: `EXACT_SHARED`
- Source: environment.json
- Confidence: high
- Evidence: SpectrogramShared.hlsl:4-7 uint(max(uv.x*63,0)); Spectrogram:125 + SpectrogramUnlit:109 identical; audits S2/U2 confirm 64-sample index.
- Helpers: CalculateSpectrogramIndex
- Regions: vertex (Spectrogram deform index) + fragment (Unlit visibility index)

## 9. /cross_shader_classification/12

- Classification: `FAMILY_SHARED`
- Source: environment.json
- Confidence: medium
- Evidence: UNITY_INSTANCING_BUFFER_START pattern in CloudsLitTransparent (CloudProps/_Color), Lightning (Props/_Color+_TargetPoint+_TimeOffset), WaterLit (Props/_Color), Rain (Props/_Color); Spectrogram/SpectrogramUnlit use multi_compile_instancing but _Color/_SpectrogramData are non-instanced uniforms; Glowing _Color non-instanced (G2); SetDepthOnly/Stencil/Mirror no instanced props (Mirror only STEREO_INSTANCING_ON). Same macros, different buffer layouts/membership.
- Regions: instancing / per-renderer-data contracts

## 10. /cross_shader_classification/14

- Classification: `FAMILY_SHARED`
- Source: environment.json
- Confidence: medium
- Evidence: SetDepthOnly:29 Blend Zero One + Queue Geometry-1 + ZClip On + ZWrite[_ZWrite] + Cull Off; Stencil:38 Blend Zero One + Queue Geometry-1 + ZClip On + ZWrite Off + Cull[_CullMode]; both stencil Ref/Comp/Pass properties + POSITION-only (Stencil) vs POSITION+COLOR (SetDepthOnly) vertices; fragments return saturated vertex color vs float4(0,0,0,0). Lightning/Rain/WaterLit/Mirror/SpectrogramUnlit/CloudsLitTransparent share Blend[_BlendMode*]/stencil/offset/cull/ZTest property-driven pattern but with different defaults and queues.
- Regions: ShaderLab pass/state shell (blend/cull/depth/stencil/queue/LOD)

## 11. /includes/0/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Glowing:127; Lit:1017,1098,1173,1243,1695,2062,2616,2667; Note:210; ParametricBoxOpaque:138; Lighting.hlsl:60 (CalculateViewReflectionDirection); Fog.hlsl:20 (distanceSquared); ParametricShared.hlsl:8 (GetParametricCameraPosition)
- Confidence: high
- Evidence: Verbatim three-keyword guard + unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex] fallback to _WorldSpaceCameraPos. No consumer redeclares it.
- Name: GetStereoAwareCameraPosition
- Operation Class: EXACT_SHARED
- Signature: inline float3 GetStereoAwareCameraPosition()

## 12. /includes/0/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: CloudsOpaque:197; Glowing:109; Lit:1723; Arc:125; Note:232; ObstacleDistortion:185; ObstacleOutline:130; ParametricBoxOpaque:114; ParametricSliceBillboard:288; Particles:957; Rain:149; Spectrogram:133; SpectrogramUnlit:99; Unlit:96; WaterLit:295
- Confidence: high for function identity; medium for stereo effect (depends on consumer pragma discipline, see blockers)
- Evidence: Single definition; delegates to UnityCG ComputeNonStereoScreenPos then applies _StereoCameraEyeOffsets[unity_StereoEyeIndex] under the stereo guard. Arc/Unlit/Rain/ObstacleOutline never enable STEREO_INSTANCING_ON so the offset branch is dead code for them.
- Name: ComputeScreenPosCustom
- Operation Class: EXACT_SHARED
- Signature: inline float4 ComputeScreenPosCustom(float4 pos)

## 13. /includes/1/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lit:1132 (vertex emission); Lit:1265 (rim light)
- Confidence: high
- Evidence: Formula whiteBoost=(bloomValue*whiteboostMultiplier)^2*baseColorBoost-baseColorBoostThreshold matches header comment and both Lit call sites verbatim.
- Name: CalculateWhiteBoost
- Operation Class: EXACT_SHARED
- Signature: inline float CalculateWhiteBoost(float bloomValue, float whiteboostMultiplier, float baseColorBoost, float baseColorBoostThreshold)

## 14. /includes/1/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Glowing:121 (1.0, albedo.a, 1.0); Arc:162,167 (albedo.a, albedo.a*fogTransmission^2, 0.6); Note:374 (result.a x3, 1.0); ObstacleOutline:184 (1, color.a, 1); ParametricBoxFakeGlow:227 (alpha, alpha, 1); ParametricBoxOpaque:159 (alpha, alpha, 1); ParametricBoxTransparent:257 (alpha, alpha, 1); ParametricSliceBillboard:359 (alpha, alpha*alpha, _BloomWhiteMultiplier); Particles:1595 (bloomValue, boostInput, whiteboostMultiplier); Unlit:114 (albedo.a x3, 1); Lit:2515 (emissionRgb, bloomValue, _EmissionTexWhiteBoostMultiplier)
- Confidence: high
- Evidence: All call sites resolve to saturate(rgb*premultiplyAlpha + (bloomValue*whiteboostMultiplier)^2*baseColorBoost - baseColorBoostThreshold). Argument variation is per-material input routing, not a contract difference.
- Name: CalculateBloomComposition
- Operation Class: EXACT_SHARED
- Signature: inline float3 CalculateBloomComposition(float3 rgb, float premultiplyAlpha, float bloomValue, float whiteboostMultiplier, float baseColorBoost, float baseColorBoostThreshold)

## 15. /includes/1/operations/2

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Particles:1607 (albedo.rgb, bloomValue, _BloomMultiplier); Unlit:118 (albedo.rgb, albedo.a, 1.0)
- Confidence: high
- Evidence: Post-process/deferred route: float4(rgb*alpha, alpha*bloomMultiplier). Only Particles and Unlit use it; PostBloom.shader includes Bloom.hlsl but no call site was found — UNVERIFIED whether that include is vestigial.
- Name: CalculateBloomPostComposition
- Operation Class: EXACT_SHARED
- Signature: inline float4 CalculateBloomPostComposition(float3 rgb, float alpha, float bloomMultiplier)

## 16. /includes/2/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Bloom.shader:40 (13-tap+alpha gate); Bloom.shader:48 (4-tap+alpha gate); Bloom.shader:56,64 + gamma wrappers 71
- Confidence: high
- Evidence: Sole consumer; four-tap box matches Blurs.downsample4 math but with TEXTURE2D_ARGS + BLOOM_SAMPLE_UV clamp. 13-tap weights (1/8 halves, 1/32-1/16-1/8 3x3) used verbatim by passes 0/2.
- Name: BloomDownsample4 / BloomDownsample13Classic
- Operation Class: EXACT_SHARED
- Signature: inline float4 BloomDownsample{4,13Classic}(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)

## 17. /includes/2/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Bloom.shader:77 (tent+combine); Bloom.shader:86 (box+combine); gamma/Reinhard/ACES/auto-exposure wrappers 95,101,114,128,137
- Confidence: high
- Evidence: Nine-tap tent weights (1-2-1 / 2-4-2 / 1-2-1 over 16) and four-tap box used verbatim by passes 5/6 and their gamma/Reinhard/ACES derivatives. FragUpsampleReinhard inlines its own Reinhard term (col*(col*0.25+1)/(col+1)) — BloomShared has no Reinhard helper; that formula is SHADER_LOCAL to Bloom.shader.
- Name: BloomUpsampleTent / BloomUpsampleBox / BloomWeightedCombine / BloomAlphaGate
- Operation Class: EXACT_SHARED
- Signature: inline float4 BloomUpsample{Tent,Box}(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float sampleScale); BloomWeightedCombine(source, destination, sourceWeight, destinationWeight); BloomAlphaGate(color, alphaWeights)

## 18. /includes/2/operations/2

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Bloom.shader:137 (auto-exposure + ACES pass 13)
- Confidence: medium-high
- Evidence: Rec601 dot(0.3,0.59,0.11), exposureCap=0.1/sqrt(luminance), legacy branch on legacyAutoExposure>0. Sole call chain: BloomApplyKneeAndAces -> BloomRec601AutoExposureKnee + ApplyAcesTonemapping.
- Name: BloomRec601AutoExposureKnee / BloomApplyKneeAndAces
- Operation Class: EXACT_SHARED
- Signature: inline float BloomRec601AutoExposureKnee(float3 globalIntensity, float autoExposureLimit, float legacyAutoExposure)

## 19. /includes/2/operations/3

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Bloom.shader:71,95,101
- Confidence: high
- Evidence: Cubic color*(color*(color*0.305306+0.682171)+0.012522878) applied verbatim by passes 4/7/8/10. Same polynomial appears in BloomfogMesh vert (GammaToLinear) — same constants, different wrap (float4 vs rgb-only) and different file; treated as SEMANTICALLY_SIMILAR_NOT_SAFE sibling, not a call.
- Name: BloomApplyGamma
- Operation Class: EXACT_SHARED
- Signature: inline float4 BloomApplyGamma(float4 color)

## 20. /includes/4/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Fog.hlsl internal (ApplyBloomFog, ApplyBloomHeightFog); Lit:2614; Arc:159; ObstacleDistortion:272; Spectrogram:179; Particles:1538 (renamed local bloomFogDistanceSquared, calls function — clean)
- Confidence: high for function; medium for safety (shadowing hazards above)
- Evidence: dot(pos - GetStereoAwareCameraPosition(), same). Note:379 and Lit:1952/2308 declare same-named locals in narrower scopes; no same-scope post-declaration call exists today.
- Name: distanceSquared
- Operation Class: EXACT_SHARED
- Signature: inline float distanceSquared(float3 pos)

## 21. /includes/4/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Glowing:132; Arc:158; Note:381; ObstacleDistortion:271; Spectrogram:178; Lit:2613; Particles:1542
- Confidence: high
- Evidence: max(distSq-fogStartOffset,0) -> max(*fogScale - CUSTOM_FOG_OFFSET,0) -> 1/(x*attenuation+1) -> -x+1. All call sites pass (distance-squared, _FogStartOffset, _FogScale) positionally.
- Name: CalculateCustomFogFactor
- Operation Class: EXACT_SHARED
- Signature: inline float CalculateCustomFogFactor(float distanceSq, float fogStartOffset, float fogScale)

## 22. /includes/4/operations/2

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: CloudsOpaque:224 (worldPos, _HeightFogOffset, 1.0); Note:385,403 (worldPos.xyz, _FogHeightOffset, _FogHeightScale); Spectrogram:175; ObstacleDistortion:268
- Confidence: high
- Evidence: clamp/saturate smoothstep t*t*(3-2t) over (y*scale+offset - band)/band. CloudsOpaque passes literal 1.0 scale; note arg-order (worldPos, offset, scale) is consistent.
- Name: CalculateCustomHeightFogFactor
- Operation Class: EXACT_SHARED
- Signature: inline float CalculateCustomHeightFogFactor(float3 worldPos, float fogHeightOffset, float fogHeightScale)

## 23. /includes/4/operations/3

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lit:1293 via ApplyHeightFogCurve wrapper; Lit:2682-ish exactHeightInput paths; Particles:605-622 via CalculateParticleHeightFogClearFactor
- Confidence: high for the function; medium for Particles wrapper (see below)
- Evidence: 1 - saturate(x)*saturate(x)*(3-2*saturate(x)) over precomputed exactHeightInput. Lit wraps it in ApplyHeightFogCurve (lerp toward 0.1 gray) — wrapper is SHADER_LOCAL-adjacent Lit composition, function itself shared.
- Name: CalculateHeightFogFactor
- Operation Class: EXACT_SHARED
- Signature: inline float CalculateHeightFogFactor(float exactHeightInput)

## 24. /includes/4/operations/4

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: includes.json
- Call Sites: Particles:605-622
- Confidence: medium
- Evidence: PRECISE_FOG branch duplicates the height-curve math inline (heightInput -= band; saturate; 1-h*h*(3-2h)) instead of calling CalculateHeightFogFactor; #else branch calls it. Same output by inspection, two spellings — unification blocked on DXBC proof (read-only: report only).
- Name: CalculateParticleHeightFogClearFactor (Particles-local wrapper)
- Operation Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Signature: inline float CalculateParticleHeightFogClearFactor(float3 worldPosition) [PRECISE_FOG]

## 25. /includes/4/operations/5

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: includes.json
- Call Sites: Lit:2603 (under COLOR_BY_FOG && !(BLOOM_FOG&&FOG) gate); Particles:1495
- Confidence: medium-high
- Evidence: Per-material color-fog args + FOG_COLOR_HIGHLIGHT and BLOOM_FOG&&FOG compile-time branches. Lit adds a call-site COLOR_BY_FOG gate the library does not own.
- Name: ApplyColorFog
- Operation Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Signature: inline float4 ApplyColorFog(float4 result, float3 worldPosition, float colorFogMultiplier, float colorFogMax, float colorFogHighlightMultiplier, float colorFogInfluence, float fogHeightScale, float fogHeightOffset)

## 26. /includes/4/operations/6

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: includes.json
- Call Sites: CloudsOpaque:247; Note:393; Lit:2632; Particles:1463,1552,1556; Rain:194; ParametricBoxOpaque:169
- Confidence: high
- Evidence: BLOOM_FOG-gated centered-ratio prepass sample, else 0. Rain:192-194 documents the screen-door semantics. All call sites use .rgb and handle alpha themselves.
- Name: SampleBloomPrePass / BlendFogColor / ApplyBloomFog* / ApplyBloomHeightFog*
- Operation Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Signature: SampleBloomPrePass(float4 screenPos); BlendFogColor(col, bloomfogCol) [_FOGTYPE_ALPHA|_FOGTYPE_COLOR]; ApplyBloomFog(col, screenPos, worldPos, fogStartOffset, fogScale); ApplyBloomHeightFog(+fogHeightOffset, fogHeightScale) + CalculatedFactor variants

## 27. /includes/5/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lighting.hlsl internal (all falloff diffuse/specular accumulators); Mirror:192,194 via wrapper
- Confidence: high
- Evidence: 1/(dot(lightOffset,lightOffset)/radiusSquared*25+1). Used verbatim by falloff diffuse/specular loops; Lit private-point-light math deliberately uses different denominators (see blockers).
- Name: CalculateLightFalloff
- Operation Class: EXACT_SHARED
- Signature: inline float CalculateLightFalloff(float3 worldPosition, int lightIndex)

## 28. /includes/5/operations/1

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: includes.json
- Call Sites: Lighting.hlsl internal (5-light accumulation); CloudsOpaque:236 (DIFFUSE path, normal possibly pre-inverted); CloudsLitTransparent:276-277 (called twice: n and -n); Mirror:194 via wrapper; Spectrogram:153,164
- Confidence: high for function; medium for both-sides equivalence (see mismatches)
- Evidence: Five _DirectionalLightDirections/Colors taps shared verbatim. BOTH_SIDES_DIFFUSE branch (max(n,0)+min(n,0)*(-mult)) is compile-time-selected; default mult=1.0.
- Name: CalculateDirectionalDiffuseTerm / CalculateLightDiffuseAccumulation / CalculateLightDiffuse
- Operation Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Signature: inline float CalculateDirectionalDiffuseTerm(float normalDot, float bothSidesDiffuseMultiplier=1.0); inline float3 CalculateLightDiffuse(float3 normalWS, float bothSidesDiffuseMultiplier=1.0)

## 29. /includes/5/operations/2

- Classification: `FAMILY_SHARED`
- Source: includes.json
- Call Sites: Mirror:189-196 (LIGHT_FALLOFF dispatcher)
- Confidence: high
- Evidence: Thin Mirror-local wrapper selecting CalculateLightFalloffDiffuse vs CalculateLightDiffuse. Only Mirror defines it.
- Name: CalculateMirrorDiffuseLighting (Mirror-local)
- Operation Class: FAMILY_SHARED
- Signature: float3 CalculateMirrorDiffuseLighting(float3 normal, float3 worldPos) [LIGHT_FALLOFF]

## 30. /includes/5/operations/3

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lit:2081 (+2094 box-project); WaterLit:349 (+362 box-project); ParametricBoxTransparent:245-246 via SampleReflectionProbePair path (uses reflect() instead — see Reflection entry)
- Confidence: high
- Evidence: normalize(worldPosition-camera) reflected about normal; camera from GetStereoAwareCameraPosition. Lit/WaterLit call verbatim.
- Name: CalculateViewReflectionDirection / CalculateSpecularReflectionDirection / CalculateSpecularLobeFactor / CalculateLightSpecularLobe / CalculateLightSpecular / CalculateLightFalloffDiffuse / CalculateLightFalloffSpecularLobe / CalculateLightFalloffSpecular
- Operation Class: EXACT_SHARED
- Signature: See file: lobe factor saturate(1-dot(diff,diff)*scale*0.5)^4; smoothness^4*500 specular scale; 5-light loops in plain and falloff flavors

## 31. /includes/6/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Reflection.hlsl internal (pair decode); Lit:2099; ParametricBoxTransparent:245
- Confidence: high
- Evidence: Per-channel low=min(ch,0.5)/high=max(ch-0.5,0)*bakeId.w; decoded=low*bakeId.rgb+high*high; pair sums 6 channels then saturate(*2)*intensity. Verbatim at both call sites.
- Name: DecodeReflectionProbeChannel / DecodeReflectionProbePair / SampleReflectionProbePairLod / SampleReflectionProbePair
- Operation Class: EXACT_SHARED
- Signature: DecodeReflectionProbeChannel(float channel, float4 lightBakeId); DecodeReflectionProbePair(probe1, probe2, bakeIdA..F, intensity); SampleReflectionProbePairLod(reflectionDirection, reflectionLod, cube1, cube2, bakeIdA..F, intensity); SampleReflectionProbePair(reflectionDirection, smoothness, ...) with LOD=roughness*(1.7-0.7*roughness)*6

## 32. /includes/6/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lit:2094 (custom _ReflectionProbeBounds* + offsets); WaterLit:362 (unity_SpecCube0_*Box + w>0 gate + offsets)
- Confidence: high
- Evidence: Sign-selected bounds intersection, min-factor distance, direction*dist+worldPos-probePosition. Identical math; bounds/offset sources differ by family (custom uniforms vs engine SpecCube0). WaterLit adds a probe-present (w>0) gate Lit lacks.
- Name: BoxProjectReflectionDirection
- Operation Class: EXACT_SHARED
- Signature: inline float3 BoxProjectReflectionDirection(float3 reflectionDirection, float3 worldPosition, float3 boundsMin, float3 boundsMax, float3 probePosition)

## 33. /includes/7/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lit:1016 (ApplyParallax); Lit:1116 via ResolveTime wrapper; Particles:777,816,866,977 (all via UNITY_ACCESS_INSTANCED_PROP(Props,_TimeOffset))
- Confidence: high
- Evidence: (t/20,t,2t,3t) convention + GetTimeOffsetVector(offset)=(offset*0.05,offset,offset*2,offset*3); FREEZE->offset alone, SONG_TIME->_SongTime+offset, else _Time+_TimeHelperOffset+offset. Both consumers pass per-instance _TimeOffset and honor the same KeywordEnum.
- Name: GetTimeOffsetVector / GetTime
- Operation Class: EXACT_SHARED
- Signature: inline float4 GetTimeOffsetVector(float offset); inline float4 GetTime(float offset) [_CUSTOM_TIME_FREEZE|_CUSTOM_TIME_SONG_TIME]

## 34. /includes/8/operations/0

- Classification: `FAMILY_SHARED`
- Source: includes.json
- Call Sites: ParametricBoxFakeGlow:160,208; ParametricBoxTransparent:225; ParametricSliceBillboard:194,273
- Confidence: high (alias identity); UNVERIFIED (reason for alias existence)
- Evidence: return GetStereoAwareCameraPosition(). Used by 3/4 parametric shaders; Opaque bypasses. No added logic.
- Name: GetParametricCameraPosition
- Operation Class: FAMILY_SHARED
- Signature: inline float3 GetParametricCameraPosition()

## 35. /includes/8/operations/1

- Classification: `FAMILY_SHARED`
- Source: includes.json
- Call Sites: ParametricBoxFakeGlow:188; ParametricBoxOpaque:133; ParametricBoxTransparent:221; ParametricSliceBillboard:184
- Confidence: high
- Evidence: saturate((worldY*scale+offset-(globalHeight+globalStartY))/max(globalHeight,1e-5)) then h*h*(3-2h). Verbatim across all four; FakeGlow/Transparent feed _CustomFogHeightFogHeight/StartY as globalHeight/StartY.
- Name: CalculateParametricHeightRamp
- Operation Class: FAMILY_SHARED
- Signature: inline float CalculateParametricHeightRamp(float worldY, float heightScale, float heightOffset, float globalHeight, float globalStartY)

## 36. /includes/8/operations/2

- Classification: `FAMILY_SHARED`
- Source: includes.json
- Call Sites: ParametricBoxFakeGlow:209; ParametricBoxOpaque:144,147; ParametricBoxTransparent:230; ParametricSliceBillboard:193
- Confidence: high for family sharing; SEMANTICALLY_SIMILAR_NOT_SAFE vs Fog (see blockers)
- Evidence: rcp(max(distSq-start,0)*(scale/max(alphaDivisor,1))-offset,0)*attenuation+1). Transmission contract (1=clear) inverts Fog's factor contract (1=fogged); alphaDivisor has no Fog equivalent.
- Name: CalculateParametricDistanceTransmission
- Operation Class: FAMILY_SHARED
- Signature: inline float CalculateParametricDistanceTransmission(float3 worldPosition, float3 cameraPosition, float fogStartOffset, float fogScale, float alphaDivisor, float fogOffset, float fogAttenuation)

## 37. /includes/8/operations/3

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: includes.json
- Call Sites: ParametricBoxTransparent:208; ParametricSliceBillboard:333
- Confidence: high
- Evidence: Warp (zoom/skew perspective divide) + scroll*time+position scaled; WORLD_NOISE_WARP selects warp arg 1.0/0.0; WORLD_SPACE_FADE multiplies world-Y fade. Only the two noise-consuming parametric shaders call it.
- Name: WarpParametricNoisePosition / CalculateParametricNoiseUv / CalculateParametricWorldFade / SampleParametricWorldNoise
- Operation Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Signature: Warp(noisePosition, zoomStrength, skewStrength); NoiseUv(worldPosition, scrolling, timeValue, scale, zoom, skew, applyWarp); WorldFade(worldY, fadePosition, fadeSlope); SampleWorldNoise(worldPosition, sampler3D noiseTexture, scrolling, timeValue, scale, intensityOffset, intensityScale, fadePosition, fadeSlope, warpZoom, warpSkew) [WORLD_NOISE_WARP|WORLD_SPACE_FADE]

## 38. /includes/9/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: BloomfogSkybox:91; CloudsOpaque:258; Lit:2685 + 2687(DEFECT_CANDIDATE); Mirror:273; ParametricBoxOpaque:164; Particles:1427,1452,1524,1560,1570; Spectrogram:186; WaterLit:414
- Confidence: high (except Lit:2687 — see blockers)
- Evidence: tex2D(blueNoise, screenPos.xy/screenPos.ww).r-0.5 added as rgb += noise*(1/255). Verbatim across 9 consumers; header comment correctly scopes it (fog lives in Fog.hlsl, Lit owns distance-darkening/rim).
- Name: ApplyNoiseDither
- Operation Class: EXACT_SHARED
- Signature: inline float4 ApplyNoiseDither(float4 result, float4 noiseScreenPosition, sampler2D globalBlueNoiseTex)

## 39. /includes/9/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: BloomfogSkybox:71; Lit:1725; Mirror:182; ParametricBoxOpaque:115; ParametricSliceBillboard:289; Particles:959; Spectrogram:134; WaterLit:297
- Confidence: high
- Evidence: screenPos.xy*=noiseScale; += clip.w*randomValue + objectTranslation; .zw = clip.zw. Verbatim constructor used by all 8 dithered vertex stages.
- Name: BuildNoiseScreenPosition
- Operation Class: EXACT_SHARED
- Signature: inline float4 BuildNoiseScreenPosition(float4 screenPosition, float4 clipPosition, float2 noiseScale, float randomValue, float2 objectTranslation)

## 40. /includes/10/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Lit:1866; WaterLit:345
- Confidence: high
- Evidence: worldPosition/normalWS/uv0/uv1/baseColor/metallic/smoothness assigned; secondaryUvTiling=1, secondaryUvOffset=0, mpm=1, occlusion=occlusionDetail=1, lightmapUv=0. Both call sites match positionally.
- Name: InitializeSurfaceData (+ SurfaceData/LightingData/EmissionData structs)
- Operation Class: EXACT_SHARED
- Signature: inline SurfaceData InitializeSurfaceData(float3 worldPosition, float3 normalWS, float2 uv0, float2 uv1, float4 baseColor, float metallic, float smoothness)

## 41. /includes/11/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: BasicGradient:101-188 (Quadratic/Cubic/Quartic/Quintic/Sinusoidal/Exponential/Circular/Elastic/Back/Bounce × In/Out/InOut + Step)
- Confidence: high
- Evidence: Sole consumer calls the full set under its own easing-type dispatch. Functions are pure (input k only), no uniforms, no keywords — output equality is structural.
- Name: Step + Quadratic/Cubic/Quartic/Quintic/Sinusoidal/Exponential/Circular/Elastic/Back/Bounce _In/_Out/_InOut (31 functions)
- Operation Class: EXACT_SHARED
- Signature: inline float <Family>_<In|Out|InOut>(float k) (Elastic/Exponential/Bounce guard k<=0||k>=1)

## 42. /includes/12/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: ObstacleDistortion:203; ObstacleOutline:161; ParametricBoxFakeGlow:196
- Confidence: high
- Evidence: (worldPosition-objectOrigin+textureOffset)*textureScale verbatim at all three sites.
- Name: CalculateObjectSpaceCutoutPosition
- Operation Class: EXACT_SHARED
- Signature: inline float3 CalculateObjectSpaceCutoutPosition(float3 worldPosition, float3 objectOrigin, float3 textureOffset, float textureScale)

## 43. /includes/12/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: ObstacleDistortion:206; ObstacleOutline:164; ParametricBoxFakeGlow:198
- Confidence: high
- Evidence: clip(noiseSample-1.1*cutout+0.1) verbatim at all three sites.
- Name: ApplyCutoutNoise
- Operation Class: EXACT_SHARED
- Signature: inline void ApplyCutoutNoise(float noiseSample, float cutout)

## 44. /includes/13/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: CloudsLitTransparent:228; Note:198 (direction input — see mismatches); ObjectShared internal (CalculateRotatedObjectPosition)
- Confidence: high
- Evidence: sincos Y-rotation verbatim; direction use is algebraically valid for pure rotation.
- Name: RotateObjectPositionY
- Operation Class: EXACT_SHARED
- Signature: inline float3 RotateObjectPositionY(float3 position, float angleRadians)

## 45. /includes/13/operations/1

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Arc:123; Note:239
- Confidence: high
- Evidence: Rotate(worldPosition-offset)+offset with .w = objectTime+0.001-songTime. Both call sites consume the packed .w under family semantics.
- Name: CalculateRotatedObjectPosition
- Operation Class: EXACT_SHARED
- Signature: inline float4 CalculateRotatedObjectPosition(float3 worldPosition, float3 offset, float angleRadians, float objectTime, float songTime)

## 46. /includes/14/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: Particles:796,1073 (_SpectrogramData[...] indexing); Spectrogram:125 (uint index = ...(i.uv.x)); SpectrogramUnlit:109 (same)
- Confidence: high
- Evidence: uint(max(horizontalUv*63.0, 0.0)) verbatim; all consumers feed a horizontal UV and index 64-wide data.
- Name: CalculateSpectrogramIndex
- Operation Class: EXACT_SHARED
- Signature: inline uint CalculateSpectrogramIndex(float horizontalUv)

## 47. /includes/15/operations/0

- Classification: `EXACT_SHARED`
- Source: includes.json
- Call Sites: SkyGradient:76; CloudsLitTransparent:294; CloudsOpaque:241; Lit:2225,2586; Mirror:254; BasicGradient:227; Event:82; Note:370; ObstacleSimple:111; Spectrogram:172; Unlit:127; WaterLit:398; Bloom.shader:128 + via BloomApplyKneeAndAces:137
- Confidence: high
- Evidence: Narkowicz ACES fit saturate(x*(2.51x+0.03)/(x*(2.43x+0.59)+0.14)) verbatim; 12 direct + 1 transitive consumer families. Per-call-site gating/placement differs by design (see blockers).
- Name: ApplyAcesTonemapping
- Operation Class: EXACT_SHARED
- Signature: inline float4 ApplyAcesTonemapping(float4 col)

## 48. /include_contracts/0

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:849
- Guard: #ifndef CHROMAPPER_DATA_INCLUDED / #define CHROMAPPER_DATA_INCLUDED (Data.hlsl:1-2)
- Provides: struct SurfaceData{worldPosition,normalWS,uv0,uv1,secondaryUvTiling,secondaryUvOffset,baseColor,metallic,smoothness,mpm,occlusion,occlusionDetail,lightmapUv}; struct LightingData{directDiffuse,directSpecular,reflection,ambient}; struct EmissionData{color,bloomAlpha}; InitializeSurfaceData(worldPos,normalWS,uv0,uv1,baseColor,metallic,smoothness)->SurfaceData (defaults tiling=1,offset=0,mpm=1,occlusion=1,detail=1,lightmap=0)
- Sharing Class: EXACT_SHARED
- Uniforms Owned: none (pure types + initializer; no globals)

## 49. /include_contracts/1

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:850; CloudsOpaque.shader:112; Glowing.shader:75; ParametricBoxOpaque.shader:69; ParametricSliceBillboard.shader:126; Rain.shader:103; Unlit.shader:53
- Guard: #ifndef CHROMAPPER_CAMERA_INCLUDED (Camera.hlsl:1-2)
- Provides: float2 _StereoCameraEyeOffsets; GetStereoAwareCameraPosition()->float3 (UNITY_SINGLE_PASS_STEREO||STEREO_INSTANCING_ON||STEREO_MULTIVIEW_ON ? unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex] : _WorldSpaceCameraPos); ComputeScreenPosCustom(float4 pos)->float4 (ComputeNonStereoScreenPos + eye-offset + UV_STARTS_AT_TOP flip under stereo)
- Sharing Class: EXACT_SHARED
- Uniforms Owned: _StereoCameraEyeOffsets (float2) + engine unity_StereoWorldSpaceCameraPos/unity_StereoEyeIndex/_WorldSpaceCameraPos

## 50. /include_contracts/2

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: lit.json
- Consumers: Lit.shader:851; Particles.shader:388
- Guard: #ifndef CHROMAPPER_TIME_INCLUDED (Time.hlsl:1-2)
- Provides: uniform float4 _SongTime; uniform float4 _TimeHelperOffset; GetTimeOffsetVector(float offset)->float4(offset*0.05,offset,offset*2,offset*3); GetTime(float offset)->float4 (FREEZE=>offsetVec; SONG_TIME=>_SongTime+offsetVec; else _Time+_TimeHelperOffset+offsetVec)
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Uniforms Owned: _SongTime, _TimeHelperOffset (+ engine _Time)

## 51. /include_contracts/3

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:852; CloudsLitTransparent.shader:172; CloudsOpaque.shader:114; Mirror.shader:105; Spectrogram.shader:76
- Guard: #ifndef CHROMAPPER_LIGHTING_INCLUDED (Lighting.hlsl:1-2); internally #include Data.hlsl + Camera.hlsl
- Provides: uniform _DirectionalLightPositions[5],_DirectionalLightRadii[5],_DirectionalLightDirections[5],_DirectionalLightColors[5],_PrivatePointLightPosition,_PrivatePointLightIntensity; CalculateLightFalloff(pos,idx)->float 1/(dot(d,d)/r^2*25+1); CalculateDirectionalDiffuseTerm(ndot,both)->float; CalculateLightDiffuseAccumulation(n,both)->float3 (order 1,0,2,3,4); CalculateLightDiffuse; CalculateViewReflectionDirection(pos,n)->float3; CalculateSpecularLobeFactor(ld,rd,scale)->float saturate(1-dot(d,d)*scale*0.5)^4; CalculateLightSpecularLobe; CalculateSpecularReflectionDirection(pos,n,smoothness)->(rd,scale=smooth^4*500); CalculateLightSpecular; CalculateLightFalloffDiffuse; CalculateLightFalloffSpecularLobe; CalculateLightFalloffSpecular
- Sharing Class: EXACT_SHARED
- Uniforms Owned: 5-light arrays + private-point position/intensity (color passed via Lit Props/uniform route)

## 52. /include_contracts/4

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:853; ParametricBoxTransparent.shader:121; WaterLit.shader:210
- Guard: #ifndef CHROMAPPER_REFLECTION_INCLUDED (Reflection.hlsl:1-2); #include Data+Lighting; comment: per-material inputs passed as args, library reads no consumer uniforms
- Provides: DecodeReflectionProbeChannel(float,float4)->float3; DecodeReflectionProbePair(probe1,probe2,6x float4 bakeId,intensity)->float3 saturate(decoded*2)*intensity; SampleReflectionProbePairLod(dir,lod,2x samplerCUBE,6x bakeId,intensity)->float3; SampleReflectionProbePair(dir,smoothness,...)->float3 with roughness=(1-smooth), lod=rough*(1.7-0.7*rough)*6; BoxProjectReflectionDirection(rd,worldPos,boundsMin/Max,probePos)->float3
- Sharing Class: EXACT_SHARED
- Uniforms Owned: none inside library; consumer declares samplers _ReflectionProbeTexture1/2, bakeIds A-F, intensity, bounds/position

## 53. /include_contracts/5

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:854; Unlit.shader:55; Glowing.shader:77; ParametricBoxOpaque.shader:71; ParametricBoxTransparent.shader:119; ParametricSliceBillboard.shader:128; ParametricBoxFakeGlow.shader:91; Particles.shader:387
- Guard: #ifndef CHROMAPPER_BLOOM_INCLUDED (Bloom.hlsl:1-2)
- Provides: float _BaseColorBoost,_BaseColorBoostThreshold (camera-global, not material Props); CalculateWhiteBoost(bloomValue,whiteboostMult,baseBoost,baseThresh)->float (bv*wb)^2*bb-bt; CalculateBloomComposition(rgb,premultAlpha,bloomValue,wb,bb,bt)->float3 saturate(rgb*premultAlpha+whiteBoost); CalculateBloomPostComposition(rgb,alpha,bloomMult)->float4(rgb*alpha,alpha*bloomMult)
- Sharing Class: EXACT_SHARED
- Uniforms Owned: _BaseColorBoost/_BaseColorBoostThreshold globals

## 54. /include_contracts/6

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:855; CloudsOpaque.shader:116; Mirror.shader:107; ParametricBoxOpaque.shader:72; ParametricSliceBillboard.shader:130; Particles.shader:390; Spectrogram.shader:78; WaterLit.shader:212
- Guard: #ifndef CHROMAPPER_POST_PROCESS_INCLUDED (PostProcess.hlsl:1-2)
- Provides: ApplyNoiseDither(float4 result,float4 noiseScreenPos,sampler2D blueNoise)->float4 (uv=noiseSP.xy/noiseSP.ww; rgb+= (tex.r-0.5)*(1/255)); BuildNoiseScreenPosition(screenPos,clipPos,noiseScale,randomValue,objectTranslation)->float4 (xy*=scale; xy+=clip.w*rand+objXZ; zw=clip.zw)
- Sharing Class: EXACT_SHARED
- Uniforms Owned: none inside; consumer declares _GlobalBlueNoiseTex/_GlobalBlueNoiseParams/_GlobalRandomValue

## 55. /include_contracts/7

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: lit.json
- Consumers: Lit.shader:856; 11+ other shaders listed in consumers_seen_exact.Fog.hlsl
- Guard: #ifndef CHROMAPPER_FOG_INCLUDED (Fog.hlsl:1-2) + #ifndef CUSTOM_FOG_ATTENUATION_NAME/OFFSET_NAME/HEIGHT_START_Y/HEIGHT (defaults _CustomFogAttenuation/_CustomFogOffset/_CustomFogHeightFogStartY/_CustomFogHeightFogHeight)
- Provides: float CUSTOM_FOG_ATTENUATION/OFFSET/HEIGHT_START_Y/HEIGHT; float2 _CustomFogTextureToScreenRatio; sampler2D _BloomPrePassTexture; distanceSquared(pos)->float dot(pos-stereoCam); CalculateCustomFogFactor(distSq,startOffset,scale)->float (-1/(max(max(dSq-startOff,0)*scale-fogOff,0)*atten+1)+1); CalculateCustomHeightFogFactor(worldPos,heightOff,heightScale)->smoothstep-ish; CalculateHeightFogFactor(exactInput)->1-x^2*(3-2x); ApplyColorFog(result,worldPos,cFogMult,cFogMax,cFogHiMult,cFogInfl,fogHScale,fogHOff)->float4 (#if FOG_COLOR_HIGHLIGHT hi=min(0.1*hiMult*(1+min(0.0001*mult,max)),max); BLOOM_FOG&&FOG=>direct colorFogResult else height-weighted); SampleBloomPrePass(screenPos)->float4 (BLOOM_FOG ? tex(_BloomPrePassTexture,(uv-0.5)*ratio+0.5),0 : 0); BlendFogColor(col,bloomfogCol) (_FOGTYPE_ALPHA=>a; _FOGTYPE_COLOR=>rgb; else bloomfogCol); ApplyBloomFogCalculatedFactor; ApplyBloomFog(col,screen,world,fogStart,fogScale); ApplyBloomHeightFogCalculatedFactor; ApplyBloomHeightFog
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Uniforms Owned: _CustomFog* globals + _CustomFogTextureToScreenRatio + _BloomPrePassTexture

## 56. /include_contracts/8

- Classification: `EXACT_SHARED`
- Source: lit.json
- Consumers: Lit.shader:857; CloudsLitTransparent.shader:175; CloudsOpaque.shader:115; Mirror.shader:106; Spectrogram.shader:77; Unlit.shader:56; WaterLit.shader:211
- Guard: #ifndef CHROMAPPER_TONEMAPPING_INCLUDED (Tonemapping.hlsl:4-5)
- Provides: ApplyAcesTonemapping(float4 col)->float4 saturate(col*(2.51*col+0.03)/(col*(2.43*col+0.59)+0.14)) [a passthrough]
- Sharing Class: EXACT_SHARED
- Uniforms Owned: none; constants a=2.51 b=0.03 c=2.43 d=0.59 e=0.14

## 57. /regions/0

- Classification: `EXACT_SHARED`
- Source: lit.json
- Alpha Rgb Order: No transparency: defaults One/Zero RGB and Zero/Zero bloom-alpha => opaque; albedo.a is bloom (glow) channel consumed by bloom prepass/post, not transparency. Bloom-alpha writers: flipbook/plain/gradient/whiteboost emission, vertex emission, _RIMLIGHT_ADDITIVE overwrite, _RIMLIGHT_LERP overwrite via ApplyRimLight. Occlusion-after-emission also scales additive-rim alpha. Final return carries composed bloom alpha through fog/dither (fog/dither preserve or blend per BlendFogColor) to SV_Target.
- Confidence: high (source order + LIT_REAUDIT proven matches for POST_BLOOM mapping, instanced brightness, color/bloom paths)
- Coordinate Spaces: blend state is not spatial; screen-space bloom prepass sampling uses ComputeScreenPosCustom space when BLOOM_FOG
- Evidence: Lit.shader:311-314 props, 334 Blend line, 2167-2700 emission/rim/fog/dither/dissolve order; Bloom.hlsl:CalculateBloomComposition/PostComposition; LIT_REAUDIT.md Proven matches POST_BLOOM
- Guards: Blend [_BlendModeSrc][_BlendModeDst],[_BlendModeSrcA][_BlendModeDstA]; Tags RenderType=Opaque; Cull/ZTest/ZWrite/Stencil parameterized (_CullMode=2,_ZWrite=1,_ZTest=4,Stencil 0/8/0); POST_BLOOM global multi_compile_fragment selects CalculateBloomComposition vs CalculateBloomPostComposition white-boost elision
- Inputs Varyings: n/a (fixed-function state + SV_Target float4 albedo)
- Operation Signatures: state: Blend(srcRGB,dstRGB,srcA,dstA)=([_BlendModeSrc],[_BlendModeDst],[_BlendModeSrcA],[_BlendModeDstA]) defaults (1,0,0,0); bloom: float3 CalculateBloomComposition(float3 rgb,float premult,float bloom,float wb,float bb,float bt); post: float4 CalculateBloomPostComposition(float3 rgb,float alpha,float mult)
- Precision Constants: BlendMode enums; bloom uses saturate; whiteboost square; bloomAlpha factor 3.5 (plain/whiteboost) visible at Lit 1150,2262-2270,2514
- Sampling Lod: n/a except SampleBloomPrePass tex2D(_BloomPrePassTexture, (screenUV-0.5)*ratio+0.5) under BLOOM_FOG
- Sharing Class: EXACT_SHARED
- Stage: output-merger / alpha-bloom-alpha contract
- Time: n/a
- Title: R00 render state and bloom-alpha contract
- Uniforms Instanced: _BlendModeSrc/Dst/SrcA/DstA,_CullMode,_ZWrite,_ZTest,_StencilRefValue/_StencilComp/_StencilPass (material); _BaseColorBoost/_BaseColorBoostThreshold (camera-global, not Props)

## 58. /regions/10

- Classification: `EXACT_SHARED`
- Source: lit.json
- Alpha Rgb Order: directDiffuse+directSpecular added into albedo rgb with metallic terms; groundFade scales directBaseColor pre-light and specular post-light; specular multiplied by occlusion when OCCLUSION (before emission ordering splits occlusion rgb timing)
- Confidence: high (LIT_REAUDIT Proven: direct/five-light/spec/falloff/private/lightmap match; 0.96 vs 1.0 metallic diffuse scale branch observed)
- Coordinate Spaces: world (positions/normals for NdotL, falloff distance, private-point vector; local option via unity_ObjectToWorld for POINT_LIGHT_IS_LOCAL); lightmap UV space
- Evidence: Lit.shader:1906-2003 lighting block; Lighting.hlsl helpers; LIT_REAUDIT.md Proven matches
- Guards: DIFFUSE (+BOTH_SIDES_DIFFUSE fragment, LIGHT_FALLOFF fragment); PRIVATE_POINT_LIGHT (+POINT_LIGHT_IS_LOCAL fragment, INSTANCED_PRIVATE_POINT_LIGHT selects uniform vs instanced color); SPECULAR; LIGHTMAP (vertex payload + fragment decode); GROUND_FADE (fragment); _PROBE_CALCULATION_PRECISE (ambient saturation path)
- Inputs Varyings: composableSurface.worldPosition/normalWS/baseColor/metallic/smoothness/occlusion
- Operation Signatures: ambient: max(_AmbientMult*instNominalRGB, _AmbientMin); PRECISE? base*ambient*((sat+1)*(1-metal)) : base*ambient (sat=(max-min)/max); diffuse_lights: DIFFUSE?(FALLOFF?CalculateLightFalloffDiffuse(pos,N):CalculateLightDiffuse(N,_BothSides)) : 0 (+private: LOCAL?mul(obj,privPos):privPos; vec=lightPos-pos; d2=max(dot,1e-5); dir=vec/sqrt(d2); d=BOTH?abs(dot):max(dot,0); +=d*privColor*privInt/d2); direct: diffuseLights*directBase (directBase=base*(GROUND_FADE?1-saturate(-y*scale+off):1)); DIFFUSE&&SPECULAR? direct*(0.96*(1-metal)) : direct*(1-metal); spec: SPECULAR? color=DIFFUSE?0.04+metal*(direct-0.04):0.04+metal*(base-0.04); lights=FALLOFF?FalloffSpec(pos,N,smooth):Spec(pos,N,smooth); *=color*(_SpecIntensity*groundFade) (*occlusion if OCCLUSION); lightmap: (l1.r*A+l1.g*B+l1.b*C+l2.r*D+l2.g*E+l2.b*F)*4.594793*(1-metal)*base added to directDiffuse; lib: float3 CalculateLightDiffuse(float3 N,float both); float3 CalculateLightSpecular(float3 pos,float3 N,float smooth); falloff variants; lobe saturate(1-dot(d,d)*scale*0.5)^4, scale=smooth^4*500, order 1,0,2,3,4
- Precision Constants: falloff 1/(d2/r2*25+1); private d2 floor 1e-5; lightmap gain 4.594793; diffuse metallic 0.96 vs 1.0; specular base 0.04
- Sampling Lod: lightmap tex2D(_LightMap1/2, lightmapUv) fragment; spec/reflection LOD in R11
- Sharing Class: EXACT_SHARED
- Stage: fragment direct lighting
- Time: n/a
- Title: R10 direct lighting, falloff, private point, lightmap, ground fade
- Uniforms Instanced: _NominalDiffuseLevel (instanced),_AmbientMultiplier/_MinimalValue,_BothSidesDiffuseMultiplier,_SpecularIntensity,_PrivatePointLightPosition/Intensity + _PrivatePointLightColor (uniform iff USE_UNIFORM_PRIVATE_POINT_COLOR else instanced),_GroundFadeScale/Offset,_LightMap1/2+_LightmapLightBakeIdA-F,_DirectionalLight* (library globals)

## 59. /regions/11

- Classification: `EXACT_SHARED`
- Source: lit.json
- Alpha Rgb Order: reflection written to composableLighting.reflection rgb (added to albedo rgb at R12 head); rim-dim darkening + smoothness reduction + groundFade multiply reflection; MULTIPLY_REFLECTIONS tints by base/metallic; alpha untouched here
- Confidence: high for decode/LOD/box/static/precise/multiply order (LIT_REAUDIT Proven); high for fragment-distance RIM_DIM (fix #1 vertex passes facing, fragment computes distance); medium for antiflicker gradient blend exactness (payload+centroid proven, numeric weight source-observed)
- Coordinate Spaces: world (view dir = normalize(worldPos-stereoCam); reflection dir view-2dot*N or STATIC pos+N; box projection in world bounds); cubemap direction+LOD space
- Evidence: Lit.shader:1700-1728 vertex rim/reflection varyings; 2005-2163 fragment reflection; 906-908 + 1298 rim helper; Reflection.hlsl; LIT_REAUDIT.md fixes #1 + Proven reflection
- Guards: REFLECTION_TEXTURE (+MULTIPLY_REFLECTIONS fragment, RIM_DIM, INVERT_RIM_DIM vertex-switch for rimDim only); else RIM_DIM + REFLECTION_PROBE (+BOX_PROJECTION(+OFFSET) fragment, REFLECTION_STATIC, _PROBE_CALCULATION_PRECISE, MULTIPLY_REFLECTIONS, SPECULAR_ANTIFLICKER); PRECISE vs fast path
- Inputs Varyings: worldPos/worldNormal (or antiflicker-substituted); i.reflectionTextureDirection/RimFactor or i.rimDim; smoothness/metallic/base
- Operation Signatures: rimDim_dist: float CalculateLitReflectionTextureRimDim(float3 pos,float facing,float distOff,float distScale,float rimScale)=(max(len(pos-cam)-distOff,0)*distScale+rimScale)*facing; tex_path: smooth(-=rimDim*smoothness if RIM_DIM); rough=1-smooth; lod=rough*(1.7-0.7*rough)*6; refl=texCUBElod(_EnvironmentReflectionCube,float4(dir,lod)).rgb*_TexIntensity; MULTIPLY?*=1+metal*(base-1); *=2*(metal*0.8+0.2); *=smooth (*=1-rimDim*darkening if RIM_DIM); probe_fast_or_precise: smooth(-=rimDim*smoothness); ANTIFLICKER? camDist=len(pos-cam); w=saturate((distOff-camDist)*distScale)*strength; grad=min(max(dot(dx,dx),dot(dy,dy)),1); filt=min(1-pow(grad,0.333),smooth); smooth+=w*(filt-smooth); dir=STATIC?pos+N:CalculateViewReflectionDirection(pos,N); BOX?dir=BoxProjectReflectionDirection(dir,pos,boundsMin/Max,probePos) (offsets if OFFSET); refl=SampleReflectionProbePair(dir,smooth,probe1/2,bakeA-F,intensity); PRECISE? grayscale/saturation/metallic chain : fast multiply chain (see body); lib: float3 DecodeReflectionProbeChannel(float,float4); float3 DecodeReflectionProbePair(float3,float3,6x float4,float); float3 SampleReflectionProbePair(float3 dir,float smooth,samplerCUBE x2,6x float4,float); float3 SampleReflectionProbePairLod(...float lod...); float3 BoxProjectReflectionDirection(float3,float3,float3,float3,float3)
- Precision Constants: LOD rough*(1.7-0.7*rough)*6; rim distance max(...,0); antiflicker pow(grad,0.333), clamp grad 1.0; precise grayscale 0.33, metal*metal*2.5+1, coloredMult, whiteOff, 0.1 white curve, saturation guard max(sat,0.95); fast 2*(metal*0.8+0.2)
- Sampling Lod: texCUBElod explicit LOD (roughness-mapped); probe pair LOD shared helper; WaterLit duplicates box+LOD inline (FAMILY_SHARED, not via helper for LOD calc)
- Sharing Class: EXACT_SHARED
- Stage: fragment reflection + rim-dim
- Time: n/a (camera-distance terms are spatial, not temporal)
- Title: R11 reflection texture/probe, box projection, antiflicker, rim-dim
- Uniforms Instanced: _EnvironmentReflectionCube/_ReflectionTexIntensity,_ReflectionProbeTexture1/2,_LightProbeLightBakeIdA-F,_ReflectionProbePosition/BoundsMin/Max/Intensity/Grayscale,_ColoredMetalMultiplier,_WhiteOffset,box offsets,_RimScale/Offset/DistanceOffset/Scale/Smoothness/Darkening,_AntiflickerStrength/Scale/Offset

## 60. /regions/16

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: lit.json
- Alpha Rgb Order: occlusion-after multiplies rgb (and additive-rim alpha); ACES-after applies to rgb+a passthrough; highlight adds rgb pulse; COLOR_BY_FOG (non-bloomfog, non-grid/legacy hologram) replaces via ApplyColorFog height-weighted blend
- Confidence: high for occlusion-before/after split + ACES approach gate (source-observed; audit ACES lifecycle owned by PerCameraShaderSetupController, BLOOM_FOG selects ACES only for bounded passes); medium for highlight pulse constants
- Coordinate Spaces: world (highlight pulse uses worldPos.x*0.2+worldPos.y + time.w*0.15); fog height uses worldPos.y*scale+offset
- Evidence: Lit.shader:2585-2610 occlusion-after/ACES-after/highlight/COLOR_BY_FOG; Fog.hlsl ApplyColorFog; LIT_REAUDIT.md bloom-fog follow-up (COLOR_BY_FOG on sampled bloom prepass color in multiplier/highlight/clamp/influence/fog order)
- Guards: OCCLUSION&&!OCCLUSION_BEFORE_EMISSION; ACES_TONE_MAPPING&&!_ACES_APPROACH_BEFORE_EMISSIVE; HIGHLIGHT_SELECTION; COLOR_BY_FOG&&!(BLOOM_FOG&&FOG)&&!_HOLOGRAM_GRID&&!_HOLOGRAM_LEGACY (note: SCANLINE not excluded)
- Inputs Varyings: i.highlightSelection; worldPos; composableTime.w
- Operation Signatures: occl_after: rgb*=occlusion; ADDITIVE? a*=occlusion; aces_after: albedo=ApplyAcesTonemapping(albedo) (saturate(col*(2.51col+0.03)/(col*(2.43col+0.59)+0.14))); highlight: p=frac(time.w*0.15+world.x*0.2+world.y); p=max(1-p*5,0); p=p^2*(3-2p); p*=saturate(100-100p); p=saturate(0.4*p^2*sel); rgb+=p; colorfog_plain: albedo=ApplyColorFog(albedo,world,_CfogMult,_CfogMax,_CfogHiMult,_CfogInfl,_FogHScale,_FogHOff) (FOG_COLOR_HIGHLIGHT gates highlight term; BLOOM_FOG&&FOG takes direct colorFogResult else height-weighted)
- Precision Constants: highlight 0.15,0.2,5.0,3-2p,100,0.4; fog color 0.0001*mult, 0.1*hiMult*(1+resolved)
- Sampling Lod: n/a
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Stage: fragment post-emission occlusion/ACES/highlight/plain color-fog
- Time: highlight time.w (GetTime().w => Standard _Time.w+offset*3 / Song / Freeze offset*3)
- Title: R16 post-emission occlusion, ACES-after, highlight, plain color-fog
- Uniforms Instanced: _ColorFogMultiplier/Max/Influence/_HighlightMultiplier (FOG_COLOR_HIGHLIGHT),_FogHeightScale/Offset (plain color-fog height)

## 61. /regions/17

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: lit.json
- Alpha Rgb Order: bloomfog blends rgb+a toward prepass via fogFactor (BlendFogColor selects a vs rgb vs full by _FOGTYPE_*; Lit defines none => full bloomfogCol overwrite). Height variant uses (heightFog*-fog+1) weighting. Plain FOG+HEIGHT (non-colorfog, non-grid/legacy) lerps rgb toward grey 0.1 by height factor (alpha preserved: float4(0.1,0.1,0.1,0)-result weighting keeps a). COLOR_BY_FOG bloom variant blends fogSource->fogTarget by 1-height*(1-fog).
- Confidence: high for fog order + height-curve + soften math (audit: fog order, RIM_DIM fragment distance, Grid exp2 mapping proven; COLOR_BY_FOG-on-prepass multiplier/highlight/clamp/influence/order restored)
- Coordinate Spaces: world + camera distance (distanceSquared via stereo camera; soften uses length(world-cam)); screen (prepass UV from screenPos); height from worldPos.y*scale+offset (-global height start/height, /height)
- Evidence: Lit.shader:2612-2681 terminal fog; Fog.hlsl CalculateCustomFogFactor/Height/ApplyBloomFog/Height/ApplyColorFog/SampleBloomPrePass; LIT_REAUDIT.md Binary-proven fixes (bloom-fog Grid/Legacy + COLOR_BY_FOG order)
- Guards: BLOOM_FOG(global)&&FOG(fragment) -> ApplyBloomFog vs ApplyBloomHeightFogCalculatedFactor (HEIGHT_FOG fragment; HEIGHT_FOG_DEPTH_SOFTEN fragment selects soften heightInput); elif FOG&&HEIGHT_FOG&&!COLOR_BY_FOG&&!_HOLOGRAM_GRID&&!_HOLOGRAM_LEGACY -> ApplyHeightFogCurve (soften vs plain heightInput); COLOR_BY_FOG inside bloom-height selects prepass-colorfog branch (FOG_COLOR_HIGHLIGHT inner)
- Inputs Varyings: worldPos; i.screenPos
- Operation Signatures: fog: d2=distanceSquared(world); f=CalculateCustomFogFactor(d2,_FogStart,_FogScale) (max(max(d2-start,0)*scale-fogOff,0)*atten+1 inverted); height: hInput=SOFTEN? world.y*(hScale/(dist*soften*0.01))+hOff-dist*softenOff*0.001 : world.y*hScale+hOff; h=clamp((hInput-(H+HStart))/H,0,1); hF=h^2*(3-2h); bloomfog: ApplyBloomFog(albedo,screen,world,fogStart,fogScale)=BlendFog(f*fog(-col+prepass)+col); height: (hF*-f+1)*(-col+prepass)+col; colorfog_bloom: bloom=SampleBloomPrePass(screen).rgb; fog=min(bloom*mult,(max)); HIGHLIGHT? m=max(bloom); hi=m^4*hiMult clamped then fog=min(fog*(1+hi),max); target=min(float4(fog,0),max); src=float4(rgb*infl+fog,a); blend=1-hF*(1-f); albedo=src+blend*(target-src); plain_height: ApplyHeightFogCurve(result,exact)=exactH(=CalculateHeightFogFactor)* (float4(0.1,0.1,0.1,0)-result)+result
- Precision Constants: soften 0.01/0.001; grey 0.1; smooth h^2*(3-2h); fog attenuation/offset globals; prepass UV (uv-0.5)*ratio+0.5
- Sampling Lod: SampleBloomPrePass tex2D(_BloomPrePassTexture, customUV) under BLOOM_FOG else 0
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Stage: fragment terminal fog
- Time: n/a (distance/height spatial)
- Title: R17 terminal bloom-fog / height-fog / plain height curve + bloom color-fog
- Uniforms Instanced: _FogStartOffset/_Scale,_FogHeightScale/Offset,_FogSoften/_SoftenOffset,_CustomFogAttenuation/Offset/Height/StartY (globals),_CustomFogTextureToScreenRatio/_BloomPrePassTexture (globals),_ColorFog* (bloom variant)

## 62. /regions/18

- Classification: `EXACT_SHARED`
- Source: lit.json
- Alpha Rgb Order: dither adds (tex.r-0.5)/255 to rgb only (alpha preserved); dissolve edge lerps rgb toward _DissolveColorIntensity*instDissolveColor.rgb by dissolveFactor (alpha preserved). This terminal order (dither THEN edge lerp) is the audited fix #7.
- Confidence: high for order (fix #7) + dither helper sharing; high for dissolve-factor source (R08)
- Coordinate Spaces: screen (noiseScreenPos from BuildNoiseScreenPosition: screen.xy*noiseScale + clip.w*rand + objTrans; zw=clip.zw; uv=xy/ww) else 0.0 fallback path (still samples blue noise at 0/0 => constant offset, source-observed)
- Evidence: Lit.shader:2683-2699 terminal; PostProcess.hlsl ApplyNoiseDither/BuildNoiseScreenPosition; LIT_REAUDIT.md fix #7
- Guards: NOISE_DITHERING (USE_NOISE_SCREEN_POSITION same) with fallback ApplyNoiseDither(albedo,0.0,tex) when payload off; DISSOLVE&&DISSOLVE_COLOR
- Inputs Varyings: i.noiseScreenPos (or 0.0); dissolveFactor from R08
- Operation Signatures: dither: float4 ApplyNoiseDither(float4 r,float4 sp,sampler2D t)={uv=sp.xy/sp.ww; n=tex2D(t,uv).r-0.5; r.rgb+=n*(1/255); return r}; build: float4 BuildNoiseScreenPosition(float4 sp,float4 clip,float2 sc,float rv,float2 objT)={sp.xy*=sc; sp.xy+=clip.w*rv+objT; sp.zw=clip.zw}; edge: rgb=lerp(rgb,_DissolveColorIntensity*instDissolveColor.rgb,dissolveFactor); return albedo
- Precision Constants: 1/255; noise -0.5 center
- Sampling Lod: tex2D(_GlobalBlueNoiseTex, noiseUv) fragment
- Sharing Class: EXACT_SHARED
- Stage: fragment terminal dither + dissolve edge
- Time: _GlobalRandomValue jitters noise screen position (per-frame global, not GetTime)
- Title: R18 terminal noise dither then dissolve edge color
- Uniforms Instanced: _GlobalBlueNoiseTex/_GlobalBlueNoiseParams/_GlobalRandomValue (globals)

## 63. /strongest_candidates/0

- Classification: `EXACT_SHARED`
- Source: lit.json
- Claim: EXACT_SHARED ApplyAcesTonemapping, CalculateWhiteBoost/CalculateBloomComposition(/Post), ApplyNoiseDither/BuildNoiseScreenPosition, CalculateLight* (+falloff/specular lobe), Decode/SampleReflectionProbePair(+Lod)/BoxProjectReflectionDirection, GetStereoAwareCameraPosition/ComputeScreenPosCustom, CalculateCustomFogFactor(+Height)/SampleBloomPrePass/ApplyBloomFog(+Height)/BlendFogColor are byte-shared via ShaderLibrary includes. Strongest because identical source text is #included by Lit + 2-12 other shaders (see consumers_seen_exact) with no per-shader fork.
- Comparison Class: EXACT_SHARED
- Evidence: include-contract guards + consumer grep lists; LIT_REAUDIT Proven matches (lighting, reflection decode/LOD/box, POST_BLOOM, fog order)

## 64. /strongest_candidates/1

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: lit.json
- Claim: SHARED_WITH_COMPILE_TIME_PARAMETERS GetTime (+ResolveTime/ResolveHologramTime wrappers), ApplyColorFog (+CalculateHeightFogFactor variants), terminal bloom/height/soften selection. Same helper text, behavior selected by _CUSTOM_TIME_*/FOG_COLOR_HIGHLIGHT/BLOOM_FOG&&FOG/HEIGHT_FOG_DEPTH_SOFTEN/COLOR_BY_FOG/hologram exclusions. Particles.shader already shares GetTime; Unlit/Parametric/WaterLit share fog/ACES branches with different exclusion sets, so parameterization must be preserved.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Time.hlsl:18-26 + Lit 1016,1114,1156 wrappers; Fog.hlsl ApplyColorFog + Lit 2601,2630 branches; Particles.shader:777+ GetTime uses

## 65. /regions/1

- Classification: `FAMILY_SHARED`
- Source: particles.json
- Behavior: Transparent queue/render-type, PreviewType Plane, CanUseSpriteAtlas True. Dual blend Blend[src,dst],[srcA,dstA] + BlendOp + Cull/ZTest/ZWrite/Offset/Stencil all material-parameterized. Defaults: additive-ish color (Src=1?) with bloom alpha SrcA=0/DstA=1 per Properties L227-244 (verify: _BlendModeSrc=1 One, _Dst=1 One, _SrcA=0 Zero, _DstA=1 One; _ZWrite=0 default per reaudit; _ZTest=4 LEqual). Lighting Off. Single Pass HLSLPROGRAM vert/frag.
- Confidence: high
- Evidence: SubShader L247-273; PARTICLE_REAUDIT.md Default _ZWrite=0; census of 284-row MPB/queue note (explicit source queues preserved).
- Guards: None (fixed pipeline state referencing material uniforms).
- Id: P-PIPELINE
- Lines: 247-276
- Operation Signatures: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite [_ZWrite]; Offset [_OffsetFactor], [_OffsetUnits]; Stencil { Ref [_StencilRefValue] Comp [_StencilComp] Pass [_StencilPass] }
- Rgb Alpha: Dual-blend contract is the bloom-alpha transport: color additive, alpha carries bloom/whiteboost payload (_BloomMultiplier scaling in ALWAYS/POST paths).
- Sampling: N/A.
- Sharing Candidates: Rain.shader (same dual-blend header pattern, different defaults); Unlit/Glowing (same stencil/offset parameter idiom)
- Sharing Class: FAMILY_SHARED
- Spaces: Clip/depth via ZTest/ZWrite/Offset; stencil ref/comp/pass.
- Stage: config
- Time: N/A.
- Title: Pipeline state, dual (color + bloom-alpha) blending, queue

## 66. /regions/3

- Classification: `EXACT_SHARED`
- Source: particles.json
- Behavior: Props buffer (11 entries + conditional _ColorsArrayOffset) via UNITY_ACCESS_INSTANCED_PROP with direct fallback; PerDrawSprite instanced-vs-CBUFFER dual with PARTICLES_RENDERER_COLOR/_FLIP macros; UnityPerMaterial CBUFFER (20 fields). Vertex: SETUP/TRANSFER/INITIALIZE_VERTEX_OUTPUT_STEREO; Frag: SETUP_INSTANCE_ID + SETUP_STEREO_EYE_INDEX_POST_VERTEX. Stereo: STEREO_INSTANCING_ON multi_compile + SINGLE_PASS/MULTIVIEW guards in Camera + UnityStereoTransformScreenSpaceTex for depth UV + per-eye camera + Y-billboard per-eye transform + XR eye-texture dims for blue noise. Fire adapter _StartTime seconds vs CPU song-beat fade.
- Confidence: high
- Evidence: L544-603; L718-720/1113-1114; Camera.hlsl L6-25; PARTICLE_REAUDIT.md Unity 6000.3.13f1 fallback note; stereo blue-noise XR note; fire-adapter note; census notes (Rain per-renderer _Color; 284-row MPB census).
- Guards: UNITY_INSTANCING_ENABLED; STEREO_INSTANCING_ON / UNITY_SINGLE_PASS_STEREO / STEREO_MULTIVIEW_ON; UNITY_UV_STARTS_AT_TOP (Camera).
- Id: P-INST-STEREO
- Lines: 544-603, 634, 690-691, 694-711, 718-720, 765, 957-966, 1105, 1113-1114, 1372-1373, Camera.hlsl
- Operation Signatures: UNITY_INSTANCING_BUFFER_START(Props) / UNITY_DEFINE_INSTANCED_PROP(type,name) / UNITY_INSTANCING_BUFFER_END(Props); UNITY_ACCESS_INSTANCED_PROP(Props, _Color|_SecondaryColor|_MaskStrength|_Mask2Strength|_TimeOffset|_StartTime|_MeshPackingId|_UV3Offset|_UV3Scale|_DistortionStrength|_ColorsArrayOffset); UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); inline float3 GetStereoAwareCameraPosition(); inline float4 ComputeScreenPosCustom(float4 pos); inline float4 UnityFlipSprite(in float3 pos, in half2 flip); inline float4 GetSpriteRendererColor()
- Rgb Alpha: Instanced _Color/_SecondaryColor/_MaskStrengths feed alpha chain per-draw; sprite adapter gates color (white vs renderer) by flip presence; no alpha in plumbing itself.
- Sampling: Depth/stereo sampling hooks (projectedUv stereo transform L1372; blue-noise XR dims).
- Sharing Candidates: Lit/Unlit/Mirror/WaterLit/Parametric* (identical UNITY_SETUP/TRANSFER + GetStereoAwareCameraPosition/ComputeScreenPosCustom call shapes) — EXACT call-shape reuse; Rain.shader (same Props._Color/_TimeOffset idiom)
- Sharing Class: EXACT_SHARED
- Spaces: Object/world/view/clip/screen + per-eye camera; object translation for noise/dissolve-center/cutout.
- Stage: both
- Time: _TimeOffset/_StartTime instanced time roots (see P-TIME).
- Title: Instancing, sprite adapter, stereo, and screen plumbing

## 67. /regions/5

- Classification: `FAMILY_SHARED`
- Source: particles.json
- Behavior: Object/local (i.vertex, localPos, dissolve LOCAL, curve angle=y/x), world (worldPos anchor for fog/clip/cutout/dissolve-WORLD/soft/close/view/hologram-lp; object translation via unity_ObjectToWorld._m03_m13_m23), view (eye depth -mul(UNITY_MATRIX_V,worldPos).z for soft payload/close gate), clip (SV_POSITION via UnityObjectToClipPos / UNITY_MATRIX_P from view billboards / UNITY_MATRIX_VP for Y-billboard), screen (ComputeScreenPosCustom + projectedUv + texel clamp + stereo transform; noiseScreenPos scaled by _GlobalBlueNoiseParams + clip.w*random + object xz), UV (raw -> ST -> pan -> worldspace-projection -> secondary-override -> distortion-perturb -> custom-wrap -> flipbook-atlas), spectrogram-index (uv3.x*63), dissolve spaces (Local/World/WorldCentered), height altitude (worldPos.y*scale+offset).
- Confidence: high
- Evidence: L605-617, 726-814, 820-962, 1030-1107, 1118-1145, 1331-1404, 1435, 1538-1547; reaudit R13/R15/R16/R17/R18/R19.
- Guards: USE_BILLBOARD; WORLDSPACE_PANNING_MAIN/DISTORTION; _DISSOLVE_SPACE_WORLD[_CENTERED]; SOFT+depth; stereo guards.
- Id: P-SPACES
- Lines: 605-617, 726-814, 820-962, 1030-1107, 1118-1647 passim
- Operation Signatures: mul(unity_ObjectToWorld, v).xyz; mul(unity_WorldToObject, ...); mul(UNITY_MATRIX_V|P|VP, ...) / UnityObjectToClipPos(v) / UnityObjectToWorldNormal(n); ComputeScreenPosCustom(o.vertex); UnityStereoTransformScreenSpaceTex(projectedUv); -mul(UNITY_MATRIX_V, float4(worldPos,1)).z (eye depth); unity_ObjectToWorld._m03_m13_m23 (object translation); unity_WorldToObject._m30_m31_m32 (worldspace speed row) + abs(normal) projection
- Rgb Alpha: Spaces select fog/alpha (eye depth for soft/close; worldPos for height/distance fog; screen for prepass/depth/noise).
- Sampling: Screen/projected UVs sample prepass/depth/noise; world/object-relative positions sample cutout/hologram.
- Sharing Candidates: Lit/Unlit/Mirror/WaterLit/Parametric/Rain/Spectrogram (same space taxonomy + object-translation + ComputeScreenPosCustom idioms) — FAMILY (payload wiring differs)
- Sharing Class: FAMILY_SHARED
- Spaces: (this region)
- Stage: both
- Time: Worldspace speed term uses matrix row (not time); panning adds time.y (see P-TIME).
- Title: Coordinate spaces and transforms

## 68. /regions/6

- Classification: `EXACT_SHARED`
- Source: particles.json
- Behavior: Contract: (t/20,t,2t,3t) + offset*(0.05,1,2,3); FREEZE=offset only; SONG=_SongTime+offset; STD=_Time+_TimeHelperOffset+offset (R3 exact packing). Particles always passes Props._TimeOffset; flipbook also subtracts Props._StartTime. Component map: .y = ALL UV/mask/distortion/secondary pans + worldspace pans + layeredTime + vertex-flipbook phase + flipbook timing base; .x = gradient LUT position ONLY (frac(_GradientPosition+time.x*speed)); .w = hologram hTime ONLY; .z unused. Displacement pan uses time.y*_DisplacementPanningSpeed. Distortion plain/ layered/worldspace all scale pan by *0.1 (R6) with unscaled -1 offset in frag. SPECTROGRAM/SPATIAl paths use same .y base.
- Confidence: high
- Evidence: Time.hlsl L4-27; Particles L777/779/816/828/842/861/866-889/898/906/935/950/955/976-978/1027/1116/1336/1434-1444; PARTICLE_REAUDIT.md R3 + Queen/Pyro anchors (post-color-fog dither, MIPMAP sampling-only).
- Guards: _CUSTOM_TIME_SONG_TIME/_CUSTOM_TIME_FREEZE (local); default standard.
- Id: P-TIME
- Lines: Time.hlsl; Particles 777-1336/1434 passim
- Operation Signatures: inline float4 GetTimeOffsetVector(float offset); inline float4 GetTime(float offset); float4 time = GetTime(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)); float layeredTime = GetTime(...).y; float flipbookTime = (GetTime(...).y - UNITY_ACCESS_INSTANCED_PROP(Props,_StartTime)) * _FlipbookSpeed; float2 gradientUv = float2(albedo.a, frac(_GradientPosition + time.x * _GradientPanningSpeed)); float hTime = time.w
- Rgb Alpha: Time selects alpha-affecting pans (all .y) + gradient-x + hologram-w; wrong component = wrong fade rhythm (R3).
- Sampling: Time offsets sampling UVs (never LOD/bias except via uv).
- Sharing Candidates: Lit.shader (identical GetTime call shape L1016/1116) — EXACT; Rain/Spectrogram (same .y-pan idiom) — EXACT contract, different sites
- Sharing Class: EXACT_SHARED
- Spaces: UV/screen/worldscroll derived from time; worldspace speed term is matrix-derived, time-independent.
- Stage: both
- Time: (this region)
- Title: Time contract and per-site component map (R3)

## 69. /regions/7

- Classification: `FAMILY_SHARED`
- Source: particles.json
- Behavior: Sites: displacement tex2Dlod(...,0) explicit L0 (vert, no derivatives); main/mask/mask2/secondary/gradient/distortion plain tex2D (frag, derivative LOD); main-with-MIPMAP_BIAS tex2Dbias(...,_MipmapBias) IFF MIPMAP_BIAS (Pyro sampling-only, no other effect); cutout tex3D(_CutoutTex) (frag); depth SAMPLE_DEPTH_TEXTURE (frag, full-res built-in; game bias neutral per reaudit); blue-noise tex2D(_GlobalBlueNoiseTex) via ApplyNoiseDither; prepass tex2D(_BloomPrePassTexture) via SampleBloomPrePass. NO manual LOD for flipbook/atlas. World-noise cutout + depth + prepass share screen/object-relative coords.
- Confidence: high
- Evidence: L780/1131-1133/1152/1191/1198/1227/1242/1244/1262/1264/1292/1315/1337/1378 + ApplyNoiseDither/SampleBloomPrePass helpers; PARTICLE_REAUDIT.md Pyro 04ac3ff0 (MIPMAP sampling-only), soft-depth f316ae8e (clamp/scale/bias/reciprocal/subtract/saturate), blue-noise 8a3c8f3a, full-res depth neutralizes game bias.
- Guards: MIPMAP_BIAS (main only); SOFT+DEPTH_TEXTURE|DEPTH_TEXTURE_ENABLED (depth); NOISE_DITHERING (noise); BLOOM_FOG/_FOGTYPE_* (prepass); _CUTOUTTYPE_WORLDSPACE_NOISE (tex3D).
- Id: P-SAMPLE-LOD
- Lines: 780, 1131-1337, 1378, helpers
- Operation Signatures: tex2Dlod(_DisplacementTex, float4(dispUV,0,0)); tex2D(sampler, uv) (main/mask/secondary/gradient/distortion); tex2Dbias(_MainTex, float4(uv,0,_MipmapBias)) iff MIPMAP_BIAS; tex3D(_CutoutTex, (cutoutPosition+offset)*scale).a; SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, projectedUv) + 1/(_ZBufferParams.z*raw+_ZBufferParams.w); tex2D(_GlobalBlueNoiseTex, noiseScreenPos.xy/noiseScreenPos.ww).r - 0.5; tex2D(_BloomPrePassTexture, ((screenPos.xy/screenPos.w)-0.5)*ratio+0.5).rgb
- Rgb Alpha: Sampling feeds alpha (main R/A, flipbook dot, mask R/A, dissolve none, depth fades) vs RGB (TEXTURE_COLOR rgb*_BaseLayer, gradient rgb, hologram add, fog/prepass rgb). _BaseLayer scales RGB ONLY (frag L1247).
- Sampling: (this region)
- Sharing Candidates: Lit/Unlit/Mirror/WaterLit/Parametric (tex2Dbias-gated MIPMAP + tex3D cutout + SAMPLE_DEPTH + blue-noise call shapes) — FAMILY (textures/factors differ); Spectrogram.shader (same CalculateSpectrogramIndex-gated sampling) — FAMILY
- Sharing Class: FAMILY_SHARED
- Spaces: UV/screen/object-relative/depth-projection coords per site.
- Stage: both
- Time: UV sites use time.y (.x gradient, .w hologram); LOD/bias independent of time.
- Title: Sampling, LOD/bias, and depth/noise/prepass routes

## 70. /regions/20

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: particles.json
- Behavior: Masks (Britney orphan-mask: no distortion sample without SIMPLE parent): sample=tex2D(_MaskTex,maskUv) (.r iff MASK_RED_IS_ALPHA else .a); strength=Props._MaskStrength. Modes via _MASKBLEND_*: default Multiply a*=lerp(1,m,strength); ADD a+=m*strength*color.a; MASKED_ADD a*=1+m*strength. MASK2 identical (_MASK2BLEND_*). COLOR_GRADIENT (R2): AFTER both masks; uv=(albedo.a UNSATURATED accumulated alpha, frac(_GradientPosition+time.x*speed)); gradient=tex2D(_ColorGradient,uv); rgb*=gradient.rgb (RGB-ONLY; LUT alpha never used; recovered routes never use it). DISSOLVE (R14 + axis-factor note): pos=i.localPos (or worldPos iff _DISSOLVE_SPACE_WORLD[_CENTERED]; centered-=object translation); d=(dot(pos,normalize(axis))-_DissolveOffset)*(reverse?-1:1); t=saturate(d*_DissolveScale+0.5); a*=t. No strength uniform; mapped scale/reverse properties.
- Confidence: high
- Evidence: L1283-1359; reaudit R2/R5/R14 (+R30 axis-factor reference in code comment? dissolve axis factor multiplies chain once).
- Guards: MASK[_BLEND_*]/MASK2[_BLEND_*] + RED_IS_ALPHA (frag/both); COLOR_GRADIENT (both); DISSOLVE + _DISSOLVE_SPACE_WORLD[_CENTERED] + DISSOLVE_PROGRESS_FROM_VERTEX_ALPHA (declared, identity-only).
- Id: P-MASK-GRAD-DISSOLVE
- Lines: 1283-1359
- Operation Signatures: maskValue = RED_IS_ALPHA ? tex.r : tex.a; Multiply: albedo.a*=lerp(1,maskValue,strength) | Add: albedo.a+=maskValue*strength*color.a | MaskedAdd: albedo.a*=1+maskValue*strength; gradientUv=float2(albedo.a, frac(_GradientPosition+time.x*_GradientPanningSpeed)); albedo.rgb*=tex2D(_ColorGradient,gradientUv).rgb (R2); float t=saturate(dot(pos,normalize(_DissolveAxisVector.xyz))...*_DissolveScale+0.5); albedo.a*=t (R14)
- Rgb Alpha: Masks/dissolve touch ALPHA only; gradient touches RGB only (alpha is LUT-x, not LUT-alpha).
- Sampling: Mask/mask2/gradient tex2D; dissolve pure math.
- Sharing Candidates: Lit dissolve/mask-lerp idioms share SHAPE only; unsaturated-alpha LUT-x + time.x + RGB-only + axis-factor-once are compile-time-parameterized differences — SHARED_WITH_COMPILE_TIME_PARAMETERS at most; verify per-site before reuse
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Spaces: Mask uv (vert); gradient-x is alpha-space; dissolve local/world/centered.
- Stage: frag (uvs in vert)
- Time: Mask pans time.y (vert); gradient time.x; dissolve static.
- Title: Mask blends + gradient-after-masks (R2) + dissolve axis factor (R14)

## 71. /regions/21

- Classification: `FAMILY_SHARED`
- Source: particles.json
- Behavior: SOFT_PARTICLES+depth (Britney no-global ebdcf197... has NO depth sample; depth-enabled f316ae8e recovers full order): projectedUv=(screenPos.xy/screenPos.w) clamped to 1-0.5*texel; stereo transform iff stereo; rawDepth=SAMPLE_DEPTH_TEXTURE; sceneDepth=1/(_ZBufferParams.z*raw+_ZBufferParams.w) (reciprocal decode); eyeDepth=screenPos.z (vert eye-depth payload, gated 28ca48d5); softFade=saturate((sceneDepth-eyeDepth)*_SoftFactor); a*=softFade. CLOSE_TO_CAMERA (frag db0bff39): eyeDepth=-mul(UNITY_MATRIX_V,worldPos).z (recomputed, Euclidean NOT used); fade=saturate((eyeDepth-dist)*width) LINEAR (not smoothstep); a*=fade. VIEW_ALIGN (frag e17f3714, R10/R16): camDir=normalize(worldPos-stereoCam); alignment=|dot(camDir,normalize(worldNormal))|; squared iff _SquareAngle>0.5; v=alignment*_ViewAlignFactor+_ViewAlignOffset (+1 iff factor<0); a*=min(v,1) UPPER-ONLY clamp (negatives flow downstream).
- Confidence: high
- Evidence: L1360-1404 + vert payload L963-967; anchors f316ae8e/ebdcf197/db0bff39/e17f3714/28ca48d5.
- Guards: SOFT_PARTICLES (both) + DEPTH_TEXTURE|DEPTH_TEXTURE_ENABLED; CLOSE_TO_CAMERA_DISAPPEAR (frag); VIEW_ALIGN_DISAPPEAR (both).
- Id: P-SOFT-CLOSE-VIEW
- Lines: 963-967, 1360-1404
- Operation Signatures: projectedUv=min(screenPos.xy/screenPos.w, 1-0.5*_CameraDepthTexture_TexelSize.xy); [stereo transform]; sceneDepth=1/(_ZBufferParams.z*SAMPLE_DEPTH_TEXTURE+_ZBufferParams.w); softFade=saturate((sceneDepth-i.screenPos.z)*_SoftFactor); albedo.a*=softFade; fade=saturate((eyeDepth-_CloseCameraDisappearDistance)*_CloseCameraDisappearWidth); albedo.a*=fade; viewAlign=alignment*_ViewAlignFactor+_ViewAlignOffset (+1 iff factor<0); albedo.a*=min(viewAlign,1) (R10)
- Rgb Alpha: All three are alpha gates (no RGB). View-align upper-only clamp is the subtle sharing hazard.
- Sampling: Depth texture (biased-neutral full-res) + stereo-projected uv.
- Sharing Candidates: Rain.shader SOFT/CLOSE/VIEW subset (same evidence family f316/db0b/e17f) — FAMILY_SHARED (payload/constant parity must be verified per shader)
- Sharing Class: FAMILY_SHARED
- Spaces: Screen-projection + eye-depth + worldNormal.
- Stage: frag (+vert eye payload)
- Time: N/A.
- Title: Soft-depth (f316) + close-to-camera (db0b) + view-align (e17f,R10)

## 72. /regions/22

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: particles.json
- Behavior: Non-clip tail (skipped under ALPHA_CLIP): a*=_AlphaMultiplier; a*=worldNoiseCutoutFactor; SQUARE_ALPHA: a*=saturate(a) (=> saturate(a)*a, NOT a^2). Clip tail (ALPHA_CLIP, after fog+dither): SAME three ops in SAME order (dither first, then multiplier/cutout/square L1569-1577). Parameterized by SQUARE_ALPHA + _CUTOUTTYPE_ALPHA_CLIP position. _AlphaMultiplier is the post-everything (except fog/bloom/mirror/fill/override) gain; worldNoiseCutoutFactor is 1.0 unless WORLDSPACE_NOISE.
- Confidence: high
- Evidence: L1406-1414 + L1565-1578; frags 953990a3/40d13975 (early-clip + deferred tail).
- Guards: _CUTOUTTYPE_ALPHA_CLIP (frag; selects tail position); SQUARE_ALPHA (frag); _CUTOUTTYPE_WORLDSPACE_NOISE (factor source).
- Id: P-ALPHA-TAIL
- Lines: 1406-1414, 1565-1578
- Operation Signatures: albedo.a*=_AlphaMultiplier; albedo.a*=worldNoiseCutoutFactor; iff SQUARE_ALPHA albedo.a*=saturate(albedo.a)
- Rgb Alpha: Alpha-only tail; position relative to fog/dither differs by clip mode (sharing hazard).
- Sampling: N/A.
- Sharing Candidates: Rain/Unlit alpha tails (same three-op shape, different position) — SHARED_WITH_COMPILE_TIME_PARAMETERS
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Spaces: N/A.
- Stage: frag
- Time: N/A.
- Title: Alpha tail: multiplier / cutout factor / square, dual clip paths

## 73. /regions/26

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: particles.json
- Behavior: Per-vertex distance payload (R13): bloomFogWorldPosition=o.worldPos (except CAMERA_FACING uses unmodified source vertex); offset=worldPos-stereoCam; o.bloomFogDistanceSquared=dot(off,off) interpolated. BLOOM_FOG frag (BEFORE dither/whiteboost/premult): distSq=interpolated, or distanceSquared(worldPos) iff PRECISE_FOG (per-fragment high precision); distanceFog=1-CalculateCustomFogFactor(distSq,_FogStartOffset,_FogScale); fogAmount=distanceFog, or (1-HeightClear)*distanceFog iff HEIGHT_FOG (HeightClear=CalculateParticleHeightFogClearFactor). Per-type: ALPHA a*=fogAmount; COLOR rgb*=prepass.rgb AND a*=fogAmount; LERP rgb=lerp(rgb,prepass.rgb,1-fogAmount). Then dither iff NOISE && !CLIP. Blends via BlendFogColor semantics internally ( fogFactor*(prepass-col)+col with per-type select).
- Confidence: high
- Evidence: L1099-1107 + L1534-1563; reaudit R13 + bloom-fog follow-up + PRECISE_FOG note.
- Guards: BLOOM_FOG (global fragment); PRECISE_FOG; HEIGHT_FOG; _FOGTYPE_ALPHA|COLOR|LERP; NOISE_DITHERING+!CLIP (dither slot).
- Id: P-BLOOM-FOG
- Lines: 1099-1107, 1534-1563
- Operation Signatures: o.bloomFogDistanceSquared=dot(worldPos-stereoCam,worldPos-stereoCam) (R13); distanceSquared(i.worldPos) iff PRECISE_FOG else interpolated; float fogAmount=[(1-HeightClear)*](1-CalculateCustomFogFactor(distSq,_FogStartOffset,_FogScale)); ALPHA: a*=fogAmount | COLOR: rgb*=prepass.rgb,a*=fogAmount | LERP: rgb=lerp(rgb,prepass.rgb,1-fogAmount); inline float CalculateCustomFogFactor(float distanceSq,float fogStartOffset,float fogScale); inline float4 SampleBloomPrePass(float4 screenPos)
- Rgb Alpha: ALPHA gates alpha; COLOR gates both (rgb replaced by prepass product); LERP gates rgb by (1-amount). bloomValue snapshot AFTER this drives premult.
- Sampling: Prepass rgb + (no depth here).
- Sharing Candidates: Spectrogram/Unlit/Mirror/WaterLit ApplyBloomFog* calls (same helpers, different payload: they compute distance per-fragment or pass worldPos) — SHARED_WITH_COMPILE_TIME_PARAMETERS (payload site + PRECISE + HEIGHT combine parameterized)
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Spaces: worldPos->distanceSq (interp vs per-frag) + screenPos (prepass) + altitude (height).
- Stage: vert payload + frag apply
- Time: N/A.
- Title: Bloom-fog distance payload (R13) + per-type apply

## 74. /regions/27

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: particles.json
- Behavior: Vertex payload part of P-BLOOM-FOG split for clarity: see P-BLOOM-FOG. This entry covers HeightClear wrapper contract (PRECISE_FOG compile-time parameter): iff PRECISE_FOG, CalculateParticleHeightFogClearFactor evaluates exact smoothstep curve per-fragment from worldPos (source PRECISE route); else delegates to shared CalculateHeightFogFactor(heightInput) where heightInput=worldPos.y*_FogHeightScale+_FogHeightOffset minus globals. Used by COLOR_BY_FOG height-alpha (L1490), non-bloom fog (L1511), bloom-fog combine (L1546), FILL coverage (L1629). R8 compiles VIEW/NOISE/VERTEX_COLOR both stages; height path already carries worldPos to frag so exact curve is cheap.
- Confidence: high
- Evidence: L605-617 + uses L1490/1511/1546/1629; reaudit R8 + bloom-fog follow-up.
- Guards: PRECISE_FOG (frag) selects branch inside shared wrapper.
- Id: P-HEIGHT-CLEAR
- Lines: 605-617, 1490, 1511, 1546, 1629
- Operation Signatures: inline float CalculateParticleHeightFogClearFactor(float3 worldPosition); inline float CalculateHeightFogFactor(float exactHeightInput) (shared); PRECISE branch: heightInput-=(H+StartY); saturate(/H); 1-h*h*(3-2h)
- Rgb Alpha: Returns CLEAR factor (1=smooth); callers use 1-clear (alpha/fogAmount) or clear (LERP rgb, fill coverage).
- Sampling: N/A.
- Sharing Candidates: Lit.shader L1293 (same CalculateHeightFogFactor call shape) — SHARED_WITH_COMPILE_TIME_PARAMETERS (PRECISE selects site)
- Sharing Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Spaces: Altitude (worldPos.y).
- Stage: frag (wrapper) + callers
- Time: N/A.
- Title: Height-clear wrapper + PRECISE_FOG compile-time parameter

## 75. /regions/28

- Classification: `EXACT_SHARED`
- Source: particles.json
- Behavior: Mode lattice on bloomValue=a snapshot (L1580) + boostInput (L1581-1593): default boostInput=bloomValue; non-bloom HEIGHT LERP overrides boostInput=bloomValue*whiteBoostFogFactor (R7 attenuation); REMAP_WHITEBOOST_START overrides boostInput=(bloomValue*_QuestWhiteboostMultiplier-_WhiteBoostRemapStart)/max(1-start,1e-4) clamped >=0 and forces multiplier=1. ALWAYS | (MAINEFFECT && !POST_BLOOM): rgb=CalculateBloomComposition(rgb,bloomValue,boostInput,multiplier,_BaseColorBoost,_Threshold) = saturate(rgb*premultAlpha + (bloom*mult)^2*boost - threshold); a=bloomValue*_BloomMultiplier (ALWAYS) or bloomValue (MAINEFFECT). MAINEFFECT && POST_BLOOM (game MAIN_EFFECT on, DXBC e025580b): albedo=CalculateBloomPostComposition(rgb,bloomValue,_BloomMultiplier) (boost compiles out; post bloom provides glow). Else (no whiteboost): rgb*=abs(bloomValue). Emission-like behavior of this unlit shader = _Intensity + _BaseLayer(rgb-only) + TEXTURE_COLOR rgb + gradient rgb + hologram add + fill floor + THIS whiteboost/premult stage (no Lighting.hlsl).
- Confidence: high
- Evidence: L1580-1610; Bloom.hlsl L11-41; reaudit whiteboost/POST_BLOOM + MAIN_EFFECT_ENABLED alias notes.
- Guards: _WHITEBOOSTTYPE_MAINEFFECT|_WHITEBOOSTTYPE_ALWAYS (frag); POST_BLOOM (global); REMAP_WHITEBOOST_START (frag); FOG+HEIGHT+LERP+!BLOOM+!COLOR_BY_FOG (boost attenuation).
- Id: P-WHITEBOOST
- Lines: 1580-1610
- Operation Signatures: inline float CalculateWhiteBoost(float bloomValue,float whiteboostMultiplier,float baseColorBoost,float baseColorBoostThreshold); inline float3 CalculateBloomComposition(float3 rgb,float premultiplyAlpha,float bloomValue,float whiteboostMultiplier,float baseColorBoost,float baseColorBoostThreshold); inline float4 CalculateBloomPostComposition(float3 rgb,float alpha,float bloomMultiplier); float bloomValue=albedo.a; boostInput per above; whiteboostMultiplier=_QuestWhiteboostMultiplier (or 1.0 iff REMAP)
- Rgb Alpha: Bloom alpha = a (scaled by _BloomMultiplier in ALWAYS/POST paths); RGB premultiplied by alpha then boosted. abs() guards negative view-align spill.
- Sampling: N/A.
- Sharing Candidates: Lit L1132/1265/2515, Unlit L114/118, Glowing L121, Arc L162/167, Note L374, ParametricBox* L159/227/257/359 — identical helper call shapes — EXACT_SHARED (mode lattice parameterized)
- Sharing Class: EXACT_SHARED
- Spaces: N/A.
- Stage: frag
- Time: N/A.
- Title: Whiteboost / premultiply / POST_BLOOM composition (e025580b)

## 76. /regions/30

- Classification: `EXACT_SHARED`
- Source: particles.json
- Behavior: Verbatim-shared helper contracts + Particles call sites (see cross_shader_sharing top-level for full signatures/consumers): distanceSquared (Fog L18; stereo-cam delta; used PRECISE path L1538; also wrapped as interpolated payload R13); CalculateCustomFogFactor (Fog L24; max(distSq-start,0)->max(*scale-offset,0)->1/(att*..+1)->1-..; used L1542); CalculateHeightFogFactor/CalculateCustomHeightFogFactor (Fog L42-60; smoothstep pair; wrapped by P-HEIGHT-CLEAR); ApplyColorFog (Fog L62; mult=min(1e-4*mult,max); highlight iff FOG_COLOR_HIGHLIGHT; BLOOM_FOG&&FOG early-return else height-lerp; used L1495); SampleBloomPrePass (Fog L91; BLOOM_FOG-gated uv=((xy/w)-0.5)*ratio+0.5; used L1463/1552/1556); BlendFogColor/ApplyBloom*Calculated (Fog L102-143; per-type select; Particles does NOT call wholesale Apply* — inlines R20/R7/twin); CalculateWhiteBoost/Composition/PostComposition (Bloom L11-41; used L1595/1607); GetTime(+OffsetVector) (Time L13-27; all sites); CalculateSpectrogramIndex (Spectro L4; uint(max(uv*63,0)); used L796/1073); ApplyNoiseDither/BuildNoiseScreenPosition (PostProcess L7-25; used L959/1427/1452/1524/1560/1570); GetStereoAwareCameraPosition/ComputeScreenPosCustom (Camera L6-25; used L765/957/1105/1395).
- Confidence: high
- Evidence: Include files verbatim + Particles call lines listed; consumer grep table in cross_shader_sharing.
- Guards: BLOOM_FOG/FOG/FOG_COLOR_HIGHLIGHT/_FOGTYPE_* (Fog); _CUSTOM_TIME_* (Time); stereo guards (Camera); none (Bloom/Spectro/PostProcess core).
- Id: P-HELPERS
- Lines: Fog.hlsl; Bloom.hlsl; Time.hlsl; SpectrogramShared.hlsl; PostProcess.hlsl; Camera.hlsl(transitive); Particles call sites as listed
- Operation Signatures: (see cross_shader_sharing exports + behavior call sites)
- Rgb Alpha: Helpers are color/alpha-agnostic math except BlendFogColor/per-type selects and premult helpers (documented in P-WHITEBOOST/P-BLOOM-FOG).
- Sampling: Helpers wrap prepass/depth/noise sampling (see P-SAMPLE-LOD).
- Sharing Candidates: All consumers in cross_shader_sharing — EXACT_SHARED verbatim (same file, same signature, same formula); only call-site payloads/params differ
- Sharing Class: EXACT_SHARED
- Spaces: World/screen/uv/altitude per helper.
- Stage: both
- Time: GetTime contract (see P-TIME).
- Title: Shared helper contracts + Particles call sites

## 77. /regions/31

- Classification: `FAMILY_SHARED`
- Source: particles.json
- Behavior: Early discards before base color: PLANE_CLIPPING (frag 4eed5d58): discard iff dot(worldPos-_ClippingPlanePosition.xyz,_ClippingPlaneNormal.xyz)<0 (negative half-space; material-driven, external plane). World-noise cutout (frags 5431f008/4eed5d58): cutoutPosition=worldPos-objectTranslation; noise=tex3D(_CutoutTex,(pos+_CutoutTexOffset.xyz)*_CutoutTexScale).a; dist=noise-1.1*_Cutout+0.1; discard iff <0; ramp=saturate(dist/max(_CutoutGradientWidth,1e-6)) smoothed cubic -> worldNoiseCutoutFactor (1.0 when feature off; multiplies alpha tail later, NOT rgb). Early alpha-clip compares (see P-MAIN-SAMPLE) occur at sample time, before masks/fades/fog/dither/alpha-processing. Cutout.hlsl sibling (NOT included by Particles) exports CalculateObjectSpaceCutoutPosition + ApplyCutoutNoise(clip-only, same 1.1/0.1 constants) — Particles inlines equivalent position math but ADDS smoothstep ramp + factor-multiply instead of clip() — family divergence.
- Confidence: high
- Evidence: L1118-1138 + L1230-1275 early compares; Cutout.hlsl verbatim; reaudit _CutoutTex global owner (Noise3DTexturesGenerator).
- Guards: PLANE_CLIPPING (frag); _CUTOUTTYPE_WORLDSPACE_NOISE (frag); _CUTOUTTYPE_ALPHA_CLIP (frag early compares).
- Id: P-EARLY-DISCARD
- Lines: 1118-1138, 1230-1275, Cutout.hlsl
- Operation Signatures: if (dot(i.worldPos-_ClippingPlanePosition.xyz,_ClippingPlaneNormal.xyz)<0) discard; cutoutNoise=tex3D(_CutoutTex,(i.worldPos-objTrans+_CutoutTexOffset.xyz)*_CutoutTexScale).a; cutoutDistance=noise-1.1*_Cutout+0.1; if (<0) discard; ramp=cubic(saturate(dist/max(width,1e-6))); Cutout.hlsl: CalculateObjectSpaceCutoutPosition(wpos,origin,offset,scale); ApplyCutoutNoise(noise,cutout){clip(noise-1.1*cutout+0.1);}
- Rgb Alpha: Discards kill pixel; surviving factor modulates ALPHA tail only.
- Sampling: tex3D global cutout (object-relative).
- Sharing Candidates: ParametricBoxFakeGlow (includes Cutout.hlsl clip-only) + Rain/Lit plane-clip/cutout family — FAMILY_SHARED (clip vs ramp+multiply divergence must be preserved)
- Sharing Class: FAMILY_SHARED
- Spaces: worldPos object-relative; clip plane world.
- Stage: frag
- Time: N/A.
- Title: Early discards: plane-clip + world-noise cutout vs Cutout.hlsl

## 78. ApplyAcesTonemapping(float4)->float4

- Classification: `EXACT_SHARED`
- Source: post_bloom.json
- Evidence: SkyGradient.shader calls ApplyAcesTonemapping(color) verbatim from Tonemapping.hlsl under USE_TONE_MAPPING||ACES_TONE_MAPPING; identical saturate(ACES fit) also used by Bloom.shader pass 12 and BloomShared.BloomApplyKneeAndAces tail.
- Operation: ApplyAcesTonemapping(float4)->float4
- Users: SkyGradient.shader frag; Bloom.shader FragUpsampleAces; BloomShared.hlsl BloomApplyKneeAndAces
- Note: consider exact-shared Tonemapping.hlsl

## 79. ApplyNoiseDither(float4,float4,sampler2D)->float4; BuildNoiseScreenPosition(float4,float4,float2,float,float2)->float4

- Classification: `EXACT_SHARED`
- Source: post_bloom.json
- Evidence: BloomfogSkybox.shader frag calls ApplyNoiseDither(col, noiseScreenPos, _GlobalBlueNoiseTex) and BuildNoiseScreenPosition(...) directly from PostProcess.hlsl; signatures match verbatim.
- Operation: ApplyNoiseDither(float4,float4,sampler2D)->float4; BuildNoiseScreenPosition(float4,float4,float2,float,float2)->float4
- Users: BloomfogSkybox.shader vert/frag; Mirror/WaterLit/Lit/Spectrogram/CloudsOpaque dither routes (out of scope but same include)
- Note: consider exact-shared PostProcess.hlsl

## 80. BloomShared.hlsl re-export of Tonemapping.hlsl

- Classification: `EXACT_SHARED`
- Source: post_bloom.json
- Evidence: Bloom.shader HLSLINCLUDE includes BloomShared.hlsl which includes Tonemapping.hlsl; FragUpsampleAces and FragUpsampleAutoExposureAces call through without local reimplementation.
- Operation: BloomShared.hlsl re-export of Tonemapping.hlsl
- Users: Bloom.shader passes 12,13
- Note: consider exact-shared include chain

## 81. BloomApplyGamma(float4)->float4 vs BloomfogMesh vert cubic

- Classification: `FAMILY_SHARED`
- Source: post_bloom.json
- Evidence: Constants match to 6-9 digits (0.305306 vs 0.305306011; 0.682171 vs 0.682171111; 0.012522878 identical) and both preserve alpha; BloomShared variant is float4 helper, mesh variant is inline vertex RGB-only cubic.
- Operation: BloomApplyGamma(float4)->float4 vs BloomfogMesh vert cubic
- Users: Bloom.shader passes 4,7,8,10; BloomfogMesh.shader vert
- Note: consider family-shared with precision parameter, not byte-exact merge

## 82. BloomDownsample4 vs downsample4

- Classification: `FAMILY_SHARED`
- Source: post_bloom.json
- Evidence: Same 4-offset box geometry (d=texelSize.xyxy*(-1,-1,1,1), 0.25 mean) but BloomShared clamps every tap via BLOOM_SAMPLE_UV(saturate) and takes explicit texelSize arg; Blurs.downsample4 takes (blurTex,uv,radius,texelSize) with radius multiplier and no clamp; Bloom.shader pass 1 wraps texelSize in abs() while pass 3 comment retains clamp for sampler-compat.
- Operation: BloomDownsample4 vs downsample4
- Users: Bloom.shader FragDownsample4/4Alpha; (no live user of Blurs.downsample4)
- Note: consider family-shared only with clamp+radius compile-time parameters

## 83. BloomUpsampleTent vs upsampleTent

- Classification: `FAMILY_SHARED`
- Source: post_bloom.json
- Evidence: Same 9-tap tent weights (1,2,1 / 2,4,2 / 1,2,1 /16) and d-vector swizzle family, but scale convention differs: BloomShared takes sampleScale directly (driver passes BloomRenderUtility sampleScale), Blurs takes radius*0.5 internally; clamp and sampler types differ as above.
- Operation: BloomUpsampleTent vs upsampleTent
- Users: Bloom.shader FragUpsampleTent* (passes 5,7,11,12,13); (no live user of Blurs.upsampleTent)
- Note: consider family-shared only with scale-convention parameter

## 84. /behavior_rows/0

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: BF-MESH
- Shader Path: Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader
- Stage: vertex+fragment
- Behavior: Global matrix transforms the mesh; cubic gamma-to-linear transfer changes RGB while preserving vertex alpha; line-mask sampling and BLOOM_FOG distance transmission produce premultiplied output.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader:63-110; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/BloomPrePassLine/_shader.json
- Limits: BLOOM_FOG is a compile-time branch; the tangent/line-mask path remains mesh-specific.

## 85. /behavior_rows/1

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: BF-SKYBOX
- Shader Path: Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader
- Stage: vertex+fragment
- Behavior: Builds a fullscreen skybox quad, samples the bloom prepass only when BLOOM_FOG is enabled, applies global blue-noise dither, and clears alpha.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader:53-93; Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader:3-8; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/BloomSkyboxQuad/_shader.json
- Limits: OVERDRAW_VIEW is intentionally omitted.

## 86. /behavior_rows/2

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: BF-SKYGRADIENT
- Shader Path: Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader
- Stage: vertex+fragment
- Behavior: Uses SV_VertexID to build the fullscreen triangle, unprojects a camera ray, samples the gradient by normalized ray Y, and optionally applies ACES tone mapping.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader:43-79; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/SkyGradient/_shader.json
- Limits: The source is a Hidden/SkyGradient replacement; ShaderLab state is not established by stage binaries.

## 87. /behavior_rows/3

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: CLOUD-OPAQUE
- Shader Path: Assets/_Graphics/Shaders/CloudsOpaque.shader
- Stage: vertex+fragment
- Behavior: Optional world-noise swirl/displacement, inverted-origin normal, five-light diffuse, height/distance fog, optional ACES and blue-noise dither; alpha is always zero.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/CloudsOpaque.shader:164-265; Assets/_Graphics/Shaders/CLOUD_REAUDIT.md:24-31; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/CloudsOpaque/_shader.json
- Limits: OVERDRAW_VIEW and absent source routes are omitted.

## 88. /behavior_rows/4

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: CLOUD-TRANSPARENT
- Shader Path: Assets/_Graphics/Shaders/CloudsLitTransparent.shader
- Stage: vertex+fragment
- Behavior: Rotation-layer weights and vertex wave deform the cloud sheet; the complete Billie route samples distortion then diffuse, adds front/back five-light terms, applies bottom/runway fades, and preserves separate RGB/alpha blend factors.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/CloudsLitTransparent.shader:215-310; Assets/_Graphics/Shaders/CLOUD_REAUDIT.md:33-44; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/CloudsLitTransparent/_shader.json
- Limits: Partial keyword bundles fall back to the generic color route; an extra no-keyword hash has an unproven pass role.

## 89. /behavior_rows/5

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: GLOWING
- Shader Path: Assets/_Graphics/Shaders/Glowing.shader
- Stage: vertex+fragment
- Behavior: Returns material _Color, optionally applies main-effect white boost, then applies bloom fog using alpha-weighted start/scale and the bloom prepass.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Glowing.shader:114-136; Assets/_Graphics/Shaders/Glowing.shader:3-24; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Glowing/_shader.json
- Limits: Geometry fallback role is project policy; direct Custom/Glowing recovery evidence exists.

## 90. /behavior_rows/6

- Classification: `EXACT_SHARED`
- Source: scope_remaining.json
- Id: GRABPASS
- Shader Path: Assets/_Graphics/Shaders/GrabPassTexture1.shader
- Stage: vertex+fragment
- Behavior: Captures _GrabTexture1 with Cull Off, ZWrite Off, ZTest Always, ColorMask 0, and writes zero.
- Comparison Class: EXACT_SHARED
- Evidence: Assets/_Graphics/Shaders/GrabPassTexture1.shader:3-43; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/GrabPassTexture1/_shader.json
- Limits: The grab texture lifecycle is owned by the consuming runtime.

## 91. /behavior_rows/7

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: LIGHTNING
- Shader Path: Assets/_Graphics/Shaders/Lightning.shader
- Stage: vertex+fragment
- Behavior: Target-point conversion, two scrolling noise scales, timing-texture edge fade, extrusion, main texture/color/alpha, bloom and white-boost output; instancing/stereo variants are present.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Lightning.shader:1-2; Assets/_Graphics/Shaders/LIGHTNING_REAUDIT.md:5-40; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SimpleLightning/_shader.json
- Limits: Runtime time alignment is proven separately; Metallica zero-length source transforms are a data defect.

## 92. /behavior_rows/8

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: LIT
- Shader Path: Assets/_Graphics/Shaders/Lit.shader
- Stage: vertex+fragment
- Behavior: Large SimpleLit feature family: secondary UVs, material/vertex/emission/mask/displacement paths, five-light lighting, reflection, parallax/dissolve/rim, fog, bloom, ACES, lightmap, instancing and stereo.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/LIT_REAUDIT.md:23-63; Assets/_Graphics/Shaders/LIT_REAUDIT.md:158-170; /tmp/opencode/lit_modularization_20260903/evidence_audit/report.md:7-16; /mnt/programs/Code/GitRepository/ChroMapper/Assets/_Graphics/Shaders/Lit.shader:1-3
- Limits: Sixteen executable replacement routes have no recovered fragment binary; pass 1 role remains unproven.

## 93. /behavior_rows/9

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: MIRROR
- Shader Path: Assets/_Graphics/Shaders/Mirror.shader
- Stage: vertex+fragment
- Behavior: Normal/detail-normal scrolling perturbs screen reflection UV; optional diffuse/lightmap lighting and dirt multiply the reflected result; bloom fog and unconditional dither finish the output.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Mirror.shader:198-275; Assets/_Graphics/Shaders/Mirror.shader:4-27; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Mirror/_shader.json
- Limits: Game stereo reflection-atlas and OVERDRAW_VIEW routes are omitted by project design.

## 94. /behavior_rows/11

- Classification: `FAMILY_SHARED`
- Source: scope_remaining.json
- Id: NOTE
- Shader Path: Assets/_Graphics/Shaders/Object/Note.shader
- Stage: vertex+fragment
- Behavior: Union target for NoteHD and NoteLW with instanced color/strobe/timeline adapter, cutout and plane cuts, face-dependent reflection, rim, ACES/white boost, fog, bloom-fog, and optional fake-mirror premultiplication.
- Comparison Class: FAMILY_SHARED
- Evidence: Assets/_Graphics/Shaders/Object/Note.shader:201-418; Assets/_Graphics/Shaders/README.md:110-138; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/NoteHD/_shader.json; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/NoteLW/_shader.json
- Limits: HD/LW source families diverge in alpha, reflection, cutout and fake-mirror routes; the union is not an exact source clone.

## 95. /behavior_rows/12

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: OBST-DIST
- Shader Path: Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader
- Stage: vertex+fragment
- Behavior: Depth-aware screen displacement samples the grab/bloom source with tunable UV scale, cutout, clipping, rim and fog routes; runtime owns depth/grab inputs.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader:1-16; Assets/_Graphics/Shaders/README.md:42-44; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/ScreenDisplacementHD/_shader.json
- Limits: OVERDRAW_VIEW is omitted and project preview routes extend the source contract.

## 96. /behavior_rows/14

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PFG
- Shader Path: Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader
- Stage: vertex+fragment
- Behavior: Face-relative parametric deformation, squared texture alpha, height/angle/distance fades, optional noise cutout/clipping, premultiplied output and compile-time white boost.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader:131-231; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:20-35; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/ParametricBoxFakeGlow/_shader.json
- Limits: Zero-height behavior differs because ParametricShared guards the denominator; see PFG-DEFECT.

## 97. /behavior_rows/15

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PBO
- Shader Path: Assets/_Graphics/Shaders/ParametricBoxOpaque.shader
- Stage: vertex+fragment
- Behavior: Instanced color alpha is squared; optional height/distance fog, unconditional blue-noise dither, white boost, and bloom-prepass or constant fog target produce opaque output.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/ParametricBoxOpaque.shader:103-173; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:20-35; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/OpaqueNeonLight/_shader.json
- Limits: This maps OpaqueNeonLight, not FrameHD. FrameHD has separate anonymous layouts.

## 98. /behavior_rows/16

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PBT
- Shader Path: Assets/_Graphics/Shaders/ParametricBoxTransparent.shader
- Stage: vertex+fragment
- Behavior: Height-selected width/alpha, optional parametric world noise/fade/warp, squared alpha, optional packed-probe reflection, fog transmission and glass-opacity addition.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/ParametricBoxTransparent.shader:175-263; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:20-50; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/TransparentNeonLight/_shader.json
- Limits: Source controls for unsupported SPECULAR/NORMAL_MAP/rim routes remain inert.

## 99. /behavior_rows/17

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PSB
- Shader Path: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader
- Stage: vertex+fragment
- Behavior: Three-slice cap/body geometry, optional Y billboard and alpha-width scaling, cubic alpha/noise/texture fades, fog, white boost, masked dither and bloom multiplier.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader:206-378; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:7-18; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Parametric3SliceSprite/_shader.json
- Limits: The current cap-UV base is a defect candidate; see PSB-DEFECT.

## 100. /behavior_rows/18

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PARTICLES
- Shader Path: Assets/_Graphics/Shaders/Particles.shader
- Stage: vertex+fragment
- Behavior: Recovered particle family covers renderer color, masks, gradients, flipbooks, depth/soft particles, billboards, lifetime/dissolve, world UV/noise, fog, bloom, white boost, dither, instancing and stereo.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:6-21; Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:42-80; Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:152-170; /mnt/programs/Code/GitRepository/ChroMapper/Assets/_Graphics/Shaders/Particles.shader:1-3
- Limits: OVERDRAW_VIEW, zero-row selectors and unsupported VERTEX_DISPLACEMENT routes remain omitted/unverified.

## 101. /behavior_rows/19

- Classification: `EXACT_SHARED`
- Source: scope_remaining.json
- Id: PP-BLIT
- Shader Path: Assets/_Graphics/Shaders/Post Process/BlitBlendColor.shader
- Stage: vertex+fragment
- Behavior: SV_VertexID fullscreen triangle returns _Color; SrcAlpha/OneMinusSrcAlpha blending writes RGB only.
- Comparison Class: EXACT_SHARED
- Evidence: Assets/_Graphics/Shaders/Post Process/BlitBlendColor.shader:8-49; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/BlitBlendColor/_shader.json
- Limits: Exact output is direct; pass state remains project-side ShaderLab evidence.

## 102. /behavior_rows/20

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PP-BLOOM
- Shader Path: Assets/_Graphics/Shaders/Post Process/Bloom.shader
- Stage: 14 fragment passes
- Behavior: Four/13-tap downsample, alpha gates, tent/box upsample, gamma, direct combine, Reinhard, ACES and auto-exposure ACES routes.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Post Process/Bloom.shader:37-138; Assets/_Graphics/Shaders/Post Process/Bloom.shader:141-255; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/PostProcessing/Bloom/_shader.json
- Limits: Pass selection is separate from fragment behavior; sampler/addressing compatibility is not fully proven.

## 103. /behavior_rows/22

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: PP-POSTBLOOM
- Shader Path: Assets/_Graphics/Shaders/Post Process/PostBloom.shader
- Stage: vertex+fragment
- Behavior: Samples scene and post-bloom textures, computes four-tap alpha white boost, applies blue-noise dither, combines bloom and scene, and conditionally clears output alpha.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Post Process/PostBloom.shader:5-31; Assets/_Graphics/Shaders/Post Process/PostBloom.shader:90-148; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/MainEffect/_shader.json
- Limits: LIV_MR/CLEAR_SCREEN_ALPHA and mono/stereo variants are compile-time routes; source scale/offset and SampleBias slots are not producer-proven.

## 104. /behavior_rows/23

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: RAIN
- Shader Path: Assets/_Graphics/Shaders/Rain.shader
- Stage: vertex+fragment
- Behavior: Mesh streams drive streak phase and vertical fade; texture/color/vertex alpha, color-fog, masks, and premultiplied output are selected by keywords, with optional bloom prepass.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Rain.shader:136-203; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Rain/_shader.json
- Limits: Runtime mesh streams and per-renderer color are required; visual parity is not established by static assets.

## 105. /behavior_rows/24

- Classification: `EXACT_SHARED`
- Source: scope_remaining.json
- Id: DEPTH
- Shader Path: Assets/_Graphics/Shaders/SetDepthOnly.shader
- Stage: vertex+fragment
- Behavior: Saturates vertex COLOR, transforms POSITION, returns interpolated color, and applies material stencil/depth state; only stereo is a compiled variant.
- Comparison Class: EXACT_SHARED
- Evidence: Assets/_Graphics/Shaders/SetDepthOnly.shader:61-75; Assets/_Graphics/Shaders/SetDepthOnly.shader:3-15; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SetDepthOnly/_shader.json
- Limits: Recovered materials override defaults (Ref=1, Always, Replace, ZWrite Off); stage binaries do not prove state.

## 106. /behavior_rows/25

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: SPECTRO
- Shader Path: Assets/_Graphics/Shaders/Spectrogram.shader
- Stage: vertex+fragment
- Behavior: UV.x selects one of 64 samples; peak offset deforms vertices; optional five-light diffuse/specular/falloff, always-evaluated height fog, bloom fog, ACES and unconditional blue-noise dither produce zero alpha.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/Spectrogram.shader:1-14; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:29-38; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Spectrogram/_shader.json
- Limits: OVERDRAW_VIEW and white-boost/noise-keyword routes are intentionally omitted.

## 107. /behavior_rows/26

- Classification: `EXACT_SHARED`
- Source: scope_remaining.json
- Id: SPECTRO-UNLIT
- Shader Path: Assets/_Graphics/Shaders/SpectrogramUnlit.shader
- Stage: vertex+fragment
- Behavior: UV.x selects 64 samples; visibility is a step against sample height and scale; color alpha is multiplied by visibility; bloom fog lerps full RGBA and no dither/tonemap is used.
- Comparison Class: EXACT_SHARED
- Evidence: Assets/_Graphics/Shaders/SpectrogramUnlit.shader:1-14; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:40-48; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/UnlitSpectrogram/_shader.json
- Limits: Diagnostic OVERDRAW_VIEW is omitted.

## 108. /behavior_rows/27

- Classification: `EXACT_SHARED`
- Source: scope_remaining.json
- Id: STENCIL
- Shader Path: Assets/_Graphics/Shaders/Stencil.shader
- Stage: vertex+fragment
- Behavior: Transforms POSITION to clip space, writes zero RGBA, preserves destination color with zero/one blend, and updates only the configured stencil operation.
- Comparison Class: EXACT_SHARED
- Evidence: Assets/_Graphics/Shaders/Stencil.shader:7-18; Assets/_Graphics/Shaders/Stencil.shader:35-100; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SimpleStencil/_shader.json
- Limits: Ref/material values differ by environment; stage binaries do not prove ShaderLab state.

## 109. /behavior_rows/28

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: scope_remaining.json
- Id: WATER
- Shader Path: Assets/_Graphics/Shaders/WaterLit.shader
- Stage: vertex+fragment
- Behavior: Recovered production routes use normal/detail normals, packed lightmap or probe reflection, Z fade, ACES, bloom height fog and post-fog dither; unsupported source feature properties remain serialized.
- Comparison Class: SHARED_WITH_COMPILE_TIME_PARAMETERS
- Evidence: Assets/_Graphics/Shaders/WaterLit.shader:282-417; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:9-27; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/WaterLit/_shader.json
- Limits: Unity spec-cube fallback is not the recovered packed-probe route; see WATER-DEFECT.

## 110. Parametric bloom alpha and white-boost call sites

- Classification: `SHARED_WITH_COMPILE_TIME_PARAMETERS`
- Source: parametric_object.json
- Operation: Parametric bloom alpha and white-boost call sites
- Confidence: HIGH
- Evidence: Seven of eight reviewed parametric/object shaders call Bloom.hlsl, but premultiply alpha, bloom value, white multiplier, and fog tap point differ by shader.

## 111. Parametric height and distance cores

- Classification: `FAMILY_SHARED`
- Source: parametric_object.json
- Operation: Parametric height and distance cores
- Confidence: HIGH
- Evidence: Four parametric consumers use ParametricShared; non-bloom direction, divisor source, and HEIGHT_FOG gating differ.
