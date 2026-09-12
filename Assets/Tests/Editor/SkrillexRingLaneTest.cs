using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // SkrillexRingLane* covers the two basic-event lanes that share rotation and zoom consumers in the OEM environment.
    public class SkrillexRingLaneTest : TestBase
    {
        private const string EnvironmentSceneName = "SkrillexEnvironment";
        private const string TrackDefinitionsPath =
            "Assets/__Scripts/Environments/TrackDefinitions/SkrillexEnvironmentTrackDefinitions.asset";
        private const string EnvironmentDataPath =
            "Assets/__Scenes/Environments/Data/SkrillexEnvironment.json";

        private Scene environmentScene;

        // SkrillexRingLane* behavior tests initialize the production scene effects against the shared editor runtime.
        [UnitySetUp]
        public IEnumerator LoadSkrillexEnvironmentOnly()
        {
            yield return SceneManager.LoadSceneAsync(EnvironmentSceneName, LoadSceneMode.Additive);
            environmentScene = SceneManager.GetSceneByName(EnvironmentSceneName);
            var descriptor = environmentScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnvironmentDescriptor>(true))
                .Single();

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            descriptor.Initialize(context);
        }

        // Unloading only the additive Skrillex scene preserves TestBase's shared map and callback baseline.
        [UnityTearDown]
        public IEnumerator UnloadSkrillexEnvironmentOnly()
        {
            if (environmentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(environmentScene);
            }
        }

        // Both mixed Skrillex lanes must identify their ring set and advertise that the event controls rotation and zoom.
        [TestCase(8, "Ring 2 Rotation / Zoom")]
        [TestCase(9, "Ring 1 Rotation / Zoom")]
        public void SkrillexMixedRingLaneUsesDescriptiveTrackName(int eventType, string expectedName)
        {
            var tracksDefinition = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(TrackDefinitionsPath);
            Assert.That(tracksDefinition, Is.Not.Null, "The Skrillex track definition asset did not load.");
            tracksDefinition.Initialize();

            Assert.That(tracksDefinition.Basic[eventType].Name, Is.EqualTo(expectedName));
        }

        // SkrillexPanelSpeedLanesUseDescriptiveTrackName removes the misleading laser reference from panel-motion lanes.
        [TestCase(12, "Left Panel Speed")]
        [TestCase(13, "Right Panel Speed")]
        public void SkrillexPanelSpeedLanesUseDescriptiveTrackName(int eventType, string expectedName)
        {
            var tracksDefinition = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(TrackDefinitionsPath);
            Assert.That(tracksDefinition, Is.Not.Null, "The Skrillex track definition asset did not load.");
            tracksDefinition.Initialize();

            Assert.That(tracksDefinition.Basic[eventType].Name, Is.EqualTo(expectedName));
        }

        // Regenerating track definitions from the untouched UGEcko dump must reproduce ChroMapper's corrected labels.
        [Test]
        public void SkrillexTrackDefinitionImportRewritesMixedRingLaneNames()
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(EnvironmentDataPath);
            Assert.That(dataAsset, Is.Not.Null, "The raw Skrillex environment export did not load.");
            var environmentData = JSON.Parse(dataAsset.text)["environmentData"];
            var exportedTracks = environmentData["lightTracks"]["eventTracks"].AsArray;
            Assert.That(exportedTracks, Is.Not.Null, "The raw Skrillex export had no light-track metadata.");
            var lightTracks = new LightTrackDefinitions
            {
                BasicLightTracks = exportedTracks.Linq
                    .Select(track =>
                        new LightTrackDefinitions.BasicTrackDefinition
                        {
                            TrackName = track.Value["trackName"],
                            EventType = int.Parse(track.Value["eventType"].Value.Substring("Event".Length)),
                            ToolbarType = ParseToolbarType(track.Value["toolbarType"]),
                            Page = track.Value["page"]
                        })
                    .ToList(),
                GroupPages = new Dictionary<string, List<LightTrackDefinitions.PageDefinition>>()
            };
            var tracksDefinition = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            try
            {
                lightTracks.CopyTo(
                    tracksDefinition,
                    System.Array.Empty<EnvironmentDataObject>(),
                    environmentData["environmentId"]);

                Assert.That(tracksDefinition.Basic[8].Name, Is.EqualTo("Ring 2 Rotation / Zoom"));
                Assert.That(tracksDefinition.Basic[9].Name, Is.EqualTo("Ring 1 Rotation / Zoom"));
                // SkrillexTrackDefinitionImportRewritesMixedRingLaneNames also preserves corrected panel-speed aliases.
                Assert.That(tracksDefinition.Basic[12].Name, Is.EqualTo("Left Panel Speed"));
                Assert.That(tracksDefinition.Basic[13].Name, Is.EqualTo("Right Panel Speed"));
                // SkrillexBasicEventLanesUseEnvironmentPresentationOrder must survive track-definition regeneration.
                CollectionAssert.AreEqual(
                    new[] { 0, 2, 3, 6, 7, 1, 4, 5, 9, 8, 12, 13 },
                    tracksDefinition.Basic.Keys);
            }
            finally
            {
                Object.DestroyImmediate(tracksDefinition);
            }
        }

        private static BasicEventKind ParseToolbarType(string value) => value switch
        {
            "None" => BasicEventKind.None,
            "Lights" => BasicEventKind.Lights,
            "Toggle" => BasicEventKind.Toggle,
            "FloatValue" => BasicEventKind.FloatValue,
            "IntValue" => BasicEventKind.IntValue,
            "BtsCharacterSelection" => BasicEventKind.BtsCharacter,
            "CarSelection" => BasicEventKind.Car,
            _ => BasicEventKind.Generic
        };

        // SkrillexBasicEventLanesUseEnvironmentPresentationOrder protects the visual order without changing event identities.
        [Test]
        public void SkrillexBasicEventLanesUseEnvironmentPresentationOrder()
        {
            var tracksDefinition = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(TrackDefinitionsPath);
            Assert.That(tracksDefinition, Is.Not.Null, "The Skrillex track definition asset did not load.");
            tracksDefinition.Initialize();

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var descriptor = environmentScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnvironmentDescriptor>(true))
                .Single();
            var previousDescriptor = context.Descriptor;
            var previousTrackDefinitions = context.TrackDefinitions;
            try
            {
                context.Descriptor = descriptor;
                context.TrackDefinitions = tracksDefinition;
                labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

                var expectedEventTypes = new[] { 0, 2, 3, 6, 7, 1, 4, 5, 9, 8, 12, 13 };
                Assert.That(labels.LaneCount, Is.EqualTo(expectedEventTypes.Length));
                for (var lane = 0; lane < expectedEventTypes.Length; lane++)
                {
                    Assert.That(labels.LaneIdToEventType(lane), Is.EqualTo(expectedEventTypes[lane]), $"Lane {lane}");
                }
            }
            finally
            {
                context.Descriptor = previousDescriptor;
                context.TrackDefinitions = previousTrackDefinitions;
            }
        }

    }
}
