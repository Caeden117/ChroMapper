# Recovered Shader Audit

## Purpose

This directory is the Phase 1-3 audit baseline for recovered shaders. It documents source contracts and evidence limits.

The files do not claim complete visual or runtime parity. The audit did not run Unity during publication.

## Authoritative scope

`RecoveredShaderCatalog.json` defines the 26-entry recovered scope. Names alone do not qualify a shader for this catalog.

The behavior scan found 29 evidence candidates. It used a broader source and binary evidence rule.

The three non-catalog candidates are `GrabPassTexture1.shader`, `Post Process/BlitBlendColor.shader`, and `Post Process/ChromaticAberration.shader`. Their status is `adjacent_unverified`.

## Baseline

- Audit date: `2026-09-03`.
- ChroMapper version: `0.14.0`.
- Unity version: `6000.3.13f1 (8c4f11e4fb20)`.
- Historical shader corpus: Beat Saber `1.44.3`.
- ABI schema: `1.0.0`.

## Files and schemas

- `RecoveredShaderCatalog.json` contains catalog metadata and 26 `recovered_shaders` objects.
- `RecoveredShaderCatalog.md` is the human-readable catalog.
- `RecoveredShaderABI.before.json` contains 26 ordered ShaderLab ABI snapshots.
- `RecoveredShaderFeatureMatrix.csv` contains 232 behavior rows and 30 original behavior fields.
- The feature matrix adds `catalog_scope_status` and `catalog_confidence` fields.

Twelve catalog shaders have different catalog and behavior-matrix source hashes. `RecoveredShaderPipelines.md` marks each cross-snapshot join.
- `RecoveredShaderDependencyGraph.json` contains all catalog include closures and the Lit semantic graph.
- `RecoveredShaderPipelines.md` joins ABI order with behavior operation order for all 26 catalog shaders.
- `RecoveredShaderExactDuplication.md` contains four all-source exact groups and zero recovered members.
- `RecoveredShaderSimilarNotEquivalent.md` contains 17 do-not-merge records.
- `RecoveredShaderSharedCandidates.md` contains reviewed sharing candidates.
- `RecoveredShaderIncludeClassification.md` contains 16 include placement records.
- `RecoveredShaderDefectCandidates.md` contains 11 evidence records. It does not define fixes.
- `RecoveredShaderVariantCandidates.md` contains 29 compile-time matrix candidates. It does not prove runtime reachability.
- `RecoveredShaderConsumerReport.md` reconciles consumer, mesh, platform, and compiled counts.
- `RecoveredShaderEvidenceGaps.md` keeps the catalog and cross-phase uncertainty registers.

Arrays preserve source order unless a schema states that an array is a set. All repository paths use POSIX separators.

## Confidence taxonomy

- `HIGH` means that exact binary or audit evidence agrees with the current implementation contract.
- `MEDIUM` means that source, runtime, or property evidence is strong, but one evidence join is incomplete.
- `LOW` means that the result uses static inference, an unsupported route, or unresolved ownership.

The catalog also uses compound labels such as `medium-high` and `low-medium`. These labels remain unchanged from the catalog evidence.

Comparison classifications and confidence are separate fields. An equal operation hash is only a review candidate.

## ABI contract

The ABI comparison covers these ordered fields:

- Shader declaration and root commands.
- Property names, attributes, display names, types, and defaults.
- SubShader, pass, tag, render-state, and stencil values.
- Program kinds and pass ownership.
- Pragmas, keyword groups, entry points, targets, and profile constraints.
- Instancing declarations, bindings, structs, Unity built-ins, and output targets.
- Direct include order and resolved include identity.

Source hashes, byte counts, line numbers, formatting records, and recursive include hashes are evidence fields. They are not ABI fields.

The comparator accepts explicit path relocations. A relocation does not suppress a listed ABI change.

## Regeneration

Run the parser from the repository root:

```sh
python3 Assets/_Graphics/Shaders/Audit/Recovered/Tools/parse_recovered_shader_abi.py \
  --repo . \
  --out Assets/_Graphics/Shaders/Audit/Recovered/GeneratedABI
```

If the Unity `CGIncludes` directory is available, set `UNITY_BUILTIN_INCLUDE_ROOT` to that directory.

The parser records unresolved built-ins when this variable is not set.

Compare the baseline with a generated snapshot:

```sh
python3 Assets/_Graphics/Shaders/Audit/Recovered/Tools/compare_recovered_shader_abi.py \
  Assets/_Graphics/Shaders/Audit/Recovered/RecoveredShaderABI.before.json \
  Assets/_Graphics/Shaders/Audit/Recovered/GeneratedABI/abi_before.json
```

For an expected move, add one or more `--relocation OLD=NEW` arguments. Both values must be repository-relative prefixes.

The publication reconciles these source artifact roots:

- `/tmp/opencode/recovered_shader_refactor_20260903/{catalog,abi,behavior,consumers,mesh_platform}`
- `/tmp/opencode/lit_modularization_20260903/{source_audit,control_audit,platform_mesh_audit,evidence_audit,compiled_baseline}`

The source artifacts are not durable repository inputs. Regeneration depends on the copied parser and current repository sources.

## Validation limits

The parser does not run the Unity ShaderLab parser or an HLSL compiler. It does not expand preprocessor variants.

The compiled baseline came from an earlier Phase 3 Unity run. This publication did not run Unity.
