using System;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSTransformAutomaticLanePlaybackTest : TestBase
    {
        private const int DiagnosticGroupId = 1000001;
        private const float RotationValue = 90f;
        private const float TranslationValue = 0.5f;

        public enum TransformNodeKind
        {
            Rotation,
            Translation
        }

        private GameObject effectRoot;
        private readonly Transform[] rotationTargets = new Transform[3];
        private readonly Transform[] translationTargets = new Transform[3];
        private LightRotationGroupEffect rotationEffect;
        private LightTranslationGroupEffect translationEffect;
        private LightRotationGroupEffectManager sceneRotationManager;
        private LightTranslationGroupEffectManager sceneTranslationManager;
        private GLSEventGridProvider provider;
        private GLSEventGridContainer eventCollection;

        // Every automatic-lane operation is observed through both transform effect implementations, not only map data.
        [SetUp]
        public void CreateTransformPreviews()
        {
            effectRoot = new GameObject(nameof(GLSTransformAutomaticLanePlaybackTest));
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc, Is.Not.Null);

            rotationEffect = effectRoot.AddComponent<LightRotationGroupEffect>();
            rotationEffect.Atsc = atsc;
            rotationEffect.Count = 1;

            translationEffect = effectRoot.AddComponent<LightTranslationGroupEffect>();
            translationEffect.Atsc = atsc;
            translationEffect.Count = 1;
            translationEffect.TranslationLimits = new[]
            {
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f)
            };
            translationEffect.DistributionLimits = new[]
            {
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            };

            // Distinct targets make an axis move observable even when the source lane had already rendered a value.
            for (var axis = 0; axis < 3; axis++)
            {
                rotationTargets[axis] = CreateTarget($"Rotation {((Axis)axis)}");
                translationTargets[axis] = CreateTarget($"Translation {((Axis)axis)}");
                rotationEffect.Register(0, (Axis)axis, false, rotationTargets[axis]);
                translationEffect.Register(0, (Axis)axis, false, translationTargets[axis]);
            }

            rotationEffect.Initialize();
            translationEffect.Initialize();

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Descriptor, Is.Not.Null);
            sceneRotationManager = context.Descriptor.LightRotationGroupEffectManager;
            sceneTranslationManager = context.Descriptor.LightTranslationGroupEffectManager;
            Assert.That(sceneRotationManager.IdToEffect.ContainsKey(DiagnosticGroupId), Is.False);
            Assert.That(sceneTranslationManager.IdToEffect.ContainsKey(DiagnosticGroupId), Is.False);
            sceneRotationManager.IdToEffect.Add(DiagnosticGroupId, rotationEffect);
            sceneTranslationManager.IdToEffect.Add(DiagnosticGroupId, translationEffect);

            provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            Assert.That(provider, Is.Not.Null);
            Assert.That(eventCollection, Is.Not.Null);
            SetAllGlsEventPlacementsIdle();
        }

        [TestCase(TransformNodeKind.Rotation)]
        [TestCase(TransformNodeKind.Translation)]
        public void InitialTransformGroupPlacementUpdatesPopulatedAutomaticLane(TransformNodeKind kind)
        {
            var group = CreateGroup(kind, Axis.X, true);

            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    group,
                    Array.Empty<BaseObject>(),
                    "Place an initial automatic-axis transform group."),
                true);

            AssertRendered(kind, Axis.X);
        }

        [TestCase(TransformNodeKind.Rotation)]
        [TestCase(TransformNodeKind.Translation)]
        public void PlacingNodeIntoAutomaticLaneUpdatesPreview(TransformNodeKind kind)
        {
            var group = CreateGroup(kind, Axis.X, false);
            PrepareExistingGroup(group);
            var emptyYAxis = GetDisplayedAutomaticLane(Axis.Y);

            eventCollection.PlaceInDisplayOnlyLane(CreateEvent(kind), emptyYAxis);

            AssertRendered(kind, Axis.Y);
        }

        [TestCase(TransformNodeKind.Rotation)]
        [TestCase(TransformNodeKind.Translation)]
        public void PastingNodeIntoAutomaticLaneUpdatesPreview(TransformNodeKind kind)
        {
            var group = CreateGroup(kind, Axis.X, false);
            PrepareExistingGroup(group);
            var sourceEvent = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            var emptyYAxis = GetDisplayedAutomaticLane(Axis.Y);
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            Assert.That(selectionController, Is.Not.Null);
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
            ConfigurePasteLane(selectionController, kind, group, emptyYAxis);
            SelectionController.Select(sourceEvent);
            selectionController.Copy();

            selectionController.Paste();

            AssertRendered(kind, Axis.Y);
        }

        [TestCase(TransformNodeKind.Rotation)]
        [TestCase(TransformNodeKind.Translation)]
        public void AltDraggingNodeIntoAutomaticLaneUpdatesPreview(TransformNodeKind kind)
        {
            var group = CreateGroup(kind, Axis.X, true);
            PrepareExistingGroup(group);
            var emptyYAxis = GetDisplayedAutomaticLane(Axis.Y);
            var movedEvent = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            var originalGroup = BeatmapFactory.Clone(group);
            eventCollection.SilentRemoveObject(movedEvent);
            movedEvent.EventBoxData = emptyYAxis;
            movedEvent.BoxIndex = -1;

            eventCollection.MoveToDisplayOnlyLane(
                movedEvent,
                originalGroup.ReadOnlyBoxes[0].ReadOnlyEvents[0],
                emptyYAxis,
                originalGroup);

            AssertRendered(kind, Axis.Y);
        }

        [TestCase(TransformNodeKind.Rotation)]
        [TestCase(TransformNodeKind.Translation)]
        public void ShiftingNodeIntoAutomaticLaneUpdatesPreview(TransformNodeKind kind)
        {
            var group = CreateGroup(kind, Axis.X, true);
            PrepareExistingGroup(group);
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            Assert.That(selectionController, Is.Not.Null);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);

            selectionController.ShiftSelection(1, 0);

            AssertRendered(kind, Axis.Y);
        }

        [TestCase(TransformNodeKind.Rotation)]
        [TestCase(TransformNodeKind.Translation)]
        public void AxisCyclingNodeIntoAutomaticLaneUpdatesPreview(TransformNodeKind kind)
        {
            var group = CreateGroup(kind, Axis.X, true);
            PrepareExistingGroup(group);
            var cycleMethod = typeof(GLSCommonCommand).GetMethod(
                "CycleTransformEventAxis",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(cycleMethod, Is.Not.Null);

            cycleMethod.Invoke(null, new object[] { group.ReadOnlyBoxes[0].ReadOnlyEvents[0], 1 });

            AssertRendered(kind, Axis.Y);
        }

        // This fixture removes only its diagnostic map groups, scene registrations, clipboard, and synthetic targets.
        protected override void BeforeCleanup()
        {
            SetAllGlsEventPlacementsIdle();
            SelectionController.CopiedObjects.Clear();
            provider.LastContext = null;
            provider.GroupContext = null;
            RemoveDiagnosticGroups();
            sceneRotationManager.IdToEffect.Remove(DiagnosticGroupId);
            sceneTranslationManager.IdToEffect.Remove(DiagnosticGroupId);
            Object.DestroyImmediate(effectRoot);
        }

        // Synthetic targets remain children of the fixture root so teardown cannot leak Unity objects across cases.
        private Transform CreateTarget(string targetName)
        {
            var target = new GameObject(targetName);
            target.transform.SetParent(effectRoot.transform);
            return target.transform;
        }

        // Register the existing group in both the map and live effect before replacing it through the editor.
        private void PrepareExistingGroup(BaseEventBoxGroup group)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, false, false, true);
            provider.LastContext = null;
            provider.GroupContext = group;
            switch (group)
            {
                case BaseLightRotationEventBoxGroup rotationGroup:
                    rotationEffect.InsertData(rotationGroup);
                    rotationEffect.Refresh();
                    break;
                case BaseLightTranslationEventBoxGroup translationGroup:
                    translationEffect.InsertData(translationGroup);
                    translationEffect.Refresh();
                    break;
            }
        }

        // Confirm the lane is still display-only before editing it.
        private BaseEventBox GetDisplayedAutomaticLane(Axis axis)
        {
            Assert.That(provider.TryGetDisplayedBox((int)axis, out var box), Is.True);
            Assert.That(box.IsAutomaticAxisLane, Is.True);
            Assert.That(box.ReadOnlyEvents.Count, Is.Zero);
            return box;
        }

        // Paste must target the exact placement instance serialized into SelectionController, matching the real hover workflow.
        private static void ConfigurePasteLane(
            SelectionController selectionController,
            TransformNodeKind kind,
            BaseEventBoxGroup group,
            BaseEventBox lane)
        {
            SetAllGlsEventPlacementsIdle();
            var fieldName = kind == TransformNodeKind.Rotation
                ? "glsEventRotationPlacement"
                : "glsEventTranslationPlacement";
            var placementField = typeof(SelectionController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(placementField, Is.Not.Null);
            var placement = placementField.GetValue(selectionController) as BasePlacement;
            Assert.That(placement, Is.Not.Null);

            switch (placement)
            {
                case GLSEventRotationPlacement rotationPlacement:
                    ConfigureQueuedEvent(rotationPlacement.QueuedData, group, lane);
                    rotationPlacement.State = PlacementState.Active;
                    break;
                case GLSEventTranslationPlacement translationPlacement:
                    ConfigureQueuedEvent(translationPlacement.QueuedData, group, lane);
                    translationPlacement.State = PlacementState.Active;
                    break;
            }
        }

        // A ghost lane has no authored box index, which is the sentinel consumed by inner-node paste materialization.
        private static void ConfigureQueuedEvent(
            BaseGLSEvent queuedEvent,
            BaseEventBoxGroup group,
            BaseEventBox lane)
        {
            Assert.That(queuedEvent, Is.Not.Null);
            queuedEvent.EventBoxGroupData = group;
            queuedEvent.EventBoxData = lane;
            queuedEvent.BoxIndex = -1;
            queuedEvent.RelativeJsonTime = 0f;
            queuedEvent.RecomputeSongBpmTime();
        }

        // Shared placement state is reset because SelectionController chooses the first active GLS subtype during paste.
        private static void SetAllGlsEventPlacementsIdle()
        {
            foreach (var placement in Object.FindObjectsByType<GLSEventColorPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                placement.State = PlacementState.Idle;
            }
            foreach (var placement in Object.FindObjectsByType<GLSEventRotationPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                placement.State = PlacementState.Idle;
            }
            foreach (var placement in Object.FindObjectsByType<GLSEventTranslationPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                placement.State = PlacementState.Idle;
            }
            foreach (var placement in Object.FindObjectsByType<GLSEventFloatFXPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                placement.State = PlacementState.Idle;
            }
        }

        // The two concrete group models share axis-lane semantics but retain their production node value types.
        private static BaseEventBoxGroup CreateGroup(TransformNodeKind kind, Axis axis, bool automatic)
        {
            BaseEventBoxGroup group;
            if (kind == TransformNodeKind.Rotation)
            {
                group = new BaseLightRotationEventBoxGroup
                {
                    ID = DiagnosticGroupId,
                    JsonTime = 0f,
                    Boxes =
                    {
                        new BaseLightRotationEventBox
                        {
                            Axis = (int)axis,
                            IsAutomaticAxisLane = automatic,
                            IndexFilter = CreateFilter(),
                            Events = new[]
                            {
                                new BaseLightRotationBase
                                {
                                    Rotation = RotationValue,
                                    Direction = (int)LightRotationDirection.Automatic,
                            Easing = (int)EaseType.Linear
                                }
                            }
                        }
                    }
                };
                ((BaseLightRotationEventBoxGroup)group).NormalizeLoadedEventConflicts();
            }
            else
            {
                group = new BaseLightTranslationEventBoxGroup
                {
                    ID = DiagnosticGroupId,
                    JsonTime = 0f,
                    Boxes =
                    {
                        new BaseLightTranslationEventBox
                        {
                            Axis = (int)axis,
                            IsAutomaticAxisLane = automatic,
                            IndexFilter = CreateFilter(),
                            Events = new[]
                            {
                                new BaseLightTranslationBase
                                {
                                    Translation = TranslationValue,
                            Easing = (int)EaseType.Linear
                                }
                            }
                        }
                    }
                };
                ((BaseLightTranslationEventBoxGroup)group).NormalizeLoadedEventConflicts();
            }

            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            return group;
        }

        // Newly placed nodes use the same values as initial groups so every operation has one physical expected result.
        private static BaseGLSEvent CreateEvent(TransformNodeKind kind) => kind == TransformNodeKind.Rotation
            ? new BaseLightRotationBase
            {
                Rotation = RotationValue,
                Direction = (int)LightRotationDirection.Automatic,
                            Easing = (int)EaseType.Linear
            }
            : new BaseLightTranslationBase
            {
                Translation = TranslationValue,
                            Easing = (int)EaseType.Linear
            };

        // A one-element Division filter targets the registered element zero in each synthetic effect.
        private static BaseIndexFilter CreateFilter() => new()
        {
            Type = (int)IndexFilterType.Division,
            Param0 = 1,
            Param1 = 0
        };

        // A rendered assertion advances the existing cache, so rebuilding on reload cannot make a false positive pass.
        private void AssertRendered(TransformNodeKind kind, Axis axis)
        {
            var songTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(0f);
            if (kind == TransformNodeKind.Rotation)
            {
                rotationEffect.UpdateTime(false, songTime);
                var expected = Quaternion.AngleAxis(RotationValue, AxisVector(axis));
                Assert.That(
                    Quaternion.Angle(rotationTargets[(int)axis].localRotation, expected),
                    Is.LessThan(0.001f),
                    $"The populated automatic {axis} rotation lane was absent from the live effect cache.");
            }
            else
            {
                translationEffect.UpdateTime(false, songTime);
                Assert.That(
                    translationTargets[(int)axis].localPosition[(int)axis],
                    Is.EqualTo(TranslationValue).Within(0.001f),
                    $"The populated automatic {axis} translation lane was absent from the live effect cache.");
            }
        }

        // Unity's Axis enum maps directly to transform components, but rotations require an explicit unit vector.
        private static Vector3 AxisVector(Axis axis) => axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => Vector3.zero
        };

        // Replacement actions can leave a different group instance than the fixture created, so remove by diagnostic identity.
        private static void RemoveDiagnosticGroups()
        {
            var rotationCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupRotationGridContainer>(ObjectType.GLSRotation);
            for (var index = rotationCollection.MapObjects.Count - 1; index >= 0; index--)
            {
                if (rotationCollection.MapObjects[index].ID == DiagnosticGroupId)
                {
                    rotationCollection.SilentRemoveObject(rotationCollection.MapObjects[index]);
                }
            }

            var translationCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupTranslationGridContainer>(ObjectType.GLSTranslation);
            for (var index = translationCollection.MapObjects.Count - 1; index >= 0; index--)
            {
                if (translationCollection.MapObjects[index].ID == DiagnosticGroupId)
                {
                    translationCollection.SilentRemoveObject(translationCollection.MapObjects[index]);
                }
            }
        }
    }
}
