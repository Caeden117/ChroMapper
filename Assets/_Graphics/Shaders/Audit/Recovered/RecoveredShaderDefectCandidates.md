# Defect candidates

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their behavior records remain available with an `adjacent_unverified` scope status.

## 1. /cross_shader_classification/10

- Classification: `DEFECT_CANDIDATE`
- Source: environment.json
- Confidence: medium
- Evidence: WaterLit:392 factor 4.59479332 (audit W5 exact) vs Mirror:249 factor 4.594793 (truncated 3e-8 relative). Same bake-ID decode sum shape; needs binary-authority check before sharing.
- Regions: fragment lightmap decode (WaterLit vs Mirror)

## 2. /regions/35

- Classification: `DEFECT_CANDIDATE`
- Source: particles.json
- Behavior: Dormant-but-serialized hazard: 7 current materials request unsupported VERTEX_DISPLACEMENT; 11 active MeshRenderer bindings can reach them (reaudit). Selectable-but-uncompiled enum values compile to None/off silently: DISTORTION Flowmap, CUTOUT Scale, SECONDARY_UVS Trails/ObjectSpace, SPECTROGRAM Flat, OVERRIDE_FLAT, WHITEBOOST GreenChannel, MOTION_VECTORS on, EROSION/WORLD_NOISE/REVEAL/CHROMATIC/DISSOLVE-extras on, VERTEX_START_END on, _VERTEXCHANNELS_RGB. Fragment LIFETIME keyword wide-compile with no frag body can mislead readers into expecting frag gating. VERTEX_SQUARE_ALPHA wide-compile with frag-only consumption ditto. _BaseLayer RGB-only scaling is a copy-paste hazard (full-multiply lookalike). FAKE_MIRROR material-owned path without global runtime owner + stale source data (35 FakeMirror markers are transforms-only; 0 particle mats enable it) — enabling without owner/data change is unsafe.
- Confidence: high (static) / medium (blast radius needs Populate Build Data run)
- Evidence: Body-absence grep; PARTICLE_REAUDIT.md Full-corpus (7 mats/11 bindings; fake-mirror owner; Build Data authoritative-keyword behavior).
- Guards: The dormant selectors + wide-compiled LIFETIME/VERTEX_SQUARE_ALPHA.
- Id: P-DEFECT-A
- Lines: Props L28-245 vs body 392+; pragmas L299/343-344
- Operation Signatures: (hazard = UI suggests code where body has none; see P-UNVERIFIED-B list)
- Rgb Alpha: Hazard: enabling dormant alpha paths (erosion/dissolve-extras/green/gradient-alpha) silently does nothing (or falls to None), masking data errors.
- Sampling: Hazard: dormant samplers suggest sampling that never happens.
- Sharing Candidates: Do NOT share dormant names as contracts; fix source data + regenerate (Populate Build Data), do not add shader predicates (per reaudit).
- Sharing Class: DEFECT_CANDIDATE
- Spaces: N/A.
- Stage: config+both
- Time: N/A.
- Title: Dormant-but-serialized hazard

## 3. /regions/36

- Classification: `DEFECT_CANDIDATE`
- Source: particles.json
- Behavior: Generated-data drift (ownership/mapping, NOT shader predicates): 6 current source routes disagree with 3 generated material assets + 123 referenced routes do not resolve to expected material in matching saved environment scene (reaudit) — regenerate via Populate Build Data, do not edit manually; preserve explicit source queues (queue is not a keyword, not proven by hashes); environment SO _CustomZWrite wins over conflicting exported particle _ZWrite (conversion zero); dangling material pointers are data defects (correct source, regenerate). MESH_PACKING collapse-point divergence (origin vs game point) is a latent visual-parity risk despite raster-equivalence claim. Full-res depth neutralizing game bias is intentional but is a divergence to carry in any shared depth helper. STEREO_INSTANCING_ON as global multi_compile + local-feature stripping interaction is a build-risk to re-verify after lattice edits.
- Confidence: medium
- Evidence: PARTICLE_REAUDIT.md Full-corpus findings (6v3+123; queue; _CustomZWrite; dangling pointers; collapse point; depth bias).
- Guards: N/A (data/build).
- Id: P-DEFECT-B
- Lines: PARTICLE_REAUDIT.md Full-corpus findings
- Operation Signatures: Populate Build Data (imported keyword lists authoritative; declared locals only; runtime globals untouched)
- Rgb Alpha: Stale assets can carry wrong blend/cutout/alpha state into the fixed-order chain; regeneration, not shader branching, is the fix.
- Sampling: N/A.
- Sharing Candidates: No shader sharing; process fix (regenerate), not code.
- Sharing Class: DEFECT_CANDIDATE
- Spaces: N/A.
- Stage: build/data
- Time: N/A.
- Title: Generated-data drift and build risks

## 4. ReflectionProbeBakePipeline.ApplyBloom vs BloomAlphaGate(_BloomParams.z)

- Classification: `DEFECT_CANDIDATE`
- Source: post_bloom.json
- Evidence: Bake driver sets bloomMaterial _BloomThreshold (material float) while shader/include chain reads _BloomParams.z (global vector set only by runtime RecordRender:136-137); bake prefilter passes 0/1 therefore likely read stale/zero alpha-gate unless a global _BloomParams is set elsewhere. Flagged, no fix per assignment scope.
- Operation: ReflectionProbeBakePipeline.ApplyBloom vs BloomAlphaGate(_BloomParams.z)
- Users: ReflectionProbeBakePipeline; Bloom.shader passes 0,1
- Note: defect-candidate (needs owner verification)

## 5. /behavior_rows/29

- Classification: `DEFECT_CANDIDATE`
- Source: scope_remaining.json
- Id: PSB-DEFECT
- Shader Path: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader
- Stage: fragment/vertex contract
- Behavior: The audited exact cap UV base is 0.25, but the current vertex formula uses literal 0.36 in adjustedUvY.
- Comparison Class: DEFECT_CANDIDATE
- Evidence: Assets/_Graphics/Shaders/ParametricSliceBillboard.shader:265-270; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:11-18
- Limits: No fix was made. This report only records the source/evidence discrepancy.

## 6. /behavior_rows/30

- Classification: `DEFECT_CANDIDATE`
- Source: scope_remaining.json
- Id: PFG-DEFECT
- Shader Path: Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader
- Stage: height-ramp helper
- Behavior: The recovered DXBC divides by the global height directly; ParametricShared.hlsl clamps the denominator with max(globalHeight, 1e-5), changing zero/almost-zero height behavior.
- Comparison Class: DEFECT_CANDIDATE
- Evidence: Assets/_Graphics/Shaders/ShaderLibrary/ParametricShared.hlsl:11-19; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:32-35
- Limits: The discrepancy is limited to zero or near-zero global height and was not changed.

## 7. /behavior_rows/31

- Classification: `DEFECT_CANDIDATE`
- Source: scope_remaining.json
- Id: WATER-DEFECT
- Shader Path: Assets/_Graphics/Shaders/WaterLit.shader
- Stage: reflection route
- Behavior: Recovered WaterLit samples and decodes two packed reflection cubes; current source uses Unity spec-cube inputs in REFLECTION_PROBE, while Billie/Gaga ReflectionProbeData references are null.
- Comparison Class: DEFECT_CANDIDATE
- Evidence: Assets/_Graphics/Shaders/WaterLit.shader:344-379; Assets/_Graphics/Shaders/WaterLit.shader:22-25; Assets/_Graphics/Shaders/WATER_SPECTROGRAM_REAUDIT.md:13-25
- Limits: Replacement needs packed probe assets and runtime/material evidence; no semantic fix was made.

## 8. Arc HEIGHT_FOG selector

- Classification: `DEFECT_CANDIDATE`
- Source: parametric_object.json
- Operation: Arc HEIGHT_FOG selector
- Confidence: MEDIUM
- Evidence: HEIGHT_FOG is declared/property-backed but no Arc operation reads it; binary-backed Arc evidence covers the joint PRECISE_FOG/_FOGTYPE_ALPHA route only.

## 9. ObstacleDistortion duplicate BLOOM_FOG pragma token

- Classification: `DEFECT_CANDIDATE`
- Source: parametric_object.json
- Operation: ObstacleDistortion duplicate BLOOM_FOG pragma token
- Confidence: HIGH
- Evidence: Current source declares `#pragma multi_compile_fragment _ BLOOM_FOG BLOOM_FOG`; duplicate selector is syntactic and no semantic fix is proposed.

## 10. Stereo pragma completeness for Arc, Rain, Unlit, ObstacleOutline and ObstacleDistortion

- Classification: `DEFECT_CANDIDATE`
- Source: parametric_object.json/includes.json
- Operation: Stereo pragma completeness for Arc, Rain, Unlit, ObstacleOutline and ObstacleDistortion
- Confidence: MEDIUM
- Evidence: These consumers use some Camera/stereo helpers or macros without the complete STEREO_INSTANCING_ON pragma/setup contract. Current non-stereo behavior is known; recovered stereo equivalence needs runtime/binary proof.

## 11. Lit scalar noise-screen-position fallback

- Classification: `DEFECT_CANDIDATE`
- Source: includes.json
- Operation: Lit scalar noise-screen-position fallback
- Confidence: MEDIUM
- Evidence: Lit.shader line 2687 calls ApplyNoiseDither(albedo, 0.0, ...); scalar splat makes the helper divide 0/0 for UV behind !USE_NOISE_SCREEN_POSITION. Runtime/variant proof is required; no fix is proposed.
