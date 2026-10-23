using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Geometry Appearance SO", fileName = "GeometryAppearanceSO")]
    public class GeometryAppearanceSO : ScriptableObject
    {
        [SerializeField] private Material regularMaterial;
        [SerializeField] private Material lightOpaqueMaterial;
        [SerializeField] private Material lightTransparentMaterial;
        [SerializeField] private Material glowingMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Shader baseWaterShader;
        [SerializeField] private Material billieWaterMaterial;
        [SerializeField] private Material btsMaterial;
        [SerializeField] private Material interscopeConcreteMaterial;
        [SerializeField] private Material interscopeCarMaterial;
        [SerializeField] private Material obstacleMaterial;

        private static readonly BaseMaterial standard = new() { Shader = "Standard" };
        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int cullModeId = Shader.PropertyToID("_CullMode");
        private readonly Dictionary<string, Material> keywordMaterials = new();

        private void OnDisable()
        {
            foreach (var material in keywordMaterials.Values)
            {
                if (Application.isPlaying)
                    Destroy(material);

                else
                    DestroyImmediate(material);

            }

            keywordMaterials.Clear();
        }

        internal static BaseMaterial ResolveMaterial(BaseEnvironmentEnhancement eh)
        {
            BaseMaterial basemat = standard;
            switch (eh.Geometry[eh.GeometryKeyMaterial])
            {
                case JSONString str:
                    if (!BeatSaberSongContainer.Instance.Map.Materials.TryGetValue(str.Value, out basemat))
                    {
                        if (str.Value != "standard")
                            Debug.LogError($"Missing material \"{str.Value}\"! {BeatmapLogContext.Describe(eh)}");

                        basemat = standard;
                    }

                    break;
                case JSONObject obj:
                    basemat = new BaseMaterial(obj);
                    break;
                default:
                    Debug.LogError($"Geometry with invalid material [{eh.Geometry[eh.GeometryKeyMaterial]}]! {BeatmapLogContext.Describe(eh)}");
                    break;
            }

            return basemat;
        }

        internal static ShaderType ResolveShader(BaseMaterial basemat, BaseEnvironmentEnhancement eh)
        {
            if (!Enum.TryParse(basemat.Shader ?? "Standard", out ShaderType shader)
                || !Enum.IsDefined(typeof(ShaderType), shader))
            {
                Debug.LogError($"Invalid shader '{basemat.Shader}'! {BeatmapLogContext.Describe(eh)}");
                return ShaderType.Standard;
            }

            return shader;
        }

        public void SetGeometryAppearance(GeometryContainer container)
        {
            var eh = container.EnvironmentEnhancement;
            if (eh.Geometry == null)
                return;

            var basemat = container.MaterialDefinition;
            var shader = ResolveShader(basemat, eh);

            var material = shader switch
            {
                ShaderType.OpaqueLight => lightOpaqueMaterial,
                ShaderType.TransparentLight => lightTransparentMaterial,
                ShaderType.Glowing => GetGeneratedPresetMaterial(glowingMaterial, shader),
                ShaderType.BaseWater => GetBaseWaterMaterial(),
                // Chroma uses WaterfallFalling for BillieWater geometry, which has no planar reflection component.
                ShaderType.BillieWater => billieWaterMaterial,
                ShaderType.WaterfallMirror => waterMaterial,
                ShaderType.BTSPillar => btsMaterial,
                ShaderType.InterscopeConcrete => interscopeConcreteMaterial,
                ShaderType.InterscopeCar => interscopeCarMaterial,
                ShaderType.Obstacle => obstacleMaterial,
                _ => GetGeneratedPresetMaterial(regularMaterial, ShaderType.Standard),
            };

            // Chroma uses unlit Glowing for Standard and BTSPillar only when shaderKeywords is explicitly empty.
            var unlitStandard = (shader == ShaderType.Standard || shader == ShaderType.BTSPillar)
                && basemat.ShaderKeywords is { Count: 0 };
            if (unlitStandard)
                material = GetUnlitStandardMaterial();

            else if (basemat.ShaderKeywords != null)
            {
                var keywords = basemat.ShaderKeywords
                    .Select(keyword => CanonicalizeKeyword(keyword, shader))
                    .Where(keyword => keyword != null);
                material = GetKeywordMaterial(material, keywords);
            }

            Enum.TryParse(eh.Geometry[eh.GeometryKeyType].Value, out GeometryContainer.GeometryType geometryType);
            if (shader == ShaderType.TransparentLight
                && (geometryType == GeometryContainer.GeometryType.Quad || geometryType == GeometryContainer.GeometryType.Triangle))
            {
                material = GetDoubleSidedTransparentPlanarMaterial(material);
            }

            // Native Chroma zeroes the authored color's alpha on the empty-keywords upgrade so the
            // surface contributes zero bloom while remaining opaque.
            if (basemat.Color.HasValue || unlitStandard || IsGeneratedPreset(shader))
            {
                var color = basemat.Color ?? Color.clear;
                if (unlitStandard)
                    color.a = 0f;

                container.MpbController.Mpb.SetColor(colorId, color);
            }

            // For animating material color
            if (basemat.TrackNames.Count > 0)
                container.MaterialAnimator.AttachToMaterial(container, basemat.TrackData, unlitStandard);

            foreach (var r in container.MpbController.Renderers)
                r.sharedMaterial = material;

            container.MpbController.ApplyChanges();
        }

        private static bool IsGeneratedPreset(ShaderType shader) => shader is ShaderType.Standard
            or ShaderType.OpaqueLight or ShaderType.TransparentLight or ShaderType.BaseWater or ShaderType.Glowing;

        // BaseWater starts from WaterLit's shader defaults. WaterfallMirror and WaterfallFalling carry different reflection inputs.
        private Material GetBaseWaterMaterial()
        {
            const string BaseWaterKey = "base-water";
            if (keywordMaterials.TryGetValue(BaseWaterKey, out var cached))
                return cached;

            var material = new Material(baseWaterShader)
            {
                name = "BaseWater (Geometry Preset)",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive,
                color = Color.clear,
                shaderKeywords = new[]
                {
                    "FOG", "HEIGHT_FOG", "INVERT_RIMLIGHT", "MASK_RED_IS_ALPHA", "NOISE_DITHERING",
                    "NORMAL_MAP", "REFLECTION_PROBE", "REFLECTION_PROBE_BOX_PROJECTION",
                    "_DECALBLEND_ALPHABLEND", "_DISSOLVEAXIS_LOCALX", "_EMISSIONCOLORTYPE_FLAT",
                    "_EMISSIONTEXTURE_NONE", "_RIMLIGHT_NONE", "_ROTATE_UV_NONE", "_VERTEXMODE_NONE",
                    "_WHITEBOOSTTYPE_NONE", "_ZWRITE_ON"
                }
            };
            keywordMaterials.Add(BaseWaterKey, material);
            return material;
        }

        // Generated presets use shader defaults rather than the settings on an environment's imported material.
        private Material GetGeneratedPresetMaterial(Material source, ShaderType shader)
        {
            var cacheKey = "generated-preset:" + shader;
            if (keywordMaterials.TryGetValue(cacheKey, out var cached))
                return cached;

            var material = new Material(source)
            {
                name = source.name + " (Geometry Preset)",
                hideFlags = HideFlags.HideAndDontSave
            };
            var fogStartOffset = shader == ShaderType.Glowing
                ? float.PositiveInfinity
                : 0f;
            material.SetFloat("_FogStartOffset", fogStartOffset);
            if (shader == ShaderType.Standard)
                material.DisableKeyword("NOISE_DITHERING");

            material.SetColor(colorId, Color.clear);
            keywordMaterials.Add(cacheKey, material);
            return material;
        }

        private Material GetDoubleSidedTransparentPlanarMaterial(Material source)
        {
            var key = "planar-transparent-cull-off:" + source.GetInstanceID();
            if (keywordMaterials.TryGetValue(key, out var cached))
                return cached;

            var material = new Material(source)
            {
                name = source.name + " (Geometry Planar Double Sided)",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetFloat(cullModeId, (float)UnityEngine.Rendering.CullMode.Off);
            keywordMaterials.Add(key, material);
            return material;
        }

        // The empty-keywords upgrade keeps Glowing's infinite fog offset and suppresses authored bloom alpha.
        private Material GetUnlitStandardMaterial()
        {
            const string UnlitStandardKey = "unlit-standard";
            if (keywordMaterials.TryGetValue(UnlitStandardKey, out var cached))
                return cached;

            var material = new Material(glowingMaterial)
            {
                name = "Glowing (Unlit Standard)",
                hideFlags = HideFlags.HideAndDontSave,
                shaderKeywords = Array.Empty<string>()
            };
            material.SetFloat("_FogStartOffset", float.PositiveInfinity);
            keywordMaterials.Add(UnlitStandardKey, material);
            return material;
        }

        private Material GetKeywordMaterial(Material source, IEnumerable<string> keywords)
        {
            var canonicalKeywords = keywords.Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var key = source.GetInstanceID() + ":" + string.Join("|", canonicalKeywords);
            if (keywordMaterials.TryGetValue(key, out var cached))
                return cached;

            var material = new Material(source)
            {
                name = source.name + " (Geometry Keywords)",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.shaderKeywords = canonicalKeywords;
            keywordMaterials.Add(key, material);
            return material;
        }

        private static string CanonicalizeKeyword(string keyword, ShaderType shader)
        {
            // Native keyword names vary by shader family, so aliases must not enable a different family's branch.
            if (shader is ShaderType.OpaqueLight or ShaderType.TransparentLight && keyword == "ENABLE_HEIGHT_FOG")
                return "HEIGHT_FOG";

            if (shader == ShaderType.TransparentLight)
            {
                if (keyword == "ENABLE_WORLD_NOISE")
                    return "WORLD_NOISE";

                if (keyword == "ENABLE_WORLD_SPACE_FADE")
                    return "WORLD_SPACE_FADE";

            }

            if (shader == ShaderType.WaterfallMirror && keyword == "ENABLE_DIRT")
                return "DIRT";

            if (IsSimpleLitShader(shader))
            {
                if (keyword == "ENABLE_RIM_DIM")
                    return "RIM_DIM";

                if (keyword == "ENABLE_EMISSION_ANGLE_DISAPPEAR")
                    return "EMISSION_ANGLE_DISAPPEAR";

            }

            // These native global keywords can also be enabled on an authored material when the camera disables its global gate.
            if (keyword == "ENABLE_BLOOM_FOG")
                return "BLOOM_FOG";

            if (keyword == "MAIN_EFFECT_ENABLED"
                && (IsSimpleLitShader(shader) || shader is ShaderType.OpaqueLight or ShaderType.TransparentLight or ShaderType.Glowing))
            {
                return "POST_BLOOM";
            }

            return shader == ShaderType.Glowing
                ? CanonicalizeGlowingKeyword(keyword)
                : keyword;
        }

        private static bool IsSimpleLitShader(ShaderType shader) => shader is ShaderType.Standard or ShaderType.BTSPillar
            or ShaderType.InterscopeConcrete or ShaderType.InterscopeCar;

        private static string CanonicalizeGlowingKeyword(string keyword) => keyword switch
        {
            "_WHITEBOOSTTYPE_MAINEFFECT" => "_WHITEBOOSTTYPE_MAINEFFECT",
            "_WHITEBOOSTTYPE_ALWAYS" => "_WHITEBOOSTTYPE_ALWAYS",
            "_CUTOUT_NORMAL" => "CUTOUT",
            "_NOISE_DITHERING" => "NOISE_DITHERING",
            "_ENABLE_COLOR_INSTANCING" => null,
            "_CUTOUT_NONE" => null,
            "INSTANCING_ON" => null,
            "STEREO_INSTANCING_ON" => null,
            "UNITY_SINGLE_PASS_STEREO" => null,
            "STEREO_MULTIVIEW_ON" => null,
            "STEREO_CUBEMAP_RENDER_ON" => null,
            _ => keyword
        };

        // Numeric strings are accepted by Chroma's enum parser, so these values are part of the map format.
        internal enum ShaderType
        {
            Standard = 0,
            OpaqueLight = 1,
            TransparentLight = 2,
            BaseWater = 3,
            BillieWater = 4,
            BTSPillar = 5,
            InterscopeConcrete = 6,
            InterscopeCar = 7,
            Obstacle = 8,
            WaterfallMirror = 9,
            Glowing = 10
        }

        internal static bool IsLightType(ShaderType shaderType)
        {
            return shaderType == ShaderType.OpaqueLight
                || shaderType == ShaderType.TransparentLight
                || shaderType == ShaderType.BillieWater;
        }

    }
}
