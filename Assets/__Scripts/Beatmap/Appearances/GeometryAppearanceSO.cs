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
        [SerializeField] private Material btsMaterial;
        [SerializeField] private Material obstacleMaterial;

        private static BaseMaterial standard;
        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int cullModeId = Shader.PropertyToID("_CullMode");
        private readonly Dictionary<string, Material> keywordMaterials = new();

        public void OnEnable() => standard = new BaseMaterial { Shader = "Standard" };

        private void OnDisable()
        {
            foreach (var material in keywordMaterials.Values)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            keywordMaterials.Clear();
        }

        public void SetGeometryAppearance(GeometryContainer container)
        {
            var eh = container.EnvironmentEnhancement;

            // Bail if not geometry - environment enhancement is handled elsewhere
            if (eh.Geometry == null) return;

            BaseMaterial basemat = standard;
            switch (eh.Geometry[eh.GeometryKeyMaterial])
            {
                case JSONString str:
                    if (str.Value != "standard")
                    {
                        if (!BeatSaberSongContainer.Instance.Map.Materials.TryGetValue(str.Value, out basemat))
                        {
                            Debug.LogError($"Missing material \"{str.Value}\"!");
                            basemat = standard;
                        }
                    }

                    break;
                case JSONObject obj:
                    basemat = new BaseMaterial(obj);
                    break;
                default:
                    Debug.LogError("Geometry with invalid material!");
                    break;
            }

            ShaderType shader = ShaderType.Standard;
            if (!Enum.TryParse(basemat.Shader ?? "Standard", out shader))
                Debug.LogError($"Invalid shader '{basemat.Shader}'!");

            var material = shader switch
            {
                ShaderType.OpaqueLight => lightOpaqueMaterial,
                ShaderType.TransparentLight => lightTransparentMaterial,
                ShaderType.Glowing => glowingMaterial,
                ShaderType.BaseWater => waterMaterial,
                ShaderType.BillieWater => waterMaterial,
                ShaderType.WaterfallMirror => waterMaterial,
                ShaderType.BTSPillar => btsMaterial,
                ShaderType.Obstacle => obstacleMaterial,
                _ => regularMaterial,
            };

            // Chroma changes SPECIFICALLY Standard and BTSPillar to unlit Glowing material ONLY when the authored
            // shaderKeywords array is present AND empty.
            var unlitStandard = (shader == ShaderType.Standard || shader == ShaderType.BTSPillar)
                && basemat.ShaderKeywords is { Count: 0 };
            if (unlitStandard)
                material = GetUnlitStandardMaterial();
            else if (basemat.ShaderKeywords != null)
            {
                IEnumerable<string> keywords = basemat.ShaderKeywords;
                if (shader == ShaderType.Glowing)
                    keywords = keywords.Select(CanonicalizeGlowingKeyword).Where(x => x != null);
                material = GetKeywordMaterial(material, keywords);
            }

            var geometryType = eh.Geometry[eh.GeometryKeyType].Value;
            if (shader == ShaderType.TransparentLight
                && (geometryType == "Quad" || geometryType == "Triangle"))
            {
                material = GetDoubleSidedTransparentPlanarMaterial(material);
            }

            // Native Chroma zeroes the authored color's alpha on the empty-keywords upgrade so the
            // surface contributes zero bloom while remaining opaque.
            if (basemat.Color.HasValue || unlitStandard)
            {
                var color = basemat.Color ?? Color.clear;
                if (unlitStandard)
                    color.a = 0f;
                container.MpbController.Mpb.SetColor(colorId, color);
            }

            // For animating material color
            if (basemat.Track is string track)
                container.MaterialAnimator.AttachToMaterial(container, track, unlitStandard);

            foreach (var r in container.MpbController.Renderers) r.sharedMaterial = material;
            container.MpbController.ApplyChanges();
        }

        private Material GetDoubleSidedTransparentPlanarMaterial(Material source)
        {
            var key = "planar-transparent-cull-off:" + source.GetInstanceID();
            if (keywordMaterials.TryGetValue(key, out var cached)) return cached;

            var material = new Material(source)
            {
                name = source.name + " (Geometry Planar Double Sided)",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetFloat(cullModeId, (float)UnityEngine.Rendering.CullMode.Off);
            keywordMaterials.Add(key, material);
            return material;
        }

        // Cached clone of the Glowing material for the Standard/BTSPillar + shaderKeywords:[]
        // upgrade only - native sets _FogStartOffset to +inf on its Glowing base. Cloning keeps
        // intended Glowing/TransparentLight materials on their own serialized settings.
        private Material GetUnlitStandardMaterial()
        {
            const string key = "unlit-standard";
            if (keywordMaterials.TryGetValue(key, out var cached)) return cached;
            var material = new Material(glowingMaterial)
            {
                name = "Glowing (Unlit Standard)",
                hideFlags = HideFlags.HideAndDontSave,
                shaderKeywords = Array.Empty<string>()
            };
            material.SetFloat("_FogStartOffset", float.PositiveInfinity);
            keywordMaterials.Add(key, material);
            return material;
        }

        private Material GetKeywordMaterial(Material source, IEnumerable<string> keywords)
        {
            var canonicalKeywords = keywords.Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var key = source.GetInstanceID() + ":" + string.Join("|", canonicalKeywords);
            if (keywordMaterials.TryGetValue(key, out var cached)) return cached;

            var material = new Material(source)
            {
                name = source.name + " (Geometry Keywords)",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.shaderKeywords = canonicalKeywords;
            keywordMaterials.Add(key, material);
            return material;
        }

        private static string CanonicalizeGlowingKeyword(string keyword) => keyword switch
        {
            "_WHITEBOOSTTYPE_MAINEFFECT" => "_WHITEBOOSTTYPE_MAINEFFECT",
            "_WHITEBOOSTTYPE_ALWAYS" => "_WHITEBOOSTTYPE_ALWAYS",
            "_CUTOUT_NORMAL" => "CUTOUT",
            "_NOISE_DITHERING" => "NOISE_DITHERING",
            "_ENABLE_COLOR_INSTANCING" => null,
            "ENABLE_BLOOM_FOG" => null,
            "MAIN_EFFECT_ENABLED" => null,
            "_CUTOUT_NONE" => null,
            "INSTANCING_ON" => null,
            "STEREO_INSTANCING_ON" => null,
            "UNITY_SINGLE_PASS_STEREO" => null,
            "STEREO_MULTIVIEW_ON" => null,
            "STEREO_CUBEMAP_RENDER_ON" => null,
            _ => keyword
        };

        // Straight outta heck
        enum ShaderType
        {
            Standard,
            OpaqueLight,
            TransparentLight,
            Glowing,
            BaseWater,
            BillieWater,
            BTSPillar,
            InterscopeConcrete,
            InterscopeCar,
            Obstacle,
            WaterfallMirror
        }

        static bool IsLightType(ShaderType shaderType)
        {
            return shaderType == ShaderType.OpaqueLight
                || shaderType == ShaderType.TransparentLight
                || shaderType == ShaderType.BillieWater;
        }
    }
}
