using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

/// <summary>Applies the allowlisted Vivify rendering settings as absolute-time custom-event properties.</summary>
public sealed class RenderingSettingsManager
{
    public CustomEventPropertyTimeline PropertyTimeline;
    public VivifyAssetBundleManager VivifyAssetBundleManager;
    public InstantiateObjectPrefabManager InstantiateObjectPrefabManager;
    public Action<bool> SetSkyboxOverride;

    private static readonly int lightShadowBiasId = Shader.PropertyToID("unity_LightShadowBias");
    private int? previousPixelLightCount;
    private Vector4? previousLightShadowBias;

    /// <summary>Captures lighting state and applies Beat Saber's Vivify light budget after outgoing event properties have been reset.</summary>
    /// <param name="vivify">Whether the selected difficulty requires Vivify; other difficulties keep the editor defaults.</param>
    public void ApplyDefaults(bool vivify)
    {
        RestoreDefaults();
        if (!vivify) return;

        previousPixelLightCount = QualitySettings.pixelLightCount;
        previousLightShadowBias = Shader.GetGlobalVector(lightShadowBiasId);
        // Beat Saber uses 0; Unity still renders the main directional light in the base pass.
        QualitySettings.pixelLightCount = 0;
    }

    /// <summary>Restores light shader state and, unless rebuilding the same map, the editor's light budget.</summary>
    /// <param name="preserveMapDefaults">Keep the Vivify light budget and captured defaults while rebuilding the same map.</param>
    public void RestoreDefaults(bool preserveMapDefaults = false)
    {
        // Unity leaves this uniform set after shadow-casting bundle lights are removed.
        // Unlit outline passes can still read it even when the next map has no such light.
        if (previousLightShadowBias.HasValue)
            Shader.SetGlobalVector(lightShadowBiasId, previousLightShadowBias.Value);
        if (preserveMapDefaults) return;
        previousLightShadowBias = null;
        if (!previousPixelLightCount.HasValue) return;
        QualitySettings.pixelLightCount = previousPixelLightCount.Value;
        previousPixelLightCount = null;
    }

    public void Assign(CustomEventStateData state)
    {
        if (state?.Base?.Data == null) return;

        var data = state.Base.Data;
        var easing = CustomEventValueParser.GetEasing(data);
        AssignRenderSettings(state, data["renderSettings"], easing);
        AssignQualitySettings(state, data["qualitySettings"], easing);
        AssignXrSettings(state, data["xrSettings"], easing);
    }

    private void AssignRenderSettings(CustomEventStateData state, JSONNode properties, Func<float, float> easing)
    {
        if (properties == null || !properties.IsObject) return;

        foreach (var property in properties.AsObject)
        {
            var name = property.Key;
            var value = property.Value;
            if (value == null || value.IsNull) continue;

            switch (name)
            {
                case nameof(RenderSettings.ambientIntensity):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.ambientIntensity,
                        v => RenderSettings.ambientIntensity = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.ambientMode):
                    RegisterEnum<AmbientMode>(state, name, value, () => RenderSettings.ambientMode,
                        v => RenderSettings.ambientMode = v, easing);
                    break;
                case nameof(RenderSettings.defaultReflectionMode):
                    RegisterEnum<DefaultReflectionMode>(state, name, value, () => RenderSettings.defaultReflectionMode,
                        v => RenderSettings.defaultReflectionMode = v, easing);
                    break;
                case nameof(RenderSettings.defaultReflectionResolution):
                    RegisterInt(state, name, value, () => RenderSettings.defaultReflectionResolution,
                        v => RenderSettings.defaultReflectionResolution = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.flareFadeSpeed):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.flareFadeSpeed,
                        v => RenderSettings.flareFadeSpeed = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.flareStrength):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.flareStrength,
                        v => RenderSettings.flareStrength = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.fog):
                    RegisterBool(state, name, value, () => RenderSettings.fog,
                        v => RenderSettings.fog = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.fogColor):
                    RegisterColor(state, name, value, () => RenderSettings.fogColor,
                        v => RenderSettings.fogColor = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.fogDensity):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.fogDensity,
                        v => RenderSettings.fogDensity = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.fogEndDistance):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.fogEndDistance,
                        v => RenderSettings.fogEndDistance = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.fogMode):
                    RegisterEnum<FogMode>(state, name, value, () => RenderSettings.fogMode,
                        v => RenderSettings.fogMode = v, easing);
                    break;
                case nameof(RenderSettings.fogStartDistance):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.fogStartDistance,
                        v => RenderSettings.fogStartDistance = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.haloStrength):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.haloStrength,
                        v => RenderSettings.haloStrength = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.ambientEquatorColor):
                    RegisterColor(state, name, value, () => RenderSettings.ambientEquatorColor,
                        v => RenderSettings.ambientEquatorColor = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.ambientGroundColor):
                    RegisterColor(state, name, value, () => RenderSettings.ambientGroundColor,
                        v => RenderSettings.ambientGroundColor = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.ambientLight):
                    RegisterColor(state, name, value, () => RenderSettings.ambientLight,
                        v => RenderSettings.ambientLight = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.ambientSkyColor):
                    RegisterColor(state, name, value, () => RenderSettings.ambientSkyColor,
                        v => RenderSettings.ambientSkyColor = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.reflectionBounces):
                    RegisterInt(state, name, value, () => RenderSettings.reflectionBounces,
                        v => RenderSettings.reflectionBounces = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.reflectionIntensity):
                    RegisterFloat(state, "renderSettings:" + name, value, () => RenderSettings.reflectionIntensity,
                        v => RenderSettings.reflectionIntensity = v, easing, updateEnvironment: true);
                    break;
                case nameof(RenderSettings.subtractiveShadowColor):
                    RegisterColor(state, name, value, () => RenderSettings.subtractiveShadowColor,
                        v => RenderSettings.subtractiveShadowColor = v, easing, updateEnvironment: true);
                    break;
                case "skybox":
                    RegisterSkybox(state, value);
                    break;
                case "sun":
                    RegisterSun(state, value);
                    break;
            }
        }
    }

    private void AssignQualitySettings(CustomEventStateData state, JSONNode properties, Func<float, float> easing)
    {
        if (properties == null || !properties.IsObject) return;

        foreach (var property in properties.AsObject)
        {
            var name = property.Key;
            var value = property.Value;
            if (value == null || value.IsNull) continue;

            switch (name)
            {
                case nameof(QualitySettings.anisotropicFiltering):
                    RegisterEnum<AnisotropicFiltering>(state, "qualitySettings:" + name, value,
                        () => QualitySettings.anisotropicFiltering, v => QualitySettings.anisotropicFiltering = v, easing);
                    break;
                case nameof(QualitySettings.antiAliasing):
                    RegisterInt(state, "qualitySettings:" + name, value, () => QualitySettings.antiAliasing,
                        v => QualitySettings.antiAliasing = v, easing);
                    break;
                case nameof(QualitySettings.pixelLightCount):
                    RegisterInt(state, "qualitySettings:" + name, value, () => QualitySettings.pixelLightCount,
                        v => QualitySettings.pixelLightCount = v, easing);
                    break;
                case nameof(QualitySettings.realtimeReflectionProbes):
                    RegisterBool(state, "qualitySettings:" + name, value, () => QualitySettings.realtimeReflectionProbes,
                        v => QualitySettings.realtimeReflectionProbes = v, easing);
                    break;
                case nameof(QualitySettings.shadowCascades):
                    RegisterInt(state, "qualitySettings:" + name, value, () => QualitySettings.shadowCascades,
                        v => QualitySettings.shadowCascades = v, easing);
                    break;
                case nameof(QualitySettings.shadowDistance):
                    RegisterFloat(state, "qualitySettings:" + name, value, () => QualitySettings.shadowDistance,
                        v => QualitySettings.shadowDistance = v, easing);
                    break;
                case nameof(QualitySettings.shadowmaskMode):
                    RegisterEnum<ShadowmaskMode>(state, "qualitySettings:" + name, value,
                        () => QualitySettings.shadowmaskMode, v => QualitySettings.shadowmaskMode = v, easing);
                    break;
                case nameof(QualitySettings.shadowNearPlaneOffset):
                    RegisterFloat(state, "qualitySettings:" + name, value, () => QualitySettings.shadowNearPlaneOffset,
                        v => QualitySettings.shadowNearPlaneOffset = v, easing);
                    break;
                case nameof(QualitySettings.shadowProjection):
                    RegisterEnum<ShadowProjection>(state, "qualitySettings:" + name, value,
                        () => QualitySettings.shadowProjection, v => QualitySettings.shadowProjection = v, easing);
                    break;
                case nameof(QualitySettings.shadowResolution):
                    RegisterEnum<ShadowResolution>(state, "qualitySettings:" + name, value,
                        () => QualitySettings.shadowResolution, v => QualitySettings.shadowResolution = v, easing);
                    break;
                case nameof(QualitySettings.shadows):
                    RegisterEnum<ShadowQuality>(state, "qualitySettings:" + name, value,
                        () => QualitySettings.shadows, v => QualitySettings.shadows = v, easing);
                    break;
                case nameof(QualitySettings.softParticles):
                    RegisterBool(state, "qualitySettings:" + name, value, () => QualitySettings.softParticles,
                        v => QualitySettings.softParticles = v, easing);
                    break;
            }
        }
    }

    private void AssignXrSettings(CustomEventStateData state, JSONNode properties, Func<float, float> easing)
    {
        if (properties == null || !properties.IsObject) return;

        var value = properties[nameof(XRSettings.useOcclusionMesh)];
        if (value == null || value.IsNull) return;
        RegisterBool(state, "xrSettings:" + nameof(XRSettings.useOcclusionMesh), value,
            () => XRSettings.useOcclusionMesh, v => XRSettings.useOcclusionMesh = v, easing);
    }

    private void RegisterSkybox(CustomEventStateData state, JSONNode value)
    {
        if (!value.IsString) return;
        var material = VivifyAssetBundleManager != null
            ? VivifyAssetBundleManager.AssetPathToMaterial.GetValueOrDefault(value.Value)
            : null;
        if (material == null)
        {
            Debug.LogWarning($"Vivify: missing skybox material '{value.Value}'.");
            return;
        }
        Register(state, "renderSettings:skybox", () => RenderSettings.skybox,
            v =>
            {
                RenderSettings.skybox = v;
                SetSkyboxOverride?.Invoke(v != null);
            }, _ => material, null, updateEnvironment: true);
    }

    private void RegisterSun(CustomEventStateData state, JSONNode value)
    {
        if (!value.IsString) return;
        Light sun = null;
        if (InstantiateObjectPrefabManager != null)
        {
            foreach (var instance in InstantiateObjectPrefabManager.GetObjectById(value.Value, state))
            {
                if (instance == null) continue;
                var lights = instance.GetComponentsInChildren<Light>(true);
                for (var i = 0; i < lights.Length; i++)
                {
                    if (lights[i] == null || lights[i].type != LightType.Directional) continue;
                    sun = lights[i];
                    break;
                }

                if (sun != null) break;
            }
        }

        if (sun == null)
        {
            Debug.LogWarning($"Vivify: no directional light on prefab '{value.Value}'.");
            return;
        }
        Register(state, "renderSettings:sun", () => RenderSettings.sun,
            v => RenderSettings.sun = v, _ => sun, null, updateEnvironment: true);
    }

    private void RegisterFloat(CustomEventStateData state, string property, JSONNode value,
        Func<float> read, Action<float> write, Func<float, float> easing, bool updateEnvironment = false)
    {
        if (CustomEventValueParser.TryReadFloat(value, out var number, allowBoolean: true))
        {
            Register(state, GetPropertyKey(property), read, write, _ => number, null, updateEnvironment);
            return;
        }

        var points = PointDefinitionParser.Get(value);
        if (points != null)
            Register(state, GetPropertyKey(property), read, write, points.GetFloat, easing, updateEnvironment: false);
    }

    private void RegisterInt(CustomEventStateData state, string property, JSONNode value,
        Func<int> read, Action<int> write, Func<float, float> easing, bool updateEnvironment = false)
    {
        if (CustomEventValueParser.TryReadFloat(value, out var number, allowBoolean: true))
        {
            Register(state, GetPropertyKey(property), read, write, _ => Convert.ToInt32(number), null, updateEnvironment);
            return;
        }

        var points = PointDefinitionParser.Get(value);
        if (points != null)
            Register(state, GetPropertyKey(property), read, write, t => Convert.ToInt32(points.GetFloat(t)), easing,
                updateEnvironment: false);
    }

    private void RegisterBool(CustomEventStateData state, string property, JSONNode value,
        Func<bool> read, Action<bool> write, Func<float, float> easing, bool updateEnvironment = false)
    {
        if (CustomEventValueParser.TryReadFloat(value, out var number, allowBoolean: true))
        {
            var boolean = number != 0f;
            Register(state, GetPropertyKey(property), read, write, _ => boolean, null, updateEnvironment);
            return;
        }

        var points = PointDefinitionParser.Get(value);
        if (points != null)
            Register(state, GetPropertyKey(property), read, write, t => points.GetFloat(t) != 0f, easing,
                updateEnvironment: false);
    }

    private void RegisterEnum<TEnum>(CustomEventStateData state, string property, JSONNode value,
        Func<TEnum> read, Action<TEnum> write, Func<float, float> easing)
        where TEnum : struct, Enum
    {
        if (CustomEventValueParser.TryReadFloat(value, out var number, allowBoolean: true))
        {
            Register(state, GetPropertyKey(property), read, write,
                _ => (TEnum)Enum.ToObject(typeof(TEnum), (int)number), null, updateEnvironment: false);
            return;
        }

        var points = PointDefinitionParser.Get(value);
        if (points != null)
            Register(state, GetPropertyKey(property), read, write,
                t => (TEnum)Enum.ToObject(typeof(TEnum), (int)points.GetFloat(t)), easing, updateEnvironment: false);
    }

    private void RegisterColor(CustomEventStateData state, string property, JSONNode value,
        Func<Color> read, Action<Color> write, Func<float, float> easing, bool updateEnvironment = false)
    {
        if (CustomEventValueParser.TryReadColor(value, out var color))
        {
            Register(state, GetPropertyKey(property), read, write, _ => color, null, updateEnvironment);
            return;
        }

        var points = PointDefinitionParser.Get(value);
        if (points != null)
            Register(state, GetPropertyKey(property), read, write, points.GetColor, easing, updateEnvironment: false);
    }

    private void Register<T>(CustomEventStateData state, string property,
        Func<T> read, Action<T> write, Func<float, T> sample, Func<float, float> easing,
        bool updateEnvironment)
    {
        Action<T> apply = value =>
        {
            if (EqualityComparer<T>.Default.Equals(read(), value)) return;
            write(value);
            if (updateEnvironment) DynamicGI.UpdateEnvironment();
        };

        PropertyTimeline.Register(state, null, property, read, apply, sample, easing);
    }

    private static string GetPropertyKey(string property) =>
        property.IndexOf(':') >= 0 ? property : "renderSettings:" + property;
}
