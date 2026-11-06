using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSExtensionEasingPlacementTest : GLSExtensionTestBase
    {
        public enum Kind { Color, Rotation, Translation, FloatFX }

        // Every inner and outer GLS placement must discard remembered easing on a new extension,
        // including queues restored from authored data that bypass the Extension button.
        [Test]
        public void NewExtensionUsesDefaultEasing(
            [Values] Kind kind,
            [Values] bool outer,
            [Values] bool copied,
            [Values(EaseType.None, EaseType.InQuadratic)] EaseType selected)
        {
            var placement = PreparePlacement(kind, outer, out var queued);
            var input = Field<BeatmapEasingsSelectionInputController>(placement, "EasingInputController");
            input.NotifyEasingChanged(selected);
            queued.Easing = (int)selected;
            if (queued is BaseLightColorBase color)
            {
                color.ChromaColorEasing = (int)EaseType.InCubic;
                color.ChromaStrobeEasing = (int)EaseType.OutBounce;
                color.ChromaStrobeColorEasing = (int)EaseType.InQuadratic;
                color.WriteCustom();
            }
            if (copied)
                queued.UsePrevious = 1;
            else
            {
                input.NotifyExtensionChanged(1);
                AssertDefaultEasing(queued);
                Assert.That(input.CurrentExtension, Is.EqualTo(1));
                Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.Linear));
            }

            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);

            var placed = FindPlacedGroup(kind);
            var evt = placed.ReadOnlyBoxes[0].ReadOnlyEvents.Last();
            AssertDefaultEasing(evt);
            Assert.That(evt.UsePrevious, Is.EqualTo(1));
            if (evt is BaseLightColorBase placedColor)
            {
                Assert.That(placedColor.ChromaColorEasing, Is.Null);
                Assert.That(placedColor.ChromaStrobeEasing, Is.Null);
                Assert.That(placedColor.ChromaStrobeColorEasing, Is.Null);
                var json = placedColor.ToJson();
                Assert.That(json["customData"].HasKey("easing"), Is.False);
                Assert.That(json["customData"].HasKey("colorEasing"), Is.False);
            }
        }

        // Selecting a normal node still honors its easing; only Extension placement receives the reset.
        [Test]
        public void NormalPlacementRetainsEasing([Values] Kind kind, [Values] bool outer)
        {
            var placement = PreparePlacement(kind, outer, out var queued);
            queued.Easing = (int)EaseType.InQuadratic;

            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);

            Assert.That(FindPlacedGroup(kind).ReadOnlyBoxes[0].ReadOnlyEvents.Last().Easing,
                Is.EqualTo((int)EaseType.InQuadratic));
        }

        // A user can deliberately override an existing extension through the same command in either view.
        [Test]
        public void ExistingExtensionRetainsExplicitEasing([Values] Kind kind, [Values] bool outer)
        {
            var group = CreateGroup(kind);
            var evt = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            evt.UsePrevious = 1;
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, out _);
            if (!outer)
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;

            var edited = GLSEventEasingCommand.SetEasing(evt, (int)EaseType.InQuadratic);

            Assert.That(edited, Is.Not.Null);
            Assert.That(edited.UsePrevious, Is.EqualTo(1));
            Assert.That(FindPlacedGroup(kind).ReadOnlyBoxes[0].ReadOnlyEvents[0].Easing,
                Is.EqualTo((int)EaseType.InQuadratic));
        }

        // Place nodes through the scene controllers so the test includes their reset callbacks and map
        // insertion.
        private BasePlacement PreparePlacement(Kind kind, bool outer, out BaseGLSEvent queued)
        {
            var type = PlacementType(kind, outer);
            BasePlacement placement;
            if (outer)
            {
                var provider = groupProvider.IdToTracks[0].GetComponent<PlacementProvider>();
                placement = provider.Placements.Single(candidate => candidate.GetType() == type);
                placement.Initialize(provider);
                var group = Field<BaseEventBoxGroup>(placement, "QueuedData");
                group.JsonTime = 8f;
                queued = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            }
            else
            {
                var group = CreateGroup(kind);
                BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, out _);
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
                placement = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(candidate => candidate.GetType() == type);
                placement.Initialize(null);
                queued = Field<BaseGLSEvent>(placement, "QueuedData");
                queued.EventBoxGroupData = group;
                queued.EventBoxData = group.ReadOnlyBoxes[0];
                queued.BoxIndex = 0;
                queued.RelativeJsonTime = 4f;
                queued.RecomputeSongBpmTime();
            }
            // Prior cases share these scene queues, so each normal-placement control starts explicitly non-extension.
            queued.UsePrevious = 0;
            Field<BeatmapEasingsSelectionInputController>(placement, "EasingInputController").NotifyExtensionChanged(0);
            return placement;
        }

        // The four production groups use one populated lane so view and family are the only varying inputs.
        private static BaseEventBoxGroup CreateGroup(Kind kind)
        {
            BaseEventBoxGroup group = kind switch
            {
                Kind.Color => new BaseLightColorEventBoxGroup
                {
                    Boxes = new() { new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase() } } }
                },
                Kind.Rotation => new BaseLightRotationEventBoxGroup
                {
                    Boxes = new() { new BaseLightRotationEventBox { Events = new[] { new BaseLightRotationBase() } } }
                },
                Kind.Translation => new BaseLightTranslationEventBoxGroup
                {
                    Boxes = new() { new BaseLightTranslationEventBox { Events = new[] { new BaseLightTranslationBase() } } }
                },
                _ => new BaseVfxEventEventBoxGroup
                {
                    Boxes = new() { new BaseVfxEventEventBox { Events = new[] { new BaseFxEventFloat() } } }
                }
            };
            group.ID = 0;
            group.JsonTime = 4f;
            group = BeatmapFactory.Clone(group);
            group.RecomputeSongBpmTime();
            return group;
        }

        // Read the group back from its map collection because placement or modification can replace the
        // parent instance.
        private static BaseEventBoxGroup FindPlacedGroup(Kind kind)
        {
            var type = CreateGroup(kind).ObjectType;
            BaseEventBoxGroup result = null;
            BeatmapObjectContainerCollection.GetCollectionForType(type).ForEachObjectBetweenSongBpmTime(
                0f,
                (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(16f),
                (_, obj) =>
                {
                    if (obj is BaseEventBoxGroup group && group.ID == 0)
                        result = group;
                });
            Assert.That(result, Is.Not.Null);
            return result;
        }

        // These adapters keep parameterized tests on the public data fields used by each GLS family.
        private static Type PlacementType(Kind kind, bool outer) => (kind, outer) switch
        {
            (Kind.Color, false) => typeof(GLSEventColorPlacement),
            (Kind.Color, true) => typeof(GLSGroupColorPlacement),
            (Kind.Rotation, false) => typeof(GLSEventRotationPlacement),
            (Kind.Rotation, true) => typeof(GLSGroupRotationPlacement),
            (Kind.Translation, false) => typeof(GLSEventTranslationPlacement),
            (Kind.Translation, true) => typeof(GLSGroupTranslationPlacement),
            (Kind.FloatFX, false) => typeof(GLSEventFloatFXPlacement),
            _ => typeof(GLSGroupFloatFXPlacement)
        };

        // Native zero is Linear; extension defaults must not inherit Instant (-1) or a selected curve.
        private static void AssertDefaultEasing(BaseGLSEvent evt) =>
            Assert.That(evt.Easing, Is.EqualTo((int)EaseType.Linear));
    }
}
