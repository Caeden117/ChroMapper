using System.Collections.Generic;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;

namespace Tests.Editor
{
    public class GLSOuterPreviewWindowTest : TestBase
    {
        [Test]
        public void RefreshPoolMaterializesOnlyPreviewNodesInsideWindow()
        {
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSGroupGridProvider>();
            var runtime = UnityEngine.Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var originalTracks = runtime.TrackDefinitions;
            var testTracks = UnityEngine.ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Register(new TrackDefinitionGLS
            {
                ID = 1,
                Group = "GLS outer preview window",
                Name = "GLS outer preview window",
                ColorTrack = true
            });

            try
            {
                // Page-aware pooling requires the fixture to install and select a real owner for its synthetic group.
                runtime.TrackDefinitions = testTracks;
                runtime.NotifyTrackDefinitions();
                provider.SetGroupPage("GLS outer preview window");
                var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                    @"{ ""b"": 20, ""g"": 1, ""e"": [
                        { ""f"": { ""f"": 1, ""p"": 1, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 },
                          ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                          ""e"": [
                            { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                            { ""b"": 100, ""c"": 1, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                            { ""b"": 200, ""c"": 0, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 }
                          ] }
                    ] }"));
                group.SetMap(BeatSaberSongContainer.Instance.Map);
                group.RecomputeSongBpmTime();
                var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
                collection.SpawnObject(group, false, false, true);
                // A narrow viewport around the parent must not retain far-future preview GameObjects.
                collection.RefreshPool(19f, 30f);

                Assert.IsTrue(collection.LoadedContainers.TryGetValue(group, out var loaded));
                var owner = loaded as GLSGroupContainer;
                Assert.IsNotNull(owner);
                Assert.AreEqual(0, GetPreviewGhosts(owner).Count,
                    "The two inner nodes outside the pool window must remain data-only until their beats approach the viewport.");
            }
            finally
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
                UnityEngine.Object.DestroyImmediate(testTracks);
            }
        }

        // Read the maintained preview list directly so the assertion does not scan unrelated scene objects.
        private static List<GLSGroupContainer> GetPreviewGhosts(GLSGroupContainer owner)
        {
            var field = typeof(GLSGroupContainer).GetField(
                "previewGhosts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (List<GLSGroupContainer>)field.GetValue(owner);
        }
    }
}
