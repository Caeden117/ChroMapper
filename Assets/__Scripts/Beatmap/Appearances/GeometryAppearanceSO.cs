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

        public void SetGeometryAppearance(GeometryContainer container)
        {
            var eh = container.EnvironmentEnhancement;
            var basemat = eh.Geometry[eh.GeometryKeyMaterial] switch
            {
                JSONString str => BeatSaberSongContainer.Instance.Map.Materials[str],
                JSONObject obj => (eh is V2EnvironmentEnhancement)
                    ? (BaseMaterial)new V2Material(obj)
                    : (BaseMaterial)new V3Material(obj),
                _ => throw new Exception("Geometry without a material!"),
            };

             ShaderType shader = (ShaderType)Enum.Parse(typeof(ShaderType), basemat.Shader);

             if (IsLightType(shader))
             {
                 var meshRenderer = container.Shape.GetComponent<MeshRenderer>();
                 var material = UnityEngine.Object.Instantiate(lightMaterial);

                 if (basemat.Color is Color color)
                 {
                     material.color = color;
                 }

                 meshRenderer.sharedMaterial = material;

                 var light = container.Shape.AddComponent<LightingEvent>();
                 light.OverrideLightGroup = true;
                 light.OverrideLightGroupID = eh.LightType ?? 0;
                 light.LightID = eh.LightID ?? 0;
             }
             else
             {
                 // TODO
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
