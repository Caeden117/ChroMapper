using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaEnvironmentRoutingTest : TestBase
    {
        private const string BoxId = "BTSEnvironment.[0]Environment.[22]PillarPair (3).[1]PillarR.[2]LaserR.[1]BoxLight";
        private const string FirstTrackName = "routingFirst";
        private const string SecondTrackName = "routingSecond";
        private const string ScaleTrackName = "routingScale";
        private const string MaterialName = "routingMaterial";
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        // Heck's shared controller sums positions and multiplies scales from every environment track.
        [UnityTest]
        public IEnumerator EnvironmentTrackArraysPreserveBindingsAndComposeTransforms()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = OemEnhancement(version);
                enhancement[Key(version, "track")] = JSON.Parse("[\"routingFirst\",\"routingSecond\",\"routingScale\"]");
                var events = new JSONArray();
                events.Add(TrackEvent(version, FirstTrackName, "position", "[3,4,5]"));
                events.Add(TrackEvent(version, SecondTrackName, "position", "[2,3,4]"));
                events.Add(TrackEvent(version, ScaleTrackName, "scale", "[2,3,4]"));
                yield return Load(version, Difficulty(version, enhancement, events));
                yield return Seek(4f);

                var target = BoxTarget();
                var manager = Object.FindAnyObjectByType<TracksManager>();
                foreach (var name in new[] { FirstTrackName, SecondTrackName, ScaleTrackName })
                {
                    if (!manager.GetAnimationTrack(name).Children.Any(child => child.LocalTarget == target))
                        failures.Add($"V{version}: OEM target is not bound to authored track '{name}'.");

                }

                RecordVector(
                    failures,
                    target.position,
                    new Vector3(5f, 7f, 9f) * PositionUnits(version),
                    $"V{version}: composed world position");
                RecordVector(
                    failures,
                    target.localScale,
                    new Vector3(2f, 3f, 4f),
                    $"V{version}: disjoint scale track");
                var parsed = BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Single();
                RecordTrackExport(failures, parsed.ToJson(), enhancement, version, "enhancement");
                RecordTrackExport(failures, parsed.Clone().ToJson(), enhancement, version, "cloned enhancement");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Chroma's material animator replaces authored color with the product of track RGBA values.
        [UnityTest]
        public IEnumerator MaterialTrackArraysPreserveBindingsAndMultiplyColor()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var material = new JSONObject
                {
                    [Key(version, "shader")] = "Standard",
                    [Key(version, "color")] = JSON.Parse("[1,1,1,1]"),
                    [Key(version, "track")] = JSON.Parse("[\"routingFirst\",\"routingSecond\"]")
                };
                var enhancement = GeometryEnhancement(version);
                enhancement[Key(version, "geometry")][Key(version, "material")] = MaterialName;
                var events = new JSONArray();
                events.Add(TrackEvent(version, FirstTrackName, "color", "[0.5,0.6,0.7,0.8]"));
                events.Add(TrackEvent(version, SecondTrackName, "color", "[0.4,0.5,0.6,0.7]"));
                var data = Difficulty(version, enhancement, events);
                data[Key(version, "customData")][Key(version, "materials")] = new JSONObject
                {
                    [MaterialName] = material
                };
                yield return Load(version, data);
                yield return Seek(4f);

                var container = Geometry();
                var manager = Object.FindAnyObjectByType<TracksManager>();
                foreach (var name in new[] { FirstTrackName, SecondTrackName })
                {
                    if (!manager.GetAnimationTrack(name).Children.Contains(container.MaterialAnimator))
                        failures.Add($"V{version}: material animator is not bound to authored track '{name}'.");

                }

                var expected = new Color(0.2f, 0.3f, 0.42f, 0.56f);
                var colorId = Shader.PropertyToID("_Color");
                var block = new MaterialPropertyBlock();
                foreach (var renderer in container.MpbController.Renderers)
                {
                    renderer.GetPropertyBlock(block);
                    var actual = block.GetColor(colorId);
                    if (Vector4.Distance(actual, expected) > 0.0001f)
                        failures.Add($"V{version}: rendered material RGBA={actual}, native track product={expected}.");

                }

                var parsed = BeatSaberSongContainer.Instance.Map.Materials[MaterialName];
                RecordTrackExport(failures, parsed.ToJson(), material, version, "material");
                RecordTrackExport(failures, parsed.Clone().ToJson(), material, version, "cloned material");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // EnvironmentEnhancementManager asks TransformControllerFactory to overwrite earlier bindings.
        [UnityTest]
        public IEnumerator LaterEnvironmentTrackBindingReplacesEarlierController()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var first = OemEnhancement(version);
                first[Key(version, "track")] = FirstTrackName;
                var last = OemEnhancement(version);
                last[Key(version, "track")] = ScaleTrackName;
                var events = new JSONArray();
                events.Add(TrackEvent(version, FirstTrackName, "position", "[10,20,30]"));
                events.Add(TrackEvent(version, ScaleTrackName, "scale", "[2,3,4]"));
                var data = Difficulty(version, first, events);
                data[Key(version, "customData")][Key(version, "environment")].Add(last);
                yield return Load(version, data);
                yield return Seek(4f);

                var target = BoxTarget();
                RecordVector(
                    failures,
                    target.position,
                    new Vector3(1f, 2f, 3f) * PositionUnits(version),
                    $"V{version}: superseded track must not keep writing position");
                RecordVector(
                    failures,
                    target.localScale,
                    new Vector3(2f, 3f, 4f),
                    $"V{version}: replacement track scale");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator GeometryHonorsExplicitInactiveEnhancement()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = GeometryEnhancement(version);
                enhancement[Key(version, "active")] = false;
                yield return Load(version, Difficulty(version, enhancement, new JSONArray()));
                var container = Geometry();
                if (container.gameObject.activeSelf)
                    failures.Add($"V{version}: active:false geometry is active after enhancement loading.");

                foreach (var renderer in container.MpbController.Renderers)
                {
                    if (renderer.gameObject.activeInHierarchy)
                        failures.Add($"V{version}: active:false geometry leaves a rendered child active.");

                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator FiveEnvironmentTracksComposePositionScaleAndRotation()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = OemEnhancement(version);
                var names = new JSONArray();
                var events = new JSONArray();
                for (var i = 1; i <= 5; i++)
                {
                    var name = "environmentContributor" + i;
                    names.Add(name);
                    var ev = TrackEvent(version, name, "position", $"[{i},{i * 2},{i * 3}]");
                    var data = ev[
                        version == 2
                            ? "_data"
                            : "d"];
                    data[Key(version, "scale")] = JSON.Parse("[2,2,2]");
                    data[Key(version, "rotation")] = JSON.Parse("[0,0,15]");
                    events.Add(ev);
                }

                enhancement[Key(version, "track")] = names;
                yield return Load(version, Difficulty(version, enhancement, events));
                yield return Seek(4f);
                var target = BoxTarget();
                RecordVector(
                    failures,
                    target.position,
                    new Vector3(15f, 30f, 45f) * PositionUnits(version),
                    $"V{version}: all five position tracks");
                RecordVector(
                    failures,
                    target.localScale,
                    Vector3.one * 32f,
                    $"V{version}: all five scale tracks");
                if (Quaternion.Angle(target.rotation, Quaternion.Euler(0f, 0f, 75f)) > 0.01f)
                    failures.Add($"V{version}: all five rotation tracks produced {target.rotation.eulerAngles}, expected Z=75 degrees.");

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator ArrayColorEventsSuggestChromaForTrackOnlyObjects()
        {
            foreach (var version in new[] { 2, 3 })
            {
                var ev = TrackEvent(version, FirstTrackName, "color", "[0.8,0.7,0.6,1]");
                ev[
                    version == 2
                        ? "_data"
                        : "d"][Key(version, "track")] =
                    JSON.Parse("[\"routingFirst\",\"routingSecond\"]");
                var events = new JSONArray();
                events.Add(ev);
                var note = version == 2
                    ? JSON.Parse("{\"_time\":10,\"_lineIndex\":0,\"_lineLayer\":0,\"_type\":0,\"_cutDirection\":1}")
                    : JSON.Parse("{\"b\":10,\"x\":0,\"y\":0,\"c\":0,\"d\":1}");
                note[Key(version, "customData")] = new JSONObject { [Key(version, "track")] = FirstTrackName };
                var notes = new JSONArray();
                notes.Add(note);
                var data = new JSONObject
                {
                    [Key(version, "version")] = version == 2
                        ? "2.6.0"
                        : "3.2.0",
                    [
                        version == 2
                            ? "_notes"
                            : "colorNotes"] = notes,
                    [Key(version, "customData")] = new JSONObject { [Key(version, "customEvents")] = events }
                };
                yield return Load(version, data);
                var song = BeatSaberSongContainer.Instance;
                Assert.That(song.Map.IsChroma(), Is.False, "The control must require track-color classification, not a static color or enhancement.");
                Assert.That(
                    new ChromaReq().IsRequiredOrSuggested(song.MapDifficultyInfo, song.Map),
                    Is.EqualTo(RequirementCheck.RequirementType.Suggestion),
                    $"V{version}: accepted array track color did not suggest Chroma for its bound object.");
            }
        }

        [UnityTest]
        public IEnumerator AllLookupMethodsApplyVersionedTransformsAndInactiveState()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                foreach (var method in new[] { "Exact", "Contains", "StartsWith", "EndsWith", "Regex" })
                {
                    var id = method switch
                    {
                        "Contains" => BoxId.Substring(BoxId.IndexOf("[22]", System.StringComparison.Ordinal)),
                        "StartsWith" => BoxId.Substring(0, BoxId.Length - 5),
                        "EndsWith" => BoxId.Substring(BoxId.IndexOf(".[0]", System.StringComparison.Ordinal)),
                        "Regex" => "^" + System.Text.RegularExpressions.Regex.Escape(BoxId) + "$",
                        _ => BoxId
                    };
                    var enhancement = OemEnhancement(version);
                    enhancement[Key(version, "id")] = id;
                    enhancement[Key(version, "lookupMethod")] = method;
                    enhancement[Key(version, "localPosition")] = JSON.Parse("[4,5,6]");
                    enhancement[Key(version, "rotation")] = JSON.Parse("[10,20,30]");
                    enhancement[Key(version, "localRotation")] = JSON.Parse("[0,0,90]");
                    enhancement[Key(version, "scale")] = JSON.Parse("[2,3,4]");
                    enhancement[Key(version, "active")] = false;
                    yield return Load(version, Difficulty(version, enhancement, new JSONArray()));
                    var target = BoxTarget();
                    RecordVector(
                        failures,
                        target.localPosition,
                        new Vector3(4f, 5f, 6f) * PositionUnits(version),
                        $"V{version} {method}: localPosition precedence");
                    RecordVector(
                        failures,
                        target.localScale,
                        new Vector3(2f, 3f, 4f),
                        $"V{version} {method}: authored local scale");
                    if (Quaternion.Angle(target.localRotation, Quaternion.Euler(0f, 0f, 90f)) > 0.01f)
                        failures.Add($"V{version} {method}: localRotation did not override rotation.");

                    if (target.gameObject.activeSelf)
                        failures.Add($"V{version} {method}: active:false OEM target remained active.");

                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator ExplicitZeroDuplicationAndLegacyLightIdRoundTrip()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = OemEnhancement(version);
                enhancement[Key(version, "duplicate")] = 0;
                enhancement[Key(version, "active")] = false;
                if (version == 2)
                    enhancement["_lightID"] = 0;

                yield return Load(version, Difficulty(version, enhancement, new JSONArray()));
                var output = BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Single().ToJson();
                var duplicateKey = Key(version, "duplicate");
                var duplicate = output.GetValueOrDefault(duplicateKey, null);
                if (duplicate == null || duplicate.AsInt != 0)
                    failures.Add($"V{version}: dropping duplicate:0 changes later reload into an original-object enhancement.");

                if (version == 2)
                {
                    var lightId = output.GetValueOrDefault("_lightID", null);
                    if (lightId == null || lightId.AsInt != 0)
                        failures.Add("Legacy light ID zero is an accepted requested ID and must survive export.");

                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator LightComponentRequestsRetargetAndRegisterEveryMatchedDescendant()
        {
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = OemEnhancement(version);
                enhancement[Key(version, "id")] = BoxId.Substring(0, BoxId.LastIndexOf('.'));
                if (version == 2)
                {
                    enhancement["_duplicate"] = 1;
                    enhancement["_lightID"] = 10000;
                }

                else
                    enhancement["components"] = new JSONObject
                    {
                        ["ILightWithId"] = new JSONObject { ["lightID"] = 10000, ["type"] = 1 }
                    };

                yield return Load(version, Difficulty(version, enhancement, new JSONArray()));
                var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
                var targetId = enhancement[Key(version, "id")].Value;
                var target = descriptor.ChromaIDMarkers.Single(
                    marker => version == 2
                        ? marker.ChromaID.EndsWith("LaserR(Clone)", System.StringComparison.Ordinal)
                        && marker.ChromaID.StartsWith(targetId.Substring(0, targetId.LastIndexOf('.') + 1), System.StringComparison.Ordinal)
                        : marker.ChromaID == targetId);
                var controllers = target.GetComponentsInChildren<LightController>(true);
                Assert.That(controllers, Is.Not.Empty, "The canonical request must reach an existing OEM light family.");
                foreach (var controller in controllers)
                {
                    if (version == 3)
                        Assert.That(controller.Type, Is.EqualTo(1), "ILightWithId.type must retarget descendants, not just the matched marker.");

                    var effect = descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(controller.Type);
                    Assert.That(
                        effect.LightIdRemapEntries.Any(entry => entry.x >= 10000 && entry.y == controller.ID),
                        Is.True,
                        $"V{version}: requested light ID table has no entry for descendant {controller.name}.");
                }
            }
        }

        [UnityTest]
        public IEnumerator LaterArrayTrackAnimationUpdatesBesideAHeldFirstTrack()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = OemEnhancement(version);
                enhancement[Key(version, "track")] = JSON.Parse("[\"routingFirst\",\"routingSecond\"]");
                var events = new JSONArray();
                var first = TrackEvent(version, FirstTrackName, "position", "[1,0,0]");
                first[
                    version == 2
                        ? "_time"
                        : "b"] = 0f;
                events.Add(first);
                var second = TrackEvent(version, SecondTrackName, "position", "[[0,0,0,0],[5,0,0,1]]");
                second[
                    version == 2
                        ? "_data"
                        : "d"][Key(version, "duration")] = 4f;
                events.Add(second);
                yield return Load(version, Difficulty(version, enhancement, events));
                var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
                var previousClockEnabled = clock.enabled;
                clock.enabled = false;
                try
                {
                    TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 0f);
                    var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                    foreach (var beat in new[] { 1f, 3f, 4f, 6f })
                    {
                        seconds.SetValue(clock, clock.GetSecondsFromBeat(beat));
                        yield return null;
                        yield return null;
                        var contribution = Mathf.Clamp01((beat - 2f) / 4f) * 5f;
                        RecordVector(
                            failures,
                            BoxTarget().position,
                            new Vector3(1f + contribution, 0f, 0f) * PositionUnits(version),
                            $"V{version} b{beat}: held first track cannot suppress later track animation");
                    }
                }

                finally
                {
                    if (clock.IsPlaying)
                        TestUtils.PauseDeterministicPlayback(clock);

                    clock.enabled = previousClockEnabled;
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator MaterialTrackPathColorsUseNormalizedZeroAndMultiplyGlobalColors()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = GeometryEnhancement(version);
                enhancement[Key(version, "geometry")][Key(version, "material")] = new JSONObject
                {
                    [Key(version, "shader")] = "Standard", [Key(version, "color")] = JSON.Parse("[1,1,1,1]"),
                    [Key(version, "track")] = JSON.Parse("[\"routingFirst\",\"routingSecond\"]")
                };
                var events = new JSONArray();
                var first = TrackEvent(version, FirstTrackName, "color", "[[0.5,0.6,0.7,0.8,0],[0.9,0.9,0.9,0.9,1]]");
                first[
                    version == 2
                        ? "_type"
                        : "t"] = "AssignPathAnimation";
                events.Add(first);
                var second = TrackEvent(version, SecondTrackName, "color", "[[0.4,0.5,0.6,0.7,0],[0.9,0.9,0.9,0.9,1]]");
                second[
                    version == 2
                        ? "_type"
                        : "t"] = "AssignPathAnimation";
                events.Add(second);
                events.Add(TrackEvent(version, FirstTrackName, "color", "[0.8,0.7,0.6,0.5]"));
                yield return Load(version, Difficulty(version, enhancement, events));
                foreach (var beat in new[] { 4f, 6f, 4f })
                {
                    yield return Seek(beat);
                    var block = new MaterialPropertyBlock();
                    var expected = new Color(0.16f, 0.21f, 0.252f, 0.28f);
                    foreach (var renderer in Geometry().MpbController.Renderers)
                    {
                        renderer.GetPropertyBlock(block);
                        var actual = block.GetColor("_Color");
                        if (Vector4.Distance(actual, expected) > 0.0001f)
                            failures.Add($"V{version} b{beat}: material path/global RGBA={actual}, native normalized-zero product={expected}.");

                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator LaterEnhancementsFindDistinctClonesByNativeSiblingIds()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var parentId = BoxId.Substring(0, BoxId.LastIndexOf('.'));
                var firstCloneId = parentId + ".[2]BoxLight(Clone)";
                var secondCloneId = parentId + ".[3]BoxLight(Clone)";
                var duplicate = OemEnhancement(version);
                duplicate[Key(version, "duplicate")] = 2;
                var enhancement = new JSONObject
                {
                    [Key(version, "id")] = firstCloneId, [Key(version, "lookupMethod")] = "Exact",
                    [Key(version, "position")] = JSON.Parse("[7,8,9]")
                };
                var inactive = new JSONObject
                {
                    [Key(version, "id")] = secondCloneId, [Key(version, "lookupMethod")] = "Exact",
                    [Key(version, "active")] = false
                };
                var data = Difficulty(version, duplicate, new JSONArray());
                data[Key(version, "customData")][Key(version, "environment")].Add(enhancement);
                data[Key(version, "customData")][Key(version, "environment")].Add(inactive);
                yield return Load(version, data);
                var markers = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers;
                var clones = markers.Where(
                    marker => marker.name == "BoxLight(Clone)"
                    && marker.ChromaID.StartsWith(parentId + ".", System.StringComparison.Ordinal)).ToArray();
                Assert.That(clones.Length, Is.EqualTo(2), "The request must create two actual cloned OEM targets.");
                foreach (var expectedId in new[] { firstCloneId, secondCloneId })
                {
                    var matches = clones.Where(marker => marker.ChromaID == expectedId).ToArray();
                    if (matches.Length != 1)
                    {
                        failures.Add($"V{version}: later Exact lookup needs unique native clone ID {expectedId}; matches={matches.Length}.");
                        continue;
                    }

                    if (expectedId == firstCloneId)
                        RecordVector(
                            failures,
                            matches[0].transform.position,
                            new Vector3(7f, 8f, 9f) * PositionUnits(version),
                            $"V{version}: ordered clone enhancement");

                    else if (matches[0].gameObject.activeSelf)
                        failures.Add($"V{version}: second clone did not receive its independent inactive instruction.");

                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator RemovingMaterialPathAnimationRestoresAuthoredColorAcrossUndoRedo()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                var enhancement = GeometryEnhancement(version);
                enhancement[Key(version, "geometry")][Key(version, "material")] = new JSONObject
                {
                    [Key(version, "shader")] = "Standard", [Key(version, "color")] = JSON.Parse("[0.1,0.2,0.3,0.4]"),
                    [Key(version, "track")] = FirstTrackName
                };
                var events = new JSONArray();
                var ev = TrackEvent(version, FirstTrackName, "color", "[0.5,0.6,0.7,0.8]");
                ev[
                    version == 2
                        ? "_type"
                        : "t"] = "AssignPathAnimation";
                events.Add(ev);
                yield return Load(version, Difficulty(version, enhancement, events));
                yield return Seek(4f);
                RecordMaterialColor(failures, new Color(0.5f, 0.6f, 0.7f, 0.8f), $"V{version} animated control");
                var source = BeatSaberSongContainer.Instance.Map.CustomEvents.Single();
                BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(source, "Delete material path color"), true);
                yield return Seek(4f);
                RecordMaterialColor(failures, new Color(0.1f, 0.2f, 0.3f, 0.4f), $"V{version} deleting the only path event");
                var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
                actions.Undo();
                yield return Seek(4f);
                RecordMaterialColor(failures, new Color(0.5f, 0.6f, 0.7f, 0.8f), $"V{version} undoing deletion");
                actions.Redo();
                yield return Seek(4f);
                RecordMaterialColor(failures, new Color(0.1f, 0.2f, 0.3f, 0.4f), $"V{version} redoing deletion");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void RecordMaterialColor(List<string> failures, Color expected, string sample)
        {
            var block = new MaterialPropertyBlock();
            foreach (var renderer in Geometry().MpbController.Renderers)
            {
                renderer.GetPropertyBlock(block);
                var actual = block.GetColor("_Color");
                if (Vector4.Distance(actual, expected) > 0.0001f)
                    failures.Add($"{sample}: renderer RGBA={actual}, authored/native={expected}.");

            }
        }

        [UnityTest]
        public IEnumerator LaterExactEnhancementFindsCloneWhoseNameContainsADot()
        {
            const string parentId = "EDMEnvironment.[0]Environment.[20]SingleSourceLaserLeftMid";
            const string originalId = parentId + ".[0]SingleSourceLaser1.2";
            const string cloneId = parentId + ".[1]SingleSourceLaser1.2(Clone)";
            var duplicate = new JSONObject { ["id"] = originalId, ["lookupMethod"] = "Exact", ["duplicate"] = 1 };
            var data = Difficulty(3, duplicate, new JSONArray());
            data["customData"]["environment"].Add(
                new JSONObject
                {
                    ["id"] = cloneId, ["lookupMethod"] = "Exact", ["position"] = JSON.Parse("[7,8,9]")
                });
            yield return Load(3, data, "EDMEnvironment");
            var markers = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers;
            var clones = markers.Where(marker => marker.name == "SingleSourceLaser1.2(Clone)").ToArray();
            Assert.That(clones.Length, Is.EqualTo(1), "The native request must create the actual dotted-name clone.");
            Assert.That(clones[0].ChromaID, Is.EqualTo(cloneId), "Native sibling IDs preserve the complete game-object name.");
            Assert.That(
                Vector3.Distance(clones[0].transform.position, new Vector3(7f, 8f, 9f)),
                Is.LessThan(0.001f),
                "The later Exact instruction must reach the correctly identified clone.");
        }

        // Execute outside the load coroutine so the unfixed Regex exception cannot wedge scene loading.
        [UnityTest]
        public IEnumerator InvalidRegexLogsAndSkipsWhileFollowingEnhancementStillApplies()
        {
            yield return Load(3, new JSONObject { ["version"] = "3.2.0" });
            var collection = Object.FindAnyObjectByType<GeometryGridContainer>();
            var prefab = (GameObject)typeof(GeometryGridContainer)
                .GetField("geometryPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(collection);
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var manager = Object.FindAnyObjectByType<TracksManager>();
            var previousContainers = new HashSet<GeometryContainer>(
                Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            System.Exception lookupException = null;
            GeometryContainer rejected = null;
            var errorMessages = new List<string>();
            void CaptureLookupError(string message, string stack, LogType type)
            {
                if (type == LogType.Error
                    && message.StartsWith("Error processing environment data for:", System.StringComparison.Ordinal))
                {
                    errorMessages.Add(message);
                    LogAssert.Expect(LogType.Error, message);
                }
            }

            Application.logMessageReceived += CaptureLookupError;
            try
            {
                var malformed = new BaseEnvironmentEnhancement(
                    (JSONNode)new JSONObject
                {
                    ["id"] = "[", ["lookupMethod"] = "Regex"
                });
                try
                {
                    rejected = GeometryContainer.SpawnGeometry(malformed, ref prefab, context, manager);
                }

                catch (System.Exception exception)
                {
                    lookupException = exception;
                }

                var valid = new BaseEnvironmentEnhancement(
                    (JSONNode)new JSONObject
                {
                    ["id"] = BoxId, ["lookupMethod"] = "Exact", ["position"] = JSON.Parse("[7,8,9]")
                });
                var following = GeometryContainer.SpawnGeometry(valid, ref prefab, context, manager);
                Assert.That(following, Is.Not.Null, "A rejected instruction must leave the next valid request processable.");
                Assert.That(
                    Vector3.Distance(BoxTarget().position, new Vector3(7f, 8f, 9f)),
                    Is.LessThan(0.001f),
                    "Native per-instruction log-and-skip permits the following enhancement to change its target.");
                Assert.That(
                    lookupException,
                    Is.Null,
                    $"A malformed Regex request must log and skip instead of throwing into map loading: {lookupException}");
                Assert.That(errorMessages.Count, Is.EqualTo(1), "A skipped invalid Regex must log the native per-instruction error.");
                Assert.That(rejected, Is.Null, "A rejected request must not leave an owned geometry container.");
            }

            finally
            {
                Application.logMessageReceived -= CaptureLookupError;
                foreach (var created in Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!previousContainers.Contains(created))
                        Object.DestroyImmediate(created.gameObject);

                }
            }
        }

        [UnityTest]
        public IEnumerator QuotedNumericLookupMethodsUseNativeOrdinals()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                foreach (var ordinal in new[] { 0, 1, 2, 3, 4 })
                {
                    var id = ordinal switch
                    {
                        0 => "^" + System.Text.RegularExpressions.Regex.Escape(BoxId) + "$",
                        2 => "PillarPair (3).[1]PillarR.[2]LaserR.[1]BoxLight",
                        3 => BoxId.Substring(0, BoxId.Length - 5),
                        4 => ".[1]BoxLight",
                        _ => BoxId
                    };
                    var enhancement = new JSONObject
                    {
                        [Key(version, "id")] = id,
                        [Key(version, "lookupMethod")] = ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [Key(version, "position")] = JSON.Parse("[7,8,9]")
                    };
                    yield return Load(version, Difficulty(version, enhancement, new JSONArray()));
                    RecordVector(
                        failures,
                        BoxTarget().position,
                        new Vector3(7f, 8f, 9f) * PositionUnits(version),
                        $"V{version}: native quoted lookup ordinal{ordinal}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator HeldGeometryParentEditsMatchColdReloadAcrossUndoRedo()
        {
            var enhancement = GeometryEnhancement(3);
            enhancement["track"] = FirstTrackName;
            var events = new JSONArray();
            var children = new JSONArray();
            children.Add(FirstTrackName);
            events.Add(
                new JSONObject
                {
                    ["b"] = 0f, ["t"] = "AssignTrackParent",
                    ["d"] = new JSONObject
                    {
                        ["childrenTracks"] = children,
                        ["parentTrack"] = SecondTrackName,
                        ["worldPositionStays"] = true
                    }
                });
            var parent = TrackEvent(3, SecondTrackName, "rotation", "[[0,0,0,0],[0,0,90,1]]");
            parent["d"]["duration"] = 30f;
            events.Add(parent);
            var child = TrackEvent(3, FirstTrackName, "position", "[3,0,10]");
            child["d"]["rotation"] = JSON.Parse("[0,0,20]");
            child["d"]["duration"] = 12f;
            events.Add(child);
            var originalData = Difficulty(3, enhancement, events);
            yield return Load(3, originalData.Clone());
            yield return Seek(16f);
            var originalPosition = Geometry().transform.position;
            var originalRotation = Geometry().transform.rotation;
            var editedData = originalData.Clone();
            editedData["customData"]["customEvents"][1]["d"]["rotation"] = JSON.Parse("[[0,0,45,0],[0,0,135,1]]");
            yield return Load(3, editedData.Clone());
            yield return Seek(16f);
            var editedPosition = Geometry().transform.position;
            var editedRotation = Geometry().transform.rotation;
            yield return Load(3, originalData.Clone());
            yield return Seek(16f);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousClockEnabled = clock.enabled;
            clock.enabled = false;
            try
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 16f);
                yield return null;
                yield return null;
                var source = BeatSaberSongContainer.Instance.Map.CustomEvents.Single(ev => ev.CustomTrack?.Value == SecondTrackName);
                var edited = new Beatmap.Base.Customs.BaseCustomEvent((JSONNode)source.ToJson().Clone());
                edited.Data["rotation"] = JSON.Parse("[[0,0,45,0],[0,0,135,1]]");
                BeatmapActionContainer.AddAction(new BeatmapObjectUpdatedAction(edited, source, "Edit held geometry parent"), true);
                yield return null;
                yield return null;
                AssertHeldPose(editedPosition, editedRotation, "parent edit without seeking");
                var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
                actions.Undo();
                yield return null;
                yield return null;
                AssertHeldPose(originalPosition, originalRotation, "undo parent edit without seeking");
                actions.Redo();
                yield return null;
                yield return null;
                AssertHeldPose(editedPosition, editedRotation, "redo parent edit without seeking");
            }

            finally
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }
        }

        private static void AssertHeldPose(Vector3 position, Quaternion rotation, string operation)
        {
            var target = Geometry().transform;
            Assert.That(
                Vector3.Distance(target.position, position),
                Is.LessThan(0.001f),
                $"{operation}: held world position must match an independent cold reload of the same authored map.");
            Assert.That(
                Quaternion.Angle(target.rotation, rotation),
                Is.LessThan(0.01f),
                $"{operation}: held world rotation must match an independent cold reload of the same authored map.");
        }

        private IEnumerator Load(int version, JSONNode data, string environmentName = "BTSEnvironment")
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(version, data, environmentName: environmentName);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;
        }

        private static IEnumerator Seek(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        private static string Key(int version, string name) => version == 2
            ? "_" + name
            : name;

        private static float PositionUnits(int version) => version == 2
            ? 0.6f
            : 1f;

        private static JSONObject OemEnhancement(int version) => new()
        {
            [Key(version, "id")] = BoxId,
            [Key(version, "lookupMethod")] = "Exact",
            [Key(version, "position")] = JSON.Parse("[1,2,3]")
        };

        private static JSONObject GeometryEnhancement(int version) => new()
        {
            [Key(version, "geometry")] = new JSONObject
            {
                [Key(version, "type")] = "Cube",
                [Key(version, "material")] = new JSONObject
                {
                    [Key(version, "shader")] = "Standard",
                    [Key(version, "color")] = JSON.Parse("[1,1,1,1]")
                }
            }
        };

        private static JSONObject TrackEvent(int version, string track, string property, string points)
        {
            var data = new JSONObject
            {
                [Key(version, "track")] = track,
                [Key(version, property)] = JSON.Parse(points)
            };
            return version == 2
                ? new JSONObject { ["_time"] = 2f, ["_type"] = "AnimateTrack", ["_data"] = data }
                : new JSONObject { ["b"] = 2f, ["t"] = "AnimateTrack", ["d"] = data };
        }

        private static JSONObject Difficulty(int version, JSONObject enhancement, JSONArray events)
        {
            var environment = new JSONArray();
            environment.Add(enhancement);
            return new JSONObject
            {
                [Key(version, "version")] = version == 2
                    ? "2.6.0"
                    : "3.2.0",
                [Key(version, "customData")] = new JSONObject
                {
                    [Key(version, "environment")] = environment,
                    [Key(version, "customEvents")] = events
                }
            };
        }

        private static Transform BoxTarget() => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == BoxId).transform;

        private static GeometryContainer Geometry() => Object.FindAnyObjectByType<GeometryGridContainer>()
            .LoadedContainers.Values.OfType<GeometryContainer>().Single();

        private static void RecordVector(List<string> failures, Vector3 actual, Vector3 expected, string stage)
        {
            if (Vector3.Distance(actual, expected) > 0.001f)
                failures.Add($"{stage}: actual={actual}, native={expected}.");

        }

        private static void RecordTrackExport(
            List<string> failures,
            JSONNode output,
            JSONNode input,
            int version,
            string stage)
        {
            var trackKey = Key(version, "track");
            var expected = input.GetValueOrDefault(trackKey, null);
            var actual = output.GetValueOrDefault(trackKey, null);
            if (actual == null || actual.ToString() != expected.ToString())
                failures.Add($"V{version}: {stage} export lost authored track request {expected}; actual={actual}.");

        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
