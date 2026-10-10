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
}
