using System.Collections;
using System.Linq;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Heck TransformData and TransformController choose localPosition over position
    // independently of JSON property order, then convert the chosen V2 vector by 0.6.
    public class EnvironmentPositionPrecedenceTest : TestBase
    {
        private const string BoxId =
            "BTSEnvironment.[0]Environment.[22]PillarPair (3).[1]PillarR.[2]LaserR.[1]BoxLight";
        private const string TrackName = "bothPositionKinds";
        private const string WorldOnlyTrackName = "worldPositionOnly";
        private const string ParentTrackName = "positionParent";
        private const string ChildTrackName = "positionChild";

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(3, CreateDifficulty(3), environmentName: "BTSEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Both the static environment enhancement and AnimateTrack carry two position
        // fields; Heck must choose the local value in V3 and V2, including on a stopped seek.
        [UnityTest]
        public IEnumerator LocalPositionWinsOverPositionInEnhancementAndTrackAnimation()
        {
            // The preceding parent case can leave a different scene fixture loaded;
            // establish this case's environment before inspecting its spawn pose.
            yield return TestUtils.ReloadMap(3, CreateDifficulty(3), environmentName: "BTSEnvironment");
            AssertBoxLocalPosition(new Vector3(3f, 4f, 5f), "V3 enhancement");
            AssertGeometryLocalPosition(new Vector3(3f, 4f, 5f), "V3 geometry enhancement");
            yield return SeekTo(4f);
            AssertBoxLocalPosition(new Vector3(7f, 8f, 9f), "V3 AnimateTrack");
            AssertGeometryLocalPosition(new Vector3(7f, 8f, 9f), "V3 geometry AnimateTrack");

            yield return TestUtils.ReloadMap(2, CreateDifficulty(2), environmentName: "BTSEnvironment");
            AssertBoxLocalPosition(new Vector3(1.8f, 2.4f, 3f), "V2 enhancement");
            AssertGeometryLocalPosition(new Vector3(1.8f, 2.4f, 3f), "V2 geometry enhancement");
            yield return SeekTo(4f);
            AssertBoxLocalPosition(new Vector3(4.2f, 4.8f, 5.4f), "V2 AnimateTrack");
            AssertGeometryLocalPosition(new Vector3(4.2f, 4.8f, 5.4f), "V2 geometry AnimateTrack");
        }

        // Heck writes Transform.position for a position-only geometry track. Moving
        // CM's track parent makes a mistaken localPosition write observable in both versions.
        [UnityTest]
        public IEnumerator PositionOnlyGeometryUsesWorldSpaceWithMovedParent()
        {
            yield return TestUtils.ReloadMap(3, CreateDifficulty(3), environmentName: "BTSEnvironment");
            yield return SeekTo(4f);
            yield return AssertWorldOnlyGeometry(new Vector3(90f, 80f, 70f), "V3 position");

            yield return TestUtils.ReloadMap(2, CreateDifficulty(2), environmentName: "BTSEnvironment");
            yield return SeekTo(4f);
            yield return AssertWorldOnlyGeometry(new Vector3(54f, 48f, 42f), "V2 _position");
        }

        // Heck's V3 ParentObject has a TransformController, so localPosition wins;
        // Noodle's V2 ParentObject reads _position as its offset and ignores _localPosition.
        [UnityTest]
        public IEnumerator AssignedTrackParentUsesVersionSpecificPositionOrder()
        {
            yield return TestUtils.ReloadMap(3, CreateParentDifficulty(3), environmentName: "BTSEnvironment");
            yield return SeekTo(4f);
            AssertChildGeometryWorldX(1f, "V3 parent localPosition");

            yield return TestUtils.ReloadMap(2, CreateParentDifficulty(2), environmentName: "BTSEnvironment");
            yield return SeekTo(4f);
            AssertChildGeometryWorldX(6f, "V2 Noodle parent _position");
        }

        // Heck's V3 parent controller sets rotation before a world position. A track
        // proxy split over two Unity transforms must preserve that final world point.
        [UnityTest]
        public IEnumerator ParentWorldPositionIsAppliedAfterTrackRotation()
        {
            yield return TestUtils.ReloadMap(3, CreateParentDifficulty(3, worldPositionWithRotation: true),
                environmentName: "BTSEnvironment");
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(4f);
            var childTrack = Object.FindAnyObjectByType<TracksManager>()
                .GetAnimationTrack(ChildTrackName).Track;
            // A stopped seek must land the rotated world position in the same OnTimeChanged pass;
            // deferring the check past a frame would hide a drained double-Get on WorldRotation.
            Assert.That(Quaternion.Angle(childTrack.transform.rotation, Quaternion.Euler(0f, 0f, 90f)),
                Is.LessThan(0.01f), "The fixture did not rotate the parent proxy in the seek frame.");
            AssertChildGeometryWorldX(10f, "V3 parent world position after rotation (seek frame)");
            yield return null;
            yield return null;
            Assert.That(Quaternion.Angle(childTrack.transform.rotation, Quaternion.Euler(0f, 0f, 90f)),
                Is.LessThan(0.01f), "The fixture did not rotate the parent proxy.");
            AssertChildGeometryWorldX(10f, "V3 parent world position after rotation");
        }

        // LoadedDifficultySelectController runs UpdateMapData+HardRefresh BEFORE assigning
        // the new map to the shared container, so RefreshTrackParentMapVersion captures the
        // outgoing map's version: a V3->V2 switch must still resolve the V2 _position field.
        [UnityTest]
        public IEnumerator SwitchToV2BeforeSharedMapSwapUsesIncomingMapVersion()
        {
            yield return TestUtils.ReloadMap(3, CreateParentDifficulty(3), environmentName: "BTSEnvironment");

            var songContainer = BeatSaberSongContainer.Instance;
            var previousMap = songContainer.Map;
            var previousRunnerVersion = Settings.TestRunnerSettings.MapVersion;
            var previousMapVersion = Settings.Instance.MapVersion;
            Settings.TestRunnerSettings.MapVersion = 2;
            try
            {
                var v2Map = BeatmapFactory.GetDifficultyFromJson(
                    CreateParentDifficulty(2),
                    "testmap",
                    songContainer.Info,
                    songContainer.MapDifficultyInfo);

                // Mirror the selector's ordering: refresh the incoming V2 data while the
                // shared container map still reports the outgoing V3 major version.
                var mapLoader = Object.FindAnyObjectByType<MapLoader>();
                mapLoader.UpdateMapData(v2Map);
                mapLoader.HardRefresh();
                songContainer.Map = v2Map;

                yield return SeekTo(4f);
                // The child track's ObjectParentTransform is the proxy the parent animator
                // drives: V2 writes _position 10*0.6=6, the stale V3 path writes
                // _localPosition 1*0.6=0.6.
                var childTrack = Object.FindAnyObjectByType<TracksManager>()
                    .GetAnimationTrack(ChildTrackName).Track;
                Assert.That(childTrack.ObjectParentTransform.localPosition.x,
                    Is.EqualTo(6f).Within(0.01f),
                    $"stale map version chose V3 localPosition precedence: " +
                    $"x={childTrack.ObjectParentTransform.localPosition.x}, expected 6 from V2 _position 10*0.6.");
            }
            finally
            {
                songContainer.Map = previousMap;
                Object.FindAnyObjectByType<MapLoader>().UpdateMapData(previousMap);
                Settings.TestRunnerSettings.MapVersion = previousRunnerVersion;
                Settings.Instance.MapVersion = previousMapVersion;
            }
        }

        private static void AssertBoxLocalPosition(Vector3 expected, string stage)
        {
            var box = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.ChromaIDMarkers
                .Single(marker => marker.ChromaID == BoxId)
                .transform;
            Assert.That(Vector3.Distance(box.localPosition, expected), Is.LessThan(0.01f),
                $"{stage} chose position over localPosition: local={box.localPosition}, expected={expected}.");
        }

        // Chroma gives new geometry the same Heck TransformController as an enhanced
        // OEM object, so its tracked position fields must have the same precedence.
        private static void AssertGeometryLocalPosition(Vector3 expected, string stage)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var target = geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .Single(container => container.EnvironmentEnhancement.Geometry != null
                    && container.EnvironmentEnhancement.Track == TrackName)
                .transform;
            Assert.That(Vector3.Distance(target.localPosition, expected), Is.LessThan(0.01f),
                $"{stage} chose position over localPosition: local={target.localPosition}, expected={expected}.");
        }

        private static IEnumerator AssertWorldOnlyGeometry(Vector3 expected, string stage)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var target = geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .Single(container => container.EnvironmentEnhancement.Geometry != null
                    && container.EnvironmentEnhancement.Track == WorldOnlyTrackName)
                .transform;
            var parent = target.parent;
            var initialParentPosition = parent.position;
            try
            {
                parent.position += new Vector3(10f, 20f, 30f);
                yield return null;
                yield return null;

                Assert.That(Vector3.Distance(target.position, expected), Is.LessThan(0.01f),
                    $"{stage} wrote localPosition; world={target.position}, expected={expected}.");
            }
            finally
            {
                parent.position = initialParentPosition;
            }
        }

        private static void AssertChildGeometryWorldX(float expected, string stage)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var target = geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .Single(container => container.EnvironmentEnhancement.Geometry != null
                    && container.EnvironmentEnhancement.Track == ChildTrackName)
                .transform;
            Assert.That(target.position.x, Is.EqualTo(expected).Within(0.01f),
                $"{stage} moved the child geometry to x={target.position.x} instead of {expected}.");
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // Each version uses its native serialized field names while preserving identical
        // transform instructions, so the test exercises the actual V2/V3 map load paths.
        private static JSONNode CreateDifficulty(int version)
        {
            var v2 = version == 2;
            var positionKey = v2 ? "_position" : "position";
            var localPositionKey = v2 ? "_localPosition" : "localPosition";
            var trackKey = v2 ? "_track" : "track";
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                [v2 ? "_id" : "id"] = BoxId,
                [v2 ? "_lookupMethod" : "lookupMethod"] = "Exact",
                [trackKey] = TrackName,
                [positionKey] = JSON.Parse("[100,100,100]"),
                [localPositionKey] = JSON.Parse("[3,4,5]")
            });
            // The primitive geometry path uses CM's container animator; in Heck it
            // shares the OEM object's TransformController position precedence.
            environment.Add(new JSONObject
            {
                [v2 ? "_geometry" : "geometry"] = new JSONObject
                {
                    [v2 ? "_type" : "type"] = "Cube",
                    [v2 ? "_material" : "material"] = "standard"
                },
                [trackKey] = TrackName,
                [positionKey] = JSON.Parse("[100,100,100]"),
                [localPositionKey] = JSON.Parse("[3,4,5]")
            });
            environment.Add(new JSONObject
            {
                [v2 ? "_geometry" : "geometry"] = new JSONObject
                {
                    [v2 ? "_type" : "type"] = "Cube",
                    [v2 ? "_material" : "material"] = "standard"
                },
                [trackKey] = WorldOnlyTrackName,
                [positionKey] = JSON.Parse("[0,0,0]")
            });
            var eventData = new JSONObject
            {
                [trackKey] = TrackName,
                [v2 ? "_duration" : "duration"] = 0f,
                [positionKey] = JSON.Parse("[[90,80,70,0]]"),
                [localPositionKey] = JSON.Parse("[[7,8,9,0]]")
            };
            var events = new JSONArray();
            events.Add(v2
                ? new JSONObject
                {
                    ["_time"] = 4f,
                    ["_type"] = "AnimateTrack",
                    ["_data"] = eventData
                }
                : new JSONObject
                {
                    ["b"] = 4f,
                    ["t"] = "AnimateTrack",
                    ["d"] = eventData
                });
            var worldOnlyData = new JSONObject
            {
                [trackKey] = WorldOnlyTrackName,
                [v2 ? "_duration" : "duration"] = 0f,
                [positionKey] = JSON.Parse("[[90,80,70,0]]")
            };
            events.Add(v2
                ? new JSONObject
                {
                    ["_time"] = 4f,
                    ["_type"] = "AnimateTrack",
                    ["_data"] = worldOnlyData
                }
                : new JSONObject
                {
                    ["b"] = 4f,
                    ["t"] = "AnimateTrack",
                    ["d"] = worldOnlyData
                });

            if (!v2)
            {
                return new JSONObject
                {
                    ["version"] = "3.3.0",
                    ["customData"] = new JSONObject
                    {
                        ["environment"] = environment,
                        ["customEvents"] = events
                    }
                };
            }

            return new JSONObject
            {
                ["_version"] = "2.2.0",
                ["_events"] = new JSONArray(),
                ["_notes"] = new JSONArray(),
                ["_obstacles"] = new JSONArray(),
                ["_waypoints"] = new JSONArray(),
                ["_customData"] = new JSONObject
                {
                    ["_environment"] = environment,
                    ["_customEvents"] = events
                }
            };
        }

        // The child cube has no own motion. Only its parent-track event can change its
        // world X, exposing how CM combines or prioritizes the two position fields.
        private static JSONNode CreateParentDifficulty(int version, bool worldPositionWithRotation = false)
        {
            var v2 = version == 2;
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                [v2 ? "_geometry" : "geometry"] = new JSONObject
                {
                    [v2 ? "_type" : "type"] = "Cube",
                    [v2 ? "_material" : "material"] = "standard"
                },
                [v2 ? "_track" : "track"] = ChildTrackName
            });
            var parentData = new JSONObject
            {
                [v2 ? "_parentTrack" : "parentTrack"] = ParentTrackName,
                [v2 ? "_childrenTracks" : "childrenTracks"] = JSON.Parse($"[\"{ChildTrackName}\"]"),
                [v2 ? "_worldPositionStays" : "worldPositionStays"] = false
            };
            var animationData = new JSONObject
            {
                [v2 ? "_track" : "track"] = ParentTrackName,
                [v2 ? "_duration" : "duration"] = 0f,
                [v2 ? "_position" : "position"] = JSON.Parse("[[10,0,0,0]]"),
            };
            if (worldPositionWithRotation)
                animationData["rotation"] = JSON.Parse("[[0,0,90,0]]");
            else
                animationData[v2 ? "_localPosition" : "localPosition"] = JSON.Parse("[[1,0,0,0]]");
            var events = new JSONArray();
            if (v2)
            {
                events.Add(new JSONObject
                {
                    ["_time"] = 0f,
                    ["_type"] = "AssignTrackParent",
                    ["_data"] = parentData
                });
                events.Add(new JSONObject
                {
                    ["_time"] = 4f,
                    ["_type"] = "AnimateTrack",
                    ["_data"] = animationData
                });
                return new JSONObject
                {
                    ["_version"] = "2.2.0",
                    ["_events"] = new JSONArray(),
                    ["_notes"] = new JSONArray(),
                    ["_obstacles"] = new JSONArray(),
                    ["_waypoints"] = new JSONArray(),
                    ["_customData"] = new JSONObject
                    {
                        ["_environment"] = environment,
                        ["_customEvents"] = events
                    }
                };
            }

            events.Add(new JSONObject
            {
                ["b"] = 0f,
                ["t"] = "AssignTrackParent",
                ["d"] = parentData
            });
            events.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateTrack",
                ["d"] = animationData
            });
            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = events
                }
            };
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
