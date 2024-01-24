using System;
using System.Collections.Generic;
using Beatmap.Base.Customs;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using Beatmap.Containers;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Geometry Appearance SO", fileName = "GeometryAppearanceSO")]
    public class GeometryAppearanceSO : ScriptableObject
    {
        [SerializeField] private Material lightMaterial;
        [SerializeField] private Material shinyMaterial;
        [SerializeField] private Material obstacleMaterial;
        [SerializeField] private Material regularMaterial;

        private static V3Material standard = new V3Material(new JSONObject { ["shader"] = "Standard" });

        public void SetGeometryAppearance(GeometryContainer container)
        {
            var eh = container.EnvironmentEnhancement;
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
                    basemat = (eh is V2EnvironmentEnhancement)
                        ? (BaseMaterial)new V2Material(obj)
                        : (BaseMaterial)new V3Material(obj);
                    break;
                default:
                    Debug.LogError("Geometry with invalid material!");
                    break;
            }

            ShaderType shader = (ShaderType)Enum.Parse(typeof(ShaderType), basemat.Shader ?? "Standard");

            var meshRenderer = container.Shape.GetComponent<MeshRenderer>();

            // Why can't we have C#9
            var material = shader switch
            {
                ShaderType.OpaqueLight  => lightMaterial,
                ShaderType.TransparentLight => lightMaterial,
                ShaderType.BaseWater => shinyMaterial,
                ShaderType.BillieWater => shinyMaterial,
                ShaderType.WaterfallMirror => shinyMaterial,
                ShaderType.Obstacle => obstacleMaterial,
                _ => regularMaterial,
            };

            if (basemat.Color is Color color)
            {
                var colorKeyword = shader switch
                {
                    ShaderType.OpaqueLight => "_EmissionColor",
                    ShaderType.TransparentLight => "_EmissionColor",
                    ShaderType.Obstacle => "_ColorTint",
                    _ => "_Color",
                };

                container.MaterialPropertyBlock.SetColor(colorKeyword, color);
            }

            // For animating material color
            if (basemat.Track is string track)
            {
                container.Animator.AddParent(track);
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.SetPropertyBlock(container.MaterialPropertyBlock);

            if (eh.LightID != null)
            {
                var light = container.Shape.AddComponent<LightingEvent>();
                light.OverrideLightGroup = true;
                light.OverrideLightGroupID = eh.LightType ?? 0;
                light.LightID = eh.LightID ?? 0;
            }
        }

        // Straight outta heck
        enum ShaderType
        {
            Standard,
            OpaqueLight,
            TransparentLight,
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
