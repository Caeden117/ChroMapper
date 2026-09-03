# Recovered Shader Catalog

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their behavior records remain available with an `adjacent_unverified` scope status.

Scope: `Assets/` and `Packages/` at the Phase 1 baseline (2026-09-03).

A shader is included only when comments, mapping metadata, recovered-binary/hash evidence, or audit documentation supports replacement/recovery status. Names alone were not used.

## Counts

- Shader source assets: **146** (`.shader` 114, `.cginc` 9, `.hlsl` 17, ShaderGraph 6).
- Evidence-based recovered/replacement catalog entries: **26**.

## Catalog

| Project shader | Suspected original / role | Family | Passes | Materials | Scenes | Confidence | Evidence |
|---|---|---:|---:|---:|---:|---|---|
| `ChroMapper/BloomfogMesh`<br>`Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader` | `Beat Saber bloom-fog mesh route (exact source ShaderLab name unrecorded)` | bloom_fog | 1 | 1 | 0 | medium-high | Inline BM findings include authoritative material properties and recovered binary hashes; decompilation attribution. |
| `ChroMapper/BloomfogSkybox`<br>`Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader` | `Custom/BloomSkyboxQuad (suspected from inline comparison)` | bloom_fog | 1 | 1 | 48 | medium-high | Inline BS audit compares 1.42.2/1.44.3 hashes and recovered prepass/dither behavior. |
| `ChroMapper/Sky Gradient`<br>`Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader` | `Beat Saber sky-gradient route (exact source ShaderLab name unrecorded)` | bloom_fog | 1 | 0 | 0 | medium-high | Inline SG4 recovered vertex/fragment hashes and formulas. |
| `ChroMapper/Clouds Lit Transparent`<br>`Assets/_Graphics/Shaders/CloudsLitTransparent.shader` | `Custom/CloudsLitTransparent` | clouds | 1 | 1 | 1 | high | Explicit replacement; recovered and verified against Beat Saber 1.44.3 DXBC. |
| `ChroMapper/Clouds Opaque`<br>`Assets/_Graphics/Shaders/CloudsOpaque.shader` | `Custom/CloudsOpaque` | clouds | 1 | 2 | 1 | high | Explicit replacement; recovered from Beat Saber 1.44.3 DXBC and hash-matrix audit. |
| `ChroMapper/Glowing`<br>`Assets/_Graphics/Shaders/Glowing.shader` | `Geometry fallback role; exact source ShaderLab name unrecorded` | fallback | 1 | 1 | 0 | medium-high | README mapping plus inline G1-G8 authoritative properties, binary hashes, aliases, and unsupported-route findings. |
| `ChroMapper/Lightning`<br>`Assets/_Graphics/Shaders/Lightning.shader` | `Custom/SimpleLightning` | environment_fx | 1 | 5 | 2 | high | Explicit replacement header; exact ASM and semantic HLSL for all 18 executable programs. |
| `ChroMapper/Lit`<br>`Assets/_Graphics/Shaders/Lit.shader` | `Custom/SimpleLit` | environment_lit | 1 | 505 | 48 | high | Explicit replacement header; 1.44.3 DXBC/HLSL/ASM audit, 44 environments, 4,604 pass-0 blobs. |
| `ChroMapper/Mirror`<br>`Assets/_Graphics/Shaders/Mirror.shader` | `Custom/Mirror` | mirror | 1 | 42 | 45 | high | Explicit Beat Saber replacement header and inline M1-M9 versioned audit findings. |
| `ChroMapper/Object/Arc`<br>`Assets/_Graphics/Shaders/Object/Arc.shader` | `Custom/SliderNoteCrossedStrips` | object_adapter | 1 | 1 | 0 | low-medium | README mapping plus recovered alpha/fog comments; no dedicated versioned corpus or hash audit. |
| `ChroMapper/Object/Note`<br>`Assets/_Graphics/Shaders/Object/Note.shader` | `Custom/Note (NoteHD/NoteLW recovered union)` | object_adapter_recovered | 1 | 5 | 0 | medium-high | README canonical recovered contract and extensive in-source recovered formula/property comments; adapter data remains. |
| `ChroMapper/Object/Obstacle Distortion`<br>`Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader` | `Custom/ScreenDisplacementHD` | object_adapter_recovered | 1 | 1 | 0 | medium-high | README mapping plus inline authoritative source properties and recovered non-XR binary formulas; adapter extensions remain. |
| `ChroMapper/Object/Obstacle Outline`<br>`Assets/_Graphics/Shaders/Object/ObstacleOutline.shader` | `Custom/ParametricBoxFrameHD role only; not a binary-proven full replacement` | object_adapter | 1 | 1 | 0 | medium | README role mapping and source-comparison comments; PARAMETRIC_REAUDIT states neon shaders do not replace FrameHD/LW. |
| `ChroMapper/Parametric Box Fake Glow`<br>`Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader` | `Custom/ParametricBoxFakeGlow` | parametric | 1 | 1 | 45 | high | Explicit replacement header; 1.44.3 corpus and recovered operation corrections. |
| `ChroMapper/Parametric Box Opaque`<br>`Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` | `Custom/OpaqueNeonLight` | parametric | 1 | 12 | 21 | high | Explicit replacement header plus source-property and DXBC hash evidence. |
| `ChroMapper/Parametric Box Transparent`<br>`Assets/_Graphics/Shaders/ParametricBoxTransparent.shader` | `Custom/TransparentNeonLight` | parametric | 1 | 23 | 47 | high | Explicit replacement header plus source-property and recovered-route evidence. |
| `ChroMapper/Parametric Slice Billboard`<br>`Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` | `Custom/Parametric3SliceSprite` | parametric | 1 | 71 | 38 | high | Explicit replacement header; 61,599 occurrences and recovered operation corrections. |
| `ChroMapper/Particles`<br>`Assets/_Graphics/Shaders/Particles.shader` | `Custom/CustomParticles` | particles | 1 | 220 | 47 | high | Explicit replacement header; 1.44.3 recovered DXBC audit, 345 routes and 1,574 unique blobs. |
| `ChroMapper/Post Process/Bloom`<br>`Assets/_Graphics/Shaders/Post Process/Bloom.shader` | `Beat Saber bloom chain (exact source ShaderLab name unrecorded)` | post_process | 14 | 0 | 0 | medium-high | Fourteen inline hash-labelled audit passes; README records runtime-observed 13-tap route. |
| `ChroMapper/Post Process/Post Bloom`<br>`Assets/_Graphics/Shaders/Post Process/PostBloom.shader` | `Hidden/MainEffect` | post_process | 1 | 0 | 0 | high | Explicit replacement; inline ME1-ME9 recovered corpus, hash, UV, dither and composition evidence. |
| `ChroMapper/Rain`<br>`Assets/_Graphics/Shaders/Rain.shader` | `Custom/Rain` | environment_fx | 1 | 1 | 1 | medium | README mapping and explicit recovered-formula comment, but no versioned corpus or hashes. |
| `ChroMapper/Set Depth Only`<br>`Assets/_Graphics/Shaders/SetDepthOnly.shader` | `Custom/SetDepthOnly` | depth_stencil | 1 | 2 | 35 | high | Explicit replacement; inline D1-D6 audit and 35 recovered game materials. |
| `ChroMapper/Spectrogram`<br>`Assets/_Graphics/Shaders/Spectrogram.shader` | `Custom/Spectrogram` | spectrogram | 1 | 34 | 14 | high | Explicit replacement; versioned corpus and exact-assembly coverage of 128 production routes. |
| `ChroMapper/Spectrogram Unlit`<br>`Assets/_Graphics/Shaders/SpectrogramUnlit.shader` | `Custom/UnlitSpectrogram` | spectrogram | 1 | 3 | 2 | high | Explicit replacement; versioned corpus and all normal routes covered. |
| `ChroMapper/Stencil`<br>`Assets/_Graphics/Shaders/Stencil.shader` | `Custom/SimpleStencil` | depth_stencil | 1 | 2 | 2 | high | Explicit replacement; inline S1-S6 property, hash, and fragment evidence. |
| `ChroMapper/Water Lit`<br>`Assets/_Graphics/Shaders/WaterLit.shader` | `Custom/WaterLit` | water | 1 | 4 | 2 | high | Explicit replacement; 1.44.3 corpus and exact assembly, with reflection fallback gap. |

## Per-shader details

### `ChroMapper/BloomfogMesh`

- Path: `Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader`
- Suspected original: `Beat Saber bloom-fog mesh route (exact source ShaderLab name unrecorded)`
- Family / confidence: `bloom_fog` / **medium-high**
- Passes: 1
- Current render-state declarations: ZWrite Off; Cull Off; Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]; BlendOp [_BlendOp]
- Properties (6): `_MainTex`, `_BlendSrcFactor`, `_BlendDstFactor`, `_BlendSrcFactorA`, `_BlendDstFactorA`, `_BlendOp`
- Keywords (2): `_`, `BLOOM_FOG`
- Includes: `UnityCG.cginc`
- Materials: 1; serialized consumer files: 1; scene consumers: 0.
- Runtime/mesh summary: Use GUID-linked material/serialized consumers; no direct Shader.Find ownership established.
- Audit: `inline shader evidence and/or README mapping`
- Gap: Exact original ShaderLab identity is unrecorded; no standalone audit/capture.

### `ChroMapper/BloomfogSkybox`

- Path: `Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader`
- Suspected original: `Custom/BloomSkyboxQuad (suspected from inline comparison)`
- Family / confidence: `bloom_fog` / **medium-high**
- Passes: 1
- Current render-state declarations: Cull Off; ZWrite Off; ZTest LEqual
- Properties (0): none
- Keywords (0): none
- Includes: `UnityCG.cginc`, `../ShaderLibrary/PostProcess.hlsl`
- Materials: 1; serialized consumer files: 49; scene consumers: 48.
- Runtime/mesh summary: Use GUID-linked material/serialized consumers; no direct Shader.Find ownership established.
- Audit: `inline shader evidence and/or README mapping`
- Gap: Original identity is only suspected from inline comparison; no standalone audit/capture.

### `ChroMapper/Sky Gradient`

- Path: `Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader`
- Suspected original: `Beat Saber sky-gradient route (exact source ShaderLab name unrecorded)`
- Family / confidence: `bloom_fog` / **medium-high**
- Passes: 1
- Current render-state declarations: Cull Off; ZWrite Off; ZTest Always; Blend One One, Zero Zero
- Properties (2): `_GradientTex`, `_Color`
- Keywords (0): none
- Includes: `UnityCG.cginc`, `../ShaderLibrary/Tonemapping.hlsl`
- Materials: 0; serialized consumer files: 0; scene consumers: 0.
- Runtime/mesh summary: Use GUID-linked material/serialized consumers; no direct Shader.Find ownership established.
- Audit: `inline shader evidence and/or README mapping`
- Gap: Exact original ShaderLab identity is unrecorded; no standalone audit/capture.

### `ChroMapper/Clouds Lit Transparent`

- Path: `Assets/_Graphics/Shaders/CloudsLitTransparent.shader`
- Suspected original: `Custom/CloudsLitTransparent`
- Family / confidence: `clouds` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; ZWrite Off; ZTest LEqual; Cull Off
- Properties (31): `_Color`, `_DiffuseTex`, `_DistortTex`, `_DistortTexSpeed`, `_DistortAmount`, `_DistortUVChannel`, `_BackLightingBoost`, `_FadeBottomMin`, `_FadeBottomMax`, `_RunwayFadeOffset`, `_RunwayFadeScale`, `_RotateLayerSpeeds`, `_VertexWaveFrequency`, `_VertexWaveAmplitude`, `_EnableFog`, `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_AlignNormalsToWorldOrigin`, `_EnableBackLighting`, `_EnableDiffuse`, `_EnableDiffuseTexture`, `_EnableDistortTexture`, `_EnableBottomFade`, `_EnableFadeRunway`, `_EnableVertexWave`, `_VertexMode`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`
- Keywords (4): `_`, `STEREO_INSTANCING_ON`, `ALIGN_NORMALS_TO_WORLD_ORIGIN`, `ACES_TONE_MAPPING`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Lighting.hlsl`, `ShaderLibrary/ObjectShared.hlsl`, `ShaderLibrary/Tonemapping.hlsl`
- Materials: 1; serialized consumer files: 2; scene consumers: 1.
- Runtime/mesh summary: 1 material variant / 1 environment link / 1 environment; Billie clouds / BillieClouds mesh.
- Audit: `Assets/_Graphics/Shaders/CLOUD_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Clouds Opaque`

- Path: `Assets/_Graphics/Shaders/CloudsOpaque.shader`
- Suspected original: `Custom/CloudsOpaque`
- Family / confidence: `clouds` / **high**
- Passes: 1
- Current render-state declarations: Cull [_CullMode]; ZWrite On; ZTest LEqual
- Properties (18): `_MainTex`, `_NoiseTex`, `_WorldNoiseScale`, `_WorldNoiseIntensityScale`, `_WorldNoiseIntensityOffset`, `_WorldNoiseScrolling`, `_Speed`, `_Offset`, `_FogStartOffset`, `_FogScale`, `_HeightFogOffset`, `_CullMode`, `_EnableDiffuse`, `_EnableBothSidesDiffuse`, `_InvertDiffuseNormal`, `_EnableWorldNoise`, `_EnableFog`, `_EnableNoiseDithering`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Camera.hlsl`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Lighting.hlsl`, `ShaderLibrary/Tonemapping.hlsl`, `ShaderLibrary/PostProcess.hlsl`
- Materials: 2; serialized consumer files: 2; scene consumers: 1.
- Runtime/mesh summary: 2 variants / 2 links / 1 environment; BTS HighClouds and LowClouds meshes.
- Audit: `Assets/_Graphics/Shaders/CLOUD_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Glowing`

- Path: `Assets/_Graphics/Shaders/Glowing.shader`
- Suspected original: `Geometry fallback role; exact source ShaderLab name unrecorded`
- Family / confidence: `fallback` / **medium-high**
- Passes: 1
- Current render-state declarations: Cull Back; ZTest LEqual; ZWrite On
- Properties (11): `_EnableColorInstancing`, `_Color`, `_FogStartOffset`, `_FogScale`, `_CUTOUT`, `_Cutout`, `_CutoutTexScale`, `_CutoutTexOffset`, `_CutoutTex`, `_WhiteBoostType`, `_NoiseDithering`
- Keywords (3): `_`, `STEREO_INSTANCING_ON`, `POST_BLOOM`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Camera.hlsl`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Bloom.hlsl`
- Materials: 1; serialized consumer files: 1; scene consumers: 0.
- Runtime/mesh summary: GeometryAppearanceSO glowingMaterial fallback.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Lightning`

- Path: `Assets/_Graphics/Shaders/Lightning.shader`
- Suspected original: `Custom/SimpleLightning`
- Family / confidence: `environment_fx` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite Off; Offset [_OffsetFactor], [_OffsetUnits]; Stencil
- Properties (32): `_Color`, `_MainTex`, `_NoiseTex`, `_TimingTex`, `_NoiseSmallScale`, `_SmallScaleNoiseStrength`, `_SmallScaleNoiseScrollingSpeed`, `_NoiseBigScale`, `_BigScaleNoiseStrength`, `_BigScaleNoiseScrollingSpeed`, `_NoiseScrollingSpeed`, `_XNoiseOffsetStrength`, `_Extrude`, `_ColorBoost`, `_WhiteBoost`, `_EdgeFadeStrength`, `_TargetPoint`, `_EnableTargetPoint`, `_EnableTimeOffset`, `_TimeOffset`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`, `_BlendOp`, `_CullMode`, `_ZTest`, `_OffsetFactor`, `_OffsetUnits`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`
- Materials: 5; serialized consumer files: 3; scene consumers: 2.
- Runtime/mesh summary: 5 variants / 5 links / 2 environments; Gaga and Metallica; 16 renderer slots.
- Audit: `Assets/_Graphics/Shaders/LIGHTNING_REAUDIT.md`
- Gap: Frame capture required; eight Metallica origin/target transforms are missing in current data.

### `ChroMapper/Lit`

- Path: `Assets/_Graphics/Shaders/Lit.shader`
- Suspected original: `Custom/SimpleLit`
- Family / confidence: `environment_lit` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite [_ZWrite]; Stencil; offset * offset,
- Properties (318): `_Color`, `_AvatarComputeSkinning`, `_Secondary_UVs`, `_InstancedSecondaryTiling`, `_InstancedSecondaryOffset`, `_UVScale`, `_AdditiveUVOffset`, `_InputUvMultiplier`, `_EnableMetalSmoothnessTex`, `_MetalSmoothnessTex`, `_SecondaryUVsMPM`, `_EnableCustomMPMMip`, `_MpmMipBias`, `_Metallic_Texture`, `_Metallic`, `_Smoothness_Texture`, `_Smoothness`, `_SpecularAntiflicker`, `_AntiflickerStrength`, `_AntiflickerDistanceScale`, `_AntiflickerDistanceOffset`, `_PreciseNormal`, `_VertexMode`, `_EmissionThreshold`, `_EmissionColor`, `_EmissionStrength`, `_EmissionBloomIntensity`, `_Vertex_WhiteBoostType`, `_QuestWhiteboostMultiplier`, `_DisplacementSpatial`, `_DisplacementBidirectional`, `_Spectrogram`, `_DisplacementStrength`, `_DisplacementAxisMultiplier`, `_EnableVertexDisplacementMask`, `_VertexDisplacement_Mask_Source`, `_VertexDisplacementMask`, `_VertexDisplacementMaskSpeed`, `_VertexDisplacementMaskMode`, `_VertexDisplacementMaskMultiplier`, `_VertexDisplacementMaskOffset`, `_VertexDisplacement3DTexture`, `_VertexDisplacement3DTexOffset`, `_VertexDisplacement3DTexPanning`, `_VertexDisplacement3DTexScale`, `_EmissionTexture`, `_Emission_Texture_Source`, `_EmissionTex`, `_EmissionTexSpeed`, `_SecondaryUVsEmissionTex`, `_Emission_Alpha_Source`, `_EmissionBrightness`, `_LookupTextureEmission`, `_EnableEmissionAngleDisappear`, `_EmissionThresholdAngle`, `_EmissionColorType`, `_EmissionTexColor`, `_EmissionGradientTex`, `_EmissionGradientPosition`, `_EmissionGradientPanningSpeed`, `_EmissionGradientIntensity`, `_EmissionTexBloomIntensity`, `_EmissionTexWhiteBoostMultiplier`, `_PulseMask`, `_SecondaryUVsPulseTex`, `_InvertPulseTexture`, `_PulseMultiplyByTexture`, `_PulseWidth`, `_PulseSpeed`, `_PulseSmooth`, `_FlipbookColumns`, `_FlipbookRows`, `_FlipbookNonloopableFrames`, `_FlipbookSpeed`, `_FlipbookBlendingOff`, `_EnableEmissionMask`, `_MaskBlend`, `_EmissionMask`, `_SecondaryUVsMask`, `_EmissionMaskSpeed`, `_EmissionMaskIntensity`, `_EnableSecondaryEmissionMask`, `_Secondary_Mask_Blend`, `_SecondaryEmissionMask`, `_SecondaryUVsMask2`, `_SecondaryEmissionMaskSpeed`, `_SecondaryEmissionMaskIntensity`, `_Emission_Step`, `_EmissionMaskStepValue`, `_EmissionMaskStepWidth`, `_Parallax`, `_EnableReflectedDir`, `_Parallax_Projection`, `_ParallaxColor`, `_ParallaxMap`, `_SecondaryUVsParallax`, `_ParallaxTexSpeed`, `_ParallaxIntensity`, `_ParallaxIntensity_Step`, `_Layers`, `_StartOffset`, `_OffsetStep`, `_Parallax_Iridescence`, `_IridescenceAxesMultiplier`, `_IridescenceTiling`, `_IridescenceColorInfluence`, `_Parallax_Masking`, `_ParallaxMaskingMap`, `_ParallaxMaskSpeed`, `_ParallaxMaskIntensity`, `_RimLight`, `_InvertRimlight`, `_EnableDirectionalRim`, `_RimPerpendicularAxis`, `_RimLightEdgeStart`, `_RimLightColor`, `_RimLightIntensity`, `_RimLightBloomIntensity`, `_Rim_WhiteBoostType`, `_RimLightWhiteboostMultiplier`, `_AmbientMinimalValue`, `_NominalDiffuseLevel`, `_AmbientMultiplier`, `_EnableDiffuse`, `_EnableLightFalloff`, `_InvertDiffuseNormal`, `_EnableBothSidesDiffuse`, `_BothSidesDiffuseMultiplier`, `_PrivatePointLight`, `_InstancedPrivatePointLightColor`, `_PrivatePointLightColor`, `_PointLightPositionLocal`, `_PrivatePointLightIntensity`, `_PrivatePointLightPosition`, `_EnableDiffuseTexture`, `_Diffuse_Texture_Source`, `_DiffuseTex`, `_SecondaryUVsDiffuse`, `_AlbedoMultiplier`, `_EnableSpecular`, `_SpecularIntensity`, `_EnableLightmap`, `_EnableNormalMap`, `_NormalTex`, `_SecondaryUVsNormal`, `_NormalScale`, `_UseSphericalNormalOffset`, `_SphericalNormalOffsetIntensity`, `_SphericalNormalOffsetCenter`, `_EnableReflectionTexture`, `_ReflectionTexIntensity`, `_EnvironmentReflectionCube`, `_EnableReflectionProbe`, `_Probe_Calculation`, `_ReflectionProbeDisabledWhiteboost`, `_ReflectionProbeGrayscale`, `_ColoredMetalMultiplier`, `_WhiteOffset`, `_ReflectionProbeIntensity`, `_ReflectionProbeBoxProjection`, `_EnableBoxProjectionOffset`, `_ReflectionProbeBoxProjectionSizeOffset`, `_ReflectionProbeBoxProjectionPositionOffset`, `_ReflectionStatic`, `_ReflectionSingleCubemap`, `_MultiplyReflections`, `_EnableRimDim`, `_RimScale`, `_RimOffset`, `_RimDistanceOffset`, `_RimDistanceScale`, `_RimSmoothness`, `_RimDarkening`, `_InvertRimDim`, `_EnableGroundFade`, `_GroundFadeScale`, `_GroundFadeOffset`, `_EnableOcclusion`, `_Occlusion_Source`, `_DirtTex`, `_SecondaryUVsOcclusion`, `_OcclusionIntensity`, `_EnableOcclusionDetail`, `_DirtDetailTex`, `_SecondaryUVsOcclusionDetail`, `_OcclusionDetailIntensity`, `_OcclusionBeforeEmission`, `_EnableRotateUV`, `_RotateUV`, `_UVColorSegments`, `_UvSegmentsIgnoreRim`, `_HighlightSelection`, `_SegmentToHighlight`, `_EnableFog`, `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_EnableHeightFogSoften`, `_FogSoften`, `_FogSoftenOffset`, `_EmissionFogSuppression`, `_MainEffectFogSuppression`, `_ColorFog`, `_ColorFogMultiplier`, `_ColorFogMax`, `_ColorFogInfluence`, `_FogColorHighlight`, `_ColorFogHighlightMultiplier`, `_EnableDistanceDarkening`, `_DarkeningScale`, `_DarkeningIntensity`, `_DarkeningCenter`, `_DarkeningDirection`, `_Hologram`, `_UseHologramMaterialization`, `_HologramColor`, `_HologramGridSize`, `_HologramFill`, `_HologramStripeSpeed`, `_HologramScanDistance`, `_HologramPhaseOffset`, `_HoloMaterialize`, `_HoloIntensity`, `_HaltScan`, `_EnableFakeMirrorTransparency`, `_FakeMirrorTransparency`, `_NoteVertexDistortion`, `Note_Plane_Cut`, `_CutPlaneEdgeGlowWidth`, `_NoteSize`, `_CutPlane`, `Cutout_Type`, `_Cutout`, `_CutoutTexScale`, `_EnableCloseToCameraCutout`, `_CloseToCameraCutoutOffset`, `_CloseToCameraCutoutScale`, `_GlowCutoutColor`, `_EnableDissolve`, `_DissolveAlpha`, `_AlphaMultiplier`, `_DissolveScale`, `_DissolveReverse`, `_Dissolve_Space`, `_FadeStartY`, `_FadeEndY`, `_FadeZoneInterceptX`, `_FadeZoneSlope`, `_BodyFadeGamma`, `_DissolveAxisVector`, `_UseDissolveProgress`, `_DissolveOffset`, `_DissolveStartValue`, `_DissolveEndValue`, `_DissolveProgress`, `_UseDissolveColor`, `_DissolveColor`, `_DissolveColorIntensity`, `_CutColorFalloff`, `_CutColorBacksideFalloff`, `_Dissolve_Grid`, `_GridThickness`, `_GridSize`, `_GridFalloff`, `_GridSpeed`, `_UseDissolveTexture`, `_DissolveTexture`, `_DissolveTextureSpeed`, `_DissolveTextureInfluence`, `Distortion`, `_Distortion_Target`, `_DistortionTex`, `_SecondaryUVsDistortion`, `_DistortionStrength`, `_DistortionAxes`, `_DistortionPanning`, `_EnableNoiseDithering`, `_LinearToGamma`, `_Custom_Time`, `_Curve_Vertices`, `_Aces_Approach`, `_Texture3D_Lookup`, `_LookupTex`, `_LookupGridSize`, `_LookupXYZDisplacementScale`, `_LookupXDisplacementMapping`, `_LookupYDisplacementMapping`, `_LookupZDisplacementMapping`, `_LookupRadialDisplacementScale`, `_LookupRadialDisplacementMapping`, `_LookupMaxScale`, `_LookupScaleMapping`, `_LookupRotationMultiplier`, `_LookupRotationMapping`, `_LookupEmissiveMapping`, `_LookupEmissiveModulationStrength`, `_CullMode`, `_ZWrite`, `_ZTest`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`, `_MeshPacking`, `_MeshPackingId`, `_UseColorArray`, `_SDFNoiseOffset`, `_SDFNoisePanning`, `_SDFNoiseIntensity`, `_SDFNoiseScale`, `_SDFPointIntensity`, `_SDFNegativeIntensity`, `_SDFNoiseTex`
- Keywords (65): `_`, `STEREO_INSTANCING_ON`, `_SECONDARY_UVS_IMPORT`, `_SECONDARY_UVS_EXTERNAL_SCALE`, `_SECONDARY_UVS_OBJECT_SPACE`, `_SECONDARY_UVS_ADDITIVE_OFFSET`, `PRECISE_NORMAL`, `_VERTEXMODE_COLOR`, `_VERTEXMODE_EMISSION`, `\`, `_VERTEX_WHITEBOOSTTYPE_MAINEFFECT`, `MESH_PACKING`, `_EMISSIONTEXTURE_SIMPLE`, `_EMISSIONTEXTURE_PULSE`, `SECONDARY_UVS_EMISSION`, `SECONDARY_UVS_PULSE`, `SECONDARY_UVS_EMISSION_MASK`, `SECONDARY_UVS_EMISSION_MASK2`, `PRIVATE_POINT_LIGHT`, `DIFFUSE`, `SPECULAR`, `INVERT_RIM_DIM`, `_PARALLAX_FLEXIBLE_REFLECTED`, `PARALLAX_IRIDESCENCE`, `SECONDARY_UVS_PARALLAX`, `NOISE_DITHERING`, `REFLECTION_TEXTURE`, `REFLECTION_PROBE`, `_CUSTOM_TIME_SONG_TIME`, `_CUSTOM_TIME_FREEZE`, `COLOR_ARRAY`, `UV_COLOR_SEGMENTS`, `HIGHLIGHT_SELECTION`, `_HOLOGRAM_GRID`, `_HOLOGRAM_SCANLINE`, `_HOLOGRAM_LEGACY`, `LIGHTMAP`, `COLOR_BY_FOG`, `DIRECTIONAL_RIM`, `DISSOLVE_TEXTURE`, `EMISSION_ANGLE_DISAPPEAR`, `RIM_DIM`, `FOG_COLOR_HIGHLIGHT`, `INSTANCED_PRIVATE_POINT_LIGHT`, `NORMAL_MAP`, `OCCLUSION_BEFORE_EMISSION`, `OCCLUSION_DETAIL`, `REFLECTION_STATIC`, `SECONDARY_UVS_MPM`, `SECONDARY_UVS_OCCLUSION`, `SECONDARY_UVS_OCCLUSION_DETAIL`, `SPECULAR_ANTIFLICKER`, `TEXTURE3D_EMISSION`, `TEXTURE3D_LOOKUP`, `USE_SPHERICAL_NORMAL_OFFSET`, `_DISSOLVE_SPACE_WORLD_CENTERED`, `_DISTORTION_TARGET_EMISSIONTEX`, `_EMISSIONCOLORTYPE_GRADIENT`, `_METALLIC_TEXTURE_MPM_R`, `_OCCLUSION_SOURCE_MPM_B`, `_PROBE_CALCULATION_PRECISE`, `_RIMLIGHT_LERP`, `_RIMLIGHT_ADDITIVE`, `_RIM_WHITEBOOSTTYPE_MAINEFFECT`, `_SMOOTHNESS_TEXTURE_MPM_A`
- Includes: `UnityCG.cginc`, `Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc`, `ShaderLibrary/Data.hlsl`, `ShaderLibrary/Camera.hlsl`, `ShaderLibrary/Time.hlsl`, `ShaderLibrary/Lighting.hlsl`, `ShaderLibrary/Reflection.hlsl`, `ShaderLibrary/Bloom.hlsl`, `ShaderLibrary/PostProcess.hlsl`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Tonemapping.hlsl`
- Materials: 505; serialized consumer files: 52; scene consumers: 48.
- Runtime/mesh summary: 313 variants / 413 links / 47 environments; 505 .mat assets; 5,760 renderer slots / 248 meshes.
- Audit: `Assets/_Graphics/Shaders/LIT_REAUDIT.md`
- Gap: Sixteen executable routes lack recovered fragment binaries; runtime globals/MPBs and visual parity are not proven; pass 1 is unproven.

### `ChroMapper/Mirror`

- Path: `Assets/_Graphics/Shaders/Mirror.shader`
- Suspected original: `Custom/Mirror`
- Family / confidence: `mirror` / **high**
- Passes: 1
- Current render-state declarations: Cull Back; ZTest LEqual; ZWrite On; Stencil
- Properties (25): `_NormalTex`, `_BumpIntensity`, `_ReflectionIntensity`, `_TextureScrolling`, `_Metallic`, `_Smoothness`, `_DetailNormalMap`, `_DetailNormalTextureScale`, `_DetailNormalIntensity`, `_DetailNormalTexScrolling`, `_EnableLightmap`, `_EnableDiffuse`, `_EnableLightFalloff`, `_EnableSpecular`, `_SpecularIntensity`, `_EnableDirt`, `_DirtTex`, `_DirtIntensity`, `_TintColor`, `_FogStartOffset`, `_FogScale`, `_ReflectionTex`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Lighting.hlsl`, `ShaderLibrary/Tonemapping.hlsl`, `ShaderLibrary/PostProcess.hlsl`
- Materials: 42; serialized consumer files: 47; scene consumers: 45.
- Runtime/mesh summary: 20 variants / 80 links / 45 environments; 42 .mat assets; 69 renderer slots / 26 meshes.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No standalone audit; stereo reflection-atlas behavior and omitted routes need runtime/visual validation.

### `ChroMapper/Object/Arc`

- Path: `Assets/_Graphics/Shaders/Object/Arc.shader`
- Suspected original: `Custom/SliderNoteCrossedStrips`
- Family / confidence: `object_adapter` / **low-medium**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite [_ZWrite]
- Properties (18): `_Color`, `_MainTex`, `_FadeSize`, `_Rotation`, `_EnableFog`, `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightOffset`, `_FogHeightScale`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`, `_BlendOp`, `_CullMode`, `_ZTest`, `_ZWrite`
- Keywords (0): none
- Includes: `UnityCG.cginc`, `../ShaderLibrary/Camera.hlsl`, `../ShaderLibrary/Fog.hlsl`, `../ShaderLibrary/Bloom.hlsl`, `../ShaderLibrary/ObjectShared.hlsl`
- Materials: 1; serialized consumer files: 1; scene consumers: 0.
- Runtime/mesh summary: SplineArc.prefab Visual renderer.
- Audit: `inline shader evidence and/or README mapping`
- Gap: Mapping/formula comments only; no dedicated versioned binary audit or visual capture.

### `ChroMapper/Object/Note`

- Path: `Assets/_Graphics/Shaders/Object/Note.shader`
- Suspected original: `Custom/Note (NoteHD/NoteLW recovered union)`
- Family / confidence: `object_adapter_recovered` / **medium-high**
- Passes: 1
- Current render-state declarations: Cull [_CullMode]; ZWrite [_ZWrite]; Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]; Stencil
- Properties (48): `_Smoothness`, `_NoteSize`, `_Color`, `_ColorMultiplier`, `_FakeMirrorTransparencyEnabled`, `_FakeMirrorTransparencyMultiplier`, `_EnableReflectionMap`, `_EnvironmentReflectionCube`, `_FogType`, `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_PreciseFog`, `_EnableCutout`, `_Cutout`, `_CutoutTexOffset`, `_CutoutTexScale`, `_EnablePlaneCut`, `_CutPlaneEdgeGlowWidth`, `_CutPlane`, `_EnableRimDim`, `_RimScale`, `_RimOffset`, `_RimCameraDistanceOffset`, `_RimCameraDistanceScale`, `_RimDarkening`, `_WhiteBoostType`, `_CullMode`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_BlendSrcFactor`, `_BlendDstFactor`, `_BlendSrcFactorA`, `_BlendDstFactorA`, `_ZWrite`, `_MainTex`, `_AnimationSpawned`, `_ObjectTime`, `_Rotation`, `_StrobeColor`, `_StrobeColorEnabled`, `_OutlineWidth`, `_OverNoteInterfaceColor`, `_AlwaysTranslucent`, `_TranslucentAlpha`
- Keywords (9): `PLANE_CUT`, `ZWRITE`, `FAKE_MIRROR_TRANSPARENCY`, `_`, `CUTOUT`, `REFLECTION_MAP`, `RIM_DIM`, `POST_BLOOM`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`, `../ShaderLibrary/Camera.hlsl`, `../ShaderLibrary/Fog.hlsl`, `../ShaderLibrary/Bloom.hlsl`, `../ShaderLibrary/Tonemapping.hlsl`, `../ShaderLibrary/ObjectShared.hlsl`
- Materials: 5; serialized consumer files: 17; scene consumers: 0.
- Runtime/mesh summary: Use GUID-linked material/serialized consumers; no direct Shader.Find ownership established.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No dedicated Note audit or committed visual parity fixture.

### `ChroMapper/Object/Obstacle Distortion`

- Path: `Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader`
- Suspected original: `Custom/ScreenDisplacementHD`
- Family / confidence: `object_adapter_recovered` / **medium-high**
- Passes: 1
- Current render-state declarations: Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest LEqual; ZWrite [_ZWrite]
- Properties (35): `_MainTex`, `_DisplacementStrength`, `_CullMode`, `_Color`, `_TintColor`, `_AddColor`, `_DisplacementAlphaMul`, `_ScaleUV`, `_UVScale`, `_ScrollUV`, `_ScrollUVVelocity`, `_EnableFog`, `_FogStartOffset`, `_FogScale`, `_FogHeightScale`, `_FogHeightOffset`, `_ZWrite`, `_ClipLowAlpha`, `_ViewAngleAffectsDistortion`, `_ViewAngleDistortionParam`, `_UseDistortedTextureOnly`, `_DepthAwareDistortion`, `_EnableCutout`, `_CutoutTexScale`, `_CutoutTexOffset`, `_Cutout`, `_EnableRimDim`, `_RimDimScale`, `_RimDimOffset`, `_EnableClipping`, `_BlendSrcFactor`, `_BlendDstFactor`, `_BlendOp`, `_BlendSrcFactorA`, `_BlendDstFactorA`
- Keywords (0): none
- Includes: `UnityCG.cginc`, `../ShaderLibrary/Camera.hlsl`, `../ShaderLibrary/Fog.hlsl`, `../ShaderLibrary/Cutout.hlsl`
- Materials: 1; serialized consumer files: 1; scene consumers: 0.
- Runtime/mesh summary: Obstacle.prefab distortObstacle; runtime ScreenDisplacementRenderer.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No standalone audit; runtime grab/depth texture and renderer-property ownership need capture.

### `ChroMapper/Object/Obstacle Outline`

- Path: `Assets/_Graphics/Shaders/Object/ObstacleOutline.shader`
- Suspected original: `Custom/ParametricBoxFrameHD role only; not a binary-proven full replacement`
- Family / confidence: `object_adapter` / **medium**
- Passes: 1
- Current render-state declarations: Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite [_ZWrite]
- Properties (19): `_FogStartOffset`, `_FogScale`, `_FogHeightScale`, `_FogHeightOffset`, `_CullMode`, `_WhiteBoostType`, `_EnableCutout`, `_CutoutTexScale`, `_BlendSrcFactor`, `_BlendDstFactor`, `_BlendSrcFactorA`, `_BlendDstFactorA`, `_ZTest`, `_ZWrite`, `_Color`, `_WorldScale`, `_Cutout`, `_CutoutTexOffset`, `_SizeParams`
- Keywords (2): `_`, `POST_BLOOM`
- Includes: `UnityCG.cginc`, `../ShaderLibrary/Fog.hlsl`, `../ShaderLibrary/Bloom.hlsl`, `../ShaderLibrary/Cutout.hlsl`
- Materials: 1; serialized consumer files: 2; scene consumers: 0.
- Runtime/mesh summary: GeometryAppearanceSO and Obstacle.prefab Outline.
- Audit: `inline shader evidence and/or README mapping`
- Gap: Not a binary-proven complete FrameHD replacement; source bindings and layout remain unproven.

### `ChroMapper/Parametric Box Fake Glow`

- Path: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader`
- Suspected original: `Custom/ParametricBoxFakeGlow`
- Family / confidence: `parametric` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp Add; Cull Off; ZTest LEqual; ZWrite Off
- Properties (16): `_MainTex`, `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_AngleDisappearParam`, `_WhiteBoostType`, `_EnableCutout`, `_WorldspaceNoiseCutout`, `_CutoutTexScale`, `_EnableClipping`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`
- Keywords (9): `_`, `STEREO_INSTANCING_ON`, `BLOOM_FOG`, `HEIGHT_FOG`, `MAIN_EFFECT_WHITE_BOOST`, `_WHITEBOOSTTYPE_MAINEFFECT`, `_WHITEBOOSTTYPE_ALWAYS`, `CUTOUT`, `POST_BLOOM`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Bloom.hlsl`, `ShaderLibrary/Cutout.hlsl`, `ShaderLibrary/ParametricShared.hlsl`
- Materials: 1; serialized consumer files: 46; scene consumers: 45.
- Runtime/mesh summary: 1 variant / 45 links / 45 environments; RectangleFakeGlow mesh family.
- Audit: `Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Parametric Box Opaque`

- Path: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader`
- Suspected original: `Custom/OpaqueNeonLight`
- Family / confidence: `parametric` / **high**
- Passes: 1
- Current render-state declarations: Cull Back; ZTest LEqual; ZWrite On
- Properties (5): `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`
- Keywords (3): `_`, `STEREO_INSTANCING_ON`, `POST_BLOOM`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Camera.hlsl`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Bloom.hlsl`, `ShaderLibrary/PostProcess.hlsl`, `ShaderLibrary/ParametricShared.hlsl`
- Materials: 12; serialized consumer files: 23; scene consumers: 21.
- Runtime/mesh summary: 11 variants / 32 links / 21 environments; 12 .mat assets; 1,142 renderer slots / 11 meshes.
- Audit: `Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Parametric Box Transparent`

- Path: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader`
- Suspected original: `Custom/TransparentNeonLight`
- Family / confidence: `parametric` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest LEqual; ZWrite Off; Stencil
- Properties (41): `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_EnableWorldNoise`, `_WorldNoiseScale`, `_WorldNoiseIntensityOffset`, `_WorldNoiseIntensityScale`, `_WorldNoiseScrolling`, `_WorldNoiseDistort`, `_NoiseWarpZoomStrength`, `_NoiseWarpSkewStrength`, `_EnableWorldSpaceFade`, `_WorldSpaceFadePos`, `_WorldSpaceFadeSlope`, `_EnableSpecular`, `_SpecularIntensity`, `_SpecularHardness`, `_EnableNormalMap`, `_NormalTex`, `_NormalScale`, `_EnableReflectionProbe`, `_Smoothness`, `_ReflectionIntensity`, `_GlassOpacity`, `_EnableRimDim`, `_RimScale`, `_RimOffset`, `_RimDistanceOffset`, `_RimDistanceScale`, `_InvertRimDim`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`, `_CullMode`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_BlendOp`
- Keywords (4): `_`, `STEREO_INSTANCING_ON`, `REFLECTION_PROBE`, `POST_BLOOM`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Bloom.hlsl`, `ShaderLibrary/ParametricShared.hlsl`, `ShaderLibrary/Reflection.hlsl`
- Materials: 23; serialized consumer files: 49; scene consumers: 47.
- Runtime/mesh summary: 17 variants / 67 links / 47 environments; 23 .mat assets; 3,157 renderer slots / 4 meshes.
- Audit: `Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Parametric Slice Billboard`

- Path: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader`
- Suspected original: `Custom/Parametric3SliceSprite`
- Family / confidence: `parametric` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite Off; Offset [_OffsetFactor], [_OffsetUnits]; Stencil
- Properties (40): `_MainTex`, `_CapUVSize`, `_OffsetFactor`, `_OffsetUnits`, `_EnableFog`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_UseFogForLights`, `_FogStartOffset`, `_FogScale`, `_EnableWorldNoise`, `_WorldNoiseScale`, `_WorldNoiseIntensityOffset`, `_WorldNoiseIntensityScale`, `_WorldNoiseScrolling`, `_WorldNoiseSkew`, `_NoiseWarpZoomStrength`, `_NoiseWarpSkewStrength`, `_EnableWorldSpaceFade`, `_WorldSpaceFadePos`, `_WorldSpaceFadeSlope`, `_EnableAlphaWidthScale`, `_WhiteBoostType`, `_BloomWhiteMultiplier`, `_BloomMultiplier`, `_SquareAlpha`, `_EnableEmissionAngleDisappear`, `_EnableNoiseDithering`, `_EnableYAxisBillboard`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`, `_ZTest`, `_CullMode`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_BlendOp`
- Keywords (3): `_`, `STEREO_INSTANCING_ON`, `POST_BLOOM`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Camera.hlsl`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Bloom.hlsl`, `ShaderLibrary/ParametricShared.hlsl`, `ShaderLibrary/PostProcess.hlsl`
- Materials: 71; serialized consumer files: 39; scene consumers: 38.
- Runtime/mesh summary: 45 variants / 123 links / 47 environments; 71 .mat assets; 3,169 renderer slots; 3SliceSprite asset.
- Audit: `Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Particles`

- Path: `Assets/_Graphics/Shaders/Particles.shader`
- Suspected original: `Custom/CustomParticles`
- Family / confidence: `particles` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite [_ZWrite]; Offset [_OffsetFactor], [_OffsetUnits]; Stencil
- Properties (238): `_Color`, `_EnableObstacle`, `_Fog_Mask_Source`, `_ObstacleFogMultiplier`, `_ObstacleFogMax`, `_ObstacleColorInfluence`, `_FogColorHighlight`, `_ObstacleFogHighlightMultiplier`, `_EnableSecondaryColor`, `_SecondaryColor`, `_SecondaryColorTex`, `_SecondaryColorPanning`, `_UseColorGradient`, `_ColorGradient`, `_GradientUseAlpha`, `_GradientPosition`, `_GradientPanningSpeed`, `_UseSpectrogram`, `_SpectrogramBaseValue`, `_SpectrogramRange`, `_UseColorArray`, `_Secondary_UVs`, `_UVScale`, `_UVManualOffset`, `_EnableRotateUV`, `_RotateUV`, `_RotateMainUVOnly`, `_EnableWorldSpacePanning`, `_WorldspacePanningSpeed`, `_UsesParticleVertexStream`, `_EnableStartEnd`, `_AlphaStart`, `_AlphaEnd`, `_WidthStart`, `_WidthEnd`, `_EnableVertexColor`, `_SquareVertexAlpha`, `_RedIsVertexAlpha`, `_VertexChannels`, `_EnableLifetime`, `_EnableVertexFlipbook`, `_VertexFlipbookCount`, `_VertexFlipbookSpeed`, `_EnableVertexFlipbookFade`, `_VertexDisplacement`, `_DisplacementSecondaryUVs`, `_DisplacementTex`, `_3DDisplacement`, `_DisplacementPerParticleRandomization`, `_DisplacementStrength`, `_DisplacementAxes`, `_DisplacementPanningSpeed`, `_DisplacementPanning`, `_Spectrogram`, `_UV3Offset`, `_UV3Scale`, `_Curve_Vertices`, `_UseMainTex`, `_BaseLayer`, `_MainTex`, `_MainTexSecondaryUVs`, `_EnableMainTexWorldSpacePanning`, `_Pixelate`, `_PixelateResolution`, `_EnableTextureColor`, `_AlphaChannel`, `_TopBotFadeAngle`, `_LeftRightFadeAngle`, `_Diagonal_Channel`, `_MainPerParticleRandomization`, `_Intensity`, `_UvPanning`, `_EnableCustomPadding`, `_CustomPadding`, `_UseTextureFlipbook`, `_FlipbookColumns`, `_FlipbookRows`, `_FlipbookNonloopableFrames`, `_FlipbookSpeed`, `_FlipbookBlendingOff`, `_UseMotionVectors`, `_MotionVectorTex`, `_MotionVectorColumns`, `_MotionVectorRows`, `_MotionVectorSpeed`, `_MotionVectorIntensity`, `_EnableMask`, `_MaskSecondaryUVs`, `_MaskRedIsAlpha`, `_MaskBlend`, `_MaskTex`, `_MaskPerParticleRandomization`, `_MaskStrength`, `_MaskTexWorldspacePanning`, `_MaskPanning`, `_MaskDissolve`, `_MaskDissolveTiling`, `_MaskDissolveOffset`, `_EnableMask2`, `_Mask2SecondaryUVs`, `_Mask2TexWorldspacePanning`, `_Mask2RedIsAlpha`, `_Mask2Blend`, `_Mask2Tex`, `_Mask2ParticleRandomization`, `_Mask2Strength`, `_Mask2Panning`, `_RimLight`, `_RimlightInvert`, `_RimLightEdgeStart`, `_RimLightIntensity`, `_EnableWorldNoise`, `_WorldNoiseScale`, `_WorldNoiseIntensityOffset`, `_WorldNoiseIntensityScale`, `_WorldNoiseScrolling`, `_Erosion`, `_Erosion_Source`, `_ErosionSecondaryUVs`, `_ErosionTexWorldspacePanning`, `_ErosionPerParticleRandomization`, `_ErosionTex`, `_ErosionPanning`, `_ErosionVertexThreshold`, `_ErosionThreshold`, `_ErosionSmoothness`, `Distortion`, `Distortion_Target`, `_FlowmapSecondaryUVs`, `_FlowmapTexWorldspacePanning`, `_FlowTex`, `_FlowmapParticleRandomization`, `_FlowSpeed`, `_FlowStrength`, `_FlowAdd`, `_FlowPanning`, `_DistortionSecondaryUVs`, `_DistortionTexWorldspacePanning`, `_DistortionParticleRandomization`, `_DistortionTex`, `_DistortionStrength`, `_DistortionAxes`, `_DistortionPanning`, `_FogType`, `_FogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_PreciseFog`, `_EnableDissolve`, `_DissolveScale`, `_DissolveReverse`, `_Dissolve_Space`, `_DissolveAxisVector`, `_UseDissolveProgress`, `_DissolveOffset`, `_DissolveStartValue`, `_DissolveEndValue`, `_DissolveProgressFromVertexAlpha`, `_DissolveProgress`, `_UseDissolveColor`, `_DissolveColor`, `_DissolveColorIntensity`, `_CutColorFalloff`, `_MultiplyDissolveGridByAlpha`, `_Dissolve_Grid`, `_GridThickness`, `_GridSize`, `_GridFalloff`, `_GridSpeed`, `_UseDissolveTexture`, `_DissolveTexture`, `_DissolveTextureSpeed`, `_DissolveTextureInfluence`, `_PlaneClipping`, `_ClippingPlanePosition`, `_ClippingPlaneNormal`, `_EnableReveal`, `_CutoutType`, `_Cutout`, `_CutoutTexScale`, `_CutoutGradientWidth`, `_CutoutTexOffset`, `_EnableFakeMirrorTransparency`, `_FakeMirrorTransparency`, `_EnableVertexDistortion`, `_EnableHologram`, `_HologramColor`, `_AlphaMultiplier`, `_SquareAlpha`, `_EnableFillAlpha`, `_FillAlpha`, `_FillMask`, `_FillColor`, `_EnableCloseToCameraDisappear`, `_CloseCameraDisappearDistance`, `_CloseCameraDisappearWidth`, `_EnableViewAlignDisappear`, `_SquareAngleForViewAlignDisappear`, `_ViewAlignFactor`, `_ViewAlignOffset`, `_EnableSoftParticles`, `_SoftFactor`, `_Override_Final_Alpha`, `_OverrideFinalAlpha`, `_WhiteBoostType`, `_QuestWhiteboostMultiplier`, `_BloomMultiplier`, `_GreenChannelWhiteboost`, `_RemapWhiteboostStart`, `_WhiteBoostRemapStart`, `_Billboard`, `_BillboardScale`, `_EnableNoiseDithering`, `_Custom_Time`, `_EnableMipmapBias`, `_MipmapBias`, `_UseChromaticAberration`, `_ChromaticAberration`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendOp`, `_BlendModeSrcA`, `_BlendModeDstA`, `_CullMode`, `_ZWrite`, `_ZTest`, `_OffsetFactor`, `_OffsetUnits`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_MeshPacking`, `_MeshPackingId`, `_BloomPreset`, `_BlendingPreset`, `_StencilPreset`
- Keywords (43): `_`, `STEREO_INSTANCING_ON`, `SECONDARY_COLOR`, `COLOR_GRADIENT`, `COLOR_ARRAY`, `_SECONDARY_UVS_IMPORT`, `_SECONDARY_UVS_EXTERNAL_SCALE`, `SECONDARY_UVS_MAIN`, `WORLDSPACE_PANNING`, `VERTEX_COLOR`, `VERTEX_SQUARE_ALPHA`, `MAIN_TEXTURE`, `TEXTURE_FLIPBOOK`, `FLIPBOOK_BLENDING_OFF`, `MASK`, `SECONDARY_UVS_MASK`, `MASK_RED_IS_ALPHA`, `_MASKBLEND_ADD`, `_MASKBLEND_MASKED_ADD`, `MASK2`, `SECONDARY_UVS_MASK2`, `MASK2_RED_IS_ALPHA`, `_MASK2BLEND_ADD`, `_MASK2BLEND_MASKED_ADD`, `DISTORTION_SIMPLE`, `SECONDARY_UVS_DISTORTION`, `WORLDSPACE_PANNING_DISTORTION`, `VIEW_ALIGN_DISAPPEAR`, `SOFT_PARTICLES`, `DISSOLVE`, `_DISSOLVE_SPACE_WORLD`, `_DISSOLVE_SPACE_WORLD_CENTERED`, `DISSOLVE_PROGRESS_FROM_VERTEX_ALPHA`, `VERTEX_FLIPBOOK`, `VERTEX_FLIPBOOK_FADE`, `MIPMAP_BIAS`, `NOISE_DITHERING`, `MAIN_PER_PARTICLE_RANDOM`, `POST_BLOOM`, `DEPTH_TEXTURE`, `DEPTH_TEXTURE_ENABLED`, `_CUSTOM_TIME_SONG_TIME`, `_CUSTOM_TIME_FREEZE`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Bloom.hlsl`, `ShaderLibrary/Time.hlsl`, `ShaderLibrary/SpectrogramShared.hlsl`, `ShaderLibrary/PostProcess.hlsl`
- Materials: 220; serialized consumer files: 48; scene consumers: 47.
- Runtime/mesh summary: 154 variants / 370 links / 47 environments; 220 .mat assets; 5,343 renderer slots / 61 meshes.
- Audit: `Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md`
- Gap: Runtime/mesh parity and visual comparison remain unproven; unsupported zero-row selectors have no invented formula.

### `ChroMapper/Post Process/Bloom`

- Path: `Assets/_Graphics/Shaders/Post Process/Bloom.shader`
- Suspected original: `Beat Saber bloom chain (exact source ShaderLab name unrecorded)`
- Family / confidence: `post_process` / **medium-high**
- Passes: 14
- Current render-state declarations: Cull Off ZWrite Off ZTest Always
- Properties (1): `_MainTex`
- Keywords (0): none
- Includes: `UnityCG.cginc`, `../ShaderLibrary/BloomShared.hlsl`
- Materials: 0; serialized consumer files: 0; scene consumers: 0.
- Runtime/mesh summary: Use GUID-linked material/serialized consumers; no direct Shader.Find ownership established.
- Audit: `inline shader evidence and/or README mapping`
- Gap: Exact original ShaderLab identity is unrecorded; no committed visual capture.

### `ChroMapper/Post Process/Post Bloom`

- Path: `Assets/_Graphics/Shaders/Post Process/PostBloom.shader`
- Suspected original: `Hidden/MainEffect`
- Family / confidence: `post_process` / **high**
- Passes: 1
- Current render-state declarations: Cull Off; ZWrite Off; ZTest Always
- Properties (3): `_MainTex`, `_BloomIntensity`, `_Fade`
- Keywords (4): `_`, `STEREO_INSTANCING_ON`, `LIV_MR`, `CLEAR_SCREEN_ALPHA`
- Includes: `UnityCG.cginc`, `../ShaderLibrary/Bloom.hlsl`
- Materials: 0; serialized consumer files: 0; scene consumers: 0.
- Runtime/mesh summary: Use GUID-linked material/serialized consumers; no direct Shader.Find ownership established.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No standalone audit or committed frame/reference capture.

### `ChroMapper/Rain`

- Path: `Assets/_Graphics/Shaders/Rain.shader`
- Suspected original: `Custom/Rain`
- Family / confidence: `environment_fx` / **medium**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; BlendOp [_BlendOp]; Cull [_CullMode]; ZTest [_ZTest]; ZWrite Off; Offset [_OffsetFactor], [_OffsetUnits]; Stencil
- Properties (61): `_Height`, `_Speed`, `_BottomFadeScale`, `_TopFadeScale`, `_BottomEnd`, `_TopEnd`, `_Color`, `_EnableColorGradient`, `_ColorGradient`, `_MainTex`, `_EnableTextureColor`, `_AlphaChannel`, `_Intensity`, `_UvPanning`, `_EnableMask`, `_MaskAdditive`, `_MaskRedIsAlpha`, `_MaskTex`, `_MaskPanning`, `_MaskStrength`, `_EnableMask2`, `_Mask2RedIsAlpha`, `_Mask2Tex`, `_Mask2Panning`, `_Mask2MinValue`, `_EnableSoftParticles`, `_SoftFactor`, `_EnableCloseToCameraDisappear`, `_CloseCameraDisappearDistance`, `_CloseCameraDisappearWidth`, `_EnableViewAlignDisappear`, `_ViewAlignFactor`, `_EnableVertexColor`, `_SquareVertexAlpha`, `_RedIsVertexAlpha`, `_VertexChannels`, `_EnableLifetime`, `_FogType`, `_FogStartOffset`, `_FogScale`, `_AlphaFromFog`, `_EnableHeightFog`, `_PreciseFog`, `_EnableHologram`, `_HologramColor`, `_SquareAlpha`, `_AlphaMultiplier`, `_WhiteBoostType`, `_EnableNoiseDithering`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`, `_BlendOp`, `_CullMode`, `_ZTest`, `_OffsetFactor`, `_OffsetUnits`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`
- Keywords (6): `TEXTURE_COLOR`, `_ALPHACHANNEL_RED`, `MASK_RED_IS_ALPHA`, `VERTEX_COLOR`, `VERTEX_SQUARE_ALPHA`, `_FOGTYPE_COLOR`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Camera.hlsl`, `ShaderLibrary/Fog.hlsl`
- Materials: 1; serialized consumer files: 2; scene consumers: 1.
- Runtime/mesh summary: 1 variant / 1 link / 1 environment; Billie Rain Instance mesh.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No dedicated audit, corpus counts, version, binary hashes, capture, or screenshot.

### `ChroMapper/Set Depth Only`

- Path: `Assets/_Graphics/Shaders/SetDepthOnly.shader`
- Suspected original: `Custom/SetDepthOnly`
- Family / confidence: `depth_stencil` / **high**
- Passes: 1
- Current render-state declarations: Tags {; Blend Zero One, Zero One; ZWrite [_ZWrite]; Cull Off; Stencil {
- Properties (4): `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_ZWrite`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`
- Materials: 2; serialized consumer files: 36; scene consumers: 35.
- Runtime/mesh summary: 2 variants / 35 links / 35 environments; 25 meshes.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No standalone audit or visual capture; ShaderLab state cannot be proven from stage ASM alone.

### `ChroMapper/Spectrogram`

- Path: `Assets/_Graphics/Shaders/Spectrogram.shader`
- Suspected original: `Custom/Spectrogram`
- Family / confidence: `spectrogram` / **high**
- Passes: 1
- Current render-state declarations: Cull Back; ZTest LEqual; ZWrite [_ZWrite]
- Properties (11): `_Color`, `_PeakOffset`, `_Metallic`, `_Smoothness`, `_EnableDiffuse`, `_EnableSpecular`, `_SpecularIntensity`, `_EnableLightFalloff`, `_FogStartOffset`, `_FogScale`, `_ZWrite`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Lighting.hlsl`, `ShaderLibrary/Tonemapping.hlsl`, `ShaderLibrary/PostProcess.hlsl`, `ShaderLibrary/SpectrogramShared.hlsl`
- Materials: 34; serialized consumer files: 15; scene consumers: 14.
- Runtime/mesh summary: 13 variants / 14 links / 14 environments; 34 .mat assets; 58 renderer slots / 4 meshes.
- Audit: `Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Spectrogram Unlit`

- Path: `Assets/_Graphics/Shaders/SpectrogramUnlit.shader`
- Suspected original: `Custom/UnlitSpectrogram`
- Family / confidence: `spectrogram` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; Cull Off; ZTest LEqual; ZWrite Off
- Properties (8): `_Color`, `_SpectrogramScale`, `_FogStartOffset`, `_FogScale`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/SpectrogramShared.hlsl`
- Materials: 3; serialized consumer files: 3; scene consumers: 2.
- Runtime/mesh summary: 2 variants / 2 links / 2 environments; Interscope and Linkin Park; 4 renderer slots / 2 meshes.
- Audit: `Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md`
- Gap: No committed visual parity capture was found.

### `ChroMapper/Stencil`

- Path: `Assets/_Graphics/Shaders/Stencil.shader`
- Suspected original: `Custom/SimpleStencil`
- Family / confidence: `depth_stencil` / **high**
- Passes: 1
- Current render-state declarations: Blend Zero One, Zero One; ZWrite Off; Cull [_CullMode]; Stencil
- Properties (4): `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_CullMode`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`
- Materials: 2; serialized consumer files: 3; scene consumers: 2.
- Runtime/mesh summary: 2 variants / 2 links / 2 environments; Lattice stencil plane and Lizzo tubes.
- Audit: `inline shader evidence and/or README mapping`
- Gap: No standalone audit; render-state parity relies on source shell/established state, not stage ASM.

### `ChroMapper/Water Lit`

- Path: `Assets/_Graphics/Shaders/WaterLit.shader`
- Suspected original: `Custom/WaterLit`
- Family / confidence: `water` / **high**
- Passes: 1
- Current render-state declarations: Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]; Cull [_CullMode]; ZTest LEqual; ZWrite [_ZWrite]; Stencil
- Properties (111): `_Color`, `_EnableMetalSmoothnessTex`, `_MetalSmoothnessTex`, `_Metallic`, `_Smoothness`, `_SpecularAntiflicker`, `_AntiflickerStrength`, `_AntiflickerDistanceScale`, `_AntiflickerDistanceOffset`, `_VertexMode`, `_EmissionThreshold`, `_EmissionColor`, `_ZFade`, `_ZFadePosition`, `_ZFadeScale`, `_YFade`, `_YFadePosition`, `_YFadeScale`, `_EmissionTexture`, `_EmissionBrightness`, `_EmissionColorType`, `_EmissionTexColor`, `_EmissionGradientTex`, `_EmissionTex`, `_EmissionTexSpeed`, `_EmissionSampleTwice`, `_Emission2Tiling`, `_Emission2Speed`, `_PulseMask`, `_InvertPulseTexture`, `_PulseMultiplyByTexture`, `_PulseWidth`, `_PulseSpeed`, `_PulseSmooth`, `_EnableEmissionMask`, `_EmissionMask`, `_EmissionMaskSpeed`, `_RimLight`, `_RimLightEdgeStart`, `_RimLightColor`, `_RimLightIntensity`, `_EnableDiffuse`, `_EnableLightFalloff`, `_InvertDiffuseNormal`, `_EnableBothSidesDiffuse`, `_PrivatePointLight`, `_PrivatePointLightColor`, `_PointLightPositionLocal`, `_PrivatePointLightPosition`, `_EnableDiffuseTexture`, `_DiffuseTex`, `_EnableSpecular`, `_SpecularIntensity`, `_EnableLightmap`, `_EnableNormalMap`, `_NormalTex`, `_NormalScale`, `_NormalScaleVertical`, `_NormalTexScrolling`, `_DetailNormalMap`, `_DetailNormalTextureScale`, `_DetailNormalIntensity`, `_DetailNormalTexScrolling`, `_UseSphericalNormalOffset`, `_SphericalNormalOffsetIntensity`, `_SphericalNormalOffsetCenter`, `_EnableReflectionTexture`, `_ReflectionTexIntensity`, `_EnvironmentReflectionCube`, `_EnableReflectionProbe`, `_ReflectionProbeIntensity`, `_ReflectionProbeBoxProjection`, `_EnableBoxProjectionOffset`, `_ReflectionProbeBoxProjectionSizeOffset`, `_ReflectionProbeBoxProjectionPositionOffset`, `_EnableRimDim`, `_RimScale`, `_RimOffset`, `_RimDistanceOffset`, `_RimDistanceScale`, `_RimDarkening`, `_InvertRimDim`, `_EnableGroundFade`, `_GroundFadeScale`, `_GroundFadeOffset`, `_EnableDirt`, `_DirtTex`, `_DirtIntensity`, `_EnableDirtDetail`, `_DirtDetailTex`, `_DirtDetailIntensity`, `_RotateUV`, `_EnableFog`, `_FogStartOffset`, `_FallingFogStartOffset`, `_FogScale`, `_EnableHeightFog`, `_FogHeightScale`, `_FogHeightOffset`, `_WhiteBoostType`, `_EnableNoiseDithering`, `_LinearToGamma`, `_CullMode`, `_ZWrite`, `_StencilRefValue`, `_StencilComp`, `_StencilPass`, `_BlendModeSrc`, `_BlendModeDst`, `_BlendModeSrcA`, `_BlendModeDstA`
- Keywords (2): `_`, `STEREO_INSTANCING_ON`
- Includes: `UnityCG.cginc`, `ShaderLibrary/Fog.hlsl`, `ShaderLibrary/Reflection.hlsl`, `ShaderLibrary/Tonemapping.hlsl`, `ShaderLibrary/PostProcess.hlsl`
- Materials: 4; serialized consumer files: 3; scene consumers: 2.
- Runtime/mesh summary: 4 variants / 4 links / 2 environments; Billie waterfalls and Gaga logo; 3 renderer slots / 3 meshes.
- Audit: `Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md`
- Gap: Packed dual-cubemap reflection route is not implemented because required probe assets/data are null.

## Excluded shader groups

- AudioLink package shaders/includes: third-party package sources, not Beat Saber recoveries.
- TextMesh Pro shaders/includes/ShaderGraphs: third-party TMP sources.
- UiRoundedCorners: plugin sources.
- Editor grid/UI/selection/outline/helper shaders and environment ShaderGraphs: project-authored adapters/tools unless separately cataloged above.
- `Unlit.shader`, `GrabPassTexture1.shader`, `ObstacleSimple.shader`, `GagaArc.shader`, `Legacy/Standard.shader`, Sprite/TMP shaders: no recovery evidence sufficient for inclusion.

See `recovered_shader_catalog.json` for complete properties, keywords, states, exact material paths, GUID-linked consumer files and scenes.
