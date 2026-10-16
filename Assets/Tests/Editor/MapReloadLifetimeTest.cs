using System.Collections;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class MapReloadLifetimeTest : TestBase
    {
        [UnityTest]
        public IEnumerator ReloadStopsMirrorsBeforeDestroyingTrackParentedPlanes()
        {
            yield return CheckMirrorDestruction(false);
        }

        [UnityTest]
        public IEnumerator DifficultySwitchStopsMirrorsBeforeDestroyingTrackParentedPlanes()
        {
            yield return CheckMirrorDestruction(true);
        }

        [UnityTest]
        public IEnumerator DifficultySwitchKeepsUnchangedEnvironmentMirrorsActive()
        {
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.2.0" },
                environmentName: "FitBeatEnvironment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var descriptor = context.Descriptor;
            var mirrors = descriptor.GetComponentsInChildren<PlanarReflection>();
            Assert.That(mirrors, Is.Not.Empty);

            yield return TestUtils.SwitchToEmptyDifficulty();

            Assert.That(context.Descriptor, Is.SameAs(descriptor));
            foreach (var mirror in mirrors)
                Assert.That(
                    mirror.isActiveAndEnabled,
                    Is.True,
                    "A difficulty switch that reuses the environment must leave its mirrors running.");
        }

        private static IEnumerator CheckMirrorDestruction(bool switchDifficulty)
        {
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(@"{
                ""version"":""3.2.0"",
                ""customData"":{
                    ""customEvents"":[{
                        ""b"":0,""t"":""AssignTrackParent"",
                        ""d"":{""parentTrack"":""mirrorParent"",""childrenTracks"":[""mirrorPlane""]}
                    }],
                    ""environment"":[{
                        ""id"":""FitBeatEnvironment.[0]Environment.[4]TrackMirror"",
                        ""lookupMethod"":""Exact"",""track"":""mirrorPlane""
                    }]
                }
            }"),
                environmentName: "FitBeatEnvironment");

            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var mirror = Object.FindObjectsByType<PlanarReflection>(FindObjectsSortMode.None)
                .Single(
                    reflection => reflection.gameObject.scene == descriptor.gameObject.scene
                        && reflection.PlaneTransform != reflection.transform);
            var plane = mirror.PlaneTransform;
            Assert.That(
                plane.GetComponentInParent<Track>(),
                Is.Not.Null,
                "The mirror plane must be reparented out of the environment by AssignTrackParent.");
            Assert.That(mirror.isActiveAndEnabled, Is.True);

            var observedDestruction = false;
            var mirrorWasActive = false;
            var probe = plane.gameObject.AddComponent<EnvironmentTargetDestructionProbe>();
            probe.OnTargetDestroyed = () =>
            {
                observedDestruction = true;
                mirrorWasActive = mirror != null && mirror.isActiveAndEnabled && mirror.Renderer.enabled;
            };

            if (switchDifficulty)
                yield return TestUtils.SwitchToEmptyDifficulty();
            else
                yield return TestUtils.ReloadMap(
                    3,
                    new JSONObject { ["version"] = "3.2.0" },
                    environmentName: "FitBeatEnvironment");

            Assert.That(observedDestruction, Is.True);
            Assert.That(
                mirrorWasActive,
                Is.False,
                "The outgoing mirror could render after track cleanup destroyed its plane.");
            var incoming = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            Assert.That(
                incoming.GetComponentsInChildren<PlanarReflection>().Any(reflection => reflection.isActiveAndEnabled),
                Is.True,
                "The replacement environment must still have active mirrors.");
        }

        [UnityTest]
        public IEnumerator EnvironmentCleanupReleasesRegisteredGeometryBeforeSceneUnload()
        {
            yield return TestUtils.ReloadMap(3, JSON.Parse(@"{
                ""version"":""3.2.0"",
                ""customData"":{""environment"":[
                    {""geometry"":{""type"":""Cube"",""material"":""standard""}},
                    {""geometry"":{""type"":""Cube"",""material"":""standard""},""track"":""reloadLifetime""}
                ]}
            }"));
            var collection = Object.FindAnyObjectByType<GeometryGridContainer>();
            var containers = collection.LoadedContainers.Values.Cast<GeometryContainer>().ToArray();
            Assert.That(containers.Length, Is.EqualTo(2));
            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack("reloadLifetime");
            Assert.That(track.Children.Count, Is.GreaterThan(0));

            Object.FindAnyObjectByType<MapLoader>().DestroyTrackBoundEnvironmentObjects();

            Assert.That(collection.LoadedContainers, Is.Empty,
                "Environment unload must not leave registered geometry pointing at destroyed scene objects.");
            Assert.That(collection.ObjectsWithContainers, Is.Empty);
            foreach (var container in containers)
            {
                Assert.That(container == null, Is.True, "Cleanup retained an outgoing geometry object.");
            }

            Assert.That(track.Children, Is.Empty, "Destroyed geometry remained subscribed to its animation track.");
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            Assert.That(collection.LoadedContainers, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RepeatedTestMapReloadsReleaseSupersededAudioClips()
        {
            for (var i = 0; i < 4; i++)
            {
                var previousClip = BeatSaberSongContainer.Instance.LoadedSong;
                yield return TestUtils.ReloadMap(3,
                    new JSONObject { ["version"] = "3.2.0" }, songLengthSeconds: 60 + i);
                Assert.That(previousClip == null, Is.True,
                    "A completed test-map reload retained the superseded native audio clip.");
                Assert.That(Object.FindAnyObjectByType<AudioTimeSyncController>().SongAudioSource.clip,
                    Is.SameAs(BeatSaberSongContainer.Instance.LoadedSong));
            }
        }

        [UnityTest]
        public IEnumerator TrackParentedEnvironmentTargetIsDestroyedBeforeGeometryControllers()
        {
            yield return TestUtils.ReloadMap(3, JSON.Parse(@"{
                ""version"":""3.2.0"",
                ""customData"":{ ""customEvents"": [{
                    ""b"": 0,
                    ""t"":""AssignTrackParent"",
                    ""d"":{ ""parentTrack"":""reloadLifetimeParent"", ""childrenTracks"":[""reloadLifetimeChild""] }
                }] }
            }"));
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var loader = Object.FindAnyObjectByType<MapLoader>();
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var target = new GameObject("ReloadLifetimeTarget");
            var marker = target.AddComponent<ChromaIDMarker>();
            marker.ChromaID = "ReloadLifetimeTarget";
            context.Descriptor.ChromaIDMarkers.Add(marker);
            try
            {
                var enhancement = new Beatmap.Base.Customs.BaseEnvironmentEnhancement
                {
                    ID = marker.ChromaID,
                    LookupMethod = Beatmap.Enums.EnvironmentLookupMethod.Exact,
                    Track = "reloadLifetimeChild"
                };
                loader.LoadEnvironmentEnhancements(
                    new System.Collections.Generic.List<Beatmap.Base.Customs.BaseEnvironmentEnhancement> { enhancement });
                var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
                var child = tracks.GetAnimationTrack("reloadLifetimeChild");
                Assert.That(geometry.LoadedContainers.Count, Is.EqualTo(1));
                Assert.That(target.transform.parent, Is.SameAs(child.Track.ObjectParentTransform));
                Assert.That(child.Children.Any(animator => animator.LocalTarget == target.transform), Is.True);
                loader.DestroyTrackBoundEnvironmentObjects();
                Assert.That(target == null, Is.True,
                    "Environment cleanup lost its reparented target when it destroyed the track controller first.");
                Assert.That(geometry.LoadedContainers, Is.Empty);
                Assert.That(child.Children, Is.Empty);
            }
            finally
            {
                if (context != null && context.Descriptor != null)
                {
                    context.Descriptor.ChromaIDMarkers.Remove(marker);
                }

                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            yield return null;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            yield return TestUtils.ReloadMap(3,
                new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
        }
    }

    public class EnvironmentTargetDestructionProbe : MonoBehaviour
    {
        public System.Action OnTargetDestroyed;

        private void OnDestroy() => OnTargetDestroyed?.Invoke();
    }
}
