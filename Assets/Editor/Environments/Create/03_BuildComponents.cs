using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Axis = Beatmap.Enums.Axis;

public partial class EnvironmentSceneCreator
{
    private static void BuildComponents(CreateContainer container)
    {
        container.Descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        container.Descriptor.ID = container.Data.Data.ID;
        container.Descriptor.gameObject.GetOrAddComponent<BakedLightDataLoader>();

        container.Descriptor.ColorSchemeProvider = container.Descriptor.gameObject.AddComponent<ColorSchemeProvider>();
        var colorSchemePath = $"{Constants.ScriptsPath}/ColorSchemes/{container.Descriptor.ID}ColorScheme.asset";
        var colorScheme = AssetDatabase.LoadAssetAtPath<ColorSchemeSO>(colorSchemePath);
        if (colorScheme == null)
            Debug.LogWarning(
                $"[EnvironmentTools] ColorSchemeSO not found at '{colorSchemePath}'. Run 'Update Environment List' to create it.");
        container.Descriptor.ColorSchemeProvider.ColorScheme = colorScheme;

        container.Descriptor.SpectrogramDataProvider =
            container.Descriptor.gameObject.AddComponent<SpectrogramDataProvider>();

        container.Data.Data.FogParameters.CopyTo(container.Descriptor.BloomFogParams);
        container.Data.Data.SizeData.CopyTo(container.Descriptor.SizeData);

        var idRemapAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"{Constants.EditorPath}/LightIDTables/{container.Data.Data.ID}.json");
        var typeIdRemap = new Dictionary<int, Dictionary<int, int>>();
        if (idRemapAsset != null)
            typeIdRemap = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, int>>>(idRemapAsset.text);

        // core components
        container.Descriptor.BasicEventEffectManager =
            new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        container.Descriptor.BasicEventEffectManager.gameObject.transform.SetParent(
            GameObject.Find("Environment").transform);
        var cbe = container.Descriptor.BasicEventEffectManager.Register<ColorBoostEffect>(
            (int)EventTypeValue.ColorBoostEventType);
        cbe.ColorSchemeProvider = container.Descriptor.ColorSchemeProvider;

        container.Descriptor.LightColorGroupEffectManager = new GameObject("LightColorGroupEffectManager")
            .AddComponent<LightColorGroupEffectManager>();
        container.Descriptor.LightColorGroupEffectManager.gameObject.transform.SetParent(
            GameObject.Find("Environment").transform);
        var lcgemData = container
            .Data
            .Objects
            .FirstOrDefault(x => x.Components.LightColorGroupEffectManager != null)
            ?.Components.LightColorGroupEffectManager[0];
        if (lcgemData != null)
        {
            foreach (var lg in lcgemData.LightGroups)
                container.Descriptor.LightColorGroupEffectManager.Register(lg.GroupId, lg.NumberOfElements);
            foreach (var lightColorGroupEffect in container.Descriptor.LightColorGroupEffectManager.IdToEffect.Values)
            {
                lightColorGroupEffect.ColorSchemeProvider = container.Descriptor.ColorSchemeProvider;
                lightColorGroupEffect.ColorBoostEffect = cbe;
            }
        }

        var lseeData = container
            .Data
            .Objects
            .Where(x => x.Components.LightSwitchEventEffect != null)
            .SelectMany(x => x.Components.LightSwitchEventEffect)
            .ToArray();
        foreach (var data in lseeData)
        {
            var comp = container.Descriptor.BasicEventEffectManager.Register<BasicLightEffect>(data.EventType);
            data.FillComponents(comp.gameObject, comp, container);
            comp.ColorSchemeProvider = container.Descriptor.ColorSchemeProvider;
            comp.ColorBoostEffect = cbe;
            foreach (var (original, remap) in typeIdRemap.GetValueOrDefault(data.LightsId, new Dictionary<int, int>()))
                comp.LightIdRemapEntries.Add(new Vector2(original, remap));
        }

        var registeredLightInstance = new HashSet<int>();
        var lightToRegister = new List<(LightController controller, int lightId, int order, bool force)>();
        var sinkObject = new GameObject("Sink Object");
        sinkObject.transform.SetParent(container.Descriptor.BasicEventEffectManager.transform.parent);

        container.Descriptor.LightRotationGroupEffectManager = new GameObject("LightRotationGroupEffectManager")
            .AddComponent<LightRotationGroupEffectManager>();
        container.Descriptor.LightRotationGroupEffectManager.gameObject.transform.SetParent(
            GameObject.Find("Environment").transform);
        var lrgemData = container
            .Data
            .Objects
            .FirstOrDefault(x => x.Components.LightRotationGroupEffectManager != null)
            ?.Components.LightRotationGroupEffectManager[0];
        if (lrgemData != null)
        {
            foreach (var data in lrgemData.LightRotationGroups)
            {
                container.Descriptor.LightRotationGroupEffectManager.Register(data.GroupId, data.Count);

                RegisterRotation(Axis.X, data.XTransforms, data.MirrorX);
                RegisterRotation(Axis.Y, data.YTransforms, data.MirrorY);
                RegisterRotation(Axis.Z, data.ZTransforms, data.MirrorZ);
                continue;

                void RegisterRotation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = container.ChromaIdObjects[transformName].transform;
                        t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        container.Descriptor.LightRotationGroupEffectManager.Register(
                            data.GroupId,
                            i,
                            axis,
                            mirror,
                            t.gameObject.transform);
                    }
                }
            }
        }

        container.Descriptor.LightTranslationGroupEffectManager = new GameObject("LightTranslationGroupEffectManager")
            .AddComponent<LightTranslationGroupEffectManager>();
        container.Descriptor.LightTranslationGroupEffectManager.gameObject.transform.SetParent(
            GameObject.Find("Environment").transform);
        var ltgemData = container
            .Data
            .Objects
            .FirstOrDefault(x => x.Components.LightTranslationGroupEffectManager != null)
            ?.Components.LightTranslationGroupEffectManager[0];
        if (ltgemData != null)
        {
            foreach (var data in ltgemData.LightTranslationGroups)
            {
                container.Descriptor.LightTranslationGroupEffectManager.Register(
                    data.GroupId,
                    data.Count,
                    new[] { data.xTranslationLimits, data.yTranslationLimits, data.zTranslationLimits },
                    new[] { data.xDistributionLimits, data.yDistributionLimits, data.zDistributionLimits });

                RegisterTranslation(Axis.X, data.XTransforms, data.MirrorX);
                RegisterTranslation(Axis.Y, data.YTransforms, data.MirrorY);
                RegisterTranslation(Axis.Z, data.ZTransforms, data.MirrorZ);
                continue;

                void RegisterTranslation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = container.ChromaIdObjects[transformName].transform;
                        t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        container.Descriptor.LightTranslationGroupEffectManager.Register(
                            data.GroupId,
                            i,
                            axis,
                            mirror,
                            t.gameObject.transform);
                    }
                }
            }
        }

        container.Descriptor.FloatFxGroupEffectManager = new GameObject("FloatFxGroupEffectManager")
            .AddComponent<FloatFxGroupEffectManager>();
        container.Descriptor.FloatFxGroupEffectManager.gameObject.transform.SetParent(
            GameObject.Find("Environment").transform);
        var ffgemData = container
            .Data
            .Objects
            .FirstOrDefault(x => x.Components.FloatFxGroupEffectManager != null)
            ?.Components.FloatFxGroupEffectManager[0];
        if (ffgemData != null)
        {
            foreach (var data in ffgemData.FloatFxGroups)
            {
                container.Descriptor.FloatFxGroupEffectManager.Register(
                    data.LightGroup.GroupId,
                    data.LightGroup.NumberOfElements,
                    data.IsTriggerOnly);
            }
        }

        // PATCH: gotta tell the subclass of the instance
        foreach (var obj in container.Data.Objects.Where(x => x.Components.TubeBloomPrePassLightWithId != null))
        foreach (var data in obj.Components.TubeBloomPrePassLightWithId)
        {
            if (data.TubeBloomPrePassLight != null) data.TubeBloomPrePassLight.Instance = data.Instance;
            container.ComponentInstances[data.TubeBloomPrePassLight.InstanceId] = data.TubeBloomPrePassLight;
        }

        foreach (var obj in container.Data.Objects.Where(x => x.Components.ParticleSystem != null))
        foreach (var data in obj.Components.ParticleSystem)
        {
            if (data.Renderer == null) continue;
            container.ComponentInstances[data.Renderer.InstanceId] =
                new ParticleSystemRendererData { Instance = data.Instance.GetComponent<ParticleSystemRenderer>() };
        }

        // apply all components
        foreach (var data in container.ComponentInstances.Values) data.Apply(container);

        // reset the ring
        foreach (var obj in container.Data.Objects.Where(x => x.Components.TrackLaneRingsManager != null))
        foreach (var data in obj.Components.TrackLaneRingsManager)
            data.GetComponent().Start();

        var lightWithIdManager = container
            .Data
            .Objects.FirstOrDefault(x => x.Components.LightWithIdManager != null)
            ?.Components.LightWithIdManager[0];
        if (lightWithIdManager != null)
        {
            foreach (var (lightId, lights) in lightWithIdManager.Lights)
            {
                for (var order = 0; order < lights.Length; order++)
                {
                    var light = lights[order];
                    if (light is null) RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order);

                    if (light.ArrayId != null)
                    {
                        // Get runtime light
                        if (container.LightWithIds.TryGetValue(light.InstanceId, out var controller))
                        {
                            switch (controller)
                            {
                                case ColorArrayLightsController calc:
                                    RegisterLight(calc.ColorArrayData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                case GlobalShaderColorLightsController gsclc:
                                    RegisterLight(gsclc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                case LightmapLightsController llc:
                                    RegisterLight(llc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                case LightmapsLightsController llc:
                                    RegisterLight(llc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                case CombinedLightsController clc:
                                    RegisterLight(clc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                case CombinedLightsGroupController clgc:
                                    RegisterLight(clgc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                default:
                                    RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order, true);
                                    break;
                            }
                        }
                        // Otherwise become sink
                        else
                            RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order, true);

                        continue;
                    }

                    if (!container.ComponentInstances.ContainsKey(light.InstanceId))
                    {
                        // If for whatever reason this is missing, become sink
                        RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order, true);
                        continue;
                    }

                    // Non-runtime
                    GetAndRegisterLight(light.InstanceId, order, true);
                    registeredLightInstance.Add(light.InstanceId);
                }
            }
        }

        // the rest of the light if they were not registered due to dynamic registration
        foreach (var data in container.ComponentInstances.Keys)
            GetAndRegisterLight(data); // TODO: the rest of id, which is likely bad for lightId if they were inactive

        if (ffgemData != null)
        {
            foreach (var data in ffgemData.FloatFxGroupEffects)
            {
                var fx = container.ComponentInstances[data.Target].Instance as FxTarget;
                if (fx == null) continue;
                container.Descriptor.FloatFxGroupEffectManager.Register(data.GroupId, data.ElementId, fx);
            }
        }

        var tffgemData = container
            .Data
            .Objects
            .FirstOrDefault(x => x.Components.TriggerFloatFxGroupEffectManager != null)
            ?.Components.TriggerFloatFxGroupEffectManager[0];
        if (tffgemData != null)
        {
            foreach (var data in tffgemData.FloatFxGroupEffects)
            {
                var fx = container.ComponentInstances[data.Target].Instance as FxTarget;
                if (fx == null) continue;
                container.Descriptor.FloatFxGroupEffectManager.Register(data.GroupId, data.ElementId, fx);
            }
        }

        FinalRegisterLight();

        return;

        void GetAndRegisterLight(
            int instanceId = -1,
            int order = -1,
            bool force = false)
        {
            if (registeredLightInstance.Contains(instanceId)) return;
            var data = container.ComponentInstances[instanceId];

            switch (data)
            {
                case BloomPrePassBackgroundColorsGradientTintColorWithLightIdsData b:
                    RegisterLight(b.Instance as LightController, b.Id, order, force);
                    break;
                case DirectionalLightWithIdData c:
                    RegisterLight(c.Instance as LightController, c.Id, order, force);
                    break;
                case EnableRendererLightWithIdData d:
                    RegisterLight(d.Instance as LightController, d.Id, order, force);
                    break;
                case InstancedMaterialLightWithIdData e:
                    RegisterLight(e.Instance as LightController, e.Id, order, force);
                    break;
                case MaterialLightWithIdData f:
                    RegisterLight(f.Instance as LightController, f.Id, order, force);
                    break;
                case ParticleSystemLightWithIdData g:
                    RegisterLight(g.Instance as LightController, g.Id, order, force);
                    break;
                case RectangleFakeGlowLightWithIdData h:
                    RegisterLight(h.Instance as LightController, h.Id, order, force);
                    break;
                case SpriteArrayLightWithIdData i:
                    RegisterLight(i.Instance as LightController, i.Id, order, force);
                    break;
                case SpriteLightWithIdData j:
                    RegisterLight(j.Instance as LightController, j.Id, order, force);
                    break;
                case TubeBloomPrePassLightWithIdData k:
                    RegisterLight(k.Instance as LightController, k.Id, order, force);
                    break;
                case BurstFireEffectData l:
                    RegisterLight(l.Instance as LightController, l.Id, order, force);
                    break;
                case ContinuousFireEffectData m:
                    RegisterLight(m.Instance as LightController, m.Id, order, force);
                    break;
            }
        }

        void RegisterLight(LightController controller, int lightId, int order, bool force = false) =>
            lightToRegister.Add((controller, lightId, order, force));

        void FinalRegisterLight()
        {
            foreach (var (controller, lightId, order, force) in lightToRegister)
            {
                var lg = lcgemData?.LightGroups.FirstOrDefault(x =>
                    x.StartLightId <= lightId && lightId < x.StartLightId + x.NumberOfElements);
                if (lg != null)
                {
                    controller.Kind = LightController.LightKind.Group;
                    controller.Type = lg.GroupId;
                    controller.ID = lightId - lg.StartLightId;
                    if (force || !SkipThisShit(controller.transform)) container.Descriptor.Register(controller);
                    continue;
                }

                var lsee = lseeData.FirstOrDefault(x => x.LightsId == lightId);
                if (lsee != null)
                {
                    controller.Kind = LightController.LightKind.Basic;
                    controller.Type = lsee.EventType;
                    controller.ID = order;
                    if (force || !SkipThisShit(controller.transform)) container.Descriptor.Register(controller);
                    continue;
                }

                Debug.LogWarning(
                    $"{(controller.TryGetComponent<ChromaIDMarker>(out var marker) ? marker.ChromaID : "")}: {controller} ID {lightId} could not be registered, missing event type or group ID register?");
                continue;

                // Should we skip or register regardless
                bool SkipThisShit(Transform tr)
                {
                    if (tr.gameObject.activeInHierarchy) return false;
                    while (true)
                    {
                        if (!tr.gameObject.activeSelf)
                            return !tr.gameObject.GetComponent<ChromaIDMarker>().MarkActivator;
                        if (tr.parent == tr) break;
                        tr = tr.parent;
                    }

                    return true;
                }
            }
        }
    }
}
