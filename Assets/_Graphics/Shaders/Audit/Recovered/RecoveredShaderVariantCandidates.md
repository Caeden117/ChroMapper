# Variant candidates

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their behavior records remain available with an `adjacent_unverified` scope status.

## 1. Record 1

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/BloomfogMesh
- Path: BloomFog/BloomfogMesh.shader
- Pragmas: fragment frag; multi_compile _ BLOOM_FOG; vertex vert
- Guards: defined(BLOOM_FOG)
- Evidence Markers: BM1; BM2; BM3; BM4
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 2. Record 2

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/BloomfogSkybox
- Path: BloomFog/BloomfogSkybox.shader
- Pragmas: fragment frag; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; target 3.0; vertex vert
- Guards: defined(BLOOM_FOG); defined(UNITY_REVERSED_Z)
- Evidence Markers: BS1; BS2; BS3
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 3. Record 3

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Sky Gradient
- Path: BloomFog/SkyGradient.shader
- Pragmas: fragment frag; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ USE_TONE_MAPPING; target 3.5; vertex vert
- Guards: defined(USE_TONE_MAPPING) || defined(ACES_TONE_MAPPING)
- Evidence Markers: SG4
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 4. Record 4

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Clouds Lit Transparent
- Path: CloudsLitTransparent.shader
- Pragmas: fragment frag; multi_compile _ ACES_TONE_MAPPING; multi_compile _ STEREO_INSTANCING_ON; multi_compile_instancing; shader_feature_local ALIGN_NORMALS_TO_WORLD_ORIGIN; shader_feature_local_fragment BACK_LIGHTING; shader_feature_local_fragment DIFFUSE; shader_feature_local_fragment DIFFUSE_TEXTURE; shader_feature_local_fragment DISTORT_TEXTURE; shader_feature_local_fragment FADE_BOTTOM; shader_feature_local_fragment FADE_RUNWAY; shader_feature_local_vertex VERTEX_WAVE; shader_feature_local_vertex _VERTEXMODE_ROTATELAYERS; vertex vert
- Guards: defined(ACES_TONE_MAPPING); defined(ALIGN_NORMALS_TO_WORLD_ORIGIN); defined(ALIGN_NORMALS_TO_WORLD_ORIGIN) && \; defined(VERTEX_WAVE); defined(_VERTEXMODE_ROTATELAYERS)
- Evidence Markers: 732b1e460d13565d; LT1; LT10; LT11; LT12; LT13; LT14; LT15; LT2; LT3; LT4; LT5; LT6; LT7; LT8; LT9; fragment-732b1e460d13565d; fragment-c0bbcfd6116001fd; vertex-5fb9ced67522fa87
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 5. Record 5

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Clouds Opaque
- Path: CloudsOpaque.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local_fragment BOTH_SIDES_DIFFUSE; shader_feature_local_fragment DIFFUSE; shader_feature_local_fragment FOG; shader_feature_local_fragment INVERT_DIFFUSE_NORMAL; shader_feature_local_fragment NOISE_DITHERING; shader_feature_local_vertex WORLD_NOISE; vertex vert
- Guards: defined(ACES_TONE_MAPPING); defined(BLOOM_FOG); defined(DIFFUSE); defined(FOG); defined(INVERT_DIFFUSE_NORMAL); defined(NOISE_DITHERING); defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON); defined(WORLD_NOISE)
- Evidence Markers: 485a517d5978ccac; O1; O10; O11; O12; O2; O3; O4; O5; O6; O7; O8; O9; fragment-119bc5fd; fragment-119bc5fdf0893e09; fragment-19822184; fragment-40070c00; fragment-72154a52; vertex-1effd9ad; vertex-1effd9ad752a9c6e
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 6. Record 6

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Glowing
- Path: Glowing.shader
- Pragmas: fragment frag; multi_compile _ POST_BLOOM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT; vertex vert
- Guards: defined(BLOOM_FOG); defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)
- Evidence Markers: 03903b03afec0941; 6ff954e28b4d0c9d; 76795e0bec45bc2d; G1; G2; G3; G4; G5; G6; G7; G8; a5ad0fe89169ad76; be31848f99a79f3e; c06f1f3a78ccef72; dd1cfb62808fef87; e31bc067e9b5738a
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 7. Record 7

- Classification: `FAMILY_SHARED`
- Shader: Custom/GrabPassTexture1
- Catalog Scope Status: `adjacent_unverified`
- Path: GrabPassTexture1.shader
- Pragmas: fragment frag; vertex vert
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 8. Record 8

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Lightning
- Path: Lightning.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_instancing; shader_feature_local_vertex _ ENABLE_TARGET_POINT; shader_feature_local_vertex _ ENABLE_TIME_OFFSET; vertex vert
- Guards: defined(ENABLE_TARGET_POINT); defined(ENABLE_TIME_OFFSET)
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 9. Record 9

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Lit
- Path: Lit.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ BLOOM_FOG; multi_compile_fragment _ POST_BLOOM; multi_compile_instancing; shader_feature_local COLOR_ARRAY; shader_feature_local COLOR_BY_FOG; shader_feature_local DIFFUSE; shader_feature_local DIRECTIONAL_RIM; shader_feature_local DISSOLVE_TEXTURE; shader_feature_local EMISSION_ANGLE_DISAPPEAR; shader_feature_local FOG_COLOR_HIGHLIGHT; shader_feature_local HIGHLIGHT_SELECTION; shader_feature_local INSTANCED_PRIVATE_POINT_LIGHT; shader_feature_local INVERT_RIM_DIM; shader_feature_local LIGHTMAP; shader_feature_local MESH_PACKING; shader_feature_local NOISE_DITHERING; shader_feature_local NORMAL_MAP; shader_feature_local OCCLUSION_BEFORE_EMISSION; shader_feature_local OCCLUSION_DETAIL; shader_feature_local PARALLAX_IRIDESCENCE; shader_feature_local PRECISE_NORMAL; shader_feature_local PRIVATE_POINT_LIGHT; shader_feature_local REFLECTION_PROBE; shader_feature_local REFLECTION_STATIC; shader_feature_local REFLECTION_TEXTURE; shader_feature_local RIM_DIM; shader_feature_local SECONDARY_UVS_EMISSION; shader_feature_local SECONDARY_UVS_EMISSION_MASK; shader_feature_local SECONDARY_UVS_EMISSION_MASK2; shader_feature_local SECONDARY_UVS_MPM; shader_feature_local SECONDARY_UVS_OCCLUSION; shader_feature_local SECONDARY_UVS_OCCLUSION_DETAIL; shader_feature_local SECONDARY_UVS_PARALLAX; shader_feature_local SECONDARY_UVS_PULSE; shader_feature_local SPECULAR; shader_feature_local SPECULAR_ANTIFLICKER; shader_feature_local TEXTURE3D_EMISSION; shader_feature_local TEXTURE3D_LOOKUP; shader_feature_local USE_SPHERICAL_NORMAL_OFFSET; shader_feature_local UV_COLOR_SEGMENTS; shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE; shader_feature_local _ _EMISSIONCOLORTYPE_GRADIENT \; shader_feature_local _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_PULSE \; shader_feature_local _ _HOLOGRAM_GRID _HOLOGRAM_SCANLINE _HOLOGRAM_LEGACY; shader_feature_local _ _METALLIC_TEXTURE_MPM_R; shader_feature_local _ _RIMLIGHT_LERP _RIMLIGHT_ADDITIVE; shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE _SECONDARY_UVS_OBJECT_SPACE _SECONDARY_UVS_ADDITIVE_OFFSET; shader_feature_local _ _SMOOTHNESS_TEXTURE_MPM_A \; shader_feature_local _ _VERTEXMODE_COLOR _VERTEXMODE_EMISSION \; shader_feature_local _ _VERTEX_WHITEBOOSTTYPE_MAINEFFECT \; shader_feature_local _DISSOLVE_SPACE_WORLD_CENTERED; shader_feature_local _DISTORTION_TARGET_EMISSIONTEX; shader_feature_local _OCCLUSION_SOURCE_MPM_B; shader_feature_local _PARALLAX_FLEXIBLE_REFLECTED; shader_feature_local _PROBE_CALCULATION_PRECISE; shader_feature_local _RIM_WHITEBOOSTTYPE_MAINEFFECT; shader_feature_local_fragment BOTH_SIDES_DIFFUSE; shader_feature_local_fragment DIFFUSE_TEXTURE; shader_feature_local_fragment DISSOLVE; shader_feature_local_fragment DISSOLVE_COLOR; shader_feature_local_fragment DISSOLVE_PROGRESS; shader_feature_local_fragment DISTANCE_DARKENING; shader_feature_local_fragment DISTORTION_SIMPLE; shader_feature_local_fragment EMISSION_MASK; shader_feature_local_fragment FLIPBOOK_BLENDING_OFF; shader_feature_local_fragment FOG; shader_feature_local_fragment GROUND_FADE; shader_feature_local_fragment HEIGHT_FOG; shader_feature_local_fragment HEIGHT_FOG_DEPTH_SOFTEN; shader_feature_local_fragment INVERT_PULSE; shader_feature_local_fragment LIGHT_FALLOFF; shader_feature_local_fragment METAL_SMOOTHNESS_TEXTURE; shader_feature_local_fragment MULTIPLY_REFLECTIONS; shader_feature_local_fragment OCCLUSION; shader_feature_local_fragment POINT_LIGHT_IS_LOCAL; shader_feature_local_fragment PULSE_MULTIPLY_TEXTURE; shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION; shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET; shader_feature_local_fragment SECONDARY_EMISSION_MASK; shader_feature_local_fragment _ _ACES_APPROACH_BEFORE_EMISSIVE; shader_feature_local_fragment _ _DIFFUSE_TEXTURE_SOURCE_TEXTURE _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS; shader_feature_local_fragment _ _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R; shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_MPM_G; shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_SDF; shader_feature_local_fragment _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD; shader_feature_local_fragment _ _METALLIC_TEXTURE_SOURCE_MPM_R _METALLIC_TEXTURE_SOURCE_MPM_A; shader_feature_local_fragment _ _PARALLAX_FLEXIBLE _PARALLAX_RGB; shader_feature_local_fragment _ _PARALLAX_MASKING_TEXTURE _PARALLAX_MASKING_VERTEX_COLOR; shader_feature_local_fragment _ _PARALLAX_PROJECTION_WARPED; shader_feature_local_fragment _ _SECONDARY_MASK_BLEND_ADD _SECONDARY_MASK_BLEND_MASKED_ADD; shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_SOURCE_MPM_A _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS; shader_feature_local_vertex DISPLACEMENT_BIDIRECTIONAL; shader_feature_local_vertex DISPLACEMENT_SPATIAL; shader_feature_local_vertex VERTEXDISPLACEMENT_MASK; shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL; shader_feature_local_vertex _ _VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE \; vertex vert
- Guards: !USE_UNIFORM_PRIVATE_POINT_COLOR; !defined(REFLECTION_PROBE); !defined(_VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE); USE_ANTIFLICKER_NORMAL_PAYLOAD; USE_ANTIFLICKER_NORMAL_PAYLOAD && \; USE_EMISSION_TEXTURE_COLOR; USE_MESH_PACKING_UV1; USE_NOISE_SCREEN_POSITION; USE_NORMAL_MAP_PAYLOAD; USE_RIBBON_SPATIAL_MASK_VERTEX; USE_SECONDARY_UV; USE_SECONDARY_UV || defined(COLOR_ARRAY) || defined(LIGHTMAP); USE_SPHERE_SDF_3D_VERTEX; USE_UNIFORM_PRIVATE_POINT_COLOR; USE_UV_SCALE; USE_VERTEX_COLOR; USE_VERTEX_EMISSION; USE_VERTEX_EMISSION && !defined(_EMISSIONTEXTURE_FLIPBOOK); USE_WORLD_NORMAL; defined(ACES_TONE_MAPPING) && !defined(_ACES_APPROACH_BEFORE_EMISSIVE); defined(ACES_TONE_MAPPING) && defined(_ACES_APPROACH_BEFORE_EMISSIVE); defined(BLOOM_FOG) && defined(FOG); defined(BOTH_SIDES_DIFFUSE); defined(COLOR_ARRAY); defined(COLOR_ARRAY) && !defined(_EMISSIONCOLORTYPE_MAINEFFECT); defined(COLOR_BY_FOG); defined(COLOR_BY_FOG) && !(defined(BLOOM_FOG) && defined(FOG)) && \; defined(DIFFUSE); defined(DIFFUSE) && defined(SPECULAR); defined(DIFFUSE_TEXTURE); defined(DIRECTIONAL_RIM); defined(DISPLACEMENT_BIDIRECTIONAL); defined(DISPLACEMENT_SPATIAL); defined(DISSOLVE); defined(DISSOLVE) && defined(DISSOLVE_COLOR); defined(DISSOLVE) || defined(DISSOLVE_TEXTURE); defined(DISSOLVE_COLOR); defined(DISSOLVE_PROGRESS); defined(DISSOLVE_TEXTURE); defined(DISTANCE_DARKENING); defined(DISTORTION_SIMPLE) && \; defined(EMISSION_ANGLE_DISAPPEAR); defined(EMISSION_ANGLE_DISAPPEAR) || defined(REFLECTION_TEXTURE) || defined(RIM_DIM); defined(EMISSION_MASK); defined(FOG) && defined(HEIGHT_FOG) && \; defined(FOG_COLOR_HIGHLIGHT); defined(GROUND_FADE); defined(HEIGHT_FOG); defined(HEIGHT_FOG_DEPTH_SOFTEN); defined(HIGHLIGHT_SELECTION); defined(INVERT_PULSE); defined(INVERT_RIM_DIM); defined(LIGHTMAP); defined(LIGHT_FALLOFF); defined(MESH_PACKING); defined(METAL_SMOOTHNESS_TEXTURE); defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS); defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_R); defined(MULTIPLY_REFLECTIONS); defined(NOISE_DITHERING); defined(OCCLUSION); defined(OCCLUSION) && !defined(OCCLUSION_BEFORE_EMISSION); defined(OCCLUSION) && defined(OCCLUSION_BEFORE_EMISSION); defined(OCCLUSION_DETAIL); defined(PARALLAX_IRIDESCENCE); defined(POINT_LIGHT_IS_LOCAL); defined(PRECISE_NORMAL); defined(PRIVATE_POINT_LIGHT); defined(PRIVATE_POINT_LIGHT) && !defined(INSTANCED_PRIVATE_POINT_LIGHT); defined(PULSE_MULTIPLY_TEXTURE); defined(REFLECTION_PROBE_BOX_PROJECTION); defined(REFLECTION_PROBE_BOX_PROJECTION_OFFSET); defined(REFLECTION_STATIC); defined(REFLECTION_TEXTURE); defined(RIM_DIM); defined(SECONDARY_EMISSION_MASK); defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV; defined(SECONDARY_UVS_EMISSION_MASK) && USE_SECONDARY_UV; defined(SECONDARY_UVS_EMISSION_MASK2) && USE_SECONDARY_UV; defined(SECONDARY_UVS_MPM) && USE_SECONDARY_UV; defined(SECONDARY_UVS_OCCLUSION) && USE_SECONDARY_UV; defined(SECONDARY_UVS_OCCLUSION_DETAIL) && USE_SECONDARY_UV; defined(SECONDARY_UVS_PARALLAX) && USE_SECONDARY_UV; defined(SECONDARY_UVS_PULSE) && USE_SECONDARY_UV; defined(SPECULAR); defined(SPECULAR_ANTIFLICKER); defined(TEXTURE3D_EMISSION); defined(TEXTURE3D_LOOKUP); defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION); defined(USE_SPHERICAL_NORMAL_OFFSET); defined(UV_COLOR_SEGMENTS); defined(VERTEXDISPLACEMENT_MASK); defined(_CUSTOM_TIME_FREEZE); defined(_DISSOLVE_SPACE_WORLD_CENTERED); defined(_EMISSIONCOLORTYPE_GRADIENT); defined(_EMISSIONCOLORTYPE_WHITEBOOST) || \; defined(_EMISSIONTEXTURE_FLIPBOOK); defined(_EMISSIONTEXTURE_PULSE); defined(_EMISSIONTEXTURE_SIMPLE); defined(_EMISSION_ALPHA_SOURCE_COPY_EMISSION); defined(_EMISSION_ALPHA_SOURCE_MPM_R) && defined(METAL_SMOOTHNESS_TEXTURE); defined(_EMISSION_TEXTURE_SOURCE_MPM_G) && defined(METAL_SMOOTHNESS_TEXTURE); defined(_EMISSION_TEXTURE_SOURCE_SDF); defined(_HOLOGRAM_GRID); defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY); defined(_HOLOGRAM_LEGACY); defined(_HOLOGRAM_SCANLINE); defined(_MASKBLEND_ADD); defined(_MASKBLEND_MASKED_ADD); defined(_METALLIC_TEXTURE_MPM_R); defined(_METALLIC_TEXTURE_SOURCE_MPM_A); defined(_METALLIC_TEXTURE_SOURCE_MPM_R); defined(_OCCLUSION_SOURCE_MPM_B) && defined(METAL_SMOOTHNESS_TEXTURE); defined(_PARALLAX_FLEXIBLE) || defined(_PARALLAX_RGB); defined(_PARALLAX_FLEXIBLE_REFLECTED); defined(_PARALLAX_MASKING_TEXTURE); defined(_PARALLAX_MASKING_VERTEX_COLOR); defined(_PROBE_CALCULATION_PRECISE); defined(_RIMLIGHT_ADDITIVE); defined(_RIMLIGHT_LERP); defined(_RIMLIGHT_LERP) || defined(_RIMLIGHT_ADDITIVE); defined(_RIM_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM); defined(_SECONDARY_MASK_BLEND_ADD); defined(_SECONDARY_MASK_BLEND_MASKED_ADD); defined(_SECONDARY_UVS_EXTERNAL_SCALE); defined(_SECONDARY_UVS_IMPORT); defined(_SMOOTHNESS_TEXTURE_MPM_A); defined(_SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS); defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_A); defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS); defined(_SPECTROGRAM_FLAT); defined(_SPECTROGRAM_FULL); defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE); defined(_VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE); defined(_VERTEXMODE_COLOR); defined(_VERTEXMODE_DISPLACEMENT); defined(_VERTEXMODE_METALSMOOTHNESS); defined(_VERTEXMODE_SPECIAL); defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) || \
- Evidence Markers: 42d6f6a3521f71bc; 71020a0dc3f18c79
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 10. Record 10

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Mirror
- Path: Mirror.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ BLOOM_FOG; shader_feature_local_fragment DETAIL_NORMAL_MAP; shader_feature_local_fragment DIFFUSE; shader_feature_local_fragment DIRT; shader_feature_local_fragment LIGHTMAP; shader_feature_local_fragment LIGHT_FALLOFF; vertex vert
- Guards: defined(ACES_TONE_MAPPING) && (defined(DIFFUSE) || defined(LIGHTMAP)); defined(BLOOM_FOG); defined(DETAIL_NORMAL_MAP); defined(DIFFUSE); defined(DIFFUSE) || defined(LIGHTMAP); defined(DIRT); defined(LIGHTMAP); defined(LIGHT_FALLOFF); defined(USING_STEREO_MATRICES)
- Evidence Markers: 705d3d71f8b20274; 8d8f776efaca7ef8; M1; M2; M3; M4; M5; M6; M7; M8; M9; UV0; UV1; a2cdd3ecb791db4e; c8c83176642bd0a5; f6c1f193c04091cf; vertex-c51879856e2e11c7
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 11. Record 11

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Object/Arc
- Path: Object/Arc.shader
- Pragmas: fragment frag; multi_compile_fragment _ BLOOM_FOG; multi_compile_fragment _ CM_PREVIEW_MODE; multi_compile_instancing; shader_feature_local_fragment FOG; shader_feature_local_fragment HEIGHT_FOG; vertex vert
- Guards: defined(CM_PREVIEW_MODE); defined(FOG) && defined(BLOOM_FOG)
- Evidence Markers: fragment-5500cb795b66e75f
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 12. Record 12

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Object/Note
- Path: Object/Note.shader
- Pragmas: fragment frag; multi_compile _ CUTOUT; multi_compile _ POST_BLOOM; multi_compile _ REFLECTION_MAP; multi_compile _ RIM_DIM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ BLOOM_FOG; multi_compile_fragment _ CM_PREVIEW_MODE; multi_compile_fragment _ HEIGHT_FOG; multi_compile_fragment _ _FOGTYPE_LERP; multi_compile_fragment _ _WHITEBOOSTTYPE_MAINEFFECT; multi_compile_instancing; shader_feature_local FAKE_MIRROR_TRANSPARENCY; shader_feature_local PLANE_CUT; shader_feature_local ZWRITE; vertex vert
- Guards: !defined(CM_PREVIEW_MODE); !defined(_FOGTYPE_ALPHA); defined(ACES_TONE_MAPPING); defined(BLOOM_FOG) && (defined(_FOGTYPE_LERP) || defined(_FOGTYPE_COLOR) || defined(_FOGTYPE_ALPHA) || defined(HEIGHT_FOG)); defined(CUTOUT); defined(FAKE_MIRROR_TRANSPARENCY); defined(HEIGHT_FOG); defined(PLANE_CUT); defined(REFLECTION_MAP); defined(RIM_DIM); defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)
- Evidence Markers: 17ebe636fa11ba31; 7bb6ae1661a9efa2; F0; f30427033415f21d
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 13. Record 13

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Object/Obstacle Distortion
- Path: Object/ObstacleDistortion.shader
- Pragmas: fragment frag; multi_compile_fragment _ BLOOM_FOG BLOOM_FOG; multi_compile_fragment _ CM_PREVIEW_MODE; multi_compile_fragment _ DEPTH_TEXTURE DEPTH_TEXTURE_ENABLED; multi_compile_instancing; shader_feature_local_fragment CLIPPING; shader_feature_local_fragment CLIP_LOW_ALPHA; shader_feature_local_fragment CUTOUT; shader_feature_local_fragment DEPTH_AWARE_DISTORTION; shader_feature_local_fragment FOG; shader_feature_local_fragment HEIGHT_FOG; shader_feature_local_fragment RIM_DIM; shader_feature_local_fragment SCROLL_UV; shader_feature_local_fragment USE_DISTORTED_TEXTURE_ONLY; shader_feature_local_fragment VIEW_ANGLE_AFFECTS_DISTORTION; shader_feature_local_fragment ZWRITE; shader_feature_local_vertex SCALE_UV; target 3.5; vertex vert
- Guards: defined(BLOOM_FOG); defined(CLIPPING); defined(CLIP_LOW_ALPHA); defined(CUTOUT); defined(DEPTH_AWARE_DISTORTION) && (defined(DEPTH_TEXTURE) || defined(DEPTH_TEXTURE_ENABLED)); defined(FOG); defined(RIM_DIM); defined(SCALE_UV); defined(SCROLL_UV); defined(STEREO_INSTANCING_ON) || defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_MULTIVIEW_ON); defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON); defined(USE_DISTORTED_TEXTURE_ONLY); defined(VIEW_ANGLE_AFFECTS_DISTORTION)
- Evidence Markers: 2626bb764be28656
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 14. Record 14

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Object/Obstacle Outline
- Path: Object/ObstacleOutline.shader
- Pragmas: fragment frag; multi_compile _ POST_BLOOM; multi_compile_fragment _ BLOOM_FOG; multi_compile_fragment _ CM_PREVIEW_MODE; multi_compile_instancing; shader_feature_local_fragment CUTOUT; shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS; target 3.5; vertex vert
- Guards: !defined(CM_PREVIEW_MODE); defined(CM_PREVIEW_MODE) && defined(BLOOM_FOG); defined(CUTOUT); defined(_WHITEBOOSTTYPE_ALWAYS) || (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)); defined(_WHITEBOOSTTYPE_MAINEFFECT)
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 15. Record 15

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Parametric Box Fake Glow
- Path: ParametricBoxFakeGlow.shader
- Pragmas: fragment frag; multi_compile _ BLOOM_FOG; multi_compile _ POST_BLOOM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_instancing; shader_feature_local CUTOUT; shader_feature_local HEIGHT_FOG; shader_feature_local MAIN_EFFECT_WHITE_BOOST; shader_feature_local _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS; shader_feature_local_fragment CLIPPING; shader_feature_local_fragment WORLDSPACE_NOISE_CUTOUT; vertex vert
- Guards: defined(BLOOM_FOG); defined(CLIPPING); defined(CUTOUT); defined(HEIGHT_FOG); defined(MAIN_EFFECT_WHITE_BOOST) && \; defined(WORLDSPACE_NOISE_CUTOUT)
- Evidence Markers: 0020a90d28235a82; 01fa7ac4ac5f6546; 139018e71a01a3e2; 1afc20561ed2144b; 2dffd03d72718568; PFG1; PFG10; PFG2; PFG3; PFG4; PFG5; PFG6; PFG7; PFG8; PFG9; UV0; acbb090e6240a31d; d2a5af8334ac0b36; e329b30d3474ad13
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 16. Record 16

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Parametric Box Opaque
- Path: ParametricBoxOpaque.shader
- Pragmas: fragment frag; multi_compile _ POST_BLOOM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local_fragment HEIGHT_FOG; vertex vert
- Guards: !defined(POST_BLOOM); defined(BLOOM_FOG); defined(HEIGHT_FOG)
- Evidence Markers: 2c0168399e26d562; 3472619767b58962; 3e0bbd17e77c38d1; 6bce22b0123d38cd; 814cc1498a56699a; PBO1; PBO2; PBO3; PBO4; PBO5; PBO6; PBO7; PBO8; PBO9; c109313148f71b48; d8ea76eac9bb6f18; e98a1337526a77a9
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 17. Record 17

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Parametric Box Transparent
- Path: ParametricBoxTransparent.shader
- Pragmas: fragment frag; multi_compile _ POST_BLOOM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local REFLECTION_PROBE; shader_feature_local_fragment HEIGHT_FOG; shader_feature_local_fragment WORLD_NOISE; shader_feature_local_fragment WORLD_NOISE_WARP; shader_feature_local_fragment WORLD_SPACE_FADE; vertex vert
- Guards: !defined(POST_BLOOM); defined(BLOOM_FOG); defined(HEIGHT_FOG); defined(REFLECTION_PROBE); defined(WORLD_NOISE)
- Evidence Markers: 032dfb9c99253066; 10c6243ca42145bf; 866b14486d5d6356; PBT1; PBT2; PBT3; PBT4; PBT5; PBT6; PBT7; PBT8; PBT9; fb0a3537aa32426e
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 18. Record 18

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Parametric Slice Billboard
- Path: ParametricSliceBillboard.shader
- Pragmas: fragment frag; multi_compile _ POST_BLOOM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local_fragment ANGLE_DISAPPEAR; shader_feature_local_fragment FOG; shader_feature_local_fragment HEIGHT_FOG; shader_feature_local_fragment NOISE_DITHERING; shader_feature_local_fragment SQUARE_ALPHA; shader_feature_local_fragment USE_FOG_FOR_LIGHTS; shader_feature_local_fragment WORLD_NOISE; shader_feature_local_fragment WORLD_NOISE_WARP; shader_feature_local_fragment WORLD_SPACE_FADE; shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS; shader_feature_local_vertex ALPHA_WIDTH_SCALE; shader_feature_local_vertex Y_AXIS_BILLBOARD; vertex vert
- Guards: !defined(USE_FOG_FOR_LIGHTS) && defined(FOG); (defined(_WHITEBOOSTTYPE_ALWAYS) || \; defined(ALPHA_WIDTH_SCALE); defined(ANGLE_DISAPPEAR); defined(BLOOM_FOG); defined(HEIGHT_FOG); defined(NOISE_DITHERING); defined(SQUARE_ALPHA); defined(USE_FOG_FOR_LIGHTS) && defined(FOG); defined(WORLD_NOISE); defined(Y_AXIS_BILLBOARD)
- Evidence Markers: PSB1; PSB2; PSB3; PSB4; PSB5; PSB6; PSB7
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 19. Record 19

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Particles
- Path: Particles.shader
- Pragmas: fragment frag; multi_compile _ DEPTH_TEXTURE DEPTH_TEXTURE_ENABLED; multi_compile _ POST_BLOOM; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local COLOR_ARRAY; shader_feature_local COLOR_GRADIENT; shader_feature_local DISSOLVE; shader_feature_local DISSOLVE_PROGRESS_FROM_VERTEX_ALPHA; shader_feature_local FLIPBOOK_BLENDING_OFF; shader_feature_local MAIN_PER_PARTICLE_RANDOM; shader_feature_local MAIN_TEXTURE; shader_feature_local MASK; shader_feature_local MASK2; shader_feature_local MASK2_RED_IS_ALPHA; shader_feature_local MASK_RED_IS_ALPHA; shader_feature_local MIPMAP_BIAS; shader_feature_local NOISE_DITHERING; shader_feature_local SECONDARY_COLOR; shader_feature_local SECONDARY_UVS_DISTORTION; shader_feature_local SECONDARY_UVS_MAIN; shader_feature_local SECONDARY_UVS_MASK; shader_feature_local SECONDARY_UVS_MASK2; shader_feature_local SOFT_PARTICLES; shader_feature_local TEXTURE_FLIPBOOK; shader_feature_local VERTEX_COLOR; shader_feature_local VERTEX_FLIPBOOK; shader_feature_local VERTEX_FLIPBOOK_FADE; shader_feature_local VERTEX_SQUARE_ALPHA; shader_feature_local VIEW_ALIGN_DISAPPEAR; shader_feature_local WORLDSPACE_PANNING; shader_feature_local WORLDSPACE_PANNING_DISTORTION; shader_feature_local _ DISTORTION_SIMPLE; shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE; shader_feature_local _ _DISSOLVE_SPACE_WORLD _DISSOLVE_SPACE_WORLD_CENTERED; shader_feature_local _ _MASK2BLEND_ADD _MASK2BLEND_MASKED_ADD; shader_feature_local _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD; shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE; shader_feature_local_fragment CLOSE_TO_CAMERA_DISAPPEAR; shader_feature_local_fragment COLOR_BY_FOG; shader_feature_local_fragment CUSTOM_WRAPPING; shader_feature_local_fragment DISTORTION_TARGET_MASK; shader_feature_local_fragment FAKE_MIRROR_TRANSPARENCY; shader_feature_local_fragment FILL_ALPHA; shader_feature_local_fragment FOG_COLOR_HIGHLIGHT; shader_feature_local_fragment HEIGHT_FOG; shader_feature_local_fragment HOLOGRAM; shader_feature_local_fragment LIFETIME; shader_feature_local_fragment PIXELATE; shader_feature_local_fragment PLANE_CLIPPING; shader_feature_local_fragment PRECISE_FOG; shader_feature_local_fragment REMAP_WHITEBOOST_START; shader_feature_local_fragment SPECTROGRAM_COLOR; shader_feature_local_fragment SQUARE_ALPHA; shader_feature_local_fragment TEXTURE_COLOR; shader_feature_local_fragment _ _ALPHACHANNEL_RED; shader_feature_local_fragment _ _CUTOUTTYPE_ALPHA_CLIP _CUTOUTTYPE_WORLDSPACE_NOISE; shader_feature_local_fragment _ _FOGTYPE_LERP _FOGTYPE_COLOR _FOGTYPE_ALPHA; shader_feature_local_fragment _ _FOG_MASK_SOURCE_PRIMARY_MASK; shader_feature_local_fragment _ _OVERRIDE_FINAL_ALPHA_COLOR_BASED; shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS; shader_feature_local_vertex LIFETIME; shader_feature_local_vertex MESH_PACKING; shader_feature_local_vertex SPATIAL_DISPLACEMENT; shader_feature_local_vertex SPECTROGRAM_COLOR; shader_feature_local_vertex VERTEX_RED_IS_ALPHA; shader_feature_local_vertex WORLDSPACE_PANNING_MAIN; shader_feature_local_vertex _ _BILLBOARD_FULL _BILLBOARD_Y_AXIS _BILLBOARD_CAMERA_FACING; shader_feature_local_vertex _ _CURVE_VERTICES_AROUND_Z; shader_feature_local_vertex _ _SPECTROGRAM_FULL; shader_feature_local_vertex _ _VERTEXCHANNELS_A; vertex vert
- Guards: !defined(SPATIAL_DISPLACEMENT); !defined(_CUTOUTTYPE_ALPHA_CLIP); FOG && !defined(BLOOM_FOG) && defined(HEIGHT_FOG) && !defined(COLOR_BY_FOG); FOG && !defined(BLOOM_FOG) && defined(HEIGHT_FOG) && \; USE_BILLBOARD; defined(BLOOM_FOG); defined(CLOSE_TO_CAMERA_DISAPPEAR); defined(COLOR_ARRAY); defined(COLOR_BY_FOG); defined(COLOR_GRADIENT); defined(CUSTOM_WRAPPING); defined(DISSOLVE); defined(DISTORTION_SIMPLE); defined(DISTORTION_SIMPLE) && !(defined(MASK) && defined(MASK2)); defined(FAKE_MIRROR_TRANSPARENCY); defined(FILL_ALPHA); defined(FLIPBOOK_BLENDING_OFF); defined(HEIGHT_FOG); defined(HOLOGRAM); defined(HOLOGRAM) && defined(NOISE_DITHERING) \; defined(LIFETIME); defined(MAIN_TEXTURE); defined(MAIN_TEXTURE) && !defined(SECONDARY_UVS_MAIN); defined(MAIN_TEXTURE) && defined(SECONDARY_UVS_MAIN) && \; defined(MASK); defined(MASK) && defined(MASK2); defined(MASK2); defined(MASK2_RED_IS_ALPHA); defined(MASK_RED_IS_ALPHA); defined(MESH_PACKING); defined(MESH_PACKING) || defined(_SECONDARY_UVS_IMPORT) || defined(COLOR_ARRAY); defined(MIPMAP_BIAS); defined(NOISE_DITHERING); defined(NOISE_DITHERING) && !defined(_CUTOUTTYPE_ALPHA_CLIP); defined(NOISE_DITHERING) && !defined(_CUTOUTTYPE_ALPHA_CLIP) \; defined(NOISE_DITHERING) && defined(_FOGTYPE_COLOR) && defined(HEIGHT_FOG) \; defined(PARTICLES_DITHER_AFTER_COLOR_FOG); defined(PIXELATE); defined(PLANE_CLIPPING); defined(PRECISE_FOG); defined(REMAP_WHITEBOOST_START); defined(SECONDARY_COLOR); defined(SECONDARY_COLOR) && !defined(COLOR_ARRAY); defined(SECONDARY_UVS_DISTORTION) && \; defined(SECONDARY_UVS_MASK) && \; defined(SECONDARY_UVS_MASK2) && \; defined(SOFT_PARTICLES) && (defined(DEPTH_TEXTURE) || defined(DEPTH_TEXTURE_ENABLED)); defined(SPATIAL_DISPLACEMENT); defined(SPECTROGRAM_COLOR); defined(SQUARE_ALPHA); defined(TEXTURE_COLOR); defined(TEXTURE_FLIPBOOK); defined(UNITY_INSTANCING_ENABLED); defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON); defined(VERTEX_COLOR); defined(VERTEX_FLIPBOOK); defined(VERTEX_FLIPBOOK) && defined(VERTEX_FLIPBOOK_FADE); defined(VERTEX_FLIPBOOK_FADE); defined(VERTEX_RED_IS_ALPHA); defined(VERTEX_SQUARE_ALPHA); defined(VIEW_ALIGN_DISAPPEAR); defined(WORLDSPACE_PANNING_DISTORTION); defined(WORLDSPACE_PANNING_MAIN); defined(_ALPHACHANNEL_RED); defined(_BILLBOARD_CAMERA_FACING); defined(_BILLBOARD_FULL); defined(_BILLBOARD_Y_AXIS); defined(_CURVE_VERTICES_AROUND_Z); defined(_CUTOUTTYPE_ALPHA_CLIP); defined(_CUTOUTTYPE_WORLDSPACE_NOISE); defined(_DISSOLVE_SPACE_WORLD) || defined(_DISSOLVE_SPACE_WORLD_CENTERED); defined(_DISSOLVE_SPACE_WORLD_CENTERED); defined(_FOGTYPE_ALPHA); defined(_FOGTYPE_COLOR); defined(_FOGTYPE_LERP); defined(_FOG_MASK_SOURCE_PRIMARY_MASK) && defined(MASK); defined(_FOG_MASK_SOURCE_PRIMARY_MASK) && defined(MASK) && \; defined(_MASK2BLEND_ADD); defined(_MASK2BLEND_MASKED_ADD); defined(_MASKBLEND_ADD); defined(_MASKBLEND_MASKED_ADD); defined(_OVERRIDE_FINAL_ALPHA_COLOR_BASED); defined(_SECONDARY_UVS_EXTERNAL_SCALE); defined(_SECONDARY_UVS_IMPORT); defined(_SECONDARY_UVS_IMPORT) || defined(_SECONDARY_UVS_EXTERNAL_SCALE) || \; defined(_SPECTROGRAM_FULL); defined(_SPECTROGRAM_FULL) || defined(SPECTROGRAM_COLOR); defined(_VERTEXCHANNELS_A); defined(_WHITEBOOSTTYPE_ALWAYS); defined(_WHITEBOOSTTYPE_ALWAYS) || (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)); defined(_WHITEBOOSTTYPE_MAINEFFECT)
- Evidence Markers: 28ca48d558555d4a32f7f720ed95a43535436a9ee91ac9adeb6a094d4bd65cab; 2c574604de1e85ca; 40d13975545fd678; 4eed5d58254824a2; 5431f008df503651; 5ef6afe6d212829d; 6017450af174125e; 953990a3053f67d9; MASK2; d2178af6c7f7f78f; db0bff392a1dacb8; db5d6342028cbf16; e7fc61bdf833e455; ebdcf1970fae8aeb; ebdcf1970fae8aebda52d280eda3714430d252c10ecdd4fadb74b064ec9666f7; f316ae8ed7c1d00d20b76511973fd04e3dc11c374e9ab3f4728b9e6db89901d0; fff04977a2ce473345eff9e1bc27c9b117137b252b2c3b227e87c3bbc6aff6ec
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 20. Record 20

- Classification: `FAMILY_SHARED`
- Shader: Hidden/BlitBlendColor
- Catalog Scope Status: `adjacent_unverified`
- Path: Post Process/BlitBlendColor.shader
- Pragmas: fragment FragBlendColor; target 3.0; vertex VertDefault
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 21. Record 21

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Post Process/Bloom
- Path: Post Process/Bloom.shader
- Pragmas: fragment FragDirectCombine; fragment FragDirectCombineGamma; fragment FragDownsample13; fragment FragDownsample13Alpha; fragment FragDownsample4; fragment FragDownsample4Alpha; fragment FragDownsample4Gamma; fragment FragUpsampleAces; fragment FragUpsampleAutoExposureAces; fragment FragUpsampleBox; fragment FragUpsampleBoxGamma; fragment FragUpsampleReinhard; fragment FragUpsampleTent; fragment FragUpsampleTentGamma; vertex VertDefault
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 22. Record 22

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Post Process/Chromatic Aberration
- Catalog Scope Status: `adjacent_unverified`
- Path: Post Process/ChromaticAberration.shader
- Pragmas: fragment FragChromaticAberration; target 3.0; vertex VertDefault
- Guards: TEXTURE2D_ARGS
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 23. Record 23

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Post Process/Post Bloom
- Path: Post Process/PostBloom.shader
- Pragmas: fragment FragMainEffect; multi_compile _ STEREO_INSTANCING_ON; multi_compile_instancing; multi_compile_local _ CLEAR_SCREEN_ALPHA; multi_compile_local _ LIV_MR; target 3.5; vertex VertDefault
- Guards: defined(CLEAR_SCREEN_ALPHA) && !defined(LIV_MR); defined(STEREO_INSTANCING_ON)
- Evidence Markers: 2a39b58b76b55f8d; 2d8150c9bcaac458; 459f777b756340f9; 750826b6491d4a9d; 795279359b297b74; 96bbc8d772fc57ed; ME1; ME2; ME3; ME4; ME5; ME6; ME7; ME8; ME9
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 24. Record 24

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Rain
- Path: Rain.shader
- Pragmas: fragment frag; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local MASK_RED_IS_ALPHA; shader_feature_local TEXTURE_COLOR; shader_feature_local VERTEX_COLOR; shader_feature_local VERTEX_SQUARE_ALPHA; shader_feature_local _ALPHACHANNEL_RED; shader_feature_local _FOGTYPE_COLOR; vertex vert
- Guards: defined(BLOOM_FOG); defined(TEXTURE_COLOR); defined(VERTEX_COLOR); defined(VERTEX_SQUARE_ALPHA); defined(_ALPHACHANNEL_RED) || defined(MASK_RED_IS_ALPHA); defined(_FOGTYPE_COLOR)
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 25. Record 25

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Set Depth Only
- Path: SetDepthOnly.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; vertex vert
- Evidence Markers: 6c57a8d760f7e76d; D1; D2; D3; D4; D5; D6; fragment-058977666c847ef9; vertex-66aea619521c0b3f; vertex-d5d8e882c98a2cc8
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 26. Record 26

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Spectrogram
- Path: Spectrogram.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local_fragment DIFFUSE; shader_feature_local_fragment LIGHT_FALLOFF; shader_feature_local_fragment SPECULAR; vertex vert
- Guards: defined(ACES_TONE_MAPPING); defined(BLOOM_FOG); defined(DIFFUSE); defined(LIGHT_FALLOFF); defined(SPECULAR)
- Evidence Markers: 477dc738c669dabd; 89358b18acd1d3a4; S1; S2; S3; S4; S5; S6; UV0; a629dbe44112ae87; bdc55897e4e396fb; vertex-bead5cceaf6dbed1
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 27. Record 27

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Spectrogram Unlit
- Path: SpectrogramUnlit.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; vertex vert
- Guards: defined(BLOOM_FOG)
- Evidence Markers: 131d5989fe263d58; 25a7770007c1a811; 65e0d97fd4c2560c; 80969e1d39b26e44; U1; U2; U3; U4; U5; UV0
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 28. Record 28

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Stencil
- Path: Stencil.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; vertex vert
- Evidence Markers: 4a7230123d73103c; S1; S2; S3; S4; S5; S6; fragment-8dc2c81abf29c14b; vertex-e1db43b18c53b97e; vertex-ee83a5e328c2677c
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.

## 29. Record 29

- Classification: `FAMILY_SHARED`
- Shader: ChroMapper/Water Lit
- Path: WaterLit.shader
- Pragmas: fragment frag; multi_compile _ STEREO_INSTANCING_ON; multi_compile_fragment _ ACES_TONE_MAPPING; multi_compile_fragment _ BLOOM_FOG; multi_compile_instancing; shader_feature_local_fragment DETAIL_NORMAL_MAP; shader_feature_local_fragment FOG; shader_feature_local_fragment HEIGHT_FOG; shader_feature_local_fragment LIGHTMAP; shader_feature_local_fragment NOISE_DITHERING; shader_feature_local_fragment NORMAL_MAP; shader_feature_local_fragment REFLECTION_PROBE; shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION; shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET; shader_feature_local_fragment Z_FADE; shader_feature_local_fragment _ _DECALBLEND_ALPHABLEND; shader_feature_local_fragment _ _EMISSIONCOLORTYPE_FLAT; vertex vert
- Guards: defined(ACES_TONE_MAPPING); defined(BLOOM_FOG) && defined(FOG); defined(DETAIL_NORMAL_MAP); defined(HEIGHT_FOG); defined(LIGHTMAP); defined(NOISE_DITHERING); defined(NORMAL_MAP); defined(REFLECTION_PROBE); defined(REFLECTION_PROBE_BOX_PROJECTION); defined(REFLECTION_PROBE_BOX_PROJECTION_OFFSET); defined(Z_FADE)
- Evidence Markers: 3e6ecc087bb208b2; 8c86fd4f73cdeee5; 9699ed0481a680f5; UV0; UV1; W1; W2; W3; W4; W5; W6; W7; a8f2be99be4aa923; d1b639b649b39f9e; d6f92261fee467f2
- Note: Candidate compile-time matrix only; material/runtime reachability is not implied.
