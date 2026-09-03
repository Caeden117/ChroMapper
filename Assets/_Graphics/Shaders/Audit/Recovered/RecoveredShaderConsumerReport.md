# Recovered Shader Consumer Report

## Scope

The authoritative catalog contains 26 shaders. The environment migration map contains 16 source-to-target shader mappings.

These sets have different purposes. The migration count does not reduce the recovered catalog scope.

## Baseline

- Audit date: `2026-09-03`.
- ChroMapper version: `0.14.0`.
- Unity version: `6000.3.13f1 (8c4f11e4fb20)`.
- Historical shader corpus: Beat Saber `1.44.3`.

## Consumer counts

- Environment data: 47 JSON files or scenes, with 1,197 source material rows across 17 shader names.
- Generated environment materials: 927 `.mat` files. The repository contains 987 `.mat` files.
- Main generated material counts: Lit 505, Particles 220, Parametric Slice Billboard 71, and Mirror 42.
- Curated consumer data: 66 property rows, 16 keyword rows, and 34 shader-consumer rows.
- Static source hits: 229 `Shader.PropertyToID`, 262 material or MPB setters, 170 global setters, and 48 keyword hits.
- Dynamic scenes: 47 of 47 environment scenes contain MPB consumer components.
- Dynamic bindings: 6,951 color bindings and 532 value bindings.
- Spectrogram bindings: 73 `_DistortionStrength` rows and 32 `_DisplacementStrength` rows.

The counts describe different layers. Do not add or compare them as one population.

## ABI-sensitive consumers

Dynamic property names can fail without a compiler error. The scene data contains approximately 7,700 MPB property-name bindings.

Global keyword ownership is split across runtime controllers. The owned names include `BLOOM_FOG`, `ACES_TONE_MAPPING`, `POST_BLOOM`, `DEPTH_TEXTURE`, and `CM_PREVIEW_MODE`.

The migration process depends on 29 property remaps and 13 keyword remaps. Shader, property, and keyword names are ABI fields for this audit.

The code also uses string shader lookups. Important names include `ChroMapper/Sky Gradient`, `ChroMapper/Object/Note`, and `ChroMapper/Object/Obstacle Distortion`.

## Variant and build consumers

- Shader variant collections: 0.
- `IPreprocessShaders` or shader-stripper implementations: 0.
- Preloaded shaders: 0.
- Detailed Always Included Shaders entries: 14.
- Always-included composition: 9 built-ins, 3 resolved project shaders, and 2 dangling project GUIDs.

The detailed 14-entry snapshot replaces an earlier summary that reported one unresolved GUID.

## Mesh payload evidence

- Environment objects: 61,612.
- `MeshFilter` rows: 18,142.
- `MeshRenderer` rows: 18,126.
- Distinct environment mesh hashes: 389, with 83 missing hashes.
- Repository mesh assets decoded: 54.
- Decoded channels: 16 color, 17 UV1, 1 UV2, and 0 UV3.
- Current `_MeshPackingId` material values: `0` on 14, `1` on 450, `2` on 1, `4` on 1, and `6` on 1.

Mesh IDs 3 and 5 exist in mesh evidence. No current material uses these IDs. The audit does not infer the missing join.

## Compiled evidence

The historical corpus contains 199,684 stage rows and 4,607 unique DXBC containers. It contains 4,352 reconstructed HLSL files and 255 ASM-only hashes.

The current Lit compile probe selected 268 variants from 237 observed combinations. All 1,608 stage and API compilations succeeded.

The probe contains 536 records for each of D3D11, Vulkan, and OpenGLCore. All 30 SPIR-V validation runs and 30 disassembly runs succeeded.

Compilation success does not prove equivalence to the historical shader. The 4,607 historical binaries and 268 current variants use different scopes.

## Main risks

1. Property or keyword renames can break serialized consumers.
2. External map content uses canonical keyword names.
3. Runtime MPB values and selected variants remain unverified.
4. External user bundles are outside the repository scan.
5. No frame capture or visual comparison establishes parity.

## Regeneration evidence

The report reconciles these Phase 1-3 artifacts:

- `recovered_shader_refactor_20260903/consumers/report.md`
- `recovered_shader_refactor_20260903/consumers/material_manifest.json`
- `recovered_shader_refactor_20260903/mesh_platform/report.md`
- `lit_modularization_20260903/control_audit/report.md`
- `lit_modularization_20260903/platform_mesh_audit/report.md`
- `lit_modularization_20260903/evidence_audit/report.md`
- `lit_modularization_20260903/compiled_baseline/report.md`
