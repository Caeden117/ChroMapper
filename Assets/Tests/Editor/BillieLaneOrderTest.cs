using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    // BillieLaneOrder* protects the mapper-friendly lane sequence without modifying the authoritative UGEcko export.
    public class BillieLaneOrderTest
    {
        private const string TrackDefinitionsPath =
            "Assets/__Scripts/Environments/TrackDefinitions/BillieEnvironmentTrackDefinitions.asset";
        private const string EnvironmentDataPath =
            "Assets/__Scenes/Environments/Data/BillieEnvironment.json";

        // BillieLaneOrder* verifies the exact event identities that must remain attached to the reordered labels.
        private static readonly int[] ExpectedEventTypes = { 1, 6, 7, 0, 10, 11, 4, 2, 3, 5, 12, 13, 9, 8 };

        // BillieTrackDefinitionAssetUsesCorrectedLaneOrder catches manual generated-asset drift before regeneration.
        [Test]
        public void BillieTrackDefinitionAssetUsesCorrectedLaneOrder()
        {
            var tracksDefinition = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(TrackDefinitionsPath);
            Assert.That(tracksDefinition, Is.Not.Null, "The Billie track definition asset did not load.");
            tracksDefinition.Initialize();

            CollectionAssert.AreEqual(ExpectedEventTypes, tracksDefinition.Basic.Keys);
        }

        // BillieTrackDefinitionImportUsesCorrectedLaneOrder ensures Update Environment List reproduces the checked-in order.
        [Test]
        public void BillieTrackDefinitionImportUsesCorrectedLaneOrder()
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(EnvironmentDataPath);
            Assert.That(dataAsset, Is.Not.Null, "The raw Billie environment export did not load.");
            var environmentData = JSON.Parse(dataAsset.text)["environmentData"];
            var exportedTracks = environmentData["lightTracks"]["eventTracks"].AsArray;
            Assert.That(exportedTracks, Is.Not.Null, "The raw Billie export had no light-track metadata.");
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

                CollectionAssert.AreEqual(ExpectedEventTypes, tracksDefinition.Basic.Keys);
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
    }
}
