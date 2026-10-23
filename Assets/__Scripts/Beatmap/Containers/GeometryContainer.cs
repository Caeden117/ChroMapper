using Beatmap.Animations;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beatmap.Containers
{
    public class GeometryContainer : ObjectContainer
    {
        private static Mesh triangleMesh;

        public override BaseObject ObjectData
        {
            get => EnvironmentEnhancement;
            set => EnvironmentEnhancement = (BaseEnvironmentEnhancement)value;
        }

        public BaseEnvironmentEnhancement EnvironmentEnhancement;
        public ObjectAnimator MaterialAnimator;

        internal BaseMaterial MaterialDefinition;

        internal enum GeometryType
        {
            Sphere,
            Capsule,
            Cylinder,
            Cube,
            Plane,
            Quad,
            Triangle
        }

        public override void UpdateGridPosition()
        {
        }

        public static GeometryContainer SpawnGeometry(
            BaseEnvironmentEnhancement eh,
            ref GameObject prefab,
            BeatmapRuntimeContext context,
            TracksManager tracksManager)
        {
            Regex lookupRegex = null;
            if (eh.Geometry == null && eh.LookupMethod == EnvironmentLookupMethod.Regex)
            {
                try
                {
                    lookupRegex = new Regex(eh.ID,
                        RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.Compiled);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError($"Error processing environment data for: ID [\"{eh.ID}\"] using method [Regex]. "
                        + $"{BeatmapLogContext.Describe(eh)}\n{exception.Message}");
                    return null;
                }
            }
            var container = Instantiate(prefab).GetComponent<GeometryContainer>();
            if (context.Descriptor != null)
                SceneManager.MoveGameObjectToScene(container.gameObject, context.Descriptor.gameObject.scene);
            container.Animator.Context = context;
            container.Animator.TracksManager = tracksManager;

            // Material animation needs a separate ObjectAnimator because AttachToMaterial resets state used
            // by transform animation. Give it a child transform so position properties on a material track
            // cannot move the geometry itself or access a missing LocalTarget. Keep it disabled until a
            // material is attached.
            var materialAnimatorTarget = new GameObject("AnimationTarget");
            materialAnimatorTarget.layer = container.gameObject.layer;
            materialAnimatorTarget.transform.SetParent(container.transform, false);
            var materialAnimator = materialAnimatorTarget.AddComponent<ObjectAnimator>();
            materialAnimator.LocalTarget = materialAnimatorTarget.transform;
            materialAnimator.WorldTarget = materialAnimatorTarget.transform;
            materialAnimator.Context = context;
            materialAnimator.TracksManager = tracksManager;
            materialAnimator.enabled = false;
            container.MaterialAnimator = materialAnimator;

            container.EnvironmentEnhancement = eh;

            if (eh.Geometry != null)
            {
                // Continue with geometry generation if the Geometry object is defined
                if (!GeneratePrimitiveGeometry(container, eh, context, tracksManager))
                {
                    DestroyImmediate(container.gameObject);
                    return null;
                }

                // Primitive geometry is owned by this container, so its existing single-target animator remains valid.
                container.Animator.AttachToGeometry(eh);
            }
            else
            {
                // EnvironmentEnhancementWith*Track* regression tests require OEM hierarchies to remain intact; each
                // matched object receives a data-only track controller instead of sharing the container transform.
                GenerateEnvironmentEnhancement(container, eh, context, tracksManager, lookupRegex);
                container.Animator.enabled = false;
            }

            container.gameObject.SetActive(eh.Geometry == null || eh.Active == null || eh.Active.AsBool);
            container.UpdateCollisionGroups();

            return container;
        }

        private static bool GeneratePrimitiveGeometry(
            GeometryContainer container,
            BaseEnvironmentEnhancement eh,
            BeatmapRuntimeContext ctx,
            TracksManager tracksManager)
        {
            if (!Enum.TryParse(eh.Geometry[eh.GeometryKeyType].Value, out GeometryType geometryType)
                || !Enum.IsDefined(typeof(GeometryType), geometryType))
            {
                Debug.LogError($"Invalid geometry type '{(string)eh.Geometry[eh.GeometryKeyType]}'! {BeatmapLogContext.Describe(eh)}");
                return false;
            }

            container.MaterialDefinition = GeometryAppearanceSO.ResolveMaterial(eh);
            var shader = GeometryAppearanceSO.ResolveShader(container.MaterialDefinition, eh);
            var primitiveType = geometryType == GeometryType.Triangle
                ? PrimitiveType.Quad
                : (PrimitiveType)geometryType;
            var shape = GameObject.CreatePrimitive(primitiveType);
            shape.transform.SetParent(container.transform);
            shape.layer = container.gameObject.layer;
            container.MpbController.Renderers = new() { shape.GetComponent<MeshRenderer>() };

            var collider = shape.GetComponent<Collider>();
            var collisionKey = Settings.Instance.MapVersion == 2
                ? "_collision"
                : "collision";
            var collision = eh.Geometry.GetValueOrDefault(collisionKey, null)?.AsBool ?? false;
            // Physical colliders shorten preview light beams. Selection uses the container's separate IntersectionCollider.
            if (!collision && collider != null)
            {
                DestroyImmediate(collider);
                collider = null;
            }

            if (geometryType == GeometryType.Triangle)
            {
                if (triangleMesh == null)
                    triangleMesh = CreateTriangleMesh();

                shape.GetComponent<MeshFilter>().sharedMesh = triangleMesh;
                if (collision && collider is MeshCollider meshCollider)
                    meshCollider.sharedMesh = triangleMesh;

            }

            var descriptor = ctx.Descriptor;
            var explicitLight = eh.Components?.HasKey("ILightWithId") ?? false;
            // Chroma's light presets own their physical and fog light components even without a components payload.
            ParametricBloomFogLightController controller = null;
            if (GeometryAppearanceSO.IsLightType(shader) || explicitLight)
            {
                shape.SetActive(false);
                controller = shape.AddComponent<ParametricBloomFogLightController>();
                var light = shape.AddComponent<ParametricBoxLight>();
                light.UpdateTransform = false;
                light.Renderer = container.MpbController.Renderers[0];
                controller.BoxLight = light;

                var bf = shape.AddComponent<BloomFogObject>();
                controller.BloomFog = bf;

                // Native generated lights keep the unregistered ID -1 until ILightWithId supplies a group.
                controller.Type = eh.LightType ?? (explicitLight
                    ? 0
                    : -1);
                if (descriptor != null && controller.Type >= 0)
                    descriptor.Register(controller, eh.LightID);

                foreach (var name in eh.TrackNames)
                    tracksManager.RegisterBloomFogLights(name, new[] { controller });
                shape.SetActive(true);
            }

            if (eh.Components?.HasKey("TubeBloomPrePassLight") ?? false)
            {
                var ppLight = eh.Components["TubeBloomPrePassLight"];
                if (controller != null && ppLight["colorAlphaMultiplier"] != null)
                    controller.SetColorAlphaMultiplier(ppLight["colorAlphaMultiplier"]);

                if (controller != null && ppLight["bloomFogIntensityMultiplier"] != null)
                    controller.SetBloomFogIntensityMultiplier(ppLight["bloomFogIntensityMultiplier"]);

            }

            return true;
        }

        private static void GenerateEnvironmentEnhancement(
            GeometryContainer container,
            BaseEnvironmentEnhancement eh,
            BeatmapRuntimeContext ctx,
            TracksManager tracksManager,
            Regex lookupRegex)
        {
            // Get descriptor of currently loaded environment
            var descriptor = ctx.Descriptor;

            // No environment? No enhancement.
            if (descriptor == null) return;

            // Use the ID / Lookup method to find our target marker
            var chromaIDMarkers = descriptor.ChromaIDMarkers;
            // Yes, all the matching IDs, don't ask me why
            var targetObjects = chromaIDMarkers.Where(marker => FindMarker(marker, eh, lookupRegex)).Select(x => (x, x)).ToList();

            // Mirror Chroma's lookup warning format so missing enhancements can be compared between the editor
            // and game.
            if (targetObjects.Count == 0)
            {
                Debug.LogWarning(
                    $"Environment enhancement ID [\"{eh.ID}\"] using method [{eh.LookupMethod:G}] found nothing. {BeatmapLogContext.Describe(eh)}");
            }

            // We need to handle duplicates if defined!
            if (eh.Duplicate != null)
            {
                // Chroma precheck this and throws, but we don't care but we also do not want to destroy our PC
                // Also if this value is a lil inaccurate, feel free to change
                if (targetObjects.Count > 100)
                {
                    Debug.LogError(
                        $"Extreme value reached, you are attempting to duplicate over 100 objects! Environment enhancements stopped. {BeatmapLogContext.Describe(eh)}");
                    return;
                }

                // Because we are duplicating, we make a new target list
                var newTargetObjects = new List<(ChromaIDMarker, ChromaIDMarker)>();
                var duplicates = eh.Duplicate.Value;
                foreach (var (original, _) in targetObjects)
                {
                    for (var i = 0; i < duplicates; i++)
                    {
                        // Instantiate the clone as a root so it can be moved into the original environment
                        // scene before parenting. Parenting during Instantiate can place it in the mapper
                        // scene, where it would survive an environment reload.
                        var duplicateObject = Instantiate(original.gameObject);
                        SceneManager.MoveGameObjectToScene(duplicateObject, original.gameObject.scene);
                        duplicateObject.transform.SetParent(original.transform.parent, true);
                        var duplicate = duplicateObject.GetComponent<ChromaIDMarker>();
                        var originalParentId = duplicate.ChromaID;
                        var nameStart = original.ChromaID.Length - original.name.Length;
                        var siblingIndexStart = original.ChromaID.LastIndexOf('[', nameStart - 1);
                        var parentId = original.ChromaID[..siblingIndexStart];
                        duplicate.ChromaID = parentId + $"[{duplicateObject.transform.GetSiblingIndex()}]" + duplicate.name;
                        // GetComponentsInChildren includes the root marker, whose final ChromaID was assigned
                        // above. Rewrite only child IDs to avoid appending a second "(Clone)" to the root.
                        foreach (var childMarker in duplicateObject.GetComponentsInChildren<ChromaIDMarker>())
                        {
                            if (childMarker != duplicate)
                            {
                                childMarker.ChromaID =
                                    childMarker.ChromaID.Replace(originalParentId, duplicate.ChromaID);
                            }

                            descriptor.ChromaIDMarkers.Add(childMarker);
                        }

                        newTargetObjects.Add((original, duplicate));
                    }
                }

                targetObjects = newTargetObjects;
            }

            // Apply fog overrides only when the matched objects contain the fog component. Its preview state
            // lives on the environment descriptor root.
            var ownsFog = targetObjects.Any(pair =>
                pair.Item2.transform == descriptor.transform
                || descriptor.transform.IsChildOf(pair.Item2.transform));
            if (!ownsFog && (eh.Components?.HasKey("BloomFogEnvironment") ?? false))
                Debug.LogWarning($"BloomFogEnvironment enhancement ID [{eh.ID}] matched no fog component. {BeatmapLogContext.Describe(eh)}");
            if (ownsFog && (eh.Components?.HasKey("BloomFogEnvironment") ?? false))
            {
                var bloomFog = eh.Components["BloomFogEnvironment"];
                if (bloomFog["attenuation"] != null) descriptor.BloomFogParams.Attenuation = bloomFog["attenuation"];
                if (bloomFog["offset"] != null) descriptor.BloomFogParams.Offset = bloomFog["offset"];
                if (bloomFog["startY"] != null) descriptor.BloomFogParams.StartY = bloomFog["startY"];
                if (bloomFog["height"] != null) descriptor.BloomFogParams.Height = bloomFog["height"];

                ctx.NotifyBloomFogParamsChanged();
            }

            // Cache the map-version decision once because every matched marker uses the same V2 unit conversion.
            var v2 = tracksManager.IsV2Map;
            var adjustScale = v2
                ? BeatmapConstant.LaneSize
                : 1f;
            // Apply enhancements to each target object (original or duplicates)
            foreach (var (original, target) in targetObjects)
            {
                if (eh.Active != null) target.gameObject.SetActive(eh.Active.AsBool);

                var boxLight = eh.Position != null || eh.LocalPosition != null || eh.Scale != null || eh.TrackNames.Count > 0
                    ? target.GetComponentInChildren<ParametricBoxLight>(true)
                    : null;
                if (boxLight != null)
                    boxLight.InitIfNeeded();

                // Chroma applies authored enhancement transforms before attaching a track and never reparents the
                // matched object, which preserves nested BigTrackLaneRing renderers in all four parity cases.
                if (eh.Scale != null) target.transform.localScale = eh.Scale.Value;
                if (eh.LocalPosition != null)
                    target.transform.localPosition = eh.LocalPosition.Value * adjustScale;
                else if (eh.Position != null) target.transform.position = eh.Position.Value * adjustScale;
                if (eh.LocalRotation != null)
                    target.transform.localRotation = Quaternion.Euler(eh.LocalRotation.Value);
                else if (eh.Rotation != null) target.transform.rotation = Quaternion.Euler(eh.Rotation.Value);

                // Capture the box mesh after applying the enhancement so later light refreshes preserve only
                // the position and scale overrides.
                if (boxLight != null)
                {
                    if (eh.Position != null || eh.LocalPosition != null)
                        boxLight.CaptureAuthoredPosition();
                    if (eh.Scale != null)
                        boxLight.CaptureAuthoredScale();
                }

                // A track with no transform property must be inert; a present property is applied directly to each
                // matched transform by its own animator, matching Chroma without flattening the OEM hierarchy.
                if (eh.TrackNames.Count > 0)
                {
                    foreach (var name in eh.TrackNames)
                        tracksManager.RegisterBloomFogLights(name,
                            target.GetComponentsInChildren<ParametricBloomFogLightController>(true));
                    var animator = tracksManager.CreateEnvironmentAnimator(container.gameObject, target.transform);
                    animator.Context = ctx;
                    animator.TracksManager = tracksManager;
                    animator.AttachToEnvironmentObject(
                        target.transform,
                        eh.TrackNames,
                        v2,
                        boxLight);
                    if (target.transform == descriptor.transform
                        || descriptor.transform.IsChildOf(target.transform))
                    {
                        foreach (var name in eh.TrackNames)
                        {
                            tracksManager.BindFogComponentTarget(name);
                        }
                    }
                }

                if (eh.Duplicate != null) HandleDuplicateComponents(original.transform, target.transform);

                foreach (var controller in target.GetComponentsInChildren<LightController>(true))
                {
                    var customized = eh.LightType.HasValue || eh.LightID.HasValue;
                    if (eh.Duplicate != null || customized)
                    {
                        if (eh.Duplicate == null) descriptor.Unregister(controller);
                        if (controller.Kind == LightController.LightKind.Basic)
                            controller.Type = eh.LightType ?? controller.Type;

                        descriptor.Register(controller, eh.LightID);
                    }

                    if (eh.Components?.HasKey("TubeBloomPrePassLight") ?? false)
                    {
                        var ppLight = eh.Components["TubeBloomPrePassLight"];
                        if (controller is not ParametricBloomFogLightController pbflc) continue;
                        if (ppLight["colorAlphaMultiplier"] != null)
                            pbflc.SetColorAlphaMultiplier(ppLight["colorAlphaMultiplier"]);
                        if (ppLight["bloomFogIntensityMultiplier"] != null)
                            pbflc.SetBloomFogIntensityMultiplier(ppLight["bloomFogIntensityMultiplier"]);
                    }
                }

            }

            return;

            void HandleDuplicateComponents(Transform original, Transform target)
            {
                // var originalComponents = original.GetComponents<MonoBehaviour>();
                var targetComponents = target.GetComponents<MonoBehaviour>();

                for (var i = 0; i < targetComponents.Length; i++)
                {
                    // var originalComponent = originalComponents[i];
                    var targetComponent = targetComponents[i];

                    switch (targetComponent)
                    {
                        case TrackLaneRing trackLaneRing:
                            // var originalTrackLaneRing = (TrackLaneRing)originalComponent;
                            // _trackLaneRingOffset.CopyRing(originalTrackLaneRing, trackLaneRing);

                            if (trackLaneRing.ParentManager != null && !trackLaneRing.ParentManager.SpawnAsChildren)
                                trackLaneRing.ParentManager.Rings.Add(trackLaneRing);

                            break;

                        case StateManager<BaseEvent> effect:
                            descriptor.BasicEventEffectManager.Register(effect.ID, effect);
                            break;
                    }
                }

                foreach (Transform newTarget in target)
                {
                    var index = newTarget.GetSiblingIndex();
                    // in the future, we might need the original
                    HandleDuplicateComponents(newTarget, newTarget);
                }
            }
        }

        private static bool FindMarker(ChromaIDMarker marker, BaseEnvironmentEnhancement eh, Regex lookupRegex) =>
            eh.LookupMethod switch
            {
                EnvironmentLookupMethod.Exact => marker.ChromaID == eh.ID,
                EnvironmentLookupMethod.StartsWith => marker.ChromaID.StartsWith(eh.ID),
                EnvironmentLookupMethod.EndsWith => marker.ChromaID.EndsWith(eh.ID),
                EnvironmentLookupMethod.Contains => marker.ChromaID.Contains(eh.ID),
                EnvironmentLookupMethod.Regex => lookupRegex.IsMatch(marker.ChromaID),
                _ => throw new ArgumentException($"Unknown lookup method {eh.LookupMethod}"),
            };

        /// <summary>
        /// https://answers.unity.com/questions/1594750/is-there-a-premade-triangle-asset.html
        /// </summary>
        private static Mesh CreateTriangleMesh()
        {
            Vector3[] vertices = { new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(0f, 0.5f, 0) };
            Vector2[] uv = { new Vector3(0, 0), new Vector3(1, 0), new Vector3(0.5f, 1) };
            int[] triangles = { 0, 1, 2 };

            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
