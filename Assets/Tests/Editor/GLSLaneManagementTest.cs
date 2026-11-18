using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSGroupSupportedAxisPlacementTest : GLSExtensionTestBase
    {
        [TestCase("Left")]
        [TestCase("Right")]
        public void PlacingDaftPunkTrisWallGroupCreatesOnlyZLane(string side)
        {
            var daft = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(
                "Assets/__Scripts/Environments/TrackDefinitions/DaftPunkEnvironmentTrackDefinitions.asset");
            var triangles = daft.Gls.Values.Single(definition =>
                definition.Group == "Tris Wall" && definition.Name == side);
            var track = groupProvider.IdToTracks[0];
            track.TrackDefinition.TranslationTracks = triangles.TranslationTracks;

            var group = PlaceGroup(true, Axis.Z);
            var innerProvider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            innerProvider.GroupContext = group;

            Assert.That(innerProvider.DisplayedLaneCount, Is.EqualTo(1));
            Assert.That(innerProvider.TryGetDisplayedBox(0, out var box), Is.True);
            Assert.That(box.GetAxis(), Is.EqualTo(Axis.Z));
        }

        [Test]
        public void PlacingTransformGroupUsesFirstSupportedAxis(
            [Values(false, true)] bool translation,
            [Values(1, 2, 3, 4, 5, 6, 7)] int supportedAxes)
        {
            var track = groupProvider.IdToTracks[0];
            var enabled = new[]
            {
                (supportedAxes & 1) != 0,
                (supportedAxes & 2) != 0,
                (supportedAxes & 4) != 0
            };
            track.TrackDefinition.RotationTracks = enabled;
            track.TrackDefinition.TranslationTracks = enabled;

            PlaceGroup(translation, (Axis)System.Array.FindIndex(enabled, axis => axis));
        }

        private BaseEventBoxGroup PlaceGroup(bool translation, Axis expectedAxis)
        {
            var placementProvider = groupProvider.IdToTracks[0].GetComponent<PlacementProvider>();
            BasePlacement placement = translation
                ? placementProvider.Placements.OfType<GLSGroupTranslationPlacement>().Single()
                : placementProvider.Placements.OfType<GLSGroupRotationPlacement>().Single();
            placement.Initialize(placementProvider);
            var queued = Field<BaseEventBoxGroup>(placement, "QueuedData");
            queued.JsonTime = 8f;
            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);

            Assert.That(queued.ReadOnlyBoxes.Count, Is.EqualTo(1));
            var box = queued.ReadOnlyBoxes[0];
            Assert.That(box.GetAxis(), Is.EqualTo(expectedAxis),
                "Outer group placement must create its first event box on a supported axis.");
            Assert.That(box.ReadOnlyEvents.Count, Is.EqualTo(1));
            Assert.That(box.IsAutomaticAxisLane, Is.False);
            Assert.That(Field<BaseEventBoxGroup>(placement, "QueuedData").ReadOnlyBoxes[0].GetAxis(),
                Is.EqualTo(expectedAxis), "The next queued group must retain the supported axis.");
            return queued;
        }
    }

    public class GLSLaneManagementTest : TestBase
    {
        [TestCase(false, false, 0)]
        [TestCase(false, false, 1)]
        [TestCase(false, false, 2)]
        [TestCase(false, true, 0)]
        [TestCase(false, true, 1)]
        [TestCase(false, true, 2)]
        [TestCase(true, false, 0)]
        [TestCase(true, false, 1)]
        [TestCase(true, false, 2)]
        [TestCase(true, true, 0)]
        [TestCase(true, true, 1)]
        [TestCase(true, true, 2)]
        public void PlacingEventPreservesDuplicatesOfMaterializedAxisLanes(
            bool translation,
            bool editSections,
            int placementAxis)
        {
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup { JsonTime = 3100, ID = 3100 }
                : new BaseLightRotationEventBoxGroup { JsonTime = 3100, ID = 3100 };
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);

            for (var axis = 0; axis < 3; axis++)
            {
                Assert.True(provider.TryGetDisplayedBox(axis, out var displayBox));
                eventCollection.PlaceInDisplayOnlyLane(CreateTransformEvent(translation, 3100, 0), displayBox);
            }

            for (var axis = 0; axis < 3; axis++)
            {
                Assert.False(
                    provider.GroupContext.ReadOnlyBoxes[axis].IsAutomaticAxisLane,
                    "Placing the first event must make its axis lane permanent.");
                if (editSections)
                    GLSEventBoxCommand.SetParam0(2, provider.GroupContext, axis);

            }

            GLSEventBoxCommand.DeletePruneEventBox(provider.GroupContext);
            for (var axis = 0; axis < 3; axis++)
                GLSEventBoxCommand.DuplicateEventBox(provider.GroupContext, axis * 2);

            Assert.AreEqual(6, provider.GroupContext.ReadOnlyBoxes.Count);
            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.Undo();
            Assert.AreEqual(5, provider.GroupContext.ReadOnlyBoxes.Count);
            actions.Redo();
            Assert.AreEqual(6, provider.GroupContext.ReadOnlyBoxes.Count);

            group = provider.GroupContext;
            var boxIndex = placementAxis * 2;
            var newEvent = CreateTransformEvent(translation, group.JsonTime, 1);
            newEvent.EventBoxGroupData = group;
            newEvent.EventBoxData = group.ReadOnlyBoxes[boxIndex];
            newEvent.BoxIndex = boxIndex;
            eventCollection.SpawnObject(newEvent, false, false, true);

            AssertDuplicatedAxisLanes(provider.GroupContext, editSections ? 2 : 1, placementAxis);
            actions.Undo();
            AssertDuplicatedAxisLanes(provider.GroupContext, editSections ? 2 : 1, -1);
            actions.Redo();
            AssertDuplicatedAxisLanes(provider.GroupContext, editSections ? 2 : 1, placementAxis);
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void DuplicatingEmptyAutomaticAxisLaneCreatesPermanentBox(bool translation, int axis)
        {
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup()
                : new BaseLightRotationEventBoxGroup();
            var transformGroup = (ILightTransformEventBoxGroup)group;
            var source = transformGroup.CreateTransformBox(axis);
            source.IsAutomaticAxisLane = true;
            source.IndexFilter.Param0 = 2;
            transformGroup.TryAddTransformBox(source);

            transformGroup.DuplicateTransformBox(0);

            var duplicate = group.ReadOnlyBoxes[1];
            Assert.False(duplicate.IsAutomaticAxisLane, "An explicit duplicate must always be a permanent lane.");
            Assert.True(source.IsAutomaticAxisLane, "Duplication must preserve the source lane's lifetime.");
            Assert.AreEqual(axis, (int)duplicate.GetAxis());
            Assert.AreEqual(2, duplicate.IndexFilter.Param0);
            Assert.AreNotSame(source.IndexFilter, duplicate.IndexFilter);
            Assert.AreEqual(0, duplicate.ReadOnlyEvents.Count);
            group.PruneEmptyAutomaticAxisLanes();
            Assert.AreEqual(1, group.ReadOnlyBoxes.Count);
            Assert.AreSame(duplicate, group.ReadOnlyBoxes[0]);
        }

        private static BaseGLSEvent CreateTransformEvent(bool translation, float groupBeat, float offset) =>
            translation
                ? new BaseLightTranslationBase { JsonTime = groupBeat + offset, RelativeJsonTime = offset }
                : new BaseLightRotationBase { JsonTime = groupBeat + offset, RelativeJsonTime = offset };

        private static void AssertDuplicatedAxisLanes(BaseEventBoxGroup group, int sections, int placementAxis)
        {
            Assert.AreEqual(6, group.ReadOnlyBoxes.Count, "Placing a node must retain all three manually duplicated lanes.");
            for (var axis = 0; axis < 3; axis++)
            {
                var source = group.ReadOnlyBoxes[axis * 2];
                var duplicate = group.ReadOnlyBoxes[(axis * 2) + 1];
                Assert.AreEqual(axis, (int)source.GetAxis());
                Assert.AreEqual(axis, (int)duplicate.GetAxis());
                Assert.False(source.IsAutomaticAxisLane);
                Assert.False(duplicate.IsAutomaticAxisLane);
                Assert.AreEqual(sections, source.IndexFilter.Param0);
                Assert.AreEqual(sections, duplicate.IndexFilter.Param0);
                Assert.AreEqual(0, duplicate.ReadOnlyEvents.Count);
                Assert.AreEqual(axis == placementAxis ? 2 : 1, source.ReadOnlyEvents.Count);
                foreach (var evt in source.ReadOnlyEvents)
                {
                    Assert.AreSame(group, evt.EventBoxGroupData);
                    Assert.AreSame(source, evt.EventBoxData);
                    Assert.AreEqual(axis * 2, evt.BoxIndex);
                    Assert.AreEqual(group.JsonTime + evt.RelativeJsonTime, evt.JsonTime);
                }
            }

            var serialized = group is BaseLightTranslationEventBoxGroup translationGroup
                ? V3LightTranslationEventBoxGroup.ToJson(translationGroup)
                : V3LightRotationEventBoxGroup.ToJson((BaseLightRotationEventBoxGroup)group);
            Assert.AreEqual(6, serialized["e"].AsArray.Count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InitialTransformGroupCreatesPermanentLane(bool translation)
        {
            var placementType = translation
                ? typeof(GLSGroupTranslationPlacement)
                : typeof(GLSGroupRotationPlacement);
            var placement = Object.FindAnyObjectByType(placementType);
            var generateMethod = placementType.GetMethod(
                "GenerateOriginalData",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var group = (BaseEventBoxGroup)generateMethod.Invoke(placement, null);

            Assert.AreEqual(1, group.ReadOnlyBoxes.Count);
            Assert.AreEqual(1, group.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            Assert.False(
                group.ReadOnlyBoxes[0].IsAutomaticAxisLane,
                "A generated lane already containing its first event must be permanent.");
        }

        private const string AddLaneTooltip =
            "Add a lane following the currently selected event box lane below. Lights are owned by the first lane whose filter matches them.";

        // A new lane should keep the selected transform lane's Y or Z axis rather than jump to X.
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void AddLaneAfterSelectedTransformLanePreservesAxis(bool translation, int selectedAxis)
        {
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightTranslationEventBox { Axis = 0, BeatDistribution = 10 },
                        new BaseLightTranslationEventBox { Axis = selectedAxis, BeatDistribution = 20 },
                        new BaseLightTranslationEventBox { Axis = selectedAxis, BeatDistribution = 30 }
                    }
                }
                : new BaseLightRotationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightRotationEventBox { Axis = 0, BeatDistribution = 10 },
                        new BaseLightRotationEventBox { Axis = selectedAxis, BeatDistribution = 20 },
                        new BaseLightRotationEventBox { Axis = selectedAxis, BeatDistribution = 30 }
                    }
                };
            group.JsonTime = 3000 + selectedAxis + (translation ? 10 : 0);
            group.songBpmTime = group.JsonTime;
            if (group is BaseLightTranslationEventBoxGroup translationGroup)
            {
                translationGroup.NormalizeLoadedEventConflicts();
            }
            else if (group is BaseLightRotationEventBoxGroup rotationGroup)
            {
                rotationGroup.NormalizeLoadedEventConflicts();
            }
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;

            var editedGroup = GLSEventBoxCommand.AddEventBox(group, 2);

            Assert.AreEqual(4, editedGroup.ReadOnlyBoxes.Count);
            Assert.AreEqual(10, editedGroup.ReadOnlyBoxes[0].BeatDistribution);
            Assert.AreEqual(20, editedGroup.ReadOnlyBoxes[1].BeatDistribution);
            Assert.AreEqual(selectedAxis, (int)editedGroup.ReadOnlyBoxes[2].GetAxis());
            Assert.AreEqual(0, editedGroup.ReadOnlyBoxes[2].ReadOnlyEvents.Count);
            Assert.AreEqual(30, editedGroup.ReadOnlyBoxes[3].BeatDistribution);

            Object.FindAnyObjectByType<BeatmapActionContainer>().Undo();

            Assert.AreEqual(3, provider.GroupContext.ReadOnlyBoxes.Count);
            Assert.AreEqual(selectedAxis, (int)provider.GroupContext.ReadOnlyBoxes[1].GetAxis());
            Assert.AreEqual(30, provider.GroupContext.ReadOnlyBoxes[2].BeatDistribution);
        }

        [TestCase("Color")]
        [TestCase("Rotation")]
        [TestCase("Translation")]
        [TestCase("FloatFX")]
        public void LaterOverlappingLaneLabelIsBrightRed(string laneType)
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var effectHost = RegisterFourLightEffect(context, laneType, out var groupId);
            try
            {
                provider.GroupContext = CreateOverlappingGroup(laneType, groupId);

                var labels = GetDisplayedLabels(provider);

                Assert.GreaterOrEqual(labels.Length, laneType is "Rotation" or "Translation" ? 3 : 2);
                Assert.AreNotEqual(Color.red, labels[0].color);
                if (laneType is "Rotation" or "Translation")
                {
                    Assert.AreNotEqual(
                        Color.red,
                        labels[1].color,
                        "An earlier X lane must not consume the same light IDs on Y.");
                    Assert.AreEqual(Color.red, labels[2].color);
                }
                else
                {
                    Assert.AreEqual(Color.red, labels[1].color);
                }
            }
            finally
            {
                UnregisterEffect(context, laneType, groupId);
                Object.DestroyImmediate(effectHost);
            }
        }

        // AddLaneButtonExplainsOrderingAndOwnership keeps the insertion position and first-match rule discoverable.
        [Test]
        public void AddLaneButtonExplainsOrderingAndOwnership()
        {
            var controller = Object.FindAnyObjectByType<EventBoxViewController>();
            var buttonField = typeof(EventBoxViewController).GetField(
                "addEventBoxButton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var button = buttonField.GetValue(controller) as ButtonComponent;
            var tooltips = button.GetComponents<Tooltip>();

            Assert.That(
                tooltips.Any(tooltip => tooltip.TooltipOverride == AddLaneTooltip),
                Is.True,
                "The add-lane button needs the requested ownership explanation.");
            Assert.That(
                tooltips.Any(tooltip => tooltip.AdvancedTooltip == AddLaneTooltip),
                Is.True,
                "The advanced tooltip must preserve the same explanation.");
        }

        private static BaseEventBoxGroup CreateOverlappingGroup(string laneType, int groupId) => laneType switch
        {
            "Color" => new BaseLightColorEventBoxGroup
            {
                ID = groupId,
                Boxes = { new BaseLightColorEventBox(), new BaseLightColorEventBox() }
            },
            "Rotation" => new BaseLightRotationEventBoxGroup
            {
                ID = groupId,
                Boxes =
                {
                    new BaseLightRotationEventBox { Axis = 0 },
                    new BaseLightRotationEventBox { Axis = 1 },
                    new BaseLightRotationEventBox { Axis = 1 }
                }
            },
            "Translation" => new BaseLightTranslationEventBoxGroup
            {
                ID = groupId,
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 0 },
                    new BaseLightTranslationEventBox { Axis = 1 },
                    new BaseLightTranslationEventBox { Axis = 1 }
                }
            },
            "FloatFX" => new BaseVfxEventEventBoxGroup
            {
                ID = groupId,
                Boxes = { new BaseVfxEventEventBox(), new BaseVfxEventEventBox() }
            },
            _ => throw new AssertionException($"Unknown GLS lane type {laneType}.")
        };

        // RegisterFourLightEffect gives the label renderer a deterministic physical group without relying on the test map's environment.
        private static GameObject RegisterFourLightEffect(
            BeatmapRuntimeContext context,
            string laneType,
            out int groupId)
        {
            groupId = 987654;
            var host = new GameObject($"{laneType} lane ownership effect");
            switch (laneType)
            {
                case "Color":
                    var boost = host.AddComponent<ColorBoostEffect>();
                    var color = host.AddComponent<LightColorGroupEffect>();
                    color.Count = 4;
                    color.ColorBoostEffect = boost;
                    context.Descriptor.LightColorGroupEffectManager.IdToEffect.Add(groupId, color);
                    break;
                case "Rotation":
                    var rotation = host.AddComponent<LightRotationGroupEffect>();
                    rotation.Count = 4;
                    context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Add(groupId, rotation);
                    break;
                case "Translation":
                    var translation = host.AddComponent<LightTranslationGroupEffect>();
                    translation.Count = 4;
                    context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Add(groupId, translation);
                    break;
                case "FloatFX":
                    var floatFx = host.AddComponent<FloatFxGroupEffect>();
                    floatFx.Count = 4;
                    context.Descriptor.FloatFxGroupEffectManager.IdToEffect.Add(groupId, floatFx);
                    break;
                default:
                    throw new AssertionException($"Unknown GLS lane type {laneType}.");
            }

            return host;
        }

        // Unregister the synthetic light count before destroying its effect.
        private static void UnregisterEffect(BeatmapRuntimeContext context, string laneType, int groupId)
        {
            switch (laneType)
            {
                case "Color":
                    context.Descriptor.LightColorGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
                case "Rotation":
                    context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
                case "Translation":
                    context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
                case "FloatFX":
                    context.Descriptor.FloatFxGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
            }
        }

        // GetDisplayedLabels reads only this provider's active pool and restores visual order from lane X positions.
        private static TextMeshProUGUI[] GetDisplayedLabels(GLSEventGridProvider provider)
        {
            var labelsField = typeof(GLSEventGridProvider).GetField(
                "usedLabels",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (labelsField.GetValue(provider) as IEnumerable<TextMeshProUGUI>)
                .Where(label => label.enabled)
                .OrderBy(label => label.rectTransform.localPosition.x)
                .ToArray();
        }
    }
}
