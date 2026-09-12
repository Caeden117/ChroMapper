// Replacement for the Beat Saber game shader Custom/SimpleLit.
Shader "ChroMapper/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [Toggle(AVATAR_COMPUTE_SKINNING)] _AvatarComputeSkinning ("Avatar Compute Skinning", Float) = 0
        [KeywordEnum(None, Import, External Scale, Object Space, Additive Offset)] _Secondary_UVs ("Secondary UVs", Float) = 0
        [ShowIfAny(_SECONDARY_UVS_IMPORT)] _InstancedSecondaryTiling ("Secondary UV Tiling", Vector) = (1,1,0,0)
        [ShowIfAny(_SECONDARY_UVS_EXTERNAL_SCALE, _SECONDARY_UVS_IMPORT)] _InstancedSecondaryOffset ("Secondary UV Offset", Vector) = (0,0,0,0)
        [ShowIfAny(_SECONDARY_UVS_EXTERNAL_SCALE, _SECONDARY_UVS_OBJECT_SPACE)] _UVScale ("UV Scale", Vector) = (1,1,1,1)
        [ShowIfAny(_SECONDARY_UVS_ADDITIVE_OFFSET)] _AdditiveUVOffset ("UV Offset", Vector) = (0,0,0,0)
        [VectorShowIfAny(2)] _InputUvMultiplier ("UV Multiplier", Vector) = (1,1,0,0)
        [Header(BASE PROPERTIES)] [Space(12)] [Toggle(METAL_SMOOTHNESS_TEXTURE)] _EnableMetalSmoothnessTex ("Multi Purpose Map", Float) = 0
        [ShowIfAny(METAL_SMOOTHNESS_TEXTURE)] _MetalSmoothnessTex ("MPM Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_MPM, 2, 0_SECONDARY_UVS_NONE, METAL_SMOOTHNESS_TEXTURE)] _SecondaryUVsMPM ("MPM Secondary UVs", Float) = 0
        [ToggleShowIfAny(MPM_CUSTOM_MIP, METAL_SMOOTHNESS_TEXTURE)] _EnableCustomMPMMip ("Mip map bias", Float) = 0
        [ShowIfAny(2, METAL_SMOOTHNESS_TEXTURE, MPM_CUSTOM_MIP)] _MpmMipBias ("MPM mipmap bias", Range(-3, 0)) = 0
        [Space(12)] [KeywordEnum(None, MPM R, MPM_A, MPM Avatar B)] _Metallic_Texture ("Metallic Source", Float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 1
        [Space(12)] [KeywordEnum(None, MPM A, MPM G Roughness)] _Smoothness_Texture ("Smoothness Source", Float) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Space(12)] [Toggle(SPECULAR_ANTIFLICKER)] _SpecularAntiflicker ("Smoothness Anti-Flicker", Float) = 0
        [ShowIfAny(SPECULAR_ANTIFLICKER)] _AntiflickerStrength ("Antiflicker Strength", Range(0, 1)) = 0.7
        [ShowIfAny(SPECULAR_ANTIFLICKER)] _AntiflickerDistanceScale ("Antiflicker Distance Scale", Float) = 0.1
        [ShowIfAny(SPECULAR_ANTIFLICKER)] _AntiflickerDistanceOffset ("Antiflicker Distance Offset", Float) = 21
        [Space(12)] [Toggle(PRECISE_NORMAL)] _PreciseNormal ("Precise Normal", Float) = 0
        [Space(18)] [KeywordEnum(None, Color, Emission, MetalSmoothness, Special, Displacement, Emissive Mult Add)] _VertexMode ("Vertex Color Mode", Float) = 0
        [Space(14)] [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionThreshold ("Emission Threshold", Range(0, 1)) = 0
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionColor ("Emission Color", Color) = (1,1,1,0)
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionStrength ("Emission Strength", Float) = 1
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionBloomIntensity ("Bloom Intensity", Float) = 1
        [EnumShowIfAny(3, None, MainEffect, Always, _VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _Vertex_WhiteBoostType ("Vertex Color Treatment", Float) = 0
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _QuestWhiteboostMultiplier ("Whiteboost Multiplier", Float) = 1
        [ToggleShowIfAny(DISPLACEMENT_SPATIAL, _VERTEXMODE_DISPLACEMENT)] _DisplacementSpatial ("RGB Direction", Float) = 0
        [ToggleShowIfAny(DISPLACEMENT_BIDIRECTIONAL, 2, DISPLACEMENT_SPATIAL, _VERTEXMODE_DISPLACEMENT)] _DisplacementBidirectional ("RGB Bidirectional", Float) = 0
        [EnumShowIfAny(3, None, Flat, Full, _VERTEXMODE_DISPLACEMENT)] _Spectrogram ("Spectrogram", Float) = 0
        [ShowIfAny(_VERTEXMODE_DISPLACEMENT)] _DisplacementStrength ("Displacement Strength", Float) = 0.1
        [ShowIfAny(_VERTEXMODE_DISPLACEMENT)] _DisplacementAxisMultiplier ("Axis Multiplier", Vector) = (1,1,1,1)
        _EnableVertexDisplacementMask ("Vertex Displacement Mask", Float) = 0
        _VertexDisplacement_Mask_Source ("Mask Source", Float) = 0
        _VertexDisplacementMask ("Mask Texture", 2D) = "white" {}
        _VertexDisplacementMaskSpeed ("Mask Texture Speed", Vector) = (0,1,0,0)
        _VertexDisplacementMaskMode ("Mask Mode", Float) = 0
        _VertexDisplacementMaskMultiplier ("Mask Multiplier", Float) = 1
        _VertexDisplacementMaskOffset ("Mask Offset", Float) = 0
        _VertexDisplacement3DTexture ("Noise Tex", 3D) = "white" {}
        _VertexDisplacement3DTexOffset ("Texture Offset", Vector) = (0,0,0,0)
        _VertexDisplacement3DTexPanning ("Texture Panning", Vector) = (0,0,0,0)
        _VertexDisplacement3DTexScale ("Texture Scale", Float) = 5
        [Header(EMISSIONS)] [Space(18)] [KeywordEnum(None, Simple, Pulse, Flipbook)] _EmissionTexture ("Texture Emission", Float) = 0
        [EnumShowIfAny(4, Texture, Fill, MPM G, SDF, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_FLIPBOOK, _EMISSION_TEXTURE_SOURCE_SDF)] _Emission_Texture_Source ("Emission Source", Float) = 0
        [ShowIfAny(1, _EMISSION_TEXTURE_SOURCE_TEXTURE, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionTex ("Emission Texture", 2D) = "white" {}
        [VectorShowIfAny(2, 2, _EMISSIONTEXTURE_SIMPLE, _EMISSION_TEXTURE_SOURCE_TEXTURE)] _EmissionTexSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [ToggleShowIfAny(SECONDARY_UVS_EMISSION, 2, 0_SECONDARY_UVS_NONE, _EMISSION_TEXTURE_SOURCE_TEXTURE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryUVsEmissionTex ("Use Secondary UVs", Float) = 0
        [EnumShowIfAny(3, Emission G, Copy Emission, MPM R, _EMISSIONTEXTURE_SIMPLE)] _Emission_Alpha_Source ("Alpha Source", Float) = 0
        _EmissionBrightness ("Brightness", Float) = 1
        [ToggleShowIfAny(TEXTURE3D_EMISSION, 1, TEXTURE3D_LOOKUP)] _LookupTextureEmission ("Use Texture3D Emission", Float) = 0
        [ToggleShowIfAny(EMISSION_ANGLE_DISAPPEAR, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EnableEmissionAngleDisappear ("Angle Disappear", Float) = 0
        [ShowIfAny(1, EMISSION_ANGLE_DISAPPEAR, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionThresholdAngle ("Threshold Angle", Float) = 0
        [Space(6)] [EnumShowIfAny(4, Flat, Whiteboost, Gradient, MainEffect, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionColorType ("Emission Color Treatment", Float) = 0
        [ShowIfAny(1, 0_EMISSIONCOLORTYPE_GRADIENT, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionTexColor ("Emission Color", Color) = (1,1,1,1)
        [ShowIfAny(1, _EMISSIONCOLORTYPE_GRADIENT, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionGradientTex ("Gradient LUT", 2D) = "white" {}
        [ShowIfAny(_EMISSIONCOLORTYPE_GRADIENT)] _EmissionGradientPosition ("LUT Position", Float) = 0.5
        [ShowIfAny(_EMISSIONCOLORTYPE_GRADIENT)] _EmissionGradientPanningSpeed ("LUT Panning", Float) = 0
        [ShowIfAny(_EMISSIONCOLORTYPE_GRADIENT)] _EmissionGradientIntensity ("LUT Intensity", Float) = 1
        _EmissionTexBloomIntensity ("Bloom Intensity", Float) = 1
        _EmissionTexWhiteBoostMultiplier ("Whiteboost multiplier", Float) = 1
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseMask ("Pulse Mask", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_PULSE, 2, 0_SECONDARY_UVS_NONE, _EMISSIONTEXTURE_PULSE)] _SecondaryUVsPulseTex ("Pulse Texture Secondary UVs", Float) = 0
        [ToggleShowIfAny(INVERT_PULSE, _EMISSIONTEXTURE_PULSE)] _InvertPulseTexture ("Invert Texture", Float) = 0
        [ToggleShowIfAny(PULSE_MULTIPLY_TEXTURE, _EMISSIONTEXTURE_PULSE)] _PulseMultiplyByTexture ("Brightness from Texture", Float) = 0
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseWidth ("Pulse Width", Float) = 0.1
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseSpeed ("Pulse Speed", Float) = 0.2
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseSmooth ("Pulse Smooth", Range(0, 0.2)) = 0.02
        [Space(12)] [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookColumns ("Flipbook Columns", Float) = 8
        [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookRows ("Flipbook Rows", Float) = 8
        [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookNonloopableFrames ("Full Non-loopable frames", Float) = 0
        [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookSpeed ("Flipbook Speed", Float) = 1
        [ToggleShowIfAny(FLIPBOOK_BLENDING_OFF, _EMISSIONTEXTURE_FLIPBOOK)] _FlipbookBlendingOff ("No Frame Blending", Float) = 0
        [ToggleShowIfAny(EMISSION_MASK, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE)] _EnableEmissionMask ("Layer 2", Float) = 0
        [EnumShowIfAny(3, Multiply, Add, Masked Add, EMISSION_MASK)] _MaskBlend ("Layer Blend", Float) = 0
        [ShowIfAny(1, EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMask ("Layer Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_EMISSION_MASK, 2, 0_SECONDARY_UVS_NONE, EMISSION_MASK)] _SecondaryUVsMask ("Use Secondary UVs", Float) = 0
        [VectorShowIfAny(2, 1, EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskSpeed ("Layer Texture Speed", Vector) = (0,1,0,0)
        [ShowIfAny(1, EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskIntensity ("Layer Intensity", Float) = 1
        [ToggleShowIfAny(SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE)] _EnableSecondaryEmissionMask ("Layer 3", Float) = 0
        [EnumShowIfAny(3, Multiply, Add, Masked Add, SECONDARY_EMISSION_MASK)] _Secondary_Mask_Blend ("Layer Blend", Float) = 0
        [ShowIfAny(1, SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryEmissionMask ("Layer Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_EMISSION_MASK2, 2, 0_SECONDARY_UVS_NONE, SECONDARY_EMISSION_MASK)] _SecondaryUVsMask2 ("Use Secondary UVs", Float) = 0
        [VectorShowIfAny(2, 1, SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryEmissionMaskSpeed ("Texture Speed", Vector) = (0,1,0,0)
        [ShowIfAny(1, SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryEmissionMaskIntensity ("Layer Intensity", Float) = 1
        [Space(12)] [EnumShowIfAny(4 , None, Mask, Secondary Mask, Emission Texture, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _Emission_Step ("Step Emission", Float) = 0
        [ShowIfAny(_EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskStepValue ("Step Value", Range(0, 1)) = 0.5
        [ShowIfAny(_EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskStepWidth ("Step Width", Range(0, 0.5)) = 0.1
        [Space(12)] [KeywordEnum(None, Flexible, RGB)] _Parallax ("Parallax Emission", Float) = 0
        [ToggleShowIfAny(_PARALLAX_FLEXIBLE_REFLECTED, 0_PARALLAX_NONE)] _EnableReflectedDir ("Reflected Direction", Float) = 0
        [EnumShowIfAny(2, Planar, Warped, 0_PARALLAX_NONE)] _Parallax_Projection ("Parallax Projection", Float) = 0
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxColor ("Parallax Color", Color) = (1,1,1,1)
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxMap ("Parallax Map", 2D) = "black" {}
        [ToggleShowIfAny(SECONDARY_UVS_PARALLAX, 2, 0_SECONDARY_UVS_NONE, 0_PARALLAX_NONE)] _SecondaryUVsParallax ("Parallax Texture Secondary UVs", Float) = 0
        [VectorShowIfAny(2, 0_PARALLAX_NONE)] _ParallaxTexSpeed ("Parallax Speed", Vector) = (0,0,0,0)
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxIntensity ("Parallax Intensity", Float) = 1
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxIntensity_Step ("Parallax Intensity Step", Float) = -0.25
        [ShowIfAny(_PARALLAX_FLEXIBLE)] _Layers ("Layers", Range(2, 6)) = 3
        [ShowIfAny(0_PARALLAX_NONE)] _StartOffset ("Start Offset", Float) = 1
        [ShowIfAny(0_PARALLAX_NONE)] _OffsetStep ("Offset Step", Float) = 1
        [ToggleShowIfAny(PARALLAX_IRIDESCENCE, 0_PARALLAX_NONE)] _Parallax_Iridescence ("Iridescence", Float) = 0
        [ShowIfAny(2, 0_PARALLAX_NONE, PARALLAX_IRIDESCENCE)] _IridescenceAxesMultiplier ("Axes Multiplier", Vector) = (1,2,3,0)
        [ShowIfAny(2, 0_PARALLAX_NONE, PARALLAX_IRIDESCENCE)] _IridescenceTiling ("Iridescence Tiling", Float) = 0.25
        [ShowIfAny(2, 0_PARALLAX_NONE, PARALLAX_IRIDESCENCE)] _IridescenceColorInfluence ("Color Influence", Range(0, 1)) = 0
        [EnumShowIfAny(3, None, Texture, Vertex Color, 0_PARALLAX_NONE)] _Parallax_Masking ("Mask by", Float) = 0
        [ShowIfAny(2, 0_PARALLAX_NONE, _PARALLAX_MASKING_TEXTURE)] _ParallaxMaskingMap ("Parallax Mask", 2D) = "white" {}
        [VectorShowIfAny(2, 2, 0_PARALLAX_NONE, _PARALLAX_MASKING_TEXTURE)] _ParallaxMaskSpeed ("Mask Speed", Vector) = (0,0,0,0)
        [ShowIfAny(2, 0_PARALLAX_NONE, _PARALLAX_MASKING_TEXTURE)] _ParallaxMaskIntensity ("Mask Intensity", Range(0, 1)) = 1
        [Space(12)] [KeywordEnum(None, Lerp, Additive)] _RimLight ("Rim Light Type", Float) = 0
        [ToggleShowIfAny(RIMLIGHT_INVERT, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _InvertRimlight ("Invert Rimlight", Float) = 0
        [ToggleShowIfAny(DIRECTIONAL_RIM, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _EnableDirectionalRim ("Make Rim Directional", Float) = 0
        [VectorShowIfAny(3, 1, DIRECTIONAL_RIM, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimPerpendicularAxis ("Rim Perpendicular Axis", Vector) = (0,1,0,0)
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightEdgeStart ("Rim Light Edge Start", Float) = 0.5
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightColor ("Rim Light Color", Color) = (1,1,1,0)
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightIntensity ("Rim Light Intensity", Float) = 1
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightBloomIntensity ("Rim Light Bloom Intensity", Float) = 1
        [EnumShowIfAny(3, None, MainEffect, Always, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _Rim_WhiteBoostType ("Rimlight Color Treatment", Float) = 0
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightWhiteboostMultiplier ("Rim Light Whiteboost Multiplier", Float) = 1
        [Header(LIGHTNING)] [Header(Ambient)] [Space(8)] _AmbientMinimalValue ("Ambient Minimum", Range(0, 1)) = 0
        _NominalDiffuseLevel ("Ambient Color", Color) = (0,0,0,0)
        _AmbientMultiplier ("Ambient Color Multiplier", Float) = 1
        [Space(18)] [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", Float) = 1
        [ToggleShowIfAny(LIGHT_FALLOFF, DIFFUSE, SPECULAR)] _EnableLightFalloff ("Light Falloff", Float) = 0
        [ToggleShowIfAny(INVERT_DIFFUSE_NORMAL, DIFFUSE)] _InvertDiffuseNormal ("Invert Diffuse Normal", Float) = 0
        [ToggleShowIfAny(BOTH_SIDES_DIFFUSE, DIFFUSE)] _EnableBothSidesDiffuse ("Both Sides Diffuse", Float) = 0
        [ShowIfAny(2, BOTH_SIDES_DIFFUSE, DIFFUSE)] _BothSidesDiffuseMultiplier ("Far Side Multiplier", Float) = 1
        [Space(12)] [Toggle(PRIVATE_POINT_LIGHT)] _PrivatePointLight ("Private Point Light", Float) = 0
        [ToggleShowIfAny(INSTANCED_PRIVATE_POINT_LIGHT, PRIVATE_POINT_LIGHT)] _InstancedPrivatePointLightColor ("Instance Color", Float) = 0
        [ShowIfAny(PRIVATE_POINT_LIGHT)] [HDR] _PrivatePointLightColor ("Color", Color) = (1,0,0,0)
        [ToggleShowIfAny(POINT_LIGHT_IS_LOCAL, PRIVATE_POINT_LIGHT)] _PointLightPositionLocal ("Make Position Local", Float) = 0
        [ShowIfAny(PRIVATE_POINT_LIGHT)] _PrivatePointLightIntensity ("Intensity Multiplier", Float) = 1
        [ShowIfAny(PRIVATE_POINT_LIGHT)] _PrivatePointLightPosition ("Light World Position", Vector) = (0,0,0,1)
        [Space(12)] [Toggle(DIFFUSE_TEXTURE)] _EnableDiffuseTexture ("Albedo Texture", Float) = 0
        [EnumShowIfAny(3, Texture, MPM R, MPM A Smoothness, DIFFUSE_TEXTURE)] _Diffuse_Texture_Source ("Diffuse Texture Source", Float) = 0
        [ShowIfAny(2, DIFFUSE_TEXTURE, _DIFFUSE_TEXTURE_SOURCE_TEXTURE)] _DiffuseTex ("Diffuse Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_DIFFUSE, 2, 0_SECONDARY_UVS_NONE, DIFFUSE_TEXTURE)] _SecondaryUVsDiffuse ("Diffuse Texture Secondary UVs", Float) = 0
        [ShowIfAny(2, DIFFUSE_TEXTURE, _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS)] _AlbedoMultiplier ("Albedo multiplier", Float) = 1
        [Space(12)] [Toggle(SPECULAR)] _EnableSpecular ("Specular", Float) = 1
        [ShowIfAny(SPECULAR)] _SpecularIntensity ("Specular Intensity", Float) = 1
        [Space(12)] [Toggle(LIGHTMAP)] _EnableLightmap ("Lightmap", Float) = 0
        [Space(12)] [Toggle(NORMAL_MAP)] _EnableNormalMap ("Normal Map", Float) = 0
        [ShowIfAny(NORMAL_MAP)] _NormalTex ("Normal Texture", 2D) = "bump" {}
        [ToggleShowIfAny(SECONDARY_UVS_NORMAL, 2, 0_SECONDARY_UVS_NONE, NORMAL_MAP)] _SecondaryUVsNormal ("Normal Map Secondary UVs", Float) = 0
        [ShowIfAny(NORMAL_MAP)] _NormalScale ("Normal Scale", Float) = 1
        [Space(12)] [Toggle(USE_SPHERICAL_NORMAL_OFFSET)] _UseSphericalNormalOffset ("Spherical Normal Offset", Float) = 0
        [ShowIfAny(USE_SPHERICAL_NORMAL_OFFSET)] _SphericalNormalOffsetIntensity ("Spherical Normal Offset Intensity", Float) = 0.5
        [ShowIfAny(USE_SPHERICAL_NORMAL_OFFSET)] _SphericalNormalOffsetCenter ("Spherical Normal Offset Center", Vector) = (0,0,0,1)
        [Header(REFLECTIONS)] [Space(12)] [Toggle(REFLECTION_TEXTURE)] _EnableReflectionTexture ("Reflection Texture", Float) = 0
        [ShowIfAny(REFLECTION_TEXTURE)] _ReflectionTexIntensity ("Texture Intensity", Float) = 1
        [ShowIfAny(REFLECTION_TEXTURE)] _EnvironmentReflectionCube ("Environment Reflection", Cube) = "" {}
        [Space(12)] [Toggle(REFLECTION_PROBE)] _EnableReflectionProbe ("Reflection Probe", Float) = 0
        [EnumShowIfAny(2, Fast, Precise, REFLECTION_PROBE)] _Probe_Calculation ("Probe Calculations", Float) = 0
        [ToggleShowIfAny(REFLECTION_PROBE_DISABLED_WHITEBOOST, REFLECTION_PROBE)] _ReflectionProbeDisabledWhiteboost ("Disable Probe Whiteboost", Float) = 0
        [ShowIfAny(2, REFLECTION_PROBE, _PROBE_CALCULATION_PRECISE)] _ReflectionProbeGrayscale ("Probe Grayscale Factor", Range(0, 1)) = 0.2
        [ShowIfAny(2, REFLECTION_PROBE, _PROBE_CALCULATION_PRECISE)] _ColoredMetalMultiplier ("Colored Metal Multiplier", Range(0, 15)) = 3.5
        [ShowIfAny(2, REFLECTION_PROBE, _PROBE_CALCULATION_PRECISE)] _WhiteOffset ("White Offset", Float) = 2
        [ShowIfAny(REFLECTION_PROBE)] _ReflectionProbeIntensity ("Probe Intensity", Float) = 1
        [ToggleShowIfAny(REFLECTION_PROBE_BOX_PROJECTION, REFLECTION_PROBE)] _ReflectionProbeBoxProjection ("Box Projection", Float) = 1
        [ToggleShowIfAny(REFLECTION_PROBE_BOX_PROJECTION_OFFSET, 2, REFLECTION_PROBE, REFLECTION_PROBE_BOX_PROJECTION)] _EnableBoxProjectionOffset ("Box Projection Offset", Float) = 0
        [ShowIfAny(3, REFLECTION_PROBE, REFLECTION_PROBE_BOX_PROJECTION, REFLECTION_PROBE_BOX_PROJECTION_OFFSET)] _ReflectionProbeBoxProjectionSizeOffset ("Box Projection Size Offset", Vector) = (0,0,0,0)
        [ShowIfAny(3, REFLECTION_PROBE, REFLECTION_PROBE_BOX_PROJECTION, REFLECTION_PROBE_BOX_PROJECTION_OFFSET)] _ReflectionProbeBoxProjectionPositionOffset ("Box Projection Position Offset", Vector) = (0,0,0,0)
        [ToggleShowIfAny(REFLECTION_STATIC, REFLECTION_PROBE)] _ReflectionStatic ("Static Reflection", Float) = 0
        [ToggleShowIfAny(REFLECTION_PROBE_SINGLE_CUBEMAP, REFLECTION_PROBE)] _ReflectionSingleCubemap ("Single Cubemap Reflections", Float) = 0
        [ToggleShowIfAny(MULTIPLY_REFLECTIONS, 2, REFLECTION_PROBE, REFLECTION_TEXTURE)] _MultiplyReflections ("Multiply Reflection Texture", Float) = 1
        [Toggle(RIM_DIM)] _EnableRimDim ("Reflection Rim Dim", Float) = 0
        [ShowIfAny(RIM_DIM)] _RimScale ("Rim Scale", Float) = 1
        [ShowIfAny(RIM_DIM)] _RimOffset ("Rim Offset", Float) = 1
        [ShowIfAny(RIM_DIM)] _RimDistanceOffset ("Rim Camera Distance Offset", Float) = 2
        [ShowIfAny(RIM_DIM)] _RimDistanceScale ("Rim Camera Distance Scale", Float) = 0.3
        [ShowIfAny(RIM_DIM)] _RimSmoothness ("Rim Smoothness", Float) = 1
        [ShowIfAny(RIM_DIM)] _RimDarkening ("Rim Darkening", Float) = 0
        [ToggleShowIfAny(INVERT_RIM_DIM, RIM_DIM)] _InvertRimDim ("Invert Rim Dim", Float) = 0
        [Header(OCCLUSION AND GROUND FADE)] [Space(12)] [Toggle(GROUND_FADE)] _EnableGroundFade ("Height Occlusion", Float) = 0
        [ShowIfAny(GROUND_FADE)] _GroundFadeScale ("Height Occlusion Scale", Float) = 0.5
        [ShowIfAny(GROUND_FADE)] _GroundFadeOffset ("Height Occlusion Offset", Float) = 1
        [Space(12)] [Toggle(OCCLUSION)] _EnableOcclusion ("Texture Occlusion", Float) = 0
        [EnumShowIfAny(3, Texture, MPM B, Avatar MPM R, OCCLUSION)] _Occlusion_Source ("Occlusion Source", Float) = 0
        [ShowIfAny(2, OCCLUSION, _OCCLUSION_SOURCE_TEXTURE)] _DirtTex ("Occlusion Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_OCCLUSION, 2, 0_SECONDARY_UVS_NONE, _OCCLUSION_SOURCE_TEXTURE, OCCLUSION)] _SecondaryUVsOcclusion ("Occlusion Map Secondary UVs", Float) = 0
        [ShowIfAny(OCCLUSION)] _OcclusionIntensity ("Occlusion Intensity", Range(0, 1)) = 1
        [Space(12)] [Toggle(OCCLUSION_DETAIL)] _EnableOcclusionDetail ("Texture Occlusion Detail", Float) = 0
        [ShowIfAny(OCCLUSION_DETAIL)] _DirtDetailTex ("Occlusion Detail Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_OCCLUSION_DETAIL, 2, 0_SECONDARY_UVS_NONE, OCCLUSION_DETAIL)] _SecondaryUVsOcclusionDetail ("Occlusion Detail Secondary UVs", Float) = 0
        [ShowIfAny(OCCLUSION_DETAIL)] _OcclusionDetailIntensity ("Occlusion Detail Intensity", Range(0, 1)) = 1
        [ToggleShowIfAny(OCCLUSION_BEFORE_EMISSION, OCCLUSION, OCCLUSION_DETAIL)] _OcclusionBeforeEmission ("Texture Occlusion Before Emission", Float) = 0
        [Header(OTHER)] [Space(16)] _EnableRotateUV ("Rotate UVs 90", Float) = 0
        _RotateUV ("Rotation Angle", Float) = 0
        [Space(12)] [Toggle(UV_COLOR_SEGMENTS)] _UVColorSegments ("UV Color Segments", Float) = 0
        [ToggleShowIfAny(UV_SEGMENTS_IGNORE_RIM, UV_COLOR_SEGMENTS)] _UvSegmentsIgnoreRim ("Don't override Rim color", Float) = 0
        [Space(12)] [Toggle(HIGHLIGHT_SELECTION)] _HighlightSelection ("Highlight Selection", Float) = 0
        [ShowIfAny(HIGHLIGHT_SELECTION)] _SegmentToHighlight ("Segment To Highlight", Float) = -1
        [Space(12)] [Toggle(FOG)] _EnableFog ("Fog", Float) = 1
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", Float) = 0
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", Float) = 1
        [ToggleShowIfAny(HEIGHT_FOG, FOG)] _EnableHeightFog ("Height Fog", Float) = 0
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", Float) = 1
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", Float) = 0
        [ToggleShowIfAny(HEIGHT_FOG_DEPTH_SOFTEN, 2, FOG, HEIGHT_FOG)] _EnableHeightFogSoften ("Soften with Distance", Float) = 0
        [ShowIfAny(3, FOG, HEIGHT_FOG, HEIGHT_FOG_DEPTH_SOFTEN)] _FogSoften ("Soften Scale", Float) = 1
        [ShowIfAny(3, FOG, HEIGHT_FOG, HEIGHT_FOG_DEPTH_SOFTEN)] _FogSoftenOffset ("Soften Offset", Float) = 1
        [ShowIfAny(1, FOG, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK, _VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL)] _EmissionFogSuppression ("Quest Fog Supression", Range(0, 1)) = 0
        [ShowIfAny(1, FOG, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK, _VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL)] _MainEffectFogSuppression ("MainEffect Fog Supression", Range(0, 1)) = 0
        [Space(18)] [Toggle(COLOR_BY_FOG)] _ColorFog ("Color by Fog", Float) = 0
        [ShowIfAny(COLOR_BY_FOG)] _ColorFogMultiplier ("Fog Multiplier", Float) = 1
        [ShowIfAny(COLOR_BY_FOG)] _ColorFogMax ("Fog Max Brightness", Float) = 1
        [ShowIfAny(COLOR_BY_FOG)] _ColorFogInfluence ("Color Influence", Range(0, 1)) = 0.5
        [ToggleShowIfAny(FOG_COLOR_HIGHLIGHT, COLOR_BY_FOG)] _FogColorHighlight ("Fog Highlight", Float) = 0
        [ShowIfAny(2, COLOR_BY_FOG, FOG_COLOR_HIGHLIGHT)] _ColorFogHighlightMultiplier ("Fog Highlight Multiplier", Float) = 30000
        [Space(12)] [Toggle(DISTANCE_DARKENING)] _EnableDistanceDarkening ("Worldspace Occlusion", Float) = 0
        [ShowIfAny(DISTANCE_DARKENING)] _DarkeningScale ("Scale", Float) = 0.35
        [ShowIfAny(DISTANCE_DARKENING)] _DarkeningIntensity ("Intensity", Float) = 1
        [VectorShowIfAny(3, DISTANCE_DARKENING)] _DarkeningCenter ("Center", Vector) = (0,0,0,0)
        [VectorShowIfAny(3, DISTANCE_DARKENING)] _DarkeningDirection ("Axes", Vector) = (1,1,1,1)
        [Space(12)] [KeywordEnum(None, Grid, Scanline, Legacy)] _Hologram ("Hologram Effect", Float) = 0
        [ToggleShowIfAny(HOLOGRAM_MATERIALIZATION, _HOLOGRAM_GRID)] _UseHologramMaterialization ("Materialization", Float) = 0
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE, _HOLOGRAM_LEGACY)] _HologramColor ("Hologram Color", Color) = (1,1,1,1)
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_LEGACY)] _HologramGridSize ("Hologram Grid Size", Float) = 3
        [ShowIfAny(_HOLOGRAM_GRID)] _HologramFill ("Hologram Fill", Float) = -0.6
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HologramStripeSpeed ("Hologram Stripe Speed", Float) = 1.43
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HologramScanDistance ("Hologram Scan Distance", Float) = 2
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HologramPhaseOffset ("Hologram Phase Offset", Range(-1, 1)) = 0
        [ShowIfAny(2, _HOLOGRAM_GRID, HOLOGRAM_MATERIALIZATION)] _HoloMaterialize ("Hologram Materialize", Range(0, 1)) = 1
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HoloIntensity ("Hologram Intensity", Float) = 1
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HaltScan ("Halt scanning", Float) = 0
        [Space(12)] [Toggle(FAKE_MIRROR_TRANSPARENCY)] _EnableFakeMirrorTransparency ("Fake Mirror Transparency", Float) = 0
        [ShowIfAny(FAKE_MIRROR_TRANSPARENCY)] _FakeMirrorTransparency ("Mirror Transparency Multiplier", Float) = 1
        [ToggleShowIfAny(MIRROR_VERTEX_DISTORTION, FAKE_MIRROR_TRANSPARENCY)] _NoteVertexDistortion ("Mirror Vertex Distortion", Float) = 0
        [HideInInspector] Note_Plane_Cut ("Note Plane Cut", Float) = 0
        [ShowIfAny(NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _CutPlaneEdgeGlowWidth ("Plane Edge Glow Width", Float) = 0.01
        [ShowIfAny(NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _NoteSize ("Note Size", Float) = 0.25
        [ShowIfAny(NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _CutPlane ("Cut Plane", Vector) = (1,0,0,0)
        [HideInInspector] Cutout_Type ("Cutout", Float) = 0
        [ShowIfAny(CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _Cutout ("Cutout Threshold", Range(0, 1)) = 0
        [ShowIfAny(CUTOUT_TYPE_HD_DISSOLVE)] _CutoutTexScale ("Cutout Texture Scale", Float) = 1
        [ToggleShowIfAny(CLOSE_TO_CAMERA_CUTOUT, CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _EnableCloseToCameraCutout ("Close to Camera Cutout", Float) = 0
        [ShowIfAny(1, CLOSE_TO_CAMERA_CUTOUT, CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _CloseToCameraCutoutOffset ("Close to Camera Cutout Offset", Float) = 0.5
        [ShowIfAny(1, CLOSE_TO_CAMERA_CUTOUT, CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _CloseToCameraCutoutScale ("Close to Camera Cutout Scale", Float) = 0.5
        [ShowIfAny(CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE, NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _GlowCutoutColor ("Cut/Cutout Glow Color", Color) = (1,1,1,1)
        [Space(12)] [Toggle(DISSOLVE)] _EnableDissolve ("Dissolve", Float) = 0
        [EnumShowIfAny(3, Clip, Fade, Both, DISSOLVE)] _DissolveAlpha ("Alpha Aproach", Float) = 0
        [ShowIfAny(1, DISSOLVE, _DISSOLVEALPHA_FADE, _DISSOLVEALPHA_BOTH)] _AlphaMultiplier ("Alpha Multiplier", Float) = 1
        [ShowIfAny(1, DISSOLVE, _DISSOLVEALPHA_FADE, _DISSOLVEALPHA_BOTH)] _DissolveScale ("Fade Falloff Scale", Float) = 5
        [Space(16)] [FloatToggleShowIfAny(DISSOLVE)] _DissolveReverse ("Invert Dissolve", Float) = 0
        [EnumShowIfAny(5, Local, World, World Centered, Uv, Avatar, DISSOLVE)] _Dissolve_Space ("Dissolve Space", Float) = 0
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeStartY ("Body Fade Fully Opaque Y", Float) = 1.25
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeEndY ("Body Fade Fully Transparent Y", Float) = 1
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeZoneInterceptX ("Fade Zone Intercept X", Float) = 0.2
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeZoneSlope ("Fade Zone Slope", Float) = 0.76
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _BodyFadeGamma ("Fadeout Factor", Float) = 2
        [ShowIfAny(2, DISSOLVE, 0_DISSOLVEAXIS_AVATAR)] _DissolveAxisVector ("Dissolve Axis", Vector) = (0,1,0,0)
        [ToggleShowIfAny(DISSOLVE_PROGRESS, DISSOLVE)] _UseDissolveProgress ("Dissolve Progress", Float) = 0
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVEAXIS_AVATAR, 0DISSOLVE_PROGRESS)] _DissolveOffset ("Dissolve Offset", Float) = 0
        [ShowIfAny(3, DISSOLVE, DISSOLVE_PROGRESS, 0_DISSOLVEAXIS_AVATAR)] _DissolveStartValue ("Dissolve Start Value", Float) = 0
        [ShowIfAny(3, DISSOLVE, DISSOLVE_PROGRESS, 0_DISSOLVEAXIS_AVATAR)] _DissolveEndValue ("Dissolve End Value", Float) = 10
        [ShowIfAny(3, DISSOLVE, DISSOLVE_PROGRESS, 0_DISSOLVEAXIS_AVATAR)] _DissolveProgress ("Dissolve Progress", Range(-1, 1)) = 0
        [ToggleShowIfAny(DISSOLVE_COLOR, 1, DISSOLVE)] _UseDissolveColor ("Dissolve Color", Float) = 0
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _DissolveColor ("Dissolve Color", Color) = (0,1,1,0)
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _DissolveColorIntensity ("Color Intensity", Float) = 1
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _CutColorFalloff ("Cut Falloff Scale", Float) = 4
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _CutColorBacksideFalloff ("Backface Falloff Multiplier", Float) = 0.07
        [EnumShowIfAny(4, None, Local, World, Uv, DISSOLVE, DISSOLVE_COLOR)] _Dissolve_Grid ("Dissolve Grid", Float) = 0
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridThickness ("Grid Thickness", Float) = 1.5
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridSize ("Grid Size", Float) = 10
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridFalloff ("Grid Falloff Scale", Float) = 4
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridSpeed ("Grid Speed", Float) = 0.1
        [ToggleShowIfAny(DISSOLVE_TEXTURE, 1, DISSOLVE)] _UseDissolveTexture ("Dissolve Texture", Float) = 0
        [ShowIfAny(2, DISSOLVE, DISSOLVE_TEXTURE)] _DissolveTexture ("Dissolve Texture", 2D) = "black" {}
        [VectorShowIfAny(2, 2, DISSOLVE, DISSOLVE_TEXTURE)] _DissolveTextureSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [ShowIfAny(2, DISSOLVE, DISSOLVE_TEXTURE)] _DissolveTextureInfluence ("Texture Influence", Float) = 0.2
        [KeywordEnum(None, Simple)] Distortion ("Distortion", Float) = 0
        [EnumShowIfAny(10, MPM, EmissionTex, Emission Mask, Secondary Emission Mask, Pulse, Parralax, Diffuse, Normal, Occlusion, Occlusion Detail, DISTORTION_SIMPLE)] _Distortion_Target ("Distortion Target", Float) = 0
        [Space(12)] [ShowIfAny(DISTORTION_SIMPLE)] _DistortionTex ("Distortion Texture", 2D) = "black" {}
        [ToggleShowIfAny(SECONDARY_UVS_DISTORTION, 2, 0_SECONDARY_UVS_NONE, DISTORTION_SIMPLE)] _SecondaryUVsDistortion ("Distortion Secondary UVs", Float) = 0
        [ShowIfAny(DISTORTION_SIMPLE)] _DistortionStrength ("Distortion Strength", Float) = 0.2
        [ShowIfAny(DISTORTION_SIMPLE)] _DistortionAxes ("Distortion Axes", Vector) = (1,1,0,0)
        [ShowIfAny(DISTORTION_SIMPLE)] _DistortionPanning ("Distortion Panning", Vector) = (0,0,0,0)
        [Space(12)] [Header(Other)] [Space] [Toggle(NOISE_DITHERING)] _EnableNoiseDithering ("Noise Dithering", Float) = 1
        [Toggle(LINEAR_TO_GAMMA)] _LinearToGamma ("LinearToGamma", Float) = 0
        [KeywordEnum(Standard, Song Time, Freeze)] _Custom_Time ("Time Behavior", Float) = 0
        [KeywordEnum(None, Around_X, Around_Y, Around_Z)] _Curve_Vertices ("Curve Vertices", Float) = 0
        [KeywordEnum(After Emissive, Before Emissive)] _Aces_Approach ("ACES approach", Float) = 0
        [Space(12)] [Toggle(TEXTURE3D_LOOKUP)] _Texture3D_Lookup ("Texture 3D Grid Lookup", Float) = 0
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupTex ("Lookup Texture", 3D) = "gray" {}
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupGridSize ("Lookup Grid Size", Vector) = (1,1,1,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupXYZDisplacementScale ("Lookup XYZ Displacement Scale", Float) = 10
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupXDisplacementMapping ("Lookup X Displacement Mapping", Vector) = (0,0,0,0)
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupYDisplacementMapping ("Lookup Y Displacement Mapping", Vector) = (0,0,0,0)
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupZDisplacementMapping ("Lookup Z Displacement Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupRadialDisplacementScale ("Lookup Radial Displacement Scale", Float) = 10
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupRadialDisplacementMapping ("Lookup Radial Displacement Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupMaxScale ("Lookup Max Scale", Float) = 2
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupScaleMapping ("Lookup Scale Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupRotationMultiplier ("Lookup Rotation Multiplier", Float) = 1
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupRotationMapping ("Lookup Rotation Mapping", Vector) = (0,0,0,0)
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupEmissiveMapping ("Lookup Emissive Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupEmissiveModulationStrength ("Emissive Modulation Strength", Float) = 1
        [Header(SETTINGS)] [Space(12)] [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", Float) = 2
        [Toggle] _ZWrite ("Z Write", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", Float) = 0
        [Space(12)] [Header(Color Blending)] [Space] [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Foreground Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Background Factor", Float) = 0
        [Header(Bloom Blending)] [Space] [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Foreground Factor", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Background Factor", Float) = 0
        [Space(12)] [Toggle(MESH_PACKING)] _MeshPacking ("Mesh Packed Instancing", Float) = 0
        [ShowIfAny(MESH_PACKING)] _MeshPackingId ("Mesh Packing Id", Float) = 1
        [Toggle(COLOR_ARRAY)] _UseColorArray ("Color Array", Float) = 0
        _SDFNoiseOffset ("Noise offset", Vector) = (0,0,0,0)
        _SDFNoisePanning ("Noise panning", Vector) = (0,0,0,0)
        _SDFNoiseIntensity ("Noise Intensity", Float) = 1
        _SDFNoiseScale ("Noise Scale", Float) = 5
        _SDFPointIntensity ("Color Intensity", Float) = 1
        _SDFNegativeIntensity ("Negative Intensity", Float) = 0.5
        _SDFNoiseTex ("Noise Tex", 3D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Stencil
        {
            Ref [_StencilRefValue]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE _SECONDARY_UVS_OBJECT_SPACE _SECONDARY_UVS_ADDITIVE_OFFSET

            #pragma shader_feature_local_fragment METAL_SMOOTHNESS_TEXTURE
            #pragma shader_feature_local_fragment _ _METALLIC_TEXTURE_SOURCE_MPM_R _METALLIC_TEXTURE_SOURCE_MPM_A
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_SOURCE_MPM_A _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS
            #pragma shader_feature_local PRECISE_NORMAL

            #pragma shader_feature_local _ _VERTEXMODE_COLOR _VERTEXMODE_EMISSION \
                _VERTEXMODE_METALSMOOTHNESS _VERTEXMODE_SPECIAL _VERTEXMODE_DISPLACEMENT \
                _VERTEXMODE_EMISSIVE_MULT_ADD
            #pragma shader_feature_local _ _VERTEX_WHITEBOOSTTYPE_MAINEFFECT \
                _VERTEX_WHITEBOOSTTYPE_ALWAYS

            #pragma shader_feature_local_vertex DISPLACEMENT_SPATIAL
            #pragma shader_feature_local_vertex DISPLACEMENT_BIDIRECTIONAL
            #pragma shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL
            #pragma shader_feature_local MESH_PACKING
            #pragma shader_feature_local_vertex VERTEXDISPLACEMENT_MASK
            #pragma shader_feature_local_vertex _ _VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE \
                _VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE

            #pragma shader_feature_local _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_PULSE \
                _EMISSIONTEXTURE_FLIPBOOK
            #pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_MPM_G
            #pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_SDF
            #pragma shader_feature_local SECONDARY_UVS_EMISSION
            #pragma shader_feature_local SECONDARY_UVS_PULSE
            #pragma shader_feature_local_fragment INVERT_PULSE
            #pragma shader_feature_local_fragment PULSE_MULTIPLY_TEXTURE
            #pragma shader_feature_local_fragment _ _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R

            #pragma shader_feature_local_fragment EMISSION_MASK
            #pragma shader_feature_local_fragment _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD
            #pragma shader_feature_local SECONDARY_UVS_EMISSION_MASK

            #pragma shader_feature_local_fragment SECONDARY_EMISSION_MASK
            #pragma shader_feature_local_fragment _ _SECONDARY_MASK_BLEND_ADD _SECONDARY_MASK_BLEND_MASKED_ADD
            #pragma shader_feature_local SECONDARY_UVS_EMISSION_MASK2

            #pragma shader_feature_local_fragment FLIPBOOK_BLENDING_OFF

            #pragma shader_feature_local PRIVATE_POINT_LIGHT
            #pragma shader_feature_local_fragment POINT_LIGHT_IS_LOCAL

            #pragma shader_feature_local DIFFUSE
            #pragma shader_feature_local_fragment BOTH_SIDES_DIFFUSE
            #pragma shader_feature_local_fragment LIGHT_FALLOFF
            #pragma shader_feature_local_fragment DIFFUSE_TEXTURE
            #pragma shader_feature_local_fragment _ _DIFFUSE_TEXTURE_SOURCE_TEXTURE _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS

            #pragma shader_feature_local SPECULAR

            #pragma shader_feature_local INVERT_RIM_DIM

            #pragma shader_feature_local_fragment _ _PARALLAX_FLEXIBLE _PARALLAX_RGB
            #pragma shader_feature_local _PARALLAX_FLEXIBLE_REFLECTED
            #pragma shader_feature_local_fragment _ _PARALLAX_PROJECTION_WARPED
            #pragma shader_feature_local PARALLAX_IRIDESCENCE
            #pragma shader_feature_local SECONDARY_UVS_PARALLAX
            #pragma shader_feature_local_fragment _ _PARALLAX_MASKING_TEXTURE _PARALLAX_MASKING_VERTEX_COLOR

            #pragma shader_feature_local_fragment DISTORTION_SIMPLE
            #pragma shader_feature_local NOISE_DITHERING
            #pragma shader_feature_local_fragment MULTIPLY_REFLECTIONS
            #pragma shader_feature_local REFLECTION_TEXTURE
            #pragma shader_feature_local REFLECTION_PROBE
            #pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION
            #pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET

            #pragma shader_feature_local_fragment GROUND_FADE

            #pragma shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE
            #pragma shader_feature_local_fragment _ _ACES_APPROACH_BEFORE_EMISSIVE
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING
            #pragma shader_feature_local COLOR_ARRAY
            #pragma shader_feature_local UV_COLOR_SEGMENTS
            #pragma shader_feature_local HIGHLIGHT_SELECTION
            #pragma shader_feature_local _ _HOLOGRAM_GRID _HOLOGRAM_SCANLINE _HOLOGRAM_LEGACY

            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG_DEPTH_SOFTEN
            #pragma shader_feature_local LIGHTMAP
            #pragma shader_feature_local_fragment OCCLUSION
            #pragma shader_feature_local_fragment DISTANCE_DARKENING
            #pragma shader_feature_local_fragment DISSOLVE
            #pragma shader_feature_local_fragment DISSOLVE_PROGRESS
            #pragma shader_feature_local_fragment DISSOLVE_COLOR

            // Selectors dropped or merged by the generated material UI are declared
            // independently so feature-signature collisions remain distinct.
            #pragma shader_feature_local COLOR_BY_FOG
            #pragma shader_feature_local DIRECTIONAL_RIM
            #pragma shader_feature_local DISSOLVE_TEXTURE
            #pragma shader_feature_local EMISSION_ANGLE_DISAPPEAR
            #pragma shader_feature_local RIM_DIM
            #pragma shader_feature_local FOG_COLOR_HIGHLIGHT
            #pragma shader_feature_local INSTANCED_PRIVATE_POINT_LIGHT
            #pragma shader_feature_local NORMAL_MAP
            #pragma shader_feature_local OCCLUSION_BEFORE_EMISSION
            #pragma shader_feature_local OCCLUSION_DETAIL
            #pragma shader_feature_local REFLECTION_STATIC
            #pragma shader_feature_local SECONDARY_UVS_MPM
            #pragma shader_feature_local SECONDARY_UVS_OCCLUSION
            #pragma shader_feature_local SECONDARY_UVS_OCCLUSION_DETAIL
            #pragma shader_feature_local SPECULAR_ANTIFLICKER
            #pragma shader_feature_local TEXTURE3D_EMISSION
            #pragma shader_feature_local TEXTURE3D_LOOKUP
            #pragma shader_feature_local USE_SPHERICAL_NORMAL_OFFSET
            #pragma shader_feature_local _DISSOLVE_SPACE_WORLD_CENTERED
            #pragma shader_feature_local _DISTORTION_TARGET_EMISSIONTEX
            #pragma shader_feature_local _ _EMISSIONCOLORTYPE_GRADIENT \
                _EMISSIONCOLORTYPE_MAINEFFECT _EMISSIONCOLORTYPE_WHITEBOOST
            #pragma shader_feature_local _ _METALLIC_TEXTURE_MPM_R
            #pragma shader_feature_local _OCCLUSION_SOURCE_MPM_B
            #pragma shader_feature_local _PROBE_CALCULATION_PRECISE
            #pragma shader_feature_local _ _RIMLIGHT_LERP _RIMLIGHT_ADDITIVE
            #pragma shader_feature_local _RIM_WHITEBOOSTTYPE_MAINEFFECT
            #pragma shader_feature_local _ _SMOOTHNESS_TEXTURE_MPM_A \
                _SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS

            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ POST_BLOOM
            #pragma multi_compile_fragment _ OVERDRAW_VIEW


            // Payload and feature macros
            // Payload requirements use the canonical feature selectors.
            #define USE_UV_SCALE (defined(_SECONDARY_UVS_EXTERNAL_SCALE) || defined(_SECONDARY_UVS_OBJECT_SPACE))
            #define USE_SECONDARY_UV_SOURCE (USE_UV_SCALE || defined(_SECONDARY_UVS_IMPORT))
            #define USE_SECONDARY_UV_CONSUMER (defined(SECONDARY_UVS_EMISSION) || \
                defined(SECONDARY_UVS_PULSE) || \
                defined(SECONDARY_UVS_EMISSION_MASK) || defined(SECONDARY_UVS_EMISSION_MASK2) || \
                defined(SECONDARY_UVS_PARALLAX) || defined(SECONDARY_UVS_MPM) || \
                defined(SECONDARY_UVS_OCCLUSION) || \
                defined(SECONDARY_UVS_OCCLUSION_DETAIL))
            #define USE_SECONDARY_UV (USE_SECONDARY_UV_SOURCE && USE_SECONDARY_UV_CONSUMER)
            #define USE_NOISE_SCREEN_POSITION defined(NOISE_DITHERING)
            #define USE_NORMAL_MAP_PAYLOAD defined(NORMAL_MAP)
            #define USE_ANTIFLICKER_NORMAL_PAYLOAD defined(SPECULAR_ANTIFLICKER)
            #if defined(MESH_PACKING)
            #if USE_SECONDARY_UV || defined(COLOR_ARRAY) || defined(LIGHTMAP)
            #define USE_MESH_PACKING_UV1 0
            #else
            #define USE_MESH_PACKING_UV1 1
            #endif
            #else
            #define USE_MESH_PACKING_UV1 0
            #endif

            #define USE_SPHERE_SDF_3D_VERTEX \
                 defined(_VERTEXMODE_DISPLACEMENT) && defined(VERTEXDISPLACEMENT_MASK) && \
                 defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE) && \
                 defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT) && defined(_CUSTOM_TIME_FREEZE) && \
                 !defined(_VERTEXMODE_COLOR) && !defined(_VERTEXMODE_EMISSION) && \
                 !defined(_VERTEXMODE_METALSMOOTHNESS) && !defined(_VERTEXMODE_SPECIAL) && \
                 !defined(_VERTEXMODE_EMISSIVE_MULT_ADD) && !defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) && \
                 !defined(DISPLACEMENT_SPATIAL) && !defined(DISPLACEMENT_BIDIRECTIONAL) && \
                 !defined(_SPECTROGRAM_FLAT) && !defined(_SPECTROGRAM_FULL) && !defined(MESH_PACKING) && \
                 !defined(_CUSTOM_TIME_SONG_TIME)
            #define USE_RIBBON_SPATIAL_MASK_VERTEX \
                 defined(_VERTEXMODE_DISPLACEMENT) && defined(DISPLACEMENT_SPATIAL) && \
                 defined(DISPLACEMENT_BIDIRECTIONAL) && defined(VERTEXDISPLACEMENT_MASK) && \
                 defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT) && defined(_CUSTOM_TIME_FREEZE) && \
                !defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE) && \
                  !defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) && \
                 !defined(_CUSTOM_TIME_SONG_TIME) && !defined(MESH_PACKING) && \
                 !defined(_SPECTROGRAM_FLAT) && !defined(_SPECTROGRAM_FULL)
            #define USE_VERTEX_EMISSION (defined(_VERTEXMODE_EMISSION) || \
                defined(_VERTEXMODE_SPECIAL) || defined(_VERTEXMODE_EMISSIVE_MULT_ADD))
            #define USE_VERTEX_COLOR (USE_VERTEX_EMISSION || defined(_VERTEXMODE_COLOR) || \
                defined(_VERTEXMODE_METALSMOOTHNESS) || defined(_VERTEXMODE_DISPLACEMENT))
            #if defined(PRIVATE_POINT_LIGHT) && !defined(INSTANCED_PRIVATE_POINT_LIGHT)
            #define USE_UNIFORM_PRIVATE_POINT_COLOR 1
            #else
            #define USE_UNIFORM_PRIVATE_POINT_COLOR 0
            #endif
            #define ENABLE_EMISSION_TEXTURE defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK)
            #define USE_EMISSION_TEXTURE !defined(_EMISSION_TEXTURE_SOURCE_MPM_G) && (defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_FLIPBOOK))
            #define USE_EMISSION_TEXTURE_COLOR ENABLE_EMISSION_TEXTURE
            // USE_EMISSION_GRADIENT_TEXTURE removed — gradient is now handled inside USE_EMISSION_TEXTURE_COLOR
            #define USE_EMISSION_MASK defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_SIMPLE)
            #define USE_FOG_SUPPRESSION defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK) || defined(_VERTEXMODE_EMISSION) || defined(_VERTEXMODE_SPECIAL)
            #define USE_WORLD_NORMAL defined(DIFFUSE) || defined(SPECULAR) || \
                defined(PARALLAX_IRIDESCENCE) || defined(_PARALLAX_FLEXIBLE_REFLECTED) || \
                defined(PRIVATE_POINT_LIGHT) || \
                defined(REFLECTION_TEXTURE) || defined(REFLECTION_PROBE) || defined(REFLECTION_STATIC) || \
                defined(_VERTEXMODE_DISPLACEMENT) || \
                defined(USE_SPHERICAL_NORMAL_OFFSET) || \
                defined(EMISSION_ANGLE_DISAPPEAR) || \
                defined(RIM_DIM) || defined(UV_COLOR_SEGMENTS) || \
                defined(_RIMLIGHT_LERP) || defined(_RIMLIGHT_ADDITIVE) || \
                defined(TEXTURE3D_LOOKUP) || \
                USE_NORMAL_MAP_PAYLOAD || USE_ANTIFLICKER_NORMAL_PAYLOAD

            // Uniform declarations
            // USE_SECONDARY_UV
            // USE_UV_SCALE
            float4 _UVScale;
            // --
            float2 _InputUvMultiplier;
            // --

            // METAL_SMOOTHNESS_TEXTURE
            sampler2D _MetalSmoothnessTex;
            float4 _MetalSmoothnessTex_ST;
            #if USE_NORMAL_MAP_PAYLOAD
            sampler2D _NormalTex;
            float4 _NormalTex_ST;
            float _NormalScale;
            #endif
            // --
            float _Smoothness;
            float _Metallic;

            samplerCUBE _ReflectionProbeTexture1;
            samplerCUBE _ReflectionProbeTexture2;
            #if defined(REFLECTION_TEXTURE)
            samplerCUBE _EnvironmentReflectionCube;
            float _ReflectionTexIntensity;
            #endif
            float4 _LightProbeLightBakeIdA;
            float4 _LightProbeLightBakeIdB;
            float4 _LightProbeLightBakeIdC;
            float4 _LightProbeLightBakeIdD;
            float4 _LightProbeLightBakeIdE;
            float4 _LightProbeLightBakeIdF;
            float3 _ReflectionProbePosition;
            float3 _ReflectionProbeBoundsMin;
            float3 _ReflectionProbeBoundsMax;
            float _ReflectionProbeIntensity;
            float _ReflectionProbeGrayscale;
            float _ColoredMetalMultiplier;
            float _WhiteOffset;
            float _AntiflickerStrength;
            float _AntiflickerDistanceScale;
            float _AntiflickerDistanceOffset;
            float3 _ReflectionProbeBoxProjectionSizeOffset;
            float3 _ReflectionProbeBoxProjectionPositionOffset;

            #if defined(NOISE_DITHERING)
            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float _GlobalRandomValue;
            #endif

            // USE_VERTEX_EMISSION
            float _EmissionThreshold;
            float _EmissionStrength;
            float _EmissionBloomIntensity;
            float _QuestWhiteboostMultiplier;
            // --

            // USE_EMISSION_TEXTURE
            sampler2D _EmissionTex;
            float4 _EmissionTex_ST;
            // _EMISSIONTEXTURE_SIMPLE
            float2 _EmissionTexSpeed;
            // --
            // --

            // EMISSION_ANGLE_DISAPPEAR && ENABLE_EMISSION_TEXTURE
            float _EmissionThresholdAngle;
            // --

            sampler2D _EmissionGradientTex;
            float4 _EmissionGradientTex_ST;
            // --
            // _EMISSIONBLOOMTYPE_GRADIENT
            float _EmissionGradientPanningSpeed;
            // --

            sampler2D _PulseMask;
            float4 _PulseMask_ST;
            float _PulseWidth;
            float _PulseSpeed;
            float _PulseSmooth;

            // _EMISSIONTEXTURE_FLIPBOOK
            float _FlipbookColumns;
            float _FlipbookRows;
            float _FlipbookNonloopableFrames;
            float _FlipbookSpeed;
            // --

            float _EmissionTexBloomIntensity;
            float _EmissionTexWhiteBoostMultiplier;

            // USE_EMISSION_MASK
            // EMISSION_MASK
            sampler2D _EmissionMask;
            float4 _EmissionMask_ST;
            float2 _EmissionMaskSpeed;
            // --
            // SECONDARY_EMISSION_MASK
            sampler2D _SecondaryEmissionMask;
            float4 _SecondaryEmissionMask_ST;
            float2 _SecondaryEmissionMaskSpeed;
            // --
            float _EmissionMaskStepValue;
            float _EmissionMaskStepWidth;
            // --

            float _AmbientMinimalValue;
            float _AmbientMultiplier;
            #if defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY)
            float _HologramGridSize;
            float _HologramScanDistance;
            float _HoloIntensity;
            #endif

            // DIFFUSE_TEXTURE
            sampler2D _DiffuseTex;
            float4 _DiffuseTex_ST;
            float _AlbedoMultiplier;
            // --

            sampler2D _DirtDetailTex;
            float4 _DirtDetailTex_ST;
            sampler2D _DirtTex;
            float4 _DirtTex_ST;
            float _OcclusionIntensity;
            sampler2D _LightMap1;
            sampler2D _LightMap2;
            float3 _LightmapLightBakeIdA;
            float3 _LightmapLightBakeIdB;
            float3 _LightmapLightBakeIdC;
            float3 _LightmapLightBakeIdD;
            float3 _LightmapLightBakeIdE;
            float3 _LightmapLightBakeIdF;

            // DIFFUSE
            float _BothSidesDiffuseMultiplier;
            // --

            // SPECULAR
            float _SpecularIntensity;
            // --

            float3 _SphericalNormalOffsetCenter;
            float _SphericalNormalOffsetIntensity;

            // VERTEXDISPLACEMENT_MASK
            #if defined(VERTEXDISPLACEMENT_MASK)
            #if defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE)
            sampler3D _VertexDisplacement3DTexture;
            float3 _VertexDisplacement3DTexOffset;
            float3 _VertexDisplacement3DTexPanning;
            float _VertexDisplacement3DTexScale;
            #elif !defined(_VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE)
            sampler2D _VertexDisplacementMask;
            float4 _VertexDisplacementMask_ST;
            float2 _VertexDisplacementMaskSpeed;
            #endif
            float _VertexDisplacementMaskMode;
            float _VertexDisplacementMaskMultiplier;
            float _VertexDisplacementMaskOffset;

            #endif
            // --

            #if defined(_SPECTROGRAM_FULL) && !defined(_SPECTROGRAM_FLAT)
            float _SpectrogramData[64];
            #endif

            // RIM_DIM
            float _RimScale;
            float _RimOffset;
            float _RimDistanceOffset;
            float _RimDistanceScale;
            float _RimSmoothness;
            float _RimDarkening;
            float _RimLightEdgeStart;
            float _RimLightIntensity;
            float _RimLightBloomIntensity;
            float3 _RimPerpendicularAxis;
            float _RimLightWhiteboostMultiplier;
            float _ColorFogMultiplier;
            float _ColorFogHighlightMultiplier;
            float _ColorFogInfluence;
            float _ColorFogMax;
            // --

            // DISTORTION_SIMPLE
            sampler2D _DistortionTex;
            float4 _DistortionTex_ST;
            float2 _DistortionPanning;
            float2 _DistortionAxes;
            // --

            // PARALLAX_IRIDESCENCE
            sampler2D _ParallaxMap;
            float4 _ParallaxMap_ST;
            float2 _ParallaxTexSpeed;
            float _ParallaxIntensity;
            float _ParallaxIntensity_Step;
            float _StartOffset;
            float _OffsetStep;
            float _Layers;
            float _IridescenceTiling;
            float3 _IridescenceAxesMultiplier;
            // _EMISSION_TEXTURE_SOURCE_SDF
            float4 _SDFPointArray[3];
            float3 _SDFNoisePanning;
            float3 _SDFNoiseOffset;
            float _SDFPointIntensity;
            float _SDFNegativeIntensity;
            sampler3D _SDFNoiseTex;
            #if defined(TEXTURE3D_LOOKUP)
            sampler3D _LookupTex;
            float3 _LookupGridSize;
            float4 _LookupXDisplacementMapping;
            float4 _LookupYDisplacementMapping;
            float4 _LookupZDisplacementMapping;
            float4 _LookupRadialDisplacementMapping;
            float4 _LookupScaleMapping;
            float4 _LookupRotationMapping;
            float4 _LookupEmissiveMapping;
            float _LookupXYZDisplacementScale;
            float _LookupRadialDisplacementScale;
            float _LookupMaxScale;
            float _LookupRotationMultiplier;
            float _LookupEmissiveModulationStrength;
            #endif
            // --

            // _PARALLAX_MASKING_TEXTURE
            sampler2D _ParallaxMaskingMap;
            float4 _ParallaxMaskingMap_ST;
            float2 _ParallaxMaskSpeed;
            float _ParallaxMaskIntensity;
            // --
            // --

            // GROUND_FADE
            float _GroundFadeScale;
            float _GroundFadeOffset;
            // --

            // BLOOM_FOG && FOG
            float _FogStartOffset;
            float _FogScale;
            // HEIGHT_FOG
            float _FogHeightOffset;
            float _FogHeightScale;
            float _FogSoften;
            float _FogSoftenOffset;
            // --
            // USE_FOG_SUPPRESSION
            float _EmissionFogSuppression;
            float _MainEffectFogSuppression;
            // --
            // --

            // DISTANCE_DARKENING
            float _DarkeningScale;
            float _DarkeningIntensity;
            float3 _DarkeningCenter;
            float3 _DarkeningDirection;

            // DISSOLVE
            #if defined(DISSOLVE) || defined(DISSOLVE_TEXTURE)
            float3 _DissolveAxisVector;
            float _DissolveOffset;
            float _DissolveStartValue;
            float _DissolveEndValue;
            float _DissolveReverse;
            float _CutColorFalloff;
            float _CutColorBacksideFalloff;
            float _DissolveColorIntensity;
            sampler2D _DissolveTexture;
            float4 _DissolveTexture_ST;
            float2 _DissolveTextureSpeed;
            float _DissolveTextureInfluence;
            #endif
            // --

            // COLOR_ARRAY
            float4 _ColorsArray[200];
            #if defined(COLOR_ARRAY)
            float _Intensity;
            float _AlphaMultiplier;
            #endif
            #if defined(UV_COLOR_SEGMENTS)
            float4 _UVColors[10];
            float4 _UVRimColors[10];
            #endif
            #if defined(HIGHLIGHT_SELECTION)
            int _SegmentToHighlight;
            #endif


            // Shared includes
            #include "UnityCG.cginc"
            #include "ShaderLibrary/Core/Data.hlsl"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Common/Time.hlsl"
            #include "ShaderLibrary/Common/Lighting.hlsl"
            #include "ShaderLibrary/Common/Reflection.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"

            // Instancing declarations
            // Props supports per-renderer MPB overrides. UNITY_ACCESS_INSTANCED_PROP
            // provides the direct-name fallback when instancing is off.
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _NominalDiffuseLevel)
                UNITY_DEFINE_INSTANCED_PROP(float, _EmissionBrightness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTexColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _EmissionGradientPosition)
                UNITY_DEFINE_INSTANCED_PROP(float, _EmissionMaskIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _SecondaryEmissionMaskIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstancedSecondaryTiling)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstancedSecondaryOffset)
                #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AdditiveUVOffset)
                #endif
                #if !USE_UNIFORM_PRIVATE_POINT_COLOR
                UNITY_DEFINE_INSTANCED_PROP(float4, _PrivatePointLightColor)
                #endif
                UNITY_DEFINE_INSTANCED_PROP(float, _OcclusionDetailIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _TimeOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _MeshPackingId)
                #if defined(_EMISSIONTEXTURE_FLIPBOOK)
                UNITY_DEFINE_INSTANCED_PROP(float, _StartTime)
                #endif
                UNITY_DEFINE_INSTANCED_PROP(float4, _DisplacementAxisMultiplier)
                UNITY_DEFINE_INSTANCED_PROP(float, _DisplacementStrength)
                #if defined(_SPECTROGRAM_FLAT)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpectrogramData)
                #endif
                UNITY_DEFINE_INSTANCED_PROP(float, _EmissionGradientIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _SDFNoiseIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _SDFNoiseScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ParallaxColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _IridescenceColorInfluence)
                UNITY_DEFINE_INSTANCED_PROP(float, _DissolveProgress)
                UNITY_DEFINE_INSTANCED_PROP(float4, _DissolveColor)
                #if defined(_RIMLIGHT_LERP) || defined(_RIMLIGHT_ADDITIVE)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RimLightColor)
                #endif
                #if defined(COLOR_ARRAY)
                UNITY_DEFINE_INSTANCED_PROP(float, _ColorsArrayOffset)
                #endif
                #if defined(TEXTURE3D_LOOKUP)
                UNITY_DEFINE_INSTANCED_PROP(float4, _LookupGridElementIndex)
                UNITY_DEFINE_INSTANCED_PROP(float4, _LookupGridObjectSpacePivot)
                #endif
                #if defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY)
                UNITY_DEFINE_INSTANCED_PROP(float4, _HologramColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _HologramFill)
                UNITY_DEFINE_INSTANCED_PROP(float, _HologramPhaseOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _HologramStripeSpeed)
                UNITY_DEFINE_INSTANCED_PROP(float, _HaltScan)
                #endif
            UNITY_INSTANCING_BUFFER_END(Props)
            #if USE_UNIFORM_PRIVATE_POINT_COLOR
            float4 _PrivatePointLightColor;
            #endif

            // Lit-local UV and surface helpers
            inline float2 TransformSecondaryUv(SurfaceData surface, float4 texture_ST)
            {
                return surface.uv1 * texture_ST.xy * surface.secondaryUvTiling +
                    texture_ST.zw + surface.secondaryUvOffset;
            }

            inline float2 TransformScrollingSecondaryUv(
                SurfaceData surface, float4 texture_ST, float2 speed, float time)
            {
                float2 scale = texture_ST.xy * surface.secondaryUvTiling;
                return surface.uv1 * scale + texture_ST.zw + surface.secondaryUvOffset +
                    time * speed * scale;
            }

            inline void ResolveSurfaceMaterial(
                inout SurfaceData surface, float4 vertexColor,
                float metallic, float smoothness, float2 inputUvMultiplier,
                sampler2D metalSmoothnessTex, float4 metalSmoothnessTex_ST,
                sampler2D dirtTex, float4 dirtTex_ST, float occlusionIntensity,
                sampler2D dirtDetailTex, float4 dirtDetailTex_ST,
                float occlusionDetailIntensity)
            {
                #if defined(_VERTEXMODE_METALSMOOTHNESS)
                surface.metallic = vertexColor.r * metallic;
                surface.smoothness = vertexColor.a * smoothness;
                #elif defined(_VERTEXMODE_SPECIAL)
                surface.metallic = vertexColor.r;
                surface.smoothness = vertexColor.a;
                #endif

                #if defined(METAL_SMOOTHNESS_TEXTURE)
                #if defined(SECONDARY_UVS_MPM) && USE_SECONDARY_UV
                float2 mpmUv = TransformSecondaryUv(surface, metalSmoothnessTex_ST);
                #else
                float2 mpmBaseUv = surface.uv0 * inputUvMultiplier;
                float2 mpmUv = mpmBaseUv * metalSmoothnessTex_ST.xy + metalSmoothnessTex_ST.zw;
                #endif
                surface.mpm = tex2D(metalSmoothnessTex, mpmUv);

                #if defined(_METALLIC_TEXTURE_MPM_R)
                surface.metallic = surface.mpm.r * metallic;
                #elif defined(_METALLIC_TEXTURE_SOURCE_MPM_R)
                surface.metallic = surface.mpm.r;
                #elif defined(_METALLIC_TEXTURE_SOURCE_MPM_A)
                surface.metallic = surface.mpm.a;
                #endif

                #if defined(_SMOOTHNESS_TEXTURE_MPM_A)
                surface.smoothness = surface.mpm.a * smoothness;
                #elif defined(_SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS)
                surface.smoothness = (1.0 - surface.mpm.g) * smoothness;
                #elif defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_A)
                surface.smoothness = surface.mpm.a;
                #elif defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS)
                surface.smoothness = surface.mpm.g;
                #endif
                #endif

                #if defined(OCCLUSION)
                #if defined(_OCCLUSION_SOURCE_MPM_B) && defined(METAL_SMOOTHNESS_TEXTURE)
                float primaryOcclusionSample = surface.mpm.b;
                #else
                #if defined(SECONDARY_UVS_OCCLUSION) && USE_SECONDARY_UV
                float2 occlusionUv = TransformSecondaryUv(surface, dirtTex_ST);
                #else
                float2 occlusionBaseUv = surface.uv0 * inputUvMultiplier;
                float2 occlusionUv = occlusionBaseUv * dirtTex_ST.xy + dirtTex_ST.zw;
                #endif
                float primaryOcclusionSample = tex2D(dirtTex, occlusionUv).r;
                #endif
                surface.occlusion = occlusionIntensity * primaryOcclusionSample +
                    (1.0 - occlusionIntensity);
                #endif

                #if defined(OCCLUSION_DETAIL)
                #if defined(SECONDARY_UVS_OCCLUSION_DETAIL) && USE_SECONDARY_UV
                float2 detailUv = TransformSecondaryUv(surface, dirtDetailTex_ST);
                #else
                float2 detailBaseUv = surface.uv0 * inputUvMultiplier;
                #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET) && defined(SECONDARY_UVS_OCCLUSION_DETAIL)
                detailBaseUv += UNITY_ACCESS_INSTANCED_PROP(Props, _AdditiveUVOffset).xy;
                #endif
                float2 detailUv = detailBaseUv * dirtDetailTex_ST.xy + dirtDetailTex_ST.zw;
                #endif
                surface.occlusionDetail = occlusionDetailIntensity *
                    tex2D(dirtDetailTex, detailUv).r + (1.0 - occlusionDetailIntensity);
                #endif
            }

            // Lit-local parallax helpers
            // _PARALLAX_FLEXIBLE_REFLECTED selects the reflected-direction variant.
            inline float4 ApplyParallax(
                float4 result, SurfaceData surface, float4 vertexColor,
                float2 inputUvMultiplier, float timeOffset, float2 parallaxTexSpeed,
                float parallaxIntensity, float parallaxIntensityStep,
                float layers, float startOffset, float offsetStep,
                float iridescenceColorInfluence,
                sampler2D parallaxTex, float4 parallaxTex_ST,
                sampler2D parallaxMaskingTex, float4 parallaxMaskingTex_ST,
                float parallaxMaskSpeed, float parallaxMaskIntensity,
                float3 iridescenceAxesMultiplier, float iridescenceTiling,
                float4 parallaxColor)
            {
                float2 baseUv = surface.uv0 * inputUvMultiplier;
                float4 timeValue = GetTime(timeOffset);
                float3 cameraPosition = GetStereoAwareCameraPosition();
                float3 directionToCamera = normalize(surface.worldPosition - cameraPosition);
                #if defined(_PARALLAX_FLEXIBLE_REFLECTED)
                float3 parallaxDirection = directionToCamera -
                    2.0 * dot(directionToCamera, surface.normalWS) * surface.normalWS;
                #else
                float3 parallaxDirection = directionToCamera;
                #endif

                float3 hueShift;
                {
                    #if defined(_PARALLAX_FLEXIBLE_REFLECTED)
                    float3 iridescenceDirection = directionToCamera -
                        2.0 * dot(directionToCamera, surface.normalWS) * surface.normalWS;
                    #else
                    float3 iridescenceDirection = directionToCamera;
                    #endif

                    #if defined(PARALLAX_IRIDESCENCE)
                    float iridescenceDot = dot(iridescenceDirection, iridescenceAxesMultiplier);
                    iridescenceDot = frac(iridescenceDot * iridescenceTiling);
                    hueShift = iridescenceDot.xxx * 6.0 + float3(0.0, 4.0, 2.0);
                    hueShift = frac(hueShift * (1.0 / 6.0)) * 6.0 - 3.0;
                    hueShift = saturate(abs(hueShift) - 1.0);
                    float3 hueShiftSquared = hueShift * hueShift;
                    hueShift = (-hueShift * 2.0 + 3.0) * hueShiftSquared;
                    #else
                    hueShift = float3(1.0, 1.0, 1.0);
                    #endif
                }

                #if defined(SECONDARY_UVS_PARALLAX) && USE_SECONDARY_UV
                float2 parallaxUv = TransformSecondaryUv(surface, parallaxTex_ST);
                #else
                float2 parallaxUv = baseUv * parallaxTex_ST.xy + parallaxTex_ST.zw;
                #endif
                parallaxUv += timeValue.x * parallaxTexSpeed * parallaxTex_ST.xy;

                float3 layerColor = float3(0.0, 0.0, 0.0);
                for (float layer = 0.0; layer < layers; layer += 1.0)
                {
                    float layerIndex = floor(layer);
                    float offset = offsetStep * layerIndex + startOffset;
                    float2 sampleUv = offset.xx * parallaxDirection.xy + parallaxUv;
                    float4 parallaxSample = tex2D(parallaxTex, sampleUv);

                    float3 layerIridescence;
                    if (layerIndex <= 0.1) layerIridescence = hueShift.xyz;
                    else if (layerIndex <= 1.1) layerIridescence = hueShift.yzx;
                    else if (layerIndex <= 2.1) layerIridescence = hueShift.zyx;
                    else if (layerIndex <= 3.1) layerIridescence = hueShift.xzy;
                    else if (layerIndex <= 4.1) layerIridescence = hueShift.yxz;
                    else layerIridescence = hueShift.zxy;

                    float intensity = (parallaxIntensityStep * layerIndex + parallaxIntensity) *
                        parallaxSample.x;
                    layerColor += intensity * layerIridescence;
                }
                #if defined(_PARALLAX_MASKING_VERTEX_COLOR)
                layerColor *= vertexColor.g;
                #elif defined(_PARALLAX_MASKING_TEXTURE)
                float4 maskSample = tex2D(
                    parallaxMaskingTex,
                    TRANSFORM_TEX(baseUv, parallaxMaskingTex) + parallaxMaskSpeed * timeValue.y);
                layerColor = lerp(layerColor, layerColor * maskSample.r, parallaxMaskIntensity);
                #endif
                #if defined(PARALLAX_IRIDESCENCE)
                float grayscaleLayer = (layerColor.r + layerColor.g + layerColor.b) * 0.5;
                float3 blended = iridescenceColorInfluence.xxx *
                    (grayscaleLayer.xxx * parallaxColor.rgb - layerColor) + layerColor;
                result.rgb += blended * parallaxColor.a;
                #else
                result.rgb += layerColor * parallaxColor.rgb;
                #endif
                return result;
            }

            // Lit-local reflection helpers
            inline float CalculateLitReflectionTextureRimDim(
                float3 worldPosition, float rimFactor,
                float rimDistanceOffset, float rimDistanceScale, float rimScale)
            {
                float3 cameraPosition = GetStereoAwareCameraPosition();
                float cameraDistance = length(worldPosition - cameraPosition);
                float rimDistance = max(cameraDistance - rimDistanceOffset, 0.0) *
                    rimDistanceScale + rimScale;
                return rimDistance * rimFactor;
            }

            // Lit-local emission helpers
            inline EmissionData InitializeEmissionData()
            {
                EmissionData emission;
                emission.color = 0.0;
                emission.bloomAlpha = 0.0;
                return emission;
            }

            inline float4 ResolveTime(float timeOffset)
            {
                return GetTime(timeOffset);
            }

            inline EmissionData ResolveVertexEmission(
                float4 vertexColor, float4 emissionColor,
                float emissionThreshold, float emissionStrength,
                float baseColorBoost, float baseColorBoostThreshold,
                float questWhiteboostMultiplier, float emissionBloomIntensity)
            {
                float threshold = saturate((vertexColor.g - emissionThreshold) /
                    (1.0 - emissionThreshold));
                threshold = threshold * threshold * (3.0 - 2.0 * threshold) * emissionStrength;
                EmissionData emission = InitializeEmissionData();
                #if defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) || \
                    (defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM))
                float4 weightedEmissionColor = emissionColor.a * emissionColor;
                float whiteBoost = CalculateWhiteBoost(
                    threshold * weightedEmissionColor.a * vertexColor.a, 1.0,
                    baseColorBoost, baseColorBoostThreshold);
                emission.color = saturate(weightedEmissionColor.rgb * threshold + whiteBoost) *
                    questWhiteboostMultiplier;
                #else
                emission.color = emissionColor.rgb * emissionColor.a * threshold;
                #endif
                #if defined(_VERTEXMODE_EMISSIVE_MULT_ADD)
                emission.bloomAlpha = vertexColor.g * vertexColor.g * emissionColor.a *
                    emissionBloomIntensity;
                #else
                emission.bloomAlpha = vertexColor.a * vertexColor.a * emissionColor.a *
                    emissionBloomIntensity;
                #endif
                return emission;
            }

            inline EmissionData ResolvePlainEmission(
                float2 emissionInput, float4 emissionColor, float emissionTexBloomIntensity)
            {
                EmissionData emission = InitializeEmissionData();
                emission.color = emissionInput.r * emissionColor.rgb * emissionColor.a;
                emission.bloomAlpha = emissionInput.g * emissionInput.g * emissionColor.a *
                    3.5 * emissionTexBloomIntensity;
                return emission;
            }

            // Lit-local hologram helper
            inline float4 ApplyHologram(
                float4 result, float3 worldPosition, float3 objectPosition, float4 timeValue,
                float gridSize, float scanDistance,
                float holoIntensity, float haltScan, float stripeSpeed, float phaseOffset,
                float fill, float3 hologramColor)
            {
                #if defined(_HOLOGRAM_GRID)
                {
                    float time = timeValue.w;
                    time = haltScan > 0.5 ? 0.0 : time;

                    float3 gridPhase = time * float3(0.0, stripeSpeed, stripeSpeed * 0.5);
                    float3 cameraPosition = GetStereoAwareCameraPosition();
                    float cameraDistance = length(worldPosition - cameraPosition);
                    float distanceFactor = saturate(cameraDistance * asfloat(0x3e088889));
                    distanceFactor = 1.0 - (1.0 - distanceFactor) * (1.0 - distanceFactor);
                    float resolvedGridSize = gridSize - distanceFactor * 10.0;
                    float colorScale = 1.0 - distanceFactor * 0.6;

                    float3 gridPosition = float3(abs(objectPosition.x), objectPosition.yz);
                    float3 gridWave = cos(frac(-gridPosition * resolvedGridSize - gridPhase) +
                        fill);
                    float grid = gridWave.x * gridWave.y * gridWave.z;

                    float scanPosition = (worldPosition.y - unity_ObjectToWorld._m13 +
                        phaseOffset * scanDistance) / scanDistance;
                    float scan = frac(-time * stripeSpeed + scanPosition);
                    float leadingInput = max((asfloat(0x3cccccc0) - scan) * 40.00004, 0.0);
                    float trailingInput = max((0.975 - scan) * -40.0, 0.0);
                    float leading = 1.0 - leadingInput * leadingInput *
                        (3.0 - 2.0 * leadingInput);
                    float trailing = trailingInput * trailingInput *
                        (3.0 - 2.0 * trailingInput);
                    float envelope = (trailing - leading) * 0.25 + 1.25;
                    float scanGrid = saturate((1.0 - scan) * leading + grid);
                    float hologram = envelope - scanGrid;

                    result.rgb += hologram * colorScale * holoIntensity * hologramColor;
                }
                #elif defined(_HOLOGRAM_SCANLINE)
                {
                    float time = timeValue.w;
                    float scanTime = haltScan > 0.5 ? -0.0 : -time * stripeSpeed;
                    float scanPosition = (worldPosition.y - unity_ObjectToWorld._m13 +
                        phaseOffset * scanDistance) / scanDistance;
                    float scan = min(
                        (1.0 - frac(scanTime + scanPosition)) * asfloat(0x3fd55555), 1.0);
                    scan = 1.0 - scan * scan * (3.0 - 2.0 * scan);
                    result.rgb += scan * holoIntensity * hologramColor;
                }
                result.a = 0.0;
                #elif defined(_HOLOGRAM_LEGACY)
                {
                    float time = timeValue.w;
                    float4 relativePosition = worldPosition.yxyz -
                        unity_ObjectToWorld._m13_m03_m13_m23;
                    float4 scaledPosition = relativePosition * gridSize;

                    float3 wavePosition = scaledPosition.yzw -
                        time * float3(0.0, 1.0, 0.0);
                    float3 waves = sin(frac(wavePosition) * asfloat(0x40490fdb)) * 1.2;
                    float pulsePosition = frac((scaledPosition.x * 0.33 + time) * 0.2);
                    pulsePosition = min(pulsePosition * 2.0, 1.0);
                    float pulseEdge = min(pulsePosition * asfloat(0x419ffffe), 1.0);
                    pulseEdge = pulseEdge * pulseEdge * (3.0 - 2.0 * pulseEdge);
                    float pulse = pulseEdge * (1.0 - pulsePosition);

                    float modulation = cos(time * 2.0 + relativePosition.y +
                        relativePosition.z - relativePosition.w * 7.0) * 0.4 + 0.8;
                    float hologram =
                        (waves.x * waves.y * waves.z * modulation + pulse) * pulse;
                    result.rgb += hologram * hologramColor;
                }
                #endif
                return result;
            }

            // Lit-local rim helpers
            inline float CalculateRimLightMask(
                float3 worldPosition, float3 normalWS, float rimLightEdgeStart,
                float3 rimPerpendicularAxis)
            {
                float3 cameraPosition = GetStereoAwareCameraPosition();
                float3 viewDirection = normalize(worldPosition - cameraPosition);
                float rimLight = 1.0 - abs(dot(normalWS, viewDirection));
                rimLight = smoothstep(0.0, 1.0, saturate(
                                          (rimLight - rimLightEdgeStart) / (1.0 - rimLightEdgeStart)));
                #if defined(DIRECTIONAL_RIM)
                float3 directionalAxis = normalize(
                    rimPerpendicularAxis + (dot(rimPerpendicularAxis, rimPerpendicularAxis) < 0.00001
                                                ? float3(0.0, 1.0, 0.0)
                                                : float3(0.0, 0.0, 0.0)));
                rimLight *= 1.0 - abs(dot(normalWS, directionalAxis));
                #endif
                return rimLight;
            }

            inline float3 ResolveRimLightTarget(
                float3 rimColor, float rimScale, float rimLight,
                float rimLightWhiteboostMultiplier,
                float baseColorBoost, float baseColorBoostThreshold)
            {
                float3 target = rimColor * rimScale;
                #if defined(_RIM_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)
                float whiteBoost = CalculateWhiteBoost(
                    rimLight, rimScale, baseColorBoost, baseColorBoostThreshold);
                target = saturate(target + whiteBoost) * rimLightWhiteboostMultiplier;
                #endif
                return target;
            }

            inline float4 ApplyRimLight(
                float4 result, float3 worldPosition, float3 normalWS,
                float rimLightEdgeStart, float4 rimLightColor,
                float rimLightIntensity, float rimLightBloomIntensity,
                float3 rimPerpendicularAxis, float rimLightWhiteboostMultiplier,
                float baseColorBoost, float baseColorBoostThreshold)
            {
                float rimLight = CalculateRimLightMask(
                    worldPosition, normalWS, rimLightEdgeStart, rimPerpendicularAxis);
                float rimLightScale = rimLightColor.a * rimLightIntensity;
                float3 rimTarget = ResolveRimLightTarget(
                    rimLightColor.rgb, rimLightScale, rimLight,
                    rimLightWhiteboostMultiplier, baseColorBoost, baseColorBoostThreshold);
                result.rgb = lerp(result.rgb, rimTarget, rimLight);
                result.a = rimLightScale * rimLight * rimLightBloomIntensity;
                return result;
            }

            // Lit-local height-fog helpers
            inline float4 ApplyHeightFogCurve(float4 result, float exactHeightInput)
            {
                float exactHeightFog = CalculateHeightFogFactor(exactHeightInput);
                return exactHeightFog.xxxx *
                    (float4(0.1, 0.1, 0.1, 0.0) - result) + result;
            }

            inline float4 ApplyLitColorFog(
                float4 result, float3 worldPosition,
                float colorFogMultiplier, float colorFogMax,
                float colorFogHighlightMultiplier, float colorFogInfluence,
                float fogHeightScale, float fogHeightOffset)
            {
                #if defined(FOG_COLOR_HIGHLIGHT)
                float colorFogHighlight = min(
                    asfloat(0x38d1b718u) * colorFogHighlightMultiplier,
                    colorFogMax);
                #else
                float colorFogHighlight = 0.0;
                #endif
                float4 fogTarget = min(
                    float4(0.1, 0.1, 0.1, 0.0) * colorFogMultiplier *
                        (1.0 + colorFogHighlight),
                    colorFogMax);
                float4 colorFogResult = float4(
                    result.rgb * colorFogInfluence + fogTarget.rgb, result.a);
                float exactHeightInput =
                    worldPosition.y * fogHeightScale + fogHeightOffset;
                float exactHeightFog = CalculateHeightFogFactor(exactHeightInput);
                return exactHeightFog.xxxx *
                    (fogTarget - colorFogResult) + colorFogResult;
            }

            // Lit-local vertex-displacement helpers
            #if defined(VERTEXDISPLACEMENT_MASK)
            inline float ComposeVertexDisplacementMask(float displacementScale, float mask)
            {
                // Scalar mode 0 multiplies the displacement scale by the sampled mask.
                return _VertexDisplacementMaskMode == 0.0
                           ? displacementScale * mask
                           : displacementScale + mask;
            }
            #endif

            // Vertex payload and program
            struct appdata
            {
                float4 vertex : POSITION;
                #if USE_VERTEX_COLOR
                float4 color : COLOR;
                #endif
                float2 uv1 : TEXCOORD0;
                #if USE_SECONDARY_UV || defined(COLOR_ARRAY) || defined(LIGHTMAP)
                float2 uv2 : TEXCOORD1;
                #elif USE_RIBBON_SPATIAL_MASK_VERTEX
                float2 displacementUv : TEXCOORD1;
                #endif
                #if defined(_SPECTROGRAM_FULL)
                float2 uv3 : TEXCOORD2;
                #endif
                #if USE_WORLD_NORMAL
                float3 normal : NORMAL;
                #endif
                #if USE_NORMAL_MAP_PAYLOAD
                float4 tangent : TANGENT;
                #endif
                #if defined(MESH_PACKING)
                #if USE_MESH_PACKING_UV1
                float2 packingUv : TEXCOORD1;
                #else
                float2 packingUv : TEXCOORD3;
                #endif
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if USE_VERTEX_COLOR
                float4 color : COLOR0;
                #endif
                #if USE_VERTEX_EMISSION
                float4 emission : COLOR1;
                #endif
                #if USE_SECONDARY_UV
                float4 uv : TEXCOORD0;
                #else
                float2 uv : TEXCOORD0;
                #endif
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                #if USE_WORLD_NORMAL
                float3 worldNormal : TEXCOORD3;
                #endif
                #if USE_NORMAL_MAP_PAYLOAD
                float3 tangentWS : TEXCOORD12;
                float3 bitangentWS : TEXCOORD13;
                #endif
                #if USE_ANTIFLICKER_NORMAL_PAYLOAD
                centroid float3 antiflickerNormal : TEXCOORD15;
                #endif
                #if USE_NOISE_SCREEN_POSITION
                float4 noiseScreenPos : TEXCOORD4;
                #endif
                #if defined(COLOR_ARRAY)
                float2 colorArrayId : TEXCOORD17;
                #endif
                #if defined(EMISSION_ANGLE_DISAPPEAR)
                float emissionAngle : TEXCOORD18;
                #endif
                #if defined(REFLECTION_TEXTURE)
                float3 reflectionTextureDirection : TEXCOORD10;
                float reflectionTextureRimFactor : TEXCOORD14;
                #elif defined(RIM_DIM)
                float rimDim : TEXCOORD14;
                #endif
                #if defined(LIGHTMAP)
                float2 lightmapUv : TEXCOORD11;
                #endif
                #if defined(_EMISSIONTEXTURE_FLIPBOOK)
                float2 flipbookUv : TEXCOORD22;
                float4 flipbookFrameSelector : TEXCOORD16;
                #endif
                #if defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION)
                float lookupEmission : TEXCOORD33;
                #endif
                #if defined(UV_COLOR_SEGMENTS)
                float4 uvSegmentColor : TEXCOORD19;
                float4 uvSegmentRimColor : TEXCOORD20;
                #endif
                #if defined(HIGHLIGHT_SELECTION)
                float highlightSelection : TEXCOORD21;
                #endif
                #if defined(_HOLOGRAM_GRID)
                float3 hologramObjectPosition : TEXCOORD30;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i, uint id : SV_VertexID)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                #if defined(HIGHLIGHT_SELECTION)
                o.highlightSelection = _SegmentToHighlight == 0;
                #endif

                #if defined(MESH_PACKING)
                float exactMeshPackingId = UNITY_ACCESS_INSTANCED_PROP(Props, _MeshPackingId);
                if (abs(i.packingUv.y - exactMeshPackingId) > 0.1)
                    i.vertex = float4(0.0, 0.0, 0.0, 0.0);
                #endif

                #if USE_SPHERE_SDF_3D_VERTEX
                {
                    float timeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
                    float4 undisplacedWorldPosition = mul(unity_ObjectToWorld, i.vertex);
                    float3 displacementUv = _VertexDisplacement3DTexScale *
                        undisplacedWorldPosition.xyz;
                    displacementUv += _VertexDisplacement3DTexPanning * timeOffset * 0.1 +
                        _VertexDisplacement3DTexOffset;
                    float displacementLod = _VertexDisplacement3DTexScale *
                        undisplacedWorldPosition.w;
                    float3 displacementMask = tex3Dlod(
                        _VertexDisplacement3DTexture,
                        float4(displacementUv, displacementLod)).rgb;
                    displacementMask = _VertexDisplacementMaskMultiplier * displacementMask +
                        _VertexDisplacementMaskOffset;
                    float displacementStrength = i.color.b *
                        UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength);
                    float3 displacementAxis =
                        UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementAxisMultiplier).xyz;
                    i.vertex.xyz += displacementMask * displacementStrength * displacementAxis * i.normal;
                }
                #elif USE_RIBBON_SPATIAL_MASK_VERTEX
                {
                    float frozenTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) * 0.05;
                    float2 displacementUv = i.displacementUv * _VertexDisplacementMask_ST.xy +
                        _VertexDisplacementMask_ST.zw;
                    displacementUv += _VertexDisplacementMask_ST.xy *
                        _VertexDisplacementMaskSpeed * frozenTime;
                    float3 displacementMask = tex2Dlod(
                        _VertexDisplacementMask, float4(displacementUv, 0.0, 0.0)).rgb;
                    displacementMask = _VertexDisplacementMaskMultiplier * displacementMask +
                        _VertexDisplacementMaskOffset;
                    float3 displacementDirection = (i.color.rgb * 2.0 - 1.0) *
                        UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementAxisMultiplier).xyz;
                    float displacementStrength =
                        UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength);
                    i.vertex.xyz += displacementMask * displacementStrength * displacementDirection;
                }
                #elif defined(_VERTEXMODE_DISPLACEMENT)
                {
                    float3 dispDir;
                #if defined(DISPLACEMENT_SPATIAL)
                // RGB channels → XYZ displacement direction
                dispDir = i.color.xyz;
                #   if defined(DISPLACEMENT_BIDIRECTIONAL)
                dispDir = dispDir * 2.0 - 1.0;
                #   elif !defined(_SPECTROGRAM_FLAT) && !defined(_SPECTROGRAM_FULL) && !defined(VERTEXDISPLACEMENT_MASK)
                dispDir *= UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength);
                #   endif
                dispDir *= UNITY_ACCESS_INSTANCED_PROP(
                    Props, _DisplacementAxisMultiplier).xyz;
                #else
                // Default: displace along vertex normal, magnitude from blue channel
                dispDir = i.normal * i.color.b;
                #   if defined(DISPLACEMENT_BIDIRECTIONAL)
                dispDir = dispDir * 2.0 - 1.0;
                #   endif
                dispDir *= UNITY_ACCESS_INSTANCED_PROP(
                    Props, _DisplacementAxisMultiplier).xyz;
                #endif

                float spectrogramScale = 1.0;
                #if defined(_SPECTROGRAM_FLAT)
                // SpectrogramRow uploads this scalar through MPB.SetFloat.
                spectrogramScale = UNITY_ACCESS_INSTANCED_PROP(Props, _SpectrogramData);
                #elif defined(_SPECTROGRAM_FULL)
                // ChroMapper's spectrogram producer uploads 64 scalar bins.
                uint bin = min((uint)(saturate(i.uv3.x) * 64.0), 63u);
                spectrogramScale = _SpectrogramData[bin];
                #endif

                float _dispScale = UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength) * (spectrogramScale);

                #if defined(VERTEXDISPLACEMENT_MASK)
                { 
                #if defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE)
                // 3D texture mask:
                // sample world-space position scaled/panned/offset into the 3D tex,
                // then multiply+offset the result to get a scalar mask.
                {
                    float4 _timeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
                    float3 _dmCoord = _VertexDisplacement3DTexPanning * _timeOffset.xxx;
                    _dmCoord = _dmCoord * float3(0.1, 0.1, 0.1) + _VertexDisplacement3DTexOffset;
                    // world-space position of unmodified vertex
                    float3 _dmWorldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                    float4 _dmSampCoord = float4(_VertexDisplacement3DTexScale.xxx * _dmWorldPos + _dmCoord, 0.0);
                    float4 _dmSamp = tex3Dlod(_VertexDisplacement3DTexture, _dmSampCoord);
                    float3 _dmVal = _VertexDisplacementMaskMultiplier.xxx * _dmSamp.xyz
                        + _VertexDisplacementMaskOffset.xxx;
                    _dispScale = ComposeVertexDisplacementMask(_dispScale, _dmVal.x);
                }
                #elif defined(_VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE)
                { 
                #if defined(_CUSTOM_TIME_FREEZE)
                float _dmTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) * 0.05;
                #else
                float _dmTime = (_Time.y + UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) * 0.05;
                #endif
                float2 _dmUv = i.uv1.xy * _EmissionTex_ST.xy + _EmissionTex_ST.zw;
                _dmUv += _EmissionTex_ST.xy * _EmissionTexSpeed * _dmTime.xx;
                float _dmSample = tex2Dlod(_EmissionTex, float4(_dmUv, 0.0, 0.0)).r;
                _dispScale = ComposeVertexDisplacementMask(
                    _dispScale,
                    _VertexDisplacementMaskMultiplier * _dmSample + _VertexDisplacementMaskOffset);
                        }
                #else
                // 2D texture mask for the VERTEXDISPLACEMENT_MASK path.
                { 
                #if defined(_CUSTOM_TIME_FREEZE)
                float _dmTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) * 0.05;
                #else
                float _dmTime = (_Time.y + UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) * 0.05;
                #endif
                float2 _dmUv = i.uv1.xy * _VertexDisplacementMask_ST.xy + _VertexDisplacementMask_ST.zw;
                float2 _dmPan = _VertexDisplacementMask_ST.xy * _VertexDisplacementMaskSpeed;
                _dmUv = _dmPan * _dmTime.xx + _dmUv;
                float4 _dmSamp = tex2Dlod(_VertexDisplacementMask, float4(_dmUv, 0, 0));
                float3 _dmVal = _VertexDisplacementMaskMultiplier.xxx * _dmSamp.xyz
                    + _VertexDisplacementMaskOffset.xxx;
                _dispScale = ComposeVertexDisplacementMask(_dispScale, _dmVal.x);
                        }
                #endif
                    }
                #endif

                i.vertex.xyz += _dispScale * dispDir;
                }
                #endif

                #if defined(TEXTURE3D_LOOKUP)
                {
                    float3 lookupIndex = UNITY_ACCESS_INSTANCED_PROP(
                        Props, _LookupGridElementIndex).xyz;
                    float3 lookupUv = (lookupIndex + 0.5) / _LookupGridSize;
                    float4 lookupValue = tex3Dlod(_LookupTex, float4(lookupUv, 0.0));
                    lookupValue = lookupValue * 2.0 - 1.0;

                    float3 radialVector = 0.001.xxx - UNITY_ACCESS_INSTANCED_PROP(
                        Props, _LookupGridObjectSpacePivot).xyz;
                    float radialDistance = length(radialVector);
                    float3 radialDirection = radialVector * rsqrt(dot(radialVector, radialVector));
                    float radialFactor = exp2(log2(max(_LookupRadialDisplacementScale, 0.0)) *
                        dot(lookupValue, _LookupRadialDisplacementMapping));
                    float lookupScale = exp2(log2(max(_LookupMaxScale, 0.0)) *
                        dot(lookupValue, _LookupScaleMapping));
                    float rotation = dot(lookupValue, _LookupRotationMapping) *
                        _LookupRotationMultiplier * 6.28319;
                    float sine = sin(rotation);
                    float cosine = cos(rotation);

                    float3 scaledPosition = lookupScale * i.vertex.xyz;
                    float2 rotatedPosition = float2(
                        cosine * scaledPosition.x - sine * scaledPosition.y,
                        sine * scaledPosition.x + cosine * scaledPosition.y);
                    float2 rotatedNormal = float2(
                        cosine * i.normal.x - sine * i.normal.y,
                        sine * i.normal.x + cosine * i.normal.y);
                    i.vertex.xyz = float3(rotatedPosition, scaledPosition.z);
                    i.normal.xy = rotatedNormal;
                    i.vertex.xyz += radialDistance * (radialFactor - 1.0) * radialDirection;
                    i.vertex.xyz += _LookupXYZDisplacementScale * float3(
                        dot(lookupValue, _LookupXDisplacementMapping),
                        dot(lookupValue, _LookupYDisplacementMapping),
                        dot(lookupValue, _LookupZDisplacementMapping));
                #if defined(TEXTURE3D_EMISSION)
                o.lookupEmission = (1.0 + dot(lookupValue, _LookupEmissiveMapping)) *
                    _LookupEmissiveModulationStrength * 0.5;
                #endif
                }
                #endif

                o.vertex = UnityObjectToClipPos(i.vertex);
                #if USE_VERTEX_COLOR
                o.color = i.color;
                // TODO: wtf does this do
                #if USE_VERTEX_EMISSION
                #if defined(COLOR_ARRAY)
                {
                    float _caIdx = round(i.uv2.x * 10.0 + i.uv2.y +
                        UNITY_ACCESS_INSTANCED_PROP(Props, _ColorsArrayOffset));
                    o.emission = _ColorsArray[_caIdx];
                }
                #else
                o.emission = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
                #endif
                #endif
                #endif

                o.uv.xy = i.uv1.xy;
                #if defined(_HOLOGRAM_GRID)
                o.hologramObjectPosition = i.vertex.xyz;
                #endif
                #if defined(UV_COLOR_SEGMENTS)
                uint uvSegmentIndex = (uint)max(i.uv1.x * 10.0, 0.0);
                o.uvSegmentColor = _UVColors[uvSegmentIndex];
                o.uvSegmentRimColor = _UVRimColors[uvSegmentIndex];
                #endif
                #if defined(HIGHLIGHT_SELECTION)
                #if defined(UV_COLOR_SEGMENTS)
                o.highlightSelection = floor(i.uv1.x * 10.0) == floor(_SegmentToHighlight);
                #endif
                #endif
                #if defined(LIGHTMAP)
                o.lightmapUv = i.uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                #if USE_SECONDARY_UV
                o.uv.zw = i.uv2.xy;
                #if USE_UV_SCALE
                o.uv.zw *= _UVScale.xy;
                #endif
                #endif

                #if defined(_EMISSIONTEXTURE_FLIPBOOK)
                {
                    float totalFrames = trunc(_FlipbookRows * _FlipbookColumns);
                    float loopingFrameCount = trunc(totalFrames - _FlipbookNonloopableFrames);
                    float elapsed = _SongTime.y +
                        UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) -
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime);
                    float frameTime = elapsed * _FlipbookSpeed;
                    float loopingTime = frameTime - _FlipbookNonloopableFrames;
                    float loopingProduct = loopingFrameCount * loopingTime;
                    float signedLoopingFrameCount = loopingProduct >= -loopingProduct
                                                        ? loopingFrameCount
                                                        : -loopingFrameCount;
                    float wrappedFrame = frac(loopingTime / signedLoopingFrameCount) *
                        signedLoopingFrameCount + _FlipbookNonloopableFrames;
                    float frame = frameTime < totalFrames ? frameTime : wrappedFrame;
                    float frameFloor = floor(frame);
                    float columnQuotient = frameFloor / _FlipbookColumns;
                    float columnSign = columnQuotient >= -columnQuotient ? 1.0 : -1.0;
                    float column = frac(abs(columnQuotient)) * columnSign * _FlipbookColumns;
                    float row = _FlipbookRows - 1.0 - floor(frame / _FlipbookColumns);
                    o.flipbookUv = (float2(column, row) + i.uv1.xy) /
                        float2(_FlipbookColumns, _FlipbookRows);

                    float frameFraction = frac(frame);
                    float4 frameSelector = frameFraction < 0.75
                                               ? float4(0.0, 0.0, 1.0, 0.0)
                                               : float4(0.0, 0.0, 0.0, 1.0);
                    frameSelector = frameFraction < 0.5 ? float4(0.0, 1.0, 0.0, 0.0) : frameSelector;
                    frameSelector = frameFraction < 0.25 ? float4(1.0, 0.0, 0.0, 0.0) : frameSelector;
                    o.flipbookFrameSelector = i.uv1.x + i.uv1.y <= 0.01 ? 0.0.xxxx : frameSelector;
                }
                #endif

                #if USE_WORLD_NORMAL
                float3 sourceLocalNormal = i.normal;
                #if defined(USE_SPHERICAL_NORMAL_OFFSET)
                sourceLocalNormal = lerp(
                    sourceLocalNormal,
                    i.vertex.xyz + _SphericalNormalOffsetCenter,
                    _SphericalNormalOffsetIntensity);
                #endif
                #if defined(PRECISE_NORMAL)
                // The precise route normalizes before interpolation and again in
                // the fragment stage.
                o.worldNormal = normalize(UnityObjectToWorldNormal(sourceLocalNormal));
                #else
                o.worldNormal = normalize(UnityObjectToWorldNormal(sourceLocalNormal));
                #endif
                #if USE_NORMAL_MAP_PAYLOAD
                o.tangentWS = normalize(UnityObjectToWorldDir(i.tangent.xyz));
                float tangentSign = i.tangent.w * unity_WorldTransformParams.w;
                o.bitangentWS = cross(o.worldNormal, o.tangentWS) * tangentSign;
                #endif
                #endif
                #if USE_ANTIFLICKER_NORMAL_PAYLOAD
                o.antiflickerNormal = o.worldNormal;
                #endif
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                #if defined(EMISSION_ANGLE_DISAPPEAR) || defined(REFLECTION_TEXTURE) || defined(RIM_DIM)
                float3 cameraPosition = GetStereoAwareCameraPosition();
                #endif
                #if defined(EMISSION_ANGLE_DISAPPEAR)
                float3 emissionViewDirection = normalize(
                    cameraPosition - o.worldPos.xyz);
                float emissionAngleDot = abs(dot(o.worldNormal, emissionViewDirection));
                o.emissionAngle = smoothstep(0.0, 1.0, saturate(
                                                 (emissionAngleDot - 0.05) / (
                                                     _EmissionThresholdAngle
                                                     - 0.05)));
                #endif
                #if defined(REFLECTION_TEXTURE)
                float3 reflectionTextureViewDirection = normalize(
                    cameraPosition - o.worldPos.xyz);
                o.reflectionTextureRimFactor = saturate(
                    _RimOffset + 1.0 - dot(o.worldNormal, reflectionTextureViewDirection));
                o.reflectionTextureDirection = reflect(
                    -reflectionTextureViewDirection, o.worldNormal);
                #elif defined(RIM_DIM)
                float3 rimViewDirection = normalize(cameraPosition - o.worldPos.xyz);
                float rimNormalDot = dot(o.worldNormal, rimViewDirection);
                #if defined(INVERT_RIM_DIM)
                float rimFacing = saturate(rimNormalDot + _RimOffset);
                #else
                float rimFacing = saturate(1.0 + _RimOffset - rimNormalDot);
                #endif
                o.rimDim = rimFacing;
                #endif
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                #if USE_NOISE_SCREEN_POSITION
                o.noiseScreenPos = BuildNoiseScreenPosition(
                    o.screenPos, o.vertex, _GlobalBlueNoiseParams,
                    _GlobalRandomValue, unity_ObjectToWorld._m03_m13);
                #endif
                #if defined(COLOR_ARRAY)
                o.colorArrayId.x = i.uv2.x;
                o.colorArrayId.y = i.uv2.y +
                    UNITY_ACCESS_INSTANCED_PROP(Props, _ColorsArrayOffset);
                #endif

                return o;
            }

            // Fragment program
            #if defined(OVERDRAW_VIEW)
            float _TrueOverdrawOn;
            float _OpaqueOverdrawOn;
            float4 _OverdrawColor;
            #endif

            float4 frag(v2f i, float facing : VFACE) : SV_Target
            {
                #if defined(OVERDRAW_VIEW)
                precise float overdraw = _TrueOverdrawOn * _OpaqueOverdrawOn;
                overdraw *= asfloat(0x3dcccccdu);
                precise float4 overdrawColor = overdraw * _OverdrawColor;
                return overdrawColor;
                #else
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                #if USE_SECONDARY_UV
                float2 uv2 = i.uv.zw;
                #else
                float2 uv2 = i.uv.xy;
                #endif

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #if defined(UV_COLOR_SEGMENTS)
                baseColor = i.uvSegmentColor;
                #endif
                #if defined(COLOR_ARRAY) && !defined(_EMISSIONCOLORTYPE_MAINEFFECT)
                float colorIndex = round(i.colorArrayId.x * 10 + i.colorArrayId.y);
                float4 arrayColor = _ColorsArray[colorIndex];
                baseColor.rgb = arrayColor.rgb * _Intensity;
                baseColor.a = arrayColor.a * _AlphaMultiplier;
                #endif
                #if defined(_VERTEXMODE_COLOR)
                baseColor *= i.color;
                #endif

                // Always start from black: baseColor contributes only via diffuse/ambient,
                // so objects are pitch dark without emission or lights.
                float4 albedo = 0;
                {
                    #if defined(DIFFUSE_TEXTURE)
                    float2 baseUv = i.uv.xy * _InputUvMultiplier;
                    #if defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_R)
                    float2 mpmUv = baseUv * _MetalSmoothnessTex_ST.xy +
                        _MetalSmoothnessTex_ST.zw;
                    baseColor.rgb *= tex2D(_MetalSmoothnessTex, mpmUv).r;
                    #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS)
                    float2 mpmUv = baseUv * _MetalSmoothnessTex_ST.xy +
                        _MetalSmoothnessTex_ST.zw;
                    baseColor.rgb *= tex2D(_MetalSmoothnessTex, mpmUv).a * _Smoothness;
                    #else
                    float2 diffuseUv = baseUv * _DiffuseTex_ST.xy + _DiffuseTex_ST.zw;
                    baseColor.rgb *= tex2D(_DiffuseTex, diffuseUv).rgb;
                    #endif
                    #endif
                }

                float3 worldPos = i.worldPos;

                // DISSOLVE_TEXTURE and DISSOLVE_COLOR are child selectors and remain
                // inert unless the semantic parent DISSOLVE is enabled.
                #if defined(DISSOLVE)
                float dissolveTime = ResolveTime(
                    UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)).y;
                float dissolveFactor;
                {
                    float3 axis = normalize(_DissolveAxisVector);
                #if defined(DISSOLVE_PROGRESS)
                float dissolveProgress = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _DissolveProgress);
                float direction = dissolveProgress < -0.001 ? -1.0 : 1.0;
                axis *= direction;
                float threshold = abs(dissolveProgress) *
                    (_DissolveEndValue - _DissolveStartValue) + _DissolveStartValue;
                #else
                float threshold = _DissolveOffset;
                #endif

                #if defined(_DISSOLVE_SPACE_WORLD_CENTERED)
                float3 localOffset = worldPos - unity_ObjectToWorld._m03_m13_m23;
                #else
                float3 localOffset = worldPos;
                #endif
                float projected = dot(localOffset, axis);
                float dissolveValue = projected - threshold;
                #if defined(DISSOLVE_TEXTURE)
                float2 dissolveUv = i.uv.xy * _DissolveTexture_ST.xy +
                    _DissolveTexture_ST.zw;
                dissolveUv += dissolveTime.xx * _DissolveTextureSpeed *
                    _DissolveTexture_ST.xy;
                float textureOffset = tex2D(_DissolveTexture, dissolveUv).r * 2.0 - 1.0;
                dissolveValue += textureOffset * _DissolveTextureInfluence;
                #endif
                dissolveValue *= (_DissolveReverse > 0.5) ? -1.0 : 1.0;

                if (dissolveValue < 0.0)
                    discard;

                #if defined(DISSOLVE_COLOR)
                float facingMultiplier = facing > 0.0 ? 1.0 : _CutColorBacksideFalloff;
                float edgeFalloff = saturate(
                    -dissolveValue * _CutColorFalloff * facingMultiplier + 1.0);
                edgeFalloff = edgeFalloff * edgeFalloff * edgeFalloff;
                dissolveFactor = edgeFalloff *
                    UNITY_ACCESS_INSTANCED_PROP(Props, _DissolveColor).a;
                #else
                dissolveFactor = 0.0;
                #endif
                }
                #endif
                #if USE_WORLD_NORMAL
                #if defined(PRECISE_NORMAL)
                float3 worldNormal = normalize(i.worldNormal);
                #else
                float3 worldNormal = i.worldNormal;
                #endif
                #else
                float3 worldNormal = 1;
                #endif
                #if USE_NORMAL_MAP_PAYLOAD
                {
                    float2 normalUv = i.uv.xy * _InputUvMultiplier;
                    normalUv = normalUv * _NormalTex_ST.xy + _NormalTex_ST.zw;
                    float4 normalSample = tex2D(_NormalTex, normalUv);
                    float2 normalXY = float2(
                        normalSample.a * normalSample.r, normalSample.g) * 2.0 - 1.0;
                    float normalZ = sqrt(1.0 - min(dot(normalXY, normalXY), 1.0));
                    normalXY *= _NormalScale;
                    worldNormal = normalize(
                        normalXY.x * i.tangentWS + normalXY.y * i.bitangentWS +
                        normalZ * worldNormal);
                }
                #endif

                // Composable lighting boundary. The structures feed the single
                // feature-composed output path in composition order.
                SurfaceData composableSurface = InitializeSurfaceData(
                    worldPos, worldNormal, i.uv.xy, uv2, baseColor,
                    _Metallic, _Smoothness);
                #if defined(_SECONDARY_UVS_IMPORT)
                composableSurface.secondaryUvTiling = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _InstancedSecondaryTiling).xy;
                composableSurface.secondaryUvOffset = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _InstancedSecondaryOffset).xy;
                #elif defined(_SECONDARY_UVS_EXTERNAL_SCALE)
                composableSurface.secondaryUvOffset = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _InstancedSecondaryOffset).xy;
                #endif
                #if defined(LIGHTMAP)
                composableSurface.lightmapUv = i.lightmapUv;
                #endif
                #if USE_VERTEX_COLOR
                ResolveSurfaceMaterial(
                    composableSurface, i.color, _Metallic, _Smoothness,
                    _InputUvMultiplier, _MetalSmoothnessTex,
                    _MetalSmoothnessTex_ST, _DirtTex, _DirtTex_ST,
                    _OcclusionIntensity, _DirtDetailTex, _DirtDetailTex_ST,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _OcclusionDetailIntensity));
                #else
                ResolveSurfaceMaterial(
                    composableSurface, 1.0, _Metallic, _Smoothness,
                    _InputUvMultiplier, _MetalSmoothnessTex,
                    _MetalSmoothnessTex_ST, _DirtTex, _DirtTex_ST,
                    _OcclusionIntensity, _DirtDetailTex, _DirtDetailTex_ST,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _OcclusionDetailIntensity));
                #endif
                float3 nominalDiffuseLevel =
                    UNITY_ACCESS_INSTANCED_PROP(Props, _NominalDiffuseLevel).rgb;
                #if !USE_UNIFORM_PRIVATE_POINT_COLOR
                float3 privatePointLightColor =
                    UNITY_ACCESS_INSTANCED_PROP(Props, _PrivatePointLightColor).rgb;
                #else
                float3 privatePointLightColor = _PrivatePointLightColor.rgb;
                #endif
                float3 ambientLight = max(
                    _AmbientMultiplier * nominalDiffuseLevel, _AmbientMinimalValue);
                LightingData composableLighting;
                {
                    composableLighting.directDiffuse = 0.0;
                    composableLighting.directSpecular = 0.0;
                    composableLighting.reflection = 0.0;
                    composableLighting.ambient = 0.0;
                    float3 directBaseColor = composableSurface.baseColor.rgb;
                    float groundFade = 1.0;
                    #if defined(GROUND_FADE)
                    groundFade = 1.0 - saturate(
                        -composableSurface.worldPosition.y * _GroundFadeScale +
                        _GroundFadeOffset);
                    directBaseColor *= groundFade;
                    #endif
                    #if defined(_PROBE_CALCULATION_PRECISE)
                    float minimumColor = min(directBaseColor.r,
                                             min(directBaseColor.g, directBaseColor.b));
                    float maximumColor = max(directBaseColor.r,
                                             max(directBaseColor.g,
                                                 directBaseColor.b));
                    float saturation = (maximumColor - minimumColor) / maximumColor;
                    composableLighting.ambient = directBaseColor * ambientLight *
                        ((saturation + 1.0) * (1.0 - composableSurface.metallic));
                    #else
                    composableLighting.ambient = directBaseColor * ambientLight;
                    #endif

                    float3 directDiffuseNormal = composableSurface.normalWS;
                    #if USE_ANTIFLICKER_NORMAL_PAYLOAD && !USE_NORMAL_MAP_PAYLOAD
                    directDiffuseNormal = dot(worldNormal, worldNormal) >= 1.01
                                              ? i.antiflickerNormal
                                              : worldNormal;
                    #endif
                    float3 diffuseLights = 0.0;
                    #if defined(DIFFUSE)
                    #if defined(LIGHT_FALLOFF)
                    diffuseLights = CalculateLightFalloffDiffuse(
                        composableSurface.worldPosition, directDiffuseNormal);
                    #else
                    diffuseLights = CalculateLightDiffuse(
                        directDiffuseNormal, _BothSidesDiffuseMultiplier);
                    #endif
                    #endif
                    #if defined(PRIVATE_POINT_LIGHT)
                    { 
                    #if defined(POINT_LIGHT_IS_LOCAL)
                    float3 lightPosition = mul(
                        unity_ObjectToWorld,
                        float4(_PrivatePointLightPosition.xyz, 1.0)).xyz;
                    #else
                    float3 lightPosition = _PrivatePointLightPosition.xyz;
                    #endif
                    float3 lightVector = lightPosition - composableSurface.worldPosition;
                    float distanceSquared = max(dot(lightVector, lightVector), 0.00001);
                    float3 lightDirection = lightVector / sqrt(distanceSquared);
                    #if defined(BOTH_SIDES_DIFFUSE)
                    float diffuse = abs(dot(directDiffuseNormal, lightDirection));
                    #else
                    float diffuse = max(dot(directDiffuseNormal, lightDirection), 0.0);
                    #endif
                    diffuseLights += diffuse * privatePointLightColor *
                        _PrivatePointLightIntensity / distanceSquared;
                    }
                    #endif

                    float3 directColor = diffuseLights * directBaseColor;
                    #if defined(DIFFUSE) && defined(SPECULAR)
                    composableLighting.directDiffuse = directColor *
                        (0.96 * (1.0 - composableSurface.metallic));
                    #else
                    composableLighting.directDiffuse = directColor *
                        (1.0 - composableSurface.metallic);
                    #endif
                    #if defined(SPECULAR)
                    #if defined(DIFFUSE)
                    float3 specularColor = 0.04 + composableSurface.metallic *
                        (directColor - 0.04);
                    #else
                    float3 specularColor = 0.04 + composableSurface.metallic *
                        (directBaseColor - 0.04);
                    #endif
                    #if defined(LIGHT_FALLOFF)
                    float3 specularLights = CalculateLightFalloffSpecular(
                        composableSurface.worldPosition, composableSurface.normalWS,
                        composableSurface.smoothness);
                    #else
                    float3 specularLights = CalculateLightSpecular(
                        composableSurface.worldPosition, composableSurface.normalWS,
                        composableSurface.smoothness);
                    #endif
                    composableLighting.directSpecular = specularLights * specularColor *
                        (_SpecularIntensity * groundFade);
                    #if defined(OCCLUSION)
                    composableLighting.directSpecular *= composableSurface.occlusion;
                    #endif
                    #endif
                    #if defined(LIGHTMAP)
                    float3 lightmap1 = tex2D(_LightMap1, composableSurface.lightmapUv).rgb;
                    float3 lightmap2 = tex2D(_LightMap2, composableSurface.lightmapUv).rgb;
                    float3 decodedLightmap =
                        lightmap1.r * _LightmapLightBakeIdA +
                        lightmap1.g * _LightmapLightBakeIdB +
                        lightmap1.b * _LightmapLightBakeIdC +
                        lightmap2.r * _LightmapLightBakeIdD +
                        lightmap2.g * _LightmapLightBakeIdE +
                        lightmap2.b * _LightmapLightBakeIdF;
                    float3 lightmapBaseColor =
                        (1.0 - composableSurface.metallic) * directBaseColor;
                    composableLighting.directDiffuse +=
                        (decodedLightmap * 4.5947933) * lightmapBaseColor;
                    #endif
                }
                float3 composableReflectionNormal = worldNormal;
                #if USE_ANTIFLICKER_NORMAL_PAYLOAD
                composableReflectionNormal = dot(worldNormal, worldNormal) >= 1.01
                                                 ? i.antiflickerNormal
                                                 : worldNormal;
                #endif
                #if defined(REFLECTION_TEXTURE)
                float composableReflectionTextureRimDim =
                    CalculateLitReflectionTextureRimDim(
                        worldPos, i.reflectionTextureRimFactor,
                        _RimDistanceOffset, _RimDistanceScale, _RimScale);
                {
                    float smoothness = composableSurface.smoothness;
                #if defined(RIM_DIM)
                smoothness = saturate(
                    smoothness - composableReflectionTextureRimDim * _RimSmoothness);
                #endif

                float roughness = 1.0 - smoothness;
                float reflectionLod = roughness * (1.7 - 0.7 * roughness) * 6.0;
                float3 reflection = texCUBElod(
                    _EnvironmentReflectionCube,
                    float4(i.reflectionTextureDirection, reflectionLod)).rgb;
                reflection *= _ReflectionTexIntensity;
                #if defined(MULTIPLY_REFLECTIONS)
                reflection *= 1.0 + composableSurface.metallic *
                    (composableSurface.baseColor.rgb - 1.0);
                #endif
                reflection *= 2.0 * (composableSurface.metallic * 0.8 + 0.2);
                reflection *= smoothness;
                #if defined(RIM_DIM)
                reflection *= 1.0 - composableReflectionTextureRimDim * _RimDarkening;
                #endif
                composableLighting.reflection = reflection;
                }
                #else
                #if defined(RIM_DIM)
                float composableRimDim = CalculateLitReflectionTextureRimDim(
                    worldPos, i.rimDim,
                    _RimDistanceOffset, _RimDistanceScale, _RimScale);
                #else
                float composableRimDim = 0.0;
                #endif
                #if !defined(REFLECTION_PROBE)
                composableLighting.reflection = 0.0;
                #else
                {
                    float smoothness = composableSurface.smoothness;
                #if defined(RIM_DIM)
                smoothness = saturate(smoothness - composableRimDim * _RimSmoothness);
                #endif
                #if defined(SPECULAR_ANTIFLICKER)
                float3 cameraPosition = GetStereoAwareCameraPosition();
                float cameraDistance = length(
                    composableSurface.worldPosition - cameraPosition);
                float weight = saturate(
                        (_AntiflickerDistanceOffset - cameraDistance) *
                        _AntiflickerDistanceScale) *
                    _AntiflickerStrength;
                float3 normalDx = ddx(composableReflectionNormal);
                float3 normalDy = ddy(composableReflectionNormal);
                float gradient = min(
                    max(dot(normalDx, normalDx), dot(normalDy, normalDy)), 1.0);
                float filteredSmoothness = min(
                    1.0 - pow(gradient, 0.333), smoothness);
                smoothness += weight * (filteredSmoothness - smoothness);
                #endif
                #if defined(REFLECTION_STATIC)
                float3 reflectionDirection =
                    composableSurface.worldPosition + composableReflectionNormal;
                #else
                float3 reflectionDirection = CalculateViewReflectionDirection(
                    composableSurface.worldPosition, composableReflectionNormal);
                #endif

                #if defined(REFLECTION_PROBE_BOX_PROJECTION)
                float3 boundsMin = _ReflectionProbeBoundsMin;
                float3 boundsMax = _ReflectionProbeBoundsMax;
                float3 probePosition = _ReflectionProbePosition;
                #if defined(REFLECTION_PROBE_BOX_PROJECTION_OFFSET)
                boundsMin -= _ReflectionProbeBoxProjectionSizeOffset;
                boundsMax += _ReflectionProbeBoxProjectionSizeOffset;
                probePosition += _ReflectionProbeBoxProjectionPositionOffset;
                #endif
                reflectionDirection = BoxProjectReflectionDirection(
                    reflectionDirection, composableSurface.worldPosition,
                    boundsMin, boundsMax, probePosition);
                #endif

                float3 reflection = SampleReflectionProbePair(
                    reflectionDirection, smoothness,
                    _ReflectionProbeTexture1, _ReflectionProbeTexture2,
                    _LightProbeLightBakeIdA, _LightProbeLightBakeIdB,
                    _LightProbeLightBakeIdC, _LightProbeLightBakeIdD,
                    _LightProbeLightBakeIdE, _LightProbeLightBakeIdF,
                    _ReflectionProbeIntensity);
                #if defined(_PROBE_CALCULATION_PRECISE)
                float grayscale = dot(float3(0.33, 0.33, 0.33), reflection);
                float metallicScale = composableSurface.metallic *
                    composableSurface.metallic * 2.5 + 1.0;
                float3 grayscaleDelta = grayscale * metallicScale - reflection;
                float scaledGrayscale = grayscale * metallicScale;
                float minimumColor = min(
                    composableSurface.baseColor.r,
                    min(composableSurface.baseColor.g, composableSurface.baseColor.b));
                float maximumColor = max(
                    composableSurface.baseColor.r,
                    max(composableSurface.baseColor.g, composableSurface.baseColor.b));
                float saturation = (maximumColor - minimumColor) / maximumColor;
                float metallicSaturation = saturation * composableSurface.metallic;
                float grayscaleFactor = max(
                    metallicSaturation, _ReflectionProbeGrayscale);
                float coloredMetalScale =
                    metallicSaturation * _ColoredMetalMultiplier + 1.0;
                reflection += grayscaleFactor * grayscaleDelta;
                float metallicFactor = saturate(
                    composableSurface.metallic -
                    scaledGrayscale * scaledGrayscale * 0.1);
                metallicFactor *= max(saturation, 0.95);
                reflection *= 1.0 + metallicFactor *
                    (composableSurface.baseColor.rgb * coloredMetalScale - 1.0);
                float whiteFactor = (1.0 - saturation) *
                    _WhiteOffset * max(_ColoredMetalMultiplier, 1.0);
                reflection *= max(
                    whiteFactor * composableSurface.baseColor.rgb *
                    composableSurface.metallic, 1.0);
                reflection *= smoothness;
                #if defined(RIM_DIM)
                reflection *= 1.0 - composableRimDim * _RimDarkening;
                #endif
                composableLighting.reflection = reflection;
                #else
                float groundFade = 1.0;
                float3 reflectionBaseColor = composableSurface.baseColor.rgb;
                #if defined(GROUND_FADE)
                groundFade = 1.0 - saturate(
                    -composableSurface.worldPosition.y * _GroundFadeScale +
                    _GroundFadeOffset);
                reflectionBaseColor *= groundFade;
                #endif
                #if defined(MULTIPLY_REFLECTIONS)
                reflection *= 1.0 + composableSurface.metallic *
                    (reflectionBaseColor - 1.0);
                #endif
                reflection *= 2.0 * (composableSurface.metallic * 0.8 + 0.2);
                reflection *= smoothness * groundFade;
                #if defined(RIM_DIM)
                reflection *= 1.0 - composableRimDim * _RimDarkening;
                #endif
                composableLighting.reflection = reflection;
                #endif
                }
                #endif
                #endif
                EmissionData composableEmission = InitializeEmissionData();
                float4 composableTime = ResolveTime(
                    UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset));

                albedo += float4(
                    composableLighting.reflection + composableLighting.ambient +
                    (composableLighting.directDiffuse + composableLighting.directSpecular),
                    0.0);

                #if defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY)
                #if defined(_HOLOGRAM_GRID)
                albedo = ApplyHologram(
                    albedo, worldPos, i.hologramObjectPosition, composableTime,
                    _HologramGridSize, _HologramScanDistance,
                    _HoloIntensity,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HaltScan),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramStripeSpeed),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramPhaseOffset),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramFill),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramColor));
                #elif defined(_HOLOGRAM_SCANLINE)
                albedo = ApplyHologram(
                    albedo, worldPos, 0.0.xxx, composableTime,
                    0.0, _HologramScanDistance,
                    _HoloIntensity,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HaltScan),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramStripeSpeed),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramPhaseOffset),
                    0.0,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramColor));
                #else
                albedo = ApplyHologram(
                    albedo, worldPos, 0.0.xxx, composableTime,
                    _HologramGridSize, 0.0, 0.0,
                    0.0, 0.0, 0.0, 0.0,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _HologramColor));
                #endif
                #endif

                #if defined(DISTANCE_DARKENING)
                {
                    float3 offset = _DarkeningCenter - worldPos;
                    float weightedDistance = dot(
                        offset * offset,
                        _DarkeningDirection * float3(0.0001, 0.0001, 0.0001));
                    albedo.rgb *= 1.0 - saturate(weightedDistance * _DarkeningScale) *
                        _DarkeningIntensity;
                }
                #endif

                #if defined(OCCLUSION) && defined(OCCLUSION_BEFORE_EMISSION)
                albedo.rgb *= composableSurface.occlusion;
                #endif
                #if defined(OCCLUSION_DETAIL) && defined(OCCLUSION_BEFORE_EMISSION)
                albedo.rgb *= composableSurface.occlusionDetail;
                #endif

                // EMISSION
                #if defined(ACES_TONE_MAPPING) && defined(_ACES_APPROACH_BEFORE_EMISSIVE)
                albedo = ApplyAcesTonemapping(albedo);
                #endif

                #if defined(_PARALLAX_FLEXIBLE) || defined(_PARALLAX_RGB)
                #if USE_VERTEX_COLOR
                albedo = ApplyParallax(
                    albedo, composableSurface, i.color,
                    _InputUvMultiplier, UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset),
                    _ParallaxTexSpeed, _ParallaxIntensity, _ParallaxIntensity_Step,
                    _Layers, _StartOffset, _OffsetStep,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _IridescenceColorInfluence),
                    _ParallaxMap, _ParallaxMap_ST,
                    _ParallaxMaskingMap, _ParallaxMaskingMap_ST,
                    _ParallaxMaskSpeed, _ParallaxMaskIntensity,
                    _IridescenceAxesMultiplier, _IridescenceTiling,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _ParallaxColor));
                #else
                albedo = ApplyParallax(
                    albedo, composableSurface, 1.0.xxxx,
                    _InputUvMultiplier, UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset),
                    _ParallaxTexSpeed, _ParallaxIntensity, _ParallaxIntensity_Step,
                    _Layers, _StartOffset, _OffsetStep,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _IridescenceColorInfluence),
                    _ParallaxMap, _ParallaxMap_ST,
                    _ParallaxMaskingMap, _ParallaxMaskingMap_ST,
                    _ParallaxMaskSpeed, _ParallaxMaskIntensity,
                    _IridescenceAxesMultiplier, _IridescenceTiling,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _ParallaxColor));
                #endif
                #endif

                #if USE_EMISSION_TEXTURE_COLOR
                #if defined(_EMISSIONTEXTURE_FLIPBOOK)
                {
                    float2 emissionUv = i.flipbookUv * _EmissionTex_ST.xy +
                        _EmissionTex_ST.zw;
                    float emissionInput = dot(
                            tex2D(_EmissionTex, emissionUv), i.flipbookFrameSelector) *
                        UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionBrightness);
                    composableEmission = ResolvePlainEmission(
                        emissionInput.xx,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor),
                        _EmissionTexBloomIntensity);
                }
                albedo.rgb += composableEmission.color;
                albedo.a = composableEmission.bloomAlpha;
                #if USE_VERTEX_EMISSION
                EmissionData flipbookVertexEmission = ResolveVertexEmission(
                    i.color, i.emission,
                    _EmissionThreshold, _EmissionStrength,
                    _BaseColorBoost, _BaseColorBoostThreshold,
                    _QuestWhiteboostMultiplier, _EmissionBloomIntensity);
                albedo.rgb += flipbookVertexEmission.color;
                albedo.a += flipbookVertexEmission.bloomAlpha;
                #endif
                #else
                #if defined(COLOR_ARRAY)
                float2 composableColorArrayId = i.colorArrayId;
                #else
                float2 composableColorArrayId = 0.0;
                #endif
                #if defined(EMISSION_ANGLE_DISAPPEAR)
                float composableEmissionAngle = i.emissionAngle;
                #else
                float composableEmissionAngle = 1.0;
                #endif
                #if defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION)
                float composableLookupEmission = i.lookupEmission;
                #else
                float composableLookupEmission = 1.0;
                #endif
                {
                    float2 emissionInput = 0.0;
                #if defined(_EMISSION_TEXTURE_SOURCE_MPM_G) && defined(METAL_SMOOTHNESS_TEXTURE)
                emissionInput = composableSurface.mpm.gg;
                #elif defined(_EMISSION_TEXTURE_SOURCE_SDF)
                {
                    float sdfMask = 1.0;
                    float sdfAccumulator = 0.0;
                    for (int pointIndex = 0; pointIndex < 3; pointIndex++)
                    {
                        float3 pointOffset = composableSurface.worldPosition -
                            _SDFPointArray[pointIndex].xyz;
                        float distanceSquared = dot(pointOffset, pointOffset);
                        float pointDistance = exp2(log2(distanceSquared) * 0.25);
                        float isPositive = _SDFPointArray[pointIndex].w > 0.0
                                               ? 1.0
                                               : 0.0;
                        float isNegative = _SDFPointArray[pointIndex].w < 0.0
                                               ? 1.0
                                               : 0.0;
                        float pointSign = floor(isPositive - isNegative);
                        float pointIntensity = pointSign < 0.0
                                                   ? _SDFNegativeIntensity
                                                   : _SDFPointIntensity;
                        float pointDistanceField = max(
                            abs(_SDFPointArray[pointIndex].w) - pointDistance, 0.0);
                        float accumulated =
                            pointDistanceField * pointIntensity + sdfAccumulator;
                        float occlusion = saturate(
                            -pointDistanceField * pointIntensity + 1.0);
                        float masked = sdfMask * occlusion;
                        if (pointSign >= 0.0)
                            sdfAccumulator = accumulated;
                        else
                            sdfMask = masked;
                    }
                    float sdfNoiseScale = UNITY_ACCESS_INSTANCED_PROP(
                        Props, _SDFNoiseScale);
                    float3 noiseUv = sdfNoiseScale.xxx *
                        composableSurface.worldPosition;
                    noiseUv += _SDFNoisePanning * composableTime.y + _SDFNoiseOffset;
                    float sdfEmission = sdfAccumulator * sdfMask +
                        tex3D(_SDFNoiseTex, noiseUv).r *
                        UNITY_ACCESS_INSTANCED_PROP(Props, _SDFNoiseIntensity);
                    emissionInput = sdfEmission.xx;
                }
                #elif defined(_EMISSIONTEXTURE_PULSE)
                { 
                #if defined(SECONDARY_UVS_PULSE) && USE_SECONDARY_UV
                float2 pulseUv = TransformSecondaryUv(
                    composableSurface, _PulseMask_ST);
                #else
                float2 baseUv = composableSurface.uv0 * _InputUvMultiplier;
                float2 pulseUv = baseUv * _PulseMask_ST.xy + _PulseMask_ST.zw;
                #endif
                float pulseTexture = tex2D(_PulseMask, pulseUv).r;
                #if defined(INVERT_PULSE)
                pulseTexture = 1.0 - pulseTexture;
                #endif
                float pulsePhase = frac(
                    pulseTexture - composableTime.x * _PulseSpeed);
                float pulseDistance = min(pulsePhase, 1.0 - pulsePhase);
                float pulse = 1.0 - smoothstep(
                    max(_PulseWidth, 0.0),
                    max(_PulseWidth + _PulseSmooth, 0.00001),
                    pulseDistance);
                #if defined(PULSE_MULTIPLY_TEXTURE)
                pulse *= pulseTexture;
                #endif
                emissionInput = pulse.xx;
                    }
                #elif defined(_EMISSIONTEXTURE_SIMPLE)
                #if defined(DISTORTION_SIMPLE) && \
                        defined(_DISTORTION_TARGET_EMISSIONTEX)
                { 
                #if defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV
                float2 distortionUv = TransformScrollingSecondaryUv(
                    composableSurface, _DistortionTex_ST,
                    _DistortionPanning * 0.1, composableTime.y);
                #else
                float2 baseUv = composableSurface.uv0 * _InputUvMultiplier;
                float2 distortionUv = baseUv * _DistortionTex_ST.xy +
                    _DistortionTex_ST.zw;
                distortionUv += composableTime.yy * _DistortionPanning *
                    _DistortionTex_ST.xy * 0.1;
                #endif
                float2 distortion = tex2D(_DistortionTex, distortionUv).rg;
                distortion = distortion *
                    UNITY_ACCESS_INSTANCED_PROP(Props, _DistortionStrength) *
                    0.1 * _DistortionAxes * 2.0 - 1.0;
                #if defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV
                float2 emissionUv = TransformScrollingSecondaryUv(
                    composableSurface, _EmissionTex_ST,
                    _EmissionTexSpeed, composableTime.x);
                emissionUv += distortion * _EmissionTex_ST.xy *
                    composableSurface.secondaryUvTiling;
                #else
                float2 emissionUv = (baseUv + distortion) * _EmissionTex_ST.xy +
                    _EmissionTex_ST.zw;
                emissionUv += composableTime.xx * _EmissionTexSpeed *
                    _EmissionTex_ST.xy;
                #endif
                emissionInput = tex2D(_EmissionTex, emissionUv).rg;
                    }
                #else
                { 
                #if defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV
                float2 emissionUv = TransformScrollingSecondaryUv(
                    composableSurface, _EmissionTex_ST,
                    _EmissionTexSpeed, composableTime.x);
                #else
                float2 baseUv = composableSurface.uv0 * _InputUvMultiplier;
                #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET) && defined(SECONDARY_UVS_EMISSION)
                baseUv += UNITY_ACCESS_INSTANCED_PROP(Props, _AdditiveUVOffset).xy;
                #endif
                float2 emissionUv = baseUv * _EmissionTex_ST.xy +
                    _EmissionTex_ST.zw;
                emissionUv += composableTime.xx * _EmissionTexSpeed *
                    _EmissionTex_ST.xy;
                #endif
                emissionInput = tex2D(_EmissionTex, emissionUv).rg;
                    }
                #endif
                #endif

                #if defined(EMISSION_ANGLE_DISAPPEAR)
                emissionInput *= composableEmissionAngle;
                #endif

                #if defined(_EMISSION_ALPHA_SOURCE_COPY_EMISSION)
                emissionInput.y = emissionInput.x;
                #elif defined(_EMISSION_ALPHA_SOURCE_MPM_R) && defined(METAL_SMOOTHNESS_TEXTURE)
                emissionInput.y = composableSurface.mpm.r;
                #endif
                #if defined(EMISSION_MASK)
                { 
                #if defined(SECONDARY_UVS_EMISSION_MASK) && USE_SECONDARY_UV
                float2 maskUv = TransformScrollingSecondaryUv(
                    composableSurface, _EmissionMask_ST,
                    _EmissionMaskSpeed, composableTime.x);
                #else
                float2 maskBaseUv = composableSurface.uv0 * _InputUvMultiplier;
                float2 maskUv = maskBaseUv * _EmissionMask_ST.xy +
                    _EmissionMask_ST.zw;
                maskUv += composableTime.xx * _EmissionMaskSpeed *
                    _EmissionMask_ST.xy;
                #endif
                float2 mask = tex2D(_EmissionMask, maskUv).rg;
                float emissionMaskIntensity = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _EmissionMaskIntensity);
                #if defined(_MASKBLEND_ADD)
                emissionInput += mask * emissionMaskIntensity;
                #elif defined(_MASKBLEND_MASKED_ADD)
                emissionInput += emissionInput * mask * emissionMaskIntensity;
                #else
                emissionInput *= mask * emissionMaskIntensity +
                    (1.0 - emissionMaskIntensity);
                #endif
                    }
                #endif
                #if defined(SECONDARY_EMISSION_MASK)
                { 
                #if defined(SECONDARY_UVS_EMISSION_MASK2) && USE_SECONDARY_UV
                float2 maskUv = TransformScrollingSecondaryUv(
                    composableSurface, _SecondaryEmissionMask_ST,
                    _SecondaryEmissionMaskSpeed, composableTime.x);
                #else
                float2 maskBaseUv = composableSurface.uv0 * _InputUvMultiplier;
                float2 maskUv = maskBaseUv * _SecondaryEmissionMask_ST.xy +
                    _SecondaryEmissionMask_ST.zw;
                maskUv += composableTime.xx * _SecondaryEmissionMaskSpeed *
                    _SecondaryEmissionMask_ST.xy;
                #endif
                float2 mask = tex2D(_SecondaryEmissionMask, maskUv).rg;
                float secondaryEmissionMaskIntensity =
                    UNITY_ACCESS_INSTANCED_PROP(
                        Props, _SecondaryEmissionMaskIntensity);
                #if defined(_SECONDARY_MASK_BLEND_ADD)
                emissionInput += mask * secondaryEmissionMaskIntensity;
                #elif defined(_SECONDARY_MASK_BLEND_MASKED_ADD)
                emissionInput += emissionInput * mask *
                    secondaryEmissionMaskIntensity;
                #else
                emissionInput *= mask * secondaryEmissionMaskIntensity +
                    (1.0 - secondaryEmissionMaskIntensity);
                #endif
                    }
                #endif
                emissionInput *= UNITY_ACCESS_INSTANCED_PROP(
                    Props, _EmissionBrightness);
                #if defined(_VERTEXMODE_EMISSIVE_MULT_ADD)
                emissionInput *= i.color.a;
                #endif

                #if defined(COLOR_ARRAY)
                float emissionColorIndex = round(
                    composableColorArrayId.x * 10.0 + composableColorArrayId.y);
                float4 emissionColor = _ColorsArray[emissionColorIndex];
                #else
                float4 emissionColor = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _EmissionTexColor);
                #endif
                #if defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION)
                emissionColor.a *= composableLookupEmission;
                #endif

                #if defined(_EMISSIONCOLORTYPE_GRADIENT)
                composableEmission = InitializeEmissionData();
                float gradientPhase = frac(
                    _EmissionGradientPanningSpeed * composableTime.x +
                    UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionGradientPosition));
                float2 gradientUv = float2(emissionInput.y, gradientPhase) *
                    _EmissionGradientTex_ST.xy;
                float3 gradient = tex2D(_EmissionGradientTex, gradientUv).rgb;
                float gradientIntensity = UNITY_ACCESS_INSTANCED_PROP(
                    Props, _EmissionGradientIntensity);
                composableEmission.color = gradientIntensity *
                    saturate(emissionInput.x * gradient);
                composableEmission.bloomAlpha =
                    _EmissionTexBloomIntensity * gradientIntensity;
                #elif defined(_EMISSIONCOLORTYPE_WHITEBOOST) || \
                        (defined(_EMISSIONCOLORTYPE_MAINEFFECT) && !defined(POST_BLOOM))
                float3 emissionRgb = emissionInput.r * emissionColor.rgb;
                float bloomValue = emissionInput.g * emissionInput.g * emissionColor.a;
                float bloomAlpha = bloomValue * 3.5 * _EmissionTexBloomIntensity;
                emissionRgb = CalculateBloomComposition(
                    emissionRgb, emissionColor.a, bloomValue,
                    _EmissionTexWhiteBoostMultiplier,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                float4 configured = float4(emissionRgb, bloomAlpha);
                composableEmission = InitializeEmissionData();
                composableEmission.color = configured.rgb;
                composableEmission.bloomAlpha = configured.a;
                #else
                composableEmission = ResolvePlainEmission(
                    emissionInput, emissionColor, _EmissionTexBloomIntensity);
                #endif
                }
                albedo.rgb += composableEmission.color;
                albedo.a += composableEmission.bloomAlpha;
                #endif
                #endif

                #if USE_VERTEX_EMISSION && !defined(_EMISSIONTEXTURE_FLIPBOOK)
                EmissionData composableVertexEmission = ResolveVertexEmission(
                    i.color, i.emission,
                    _EmissionThreshold, _EmissionStrength,
                    _BaseColorBoost, _BaseColorBoostThreshold,
                    _QuestWhiteboostMultiplier, _EmissionBloomIntensity);
                albedo.rgb += composableVertexEmission.color;
                albedo.a += composableVertexEmission.bloomAlpha;
                #endif

                #if defined(_RIMLIGHT_ADDITIVE)
                {
                    float rimLight = CalculateRimLightMask(
                        worldPos, worldNormal, _RimLightEdgeStart,
                        _RimPerpendicularAxis);
                    float4 rimLightColor = UNITY_ACCESS_INSTANCED_PROP(
                        Props, _RimLightColor);
                    float rimLightScale = rimLightColor.a * _RimLightIntensity;
                    float3 rimTarget = ResolveRimLightTarget(
                        rimLightColor.rgb, rimLightScale, rimLight,
                        _RimLightWhiteboostMultiplier,
                        _BaseColorBoost, _BaseColorBoostThreshold);
                    albedo.rgb += rimTarget * rimLight * rimLightScale;
                    albedo.a = rimLight * rimLightScale * _RimLightBloomIntensity;
                }
                #endif

                #if defined(OCCLUSION) && !defined(OCCLUSION_BEFORE_EMISSION)
                albedo *= composableSurface.occlusion;
                #endif
                #if defined(OCCLUSION_DETAIL) && !defined(OCCLUSION_BEFORE_EMISSION)
                albedo *= composableSurface.occlusionDetail;
                #endif

                #if defined(_RIMLIGHT_LERP)
                #if defined(UV_COLOR_SEGMENTS)
                albedo = ApplyRimLight(
                    albedo, worldPos, worldNormal, _RimLightEdgeStart,
                    i.uvSegmentRimColor, _RimLightIntensity,
                    _RimLightBloomIntensity,
                    _RimPerpendicularAxis, _RimLightWhiteboostMultiplier,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #else
                albedo = ApplyRimLight(
                    albedo, worldPos, worldNormal, _RimLightEdgeStart,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _RimLightColor),
                    _RimLightIntensity, _RimLightBloomIntensity,
                    _RimPerpendicularAxis, _RimLightWhiteboostMultiplier,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                #endif

                #if defined(ACES_TONE_MAPPING) && !defined(_ACES_APPROACH_BEFORE_EMISSIVE)
                albedo = ApplyAcesTonemapping(albedo);
                #endif

                #if defined(HIGHLIGHT_SELECTION)
                {
                    float pulse = frac(
                        composableTime.w * 0.15 + worldPos.x * 0.2 + worldPos.y);
                    pulse = max(1.0 - pulse * 5.0, 0.0);
                    pulse = pulse * pulse * (3.0 - 2.0 * pulse);
                    pulse *= saturate(100.0 - 100.0 * pulse);
                    pulse = saturate(0.4 * pulse * pulse * i.highlightSelection);
                    albedo.rgb += pulse;
                }
                #endif

                #if defined(COLOR_BY_FOG) && !(defined(BLOOM_FOG) && defined(FOG)) && \
                    !defined(_HOLOGRAM_GRID) && !defined(_HOLOGRAM_LEGACY)
                albedo = ApplyLitColorFog(
                    albedo, worldPos, _ColorFogMultiplier, _ColorFogMax,
                    _ColorFogHighlightMultiplier, _ColorFogInfluence,
                    _FogHeightScale, _FogHeightOffset);
                #endif

                // Terminal fog composition keeps bloom fog, height fog, and blue-noise
                // dithering in separate passes.
                #if defined(BLOOM_FOG) && defined(FOG)
                #if defined(HEIGHT_FOG)
                float customFogFactor = CalculateCustomFogFactor(
                    distanceSquared(worldPos), _FogStartOffset, _FogScale);
                #if defined(HEIGHT_FOG_DEPTH_SOFTEN)
                float cameraDistance = length(worldPos - GetStereoAwareCameraPosition());
                float heightInput = worldPos.y *
                    (_FogHeightScale / (cameraDistance * _FogSoften * 0.01));
                heightInput += _FogHeightOffset -
                    cameraDistance * _FogSoftenOffset * 0.001;
                #else
                float heightInput = worldPos.y * _FogHeightScale + _FogHeightOffset;
                #endif
                heightInput -=
                    CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME + CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME;
                heightInput = clamp(
                    heightInput / CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME, 0.0, 1.0);
                float customHeightFogFactor =
                    heightInput * heightInput * (3.0 - 2.0 * heightInput);
                #if defined(COLOR_BY_FOG)
                {
                    float3 bloomColor = SampleBloomPrePass(i.screenPos).rgb;
                    float3 colorFog = bloomColor * _ColorFogMultiplier;
                #if defined(FOG_COLOR_HIGHLIGHT)
                float bloomMaximum = max(
                    bloomColor.r, max(bloomColor.g, bloomColor.b));
                float highlight = bloomMaximum * bloomMaximum;
                highlight *= bloomMaximum;
                highlight *= _ColorFogHighlightMultiplier;
                highlight = min(highlight * bloomMaximum, _ColorFogMax);
                colorFog = min(colorFog * (1.0 + highlight), _ColorFogMax);
                #else
                colorFog = min(colorFog, _ColorFogMax);
                #endif

                float4 fogTarget = min(float4(colorFog, 0.0), _ColorFogMax);
                float4 fogSource = float4(
                    albedo.rgb * _ColorFogInfluence + colorFog, albedo.a);
                float blendFactor =
                    1.0 - customHeightFogFactor * (1.0 - customFogFactor);
                albedo = fogSource + blendFactor * (fogTarget - fogSource);
                }
                #else
                albedo = ApplyBloomHeightFogCalculatedFactor(
                    albedo, i.screenPos, customFogFactor, customHeightFogFactor);
                #endif
                #else
                albedo = ApplyBloomFog(
                    albedo, i.screenPos, worldPos, _FogStartOffset, _FogScale);
                #endif
                #elif defined(FOG) && defined(HEIGHT_FOG) && \
                    !defined(COLOR_BY_FOG) && \
                    !defined(_HOLOGRAM_GRID) && !defined(_HOLOGRAM_LEGACY)
                #if defined(HEIGHT_FOG_DEPTH_SOFTEN)
                {
                    float cameraDistance = length(
                        worldPos - GetStereoAwareCameraPosition());
                    float exactHeightInput = worldPos.y *
                        (_FogHeightScale / (cameraDistance * _FogSoften * 0.01));
                    exactHeightInput += _FogHeightOffset -
                        cameraDistance * _FogSoftenOffset * 0.001;
                    albedo = ApplyHeightFogCurve(albedo, exactHeightInput);
                }
                #else
                {
                    float exactHeightInput =
                        worldPos.y * _FogHeightScale + _FogHeightOffset;
                    albedo = ApplyHeightFogCurve(albedo, exactHeightInput);
                }
                #endif
                #endif

                #if defined(NOISE_DITHERING)
                #if USE_NOISE_SCREEN_POSITION
                albedo = ApplyNoiseDither(albedo, i.noiseScreenPos, _GlobalBlueNoiseTex);
                #else
                albedo = ApplyNoiseDither(albedo, 0.0, _GlobalBlueNoiseTex);
                #endif
                #endif

                // Dissolve-color routes apply the final edge-color
                // blend after fog and blue-noise dithering.
                #if defined(DISSOLVE) && defined(DISSOLVE_COLOR)
                albedo.rgb = lerp(
                    albedo.rgb,
                    _DissolveColorIntensity *
                    UNITY_ACCESS_INSTANCED_PROP(Props, _DissolveColor).rgb,
                    dissolveFactor);
                #endif
                return albedo;
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "META"
            Tags { "LightMode"="META" }
            Cull Off
            ZWrite On
            ZTest LEqual
            Blend One Zero, One Zero
            BlendOp Add, Add
            ColorMask RGBA

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitMetaVertex
            #pragma fragment LitMetaFragment
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"
            #include "UnityMetaPass.cginc"

            float4 _Color;
            float4 _MainTex_ST;
            float4 _DetailAlbedoMap_ST;
            float _UVSec;

            struct LitMetaAttributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct LitMetaVaryings
            {
                float4 position : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            LitMetaVaryings LitMetaVertex(LitMetaAttributes v)
            {
                LitMetaVaryings o;
                // The META stereo program reads VP[0], not the instance eye.
                #if defined(UNITY_STEREO_INSTANCING_ENABLED)
                unity_StereoEyeIndex = 0;
                #endif
                o.position = UnityMetaVertexPosition(v.vertex, v.uv1, v.uv2,
                    unity_LightmapST, unity_DynamicLightmapST);
                o.uv.xy = v.uv0 * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 detailUV = _UVSec == 0.0 ? v.uv0 : v.uv1;
                o.uv.zw = detailUV * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
                return o;
            }

            float4 LitMetaFragment() : SV_Target
            {
                float outputBoost = saturate(unity_OneOverOutputBoost);
                float3 albedo = exp2(log2(_Color.rgb) * outputBoost);
                albedo = min(albedo, unity_MaxOutputValue);
                float4 result = unity_MetaFragmentControl.x
                    ? float4(albedo, 1.0) : float4(0.0, 0.0, 0.0, 0.0);
                return unity_MetaFragmentControl.y
                    ? float4(0.0, 0.0, 0.0, 1.0) : result;
            }
            ENDHLSL
        }
    }
}
