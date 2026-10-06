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

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            yield return TestUtils.ReloadMap(3,
                new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
        }
    }
}
