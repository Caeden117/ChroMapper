# Recovered Shader Pipelines

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their records stay in `RecoveredShaderFeatureMatrix.csv` with an `adjacent_unverified` status.

## Reading order

Each section preserves the parser array order. The numeric values are the zero-based source order from the ABI snapshot.

The stage-flow records preserve the order from the broader behavior audit. These records describe static evidence, not runtime or visual parity.

Twelve catalog shaders have different catalog and behavior-matrix source hashes. Each affected section marks this cross-snapshot join as unverified.

## 1. `ChroMapper/BloomfogMesh`

- Path: `Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader`
- Source SHA-256: `81d70bb56299b43ae2eb41d69118a238c8462d2cf69e338bd9f3de2a3c7e446d`
- Behavior matrix source SHA-256: `81d70bb56299b43ae2eb41d69118a238c8462d2cf69e338bd9f3de2a3c7e446d`
- Source join: matched.
- ABI SHA-256: `a38983d1f4361c1e9651aebbce5d0a185dede9b54a2dfb70c9165253443cf66a`
- Classification: `recovered_replacement`
- Family and confidence: `bloom_fog` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex -> 1:_BlendSrcFactor -> 2:_BlendDstFactor -> 3:_BlendSrcFactorA -> 4:_BlendDstFactorA -> 5:_BlendOp.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque], states [ZWrite Off | Cull Off | Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA] | BlendOp [_BlendOp]], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc].
   - Pragmas: vertex vert -> fragment frag -> multi_compile _ BLOOM_FOG.

### Audited stage flow

1. `BF-MESH` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Global matrix transforms the mesh; cubic gamma-to-linear transfer changes RGB while preserving vertex alpha; line-mask sampling and BLOOM_FOG distance transmission produce premultiplied output.
   - Evidence: Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader:63-110; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/BloomPrePassLine/_shader.json
   - Limits: BLOOM_FOG is a compile-time branch; the tangent/line-mask path remains mesh-specific.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).

### Catalog limits

- Exact original ShaderLab identity is unrecorded; no standalone audit/capture.

## 2. `ChroMapper/BloomfogSkybox`

- Path: `Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader`
- Source SHA-256: `a3e676238c255225a456cd92a6b254ab610719eaeb30267c441d06e5215060ea`
- Behavior matrix source SHA-256: `a3e676238c255225a456cd92a6b254ab610719eaeb30267c441d06e5215060ea`
- Source join: matched.
- ABI SHA-256: `064a2c4f23a70bf2e895b094b94f900dde9a1560682b960170205ca0e18ce603`
- Classification: `recovered_replacement`
- Family and confidence: `bloom_fog` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: none.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque, Queue=Geometry+100], states [Cull Off | ZWrite Off | ZTest LEqual], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [3.0], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl].
   - Pragmas: target 3.0 -> vertex vert -> fragment frag -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_instancing.

### Audited stage flow

1. `BF-SKYBOX` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Builds a fullscreen skybox quad, samples the bloom prepass only when BLOOM_FOG is enabled, applies global blue-noise dither, and clears alpha.
   - Evidence: Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader:53-93; Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader:3-8; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/BloomSkyboxQuad/_shader.json
   - Limits: OVERDRAW_VIEW is intentionally omitted.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).

### Catalog limits

- Original identity is only suspected from inline comparison; no standalone audit/capture.

## 3. `ChroMapper/Sky Gradient`

- Path: `Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader`
- Source SHA-256: `07fa546ee9a1a6f7bbf10e40f0af798663894aba75f3f71da26209c1d13a459f`
- Behavior matrix source SHA-256: `07fa546ee9a1a6f7bbf10e40f0af798663894aba75f3f71da26209c1d13a459f`
- Source join: matched.
- ABI SHA-256: `a4f23e62b12dea7082a5d259da2b6f40c57ec59e004c144293ec7e73d0afb667`
- Classification: `recovered_replacement`
- Family and confidence: `bloom_fog` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_GradientTex -> 1:_Color.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Background, Queue=Background], states [Cull Off | ZWrite Off | ZTest Always | Blend One One, Zero Zero], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [3.5], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl].
   - Pragmas: vertex vert -> fragment frag -> target 3.5 -> multi_compile_fragment _ USE_TONE_MAPPING -> multi_compile_fragment _ ACES_TONE_MAPPING.

### Audited stage flow

1. `BF-SKYGRADIENT` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Uses SV_VertexID to build the fullscreen triangle, unprojects a camera ray, samples the gradient by normalized ray Y, and optionally applies ACES tone mapping.
   - Evidence: Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader:43-79; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/SkyGradient/_shader.json
   - Limits: The source is a Hidden/SkyGradient replacement; ShaderLab state is not established by stage binaries.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).

### Catalog limits

- Exact original ShaderLab identity is unrecorded; no standalone audit/capture.

## 4. `ChroMapper/Clouds Lit Transparent`

- Path: `Assets/_Graphics/Shaders/CloudsLitTransparent.shader`
- Source SHA-256: `6512f8d07ce060939385ec75ce7d296f1416fc668610c71ad87d41040555b2ec`
- Behavior matrix source SHA-256: `6512f8d07ce060939385ec75ce7d296f1416fc668610c71ad87d41040555b2ec`
- Source join: matched.
- ABI SHA-256: `eb82934a07cc50abb0f7078f0b40d3752dd2f6c31da5eb71da2de14e3a928057`
- Classification: `recovered_replacement`
- Family and confidence: `clouds` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_DiffuseTex -> 2:_DistortTex -> 3:_DistortTexSpeed -> 4:_DistortAmount -> 5:_DistortUVChannel -> 6:_BackLightingBoost -> 7:_FadeBottomMin -> 8:_FadeBottomMax -> 9:_RunwayFadeOffset -> 10:_RunwayFadeScale -> 11:_RotateLayerSpeeds -> 12:_VertexWaveFrequency -> 13:_VertexWaveAmplitude -> 14:_EnableFog -> 15:_FogStartOffset -> 16:_FogScale -> 17:_EnableHeightFog -> 18:_AlignNormalsToWorldOrigin -> 19:_EnableBackLighting -> 20:_EnableDiffuse -> 21:_EnableDiffuseTexture -> 22:_EnableDistortTexture -> 23:_EnableBottomFade -> 24:_EnableFadeRunway -> 25:_EnableVertexWave -> 26:_VertexMode -> 27:_BlendModeSrc -> 28:_BlendModeDst -> 29:_BlendModeSrcA -> 30:_BlendModeDstA.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | ZWrite Off | ZTest LEqual | Cull Off], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ObjectShared.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local ALIGN_NORMALS_TO_WORLD_ORIGIN -> shader_feature_local_fragment BACK_LIGHTING -> shader_feature_local_fragment DIFFUSE -> shader_feature_local_fragment DIFFUSE_TEXTURE -> shader_feature_local_fragment DISTORT_TEXTURE -> shader_feature_local_fragment FADE_BOTTOM -> shader_feature_local_fragment FADE_RUNWAY -> shader_feature_local_vertex VERTEX_WAVE -> shader_feature_local_vertex _VERTEXMODE_ROTATELAYERS -> multi_compile _ ACES_TONE_MAPPING.

### Audited stage flow

1. `CLOUD-TRANSPARENT` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Rotation-layer weights and vertex wave deform the cloud sheet; the complete Billie route samples distortion then diffuse, adds front/back five-light terms, applies bottom/runway fades, and preserves separate RGB/alpha blend factors.
   - Evidence: Assets/_Graphics/Shaders/CloudsLitTransparent.shader:215-310; Assets/_Graphics/Shaders/CLOUD_REAUDIT.md:33-44; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/CloudsLitTransparent/_shader.json
   - Limits: Partial keyword bundles fall back to the generic color route; an extra no-keyword hash has an unproven pass role.
1. `CLOUD-UNVERIFIED` / `fragment/pass role` / `UNVERIFIED`: Two no-keyword transparent-cloud rows share an extra hash whose pass role and required vertex contract are not proven.
   - Evidence: Assets/_Graphics/Shaders/CLOUD_REAUDIT.md:18-22; Assets/_Graphics/Shaders/CLOUD_REAUDIT.md:46-55
   - Limits: Current source retains the generic route rather than inventing a hybrid formula.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/CloudsLitTransparent.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/CloudsLitTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/CloudsLitTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ObjectShared.hlsl` (`3925230d685628cc84bba102b9bf73abbc062e1b0357606b40e8a9b7e82d3203`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/CloudsLitTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).

### Catalog limits

- No committed visual parity capture was found.

## 5. `ChroMapper/Clouds Opaque`

- Path: `Assets/_Graphics/Shaders/CloudsOpaque.shader`
- Source SHA-256: `84e51f737af0df8ed45dfd6e1744f6c0f3ca6a369b0187f88b1438e98b7a4b4e`
- Behavior matrix source SHA-256: `84e51f737af0df8ed45dfd6e1744f6c0f3ca6a369b0187f88b1438e98b7a4b4e`
- Source join: matched.
- ABI SHA-256: `a3c978a36e56e980f9faec2440c89468b717915724dfc54e2e2f95645148079a`
- Classification: `recovered_replacement`
- Family and confidence: `clouds` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex -> 1:_NoiseTex -> 2:_WorldNoiseScale -> 3:_WorldNoiseIntensityScale -> 4:_WorldNoiseIntensityOffset -> 5:_WorldNoiseScrolling -> 6:_Speed -> 7:_Offset -> 8:_FogStartOffset -> 9:_FogScale -> 10:_HeightFogOffset -> 11:_CullMode -> 12:_EnableDiffuse -> 13:_EnableBothSidesDiffuse -> 14:_InvertDiffuseNormal -> 15:_EnableWorldNoise -> 16:_EnableFog -> 17:_EnableNoiseDithering.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque, Queue=Geometry], states [Cull [_CullMode] | ZWrite On | ZTest LEqual], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_fragment DIFFUSE -> shader_feature_local_fragment BOTH_SIDES_DIFFUSE -> shader_feature_local_vertex WORLD_NOISE -> shader_feature_local_fragment INVERT_DIFFUSE_NORMAL -> shader_feature_local_fragment FOG -> shader_feature_local_fragment NOISE_DITHERING -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ ACES_TONE_MAPPING.

### Audited stage flow

1. `CLOUD-OPAQUE` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Optional world-noise swirl/displacement, inverted-origin normal, five-light diffuse, height/distance fog, optional ACES and blue-noise dither; alpha is always zero.
   - Evidence: Assets/_Graphics/Shaders/CloudsOpaque.shader:164-265; Assets/_Graphics/Shaders/CLOUD_REAUDIT.md:24-31; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/CloudsOpaque/_shader.json
   - Limits: OVERDRAW_VIEW and absent source routes are omitted.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/CloudsOpaque.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/CloudsOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/CloudsOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/CloudsOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 10 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 11 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 12 at depth 0: `Assets/_Graphics/Shaders/CloudsOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).
1. Include 13 at depth 0: `Assets/_Graphics/Shaders/CloudsOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).

### Catalog limits

- No committed visual parity capture was found.

## 6. `ChroMapper/Glowing`

- Path: `Assets/_Graphics/Shaders/Glowing.shader`
- Source SHA-256: `474c58f22ab992d71c614ca2c4e890cccc148018fce95b1c253b357ba163f74d`
- Behavior matrix source SHA-256: `f4557f5acf5ca0c4db452786c5b372c6ad7b150cdd59b95550f9c762637bbdb3`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `36bebf3d00057f40eb8ce074280d7f187e6413dd1eaead120bce8f89a514fa6f`
- Classification: `recovered_replacement`
- Family and confidence: `fallback` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_EnableColorInstancing -> 1:_Color -> 2:_FogStartOffset -> 3:_FogScale -> 4:_CUTOUT -> 5:_Cutout -> 6:_CutoutTexScale -> 7:_CutoutTexOffset -> 8:_CutoutTex -> 9:_WhiteBoostType -> 10:_NoiseDithering.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Geometry, RenderType=Opaque], states [Cull Back | ZTest LEqual | ZWrite On], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT -> multi_compile _ POST_BLOOM -> multi_compile_fragment _ BLOOM_FOG.

### Audited stage flow

1. `GLOWING` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Returns material _Color, optionally applies main-effect white boost, then applies bloom fog using alpha-weighted start/scale and the bloom prepass.
   - Evidence: Assets/_Graphics/Shaders/Glowing.shader:114-136; Assets/_Graphics/Shaders/Glowing.shader:3-24; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Glowing/_shader.json
   - Limits: Geometry fallback role is project policy; direct Custom/Glowing recovery evidence exists.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Glowing.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Glowing.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/Glowing.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Glowing.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).

### Catalog limits

- No committed visual parity capture was found.

## 7. `ChroMapper/Lightning`

- Path: `Assets/_Graphics/Shaders/Lightning.shader`
- Source SHA-256: `71fc66b5dbbf52e9dbaafe719024294e35e552299a48aa30e00fd5b024ed61a9`
- Behavior matrix source SHA-256: `b5fa1d06f28807918e6c10af5c41115707ca1ee4f1c190fe0780eb904c2d58a1`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `5b6373449ee2c8bbf8830150e46b04f63b7ac77d6503449c4da3ac1f1a05a257`
- Classification: `recovered_replacement`
- Family and confidence: `environment_fx` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_MainTex -> 2:_NoiseTex -> 3:_TimingTex -> 4:_NoiseSmallScale -> 5:_SmallScaleNoiseStrength -> 6:_SmallScaleNoiseScrollingSpeed -> 7:_NoiseBigScale -> 8:_BigScaleNoiseStrength -> 9:_BigScaleNoiseScrollingSpeed -> 10:_NoiseScrollingSpeed -> 11:_XNoiseOffsetStrength -> 12:_Extrude -> 13:_ColorBoost -> 14:_WhiteBoost -> 15:_EdgeFadeStrength -> 16:_TargetPoint -> 17:_EnableTargetPoint -> 18:_EnableTimeOffset -> 19:_TimeOffset -> 20:_BlendModeSrc -> 21:_BlendModeDst -> 22:_BlendModeSrcA -> 23:_BlendModeDstA -> 24:_BlendOp -> 25:_CullMode -> 26:_ZTest -> 27:_OffsetFactor -> 28:_OffsetUnits -> 29:_StencilRefValue -> 30:_StencilComp -> 31:_StencilPass.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent, IgnoreProjector=True, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite Off | Offset [_OffsetFactor], [_OffsetUnits]], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_vertex _ ENABLE_TARGET_POINT -> shader_feature_local_vertex _ ENABLE_TIME_OFFSET.

### Audited stage flow

1. `LIGHTNING` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Target-point conversion, two scrolling noise scales, timing-texture edge fade, extrusion, main texture/color/alpha, bloom and white-boost output; instancing/stereo variants are present.
   - Evidence: Assets/_Graphics/Shaders/Lightning.shader:1-2; Assets/_Graphics/Shaders/LIGHTNING_REAUDIT.md:5-40; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SimpleLightning/_shader.json
   - Limits: Runtime time alignment is proven separately; Metallica zero-length source transforms are a data defect.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Lightning.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).

### Catalog limits

- Frame capture required; eight Metallica origin/target transforms are missing in current data.

## 8. `ChroMapper/Lit`

- Path: `Assets/_Graphics/Shaders/Lit.shader`
- Source SHA-256: `a6867bcb0e3cac93e336d453312ede5bf988c0544aff1c0051e02c9bfe7a1152`
- Behavior matrix source SHA-256: `a8afd7b2974244691b41d93e2a151734487d55a7ac0fc0e5fd3fca4895fac1f6`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `5ab0bce583d40b26484353ee328a91700433fa8ac2ceeb78493e4ae92e30f6f3`
- Classification: `recovered_replacement`
- Family and confidence: `environment_lit` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_AvatarComputeSkinning -> 2:_Secondary_UVs -> 3:_InstancedSecondaryTiling -> 4:_InstancedSecondaryOffset -> 5:_UVScale -> 6:_AdditiveUVOffset -> 7:_InputUvMultiplier -> 8:_EnableMetalSmoothnessTex -> 9:_MetalSmoothnessTex -> 10:_SecondaryUVsMPM -> 11:_EnableCustomMPMMip -> 12:_MpmMipBias -> 13:_Metallic_Texture -> 14:_Metallic -> 15:_Smoothness_Texture -> 16:_Smoothness -> 17:_SpecularAntiflicker -> 18:_AntiflickerStrength -> 19:_AntiflickerDistanceScale -> 20:_AntiflickerDistanceOffset -> 21:_PreciseNormal -> 22:_VertexMode -> 23:_EmissionThreshold -> 24:_EmissionColor -> 25:_EmissionStrength -> 26:_EmissionBloomIntensity -> 27:_Vertex_WhiteBoostType -> 28:_QuestWhiteboostMultiplier -> 29:_DisplacementSpatial -> 30:_DisplacementBidirectional -> 31:_Spectrogram -> 32:_DisplacementStrength -> 33:_DisplacementAxisMultiplier -> 34:_EnableVertexDisplacementMask -> 35:_VertexDisplacement_Mask_Source -> 36:_VertexDisplacementMask -> 37:_VertexDisplacementMaskSpeed -> 38:_VertexDisplacementMaskMode -> 39:_VertexDisplacementMaskMultiplier -> 40:_VertexDisplacementMaskOffset -> 41:_VertexDisplacement3DTexture -> 42:_VertexDisplacement3DTexOffset -> 43:_VertexDisplacement3DTexPanning -> 44:_VertexDisplacement3DTexScale -> 45:_EmissionTexture -> 46:_Emission_Texture_Source -> 47:_EmissionTex -> 48:_EmissionTexSpeed -> 49:_SecondaryUVsEmissionTex -> 50:_Emission_Alpha_Source -> 51:_EmissionBrightness -> 52:_LookupTextureEmission -> 53:_EnableEmissionAngleDisappear -> 54:_EmissionThresholdAngle -> 55:_EmissionColorType -> 56:_EmissionTexColor -> 57:_EmissionGradientTex -> 58:_EmissionGradientPosition -> 59:_EmissionGradientPanningSpeed -> 60:_EmissionGradientIntensity -> 61:_EmissionTexBloomIntensity -> 62:_EmissionTexWhiteBoostMultiplier -> 63:_PulseMask -> 64:_SecondaryUVsPulseTex -> 65:_InvertPulseTexture -> 66:_PulseMultiplyByTexture -> 67:_PulseWidth -> 68:_PulseSpeed -> 69:_PulseSmooth -> 70:_FlipbookColumns -> 71:_FlipbookRows -> 72:_FlipbookNonloopableFrames -> 73:_FlipbookSpeed -> 74:_FlipbookBlendingOff -> 75:_EnableEmissionMask -> 76:_MaskBlend -> 77:_EmissionMask -> 78:_SecondaryUVsMask -> 79:_EmissionMaskSpeed -> 80:_EmissionMaskIntensity -> 81:_EnableSecondaryEmissionMask -> 82:_Secondary_Mask_Blend -> 83:_SecondaryEmissionMask -> 84:_SecondaryUVsMask2 -> 85:_SecondaryEmissionMaskSpeed -> 86:_SecondaryEmissionMaskIntensity -> 87:_Emission_Step -> 88:_EmissionMaskStepValue -> 89:_EmissionMaskStepWidth -> 90:_Parallax -> 91:_EnableReflectedDir -> 92:_Parallax_Projection -> 93:_ParallaxColor -> 94:_ParallaxMap -> 95:_SecondaryUVsParallax -> 96:_ParallaxTexSpeed -> 97:_ParallaxIntensity -> 98:_ParallaxIntensity_Step -> 99:_Layers -> 100:_StartOffset -> 101:_OffsetStep -> 102:_Parallax_Iridescence -> 103:_IridescenceAxesMultiplier -> 104:_IridescenceTiling -> 105:_IridescenceColorInfluence -> 106:_Parallax_Masking -> 107:_ParallaxMaskingMap -> 108:_ParallaxMaskSpeed -> 109:_ParallaxMaskIntensity -> 110:_RimLight -> 111:_InvertRimlight -> 112:_EnableDirectionalRim -> 113:_RimPerpendicularAxis -> 114:_RimLightEdgeStart -> 115:_RimLightColor -> 116:_RimLightIntensity -> 117:_RimLightBloomIntensity -> 118:_Rim_WhiteBoostType -> 119:_RimLightWhiteboostMultiplier -> 120:_AmbientMinimalValue -> 121:_NominalDiffuseLevel -> 122:_AmbientMultiplier -> 123:_EnableDiffuse -> 124:_EnableLightFalloff -> 125:_InvertDiffuseNormal -> 126:_EnableBothSidesDiffuse -> 127:_BothSidesDiffuseMultiplier -> 128:_PrivatePointLight -> 129:_InstancedPrivatePointLightColor -> 130:_PrivatePointLightColor -> 131:_PointLightPositionLocal -> 132:_PrivatePointLightIntensity -> 133:_PrivatePointLightPosition -> 134:_EnableDiffuseTexture -> 135:_Diffuse_Texture_Source -> 136:_DiffuseTex -> 137:_SecondaryUVsDiffuse -> 138:_AlbedoMultiplier -> 139:_EnableSpecular -> 140:_SpecularIntensity -> 141:_EnableLightmap -> 142:_EnableNormalMap -> 143:_NormalTex -> 144:_SecondaryUVsNormal -> 145:_NormalScale -> 146:_UseSphericalNormalOffset -> 147:_SphericalNormalOffsetIntensity -> 148:_SphericalNormalOffsetCenter -> 149:_EnableReflectionTexture -> 150:_ReflectionTexIntensity -> 151:_EnvironmentReflectionCube -> 152:_EnableReflectionProbe -> 153:_Probe_Calculation -> 154:_ReflectionProbeDisabledWhiteboost -> 155:_ReflectionProbeGrayscale -> 156:_ColoredMetalMultiplier -> 157:_WhiteOffset -> 158:_ReflectionProbeIntensity -> 159:_ReflectionProbeBoxProjection -> 160:_EnableBoxProjectionOffset -> 161:_ReflectionProbeBoxProjectionSizeOffset -> 162:_ReflectionProbeBoxProjectionPositionOffset -> 163:_ReflectionStatic -> 164:_ReflectionSingleCubemap -> 165:_MultiplyReflections -> 166:_EnableRimDim -> 167:_RimScale -> 168:_RimOffset -> 169:_RimDistanceOffset -> 170:_RimDistanceScale -> 171:_RimSmoothness -> 172:_RimDarkening -> 173:_InvertRimDim -> 174:_EnableGroundFade -> 175:_GroundFadeScale -> 176:_GroundFadeOffset -> 177:_EnableOcclusion -> 178:_Occlusion_Source -> 179:_DirtTex -> 180:_SecondaryUVsOcclusion -> 181:_OcclusionIntensity -> 182:_EnableOcclusionDetail -> 183:_DirtDetailTex -> 184:_SecondaryUVsOcclusionDetail -> 185:_OcclusionDetailIntensity -> 186:_OcclusionBeforeEmission -> 187:_EnableRotateUV -> 188:_RotateUV -> 189:_UVColorSegments -> 190:_UvSegmentsIgnoreRim -> 191:_HighlightSelection -> 192:_SegmentToHighlight -> 193:_EnableFog -> 194:_FogStartOffset -> 195:_FogScale -> 196:_EnableHeightFog -> 197:_FogHeightScale -> 198:_FogHeightOffset -> 199:_EnableHeightFogSoften -> 200:_FogSoften -> 201:_FogSoftenOffset -> 202:_EmissionFogSuppression -> 203:_MainEffectFogSuppression -> 204:_ColorFog -> 205:_ColorFogMultiplier -> 206:_ColorFogMax -> 207:_ColorFogInfluence -> 208:_FogColorHighlight -> 209:_ColorFogHighlightMultiplier -> 210:_EnableDistanceDarkening -> 211:_DarkeningScale -> 212:_DarkeningIntensity -> 213:_DarkeningCenter -> 214:_DarkeningDirection -> 215:_Hologram -> 216:_UseHologramMaterialization -> 217:_HologramColor -> 218:_HologramGridSize -> 219:_HologramFill -> 220:_HologramStripeSpeed -> 221:_HologramScanDistance -> 222:_HologramPhaseOffset -> 223:_HoloMaterialize -> 224:_HoloIntensity -> 225:_HaltScan -> 226:_EnableFakeMirrorTransparency -> 227:_FakeMirrorTransparency -> 228:_NoteVertexDistortion -> 229:Note_Plane_Cut -> 230:_CutPlaneEdgeGlowWidth -> 231:_NoteSize -> 232:_CutPlane -> 233:Cutout_Type -> 234:_Cutout -> 235:_CutoutTexScale -> 236:_EnableCloseToCameraCutout -> 237:_CloseToCameraCutoutOffset -> 238:_CloseToCameraCutoutScale -> 239:_GlowCutoutColor -> 240:_EnableDissolve -> 241:_DissolveAlpha -> 242:_AlphaMultiplier -> 243:_DissolveScale -> 244:_DissolveReverse -> 245:_Dissolve_Space -> 246:_FadeStartY -> 247:_FadeEndY -> 248:_FadeZoneInterceptX -> 249:_FadeZoneSlope -> 250:_BodyFadeGamma -> 251:_DissolveAxisVector -> 252:_UseDissolveProgress -> 253:_DissolveOffset -> 254:_DissolveStartValue -> 255:_DissolveEndValue -> 256:_DissolveProgress -> 257:_UseDissolveColor -> 258:_DissolveColor -> 259:_DissolveColorIntensity -> 260:_CutColorFalloff -> 261:_CutColorBacksideFalloff -> 262:_Dissolve_Grid -> 263:_GridThickness -> 264:_GridSize -> 265:_GridFalloff -> 266:_GridSpeed -> 267:_UseDissolveTexture -> 268:_DissolveTexture -> 269:_DissolveTextureSpeed -> 270:_DissolveTextureInfluence -> 271:Distortion -> 272:_Distortion_Target -> 273:_DistortionTex -> 274:_SecondaryUVsDistortion -> 275:_DistortionStrength -> 276:_DistortionAxes -> 277:_DistortionPanning -> 278:_EnableNoiseDithering -> 279:_LinearToGamma -> 280:_Custom_Time -> 281:_Curve_Vertices -> 282:_Aces_Approach -> 283:_Texture3D_Lookup -> 284:_LookupTex -> 285:_LookupGridSize -> 286:_LookupXYZDisplacementScale -> 287:_LookupXDisplacementMapping -> 288:_LookupYDisplacementMapping -> 289:_LookupZDisplacementMapping -> 290:_LookupRadialDisplacementScale -> 291:_LookupRadialDisplacementMapping -> 292:_LookupMaxScale -> 293:_LookupScaleMapping -> 294:_LookupRotationMultiplier -> 295:_LookupRotationMapping -> 296:_LookupEmissiveMapping -> 297:_LookupEmissiveModulationStrength -> 298:_CullMode -> 299:_ZWrite -> 300:_ZTest -> 301:_StencilRefValue -> 302:_StencilComp -> 303:_StencilPass -> 304:_BlendModeSrc -> 305:_BlendModeDst -> 306:_BlendModeSrcA -> 307:_BlendModeDstA -> 308:_MeshPacking -> 309:_MeshPackingId -> 310:_UseColorArray -> 311:_SDFNoiseOffset -> 312:_SDFNoisePanning -> 313:_SDFNoiseIntensity -> 314:_SDFNoiseScale -> 315:_SDFPointIntensity -> 316:_SDFNegativeIntensity -> 317:_SDFNoiseTex.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite [_ZWrite]], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Time.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE _SECONDARY_UVS_OBJECT_SPACE _SECONDARY_UVS_ADDITIVE_OFFSET -> shader_feature_local_fragment METAL_SMOOTHNESS_TEXTURE -> shader_feature_local_fragment _ _METALLIC_TEXTURE_SOURCE_MPM_R _METALLIC_TEXTURE_SOURCE_MPM_A -> shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_SOURCE_MPM_A _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS -> shader_feature_local PRECISE_NORMAL -> shader_feature_local _ _VERTEXMODE_COLOR _VERTEXMODE_EMISSION  _VERTEXMODE_METALSMOOTHNESS _VERTEXMODE_SPECIAL _VERTEXMODE_DISPLACEMENT  _VERTEXMODE_EMISSIVE_MULT_ADD -> shader_feature_local _ _VERTEX_WHITEBOOSTTYPE_MAINEFFECT  _VERTEX_WHITEBOOSTTYPE_ALWAYS -> shader_feature_local_vertex DISPLACEMENT_SPATIAL -> shader_feature_local_vertex DISPLACEMENT_BIDIRECTIONAL -> shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL -> shader_feature_local MESH_PACKING -> shader_feature_local_vertex VERTEXDISPLACEMENT_MASK -> shader_feature_local_vertex _ _VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE  _VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE -> shader_feature_local _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_PULSE  _EMISSIONTEXTURE_FLIPBOOK -> shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_MPM_G -> shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_SDF -> shader_feature_local SECONDARY_UVS_EMISSION -> shader_feature_local SECONDARY_UVS_PULSE -> shader_feature_local_fragment INVERT_PULSE -> shader_feature_local_fragment PULSE_MULTIPLY_TEXTURE -> shader_feature_local_fragment _ _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R -> shader_feature_local_fragment EMISSION_MASK -> shader_feature_local_fragment _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD -> shader_feature_local SECONDARY_UVS_EMISSION_MASK -> shader_feature_local_fragment SECONDARY_EMISSION_MASK -> shader_feature_local_fragment _ _SECONDARY_MASK_BLEND_ADD _SECONDARY_MASK_BLEND_MASKED_ADD -> shader_feature_local SECONDARY_UVS_EMISSION_MASK2 -> shader_feature_local_fragment FLIPBOOK_BLENDING_OFF -> shader_feature_local PRIVATE_POINT_LIGHT -> shader_feature_local_fragment POINT_LIGHT_IS_LOCAL -> shader_feature_local DIFFUSE -> shader_feature_local_fragment BOTH_SIDES_DIFFUSE -> shader_feature_local_fragment LIGHT_FALLOFF -> shader_feature_local_fragment DIFFUSE_TEXTURE -> shader_feature_local_fragment _ _DIFFUSE_TEXTURE_SOURCE_TEXTURE _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS -> shader_feature_local SPECULAR -> shader_feature_local INVERT_RIM_DIM -> shader_feature_local_fragment _ _PARALLAX_FLEXIBLE _PARALLAX_RGB -> shader_feature_local _PARALLAX_FLEXIBLE_REFLECTED -> shader_feature_local_fragment _ _PARALLAX_PROJECTION_WARPED -> shader_feature_local PARALLAX_IRIDESCENCE -> shader_feature_local SECONDARY_UVS_PARALLAX -> shader_feature_local_fragment _ _PARALLAX_MASKING_TEXTURE _PARALLAX_MASKING_VERTEX_COLOR -> shader_feature_local_fragment DISTORTION_SIMPLE -> shader_feature_local NOISE_DITHERING -> shader_feature_local_fragment MULTIPLY_REFLECTIONS -> shader_feature_local REFLECTION_TEXTURE -> shader_feature_local REFLECTION_PROBE -> shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION -> shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET -> shader_feature_local_fragment GROUND_FADE -> shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE -> shader_feature_local_fragment _ _ACES_APPROACH_BEFORE_EMISSIVE -> multi_compile_fragment _ ACES_TONE_MAPPING -> shader_feature_local COLOR_ARRAY -> shader_feature_local UV_COLOR_SEGMENTS -> shader_feature_local HIGHLIGHT_SELECTION -> shader_feature_local _ _HOLOGRAM_GRID _HOLOGRAM_SCANLINE _HOLOGRAM_LEGACY -> shader_feature_local_fragment FOG -> shader_feature_local_fragment HEIGHT_FOG -> shader_feature_local_fragment HEIGHT_FOG_DEPTH_SOFTEN -> shader_feature_local LIGHTMAP -> shader_feature_local_fragment OCCLUSION -> shader_feature_local_fragment DISTANCE_DARKENING -> shader_feature_local_fragment DISSOLVE -> shader_feature_local_fragment DISSOLVE_PROGRESS -> shader_feature_local_fragment DISSOLVE_COLOR -> shader_feature_local COLOR_BY_FOG -> shader_feature_local DIRECTIONAL_RIM -> shader_feature_local DISSOLVE_TEXTURE -> shader_feature_local EMISSION_ANGLE_DISAPPEAR -> shader_feature_local RIM_DIM -> shader_feature_local FOG_COLOR_HIGHLIGHT -> shader_feature_local INSTANCED_PRIVATE_POINT_LIGHT -> shader_feature_local NORMAL_MAP -> shader_feature_local OCCLUSION_BEFORE_EMISSION -> shader_feature_local OCCLUSION_DETAIL -> shader_feature_local REFLECTION_STATIC -> shader_feature_local SECONDARY_UVS_MPM -> shader_feature_local SECONDARY_UVS_OCCLUSION -> shader_feature_local SECONDARY_UVS_OCCLUSION_DETAIL -> shader_feature_local SPECULAR_ANTIFLICKER -> shader_feature_local TEXTURE3D_EMISSION -> shader_feature_local TEXTURE3D_LOOKUP -> shader_feature_local USE_SPHERICAL_NORMAL_OFFSET -> shader_feature_local _DISSOLVE_SPACE_WORLD_CENTERED -> shader_feature_local _DISTORTION_TARGET_EMISSIONTEX -> shader_feature_local _ _EMISSIONCOLORTYPE_GRADIENT  _EMISSIONCOLORTYPE_MAINEFFECT _EMISSIONCOLORTYPE_WHITEBOOST -> shader_feature_local _ _METALLIC_TEXTURE_MPM_R -> shader_feature_local _OCCLUSION_SOURCE_MPM_B -> shader_feature_local _PROBE_CALCULATION_PRECISE -> shader_feature_local _ _RIMLIGHT_LERP _RIMLIGHT_ADDITIVE -> shader_feature_local _RIM_WHITEBOOSTTYPE_MAINEFFECT -> shader_feature_local _ _SMOOTHNESS_TEXTURE_MPM_A  _SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ POST_BLOOM.

### Audited stage flow

1. `LIT` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Large SimpleLit feature family: secondary UVs, material/vertex/emission/mask/displacement paths, five-light lighting, reflection, parallax/dissolve/rim, fog, bloom, ACES, lightmap, instancing and stereo.
   - Evidence: Assets/_Graphics/Shaders/LIT_REAUDIT.md:23-63; Assets/_Graphics/Shaders/LIT_REAUDIT.md:158-170; /tmp/opencode/lit_modularization_20260903/evidence_audit/report.md:7-16; /mnt/programs/Code/GitRepository/ChroMapper/Assets/_Graphics/Shaders/Lit.shader:1-3
   - Limits: Sixteen executable replacement routes have no recovered fragment binary; pass 1 role remains unproven.
1. `LIT-UNVERIFIED` / `fragment routes` / `UNVERIFIED`: Sixteen executable replacement routes have no recovered fragment binary, and recovered pass 1 draw role/state remain unknown.
   - Evidence: Assets/_Graphics/Shaders/LIT_REAUDIT.md:65-78; Assets/_Graphics/Shaders/LIT_REAUDIT.md:109-119
   - Limits: Static audit cannot close these routes.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc` (`f0d6a26e714e8f0da1fd1691226c61a7603280382d3d7c20038f39e59f34f04f`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Time.hlsl` (`c2fd3fed529541e4425652b11ae382758f6c5e559c54755ade3da5885ad68fc4`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 11 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 12 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 13 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` (`f9c1fa70ebb8871b1afd9816689ce2ff33eac1b8a3af60d65756d9a76bd2e544`).
1. Include 14 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 15 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 16 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 17 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).
1. Include 18 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 19 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 20 at depth 0: `Assets/_Graphics/Shaders/Lit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).

### Catalog limits

- Sixteen executable routes lack recovered fragment binaries; runtime globals/MPBs and visual parity are not proven; pass 1 is unproven.

## 9. `ChroMapper/Mirror`

- Path: `Assets/_Graphics/Shaders/Mirror.shader`
- Source SHA-256: `4fd36c4518a0a74b6520325b9a71a2a51e6967c5aeb18a549631c35510242f3f`
- Behavior matrix source SHA-256: `7e8a97d5a039abfd28037f21c96a849be286cf836b53ccd83bbef4a16e57f3ab`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `d30ea48d293a20203ad5f9c1bee5de18d31a2e4febc01cdb38a08a0415ffb6fd`
- Classification: `recovered_replacement`
- Family and confidence: `mirror` / `high`

### Ordered root contract

- Root commands: Fallback "Diffuse".
- Properties: 0:_NormalTex -> 1:_BumpIntensity -> 2:_ReflectionIntensity -> 3:_TextureScrolling -> 4:_Metallic -> 5:_Smoothness -> 6:_DetailNormalMap -> 7:_DetailNormalTextureScale -> 8:_DetailNormalIntensity -> 9:_DetailNormalTexScrolling -> 10:_EnableLightmap -> 11:_EnableDiffuse -> 12:_EnableLightFalloff -> 13:_EnableSpecular -> 14:_SpecularIntensity -> 15:_EnableDirt -> 16:_DirtTex -> 17:_DirtIntensity -> 18:_TintColor -> 19:_FogStartOffset -> 20:_FogScale -> 21:_ReflectionTex -> 22:_StencilRefValue -> 23:_StencilComp -> 24:_StencilPass.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Geometry, RenderType=Opaque, DisableBatching=True], states [Cull Back | ZTest LEqual | ZWrite On], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_fragment LIGHTMAP -> shader_feature_local_fragment DIFFUSE -> shader_feature_local_fragment LIGHT_FALLOFF -> shader_feature_local_fragment DETAIL_NORMAL_MAP -> shader_feature_local_fragment DIRT -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ ACES_TONE_MAPPING.

### Audited stage flow

1. `MIRROR` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Normal/detail-normal scrolling perturbs screen reflection UV; optional diffuse/lightmap lighting and dirt multiply the reflected result; bloom fog and unconditional dither finish the output.
   - Evidence: Assets/_Graphics/Shaders/Mirror.shader:198-275; Assets/_Graphics/Shaders/Mirror.shader:4-27; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Mirror/_shader.json
   - Limits: Game stereo reflection-atlas and OVERDRAW_VIEW routes are omitted by project design.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Mirror.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Mirror.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/Mirror.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 9 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 10 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 11 at depth 0: `Assets/_Graphics/Shaders/Mirror.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).
1. Include 12 at depth 0: `Assets/_Graphics/Shaders/Mirror.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).

### Catalog limits

- No standalone audit; stereo reflection-atlas behavior and omitted routes need runtime/visual validation.

## 10. `ChroMapper/Object/Arc`

- Path: `Assets/_Graphics/Shaders/Object/Arc.shader`
- Source SHA-256: `cc5aaa365ab6dc778b11981fbe91f3e6f3d7f80dde0ea5d9c25abf1814927153`
- Behavior matrix source SHA-256: `737aed8b0feb8dd270c547238f874d33b3c62fbc0b73cec5ddb878df6808b09b`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `4d5fa7c7c97f405211480d20b3d57a6e58b092c4aa836dc671b9f813b12414ab`
- Classification: `recovered_replacement`
- Family and confidence: `object_adapter` / `low-medium`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_MainTex -> 2:_FadeSize -> 3:_Rotation -> 4:_EnableFog -> 5:_FogStartOffset -> 6:_FogScale -> 7:_EnableHeightFog -> 8:_FogHeightOffset -> 9:_FogHeightScale -> 10:_BlendModeSrc -> 11:_BlendModeDst -> 12:_BlendModeSrcA -> 13:_BlendModeDstA -> 14:_BlendOp -> 15:_CullMode -> 16:_ZTest -> 17:_ZWrite.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent+50, IgnoreProjector=True, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite [_ZWrite]], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ObjectShared.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ CM_PREVIEW_MODE -> shader_feature_local_fragment FOG -> shader_feature_local_fragment HEIGHT_FOG.

### Audited stage flow

1. `ARC` / `vertex+fragment` / `SEMANTICALLY_SIMILAR_NOT_SAFE`: Line/arc adapter samples the project arc texture alpha, builds an across-strip edge fade, applies recovered bloom composition and optional fog, and adds editor distance fade.
   - Evidence: Assets/_Graphics/Shaders/Object/Arc.shader:102-183; Assets/_Graphics/Shaders/Object/Arc.shader:135-150; Assets/_Graphics/Shaders/README.md:40-44; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SliderNoteCrossedStrips/_shader.json
   - Limits: The game mesh stores its edge coordinate in TEXCOORD1.w; the adapter uses uv.y because LineRenderer input is different.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Object/Arc.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Object/Arc.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/Object/Arc.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Object/Arc.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/Object/Arc.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ObjectShared.hlsl` (`3925230d685628cc84bba102b9bf73abbc062e1b0357606b40e8a9b7e82d3203`).

### Catalog limits

- Mapping/formula comments only; no dedicated versioned binary audit or visual capture.

## 11. `ChroMapper/Object/Note`

- Path: `Assets/_Graphics/Shaders/Object/Note.shader`
- Source SHA-256: `14f58d505830cedbd830b4ba62837516c046d35b70e6dfcfd0327dd45697dcfe`
- Behavior matrix source SHA-256: `2352f8cc9028ace1a6ee45524b5f206ce6b5a6b040dffbcf5aad18f87955b17d`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `8240cd1846bf84513a54340bdb609125cb164b01a08a6f1badfbc8d5e849ec07`
- Classification: `recovered_replacement`
- Family and confidence: `object_adapter_recovered` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Smoothness -> 1:_NoteSize -> 2:_Color -> 3:_ColorMultiplier -> 4:_FakeMirrorTransparencyEnabled -> 5:_FakeMirrorTransparencyMultiplier -> 6:_EnableReflectionMap -> 7:_EnvironmentReflectionCube -> 8:_FogType -> 9:_FogStartOffset -> 10:_FogScale -> 11:_EnableHeightFog -> 12:_FogHeightScale -> 13:_FogHeightOffset -> 14:_PreciseFog -> 15:_EnableCutout -> 16:_Cutout -> 17:_CutoutTexOffset -> 18:_CutoutTexScale -> 19:_EnablePlaneCut -> 20:_CutPlaneEdgeGlowWidth -> 21:_CutPlane -> 22:_EnableRimDim -> 23:_RimScale -> 24:_RimOffset -> 25:_RimCameraDistanceOffset -> 26:_RimCameraDistanceScale -> 27:_RimDarkening -> 28:_WhiteBoostType -> 29:_CullMode -> 30:_StencilRefValue -> 31:_StencilComp -> 32:_StencilPass -> 33:_BlendSrcFactor -> 34:_BlendDstFactor -> 35:_BlendSrcFactorA -> 36:_BlendDstFactorA -> 37:_ZWrite -> 38:_MainTex -> 39:_AnimationSpawned -> 40:_ObjectTime -> 41:_Rotation -> 42:_StrobeColor -> 43:_StrobeColorEnabled -> 44:_OutlineWidth -> 45:_OverNoteInterfaceColor -> 46:_AlwaysTranslucent -> 47:_TranslucentAlpha.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque], states [Cull [_CullMode] | ZWrite [_ZWrite] | Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]], stencil [Ref [_StencilRefValue] Comp [_StencilComp] Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ObjectShared.hlsl].
   - Pragmas: vertex vert -> fragment frag -> shader_feature_local PLANE_CUT -> shader_feature_local ZWRITE -> shader_feature_local FAKE_MIRROR_TRANSPARENCY -> multi_compile _ CUTOUT -> multi_compile _ REFLECTION_MAP -> multi_compile _ RIM_DIM -> multi_compile_fragment _ _WHITEBOOSTTYPE_MAINEFFECT -> multi_compile_fragment _ HEIGHT_FOG -> multi_compile_fragment _ _FOGTYPE_LERP -> multi_compile _ POST_BLOOM -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ ACES_TONE_MAPPING -> multi_compile_fragment _ CM_PREVIEW_MODE.

### Audited stage flow

1. `NOTE` / `vertex+fragment` / `FAMILY_SHARED`: Union target for NoteHD and NoteLW with instanced color/strobe/timeline adapter, cutout and plane cuts, face-dependent reflection, rim, ACES/white boost, fog, bloom-fog, and optional fake-mirror premultiplication.
   - Evidence: Assets/_Graphics/Shaders/Object/Note.shader:201-418; Assets/_Graphics/Shaders/README.md:110-138; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/NoteHD/_shader.json; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/NoteLW/_shader.json
   - Limits: HD/LW source families diverge in alpha, reflection, cutout and fake-mirror routes; the union is not an exact source clone.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Object/Note.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Object/Note.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/Object/Note.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Object/Note.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/Object/Note.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).
1. Include 11 at depth 0: `Assets/_Graphics/Shaders/Object/Note.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ObjectShared.hlsl` (`3925230d685628cc84bba102b9bf73abbc062e1b0357606b40e8a9b7e82d3203`).

### Catalog limits

- No dedicated Note audit or committed visual parity fixture.

## 12. `ChroMapper/Object/Obstacle Distortion`

- Path: `Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader`
- Source SHA-256: `d0b26acc8c267cbc7d95142dcbe3c49f11d4ec124990264ff387bb2277467e00`
- Behavior matrix source SHA-256: `d0b26acc8c267cbc7d95142dcbe3c49f11d4ec124990264ff387bb2277467e00`
- Source join: matched.
- ABI SHA-256: `aa4564b186a374df6a47096518ab9ec282f71669c3b2efc554c5823bc4327e74`
- Classification: `recovered_replacement`
- Family and confidence: `object_adapter_recovered` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex -> 1:_DisplacementStrength -> 2:_CullMode -> 3:_Color -> 4:_TintColor -> 5:_AddColor -> 6:_DisplacementAlphaMul -> 7:_ScaleUV -> 8:_UVScale -> 9:_ScrollUV -> 10:_ScrollUVVelocity -> 11:_EnableFog -> 12:_FogStartOffset -> 13:_FogScale -> 14:_FogHeightScale -> 15:_FogHeightOffset -> 16:_ZWrite -> 17:_ClipLowAlpha -> 18:_ViewAngleAffectsDistortion -> 19:_ViewAngleDistortionParam -> 20:_UseDistortedTextureOnly -> 21:_DepthAwareDistortion -> 22:_EnableCutout -> 23:_CutoutTexScale -> 24:_CutoutTexOffset -> 25:_Cutout -> 26:_EnableRimDim -> 27:_RimDimScale -> 28:_RimDimOffset -> 29:_EnableClipping -> 30:_BlendSrcFactor -> 31:_BlendDstFactor -> 32:_BlendOp -> 33:_BlendSrcFactorA -> 34:_BlendDstFactorA.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent+50, IgnoreProjector=True, RenderType=Transparent], states [Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest LEqual | ZWrite [_ZWrite]], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [3.5], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Cutout.hlsl].
   - Pragmas: target 3.5 -> vertex vert -> fragment frag -> multi_compile_instancing -> shader_feature_local_vertex SCALE_UV -> shader_feature_local_fragment SCROLL_UV -> shader_feature_local_fragment FOG -> shader_feature_local_fragment CLIP_LOW_ALPHA -> shader_feature_local_fragment VIEW_ANGLE_AFFECTS_DISTORTION -> shader_feature_local_fragment USE_DISTORTED_TEXTURE_ONLY -> shader_feature_local_fragment DEPTH_AWARE_DISTORTION -> shader_feature_local_fragment CUTOUT -> shader_feature_local_fragment RIM_DIM -> shader_feature_local_fragment CLIPPING -> shader_feature_local_fragment ZWRITE -> multi_compile_fragment _ DEPTH_TEXTURE DEPTH_TEXTURE_ENABLED -> multi_compile_fragment _ BLOOM_FOG BLOOM_FOG -> multi_compile_fragment _ CM_PREVIEW_MODE -> shader_feature_local_fragment HEIGHT_FOG.

### Audited stage flow

1. `OBST-DIST` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Depth-aware screen displacement samples the grab/bloom source with tunable UV scale, cutout, clipping, rim and fog routes; runtime owns depth/grab inputs.
   - Evidence: Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader:1-16; Assets/_Graphics/Shaders/README.md:42-44; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/ScreenDisplacementHD/_shader.json
   - Limits: OVERDRAW_VIEW is omitted and project preview routes extend the source contract.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Cutout.hlsl` (`ee393b5c06c6e19780f0964426002f19da3100380a12d2deb79267a64ee689e7`).

### Catalog limits

- No standalone audit; runtime grab/depth texture and renderer-property ownership need capture.

## 13. `ChroMapper/Object/Obstacle Outline`

- Path: `Assets/_Graphics/Shaders/Object/ObstacleOutline.shader`
- Source SHA-256: `ae6ed5912038a0a2e759bcc91b9c0577edeff6233c29e8e23d99364ddc8b08f4`
- Behavior matrix source SHA-256: `ae6ed5912038a0a2e759bcc91b9c0577edeff6233c29e8e23d99364ddc8b08f4`
- Source join: matched.
- ABI SHA-256: `f566733fac65a6ec6de35320f719ce90cca147a6f0abf0e34b87125f3e051eeb`
- Classification: `recovered_partial_adapter`
- Family and confidence: `object_adapter` / `medium`

### Ordered root contract

- Root commands: none.
- Properties: 0:_FogStartOffset -> 1:_FogScale -> 2:_FogHeightScale -> 3:_FogHeightOffset -> 4:_CullMode -> 5:_WhiteBoostType -> 6:_EnableCutout -> 7:_CutoutTexScale -> 8:_BlendSrcFactor -> 9:_BlendDstFactor -> 10:_BlendSrcFactorA -> 11:_BlendDstFactorA -> 12:_ZTest -> 13:_ZWrite -> 14:_Color -> 15:_WorldScale -> 16:_Cutout -> 17:_CutoutTexOffset -> 18:_SizeParams.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Geometry+3, RenderType=Opaque], states [Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite [_ZWrite]], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [3.5], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Cutout.hlsl].
   - Pragmas: target 3.5 -> vertex vert -> fragment frag -> multi_compile_instancing -> shader_feature_local_fragment CUTOUT -> shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS -> multi_compile _ POST_BLOOM -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ CM_PREVIEW_MODE.

### Audited stage flow

1. `OBST-OUTLINE` / `vertex+fragment` / `SEMANTICALLY_SIMILAR_NOT_SAFE`: Face-local cube outline selects dimensions from face normals, scales instanced size/color, performs optional cutout, and composes source-style bloom fog/white boost.
   - Evidence: Assets/_Graphics/Shaders/Object/ObstacleOutline.shader:1-15; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:37-50; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/ParametricBoxFrameHD/_shader.json
   - Limits: The source audit explicitly says this is an editor adapter, not an ObstacleCore replacement; FrameHD/LW bindings remain unresolved.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleOutline.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleOutline.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleOutline.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Object/ObstacleOutline.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Cutout.hlsl` (`ee393b5c06c6e19780f0964426002f19da3100380a12d2deb79267a64ee689e7`).

### Catalog limits

- Not a binary-proven complete FrameHD replacement; source bindings and layout remain unproven.

## 14. `ChroMapper/Parametric Box Fake Glow`

- Path: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader`
- Source SHA-256: `e1e4d80c037f9ec1cfa7a5b1a8f97b428601beedffc6de19f15b5cb171b6fb0e`
- Behavior matrix source SHA-256: `03aa4522c7601f047b9b6d8d8738fe977af8f0b242958f34fe65cc743bc9e46c`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `8a5a6f56369f42937d6151adc2979dab854bff96126f5b362fec97762fa4eb2c`
- Classification: `recovered_replacement`
- Family and confidence: `parametric` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex -> 1:_FogStartOffset -> 2:_FogScale -> 3:_EnableHeightFog -> 4:_FogHeightScale -> 5:_FogHeightOffset -> 6:_AngleDisappearParam -> 7:_WhiteBoostType -> 8:_EnableCutout -> 9:_WorldspaceNoiseCutout -> 10:_CutoutTexScale -> 11:_EnableClipping -> 12:_BlendModeSrc -> 13:_BlendModeDst -> 14:_BlendModeSrcA -> 15:_BlendModeDstA.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent, IgnoreProjector=True, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp Add | Cull Off | ZTest LEqual | ZWrite Off], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Cutout.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> multi_compile _ BLOOM_FOG -> shader_feature_local HEIGHT_FOG -> shader_feature_local MAIN_EFFECT_WHITE_BOOST -> shader_feature_local _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS -> shader_feature_local CUTOUT -> shader_feature_local_fragment WORLDSPACE_NOISE_CUTOUT -> shader_feature_local_fragment CLIPPING -> multi_compile _ POST_BLOOM.

### Audited stage flow

1. `PFG` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Face-relative parametric deformation, squared texture alpha, height/angle/distance fades, optional noise cutout/clipping, premultiplied output and compile-time white boost.
   - Evidence: Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader:131-231; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:20-35; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/ParametricBoxFakeGlow/_shader.json
   - Limits: Zero-height behavior differs because ParametricShared guards the denominator; see PFG-DEFECT.
1. `PFG-DEFECT` / `height-ramp helper` / `DEFECT_CANDIDATE`: The recovered DXBC divides by the global height directly; ParametricShared.hlsl clamps the denominator with max(globalHeight, 1e-5), changing zero/almost-zero height behavior.
   - Evidence: Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl:11-19; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:32-35
   - Limits: The discrepancy is limited to zero or near-zero global height and was not changed.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Cutout.hlsl` (`ee393b5c06c6e19780f0964426002f19da3100380a12d2deb79267a64ee689e7`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` (`05bb70df99629b8d9b998145f34355fee52c364090a3da5d475b02bc50bf85c5`).
1. Include 11 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).

### Catalog limits

- No committed visual parity capture was found.

## 15. `ChroMapper/Parametric Box Opaque`

- Path: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader`
- Source SHA-256: `3c1509a34f2d288a70b2a881e843cc18c4d686102cba414f85cba05777e0e9d3`
- Behavior matrix source SHA-256: `3c1509a34f2d288a70b2a881e843cc18c4d686102cba414f85cba05777e0e9d3`
- Source join: matched.
- ABI SHA-256: `747e9a78c1b8d0a2d2cd166c10d096583d47120d7eac1c095656789bab5486f3`
- Classification: `recovered_replacement`
- Family and confidence: `parametric` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_FogStartOffset -> 1:_FogScale -> 2:_EnableHeightFog -> 3:_FogHeightScale -> 4:_FogHeightOffset.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque], states [Cull Back | ZTest LEqual | ZWrite On], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_fragment HEIGHT_FOG -> multi_compile_fragment _ BLOOM_FOG -> multi_compile _ POST_BLOOM.

### Audited stage flow

1. `PBO` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Instanced color alpha is squared; optional height/distance fog, unconditional blue-noise dither, white boost, and bloom-prepass or constant fog target produce opaque output.
   - Evidence: Assets/_Graphics/Shaders/ParametricBoxOpaque.shader:103-173; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:20-35; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/OpaqueNeonLight/_shader.json
   - Limits: This maps OpaqueNeonLight, not FrameHD. FrameHD has separate anonymous layouts.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).
1. Include 11 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxOpaque.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` (`05bb70df99629b8d9b998145f34355fee52c364090a3da5d475b02bc50bf85c5`).
1. Include 12 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).

### Catalog limits

- No committed visual parity capture was found.

## 16. `ChroMapper/Parametric Box Transparent`

- Path: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader`
- Source SHA-256: `442b039a06d8b89308280f0a927f893e5671bcbc9303444d4ee08a12f82dad98`
- Behavior matrix source SHA-256: `442b039a06d8b89308280f0a927f893e5671bcbc9303444d4ee08a12f82dad98`
- Source join: matched.
- ABI SHA-256: `c59f70c279d1224712183d2853d97aeefed9f5d39ecde91178a051ada800dec3`
- Classification: `recovered_replacement`
- Family and confidence: `parametric` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_FogStartOffset -> 1:_FogScale -> 2:_EnableHeightFog -> 3:_FogHeightScale -> 4:_FogHeightOffset -> 5:_EnableWorldNoise -> 6:_WorldNoiseScale -> 7:_WorldNoiseIntensityOffset -> 8:_WorldNoiseIntensityScale -> 9:_WorldNoiseScrolling -> 10:_WorldNoiseDistort -> 11:_NoiseWarpZoomStrength -> 12:_NoiseWarpSkewStrength -> 13:_EnableWorldSpaceFade -> 14:_WorldSpaceFadePos -> 15:_WorldSpaceFadeSlope -> 16:_EnableSpecular -> 17:_SpecularIntensity -> 18:_SpecularHardness -> 19:_EnableNormalMap -> 20:_NormalTex -> 21:_NormalScale -> 22:_EnableReflectionProbe -> 23:_Smoothness -> 24:_ReflectionIntensity -> 25:_GlassOpacity -> 26:_EnableRimDim -> 27:_RimScale -> 28:_RimOffset -> 29:_RimDistanceOffset -> 30:_RimDistanceScale -> 31:_InvertRimDim -> 32:_BlendModeSrc -> 33:_BlendModeDst -> 34:_BlendModeSrcA -> 35:_BlendModeDstA -> 36:_CullMode -> 37:_StencilRefValue -> 38:_StencilComp -> 39:_StencilPass -> 40:_BlendOp.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent, IgnoreProjector=True, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest LEqual | ZWrite Off], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_fragment HEIGHT_FOG -> shader_feature_local_fragment WORLD_NOISE -> shader_feature_local_fragment WORLD_SPACE_FADE -> shader_feature_local_fragment WORLD_NOISE_WARP -> shader_feature_local REFLECTION_PROBE -> multi_compile_fragment _ BLOOM_FOG -> multi_compile _ POST_BLOOM.

### Audited stage flow

1. `PBT` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Height-selected width/alpha, optional parametric world noise/fade/warp, squared alpha, optional packed-probe reflection, fog transmission and glass-opacity addition.
   - Evidence: Assets/_Graphics/Shaders/ParametricBoxTransparent.shader:175-263; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:20-50; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/TransparentNeonLight/_shader.json
   - Limits: Source controls for unsupported SPECULAR/NORMAL_MAP/rim routes remain inert.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` (`05bb70df99629b8d9b998145f34355fee52c364090a3da5d475b02bc50bf85c5`).
1. Include 10 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 11 at depth 0: `Assets/_Graphics/Shaders/ParametricBoxTransparent.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` (`f9c1fa70ebb8871b1afd9816689ce2ff33eac1b8a3af60d65756d9a76bd2e544`).
1. Include 12 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 13 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 14 at depth 2: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 15 at depth 2: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).

### Catalog limits

- No committed visual parity capture was found.

## 17. `ChroMapper/Parametric Slice Billboard`

- Path: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader`
- Source SHA-256: `f6eb849feff3fe7e0493d282db11647411da225390cccb63f9881bbda5ad7c24`
- Behavior matrix source SHA-256: `307621ff1a7e89d8236830e61a1a3bd0fb04bc5d3920f12a865a7d4368b30e4b`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `38719acc8d409ddf6d32f956ffd60b7bbb3a39ca9f06aa728a0149e2f488add7`
- Classification: `recovered_replacement`
- Family and confidence: `parametric` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex -> 1:_CapUVSize -> 2:_OffsetFactor -> 3:_OffsetUnits -> 4:_EnableFog -> 5:_EnableHeightFog -> 6:_FogHeightScale -> 7:_FogHeightOffset -> 8:_UseFogForLights -> 9:_FogStartOffset -> 10:_FogScale -> 11:_EnableWorldNoise -> 12:_WorldNoiseScale -> 13:_WorldNoiseIntensityOffset -> 14:_WorldNoiseIntensityScale -> 15:_WorldNoiseScrolling -> 16:_WorldNoiseSkew -> 17:_NoiseWarpZoomStrength -> 18:_NoiseWarpSkewStrength -> 19:_EnableWorldSpaceFade -> 20:_WorldSpaceFadePos -> 21:_WorldSpaceFadeSlope -> 22:_EnableAlphaWidthScale -> 23:_WhiteBoostType -> 24:_BloomWhiteMultiplier -> 25:_BloomMultiplier -> 26:_SquareAlpha -> 27:_EnableEmissionAngleDisappear -> 28:_EnableNoiseDithering -> 29:_EnableYAxisBillboard -> 30:_BlendModeSrc -> 31:_BlendModeDst -> 32:_BlendModeSrcA -> 33:_BlendModeDstA -> 34:_ZTest -> 35:_CullMode -> 36:_StencilRefValue -> 37:_StencilComp -> 38:_StencilPass -> 39:_BlendOp.

### Ordered SubShaders and passes

1. SubShader 0: tags [PreviewType=Plane, CanUseSpriteAtlas=True, Queue=Transparent, IgnoreProjector=True, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite Off | Offset [_OffsetFactor], [_OffsetUnits]], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_vertex ALPHA_WIDTH_SCALE -> shader_feature_local_fragment SQUARE_ALPHA -> shader_feature_local_fragment ANGLE_DISAPPEAR -> shader_feature_local_vertex Y_AXIS_BILLBOARD -> shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS -> multi_compile _ POST_BLOOM -> shader_feature_local_fragment FOG -> shader_feature_local_fragment HEIGHT_FOG -> shader_feature_local_fragment USE_FOG_FOR_LIGHTS -> shader_feature_local_fragment WORLD_NOISE -> shader_feature_local_fragment WORLD_SPACE_FADE -> shader_feature_local_fragment WORLD_NOISE_WARP -> shader_feature_local_fragment NOISE_DITHERING -> multi_compile_fragment _ BLOOM_FOG.

### Audited stage flow

1. `PSB` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Three-slice cap/body geometry, optional Y billboard and alpha-width scaling, cubic alpha/noise/texture fades, fog, white boost, masked dither and bloom multiplier.
   - Evidence: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader:206-378; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:7-18; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Parametric3SliceSprite/_shader.json
   - Limits: The current cap-UV base is a defect candidate; see PSB-DEFECT.
1. `PSB-DEFECT` / `fragment/vertex contract` / `DEFECT_CANDIDATE`: The audited exact cap UV base is 0.25, but the current vertex formula uses literal 0.36 in adjustedUvY.
   - Evidence: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader:265-270; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:11-18
   - Limits: No fix was made. This report only records the source/evidence discrepancy.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` (`05bb70df99629b8d9b998145f34355fee52c364090a3da5d475b02bc50bf85c5`).
1. Include 11 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 12 at depth 0: `Assets/_Graphics/Shaders/ParametricSliceBillboard.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).

### Catalog limits

- No committed visual parity capture was found.

## 18. `ChroMapper/Particles`

- Path: `Assets/_Graphics/Shaders/Particles.shader`
- Source SHA-256: `e9d1ba767ac448cf9f400bbb72c86c3caffc385e57e2d2f34b5475250b2c3f96`
- Behavior matrix source SHA-256: `30967d086b93f2ce0f71d5f952b0591c5ef69c7725137b420faf289e5426b219`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `90a3571140b490be789ad037dea786617a544a80d919483f1d729eb346bbf101`
- Classification: `recovered_replacement`
- Family and confidence: `particles` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_EnableObstacle -> 2:_Fog_Mask_Source -> 3:_ObstacleFogMultiplier -> 4:_ObstacleFogMax -> 5:_ObstacleColorInfluence -> 6:_FogColorHighlight -> 7:_ObstacleFogHighlightMultiplier -> 8:_EnableSecondaryColor -> 9:_SecondaryColor -> 10:_SecondaryColorTex -> 11:_SecondaryColorPanning -> 12:_UseColorGradient -> 13:_ColorGradient -> 14:_GradientUseAlpha -> 15:_GradientPosition -> 16:_GradientPanningSpeed -> 17:_UseSpectrogram -> 18:_SpectrogramBaseValue -> 19:_SpectrogramRange -> 20:_UseColorArray -> 21:_Secondary_UVs -> 22:_UVScale -> 23:_UVManualOffset -> 24:_EnableRotateUV -> 25:_RotateUV -> 26:_RotateMainUVOnly -> 27:_EnableWorldSpacePanning -> 28:_WorldspacePanningSpeed -> 29:_UsesParticleVertexStream -> 30:_EnableStartEnd -> 31:_AlphaStart -> 32:_AlphaEnd -> 33:_WidthStart -> 34:_WidthEnd -> 35:_EnableVertexColor -> 36:_SquareVertexAlpha -> 37:_RedIsVertexAlpha -> 38:_VertexChannels -> 39:_EnableLifetime -> 40:_EnableVertexFlipbook -> 41:_VertexFlipbookCount -> 42:_VertexFlipbookSpeed -> 43:_EnableVertexFlipbookFade -> 44:_VertexDisplacement -> 45:_DisplacementSecondaryUVs -> 46:_DisplacementTex -> 47:_3DDisplacement -> 48:_DisplacementPerParticleRandomization -> 49:_DisplacementStrength -> 50:_DisplacementAxes -> 51:_DisplacementPanningSpeed -> 52:_DisplacementPanning -> 53:_Spectrogram -> 54:_UV3Offset -> 55:_UV3Scale -> 56:_Curve_Vertices -> 57:_UseMainTex -> 58:_BaseLayer -> 59:_MainTex -> 60:_MainTexSecondaryUVs -> 61:_EnableMainTexWorldSpacePanning -> 62:_Pixelate -> 63:_PixelateResolution -> 64:_EnableTextureColor -> 65:_AlphaChannel -> 66:_TopBotFadeAngle -> 67:_LeftRightFadeAngle -> 68:_Diagonal_Channel -> 69:_MainPerParticleRandomization -> 70:_Intensity -> 71:_UvPanning -> 72:_EnableCustomPadding -> 73:_CustomPadding -> 74:_UseTextureFlipbook -> 75:_FlipbookColumns -> 76:_FlipbookRows -> 77:_FlipbookNonloopableFrames -> 78:_FlipbookSpeed -> 79:_FlipbookBlendingOff -> 80:_UseMotionVectors -> 81:_MotionVectorTex -> 82:_MotionVectorColumns -> 83:_MotionVectorRows -> 84:_MotionVectorSpeed -> 85:_MotionVectorIntensity -> 86:_EnableMask -> 87:_MaskSecondaryUVs -> 88:_MaskRedIsAlpha -> 89:_MaskBlend -> 90:_MaskTex -> 91:_MaskPerParticleRandomization -> 92:_MaskStrength -> 93:_MaskTexWorldspacePanning -> 94:_MaskPanning -> 95:_MaskDissolve -> 96:_MaskDissolveTiling -> 97:_MaskDissolveOffset -> 98:_EnableMask2 -> 99:_Mask2SecondaryUVs -> 100:_Mask2TexWorldspacePanning -> 101:_Mask2RedIsAlpha -> 102:_Mask2Blend -> 103:_Mask2Tex -> 104:_Mask2ParticleRandomization -> 105:_Mask2Strength -> 106:_Mask2Panning -> 107:_RimLight -> 108:_RimlightInvert -> 109:_RimLightEdgeStart -> 110:_RimLightIntensity -> 111:_EnableWorldNoise -> 112:_WorldNoiseScale -> 113:_WorldNoiseIntensityOffset -> 114:_WorldNoiseIntensityScale -> 115:_WorldNoiseScrolling -> 116:_Erosion -> 117:_Erosion_Source -> 118:_ErosionSecondaryUVs -> 119:_ErosionTexWorldspacePanning -> 120:_ErosionPerParticleRandomization -> 121:_ErosionTex -> 122:_ErosionPanning -> 123:_ErosionVertexThreshold -> 124:_ErosionThreshold -> 125:_ErosionSmoothness -> 126:Distortion -> 127:Distortion_Target -> 128:_FlowmapSecondaryUVs -> 129:_FlowmapTexWorldspacePanning -> 130:_FlowTex -> 131:_FlowmapParticleRandomization -> 132:_FlowSpeed -> 133:_FlowStrength -> 134:_FlowAdd -> 135:_FlowPanning -> 136:_DistortionSecondaryUVs -> 137:_DistortionTexWorldspacePanning -> 138:_DistortionParticleRandomization -> 139:_DistortionTex -> 140:_DistortionStrength -> 141:_DistortionAxes -> 142:_DistortionPanning -> 143:_FogType -> 144:_FogStartOffset -> 145:_FogScale -> 146:_EnableHeightFog -> 147:_FogHeightScale -> 148:_FogHeightOffset -> 149:_PreciseFog -> 150:_EnableDissolve -> 151:_DissolveScale -> 152:_DissolveReverse -> 153:_Dissolve_Space -> 154:_DissolveAxisVector -> 155:_UseDissolveProgress -> 156:_DissolveOffset -> 157:_DissolveStartValue -> 158:_DissolveEndValue -> 159:_DissolveProgressFromVertexAlpha -> 160:_DissolveProgress -> 161:_UseDissolveColor -> 162:_DissolveColor -> 163:_DissolveColorIntensity -> 164:_CutColorFalloff -> 165:_MultiplyDissolveGridByAlpha -> 166:_Dissolve_Grid -> 167:_GridThickness -> 168:_GridSize -> 169:_GridFalloff -> 170:_GridSpeed -> 171:_UseDissolveTexture -> 172:_DissolveTexture -> 173:_DissolveTextureSpeed -> 174:_DissolveTextureInfluence -> 175:_PlaneClipping -> 176:_ClippingPlanePosition -> 177:_ClippingPlaneNormal -> 178:_EnableReveal -> 179:_CutoutType -> 180:_Cutout -> 181:_CutoutTexScale -> 182:_CutoutGradientWidth -> 183:_CutoutTexOffset -> 184:_EnableFakeMirrorTransparency -> 185:_FakeMirrorTransparency -> 186:_EnableVertexDistortion -> 187:_EnableHologram -> 188:_HologramColor -> 189:_AlphaMultiplier -> 190:_SquareAlpha -> 191:_EnableFillAlpha -> 192:_FillAlpha -> 193:_FillMask -> 194:_FillColor -> 195:_EnableCloseToCameraDisappear -> 196:_CloseCameraDisappearDistance -> 197:_CloseCameraDisappearWidth -> 198:_EnableViewAlignDisappear -> 199:_SquareAngleForViewAlignDisappear -> 200:_ViewAlignFactor -> 201:_ViewAlignOffset -> 202:_EnableSoftParticles -> 203:_SoftFactor -> 204:_Override_Final_Alpha -> 205:_OverrideFinalAlpha -> 206:_WhiteBoostType -> 207:_QuestWhiteboostMultiplier -> 208:_BloomMultiplier -> 209:_GreenChannelWhiteboost -> 210:_RemapWhiteboostStart -> 211:_WhiteBoostRemapStart -> 212:_Billboard -> 213:_BillboardScale -> 214:_EnableNoiseDithering -> 215:_Custom_Time -> 216:_EnableMipmapBias -> 217:_MipmapBias -> 218:_UseChromaticAberration -> 219:_ChromaticAberration -> 220:_BlendModeSrc -> 221:_BlendModeDst -> 222:_BlendOp -> 223:_BlendModeSrcA -> 224:_BlendModeDstA -> 225:_CullMode -> 226:_ZWrite -> 227:_ZTest -> 228:_OffsetFactor -> 229:_OffsetUnits -> 230:_StencilRefValue -> 231:_StencilComp -> 232:_StencilPass -> 233:_MeshPacking -> 234:_MeshPackingId -> 235:_BloomPreset -> 236:_BlendingPreset -> 237:_StencilPreset.

### Ordered SubShaders and passes

1. SubShader 0: tags [PreviewType=Plane, CanUseSpriteAtlas=True, Queue=Transparent, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite [_ZWrite] | Offset [_OffsetFactor], [_OffsetUnits]], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Time.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/SpectrogramShared.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local SECONDARY_COLOR -> shader_feature_local_fragment COLOR_BY_FOG -> shader_feature_local_fragment FOG_COLOR_HIGHLIGHT -> shader_feature_local_fragment _ _FOG_MASK_SOURCE_PRIMARY_MASK -> shader_feature_local COLOR_GRADIENT -> shader_feature_local_vertex SPECTROGRAM_COLOR -> shader_feature_local_fragment SPECTROGRAM_COLOR -> shader_feature_local COLOR_ARRAY -> shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE -> shader_feature_local SECONDARY_UVS_MAIN -> shader_feature_local WORLDSPACE_PANNING -> shader_feature_local VERTEX_COLOR -> shader_feature_local VERTEX_SQUARE_ALPHA -> shader_feature_local_vertex VERTEX_RED_IS_ALPHA -> shader_feature_local_vertex _ _VERTEXCHANNELS_A -> shader_feature_local_vertex SPATIAL_DISPLACEMENT -> shader_feature_local_vertex _ _SPECTROGRAM_FULL -> shader_feature_local_vertex _ _CURVE_VERTICES_AROUND_Z -> shader_feature_local_vertex MESH_PACKING -> shader_feature_local MAIN_TEXTURE -> shader_feature_local_fragment PIXELATE -> shader_feature_local_fragment TEXTURE_COLOR -> shader_feature_local_fragment _ _ALPHACHANNEL_RED -> shader_feature_local_fragment CUSTOM_WRAPPING -> shader_feature_local TEXTURE_FLIPBOOK -> shader_feature_local FLIPBOOK_BLENDING_OFF -> shader_feature_local MASK -> shader_feature_local SECONDARY_UVS_MASK -> shader_feature_local MASK_RED_IS_ALPHA -> shader_feature_local _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD -> shader_feature_local MASK2 -> shader_feature_local SECONDARY_UVS_MASK2 -> shader_feature_local MASK2_RED_IS_ALPHA -> shader_feature_local _ _MASK2BLEND_ADD _MASK2BLEND_MASKED_ADD -> shader_feature_local _ DISTORTION_SIMPLE -> shader_feature_local_fragment DISTORTION_TARGET_MASK -> shader_feature_local SECONDARY_UVS_DISTORTION -> shader_feature_local WORLDSPACE_PANNING_DISTORTION -> shader_feature_local_fragment _ _CUTOUTTYPE_ALPHA_CLIP _CUTOUTTYPE_WORLDSPACE_NOISE -> shader_feature_local_fragment PLANE_CLIPPING -> shader_feature_local_fragment SQUARE_ALPHA -> shader_feature_local VIEW_ALIGN_DISAPPEAR -> shader_feature_local_fragment LIFETIME -> shader_feature_local_vertex LIFETIME -> shader_feature_local SOFT_PARTICLES -> shader_feature_local_fragment CLOSE_TO_CAMERA_DISAPPEAR -> shader_feature_local_fragment FILL_ALPHA -> shader_feature_local_fragment _ _OVERRIDE_FINAL_ALPHA_COLOR_BASED -> shader_feature_local DISSOLVE -> shader_feature_local _ _DISSOLVE_SPACE_WORLD _DISSOLVE_SPACE_WORLD_CENTERED -> shader_feature_local DISSOLVE_PROGRESS_FROM_VERTEX_ALPHA -> shader_feature_local_vertex WORLDSPACE_PANNING_MAIN -> shader_feature_local VERTEX_FLIPBOOK -> shader_feature_local VERTEX_FLIPBOOK_FADE -> shader_feature_local MIPMAP_BIAS -> shader_feature_local NOISE_DITHERING -> shader_feature_local MAIN_PER_PARTICLE_RANDOM -> shader_feature_local_fragment HOLOGRAM -> shader_feature_local_fragment FAKE_MIRROR_TRANSPARENCY -> shader_feature_local_fragment PRECISE_FOG -> shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS -> multi_compile _ POST_BLOOM -> multi_compile _ DEPTH_TEXTURE DEPTH_TEXTURE_ENABLED -> shader_feature_local_fragment REMAP_WHITEBOOST_START -> shader_feature_local_vertex _ _BILLBOARD_FULL _BILLBOARD_Y_AXIS _BILLBOARD_CAMERA_FACING -> shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE -> shader_feature_local_fragment _ _FOGTYPE_LERP _FOGTYPE_COLOR _FOGTYPE_ALPHA -> shader_feature_local_fragment HEIGHT_FOG -> multi_compile_fragment _ BLOOM_FOG.

### Audited stage flow

1. `PARTICLES` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Recovered particle family covers renderer color, masks, gradients, flipbooks, depth/soft particles, billboards, lifetime/dissolve, world UV/noise, fog, bloom, white boost, dither, instancing and stereo.
   - Evidence: Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:6-21; Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:42-80; Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:152-170; /mnt/programs/Code/GitRepository/ChroMapper/Assets/_Graphics/Shaders/Particles.shader:1-3
   - Limits: OVERDRAW_VIEW, zero-row selectors and unsupported VERTEX_DISPLACEMENT routes remain omitted/unverified.
1. `PARTICLES-UNVERIFIED` / `diagnostic/unsupported routes` / `UNVERIFIED`: OVERDRAW_VIEW and zero-row selectors have no replacement formula; current materials can request unsupported VERTEX_DISPLACEMENT.
   - Evidence: Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:103-115; Assets/_Graphics/Shaders/PARTICLE_REAUDIT.md:162-170
   - Limits: Do not restore a route without authoritative source/binary evidence.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Particles.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Particles.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/Particles.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).
1. Include 9 at depth 0: `Assets/_Graphics/Shaders/Particles.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Time.hlsl` (`c2fd3fed529541e4425652b11ae382758f6c5e559c54755ade3da5885ad68fc4`).
1. Include 10 at depth 0: `Assets/_Graphics/Shaders/Particles.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/SpectrogramShared.hlsl` (`87dcbfeeb9272e782759e1b92564675afde6abee53344b1b3e15ccd32171df02`).
1. Include 11 at depth 0: `Assets/_Graphics/Shaders/Particles.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).

### Catalog limits

- Runtime/mesh parity and visual comparison remain unproven; unsupported zero-row selectors have no invented formula.

## 19. `ChroMapper/Post Process/Bloom`

- Path: `Assets/_Graphics/Shaders/Post Process/Bloom.shader`
- Source SHA-256: `68e61917c9d131b3f673721c5821cead64b99d768fdf6646a95d02448db6856d`
- Behavior matrix source SHA-256: `68e61917c9d131b3f673721c5821cead64b99d768fdf6646a95d02448db6856d`
- Source join: matched.
- ABI SHA-256: `9eed029aaf93cdd894a613d520454f3c3daf86e161d0c0f39f0c97e0626ad724`
- Classification: `recovered_replacement`
- Family and confidence: `post_process` / `medium-high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex.

### Ordered SubShaders and passes

1. SubShader 0: tags [none], states [Cull Off | ZWrite Off | ZTest Always], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 1 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 2 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 3 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 4 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 5 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 6 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 7 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 8 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 9 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 10 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 11 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 12 (`<unnamed>`): tags [none], states [none], stencil [none].
   1. Pass 13 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLINCLUDE` (shared): entries [none], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/BloomShared.hlsl].
   - Pragmas: none.
1. Program 1 `HLSLPROGRAM` (pass 0): entries [vertex:VertDefault -> fragment:FragDownsample13Alpha], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDownsample13Alpha.
1. Program 2 `HLSLPROGRAM` (pass 1): entries [vertex:VertDefault -> fragment:FragDownsample4Alpha], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDownsample4Alpha.
1. Program 3 `HLSLPROGRAM` (pass 2): entries [vertex:VertDefault -> fragment:FragDownsample13], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDownsample13.
1. Program 4 `HLSLPROGRAM` (pass 3): entries [vertex:VertDefault -> fragment:FragDownsample4], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDownsample4.
1. Program 5 `HLSLPROGRAM` (pass 4): entries [vertex:VertDefault -> fragment:FragDownsample4Gamma], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDownsample4Gamma.
1. Program 6 `HLSLPROGRAM` (pass 5): entries [vertex:VertDefault -> fragment:FragUpsampleTent], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleTent.
1. Program 7 `HLSLPROGRAM` (pass 6): entries [vertex:VertDefault -> fragment:FragUpsampleBox], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleBox.
1. Program 8 `HLSLPROGRAM` (pass 7): entries [vertex:VertDefault -> fragment:FragUpsampleTentGamma], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleTentGamma.
1. Program 9 `HLSLPROGRAM` (pass 8): entries [vertex:VertDefault -> fragment:FragUpsampleBoxGamma], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleBoxGamma.
1. Program 10 `HLSLPROGRAM` (pass 9): entries [vertex:VertDefault -> fragment:FragDirectCombine], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDirectCombine.
1. Program 11 `HLSLPROGRAM` (pass 10): entries [vertex:VertDefault -> fragment:FragDirectCombineGamma], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragDirectCombineGamma.
1. Program 12 `HLSLPROGRAM` (pass 11): entries [vertex:VertDefault -> fragment:FragUpsampleReinhard], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleReinhard.
1. Program 13 `HLSLPROGRAM` (pass 12): entries [vertex:VertDefault -> fragment:FragUpsampleAces], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleAces.
1. Program 14 `HLSLPROGRAM` (pass 13): entries [vertex:VertDefault -> fragment:FragUpsampleAutoExposureAces], target [implicit], includes [none].
   - Pragmas: vertex VertDefault -> fragment FragUpsampleAutoExposureAces.

### Audited stage flow

1. `PP-BLOOM` / `14 fragment passes` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Four/13-tap downsample, alpha gates, tent/box upsample, gamma, direct combine, Reinhard, ACES and auto-exposure ACES routes.
   - Evidence: Assets/_Graphics/Shaders/Post Process/Bloom.shader:37-138; Assets/_Graphics/Shaders/Post Process/Bloom.shader:141-255; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/PostProcessing/Bloom/_shader.json
   - Limits: Pass selection is separate from fragment behavior; sampler/addressing compatibility is not fully proven.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Post Process/Bloom.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Post Process/Bloom.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/BloomShared.hlsl` (`8f22c4256068bac963e4edc99ee18688300fe6d3d0c98b20a72202763b14ccb9`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/BloomShared.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).

### Catalog limits

- Exact original ShaderLab identity is unrecorded; no committed visual capture.

## 20. `ChroMapper/Post Process/Post Bloom`

- Path: `Assets/_Graphics/Shaders/Post Process/PostBloom.shader`
- Source SHA-256: `e6c51d2c9451fca0ed01af2a3cb4f35781b53d933c68793af70204d453dcf6a5`
- Behavior matrix source SHA-256: `e6c51d2c9451fca0ed01af2a3cb4f35781b53d933c68793af70204d453dcf6a5`
- Source join: matched.
- ABI SHA-256: `e437695e504aff4eb62bb696251bef1eb595757fb2d274dcbaaf8bb2b16c0c2e`
- Classification: `recovered_replacement`
- Family and confidence: `post_process` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_MainTex -> 1:_BloomIntensity -> 2:_Fade.

### Ordered SubShaders and passes

1. SubShader 0: tags [none], states [Cull Off | ZWrite Off | ZTest Always], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [none], stencil [none].

### Ordered programs

1. Program 0 `HLSLINCLUDE` (shared): entries [none], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl].
   - Pragmas: none.
1. Program 1 `HLSLPROGRAM` (pass 0): entries [vertex:VertDefault -> fragment:FragMainEffect], target [3.5], includes [none].
   - Pragmas: target 3.5 -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> multi_compile_local _ LIV_MR -> multi_compile_local _ CLEAR_SCREEN_ALPHA -> vertex VertDefault -> fragment FragMainEffect.

### Audited stage flow

1. `PP-POSTBLOOM` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Samples scene and post-bloom textures, computes four-tap alpha white boost, applies blue-noise dither, combines bloom and scene, and conditionally clears output alpha.
   - Evidence: Assets/_Graphics/Shaders/Post Process/PostBloom.shader:5-31; Assets/_Graphics/Shaders/Post Process/PostBloom.shader:90-148; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/MainEffect/_shader.json
   - Limits: LIV_MR/CLEAR_SCREEN_ALPHA and mono/stereo variants are compile-time routes; source scale/offset and SampleBias slots are not producer-proven.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Post Process/PostBloom.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Post Process/PostBloom.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Bloom.hlsl` (`cfb044d02d4f4ccbed2d798660497a9515f7b9878e8251ce3a913806f213a08d`).

### Catalog limits

- No standalone audit or committed frame/reference capture.

## 21. `ChroMapper/Rain`

- Path: `Assets/_Graphics/Shaders/Rain.shader`
- Source SHA-256: `eaaa574614f64779b249c6f9afff9e529412bc442ed7a9d086d7f481a628ff48`
- Behavior matrix source SHA-256: `eaaa574614f64779b249c6f9afff9e529412bc442ed7a9d086d7f481a628ff48`
- Source join: matched.
- ABI SHA-256: `b3273c8dde876028a4cbdd947f446391502df3d790a8bc9c6a2bcb345df4e9c5`
- Classification: `recovered_replacement`
- Family and confidence: `environment_fx` / `medium`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Height -> 1:_Speed -> 2:_BottomFadeScale -> 3:_TopFadeScale -> 4:_BottomEnd -> 5:_TopEnd -> 6:_Color -> 7:_EnableColorGradient -> 8:_ColorGradient -> 9:_MainTex -> 10:_EnableTextureColor -> 11:_AlphaChannel -> 12:_Intensity -> 13:_UvPanning -> 14:_EnableMask -> 15:_MaskAdditive -> 16:_MaskRedIsAlpha -> 17:_MaskTex -> 18:_MaskPanning -> 19:_MaskStrength -> 20:_EnableMask2 -> 21:_Mask2RedIsAlpha -> 22:_Mask2Tex -> 23:_Mask2Panning -> 24:_Mask2MinValue -> 25:_EnableSoftParticles -> 26:_SoftFactor -> 27:_EnableCloseToCameraDisappear -> 28:_CloseCameraDisappearDistance -> 29:_CloseCameraDisappearWidth -> 30:_EnableViewAlignDisappear -> 31:_ViewAlignFactor -> 32:_EnableVertexColor -> 33:_SquareVertexAlpha -> 34:_RedIsVertexAlpha -> 35:_VertexChannels -> 36:_EnableLifetime -> 37:_FogType -> 38:_FogStartOffset -> 39:_FogScale -> 40:_AlphaFromFog -> 41:_EnableHeightFog -> 42:_PreciseFog -> 43:_EnableHologram -> 44:_HologramColor -> 45:_SquareAlpha -> 46:_AlphaMultiplier -> 47:_WhiteBoostType -> 48:_EnableNoiseDithering -> 49:_BlendModeSrc -> 50:_BlendModeDst -> 51:_BlendModeSrcA -> 52:_BlendModeDstA -> 53:_BlendOp -> 54:_CullMode -> 55:_ZTest -> 56:_OffsetFactor -> 57:_OffsetUnits -> 58:_StencilRefValue -> 59:_StencilComp -> 60:_StencilPass.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent, RenderType=Transparent], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | BlendOp [_BlendOp] | Cull [_CullMode] | ZTest [_ZTest] | ZWrite Off | Offset [_OffsetFactor], [_OffsetUnits]], stencil [Ref [_StencilRefValue] Comp [_StencilComp] Pass [_StencilPass]].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> shader_feature_local TEXTURE_COLOR -> shader_feature_local _ALPHACHANNEL_RED -> shader_feature_local MASK_RED_IS_ALPHA -> shader_feature_local VERTEX_COLOR -> shader_feature_local VERTEX_SQUARE_ALPHA -> shader_feature_local _FOGTYPE_COLOR -> multi_compile_fragment _ BLOOM_FOG.

### Audited stage flow

1. `RAIN` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Mesh streams drive streak phase and vertical fade; texture/color/vertex alpha, color-fog, masks, and premultiplied output are selected by keywords, with optional bloom prepass.
   - Evidence: Assets/_Graphics/Shaders/Rain.shader:136-203; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Rain/_shader.json
   - Limits: Runtime mesh streams and per-renderer color are required; visual parity is not established by static assets.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Rain.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Rain.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 7 at depth 0: `Assets/_Graphics/Shaders/Rain.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 8 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).

### Catalog limits

- No dedicated audit, corpus counts, version, binary hashes, capture, or screenshot.

## 22. `ChroMapper/Set Depth Only`

- Path: `Assets/_Graphics/Shaders/SetDepthOnly.shader`
- Source SHA-256: `9f02dac78da66b4872f9f75c61474328656c8a4ba5a28a256fd4ec6331130804`
- Behavior matrix source SHA-256: `9f02dac78da66b4872f9f75c61474328656c8a4ba5a28a256fd4ec6331130804`
- Source join: matched.
- ABI SHA-256: `5e0ecc8ec49b6344d906e43aea8335415afcb85e64b952010dc367b17b57df11`
- Classification: `recovered_replacement`
- Family and confidence: `depth_stencil` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_StencilRefValue -> 1:_StencilComp -> 2:_StencilPass -> 3:_ZWrite.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Geometry-1, RenderType=Opaque], states [none], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Blend Zero One, Zero One | ZClip On | ZWrite [_ZWrite] | Cull Off], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].

### Ordered programs

1. Program 0 `CGPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc].
   - Pragmas: vertex vert -> fragment frag -> multi_compile _ STEREO_INSTANCING_ON.

### Audited stage flow

1. `DEPTH` / `vertex+fragment` / `EXACT_SHARED`: Saturates vertex COLOR, transforms POSITION, returns interpolated color, and applies material stencil/depth state; only stereo is a compiled variant.
   - Evidence: Assets/_Graphics/Shaders/SetDepthOnly.shader:61-75; Assets/_Graphics/Shaders/SetDepthOnly.shader:3-15; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SetDepthOnly/_shader.json
   - Limits: Recovered materials override defaults (Ref=1, Always, Replace, ZWrite Off); stage binaries do not prove state.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/SetDepthOnly.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).

### Catalog limits

- No standalone audit or visual capture; ShaderLab state cannot be proven from stage ASM alone.

## 23. `ChroMapper/Spectrogram`

- Path: `Assets/_Graphics/Shaders/Spectrogram.shader`
- Source SHA-256: `8218a94fa5266cf3b2d38ef1f4cda990588dc46d88ee2b73a87562459ebb4436`
- Behavior matrix source SHA-256: `213deadf948fda29c2fb0b8f6d94884eccb108a529621908e3338ed536db0d51`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `526fe3ec234ea536999d8374385bfb7eb80f6255d20133cf5891413ae415fb53`
- Classification: `recovered_replacement`
- Family and confidence: `spectrogram` / `high`

### Ordered root contract

- Root commands: Fallback "Diffuse".
- Properties: 0:_Color -> 1:_PeakOffset -> 2:_Metallic -> 3:_Smoothness -> 4:_EnableDiffuse -> 5:_EnableSpecular -> 6:_SpecularIntensity -> 7:_EnableLightFalloff -> 8:_FogStartOffset -> 9:_FogScale -> 10:_ZWrite.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Geometry, RenderType=Opaque, DisableBatching=True], states [Cull Back | ZTest LEqual | ZWrite [_ZWrite]], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/SpectrogramShared.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> shader_feature_local_fragment DIFFUSE -> shader_feature_local_fragment SPECULAR -> shader_feature_local_fragment LIGHT_FALLOFF -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ ACES_TONE_MAPPING -> multi_compile _ STEREO_INSTANCING_ON.

### Audited stage flow

1. `SPECTRO` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: UV.x selects one of 64 samples; peak offset deforms vertices; optional five-light diffuse/specular/falloff, always-evaluated height fog, bloom fog, ACES and unconditional blue-noise dither produce zero alpha.
   - Evidence: Assets/_Graphics/Shaders/Spectrogram.shader:1-14; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:29-38; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/Spectrogram/_shader.json
   - Limits: OVERDRAW_VIEW and white-boost/noise-keyword routes are intentionally omitted.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Spectrogram.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/Spectrogram.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/Spectrogram.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 9 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 10 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 11 at depth 0: `Assets/_Graphics/Shaders/Spectrogram.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).
1. Include 12 at depth 0: `Assets/_Graphics/Shaders/Spectrogram.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).
1. Include 13 at depth 0: `Assets/_Graphics/Shaders/Spectrogram.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/SpectrogramShared.hlsl` (`87dcbfeeb9272e782759e1b92564675afde6abee53344b1b3e15ccd32171df02`).

### Catalog limits

- No committed visual parity capture was found.

## 24. `ChroMapper/Spectrogram Unlit`

- Path: `Assets/_Graphics/Shaders/SpectrogramUnlit.shader`
- Source SHA-256: `87df7741ec2e3c9cbffe6a28770cd2bc4b002506b0f501928a7bbe5764deb497`
- Behavior matrix source SHA-256: `2630113e6763c5f09ca34745fae5e0a30a957cb7f6e87a65bc7eb4f05ae46b56`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `63484894eb30703fd998aa8cd756c8da3c1464a695dd9af4842ef797dd8d1cad`
- Classification: `recovered_replacement`
- Family and confidence: `spectrogram` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_SpectrogramScale -> 2:_FogStartOffset -> 3:_FogScale -> 4:_BlendModeSrc -> 5:_BlendModeDst -> 6:_BlendModeSrcA -> 7:_BlendModeDstA.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Transparent, RenderType=Transparent, DisableBatching=True], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | Cull Off | ZTest LEqual | ZWrite Off], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [none].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/SpectrogramShared.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile_fragment _ BLOOM_FOG -> multi_compile _ STEREO_INSTANCING_ON.

### Audited stage flow

1. `SPECTRO-UNLIT` / `vertex+fragment` / `EXACT_SHARED`: UV.x selects 64 samples; visibility is a step against sample height and scale; color alpha is multiplied by visibility; bloom fog lerps full RGBA and no dither/tonemap is used.
   - Evidence: Assets/_Graphics/Shaders/SpectrogramUnlit.shader:1-14; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:40-48; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/UnlitSpectrogram/_shader.json
   - Limits: Diagnostic OVERDRAW_VIEW is omitted.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/SpectrogramUnlit.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/SpectrogramUnlit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/SpectrogramUnlit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/SpectrogramShared.hlsl` (`87dcbfeeb9272e782759e1b92564675afde6abee53344b1b3e15ccd32171df02`).

### Catalog limits

- No committed visual parity capture was found.

## 25. `ChroMapper/Stencil`

- Path: `Assets/_Graphics/Shaders/Stencil.shader`
- Source SHA-256: `48de64f2e74d7348e2413d0bd7101850338f911a41b7ef3a0baf43ab03106862`
- Behavior matrix source SHA-256: `48de64f2e74d7348e2413d0bd7101850338f911a41b7ef3a0baf43ab03106862`
- Source join: matched.
- ABI SHA-256: `3df74f6549c20995e5387f1cb0e4e7a93d5e0973b26808defaa83e2b03c7290d`
- Classification: `recovered_replacement`
- Family and confidence: `depth_stencil` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_StencilRefValue -> 1:_StencilComp -> 2:_StencilPass -> 3:_CullMode.

### Ordered SubShaders and passes

1. SubShader 0: tags [Queue=Geometry-1, RenderType=Opaque], states [none], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Blend Zero One, Zero One | ZClip On | ZWrite Off | Cull [_CullMode]], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].

### Ordered programs

1. Program 0 `CGPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc].
   - Pragmas: vertex vert -> fragment frag -> multi_compile _ STEREO_INSTANCING_ON.

### Audited stage flow

1. `STENCIL` / `vertex+fragment` / `EXACT_SHARED`: Transforms POSITION to clip space, writes zero RGBA, preserves destination color with zero/one blend, and updates only the configured stencil operation.
   - Evidence: Assets/_Graphics/Shaders/Stencil.shader:7-18; Assets/_Graphics/Shaders/Stencil.shader:35-100; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SimpleStencil/_shader.json
   - Limits: Ref/material values differ by environment; stage binaries do not prove ShaderLab state.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/Stencil.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).

### Catalog limits

- No standalone audit; render-state parity relies on source shell/established state, not stage ASM.

## 26. `ChroMapper/Water Lit`

- Path: `Assets/_Graphics/Shaders/WaterLit.shader`
- Source SHA-256: `6ca64b42647c56a9fa89d3d013b69bc7ea3cc381303948368d96e95f4eb3e370`
- Behavior matrix source SHA-256: `d1c617d56d680d6b7ff2a355590ee8812ffc6277e462dc4f9dc6a8a3b65bf44a`
- Source join: different audit snapshot; semantic delta unverified.
- ABI SHA-256: `a1effc092d31cff4cc15c25a0da4f0c30c15bf8ccd150cb59d719120c9496271`
- Classification: `recovered_replacement`
- Family and confidence: `water` / `high`

### Ordered root contract

- Root commands: none.
- Properties: 0:_Color -> 1:_EnableMetalSmoothnessTex -> 2:_MetalSmoothnessTex -> 3:_Metallic -> 4:_Smoothness -> 5:_SpecularAntiflicker -> 6:_AntiflickerStrength -> 7:_AntiflickerDistanceScale -> 8:_AntiflickerDistanceOffset -> 9:_VertexMode -> 10:_EmissionThreshold -> 11:_EmissionColor -> 12:_ZFade -> 13:_ZFadePosition -> 14:_ZFadeScale -> 15:_YFade -> 16:_YFadePosition -> 17:_YFadeScale -> 18:_EmissionTexture -> 19:_EmissionBrightness -> 20:_EmissionColorType -> 21:_EmissionTexColor -> 22:_EmissionGradientTex -> 23:_EmissionTex -> 24:_EmissionTexSpeed -> 25:_EmissionSampleTwice -> 26:_Emission2Tiling -> 27:_Emission2Speed -> 28:_PulseMask -> 29:_InvertPulseTexture -> 30:_PulseMultiplyByTexture -> 31:_PulseWidth -> 32:_PulseSpeed -> 33:_PulseSmooth -> 34:_EnableEmissionMask -> 35:_EmissionMask -> 36:_EmissionMaskSpeed -> 37:_RimLight -> 38:_RimLightEdgeStart -> 39:_RimLightColor -> 40:_RimLightIntensity -> 41:_EnableDiffuse -> 42:_EnableLightFalloff -> 43:_InvertDiffuseNormal -> 44:_EnableBothSidesDiffuse -> 45:_PrivatePointLight -> 46:_PrivatePointLightColor -> 47:_PointLightPositionLocal -> 48:_PrivatePointLightPosition -> 49:_EnableDiffuseTexture -> 50:_DiffuseTex -> 51:_EnableSpecular -> 52:_SpecularIntensity -> 53:_EnableLightmap -> 54:_EnableNormalMap -> 55:_NormalTex -> 56:_NormalScale -> 57:_NormalScaleVertical -> 58:_NormalTexScrolling -> 59:_DetailNormalMap -> 60:_DetailNormalTextureScale -> 61:_DetailNormalIntensity -> 62:_DetailNormalTexScrolling -> 63:_UseSphericalNormalOffset -> 64:_SphericalNormalOffsetIntensity -> 65:_SphericalNormalOffsetCenter -> 66:_EnableReflectionTexture -> 67:_ReflectionTexIntensity -> 68:_EnvironmentReflectionCube -> 69:_EnableReflectionProbe -> 70:_ReflectionProbeIntensity -> 71:_ReflectionProbeBoxProjection -> 72:_EnableBoxProjectionOffset -> 73:_ReflectionProbeBoxProjectionSizeOffset -> 74:_ReflectionProbeBoxProjectionPositionOffset -> 75:_EnableRimDim -> 76:_RimScale -> 77:_RimOffset -> 78:_RimDistanceOffset -> 79:_RimDistanceScale -> 80:_RimDarkening -> 81:_InvertRimDim -> 82:_EnableGroundFade -> 83:_GroundFadeScale -> 84:_GroundFadeOffset -> 85:_EnableDirt -> 86:_DirtTex -> 87:_DirtIntensity -> 88:_EnableDirtDetail -> 89:_DirtDetailTex -> 90:_DirtDetailIntensity -> 91:_RotateUV -> 92:_EnableFog -> 93:_FogStartOffset -> 94:_FallingFogStartOffset -> 95:_FogScale -> 96:_EnableHeightFog -> 97:_FogHeightScale -> 98:_FogHeightOffset -> 99:_WhiteBoostType -> 100:_EnableNoiseDithering -> 101:_LinearToGamma -> 102:_CullMode -> 103:_ZWrite -> 104:_StencilRefValue -> 105:_StencilComp -> 106:_StencilPass -> 107:_BlendModeSrc -> 108:_BlendModeDst -> 109:_BlendModeSrcA -> 110:_BlendModeDstA.

### Ordered SubShaders and passes

1. SubShader 0: tags [RenderType=Opaque], states [Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA] | Cull [_CullMode] | ZTest LEqual | ZWrite [_ZWrite]], stencil [none].
   1. Pass 0 (`<unnamed>`): tags [none], states [Fog .hlsl"], stencil [Ref [_StencilRefValue], Comp [_StencilComp], Pass [_StencilPass]].

### Ordered programs

1. Program 0 `HLSLPROGRAM` (pass 0): entries [vertex:vert -> fragment:frag], target [implicit], includes [@unity_builtin/UnityCG.cginc -> Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl -> Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl].
   - Pragmas: vertex vert -> fragment frag -> multi_compile_instancing -> multi_compile _ STEREO_INSTANCING_ON -> shader_feature_local_fragment Z_FADE -> shader_feature_local_fragment NORMAL_MAP -> shader_feature_local_fragment DETAIL_NORMAL_MAP -> shader_feature_local_fragment LIGHTMAP -> shader_feature_local_fragment NOISE_DITHERING -> shader_feature_local_fragment FOG -> shader_feature_local_fragment HEIGHT_FOG -> shader_feature_local_fragment REFLECTION_PROBE -> shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION -> shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET -> shader_feature_local_fragment _ _EMISSIONCOLORTYPE_FLAT -> shader_feature_local_fragment _ _DECALBLEND_ALPHABLEND -> multi_compile_fragment _ BLOOM_FOG -> multi_compile_fragment _ ACES_TONE_MAPPING.

### Audited stage flow

1. `WATER` / `vertex+fragment` / `SHARED_WITH_COMPILE_TIME_PARAMETERS`: Recovered production routes use normal/detail normals, packed lightmap or probe reflection, Z fade, ACES, bloom height fog and post-fog dither; unsupported source feature properties remain serialized.
   - Evidence: Assets/_Graphics/Shaders/WaterLit.shader:282-417; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:9-27; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/WaterLit/_shader.json
   - Limits: Unity spec-cube fallback is not the recovered packed-probe route; see WATER-DEFECT.
1. `WATER-DEFECT` / `reflection route` / `DEFECT_CANDIDATE`: Recovered WaterLit samples and decodes two packed reflection cubes; current source uses Unity spec-cube inputs in REFLECTION_PROBE, while Billie/Gaga ReflectionProbeData references are null.
   - Evidence: Assets/_Graphics/Shaders/WaterLit.shader:344-379; Assets/_Graphics/Shaders/WaterLit.shader:22-25; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:13-25
   - Limits: Replacement needs packed probe assets and runtime/material evidence; no semantic fix was made.

### Recursive include order

1. Include 0 at depth 0: `Assets/_Graphics/Shaders/WaterLit.shader` -> `@unity_builtin/UnityCG.cginc` (`b3eaeccf5d1ead844abb5c1023367ed06a000882089bb46c38e87e385bd28f5e`).
1. Include 1 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 2 at depth 2: `@unity_builtin/UnityShaderVariables.cginc` -> `@unity_builtin/HLSLSupport.cginc` (`f8bd801346280b53bb1f64baa761ad55f6a44be5de701c7580f47bd116eded64`).
1. Include 3 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityShaderUtilities.cginc` (`99aee089004d6476edac833d57b080acb6523e6cfe583e660e7ebab0cf056b79`).
1. Include 4 at depth 2: `@unity_builtin/UnityShaderUtilities.cginc` -> `@unity_builtin/UnityShaderVariables.cginc` (`e6e421c3c64550c886eed2ca9c0442093305393c87d89004a0436feabf65b90c`).
1. Include 5 at depth 1: `@unity_builtin/UnityCG.cginc` -> `@unity_builtin/UnityInstancing.cginc` (`fcb9305be528bf0ed08cf410535fb6379d6c672ee06356d24b807f373550c37e`).
1. Include 6 at depth 0: `Assets/_Graphics/Shaders/WaterLit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` (`1f7c001a5ec0f4e01f5a3e8720b06d0f54043e7f920e747c9c180b375569f992`).
1. Include 7 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Fog.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 8 at depth 0: `Assets/_Graphics/Shaders/WaterLit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` (`f9c1fa70ebb8871b1afd9816689ce2ff33eac1b8a3af60d65756d9a76bd2e544`).
1. Include 9 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 10 at depth 1: `Assets/_Graphics/Shaders/ShaderLibrary/Reflection.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` (`c5459cc13ba02a75bc16403af4906b2a86a80679f35b82246b71fab9d0d4e581`).
1. Include 11 at depth 2: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Data.hlsl` (`4119e468cd6d9500ed69a02c9e5dd6cb3437b1bfeb99aa17ddc11473c06a551f`).
1. Include 12 at depth 2: `Assets/_Graphics/Shaders/ShaderLibrary/Lighting.hlsl` -> `Assets/_Graphics/Shaders/ShaderLibrary/Camera.hlsl` (`afe8320d1095e114f6525f6b361e56d46bfe49c90020765fa8820912e652125d`).
1. Include 13 at depth 0: `Assets/_Graphics/Shaders/WaterLit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/Tonemapping.hlsl` (`aabcc091d98de8c5e62ed82c7020c82dc4e0b7d23529bd0a5c2078fc4ed42e91`).
1. Include 14 at depth 0: `Assets/_Graphics/Shaders/WaterLit.shader` -> `Assets/_Graphics/Shaders/ShaderLibrary/PostProcess.hlsl` (`4ce764a2bd93c49cdc26a924dcbfba168874376823f00cdd6d59a68dbd5ea6ae`).

### Catalog limits

- Packed dual-cubemap reflection route is not implemented because required probe assets/data are null.
