using System.Reflection;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class EventPlacementEditorStateTest : TestBase
    {
        // LoadingBasicEventPlacementStateDoesNotRefreshPreviewAsFinalContainer reproduces editor-state restoration changing the model on a grid-less placement preview.
        [Test]
        public void LoadingBasicEventPlacementStateDoesNotRefreshPreviewAsFinalContainer()
        {
            var placement = Object.FindAnyObjectByType<EventPlacement>();
            Assert.That(placement, Is.Not.Null);
            var preview = placement.PlacementVisualContainer;
            Assert.That(preview, Is.Not.Null);

            var eventGridContainerField = typeof(EventContainer).GetField(
                "eventGridContainer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(eventGridContainerField, Is.Not.Null);
            Assert.That(
                eventGridContainerField.GetValue(preview),
                Is.Null,
                "A placement preview must not be treated as a finalized event-grid container.");

            var originalType = placement.QueuedData.Type;
            var originalValue = placement.QueuedValue;
            var originalFloatValue = placement.QueuedFloatValue;
            var originalLaserSpeed = placement.LaserSpeedText;
            var originalAlternateShader = preview.AlternateShader;
            var eventModel = preview.VisualSettings.GetEventModel();
            Assert.That(eventModel, Is.Not.Null);

            try
            {
                placement.QueuedData.Type = (int)EventTypeValue.Event0;
                preview.AlternateShader = !eventModel.AlternateShader;
                var savedState = SimpleJSON.JSON.Parse(
                    @"{ ""value"": 1, ""floatValue"": 1, ""laserSpeed"": ""8"" }");

                Assert.DoesNotThrow(() => placement.LoadEditorState(savedState));
            }
            finally
            {
                // Restore through a same-shader light model so this cleanup remains safe when verifying the unfixed regression.
                placement.QueuedData.Type = (int)EventTypeValue.Event0;
                preview.AlternateShader = eventModel.AlternateShader;
                placement.RestoreEditorState(originalValue, originalFloatValue, originalLaserSpeed);
                placement.QueuedData.Type = originalType;
                preview.AlternateShader = originalAlternateShader;
            }
        }
    }
}
