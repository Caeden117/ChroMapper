using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "CENSORED!!", mapped by Saltyfish (BeatSaver ID: 4b3da).
    // CensoredFullMapVisibilityTest is a read-only diagnostic on the complete shipped
    // ExpertPlusStandard.dat of CENSORED!! (195 BPM, FitBeatEnvironment) to localize the user's
    // hidden-geometry report: it dumps the authored enhancement/custom-event inventory and the
    // rendered geometry/frustum state, then verifies both prisms' hide/flash cycle and pixels.
    // Explicit because it reloads the full environment on every run.
    public class CensoredFullMapVisibilityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private const string FullMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
            "4b3da (CENSORED!! - Saltyfish)/ExpertPlusStandard.dat";

        private static readonly string[] SuspiciousNameParts =
            { "cens", "text", "flash", "block", "prism", "hide" };

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;

            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FullMapPath)),
                beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment",
                songLengthSeconds: 200);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit]
        public IEnumerator DumpOpeningGeometryAndEvents()
        {
            var map = BeatSaberSongContainer.Instance.Map;
            var report = new System.Text.StringBuilder();

            report.AppendLine($"=== authored inventory ===");
            report.AppendLine($"environmentEnhancements={map.EnvironmentEnhancements.Count}");
            var geometryEnhancements = map.EnvironmentEnhancements.Where(e => e.Geometry != null).ToList();
            report.AppendLine($"geometryEnhancements={geometryEnhancements.Count}");
            foreach (var group in geometryEnhancements.GroupBy(e => e.Track))
            {
                report.AppendLine($"  track={(group.Key ?? "<null>")} count={group.Count()}");
                foreach (var enh in group.Take(3))
                {
                    report.AppendLine($"    sample geometry={enh.Geometry} scale={(enh.Scale?.ToString() ?? "null")} " +
                        $"position={(enh.Position?.ToString() ?? "null")} active={(enh.Active?.ToString() ?? "null")}");
                }
            }

            var earlyEvents = map.CustomEvents.Where(e => e.JsonTime <= 8f).ToList();
            report.AppendLine($"=== custom events beat<=8: {earlyEvents.Count} ===");
            for (var i = 0; i < earlyEvents.Count; i++)
            {
                var evt = earlyEvents[i];
                var data = evt.Data?.ToString() ?? "null";
                if (data.Length > 500) data = data.Substring(0, 500) + "...";
                report.AppendLine($"  [{i}] beat={evt.JsonTime} type={evt.Type} data={data}");
            }

            Debug.Log(report.ToString());
            Assert.That(geometryEnhancements, Is.Not.Empty,
                "Diagnostic precondition: the map should author geometry enhancements.");

            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            var colorId = Shader.PropertyToID("_Color");
            var suspicious = new System.Text.StringBuilder();
            var visibilityFailures = new List<string>();

            var allGeometries = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c != null)
                .ToList();
            var redcenContainer = allGeometries.FirstOrDefault(
                c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "redcen_10_9");
            // The black backing prism lives on track theaaaa (parented under back -> censson),
            // not on a "back" geometry track.
            var theaaaaContainer = allGeometries.FirstOrDefault(
                c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "theaaaa");
            Assert.That(redcenContainer, Is.Not.Null,
                "Diagnostic precondition: geometry on track redcen_10_9 should exist.");
            Assert.That(theaaaaContainer, Is.Not.Null,
                "Diagnostic precondition: geometry on track theaaaa should exist.");

            // Authored flash: censson.position toggles the whole parented group between
            // z=4.9 (visible; leaves land near world z=10.9) and z=-6969 (hidden). Hidden at
            // 0.05/2/4/8, visible at 99.25 (open at 99), hidden again at 100.05 (close at 100),
            // then reverse seeks confirm the hide applies in both directions.
            foreach (var (beatLabel, beat, expectVisible) in new[]
            {
                ("0.05", 0.05f, false), ("2", 2f, false), ("4", 4f, false), ("8", 8f, false),
                ("99.25", 99.25f, true), ("100.05", 100.05f, false),
                ("99.25->", 99.25f, true), ("->4", 4f, false), ("->2", 2f, false),
            })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;

                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                var geometries = Object.FindObjectsByType<GeometryContainer>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(c => c != null)
                    .ToList();
                var renderers = geometries
                    .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                    .ToList();
                var inFrustum = renderers
                    .Where(r => r != null && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                    .ToList();

                var beatLog = new System.Text.StringBuilder();
                beatLog.AppendLine($"=== beat {beatLabel}: camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} | " +
                    $"geometryContainers={geometries.Count} renderers={renderers.Count} " +
                    $"inFrustum={inFrustum.Count} ===");
                foreach (var group in geometries.GroupBy(c => c.EnvironmentEnhancement?.Track))
                {
                    beatLog.AppendLine($"  group track={(group.Key ?? "<null>")} containers={group.Count()}");
                }
                foreach (var r in inFrustum.Take(30))
                {
                    var owner = r.GetComponentInParent<GeometryContainer>();
                    var enh = owner == null ? null : owner.EnvironmentEnhancement;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    beatLog.AppendLine($"  inFrustum name={r.name} track={(enh == null ? "null" : enh.Track)} " +
                        $"authoredScale={(enh == null || !enh.Scale.HasValue ? "null" : enh.Scale.Value.ToString())} " +
                        $"authoredPos={(enh == null || !enh.Position.HasValue ? "null" : enh.Position.Value.ToString())} " +
                        $"runtimePos={r.transform.position} runtimeScale={r.transform.localScale} " +
                        $"bounds={r.bounds} active={r.gameObject.activeInHierarchy} " +
                        $"enabled={r.enabled} mpbColor={mpb.GetColor(colorId)}");
                }
                Debug.Log(beatLog.ToString());

                // Focused probe: beat 0 authors AnimateTrack censson.position=[0,0,-6969] with
                // AssignTrackParent putting back and redcen (parents of redcen_*) under censson,
                // while redcen.position=[-3.2,-1,6] and back.position=[0,0.1,6.001].
                var focused = new System.Text.StringBuilder();
                foreach (var trackName in new[] { "censson", "redcen", "back", "gone", "a" })
                {
                    var ta = tracksManager == null ? null : tracksManager.GetAnimationTrack(trackName);
                    if (ta == null || ta.Track == null)
                    {
                        focused.AppendLine($"  track {trackName}: <no TrackAnimator>");
                        continue;
                    }

                    var self = ta.Track.SelfTransform;
                    var parent = ta.Track.ObjectParentTransform;
                    var animator = ta.Animator;
                    focused.AppendLine(
                        $"  track {trackName}: selfLocal={self.localPosition} selfWorld={self.position} " +
                        $"parentLocal={parent.localPosition} parentWorld={parent.position} " +
                        $"parentName={(parent.parent == null ? "null" : parent.parent.name)} " +
                        $"parentLossyScale={parent.lossyScale} " +
                        $"worldPosCount={(animator == null ? "-" : animator.WorldPosition.Count.ToString())} " +
                        $"offsetCount={(animator == null ? "-" : animator.OffsetPosition.Count.ToString())} " +
                        $"props=[{string.Join(",", ta.AnimatedProperties.Keys)}]");
                }
                AppendGeometryAncestry(focused, "redcen_10_9", redcenContainer);
                AppendGeometryAncestry(focused, "theaaaa", theaaaaContainer);

                // Leaf-track probe: find which aggregator/target writes the z=6 override that
                // cancels the inherited -6969 hide.
                foreach (var leaf in new[] { "redcen_10_9", "theaaaa" })
                {
                    var ta = tracksManager == null ? null : tracksManager.GetAnimationTrack(leaf);
                    if (ta == null || ta.Track == null)
                    {
                        focused.AppendLine($"  leaf {leaf}: <no TrackAnimator>");
                        continue;
                    }
                    var animator = ta.Animator;
                    focused.AppendLine(
                        $"  leaf {leaf}: selfLocal={ta.Track.SelfTransform.localPosition} " +
                        $"selfWorld={ta.Track.SelfTransform.position} " +
                        $"parentLocal={ta.Track.ObjectParentTransform.localPosition} " +
                        $"parentWorld={ta.Track.ObjectParentTransform.position} " +
                        $"props=[{string.Join(",", ta.AnimatedProperties.Keys)}] " +
                        $"animator={DescribeAnimator(animator)}");
                }
                foreach (var (label, container) in new[]
                {
                    ("redcen_10_9", redcenContainer),
                    ("theaaaa", theaaaaContainer),
                })
                {
                    focused.AppendLine($"  geoAnimator {label}: {DescribeAnimator(container.Animator)}");
                }
                Debug.Log($"=== beat {beatLabel}: focused track state ===\n{focused}");

                foreach (var (label, container) in new[]
                {
                    ("redcen_10_9", redcenContainer),
                    ("theaaaa", theaaaaContainer),
                })
                {
                    var live = container.GetComponentsInChildren<Renderer>(true)
                        .Where(r => r.enabled && r.name != "SelectionMesh")
                        .ToList();
                    var rendererStates = live.Select(r =>
                        $"{r.name} bounds={r.bounds} " +
                        $"inFrustum={GeometryUtility.TestPlanesAABB(planes, r.bounds)}");
                    if (expectVisible)
                    {
                        // Flash-open must place an enabled renderer actually in view near the
                        // authored world z=4.9+6=10.9, not merely keep controller state.
                        if (live.Count == 0 || !live.Any(r =>
                                GeometryUtility.TestPlanesAABB(planes, r.bounds)
                                && Mathf.Abs(r.bounds.center.z - 10.9f) <= 1f))
                        {
                            visibilityFailures.Add(
                                $"beat {beatLabel} {label} should be visible after authored " +
                                $"censson flash-open z=4.9: " +
                                $"renderers={string.Join("|", rendererStates)}");
                        }
                    }
                    else if (live.Count > 0 && live.Any(r =>
                            GeometryUtility.TestPlanesAABB(planes, r.bounds)
                            && r.bounds.center.z > -1000f))
                    {
                        visibilityFailures.Add(
                            $"beat {beatLabel} {label} should be hidden by authored censson " +
                            $"z=-6969: renderers={string.Join("|", rendererStates)}");
                    }
                }
            }

            Assert.That(visibilityFailures, Is.Empty,
                string.Join("\n", visibilityFailures));

            foreach (var c in Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var enh = c.EnvironmentEnhancement;
                var names = new[] { c.name, enh == null ? null : enh.ID, enh == null ? null : enh.Track }
                    .Where(n => n != null)
                    .ToList();
                if (names.Any(n => SuspiciousNameParts.Any(p =>
                        n.ToLowerInvariant().Contains(p))))
                {
                    var r = c.GetComponentInChildren<Renderer>(true);
                    suspicious.AppendLine($"  suspicious name={c.name} id={(enh == null ? "null" : enh.ID)} " +
                        $"track={(enh == null ? "null" : enh.Track)} " +
                        $"active={c.gameObject.activeInHierarchy} " +
                        $"rendererBounds={(r == null ? "none" : r.bounds.ToString())}");
                }
            }
            if (suspicious.Length > 0)
            {
                Debug.Log("=== suspicious-name geometry (cens/text/flash/block/prism/hide), " +
                    "frustum-agnostic ===\n" + suspicious);
            }

            // A/B pixel proof at the authored flash beats: capture the Playing camera once with
            // every redcen_*/theaaaa renderer live and once with them disabled, holding the beat
            // fixed so lighting cannot differ between the two frames.
            var censorGroup = allGeometries
                .Where(c => c.EnvironmentEnhancement != null
                    && (c.EnvironmentEnhancement.Track != null
                        && (c.EnvironmentEnhancement.Track.StartsWith("redcen_")
                            || c.EnvironmentEnhancement.Track == "theaaaa")))
                .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && r.name != "SelectionMesh")
                .ToList();
            Assert.That(censorGroup, Is.Not.Empty,
                "Diagnostic precondition: redcen_*/theaaaa renderers should exist.");

            atsc.MoveToJsonTime(99.25f);
            yield return null;
            yield return null;
            var openDelta = CaptureGroupDelta(camera, censorGroup);
            atsc.MoveToJsonTime(100.05f);
            yield return null;
            yield return null;
            var closeDelta = CaptureGroupDelta(camera, censorGroup);

            Debug.Log($"=== A/B pixel proof: beat99.25 maxDiff={openDelta.MaxDiff} " +
                $"pixels>={openDelta.Count} | beat100.05 maxDiff={closeDelta.MaxDiff} " +
                $"pixels={closeDelta.Count} ===");
            Assert.That(closeDelta.Count, Is.EqualTo(0),
                "The censored group is banished at beat 100.05 and must contribute no pixels.");
            Assert.That(openDelta.Count, Is.GreaterThanOrEqualTo(20),
                "The beat-99.25 flash-open should draw the censored group into the playing view.");
        }

        private static (int MaxDiff, int Count) CaptureGroupDelta(
            Camera camera, List<Renderer> group)
        {
            var previousTarget = camera.targetTexture;
            var sceneTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var withGroup = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var withoutGroup = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var original = group.Select(r => r.enabled).ToList();
            try
            {
                camera.targetTexture = sceneTexture;
                camera.Render();
                ReadRenderTexture(sceneTexture, withGroup);

                for (var i = 0; i < group.Count; i++) group[i].enabled = false;
                try
                {
                    camera.Render();
                    ReadRenderTexture(sceneTexture, withoutGroup);
                }
                finally
                {
                    for (var i = 0; i < group.Count; i++) group[i].enabled = original[i];
                }

                var maxDiff = 0;
                var count = 0;
                for (var y = 0; y < 512; y++)
                {
                    for (var x = 0; x < 1024; x++)
                    {
                        var a = withGroup.GetPixel(x, y);
                        var b = withoutGroup.GetPixel(x, y);
                        var diff = Mathf.RoundToInt(255f * Mathf.Max(
                            Mathf.Abs(a.r - b.r),
                            Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))));
                        maxDiff = Mathf.Max(maxDiff, diff);
                        if (diff >= 8) count++;
                    }
                }
                return (maxDiff, count);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(sceneTexture);
                Object.Destroy(withGroup);
                Object.Destroy(withoutGroup);
            }
        }

        private static void ReadRenderTexture(RenderTexture source, Texture2D destination)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static string DescribeAnimator(Beatmap.Animations.ObjectAnimator animator)
        {
            if (animator == null)
            {
                return "null";
            }

            return $"type={animator.TargetType} " +
                $"localTarget={(animator.LocalTarget == null ? "null" : animator.LocalTarget.name)} " +
                $"worldTarget={(animator.WorldTarget == null ? "null" : animator.WorldTarget.name)} " +
                $"localPosN={animator.LocalPosition.Count} " +
                $"worldPosN={animator.WorldPosition.Count} " +
                $"offsetN={animator.OffsetPosition.Count}";
        }

        private static void AppendGeometryAncestry(
            System.Text.StringBuilder sb, string label, GeometryContainer container)
        {
            var live = container.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.enabled && r.name != "SelectionMesh")
                .FirstOrDefault();
            var ancestry = new System.Text.StringBuilder();
            for (var t = container.transform; t != null; t = t.parent)
            {
                ancestry.Append($"[{t.name} pos={t.position}]");
            }
            sb.AppendLine($"  geometry {label}: ancestry={ancestry} " +
                $"liveRenderer={(live == null ? "none" : $"{live.name} bounds={live.bounds}")}");
        }

        // DumpBeat124PanelAndTextGeometry is a mechanical diagnostic for the beat-124 panel/text
        // state: it seeks the authored flash window (123.75/124/124.25/125 and back to 124) and logs
        // the authored custom events around it, the material inventory of the geometry enhancements,
        // per-focus-track animator transforms, and representative renderer geometry per material group.
        // It asserts only load preconditions - no desired pose is pinned yet.
        [UnityTest]
        [Explicit]
        public IEnumerator DumpBeat124PanelAndTextGeometry()
        {
            var map = BeatSaberSongContainer.Instance.Map;
            Assert.That(map, Is.Not.Null, "Diagnostic precondition: the full map should be loaded.");
            var geometryEnhancements = map.EnvironmentEnhancements.Where(e => e.Geometry != null).ToList();
            Assert.That(geometryEnhancements, Is.Not.Empty,
                "Diagnostic precondition: the map should author geometry enhancements.");

            var focusTracks = new[]
            {
                "space", "bruhspace", "a", "gone", "redcen", "censson",
                "back", "loss", "when", "1", "2", "3", "yeet",
            };

            // === authored custom events 110<=b<=128 of type AnimateTrack/AssignTrackParent ===
            var eventsReport = new System.Text.StringBuilder();
            var windowEvents = map.CustomEvents
                .Select((evt, index) => (evt, index))
                .Where(pair => pair.evt.JsonTime >= 110f && pair.evt.JsonTime <= 128f
                    && (pair.evt.Type == "AnimateTrack" || pair.evt.Type == "AssignTrackParent"))
                .ToList();
            eventsReport.AppendLine(
                $"=== authored custom events 110<=beat<=128 (AnimateTrack/AssignTrackParent): " +
                $"{windowEvents.Count} ===");
            foreach (var (evt, index) in windowEvents)
            {
                var data = evt.Data?.ToString() ?? "null";
                if (data.Length > 500) data = data.Substring(0, 500) + "...";
                eventsReport.AppendLine(
                    $"  [{index}] time={evt.JsonTime} type={evt.Type} " +
                    $"track={(evt.CustomTrack?.ToString() ?? evt.DataParentTrack?.ToString() ?? "null")} " +
                    $"duration={(evt.DataDuration?.ToString() ?? "null")} data={data}");
            }

            // === earliest/latest custom event touching each focus track ===
            eventsReport.AppendLine("=== focus-track event span (earliest/latest) ===");
            foreach (var trackName in focusTracks)
            {
                var matching = map.CustomEvents
                    .Where(evt => evt.HasMatchingTrack(trackName))
                    .ToList();
                if (matching.Count == 0)
                {
                    eventsReport.AppendLine($"  track {trackName}: <no events>");
                    continue;
                }
                foreach (var evt in new[] { matching.First(), matching.Last() }.Distinct())
                {
                    var data = evt.Data?.ToString() ?? "null";
                    if (data.Length > 200) data = data.Substring(0, 200) + "...";
                    eventsReport.AppendLine(
                        $"  track {trackName}: time={evt.JsonTime} type={evt.Type} data={data}");
                }
            }
            Debug.Log(eventsReport.ToString());

            // === material inventory across geometry enhancements (includes authored space
            // white planes) grouped by authored material reference ===
            var materialReport = new System.Text.StringBuilder();
            materialReport.AppendLine("=== geometry material inventory (authored reference) ===");
            foreach (var group in geometryEnhancements.GroupBy(e =>
                e.Geometry[e.GeometryKeyMaterial] is JSONString s
                    ? (string)s
                    : "<inline:" +
                        (e.Geometry[e.GeometryKeyMaterial]?["shader"]?.Value ?? "?") + ">"))
            {
                materialReport.AppendLine(
                    $"  material={(group.Key ?? "<null>")} count={group.Count()} " +
                    $"sampleTracks=[{string.Join(",", group.Select(e => e.Track ?? "<null>").Distinct().Take(5))}]");
            }
            materialReport.AppendLine("=== authored space-track geometry (white planes) ===");
            foreach (var enh in geometryEnhancements.Where(e => e.Track == "space"))
            {
                var mat = enh.Geometry[enh.GeometryKeyMaterial];
                materialReport.AppendLine(
                    $"  space geometry={enh.Geometry[enh.GeometryKeyType]} " +
                    $"materialRef={(mat?.ToString().Length > 200 ? mat.ToString().Substring(0, 200) : mat?.ToString() ?? "null")} " +
                    $"scale={(enh.Scale?.ToString() ?? "null")} pos={(enh.Position?.ToString() ?? "null")} " +
                    $"active={(enh.Active?.ToString() ?? "null")} id={enh.ID}");
            }
            Debug.Log(materialReport.ToString());

            // === authored material definitions: raw customData.materials vs parsed
            // BaseMaterial, to check Chroma's empty-shaderKeywords Standard->Glowing case ===
            var matDefLog = new System.Text.StringBuilder();
            var rawMap = JSON.Parse(File.ReadAllText(FullMapPath));
            var rawCustomData = rawMap["customData"];
            System.Func<JSONNode, string> keysOf = node =>
            {
                var keys = new List<string>();
                if (node is JSONObject)
                {
                    foreach (var k in node.Keys) keys.Add(k);
                }
                return string.Join(",", keys);
            };
            matDefLog.AppendLine(
                $"=== raw customData keys: [{(rawCustomData == null ? "<null>" : keysOf(rawCustomData))}] ===");
            var rawMaterials = rawCustomData == null ? null : rawCustomData["materials"];
            if (rawMaterials is JSONObject rawMatObj)
            {
                matDefLog.AppendLine(
                    $"raw materials count={rawMatObj.Count} names=[{keysOf(rawMatObj)}]");
                foreach (var key in new[] { "whitebg", "red", "blue", "yellow", "cyan", "green", "black" })
                {
                    var node = rawMatObj[key];
                    if (node == null)
                    {
                        matDefLog.AppendLine($"  raw {key}: <absent>");
                        continue;
                    }
                    var kwNode = node["shaderKeywords"];
                    matDefLog.AppendLine(
                        $"  raw {key}: keys=[{keysOf(node)}] " +
                        $"shader={node["shader"]} color={node["color"]} " +
                        $"hasShaderKeywords={node.HasKey("shaderKeywords")} " +
                        $"kwType={(kwNode == null ? "null" : kwNode.GetType().Name)} " +
                        $"kwCount={(kwNode == null ? "-" : kwNode.Count.ToString())} " +
                        $"kwValue={(kwNode?.ToString() ?? "null")}");
                }
            }
            else
            {
                matDefLog.AppendLine(
                    $"raw materials node type={(rawMaterials == null ? "null" : rawMaterials.GetType().Name)}");
            }
            matDefLog.AppendLine($"=== parsed map.Materials ({map.Materials.Count}) ===");
            foreach (var pair in map.Materials)
            {
                var m = pair.Value;
                matDefLog.AppendLine(
                    $"  {pair.Key}: shader={m.Shader} color={(m.Color?.ToString() ?? "null")} " +
                    $"track={(m.Track ?? "null")} shaderKeywords({m.ShaderKeywords?.Count ?? -1})=" +
                    $"[{(m.ShaderKeywords == null ? "" : string.Join(",", m.ShaderKeywords))}]");
            }
            foreach (var trackName in new[] { "a_9_5", "space" })
            {
                var container = Object.FindObjectsByType<GeometryContainer>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.EnvironmentEnhancement != null
                        && c.EnvironmentEnhancement.Track == trackName);
                var r = container == null
                    ? null
                    : container.GetComponentsInChildren<Renderer>(true)
                        .FirstOrDefault(x => x != null && x.name != "SelectionMesh");
                matDefLog.AppendLine(
                    $"  resolved {trackName}: sharedMaterial=" +
                    (r == null || r.sharedMaterial == null
                        ? "<none>"
                        : $"{r.sharedMaterial.name} shader={r.sharedMaterial.shader.name} " +
                            $"keywords=[{string.Join(",", r.sharedMaterial.shaderKeywords)}]"));
            }
            Debug.Log(matDefLog.ToString());

            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            var colorId = Shader.PropertyToID("_Color");

            foreach (var (beatLabel, beat) in new[]
            {
                ("115.5", 115.5f), ("123.99", 123.99f),
                ("123.75", 123.75f), ("124", 124f), ("124.25", 124.25f),
                ("125", 125f), ("125->124", 124f),
            })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;

                var beatLog = new System.Text.StringBuilder();
                beatLog.AppendLine(
                    $"=== beat {beatLabel}: camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} ===");

                foreach (var trackName in focusTracks)
                {
                    var ta = tracksManager == null ? null : tracksManager.GetAnimationTrack(trackName);
                    if (ta == null || ta.Track == null)
                    {
                        beatLog.AppendLine($"  track {trackName}: <no TrackAnimator>");
                        continue;
                    }
                    var self = ta.Track.SelfTransform;
                    var parent = ta.Track.ObjectParentTransform;
                    beatLog.AppendLine(
                        $"  track {trackName}: " +
                        $"selfWorld={self.position} selfLocal={self.localPosition} " +
                        $"selfRot={self.eulerAngles} selfLocalRot={self.localEulerAngles} " +
                        $"selfScale={self.localScale} | " +
                        $"parentWorld={parent.position} parentLocal={parent.localPosition} " +
                        $"parentRot={parent.eulerAngles} parentScale={parent.lossyScale} " +
                        $"parentName={(parent.parent == null ? "null" : parent.parent.name)} " +
                        $"props=[{string.Join(",", ta.AnimatedProperties.Keys)}]");
                }

                var geometries = Object.FindObjectsByType<GeometryContainer>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(c => c != null)
                    .ToList();

                // Representative renderers: track theaaaa, redcen_10_9, every space container
                // (bounded at 5), and <=3 samples per sharedMaterial group (preferring active ones)
                // so whitebg/black/color-bar groups each appear without 2200-line dumps.
                var sampled = new List<Renderer>();
                foreach (var trackName in new[] { "theaaaa", "redcen_10_9" })
                {
                    var container = geometries.FirstOrDefault(c =>
                        c.EnvironmentEnhancement != null && c.EnvironmentEnhancement.Track == trackName);
                    if (container != null)
                    {
                        sampled.AddRange(container.GetComponentsInChildren<Renderer>(true)
                            .Where(r => r != null && r.name != "SelectionMesh").Take(1));
                    }
                }
                sampled.AddRange(geometries
                    .Where(c => c.EnvironmentEnhancement != null && c.EnvironmentEnhancement.Track == "space")
                    .Take(5)
                    .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r != null && r.name != "SelectionMesh"));
                var allRenderers = geometries
                    .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r != null && r.name != "SelectionMesh")
                    .ToList();
                foreach (var group in allRenderers.GroupBy(r =>
                    r.sharedMaterial == null ? "<null>" : r.sharedMaterial.name))
                {
                    foreach (var r in group
                        .OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy)
                        .Take(3))
                    {
                        sampled.Add(r);
                    }
                }
                // Guarantee one active renderer from each named material group if authored names
                // differ from sharedMaterial names.
                foreach (var materialName in new[] { "whitebg", "black", "color-bar" })
                {
                    var byAuthored = allRenderers.Where(r =>
                    {
                        var owner = r.GetComponentInParent<GeometryContainer>();
                        var enh = owner == null ? null : owner.EnvironmentEnhancement;
                        var matRef = enh == null || enh.Geometry == null
                            ? null
                            : enh.Geometry[enh.GeometryKeyMaterial] as JSONString;
                        return matRef != null && matRef.Value == materialName
                            && r.enabled && r.gameObject.activeInHierarchy;
                    }).Take(1);
                    sampled.AddRange(byAuthored);
                }

                foreach (var r in sampled.Distinct())
                {
                    var owner = r.GetComponentInParent<GeometryContainer>();
                    var enh = owner == null ? null : owner.EnvironmentEnhancement;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    var toCam = camera.transform.position - r.bounds.center;
                    var facing = Vector3.Dot(r.transform.forward, toCam.normalized);
                    var viewport = camera.WorldToViewportPoint(r.bounds.center);
                    var path = new System.Text.StringBuilder();
                    for (var t = r.transform; t != null; t = t.parent)
                    {
                        path.Insert(0, "/" + t.name);
                    }
                    beatLog.AppendLine(
                        $"  renderer {r.name} track={(enh == null ? "null" : enh.Track)} " +
                        $"mat={(r.sharedMaterial == null ? "<null>" : r.sharedMaterial.name)} " +
                        $"boundsCenter={r.bounds.center} boundsExtents={r.bounds.extents} " +
                        $"facingDot={facing:F3} viewport={viewport} " +
                        $"mpbColor={mpb.GetColor(colorId)} " +
                        $"active={r.gameObject.activeInHierarchy} enabled={r.enabled} " +
                        $"path={path}");
                }
                // Per-beat orientation probe for the red text and black backing samples:
                // axes + bounds-center viewport, no orientation expectations assumed yet.
                foreach (var trackName in new[] { "redcen_10_9", "theaaaa" })
                {
                    var axisContainer = geometries.FirstOrDefault(c =>
                        c.EnvironmentEnhancement != null
                        && c.EnvironmentEnhancement.Track == trackName);
                    var axisRenderer = axisContainer == null
                        ? null
                        : axisContainer.GetComponentsInChildren<Renderer>(true)
                            .FirstOrDefault(x => x != null && x.name != "SelectionMesh");
                    beatLog.AppendLine(axisRenderer == null
                        ? $"  axes {trackName}: <no renderer>"
                        : $"  axes {trackName}: right={axisRenderer.transform.right} " +
                            $"up={axisRenderer.transform.up} fwd={axisRenderer.transform.forward} " +
                            $"worldRot={axisRenderer.transform.eulerAngles} " +
                            $"boundsCenter={axisRenderer.bounds.center} " +
                            $"vp={camera.WorldToViewportPoint(axisRenderer.bounds.center)}");
                }
                Debug.Log(beatLog.ToString());

                // Aggregate group bounds/frustum and space-plane facing diagnostics at the
                // pre-window and beat-124 samples (forward and reverse seek) to distinguish
                // the a_* TV grid, redcen_* text and the whitebg space planes.
                if (beatLabel == "115.5" || beatLabel == "123.99"
                    || beatLabel == "124" || beatLabel == "125->124")
                {
                    var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                    var groupLog = new System.Text.StringBuilder();
                    groupLog.AppendLine($"=== beat {beatLabel}: group aggregate ===");
                    foreach (var (label, pred) in new (string, System.Func<GeometryContainer, bool>)[]
                    {
                        ("a_*", c => c.EnvironmentEnhancement != null
                            && c.EnvironmentEnhancement.Track != null
                            && c.EnvironmentEnhancement.Track.StartsWith("a_")),
                        ("redcen_*", c => c.EnvironmentEnhancement != null
                            && c.EnvironmentEnhancement.Track != null
                            && c.EnvironmentEnhancement.Track.StartsWith("redcen_")),
                        ("theaaaa", c => c.EnvironmentEnhancement != null
                            && c.EnvironmentEnhancement.Track == "theaaaa"),
                        ("space", c => c.EnvironmentEnhancement != null
                            && c.EnvironmentEnhancement.Track == "space"),
                    })
                    {
                        var rs = geometries.Where(pred)
                            .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                            .Where(r => r != null && r.name != "SelectionMesh"
                                && r.enabled && r.gameObject.activeInHierarchy)
                            .ToList();
                        if (rs.Count == 0)
                        {
                            groupLog.AppendLine($"  group {label}: <no live renderers>");
                            continue;
                        }
                        var bounds = rs[0].bounds;
                        var vpMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var vpMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        var colors = new HashSet<string>();
                        var inFrustum = 0;
                        var frontFacing = 0;
                        foreach (var r in rs)
                        {
                            bounds.Encapsulate(r.bounds);
                            var vp = camera.WorldToViewportPoint(r.bounds.center);
                            vpMin = Vector3.Min(vpMin, vp);
                            vpMax = Vector3.Max(vpMax, vp);
                            if (vp.z > 0f) frontFacing++;
                            if (GeometryUtility.TestPlanesAABB(planes, r.bounds)) inFrustum++;
                            var gMpb = new MaterialPropertyBlock();
                            r.GetPropertyBlock(gMpb);
                            colors.Add(gMpb.GetColor(colorId).ToString());
                        }
                        var vpCenter = camera.WorldToViewportPoint(bounds.center);
                        groupLog.AppendLine(
                            $"  group {label}: live={rs.Count} inFrustum={inFrustum} " +
                            $"viewportZ>0={frontFacing} boundsCenter={bounds.center} " +
                            $"boundsExtents={bounds.extents} vpCenter={vpCenter} " +
                            $"vpMin={vpMin} vpMax={vpMax} " +
                            $"mpbColors=[{string.Join("|", colors.Take(6))}]");
                    }

                    foreach (var trackName in new[] { "a_0_4", "a_9_5", "a_18_2", "redcen_10_9" })
                    {
                        var container = geometries.FirstOrDefault(c =>
                            c.EnvironmentEnhancement != null
                            && c.EnvironmentEnhancement.Track == trackName);
                        if (container == null)
                        {
                            groupLog.AppendLine($"  hierarchy {trackName}: <no container>");
                            continue;
                        }
                        var ancestry = new System.Text.StringBuilder();
                        for (var t = container.transform; t != null; t = t.parent)
                        {
                            ancestry.Append(
                                $"[{t.name} wPos={t.position} lPos={t.localPosition} " +
                                $"wRot={t.eulerAngles} lRot={t.localEulerAngles} " +
                                $"lScale={t.localScale}]");
                        }
                        groupLog.AppendLine($"  hierarchy {trackName}: {ancestry}");
                    }

                    foreach (var container in geometries.Where(c =>
                            c.EnvironmentEnhancement != null
                            && c.EnvironmentEnhancement.Track == "space")
                        .Take(6))
                    {
                        var enh = container.EnvironmentEnhancement;
                        var r = container.GetComponentsInChildren<Renderer>(true)
                            .FirstOrDefault(x => x != null && x.name != "SelectionMesh");
                        var mf = r == null ? null : r.GetComponent<MeshFilter>();
                        var mesh = mf == null
                            ? null
                            : (mf.sharedMesh != null ? mf.sharedMesh : mf.mesh);
                        var normalText = mf == null
                            ? "<no MeshFilter>"
                            : mesh == null ? "<mesh null>" : "<no tris>";
                        var facingText = "-";
                        if (r != null && mesh != null)
                        {
                            var tris = mesh.triangles;
                            normalText = $"mesh={mesh.name} tris={tris.Length} verts={mesh.vertexCount}";
                            if (tris.Length >= 3)
                            {
                                var verts = mesh.vertices;
                                var localNormal = Vector3.Cross(
                                    verts[tris[1]] - verts[tris[0]],
                                    verts[tris[2]] - verts[tris[0]]).normalized;
                                var worldNormal = r.transform.TransformDirection(localNormal).normalized;
                                var dot = Vector3.Dot(worldNormal,
                                    (camera.transform.position - r.bounds.center).normalized);
                                normalText = $"mesh={mesh.name} tris={tris.Length} worldNormal={worldNormal}";
                                facingText = $"normalDotToCam={dot:F3}";
                            }
                        }
                        var shaderText = r == null || r.sharedMaterial == null
                            ? "<no material>"
                            : $"mat={r.sharedMaterial.name} shader={r.sharedMaterial.shader.name} " +
                                "cullMode=" + (r.sharedMaterial.HasProperty("_CullMode")
                                    ? r.sharedMaterial.GetFloat("_CullMode").ToString()
                                    : "n/a");
                        var pMpb = new MaterialPropertyBlock();
                        if (r != null) r.GetPropertyBlock(pMpb);
                        groupLog.AppendLine(
                            $"  spacePlane id={enh.ID} pos={(enh.Position?.ToString() ?? "null")} " +
                            $"rot={(enh.Rotation?.ToString() ?? "null")} " +
                            $"localRot={(enh.LocalRotation?.ToString() ?? "null")} " +
                            $"scale={(enh.Scale?.ToString() ?? "null")} | " +
                            $"{normalText} {facingText} {shaderText} " +
                            $"mpbColor={(r == null ? "-" : pMpb.GetColor(colorId).ToString())} " +
                            $"bounds={(r == null ? "-" : r.bounds.ToString())} " +
                            $"active={(r != null && r.gameObject.activeInHierarchy)} " +
                            $"enabled={(r != null && r.enabled)}");
                    }
                    Debug.Log(groupLog.ToString());
                }

                // Controlled rendered A/B at beat 124: baseline frame with everything enabled,
                // then space-only off, then a_*-only off (fresh baseline), then a cull-off clone
                // of the space material. Measures pixel deltas without asserting thresholds.
                if (beatLabel == "124")
                {
                    var spaceRenderers = allRenderers
                        .Where(r => r is MeshRenderer
                            && r.GetComponentInParent<GeometryContainer>() is GeometryContainer gc
                            && gc.EnvironmentEnhancement != null
                            && gc.EnvironmentEnhancement.Track == "space")
                        .ToList();
                    var aRenderers = allRenderers
                        .Where(r => r.GetComponentInParent<GeometryContainer>() is GeometryContainer gc
                            && gc.EnvironmentEnhancement != null
                            && gc.EnvironmentEnhancement.Track != null
                            && gc.EnvironmentEnhancement.Track.StartsWith("a_"))
                        .ToList();

                    var rt = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
                    var baseline = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
                    var toggled = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
                    var previousTarget = camera.targetTexture;
                    var abLog = new System.Text.StringBuilder();
                    abLog.AppendLine("=== beat 124 A/B renders ===");
                    try
                    {
                        camera.targetTexture = rt;

                        camera.Render();
                        ReadRenderTexture(rt, baseline);

                        // === environment/fog/lighting inventory at beat 124 (diagnostic only) ===
                        var envLog = new System.Text.StringBuilder();
                        envLog.AppendLine("=== beat 124 environment/fog/lighting ===");
                        var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
                        var fog = context == null || context.Descriptor == null
                            ? null
                            : context.Descriptor.BloomFogParams;
                        envLog.AppendLine(
                            $"  BloomFogParams={(fog == null ? "<null>" : "")}" +
                            (fog == null
                                ? ""
                                : $" attenuation={fog.Attenuation} offset={fog.Offset} " +
                                    $"height={fog.Height} startY={fog.StartY} " +
                                    $"autoExposureLimit={fog.AutoExposureLimit} " +
                                    $"legacyAutoExposure={fog.LegacyAutoExposure}"));
                        envLog.AppendLine(
                            $"  globalFog: _CustomFogAttenuation={Shader.GetGlobalFloat("_CustomFogAttenuation")} " +
                            $"_CustomFogHeightFogHeight={Shader.GetGlobalFloat("_CustomFogHeightFogHeight")} " +
                            $"_CustomFogHeightFogStartY={Shader.GetGlobalFloat("_CustomFogHeightFogStartY")} " +
                            $"_CustomFogOffset={Shader.GetGlobalFloat("_CustomFogOffset")}");

                        var dirLights = DirectionalLight.Lights ?? new List<DirectionalLight>();
                        var pointLights = PointLight.Lights ?? new List<PointLight>();
                        envLog.AppendLine(
                            $"  lights: directional={dirLights.Count} point={pointLights.Count}");
                        foreach (var dl in dirLights.Take(8))
                        {
                            var ctrl = dl.GetComponentInParent<LightController>();
                            envLog.AppendLine(
                                $"    dir name={dl.name} enabled={dl.enabled} " +
                                $"active={dl.gameObject.activeInHierarchy} " +
                                $"ctrlType={(ctrl != null ? ctrl.Type.ToString() : "n/a")} " +
                                $"ctrlID={(ctrl != null ? ctrl.ID.ToString() : "n/a")} " +
                                $"pos={dl.transform.position} fwd={dl.transform.forward} " +
                                $"color={dl.Color} intensity={dl.Intensity} radius={dl.Radius}");
                        }
                        foreach (var pl in pointLights.Take(8))
                        {
                            envLog.AppendLine(
                                $"    point name={pl.name} enabled={pl.enabled} " +
                                $"active={pl.gameObject.activeInHierarchy} " +
                                $"pos={pl.transform.position} fwd={pl.transform.forward} " +
                                $"color={pl.Color} intensity={pl.Intensity}");
                        }
                        var dirColors = Shader.GetGlobalVectorArray("_DirectionalLightColors");
                        var dirDirs = Shader.GetGlobalVectorArray("_DirectionalLightDirections");
                        var ptColors = Shader.GetGlobalVectorArray("_PointLightColors");
                        envLog.AppendLine(
                            $"  globals: dirColors({dirColors.Length})=[" +
                            $"{string.Join("|", dirColors.Take(5))}] " +
                            $"dirDirs({dirDirs.Length})=[{string.Join("|", dirDirs.Take(5))}] " +
                            $"ptColors({ptColors.Length})=[{string.Join("|", ptColors.Take(1))}]");

                        envLog.AppendLine(
                            $"  ambientLight={RenderSettings.ambientLight} " +
                            $"ambientMode={RenderSettings.ambientMode} " +
                            $"ambientIntensity={RenderSettings.ambientIntensity}");
                        var standardMat = allRenderers
                            .Select(r => r.sharedMaterial)
                            .FirstOrDefault(m => m != null && m.name.StartsWith("Standard"));
                        if (standardMat != null)
                        {
                            envLog.AppendLine(
                                $"  Standard mat: ambientMin=" +
                                (standardMat.HasProperty("_AmbientMinimalValue")
                                    ? standardMat.GetFloat("_AmbientMinimalValue").ToString()
                                    : "n/a") +
                                " nominalDiffuse=" +
                                (standardMat.HasProperty("_NominalDiffuseLevel")
                                    ? standardMat.GetColor("_NominalDiffuseLevel").ToString()
                                    : "n/a") +
                                " ambientMultiplier=" +
                                (standardMat.HasProperty("_AmbientMultiplier")
                                    ? standardMat.GetFloat("_AmbientMultiplier").ToString()
                                    : "n/a") +
                                " fogKeywords=[" +
                                string.Join(",", standardMat.shaderKeywords.Where(k =>
                                    k.Contains("FOG"))) +
                                "]");
                        }

                        var fogEnhancements = map.EnvironmentEnhancements
                            .Where(e => e.Components != null
                                && e.Components.HasKey("BloomFogEnvironment"))
                            .ToList();
                        envLog.AppendLine(
                            $"  BloomFogEnvironment enhancements={fogEnhancements.Count}");
                        foreach (var e in fogEnhancements.Take(20))
                        {
                            var comp = e.Components["BloomFogEnvironment"].ToString();
                            if (comp.Length > 300) comp = comp.Substring(0, 300) + "...";
                            envLog.AppendLine(
                                $"    id={e.ID} lookup={e.LookupMethod} track={e.Track} data={comp}");
                        }
                        var fogEvents = map.CustomEvents
                            .Where(e => e.JsonTime <= 124f
                                && (e.Type.Contains("Fog")
                                    || e.Type.Contains("Component")
                                    || e.Type.Contains("Light")
                                    || (e.Data?.ToString().Contains("BloomFog") ?? false)))
                            .ToList();
                        envLog.AppendLine(
                            $"  fog/lighting custom events beat<=124: {fogEvents.Count}");
                        foreach (var e in fogEvents.Take(20))
                        {
                            var data = e.Data?.ToString() ?? "null";
                            if (data.Length > 200) data = data.Substring(0, 200) + "...";
                            envLog.AppendLine(
                                $"    time={e.JsonTime} type={e.Type} data={data}");
                        }
                        foreach (var group in fogEvents
                            .Where(e => e.CustomTrack != null)
                            .GroupBy(e => e.CustomTrack.ToString()))
                        {
                            var latest = group.Last();
                            var data = latest.Data?.ToString() ?? "null";
                            if (data.Length > 200) data = data.Substring(0, 200) + "...";
                            envLog.AppendLine(
                                $"    latestPerTrack {group.Key}: time={latest.JsonTime} " +
                                $"type={latest.Type} data={data}");
                        }

                        // Basic light-event routing vs fog prepass: inventory the authored
                        // light events around/at beat 124.
                        envLog.AppendLine("  light events 118<=beat<=128:");
                        for (var i = 0; i < map.Events.Count; i++)
                        {
                            var ev = map.Events[i];
                            if (ev.JsonTime < 118f || ev.JsonTime > 128f) continue;
                            var cd = ev.CustomData?.ToString() ?? "null";
                            if (cd.Length > 250) cd = cd.Substring(0, 250) + "...";
                            envLog.AppendLine(
                                $"    [{i}] t={ev.JsonTime} type={ev.Type} value={ev.Value} " +
                                $"float={ev.FloatValue} " +
                                $"lightID={(ev.CustomLightID == null ? "null" : string.Join(",", ev.CustomLightID))} " +
                                $"color={(ev.CustomColor?.ToString() ?? "null")} data={cd}");
                        }
                        foreach (var et in new[] { 0, 1, 2, 3, 4 })
                        {
                            var last = map.Events
                                .LastOrDefault(e => e.Type == et && e.JsonTime <= 124f);
                            envLog.AppendLine(
                                $"    lastEvent type={et} <=124: " +
                                (last == null
                                    ? "<none>"
                                    : $"t={last.JsonTime} value={last.Value} " +
                                        $"float={last.FloatValue} color={last.CustomColor}"));
                        }

                        var bloomRt = Shader.GetGlobalTexture("_BloomPrePassTexture") as RenderTexture;
                        envLog.AppendLine(
                            $"  _BloomPrePassTexture=" +
                            (bloomRt == null
                                ? "<null>"
                                : $"{bloomRt.width}x{bloomRt.height} fmt={bloomRt.format}"));
                        envLog.AppendLine(
                            $"  BLOOM_FOG keyword enabled={Shader.IsKeywordEnabled("BLOOM_FOG")}");
                        if (bloomRt != null)
                        {
                            var prevActive = RenderTexture.active;
                            var small = RenderTexture.GetTemporary(
                                256, 256, 0, RenderTextureFormat.ARGBHalf);
                            var bloomTex = new Texture2D(256, 256, TextureFormat.RGBAFloat, false);
                            try
                            {
                                Graphics.Blit(bloomRt, small);
                                RenderTexture.active = small;
                                bloomTex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                                bloomTex.Apply();
                                var px = bloomTex.GetPixels();
                                float mn = float.MaxValue, mx = float.MinValue, sum = 0f;
                                var outerSum = 0f;
                                var outerCount = 0;
                                for (var p = 0; p < px.Length; p++)
                                {
                                    var lum = Mathf.Max(px[p].r, Mathf.Max(px[p].g, px[p].b));
                                    if (lum < mn) mn = lum;
                                    if (lum > mx) mx = lum;
                                    sum += lum;
                                    var x = p % 256;
                                    var y = p / 256;
                                    if (x < 32 || x >= 224 || y < 32 || y >= 224)
                                    {
                                        outerSum += lum;
                                        outerCount++;
                                    }
                                }
                                envLog.AppendLine(
                                    $"  bloomPrePass lum: min={mn:F4} max={mx:F4} " +
                                    $"avg={sum / px.Length:F4} center={px[128 + 128 * 256]} " +
                                    $"outerAvg={outerSum / outerCount:F4}");
                            }
                            finally
                            {
                                RenderTexture.active = prevActive;
                                RenderTexture.ReleaseTemporary(small);
                                Object.DestroyImmediate(bloomTex);
                            }
                        }

                        var bloomLights = BloomFogObject.AllBloomFogLights
                            ?? new List<BloomFogObject>();
                        var bloomActive = bloomLights.Count(b => b != null && b.isActiveAndEnabled);
                        var bloomInView = bloomLights.Count(b =>
                        {
                            if (b == null || !b.isActiveAndEnabled) return false;
                            var vp = camera.WorldToViewportPoint(b.transform.position);
                            return vp.z > 0f
                                && vp.x >= 0f && vp.x <= 1f
                                && vp.y >= 0f && vp.y <= 1f;
                        });
                        envLog.AppendLine(
                            $"  BloomFogObjects: total={bloomLights.Count} " +
                            $"active={bloomActive} inView={bloomInView}");
                        Debug.Log(envLog.ToString());

                        var spaceOriginal = spaceRenderers.Select(r => r.enabled).ToList();
                        foreach (var r in spaceRenderers) r.enabled = false;
                        camera.Render();
                        ReadRenderTexture(rt, toggled);
                        for (var i = 0; i < spaceRenderers.Count; i++)
                            spaceRenderers[i].enabled = spaceOriginal[i];
                        var spaceDiff = DiffFrames(baseline, toggled);
                        abLog.AppendLine(
                            $"  space-off: maxDiff={spaceDiff.MaxDiff} changedPixels={spaceDiff.Count} " +
                            $"changedRect=({spaceDiff.MinX},{spaceDiff.MinY})-({spaceDiff.MaxX},{spaceDiff.MaxY}) " +
                            $"baselineNearWhite center={spaceDiff.CenterWhite} outer={spaceDiff.OuterWhite}");

                        camera.Render();
                        ReadRenderTexture(rt, baseline);
                        var aOriginal = aRenderers.Select(r => r.enabled).ToList();
                        foreach (var r in aRenderers) r.enabled = false;
                        camera.Render();
                        ReadRenderTexture(rt, toggled);
                        for (var i = 0; i < aRenderers.Count; i++)
                            aRenderers[i].enabled = aOriginal[i];
                        var aDiff = DiffFrames(baseline, toggled);
                        abLog.AppendLine(
                            $"  a_*-off: maxDiff={aDiff.MaxDiff} changedPixels={aDiff.Count} " +
                            $"changedRect=({aDiff.MinX},{aDiff.MinY})-({aDiff.MaxX},{aDiff.MaxY}) " +
                            $"baselineNearWhite center={aDiff.CenterWhite} outer={aDiff.OuterWhite}");

                        // Cull-off probe: clone only the space materials, do not touch shared assets.
                        var spaceMats = spaceRenderers
                            .Select(r => r.sharedMaterial)
                            .Where(m => m != null && m.HasProperty("_CullMode"))
                            .Distinct()
                            .ToList();
                        if (spaceMats.Count > 0)
                        {
                            var clones = new Dictionary<Material, Material>();
                            var originalByRenderer = spaceRenderers.Select(r => r.sharedMaterial).ToList();
                            foreach (var m in spaceMats)
                            {
                                var clone = new Material(m) { hideFlags = HideFlags.HideAndDontSave };
                                clone.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);
                                clones[m] = clone;
                            }
                            for (var i = 0; i < spaceRenderers.Count; i++)
                            {
                                if (clones.TryGetValue(originalByRenderer[i], out var clone))
                                    spaceRenderers[i].sharedMaterial = clone;
                            }
                            camera.Render();
                            ReadRenderTexture(rt, toggled);
                            for (var i = 0; i < spaceRenderers.Count; i++)
                                spaceRenderers[i].sharedMaterial = originalByRenderer[i];
                            foreach (var clone in clones.Values) Object.Destroy(clone);
                            var cullDiff = DiffFrames(baseline, toggled);
                            abLog.AppendLine(
                                $"  space-cullOff: maxDiff={cullDiff.MaxDiff} " +
                                $"changedPixels={cullDiff.Count} " +
                                $"changedRect=({cullDiff.MinX},{cullDiff.MinY})-({cullDiff.MaxX},{cullDiff.MaxY}) " +
                                $"materialsCloned={spaceMats.Count}");
                        }
                        else
                        {
                            abLog.AppendLine("  space-cullOff: no space material exposes _CullMode");
                        }

                        // Fresh baseline + brightness metrics, then material-clone contrast
                        // probes on space planes and a_* cubes only (shared assets untouched).
                        camera.Render();
                        ReadRenderTexture(rt, baseline);
                        var baseMetrics = MeasureFrame(baseline);
                        abLog.AppendLine(
                            $"  baseline: nearWhite center={baseMetrics.CenterWhite} " +
                            $"outer={baseMetrics.OuterWhite} saturatedPanel={baseMetrics.SaturatedPanel} " +
                            $"lumCenter={baseMetrics.CenterLum:F3} lumEdge={baseMetrics.EdgeLum:F3}");

                        foreach (var (groupLabel, groupRenderers) in new[]
                        {
                            ("space", (List<Renderer>)spaceRenderers),
                            ("a_*", (List<Renderer>)aRenderers),
                        })
                        {
                            var mats = groupRenderers
                                .Select(r => r.sharedMaterial)
                                .Where(m => m != null)
                                .Distinct()
                                .ToList();
                            foreach (var m in mats)
                            {
                                abLog.AppendLine(
                                    $"  {groupLabel} mat={m.name} shader={m.shader.name} " +
                                    $"keywords=[{string.Join(",", m.shaderKeywords)}] " +
                                    $"hasAmbientMinimal={m.HasProperty("_AmbientMinimalValue")} " +
                                    "ambientValue=" + (m.HasProperty("_AmbientMinimalValue")
                                        ? m.GetFloat("_AmbientMinimalValue").ToString()
                                        : "n/a") +
                                    $" hasCull={m.HasProperty("_CullMode")}");
                            }
                            var variants = groupLabel == "space"
                                ? new[] { "ambient", "noFog", "ambient+noFog", "bothSides", "bothSides+noFog" }
                                : new[] { "ambient", "noFog", "ambient+noFog", "bothSides" };
                            foreach (var variant in variants)
                            {
                                camera.Render();
                                ReadRenderTexture(rt, baseline);
                                var originals = groupRenderers.Select(r => r.sharedMaterial).ToList();
                                var clones = new Dictionary<Material, Material>();
                                var bothSidesKw = false;
                                var hasBothSidesProp = false;
                                try
                                {
                                    foreach (var m in mats)
                                    {
                                        var clone = new Material(m)
                                        {
                                            hideFlags = HideFlags.HideAndDontSave,
                                        };
                                        if (variant.Contains("ambient")
                                            && clone.HasProperty("_AmbientMinimalValue"))
                                        {
                                            clone.SetFloat("_AmbientMinimalValue", 1f);
                                        }
                                        if (variant.Contains("noFog"))
                                        {
                                            foreach (var kw in clone.shaderKeywords.ToList())
                                            {
                                                if (kw.Contains("FOG")) clone.DisableKeyword(kw);
                                            }
                                        }
                                        if (variant.Contains("bothSides")
                                            && clone.HasProperty("_EnableBothSidesDiffuse"))
                                        {
                                            hasBothSidesProp = true;
                                            clone.SetFloat("_EnableBothSidesDiffuse", 1f);
                                            if (clone.HasProperty("_BothSidesDiffuseMultiplier"))
                                                clone.SetFloat("_BothSidesDiffuseMultiplier", 1f);
                                            clone.EnableKeyword("BOTH_SIDES_DIFFUSE");
                                            bothSidesKw = clone.IsKeywordEnabled("BOTH_SIDES_DIFFUSE");
                                        }
                                        clones[m] = clone;
                                    }
                                    for (var i = 0; i < groupRenderers.Count; i++)
                                    {
                                        if (clones.TryGetValue(originals[i], out var clone))
                                            groupRenderers[i].sharedMaterial = clone;
                                    }
                                    camera.Render();
                                    ReadRenderTexture(rt, toggled);
                                }
                                finally
                                {
                                    for (var i = 0; i < groupRenderers.Count; i++)
                                        groupRenderers[i].sharedMaterial = originals[i];
                                    foreach (var clone in clones.Values) Object.Destroy(clone);
                                }
                                var d = DiffFrames(baseline, toggled);
                                var mt = MeasureFrame(toggled);
                                abLog.AppendLine(
                                    $"  {groupLabel} {variant}: maxDiff={d.MaxDiff} " +
                                    $"changedPixels={d.Count} " +
                                    $"nearWhite c/o={mt.CenterWhite}/{mt.OuterWhite} " +
                                    $"saturatedPanel={mt.SaturatedPanel} " +
                                    $"lumCenter={mt.CenterLum:F3} lumEdge={mt.EdgeLum:F3}" +
                                    (variant.Contains("bothSides")
                                        ? $" bothSidesProp={hasBothSidesProp} bothSidesKw={bothSidesKw}"
                                        : ""));
                            }
                        }

                        // Front-vs-back probe: render once from behind the grid facing -z,
                        // then restore the camera transform.
                        var camTransform = camera.transform;
                        var savedPos = camTransform.position;
                        var savedRot = camTransform.rotation;
                        try
                        {
                            camTransform.position = new Vector3(savedPos.x, savedPos.y, 20f);
                            camTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
                            camera.Render();
                            ReadRenderTexture(rt, toggled);
                            var backMetrics = MeasureFrame(toggled);
                            abLog.AppendLine(
                                $"  behind-grid (z=20 facing -z): nearWhite c/o=" +
                                $"{backMetrics.CenterWhite}/{backMetrics.OuterWhite} " +
                                $"saturatedPanel={backMetrics.SaturatedPanel} " +
                                $"lumCenter={backMetrics.CenterLum:F3} lumEdge={backMetrics.EdgeLum:F3}");
                        }
                        finally
                        {
                            camTransform.position = savedPos;
                            camTransform.rotation = savedRot;
                        }
                    }
                    finally
                    {
                        camera.targetTexture = previousTarget;
                        Object.Destroy(rt);
                        Object.Destroy(baseline);
                        Object.Destroy(toggled);
                    }
                    Debug.Log(abLog.ToString());
                }
            }
        }

        private static (int CenterWhite, int OuterWhite, int SaturatedPanel,
            float CenterLum, float EdgeLum) MeasureFrame(Texture2D t)
        {
            var centerWhite = 0;
            var outerWhite = 0;
            var saturatedPanel = 0;
            float centerLum = 0f;
            float edgeLum = 0f;
            var centerCount = 0;
            var edgeCount = 0;
            // Panel viewport x=0.27..0.75, y=0.22..0.72 mapped to pixel space (approximate -
            // the panel vp rect came from the a_* group aggregate, not a fitted quad).
            var pxMin = Mathf.RoundToInt(0.27f * t.width);
            var pxMax = Mathf.RoundToInt(0.75f * t.width);
            var pyMin = Mathf.RoundToInt(0.22f * t.height);
            var pyMax = Mathf.RoundToInt(0.72f * t.height);
            for (var y = 0; y < t.height; y++)
            {
                for (var x = 0; x < t.width; x++)
                {
                    var p = t.GetPixel(x, y);
                    var isCenter = x >= t.width / 4 && x < t.width * 3 / 4
                        && y >= t.height / 4 && y < t.height * 3 / 4;
                    var lum = 0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b;
                    if (p.r >= 0.9f && p.g >= 0.9f && p.b >= 0.9f)
                    {
                        if (isCenter) centerWhite++; else outerWhite++;
                    }
                    if (x >= pxMin && x < pxMax && y >= pyMin && y < pyMax)
                    {
                        var max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                        var min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                        if (max > 0.4f && max - min > 0.25f) saturatedPanel++;
                    }
                    var isEdge = x < t.width / 8 || x >= t.width * 7 / 8
                        || y < t.height / 8 || y >= t.height * 7 / 8;
                    if (isCenter) { centerLum += lum; centerCount++; }
                    else if (isEdge) { edgeLum += lum; edgeCount++; }
                }
            }
            return (centerWhite, outerWhite, saturatedPanel,
                centerCount == 0 ? 0f : centerLum / centerCount,
                edgeCount == 0 ? 0f : edgeLum / edgeCount);
        }

        private static (int MaxDiff, int Count, int MinX, int MinY, int MaxX, int MaxY,
            int CenterWhite, int OuterWhite) DiffFrames(Texture2D a, Texture2D b)
        {
            var maxDiff = 0;
            var count = 0;
            var minX = a.width;
            var minY = a.height;
            var maxX = -1;
            var maxY = -1;
            var centerWhite = 0;
            var outerWhite = 0;
            for (var y = 0; y < a.height; y++)
            {
                for (var x = 0; x < a.width; x++)
                {
                    var pa = a.GetPixel(x, y);
                    var pb = b.GetPixel(x, y);
                    var diff = Mathf.RoundToInt(255f * Mathf.Max(
                        Mathf.Abs(pa.r - pb.r),
                        Mathf.Max(Mathf.Abs(pa.g - pb.g), Mathf.Abs(pa.b - pb.b))));
                    if (diff > maxDiff) maxDiff = diff;
                    if (diff >= 8)
                    {
                        count++;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                    if (pa.r >= 0.9f && pa.g >= 0.9f && pa.b >= 0.9f)
                    {
                        if (x >= a.width / 4 && x < a.width * 3 / 4
                            && y >= a.height / 4 && y < a.height * 3 / 4)
                            centerWhite++;
                        else
                            outerWhite++;
                    }
                }
            }
            return (maxDiff, count, count == 0 ? -1 : minX, count == 0 ? -1 : minY, maxX, maxY,
                centerWhite, outerWhite);
        }

        // The freshly re-downloaded ExpertPlusStandard.dat at FullMapPath carries the published
        // file's explicit shaderKeywords:[] on every named Standard material — the precondition is
        // asserted so this variant re-runs the beat-124 visual assertions on original authored data
        // (Chroma upgrades Standard with an explicitly empty shaderKeywords array to the unlit
        // Glowing material).
        [UnityTest]
        [Explicit]
        public IEnumerator PublishedMapTvPanelHasAuthoredUnlitColors()
        {
            var raw = JSON.Parse(File.ReadAllText(FullMapPath));
            Assert.That(raw["customData"]["materials"]["whitebg"]["shaderKeywords"].IsArray, Is.True);
            Assert.That(raw["customData"]["materials"]["whitebg"]["shaderKeywords"].Count, Is.Zero);
            yield return TestUtils.ReloadMap(3, raw, beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();

            // Round-trip through the real save path in memory: serialization must keep each
            // material's explicit shaderKeywords:[] so a save+reload cannot silently revert
            // these surfaces to lit Standard (the installed map was once corrupted this way).
            var output = Beatmap.V3.V3Difficulty.GetOutputJson(BeatSaberSongContainer.Instance.Map);
            foreach (var name in raw["customData"]["materials"].AsObject.Keys)
            {
                var saved = output["customData"]["materials"][name]["shaderKeywords"];
                Assert.That(saved.IsArray, Is.True,
                    $"material '{name}' lost its shaderKeywords key on save.");
                Assert.That(saved.AsArray.Count, Is.Zero,
                    $"material '{name}' shaderKeywords is not empty after save.");
            }

            // Serialize to text and re-parse so the second pass reads exactly what a real save
            // writes — the original JSONNode would keep referencing the live map's parsed data.
            var savedText = output.ToString();
            var artifactDir = Path.Combine(Application.temporaryCachePath, "CensoredMaterialParity",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(artifactDir);
            var savedPath = Path.Combine(artifactDir, "roundtripped-ExpertPlusStandard.json");
            File.WriteAllText(savedPath, savedText);
            Debug.Log($"[CensoredMat] serialized round-trip map written to {savedPath}");

            yield return TestUtils.ReloadMap(3, JSON.Parse(savedText), beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();
        }

        // Lifecycle discriminator for the round-trip centering drift above: reload the SAME
        // original file a second time while still in Playing mode. A fresh parse each load
        // separates data differences produced by serialization from reload-lifecycle state.
        [UnityTest]
        [Explicit]
        public IEnumerator FreshOriginalReloadWhilePlayingKeepsTvAligned()
        {
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(FullMapPath)),
                beatsPerMinute: 195, environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();

            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(FullMapPath)),
                beatsPerMinute: 195, environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();
        }

        // Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround pins the game's b115.234
        // and b124 appearance from the user's paired screenshots: the a_* colour grid and the redcen_*
        // red text must be viewport-x-centered on the theaaaa black backing, the space whitebg
        // surround must read near-white, and the colour bars must be saturated. Currently RED:
        // text renders at vp.x~0.21 vs panel~0.51, outer near-white ~252 (expected >50000),
        // saturated panel pixels ~326 (expected >10000).
        [UnityTest]
        [Explicit]
        public IEnumerator Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround()
        {
            var map = BeatSaberSongContainer.Instance.Map;
            Assert.That(map, Is.Not.Null, "Diagnostic precondition: the full map should be loaded.");
            Assert.That(map.EnvironmentEnhancements.Where(e => e.Geometry != null), Is.Not.Empty,
                "Diagnostic precondition: the map should author geometry enhancements.");

            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var failures = new List<string>();
            var rt = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var probe = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var captureDir = Path.Combine(Application.temporaryCachePath, "CensoredMaterialParity",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(captureDir);
            var previousTarget = camera.targetTexture;
            try
            {
                foreach (var (beatLabel, beat) in new[]
                {
                    ("115.234", 115.234f),
                    ("123.75", 123.75f), ("124", 124f), ("124.25", 124.25f),
                    ("125", 125f), ("125->124", 124f),
                })
                {
                    atsc.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;

                    var geometries = Object.FindObjectsByType<GeometryContainer>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(c => c != null && c.EnvironmentEnhancement != null)
                        .ToList();
                    List<Renderer> LiveCubes(System.Predicate<GeometryContainer> pred) =>
                        geometries.Where(c => pred(c))
                            .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                            .Where(r => r != null && r.name == "Cube"
                                && r.enabled && r.gameObject.activeInHierarchy)
                            .ToList();

                    var panelCubes = LiveCubes(c =>
                        c.EnvironmentEnhancement.Track != null
                        && c.EnvironmentEnhancement.Track.StartsWith("a_"));
                    var textCubes = LiveCubes(c =>
                        c.EnvironmentEnhancement.Track != null
                        && c.EnvironmentEnhancement.Track.StartsWith("redcen_"));
                    var backingCubes = LiveCubes(c =>
                        c.EnvironmentEnhancement.Track == "theaaaa");

                    var beatLog = new System.Text.StringBuilder();
                    beatLog.AppendLine(
                        $"=== beat {beatLabel}: camera pos={camera.transform.position} " +
                        $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} | " +
                        $"a_* cubes={panelCubes.Count} redcen_* cubes={textCubes.Count} " +
                        $"theaaaa cubes={backingCubes.Count} ===");

                    if (beatLabel == "115.234" || beatLabel == "124" || beatLabel == "125->124")
                    {
                        if (panelCubes.Count == 0)
                            failures.Add($"beat {beatLabel}: no live a_* colour grid cubes");
                        if (textCubes.Count == 0)
                            failures.Add($"beat {beatLabel}: no live redcen_* text cubes");
                        if (backingCubes.Count == 0)
                            failures.Add($"beat {beatLabel}: no live theaaaa backing cube");
                        if (panelCubes.Count > 0 && textCubes.Count > 0 && backingCubes.Count > 0)
                        {
                            Bounds Combine(List<Renderer> rs)
                            {
                                var b = rs[0].bounds;
                                foreach (var r in rs) b.Encapsulate(r.bounds);
                                return b;
                            }
                            var panelX = camera.WorldToViewportPoint(Combine(panelCubes).center).x;
                            var textX = camera.WorldToViewportPoint(Combine(textCubes).center).x;
                            var backX = camera.WorldToViewportPoint(Combine(backingCubes).center).x;
                            beatLog.AppendLine(
                                $"  centers vpX: panel={panelX:F3} text={textX:F3} backing={backX:F3}");
                            // Ancestry probe: which link of the censson parent chain carries the
                            // text off-centre on a save/reload round-trip.
                            var redcenSample = geometries.FirstOrDefault(
                                c => c.EnvironmentEnhancement.Track == "redcen_10_9");
                            if (redcenSample != null)
                            {
                                var ancestry = new System.Text.StringBuilder();
                                var depth = 0;
                                for (var t = redcenSample.transform;
                                     t != null && depth++ < 12;
                                     t = t.parent)
                                    ancestry.Append(
                                        $"{t.name}[lp={t.localPosition} lr={t.localRotation.eulerAngles}]<-");
                                beatLog.AppendLine($"  redcen_10_9 ancestry: {ancestry}");
                            }
                            if (Mathf.Abs(textX - panelX) > 0.08f)
                            {
                                failures.Add(
                                    $"beat {beatLabel}: red text vpX={textX:F3} is not centred on " +
                                    $"the colour panel vpX={panelX:F3} (|dx|={Mathf.Abs(textX - panelX):F3} > 0.08)");
                            }
                            if (Mathf.Abs(textX - backX) > 0.08f)
                            {
                                failures.Add(
                                    $"beat {beatLabel}: red text vpX={textX:F3} is not centred on " +
                                    $"the black backing vpX={backX:F3} (|dx|={Mathf.Abs(textX - backX):F3} > 0.08)");
                            }
                        }

                        camera.targetTexture = rt;
                        camera.Render();
                        ReadRenderTexture(rt, frame);
                        camera.targetTexture = previousTarget;
                        var metrics = MeasureFrame(frame);
                        beatLog.AppendLine(
                            $"  frame: nearWhite c/o={metrics.CenterWhite}/{metrics.OuterWhite} " +
                            $"saturatedPanel={metrics.SaturatedPanel} " +
                            $"lumCenter={metrics.CenterLum:F3} lumEdge={metrics.EdgeLum:F3}");
                        if (metrics.OuterWhite <= 50000)
                        {
                            failures.Add(
                                $"beat {beatLabel}: whitebg surround should read near-white " +
                                $"(outer near-white={metrics.OuterWhite}, expected >50000)");
                        }
                        if (metrics.SaturatedPanel <= 10000)
                        {
                            failures.Add(
                                $"beat {beatLabel}: colour bars should be saturated " +
                                $"(saturatedPanel={metrics.SaturatedPanel}, expected >10000)");
                        }

                        var pngPath = Path.Combine(captureDir,
                            $"frame-{beatLabel.Replace("->", "to")}.png");
                        File.WriteAllBytes(pngPath, frame.EncodeToPNG());
                        beatLog.AppendLine($"  capture: {pngPath}");

                        // Contribution evidence: hiding the text renderers must remove solid red
                        // pixels and hiding the black backing must reveal bright content; a
                        // washed-out glow would not produce these deltas on the lit panel.
                        var basePx = frame.GetPixels32();
                        var toggled = new List<Renderer>();
                        camera.targetTexture = rt;
                        try
                        {
                            foreach (var r in textCubes) { r.enabled = false; toggled.Add(r); }
                            camera.Render();
                            ReadRenderTexture(rt, probe);
                        }
                        finally
                        {
                            foreach (var r in toggled) r.enabled = true;
                            camera.targetTexture = previousTarget;
                        }
                        var probePx = probe.GetPixels32();
                        var redTextPixels = 0;
                        for (var i = 0; i < basePx.Length; i++)
                        {
                            if (basePx[i].r >= 180 && basePx[i].g <= 70 && basePx[i].b <= 70
                                && basePx[i].r - probePx[i].r >= 30) redTextPixels++;
                        }
                        beatLog.AppendLine($"  redText lostPixels={redTextPixels}");
                        if (redTextPixels <= 200)
                        {
                            failures.Add($"beat {beatLabel}: hiding redcen_* removed only " +
                                $"{redTextPixels} red pixels (expected >200; text is not " +
                                "rendering as visible red)");
                        }

                        toggled.Clear();
                        camera.targetTexture = rt;
                        try
                        {
                            foreach (var r in backingCubes) { r.enabled = false; toggled.Add(r); }
                            camera.Render();
                            ReadRenderTexture(rt, probe);
                        }
                        finally
                        {
                            foreach (var r in toggled) r.enabled = true;
                            camera.targetTexture = previousTarget;
                        }
                        probePx = probe.GetPixels32();
                        var revealedPixels = 0;
                        for (var i = 0; i < basePx.Length; i++)
                        {
                            var b = basePx[i];
                            var n = probePx[i];
                            if (Mathf.Max(b.r, Mathf.Max(b.g, b.b)) <= 20
                                && Mathf.Max(n.r, Mathf.Max(n.g, n.b)) >= 80) revealedPixels++;
                        }
                        beatLog.AppendLine($"  backing revealedPixels={revealedPixels}");
                        if (revealedPixels <= 100)
                        {
                            failures.Add($"beat {beatLabel}: hiding theaaaa revealed only " +
                                $"{revealedPixels} bright pixels (expected >100; the black " +
                                "backing is not occluding bright content)");
                        }
                    }
                    else
                    {
                        foreach (var group in geometries.GroupBy(c => c.EnvironmentEnhancement.Track))
                        {
                            beatLog.AppendLine(
                                $"  group track={(group.Key ?? "<null>")} containers={group.Count()}");
                        }
                    }
                    Debug.Log(beatLog.ToString());
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(rt);
                Object.Destroy(frame);
                Object.Destroy(probe);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = animationsBeforeTest;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
