using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaMaterialRoutingParityTest : TestBase
    {
        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly string[] shaderPresets =
        {
            "Standard", "OpaqueLight", "TransparentLight", "BaseWater", "BillieWater",
            "BTSPillar", "InterscopeConcrete", "InterscopeCar", "WaterfallMirror", "Glowing"
        };
        private static readonly string[] geometryTypes =
        {
            "Sphere", "Capsule", "Cylinder", "Cube", "Plane", "Quad", "Triangle"
        };
        private bool previousAnimations;

        [UnityTest]
        public IEnumerator AuthoredSimpleLitEmissionAngleAliasReachesCompiledBranch() => CheckEmissionAngleAlias(3);

        [UnityTest]
        public IEnumerator LegacyAuthoredSimpleLitEmissionAngleAliasReachesCompiledBranch() => CheckEmissionAngleAlias(2);

        private static IEnumerator CheckEmissionAngleAlias(int version)
        {
            var data = NewDifficulty();
            var presets = new[] { "Standard", "BTSPillar", "InterscopeConcrete", "InterscopeCar" };
            foreach (var preset in presets)
            {
                var material = new JSONObject
                {
                    ["shader"] = preset,
                    ["shaderKeywords"] = new JSONArray { [0] = "ENABLE_EMISSION_ANGLE_DISAPPEAR" }
                };
                data["customData"]["materials"][preset] = material;
                AddGeometry(data, "emission-named-" + preset, "Cube", new JSONString(preset));
                AddGeometry(data, "emission-inline-" + preset, "Cube", material.Clone());
            }

            yield return Load(data, version);
            var failures = new List<string>();
            foreach (var preset in presets)
            {
                foreach (var route in new[] { "named", "inline" })
                {
                    var material = Find("emission-" + route + "-" + preset).MpbController.Renderers.Single().sharedMaterial;
                    var keyword = new LocalKeyword(material.shader, "EMISSION_ANGLE_DISAPPEAR");
                    Assert.That(keyword.isValid, Is.True, preset + " has no compiled emission-angle positive control");
                    Check(
                        material.IsKeywordEnabled(keyword),
                        preset + " " + route + " dropped the active native emission-angle request",
                        failures);
                }

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator AuthoredLocalAliasesReachTheirNativeShaderFamily() => CheckLocalAliases(3);

        [UnityTest]
        public IEnumerator LegacyAuthoredLocalAliasesReachTheirNativeShaderFamily() => CheckLocalAliases(2);

        private static IEnumerator CheckLocalAliases(int version)
        {
            var requests = new[]
            {
                (Preset: "OpaqueLight", NativeKeyword: "ENABLE_HEIGHT_FOG", PortedKeyword: "HEIGHT_FOG"),
                (Preset: "TransparentLight", NativeKeyword: "ENABLE_HEIGHT_FOG", PortedKeyword: "HEIGHT_FOG"),
                (Preset: "TransparentLight", NativeKeyword: "ENABLE_WORLD_NOISE", PortedKeyword: "WORLD_NOISE"),
                (Preset: "TransparentLight", NativeKeyword: "ENABLE_WORLD_SPACE_FADE", PortedKeyword: "WORLD_SPACE_FADE"),
                (Preset: "WaterfallMirror", NativeKeyword: "ENABLE_DIRT", PortedKeyword: "DIRT"),
                (Preset: "Standard", NativeKeyword: "ENABLE_RIM_DIM", PortedKeyword: "RIM_DIM"),
                (Preset: "BTSPillar", NativeKeyword: "ENABLE_RIM_DIM", PortedKeyword: "RIM_DIM"),
                (Preset: "InterscopeConcrete", NativeKeyword: "ENABLE_RIM_DIM", PortedKeyword: "RIM_DIM"),
                (Preset: "InterscopeCar", NativeKeyword: "ENABLE_RIM_DIM", PortedKeyword: "RIM_DIM")
            };
            var data = NewDifficulty();
            for (var index = 0; index < requests.Length; index++)
            {
                var request = requests[index];
                var material = new JSONObject
                {
                    ["shader"] = request.Preset,
                    ["shaderKeywords"] = new JSONArray { [0] = request.NativeKeyword }
                };
                data["customData"]["materials"]["alias-" + index] = material;
                AddGeometry(data, "local-named-" + index, "Cube", new JSONString("alias-" + index));
                AddGeometry(data, "local-inline-" + index, "Cube", material.Clone());
            }

            yield return Load(data, version);
            var failures = new List<string>();
            for (var index = 0; index < requests.Length; index++)
            {
                var request = requests[index];
                foreach (var route in new[] { "named", "inline" })
                {
                    var material = Find("local-" + route + "-" + index).MpbController.Renderers.Single().sharedMaterial;
                    var keyword = new LocalKeyword(material.shader, request.PortedKeyword);
                    var label = request.Preset + " " + route + " " + request.NativeKeyword;
                    Assert.That(keyword.isValid, Is.True, label + " has no compiled ported branch positive control");
                    Check(material.IsKeywordEnabled(keyword), label + " was not routed to " + request.PortedKeyword, failures);
                }

            }

            // This old SimpleLit token is not active in the selected native shader. It must not gain another family's behavior.
            var standard = new JSONObject
            {
                ["shader"] = "Standard", ["shaderKeywords"] = new JSONArray { [0] = "ENABLE_HEIGHT_FOG" }
            };
            var scopedData = NewDifficulty();
            AddGeometry(scopedData, "unrecognized-standard-token", "Cube", standard);
            yield return Load(scopedData, version);
            Check(
                !Find("unrecognized-standard-token").MpbController.Renderers.Single().sharedMaterial.IsKeywordEnabled("HEIGHT_FOG"),
                "An inactive native Standard token gained another shader family's height-fog branch",
                failures);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator AuthoredGlobalKeywordsReachSupportedShaderGates() => CheckGlobalKeywords(3);

        [UnityTest]
        public IEnumerator LegacyAuthoredGlobalKeywordsReachSupportedShaderGates() => CheckGlobalKeywords(2);

        private static IEnumerator CheckGlobalKeywords(int version)
        {
            var data = NewDifficulty();
            foreach (var preset in shaderPresets)
            {
                var keywords = JSON.Parse("[\"ENABLE_BLOOM_FOG\"]");
                if (HasMainEffectGate(preset))
                    keywords.Add("MAIN_EFFECT_ENABLED");

                var material = new JSONObject { ["shader"] = preset, ["shaderKeywords"] = keywords };
                data["customData"]["materials"][preset] = material;
                AddGeometry(data, preset + "-global-named", "Cube", new JSONString(preset));
                AddGeometry(data, preset + "-global-inline", "Cube", material.Clone());
            }

            yield return Load(data, version);
            var previousFog = Shader.IsKeywordEnabled("BLOOM_FOG");
            var previousMainEffect = Shader.IsKeywordEnabled("POST_BLOOM");
            try
            {
                Shader.DisableKeyword("BLOOM_FOG");
                Shader.DisableKeyword("POST_BLOOM");
                var failures = new List<string>();
                foreach (var preset in shaderPresets)
                {
                    foreach (var route in new[] { "named", "inline" })
                    {
                        var material = Find(preset + "-global-" + route).MpbController.Renderers.Single().sharedMaterial;
                        var label = preset + " " + route;
                        var fog = new LocalKeyword(material.shader, "BLOOM_FOG");
                        Assert.That(
                            fog.isValid && fog.isOverridable,
                            Is.True,
                            label + " lacks the compiled native global-scope fog adapter");
                        Check(material.IsKeywordEnabled(fog), label + " dropped authored ENABLE_BLOOM_FOG", failures);
                        if (HasMainEffectGate(preset))
                        {
                            var mainEffect = new LocalKeyword(material.shader, "POST_BLOOM");
                            Assert.That(
                                mainEffect.isValid && mainEffect.isOverridable,
                                Is.True,
                                label + " lacks the compiled native global-scope main-effect adapter");
                            Check(material.IsKeywordEnabled(mainEffect), label + " dropped authored MAIN_EFFECT_ENABLED", failures);
                        }

                    }

                }

                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }

            finally
            {
                if (previousFog)
                    Shader.EnableKeyword("BLOOM_FOG");
                else
                    Shader.DisableKeyword("BLOOM_FOG");

                if (previousMainEffect)
                    Shader.EnableKeyword("POST_BLOOM");
                else
                    Shader.DisableKeyword("POST_BLOOM");

            }

        }

        private static bool HasMainEffectGate(string preset) => preset is "Standard" or "OpaqueLight"
            or "TransparentLight" or "BTSPillar" or "InterscopeConcrete" or "InterscopeCar" or "Glowing";

        [UnityTest]
        public IEnumerator NamedStandardUsesAuthoredMaterialDefinition() => CheckNamedStandard(3);

        [UnityTest]
        public IEnumerator LegacyNamedStandardUsesAuthoredMaterialDefinition() => CheckNamedStandard(2);

        private static IEnumerator CheckNamedStandard(int version)
        {
            var data = NewDifficulty();
            data["customData"]["materials"]["standard"] = new JSONObject
            {
                ["shader"] = "Glowing", ["color"] = new Color(0.2f, 0.3f, 0.4f, 0.5f)
            };
            AddGeometry(data, "authored-standard", "Cube", new JSONString("standard"));
            yield return Load(data, version);
            var container = Find("authored-standard");
            Assert.That(
                container.MpbController.Renderers.Single().sharedMaterial.shader.name,
                Is.EqualTo("ChroMapper/Glowing"),
                "Native material names are dictionary keys, including standard.");
            Assert.That(
                Vector4.Distance(
                    container.MpbController.Mpb.GetColor(colorId),
                    new Color(0.2f, 0.3f, 0.4f, 0.5f)),
                Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator NumericTriangleAliasCreatesNativeTriangleWithoutAbortingLoad()
        {
            yield return Load(NewDifficulty(), 3);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/MapEditor/Beatmap/Geometry.prefab");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var request = new BaseEnvironmentEnhancement(
                (JSONNode)new JSONObject
                {
                    ["geometry"] = new JSONObject
                    {
                        ["type"] = "6", ["material"] = new JSONObject { ["shader"] = "Standard" }
                    }
                });
            GeometryContainer container = null;
            System.Exception creationFailure = null;
            var previousIgnore = LogAssert.ignoreFailingMessages;
            try
            {
                // Exercise creation directly so the known undefined primitive cannot wedge the map-load coroutine.
                LogAssert.ignoreFailingMessages = true;
                container = GeometryContainer.SpawnGeometry(request, ref prefab, context, tracks);
            }

            catch (System.Exception exception)
            {
                creationFailure = exception;
            }

            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }

            Assert.That(creationFailure, Is.Null, "Native geometry alias6 must create Triangle: " + creationFailure);
            Assert.That(container, Is.Not.Null);
            var mesh = container.MpbController.Renderers.Single().GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(3));
            Assert.That(mesh.triangles.Length, Is.EqualTo(3));
            Assert.That(mesh.bounds.size, Is.EqualTo(new Vector3(1f, 1f, 0f)));
        }

        [UnityTest]
        public IEnumerator NumericPresetAliasesFollowNativeShaderEnumValues()
        {
            var data = NewDifficulty();
            var aliases = new Dictionary<string, string>
            {
                ["0"] = "Standard", ["1"] = "OpaqueLight", ["2"] = "TransparentLight",
                ["3"] = "BaseWater", ["4"] = "BillieWater", ["5"] = "BTSPillar",
                ["6"] = "InterscopeConcrete", ["7"] = "InterscopeCar",
                ["9"] = "WaterfallMirror", ["10"] = "Glowing"
            };
            foreach (var alias in aliases)
                AddGeometry(
                    data,
                    alias.Key,
                    "Cube",
                    new JSONObject
                    {
                        ["shader"] = alias.Key, ["color"] = Color.white
                    });

            yield return Load(data, 3);
            var failures = new List<string>();
            foreach (var alias in aliases)
            {
                var material = Find(alias.Key).MpbController.Renderers.Single().sharedMaterial;
                var expectedShader = alias.Value == "BaseWater"
                    ? "ChroMapper/Water Lit"
                    : NativePresetAsset(alias.Value).shader.name;
                Check(
                    material.shader.name == expectedShader,
                    "Native preset alias " + alias.Key
                    + " resolved " + material.shader.name + " instead of " + alias.Value,
                    failures);
                if (alias.Value != "BaseWater")
                    CheckPresetKeywords(material, alias.Value, "Native preset alias " + alias.Key, failures);

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator NamedAndInlinePresetsUseNativeMaterialRoutes() => CheckMaterialRoutes(3);

        [UnityTest]
        public IEnumerator LegacyNamedAndInlinePresetsUseNativeMaterialRoutes() => CheckMaterialRoutes(2);

        private IEnumerator CheckMaterialRoutes(int version)
        {
            Settings.Instance.Animations = true;
            var data = NewDifficulty();
            var authoredColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
            foreach (var preset in shaderPresets)
            {
                var material = new JSONObject { ["shader"] = preset, ["color"] = authoredColor };
                data["customData"]["materials"][preset] = material;
                AddGeometry(data, preset + "-named", "Cube", new JSONString(preset));
                AddGeometry(data, preset + "-inline", "Cube", material.Clone());
            }

            yield return Load(data, version);
            var failures = new List<string>();
            foreach (var preset in shaderPresets)
            {
                foreach (var route in new[] { "named", "inline" })
                {
                    var container = Find(preset + "-" + route);
                    var renderer = container.MpbController.Renderers.Single();
                    var actual = renderer.sharedMaterial;
                    var label = preset + " " + route;
                    if (preset == "BaseWater")
                    {
                        Check(actual.shader.name == "ChroMapper/Water Lit", label + " did not use WaterLit", failures);
                        foreach (var keyword in new[] { "FOG", "HEIGHT_FOG", "NORMAL_MAP", "REFLECTION_PROBE", "REFLECTION_PROBE_BOX_PROJECTION" })
                            Check(actual.IsKeywordEnabled(keyword), label + " missing " + keyword, failures);

                    }

                    else
                    {
                        var expected = NativePresetAsset(preset);
                        Assert.That(expected, Is.Not.Null, "Missing test oracle asset for " + preset);
                        Check(
                            actual.shader == expected.shader,
                            label + " resolved " + actual.shader.name
                            + " instead of " + expected.shader.name,
                            failures);
                        CheckPresetKeywords(actual, preset, label, failures);
                        Check(actual.renderQueue == expected.renderQueue, label + " discarded native render queue", failures);
                        foreach (var property in new[]
                        {
                            "_CullMode", "_Smoothness", "_Metallic", "_ReflectionProbeIntensity",
                            "_RimScale", "_RimOffset", "_VertexMode", "_EnableDiffuse", "_EnableSpecular"
                        })
                        {
                            if (expected.HasProperty(property))
                                Check(
                                    actual.HasProperty(property)
                                    && Mathf.Abs(actual.GetFloat(property) - expected.GetFloat(property)) < 0.001f,
                                    label + " discarded preset property " + property,
                                    failures);

                        }

                    }

                    var block = container.MpbController.Mpb;
                    Check(
                        Vector4.Distance(block.GetColor(colorId), authoredColor) < 0.001f,
                        label + " discarded authored color " + block.GetColor(colorId),
                        failures);
                    if (preset == "InterscopeCar")
                    {
                        Check(
                            actual.IsKeywordEnabled("REFLECTION_PROBE") && !actual.IsKeywordEnabled("DIFFUSE"),
                            label + " missing native reflection-only surface",
                            failures);
                        Check(
                            Mathf.Abs(actual.GetFloat("_Smoothness") - 1f) < 0.001f,
                            label + " lost native smoothness",
                            failures);
                    }

                    else if (preset == "InterscopeConcrete")
                    {
                        Check(
                            actual.IsKeywordEnabled("LIGHTMAP") && actual.IsKeywordEnabled("REFLECTION_PROBE")
                            && !actual.IsKeywordEnabled("DIFFUSE"),
                            label + " lost native baked reflection/lightmap inputs",
                            failures);
                        Check(
                            Mathf.Abs(actual.GetFloat("_ReflectionProbeIntensity") - 0.4f) < 0.001f,
                            label + " lost native reflection intensity",
                            failures);
                    }

                }

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator PresetKeywordOverridesReplaceDefaultsWithoutChangingUnrelatedRoutes() => CheckKeywordOverrides(3);

        [UnityTest]
        public IEnumerator LegacyPresetKeywordOverridesReplaceDefaultsWithoutChangingUnrelatedRoutes() => CheckKeywordOverrides(2);

        private IEnumerator CheckKeywordOverrides(int version)
        {
            var data = NewDifficulty();
            foreach (var preset in shaderPresets)
            {
                foreach (var empty in new[] { false, true })
                {
                    var material = new JSONObject
                    {
                        ["shader"] = preset,
                        ["color"] = new Color(0.2f, 0.3f, 0.4f, 0.5f),
                        ["shaderKeywords"] = empty
                            ? new JSONArray()
                            : JSON.Parse("[\"FOG\"]")
                    };
                    AddGeometry(data, preset + "-" + empty, "Cube", material);
                }

            }

            yield return Load(data, version);
            var failures = new List<string>();
            foreach (var preset in shaderPresets)
            {
                foreach (var empty in new[] { false, true })
                {
                    var container = Find(preset + "-" + empty);
                    var material = container.MpbController.Renderers.Single().sharedMaterial;
                    var upgraded = empty && (preset == "Standard" || preset == "BTSPillar");
                    var expectedShader = upgraded
                        ? "ChroMapper/Glowing"
                        : preset == "BaseWater"
                            ? "ChroMapper/Water Lit"
                            : NativePresetAsset(preset).shader.name;
                    var label = preset + " empty=" + empty;
                    Check(material.shader.name == expectedShader, label + " switched to an unrelated shader", failures);
                    Check(
                        material.shaderKeywords.OrderBy(keyword => keyword).SequenceEqual(
                            empty
                                ? new string[0]
                                : new[] { "FOG" }),
                        label + " did not replace default keywords",
                        failures);
                    var alpha = container.MpbController.Mpb.GetColor(colorId).a;
                    Check(
                        Mathf.Abs(
                            alpha - (upgraded
                                ? 0f
                                : 0.5f)) < 0.001f,
                        label + " changed native authored alpha handling",
                        failures);
                }

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator GeneratedPresetsUseNativeOmittedColorAndGlowingDistanceDefaults() => CheckGeneratedDefaults(3);

        [UnityTest]
        public IEnumerator LegacyGeneratedPresetsUseNativeOmittedColorAndGlowingDistanceDefaults() => CheckGeneratedDefaults(2);

        private IEnumerator CheckGeneratedDefaults(int version)
        {
            var data = NewDifficulty();
            var generatedPresets = new[] { "Standard", "OpaqueLight", "TransparentLight", "BaseWater", "Glowing" };
            foreach (var preset in generatedPresets)
                AddGeometry(data, preset, "Cube", new JSONObject { ["shader"] = preset });

            yield return Load(data, version);
            var failures = new List<string>();
            foreach (var preset in generatedPresets)
            {
                var renderer = Find(preset).MpbController.Renderers.Single();
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                // Parametric light shaders receive _Color through their instanced block rather than ShaderLab properties.
                var actualColor = block.HasProperty(colorId) || !renderer.sharedMaterial.HasProperty(colorId)
                    ? block.GetColor(colorId)
                    : renderer.sharedMaterial.GetColor(colorId);
                Check(
                    Vector4.Distance(actualColor, Vector4.zero) < 0.001f,
                    preset + " omitted color resolved " + actualColor + " instead of native clear",
                    failures);
                if (preset == "Glowing")
                    Check(
                        float.IsPositiveInfinity(renderer.sharedMaterial.GetFloat("_FogStartOffset")),
                        "Authored Glowing does not use native unbounded fog start",
                        failures);
                else if (preset == "Standard")
                {
                    Check(
                        Mathf.Abs(renderer.sharedMaterial.GetFloat("_FogStartOffset")) < 0.001f,
                        "Generated Standard lost native SimpleLit fog start0",
                        failures);
                    Check(
                        !renderer.sharedMaterial.IsKeywordEnabled("NOISE_DITHERING"),
                        "Generated Standard enabled dithering absent from native Chroma defaults",
                        failures);
                }

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator PrimitiveTypesHonorNativeMeshesAndCollisionRequests() => CheckPrimitiveTypes(3);

        [UnityTest]
        public IEnumerator LegacyPrimitiveTypesHonorNativeMeshesAndCollisionRequests() => CheckPrimitiveTypes(2);

        private IEnumerator CheckPrimitiveTypes(int version)
        {
            var data = NewDifficulty();
            foreach (var type in geometryTypes)
            {
                foreach (var collision in new[] { false, true })
                {
                    var entry = AddGeometry(
                        data,
                        type + "-" + collision,
                        type,
                        new JSONObject { ["shader"] = "Standard", ["color"] = Color.white });
                    entry["geometry"]["collision"] = collision;
                }

            }

            yield return Load(data, version);
            var failures = new List<string>();
            foreach (var type in geometryTypes)
            {
                foreach (var collision in new[] { false, true })
                {
                    var renderer = Find(type + "-" + collision).MpbController.Renderers.Single();
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var collider = renderer.GetComponent<Collider>();
                    var label = type + " collision=" + collision;
                    Check((collider != null) == collision, label + " has wrong collision state", failures);
                    if (type == "Triangle")
                    {
                        Check(
                            mesh.vertexCount == 3 && mesh.triangles.Length == 3,
                            label + " did not create native triangle mesh",
                            failures);
                        Check(mesh.bounds.size == new Vector3(1f, 1f, 0f), label + " has wrong triangle bounds", failures);
                        if (collision && collider is MeshCollider triangleCollider)
                            Check(triangleCollider.sharedMesh == mesh, label + " collider retained quad mesh", failures);

                    }

                    else
                    {
                        var nativePrimitive = GameObject.CreatePrimitive(System.Enum.Parse<PrimitiveType>(type));
                        try
                        {
                            var expected = nativePrimitive.GetComponent<MeshFilter>().sharedMesh;
                            Check(
                                mesh.vertexCount == expected.vertexCount && mesh.bounds == expected.bounds,
                                label + " changed native primitive mesh",
                                failures);
                            if (collision && collider != null)
                                Check(
                                    collider.GetType() == nativePrimitive.GetComponent<Collider>().GetType(),
                                    label + " changed native collider type",
                                    failures);

                        }

                        finally
                        {
                            Object.DestroyImmediate(nativePrimitive);
                        }

                    }

                }

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator LightPresetsCreatePhysicalLightAndFogWithoutComponentPayload() => CheckAutomaticLights(3);

        [UnityTest]
        public IEnumerator LegacyLightPresetsCreatePhysicalLightAndFogWithoutComponentPayload() => CheckAutomaticLights(2);

        private IEnumerator CheckAutomaticLights(int version)
        {
            var data = NewDifficulty();
            var lightPresets = new[] { "OpaqueLight", "TransparentLight", "BillieWater" };
            foreach (var preset in lightPresets)
                AddGeometry(data, preset, "Cube", new JSONObject { ["shader"] = preset });

            yield return Load(data, version);
            var failures = new List<string>();
            foreach (var preset in lightPresets)
            {
                var renderer = Find(preset).MpbController.Renderers.Single();
                var controller = renderer.GetComponent<ParametricBloomFogLightController>();
                if (controller == null)
                {
                    failures.Add(preset + " omitted native automatic light component");
                    continue;
                }

                Check(
                    controller.IsPhysical && controller.BoxLight != null && controller.BloomFog != null,
                    preset + " did not wire physical light and fog",
                    failures);
                var color = new Color(0.6f, 0.3f, 0.9f, 0.7f);
                controller.SetColor(color);
                controller.Refresh();
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Check(
                    Vector4.Distance(block.GetColor(colorId), color) < 0.001f,
                    preset + " light color did not reach primitive rendering",
                    failures);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static IEnumerator Load(JSONNode data, int version)
        {
            yield return TestUtils.ReloadMap(
                version,
                version == 2
                    ? ToLegacyDifficulty(data)
                    : data,
                environmentName: "GagaEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            yield return null;
            yield return null;
        }

        private static JSONNode ToLegacyDifficulty(JSONNode data)
        {
            var materials = new JSONObject();
            foreach (var material in data["customData"]["materials"])
                materials[material.Key] = ToLegacyMaterial(material.Value);

            var environment = new JSONArray();
            foreach (var entry in data["customData"]["environment"].Children)
            {
                var geometry = entry["geometry"];
                var material = geometry["material"];
                var legacyGeometry = new JSONObject
                {
                    ["_type"] = geometry["type"].Clone(),
                    ["_material"] = material.IsObject
                        ? ToLegacyMaterial(material)
                        : material.Clone()
                };
                if (geometry.HasKey("collision"))
                    legacyGeometry["_collision"] = geometry["collision"].Clone();

                environment.Add(
                    new JSONObject
                    {
                        ["_track"] = entry["track"].Clone(),
                        ["_geometry"] = legacyGeometry
                    });
            }

            return new JSONObject
            {
                ["_version"] = "2.6.0",
                ["_customData"] = new JSONObject { ["_materials"] = materials, ["_environment"] = environment }
            };
        }

        private static JSONNode ToLegacyMaterial(JSONNode material)
        {
            var legacy = new JSONObject();
            foreach (var field in material)
                legacy["_" + field.Key] = field.Value.Clone();

            return legacy;
        }

        private static JSONNode NewDifficulty() => new JSONObject
        {
            ["version"] = "3.3.0",
            ["customData"] = new JSONObject
            {
                ["materials"] = new JSONObject(),
                ["environment"] = new JSONArray()
            }
        };

        private static JSONNode AddGeometry(JSONNode data, string track, string type, JSONNode material)
        {
            var entry = new JSONObject
            {
                ["track"] = track,
                ["geometry"] = new JSONObject { ["type"] = type, ["material"] = material }
            };
            data["customData"]["environment"].AsArray.Add(entry);
            return entry;
        }

        private static GeometryContainer Find(string track) => Object.FindObjectsByType<GeometryContainer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None)
            .Single(container => container.EnvironmentEnhancement.Track == track);

        private static Material NativePresetAsset(string preset)
        {
            var path = preset switch
            {
                "Standard" => "Custom/Standard",
                "OpaqueLight" => "Custom/LightOpaque",
                "TransparentLight" => "Custom/LightTransparent",
                "Glowing" => "Custom/Glowing",
                "BTSPillar" => "BTS/BTSDarkEnvironmentWithHeightFog",
                "BillieWater" => "Billie/WaterfallFalling",
                "WaterfallMirror" => "Billie/WaterfallMirror",
                "InterscopeConcrete" => "Interscope/Concrete2",
                "InterscopeCar" => "Interscope/Car",
                _ => throw new System.ArgumentOutOfRangeException(nameof(preset))
            };
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Environments/" + path + ".mat");
        }

        private static void Check(bool condition, string message, List<string> failures)
        {
            if (!condition)
                failures.Add(message);

        }

        private static void CheckPresetKeywords(Material material, string preset, string label, List<string> failures)
        {
            if (preset == "Standard")
            {
                // These are the active CM equivalents of Chroma's generated SimpleLit defaults.
                foreach (var keyword in new[]
                {
                    "DIFFUSE", "FOG", "HEIGHT_FOG", "MULTIPLY_REFLECTIONS",
                    "REFLECTION_PROBE_BOX_PROJECTION", "SPECULAR"
                })
                {
                    Check(material.IsKeywordEnabled(keyword), label + " missing native " + keyword, failures);
                }

                Check(!material.IsKeywordEnabled("NOISE_DITHERING"), label + " introduced native-disabled dithering", failures);
                return;
            }

            Check(
                material.enabledKeywords.Select(keyword => keyword.name).OrderBy(keyword => keyword).SequenceEqual(
                    NativePresetAsset(preset).enabledKeywords.Select(keyword => keyword.name).OrderBy(keyword => keyword)),
                label + " discarded native preset keyword inputs actual=[" + string.Join(",", material.shaderKeywords)
                + "] expected=[" + string.Join(",", NativePresetAsset(preset).shaderKeywords) + "]",
                failures);
        }

        [UnityTearDown]
        public IEnumerator RestoreSharedMap()
        {
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

    }
}
