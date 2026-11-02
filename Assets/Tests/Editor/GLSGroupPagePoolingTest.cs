using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSGroupPagePoolingTest : TestBase
    {
        private const int FirstGroupId = 910001;
        private const int SecondGroupId = 910002;
        private const int SlimRotationId = 910003;
        private const int SlimTranslationId = 910004;
        private const int SlimFloatFxId = 910005;
        private const int SlimColorId = 910006;
        private const string FirstPage = "GLS page pool first";
        private const string SecondPage = "GLS page pool second";

        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider provider;
        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;

        protected override EditingMode InitialEditingMode => EditingMode.GLS;

        // Install two equivalent GLS lanes on different pages so only page membership controls materialization.
        [SetUp]
        public void ConfigurePages()
        {
            runtime = UnityEngine.Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            provider = UnityEngine.Object.FindAnyObjectByType<GLSGroupGridProvider>();
            originalTracks = runtime.TrackDefinitions;
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            SetGlsEntries(testTracks, new List<TrackDefinitionGLS>
            {
                CreateTrack(FirstGroupId, FirstPage),
                CreateTrack(SecondGroupId, SecondPage),
                CreateTrack(SlimRotationId, FirstPage, color: false, translation: false, floatFx: false),
                CreateTrack(SlimTranslationId, FirstPage, color: false, rotation: false, floatFx: false),
                CreateTrack(SlimFloatFxId, FirstPage, color: false, rotation: false, translation: false),
                CreateTrack(SlimColorId, FirstPage, rotation: false, translation: false, floatFx: false)
            });
            testTracks.Initialize();
            runtime.TrackDefinitions = testTracks;
            runtime.NotifyTrackDefinitions();
            provider.SetGroupPage(FirstPage);
        }

        // Restore the shared scene definitions after each case so page state cannot leak into unrelated fixtures.
        [TearDown]
        public void RestorePages()
        {
            if (runtime != null && originalTracks != null)
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
            }

            if (testTracks != null)
            {
                UnityEngine.Object.DestroyImmediate(testTracks);
            }
        }

        [TestCase(GlsKind.Color)]
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void RefreshPoolMaterializesOnlyTheSelectedPage(GlsKind kind)
        {
            var first = SpawnGroup(kind, FirstGroupId);
            var second = SpawnGroup(kind, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(first.ObjectType);

            collection.RefreshPool(0f, 20f, true);

            Assert.That(collection.LoadedContainers.ContainsKey(first), Is.True);
            Assert.That(collection.LoadedContainers.ContainsKey(second), Is.False,
                "An inactive GLS page must remain data-only instead of allocating nodes or ribbons.");

            if (kind == GlsKind.Color)
            {
                var inactiveNode = (BaseLightColorBase)second.OrderedEvents[0];
                Assert.That(GLSEventCommon.GetColorTimeline(inactiveNode, 4), Is.Not.Null,
                    "Inactive-page color events must remain available to the global light timeline.");
                Assert.That(collection.LoadedContainers.ContainsKey(second), Is.False,
                    "Timeline indexing must not require an inactive page's ribbon owner or preview nodes.");
            }
        }

        [Test]
        public void SwitchingPagesRebindsThePooledOwnerAndItsGhostsBeforeTheyCanRender()
        {
            var first = SpawnGroup(GlsKind.Color, FirstGroupId);
            var second = SpawnGroup(GlsKind.Color, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(first.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var firstOwner = (GLSGroupContainer)collection.LoadedContainers[first];
            Assert.That(GetPreviewGhosts(firstOwner).Count, Is.EqualTo(1));

            firstOwner.Highlighted = true;
            firstOwner.Dragged = true;
            provider.SetGroupPage(SecondPage);

            Assert.That(collection.LoadedContainers.Count, Is.EqualTo(1));
            Assert.That(collection.LoadedContainers.ContainsKey(first), Is.False);
            Assert.That(collection.LoadedContainers.TryGetValue(second, out var loadedSecond), Is.True);
            var secondOwner = (GLSGroupContainer)loadedSecond;
            Assert.That(secondOwner.transform.parent,
                Is.SameAs(provider.IdToTracks[SecondGroupId].Track.ObjectParentTransform));
            Assert.That(secondOwner.Highlighted, Is.False);
            Assert.That(secondOwner.Dragged, Is.False);
            foreach (var ghost in GetPreviewGhosts(secondOwner))
            {
                Assert.That(ghost.EventBoxGroupData, Is.SameAs(second));
                // Ghosts remain under the owner's shared activation root, whose parent must move to the selected lane.
                Assert.That(ghost.transform.parent.parent,
                    Is.SameAs(provider.IdToTracks[SecondGroupId].Track.ObjectParentTransform));
                Assert.That(ghost.Highlighted, Is.False);
                Assert.That(ghost.Dragged, Is.False);
            }
        }

        [Test]
        public void StillNotOnMyLevelStrobeGroupKeepsInitializedIconsThroughGhostRefresh()
        {
            var json = JSON.Parse(File.ReadAllText("Assets/Tests/Fixtures/StillNotOnMyLevelStrobeGhostFixture.json"));
            var group = V3LightColorEventBoxGroup.GetFromJson(json["lightColorEventBoxGroups"][0]);
            Assert.That(group.JsonTime, Is.EqualTo(180f));
            var strobe = (BaseLightColorBase)group.Boxes[0].Events[0];
            Assert.That(strobe.Frequency, Is.EqualTo(1));
            Assert.That(strobe.ChromaStrobePulseWidth, Is.EqualTo(0.15f));
            // Bind the authored group to this fixture's isolated GLS lane.
            group.ID = FirstGroupId;
            var collection = BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.SpawnObject(group, out _, false, false, true);
            Assert.DoesNotThrow(
                () => collection.RefreshPool(179f, 182f, true),
                "The beat-180 strobe group must create its preview ghost without losing icon initialization.");
            var owner = (GLSGroupContainer)collection.LoadedContainers[group];
            var ghost = GetPreviewGhosts(owner).Single();
            Assert.That(owner.GetComponentsInChildren<GLSStrobeIconRenderer>(true), Has.Length.EqualTo(2));
            Assert.That(ghost.GetComponentsInChildren<GLSStrobeIconRenderer>(true), Has.Length.EqualTo(2));
            var cameraObject = new GameObject("Strobe ghost regression camera", typeof(Camera));
            var target = new RenderTexture(32, 32, 24);
            try
            {
                Assert.DoesNotThrow(
                    () => ghost.SetIcons(strobe),
                    "An edited preview must be able to show the authored strobe icon.");
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.targetTexture = target;
                camera.Render();
                collection.RefreshPool(179f, 182f, true);
                Assert.That(GetPreviewGhosts(owner).Single(), Is.SameAs(ghost));
                camera.Render();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void RemovingAllPagesUnloadsMaterializedGroups()
        {
            var group = SpawnGroup(GlsKind.Color, FirstGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            Assert.That(collection.LoadedContainers.ContainsKey(group), Is.True);

            SetGlsEntries(testTracks, new List<TrackDefinitionGLS>());
            testTracks.Initialize();
            runtime.NotifyTrackDefinitions();

            Assert.That(collection.LoadedContainers.Count, Is.EqualTo(0));
            Assert.That(provider.ActiveGlsTrackIds, Is.Empty);
        }

        [TestCase(GlsKind.Color)]
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void ScrollingShowsEveryGlsNodeBeforeRibbonWork(GlsKind kind)
        {
            var group = SpawnGroup(kind, FirstGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);

            try
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
                collection.RefreshPool(0f, 20f, true);

                var owner = (GLSGroupContainer)collection.LoadedContainers[group];
                Assert.That(owner.gameObject.activeInHierarchy, Is.True,
                    "The first node must be visible in the scrub frame.");
                Assert.That(owner.PreviewEventData, Is.SameAs(group.OrderedEvents[0]));
                var ghosts = GetPreviewGhosts(owner);
                Assert.That(ghosts, Has.Count.EqualTo(1));
                Assert.That(ghosts[0].gameObject.activeInHierarchy, Is.True,
                    "Later GLS nodes must appear in the same scrub frame, before ribbon work runs.");
                Assert.That(ghosts[0].PreviewEventData, Is.SameAs(group.OrderedEvents[1]));
            }
            finally
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            }
        }

        [Test]
        public void ColdScrubConfiguresRetainedColorSourceImmediately()
        {
            var source = SpawnColorGroup(FirstGroupId, 0f, 0f, 40f);
            ((BaseLightColorBase)source.OrderedEvents[1]).Easing = (int)EaseType.Linear;
            ((BaseLightColorBase)source.OrderedEvents[1]).Brightness = 0.5f;
            GLSEventCommon.AddColorTransitionGroup(source);
            GLSEventCommon.SetColorTransitionLightCount(source.ID, 4);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);

            try
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
                collection.RefreshPool(19f, 25f);

                Assert.That(collection.LoadedContainers.TryGetValue(source, out var loaded), Is.True,
                    "The source group must be retained for its crossing color ribbon.");
                var owner = (GLSGroupContainer)loaded;
                Assert.That(owner.PreviewEventData, Is.SameAs(source.OrderedEvents[0]),
                    "A cold scrub must bind the retained node before the refresh returns.");
                Assert.That(owner.gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            }
        }

        [Test]
        public void RapidScrubUsesTheLatestWindowWhileAReboundOwnerIsStillQueued()
        {
            var initial = SpawnColorGroup(FirstGroupId, 0f, 0f, 1f);
            var spanning = SpawnColorGroup(FirstGroupId, 20f, 0f, 40f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.RefreshPool(0f, 5f, true);
            Assert.That(collection.LoadedContainers.ContainsKey(initial), Is.True);

            // The first automatic scrub rebinds an owner and processes only its root-hide work unit.
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
            collection.RefreshPool(19f, 25f);
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            InvokePrivate(collection, "ProcessNextPreviewConfiguration");

            // A second scrub keeps that parent for its far-offset node and must supersede the queued first window.
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
            collection.RefreshPool(55f, 65f);
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            for (var step = 0; step < 20; step++)
            {
                InvokePrivate(collection, "ProcessNextPreviewConfiguration");
            }

            Assert.That(collection.LoadedContainers.TryGetValue(spanning, out var loaded), Is.True);
            var owner = (GLSGroupContainer)loaded;
            Assert.That(GetPreviewGhosts(owner).Any(ghost =>
                    Mathf.Approximately(ghost.PreviewEventData.RelativeJsonTime, 40f)),
                Is.True,
                "The queued preview request must use the latest scrub window without waiting for play/pause.");
        }

        [Test]
        public void RebindingAnOwnerDoesNotPositionResetGhostSlotsBeforeConfiguration()
        {
            var first = SpawnColorGroup(FirstGroupId, 0f, 0f, 1f);
            var replacement = SpawnColorGroup(FirstGroupId, 40f, 0f, 1f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.RefreshPool(0f, 5f, true);
            var owner = (GLSGroupContainer)collection.LoadedContainers[first];
            var ghost = GetPreviewGhosts(owner).Single();

            // Map/pool teardown can reset a cached child before its collection owner is rebound on the next refresh.
            ghost.ResetForPool();
            owner.ResetForPool();
            owner.ObjectData = replacement;

            Assert.DoesNotThrow(owner.UpdateGridPosition,
                "A collection owner must not position cached child slots until configuration rebinds their event data.");
        }

        [Test]
        public void ResetForPoolClearsEveryExternallyVisibleOwnerState()
        {
            var group = SpawnGroup(GlsKind.Color, FirstGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var owner = (GLSGroupContainer)collection.LoadedContainers[group];
            var cachedGhost = GetPreviewGhosts(owner).Single();
            var alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
            var translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

            owner.Selected = true;
            owner.Highlighted = true;
            owner.Dragged = true;
            owner.GlsLightCount = 91;
            owner.lightGradientController.SetVisible(true);
            owner.IncomingLightGradientController.SetVisible(true);
            owner.MpbController.Mpb.SetFloat(alwaysTranslucent, 1f);
            owner.MpbController.Mpb.SetFloat(translucentAlpha, 0.2f);
            SetPrivateField(owner, "isPreviewGhost", true);

            // Reproduce the collection lifecycle: ObjectData is cleared before the type-specific reset runs.
            owner.ObjectData = null;
            owner.ResetForPool();

            Assert.That(GetPrivateField<bool>(owner, "isPreviewGhost"), Is.False,
                "A collection-owned primary must never retain a preview-ghost role.");
            Assert.That(owner.MpbController.Mpb.GetFloat(alwaysTranslucent), Is.Zero,
                "A recycled primary must clear the shader's forced-translucency branch.");
            Assert.That(owner.MpbController.Mpb.GetFloat(translucentAlpha), Is.EqualTo(1f));
            Assert.That(owner.lightGradientController.gameObject.activeSelf, Is.False);
            Assert.That(owner.IncomingLightGradientController.gameObject.activeSelf, Is.False);
            Assert.That(GetPrivateField<Transform>(owner, "previewGhostRoot").gameObject.activeSelf, Is.False,
                "The separately-parented preview root must stop rendering as soon as its owner enters the pool.");
            Assert.That(owner.Selected, Is.False);
            Assert.That(owner.Highlighted, Is.False);
            Assert.That(owner.Dragged, Is.False);
            Assert.That(owner.GlsLightCount, Is.Zero);
            Assert.That(owner.EventBoxGroupData, Is.Null);
            Assert.That(owner.PreviewEventData, Is.Null);
            Assert.That(owner.DragTarget, Is.SameAs(owner));
            Assert.That(owner.ProcessPreviewNodeConfigurationStep(), Is.True,
                "No abandoned preview configuration may survive a pool reset.");
            Assert.That(GetPreviewGhosts(owner), Has.Count.EqualTo(1),
                "Resetting transient state must preserve the expensive preview capacity.");
            Assert.That(GetPreviewGhosts(owner).Single(), Is.SameAs(cachedGhost));
            Assert.That(GetPrivateField<bool>(owner, "reusePreviewCapacityOnNextConfigure"), Is.True,
                "The next owner should reuse its inactive preview slot instead of allocating another Unity object.");
        }

        [TestCase(GlsKind.Color)]
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void RecycledPreviewNodesReceiveTheirRoleOnEveryBind(GlsKind kind)
        {
            var first = SpawnGroup(kind, FirstGroupId);
            var second = SpawnGroup(kind, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(first.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var firstOwner = (GLSGroupContainer)collection.LoadedContainers[first];
            var firstGhost = GetPreviewGhosts(firstOwner).Single();
            var alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
            var translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

            // Set stale ghost flags and material values that rebinding must clear.
            SetPrivateField(firstOwner, "isPreviewGhost", true);
            firstOwner.MpbController.Mpb.SetFloat(alwaysTranslucent, 1f);
            firstOwner.MpbController.Mpb.SetFloat(translucentAlpha, 0.2f);
            SetPrivateField(firstGhost, "isPreviewGhost", false);
            firstGhost.MpbController.Mpb.SetFloat(alwaysTranslucent, 0f);
            firstGhost.MpbController.Mpb.SetFloat(translucentAlpha, 1f);

            // Rebind this exact cached owner so unrelated fixture pool depth cannot select a different valid container.
            firstOwner.ObjectData = null;
            firstOwner.ResetForPool();
            firstOwner.ObjectData = second;
            firstOwner.GlsLightCount = 17;
            firstOwner.ConfigurePreviewNodes(_ => false);

            var secondOwner = firstOwner;
            var secondGhost = GetPreviewGhosts(secondOwner).Single();
            Assert.That(secondOwner, Is.SameAs(firstOwner));
            Assert.That(secondGhost, Is.SameAs(firstGhost));
            Assert.That(GetPrivateField<bool>(secondOwner, "isPreviewGhost"), Is.False);
            Assert.That(GetPrivateField<bool>(secondGhost, "isPreviewGhost"), Is.True);
            Assert.That(secondOwner.MpbController.Mpb.GetFloat(alwaysTranslucent), Is.Zero);
            Assert.That(secondOwner.MpbController.Mpb.GetFloat(translucentAlpha), Is.EqualTo(1f));
            Assert.That(secondGhost.MpbController.Mpb.GetFloat(alwaysTranslucent), Is.EqualTo(1f));
            Assert.That(secondGhost.MpbController.Mpb.GetFloat(translucentAlpha),
                Is.EqualTo(Mathf.Clamp01(Settings.Instance.GLSOuterTrackGhostNodeOpacity)));
            Assert.That(secondOwner.PreviewEventData, Is.SameAs(second.OrderedEvents[0]));
            Assert.That(secondGhost.PreviewEventData, Is.SameAs(second.OrderedEvents[1]));
            Assert.That(secondOwner.Selected, Is.False);
            Assert.That(secondOwner.Highlighted, Is.False);
            Assert.That(secondOwner.Dragged, Is.False);
            Assert.That(secondGhost.Selected, Is.False);
            Assert.That(secondGhost.Highlighted, Is.False);
            Assert.That(secondGhost.Dragged, Is.False);
        }

        // A pooled ghost must follow its new owner's lane height rather than retain the previous group's
        // offset.
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void RecycledGhostsFollowTheirOwnerAcrossLaneOffsets(GlsKind kind)
        {
            var wideGroup = SpawnGroup(kind, FirstGroupId, 4f);
            var slimGroup = SpawnGroup(kind, SlimTrackIdFor(kind), 40f);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(wideGroup.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var wideOwner = (GLSGroupContainer)collection.LoadedContainers[wideGroup];
            var wideLaneX = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(testTracks, wideGroup);
            Assert.That(wideLaneX, Is.GreaterThan(0.5f));
            Assert.That(wideOwner.transform.localPosition.x, Is.EqualTo(wideLaneX));
            Assert.That(GetPreviewGhosts(wideOwner), Has.Count.EqualTo(1));
            Assert.That(GetPreviewGhosts(wideOwner)[0].transform.localPosition.x, Is.EqualTo(wideLaneX),
                "The first binding must already place the ghost in its owner's lane.");

            // Scrolling past the first group pushes its pooled owner onto a group on a shallower lane track.
            collection.RefreshPool(35f, 50f, true);

            Assert.That(collection.LoadedContainers.TryGetValue(slimGroup, out var loadedSlim), Is.True);
            var slimOwner = (GLSGroupContainer)loadedSlim;
            Assert.That(slimOwner, Is.SameAs(wideOwner),
                "The scroll must reuse the pooled body for the stale-lane regression to apply.");
            var slimLaneX = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(testTracks, slimGroup);
            Assert.That(slimLaneX, Is.EqualTo(0.5f),
                "The slim track must place its only category in the first lane for this repro.");
            Assert.That(slimOwner.transform.localPosition.x, Is.EqualTo(slimLaneX));
            Assert.That(GetPreviewGhosts(slimOwner), Has.Count.EqualTo(1));
            foreach (var ghost in GetPreviewGhosts(slimOwner))
            {
                Assert.That(ghost.EventBoxGroupData, Is.SameAs(slimGroup));
                Assert.That(ghost.transform.localPosition.x, Is.EqualTo(slimLaneX),
                    "A rebound ghost must render in its owner's lane instead of the recycled lane offset.");
                Assert.That(ghost.transform.parent.parent,
                    Is.SameAs(provider.IdToTracks[SlimTrackIdFor(kind)].Track.ObjectParentTransform));
            }
        }

        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void RecycledGhostsLeaveTheFirstLaneWhenTheirOwnerMovesDeeper(GlsKind kind)
        {
            var slimGroup = SpawnGroup(kind, SlimTrackIdFor(kind), 4f);
            var wideGroup = SpawnGroup(kind, FirstGroupId, 40f);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(slimGroup.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var slimOwner = (GLSGroupContainer)collection.LoadedContainers[slimGroup];
            Assert.That(slimOwner.transform.localPosition.x, Is.EqualTo(0.5f));
            Assert.That(GetPreviewGhosts(slimOwner), Has.Count.EqualTo(1));

            collection.RefreshPool(35f, 50f, true);

            Assert.That(collection.LoadedContainers.TryGetValue(wideGroup, out var loadedWide), Is.True);
            var wideOwner = (GLSGroupContainer)loadedWide;
            var wideLaneX = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(testTracks, wideGroup);
            Assert.That(wideLaneX, Is.GreaterThan(0.5f),
                "The full track must place this category deeper than the color lane for this repro.");
            Assert.That(wideOwner.transform.localPosition.x, Is.EqualTo(wideLaneX));
            Assert.That(GetPreviewGhosts(wideOwner), Has.Count.EqualTo(1));
            foreach (var ghost in GetPreviewGhosts(wideOwner))
            {
                Assert.That(ghost.EventBoxGroupData, Is.SameAs(wideGroup));
                Assert.That(ghost.transform.localPosition.x, Is.EqualTo(wideLaneX),
                    "A ghost bound on a lane-zero track must not remain in the color lane after rebind.");
            }
        }

        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void PageSwitchRebindsGhostLanesWithTheirOwner(GlsKind kind)
        {
            var slimGroup = SpawnGroup(kind, SlimTrackIdFor(kind), 4f);
            var wideGroup = SpawnGroup(kind, SecondGroupId, 4f);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(slimGroup.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            Assert.That(collection.LoadedContainers.ContainsKey(slimGroup), Is.True);
            Assert.That(collection.LoadedContainers.ContainsKey(wideGroup), Is.False);

            provider.SetGroupPage(SecondPage);

            Assert.That(collection.LoadedContainers.TryGetValue(wideGroup, out var loadedWide), Is.True);
            var wideOwner = (GLSGroupContainer)loadedWide;
            var wideLaneX = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(testTracks, wideGroup);
            Assert.That(wideOwner.transform.localPosition.x, Is.EqualTo(wideLaneX));
            foreach (var ghost in GetPreviewGhosts(wideOwner))
            {
                Assert.That(ghost.EventBoxGroupData, Is.SameAs(wideGroup));
                Assert.That(ghost.transform.localPosition.x, Is.EqualTo(wideLaneX),
                    "A page switch must move bound ghosts into the new owner's lane.");
                Assert.That(ghost.transform.parent.parent,
                    Is.SameAs(provider.IdToTracks[SecondGroupId].Track.ObjectParentTransform));
            }
        }

        // Voidwalkers uses color, rotation, translation, and FloatFX groups with ID 10 at beat 253.5. Sharing
        // an ID must not put them in each other's lanes.
        [Test]
        public void SameIdGroupsStayInTheirTypeLanesAcrossPoolChurn()
        {
            var color = SpawnGroup(GlsKind.Color, FirstGroupId, 4f);
            var rotation = SpawnGroup(GlsKind.Rotation, FirstGroupId, 4f);
            var translation = SpawnGroup(GlsKind.Translation, FirstGroupId, 4f);
            var vfx = SpawnGroup(GlsKind.FloatFX, FirstGroupId, 4f);
            var slimColor = SpawnGroup(GlsKind.Color, SlimColorId, 40f);
            var slimRotation = SpawnGroup(GlsKind.Rotation, SlimRotationId, 40f);
            var slimTranslation = SpawnGroup(GlsKind.Translation, SlimTranslationId, 40f);
            var slimVfx = SpawnGroup(GlsKind.FloatFX, SlimFloatFxId, 40f);

            var colorCollection = BeatmapObjectContainerCollection.GetCollectionForType(color.ObjectType);
            var rotationCollection = BeatmapObjectContainerCollection.GetCollectionForType(rotation.ObjectType);
            var translationCollection = BeatmapObjectContainerCollection.GetCollectionForType(translation.ObjectType);
            var vfxCollection = BeatmapObjectContainerCollection.GetCollectionForType(vfx.ObjectType);

            // Churn every pool across lane offsets in both directions, then verify the lane after each pass.
            for (var pass = 0; pass < 2; pass++)
            {
                colorCollection.RefreshPool(0f, 20f, true);
                rotationCollection.RefreshPool(0f, 20f, true);
                translationCollection.RefreshPool(0f, 20f, true);
                vfxCollection.RefreshPool(0f, 20f, true);

                AssertLane(colorCollection, color, 0.5f);
                AssertLane(rotationCollection, rotation, 1.5f);
                AssertLane(translationCollection, translation, 2.5f);
                AssertLane(vfxCollection, vfx, 3.5f);

                colorCollection.RefreshPool(35f, 50f, true);
                rotationCollection.RefreshPool(35f, 50f, true);
                translationCollection.RefreshPool(35f, 50f, true);
                vfxCollection.RefreshPool(35f, 50f, true);

                AssertLane(colorCollection, slimColor, 0.5f);
                AssertLane(rotationCollection, slimRotation, 0.5f);
                AssertLane(translationCollection, slimTranslation, 0.5f);
                AssertLane(vfxCollection, slimVfx, 0.5f);
            }
        }

        [Test]
        public void LaneOffsetsFollowTheGroupRuntimeType()
        {
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseLightColorEventBoxGroup { ID = FirstGroupId }),
                Is.EqualTo(0f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseLightRotationEventBoxGroup { ID = FirstGroupId }),
                Is.EqualTo(1f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseLightTranslationEventBoxGroup { ID = FirstGroupId }),
                Is.EqualTo(2f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseVfxEventEventBoxGroup { ID = FirstGroupId }),
                Is.EqualTo(3f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseLightRotationEventBoxGroup { ID = SlimRotationId }),
                Is.EqualTo(0f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseLightTranslationEventBoxGroup { ID = SlimTranslationId }),
                Is.EqualTo(0f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseVfxEventEventBoxGroup { ID = SlimFloatFxId }),
                Is.EqualTo(0f));
            Assert.That(
                GLSGroupContainer.GetPositionFromTrackDefinition(
                    testTracks,
                    new BaseLightColorEventBoxGroup { ID = SlimRotationId }),
                Is.EqualTo(-1f),
                "A group whose type has no enabled category must not be assigned a lane offset.");
        }

        private void AssertLane(
            BeatmapObjectContainerCollection collection,
            BaseEventBoxGroup group,
            float expectedLaneX)
        {
            Assert.That(collection.LoadedContainers.TryGetValue(group, out var loaded), Is.True);
            var owner = (GLSGroupContainer)loaded;
            Assert.That(owner.transform.localPosition.x, Is.EqualTo(expectedLaneX),
                $"{group.GetType().Name} id={group.ID} must render at its own lane offset.");
            Assert.That(owner.transform.parent,
                Is.SameAs(provider.IdToTracks[group.ID].Track.ObjectParentTransform));
            foreach (var ghost in GetPreviewGhosts(owner))
            {
                Assert.That(ghost.EventBoxGroupData, Is.SameAs(group));
                Assert.That(ghost.transform.localPosition.x, Is.EqualTo(expectedLaneX),
                    $"{group.GetType().Name} id={group.ID} ghost must stay in its owner's lane.");
            }
        }

        [Test]
        public void QueuedPreviewConfigurationsPrioritizeEarlierGroups()
        {
            var earlier = SpawnColorGroup(FirstGroupId, 4f, 0f, 1f);
            var later = SpawnColorGroup(FirstGroupId, 12f, 0f, 1f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.RefreshPool(0f, 20f, true);
            var earlierOwner = (GLSGroupContainer)collection.LoadedContainers[earlier];
            var laterOwner = (GLSGroupContainer)collection.LoadedContainers[later];

            InvokePrivate(collection, "CancelPendingPreviewConfigurations");
            // A forced appearance refresh dirties both already-visible ribbon owners without delaying either node.
            InvokePrivate(collection, "SchedulePreviewConfiguration", laterOwner, true);
            InvokePrivate(collection, "SchedulePreviewConfiguration", earlierOwner, true);

            Assert.That(collection.NextPendingPreviewConfiguration.EventBoxGroupData, Is.SameAs(earlier),
                "The next visible node must be chosen by ascending song time, not queue insertion order.");
        }

        [Test]
        public void PreviewSchedulerBatchesCheapWorkUnits()
        {
            var originalVSync = QualitySettings.vSyncCount;
            var originalTargetFrameRate = Application.targetFrameRate;
            try
            {
                // Give this deterministic direct invocation a generous frame deadline instead of inheriting test-runner work.
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 10;
                var group = SpawnGroup(GlsKind.Rotation, FirstGroupId);
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupRotationGridContainer>(group.ObjectType);
                collection.RefreshPool(0f, 20f, true);
                var owner = (GLSGroupContainer)collection.LoadedContainers[group];

                InvokePrivate(collection, "CancelPendingPreviewConfigurations");
                SetPrivateField(owner, "configuredPrimaryPreviewEvent", null);
                InvokePrivate(collection, "SchedulePreviewConfiguration", owner, false);
                InvokePrivate(collection, "ProcessNextPreviewConfiguration");

                Assert.That(collection.NextPendingPreviewConfiguration, Is.Null,
                    "Rotation nodes must already be complete and must not occupy the color ribbon queue.");
            }
            finally
            {
                QualitySettings.vSyncCount = originalVSync;
                Application.targetFrameRate = originalTargetFrameRate;
            }
        }

        [Test]
        public void PreviewSchedulerUsesTheConfiguredFrameDeadline()
        {
            var originalVSync = QualitySettings.vSyncCount;
            var originalTargetFrameRate = Application.targetFrameRate;
            try
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 1000;
                // More than one frame's maximum work units makes the bound deterministic across fast and slow test hosts.
                var offsets = Enumerable.Range(0, 32).Select(index => index * 0.25f).ToArray();
                var group = SpawnColorGroup(FirstGroupId, 4f, offsets);
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupColorGridContainer>(group.ObjectType);
                collection.RefreshPool(0f, 20f, true);
                var owner = (GLSGroupContainer)collection.LoadedContainers[group];

                InvokePrivate(collection, "CancelPendingPreviewConfigurations");
                // Force every color node to request a ribbon so one frame cannot drain the capped queue.
                InvokePrivate(collection, "SchedulePreviewConfiguration", owner, true);
                Assert.That(collection.NextPendingPreviewConfiguration, Is.SameAs(owner));
                InvokePrivate(collection, "ProcessNextPreviewConfiguration");

                Assert.That(collection.NextPendingPreviewConfiguration, Is.SameAs(owner),
                    "One frame must leave later ribbons queued after its bounded upload slice.");
            }
            finally
            {
                QualitySettings.vSyncCount = originalVSync;
                Application.targetFrameRate = originalTargetFrameRate;
            }
        }

        // Unsubscribing must remove only this collection's callbacks. Detached collections must stop
        // responding to ghost-scale changes, while other observers keep receiving them.
        [Test]
        public void UnsubscribedCollectionStopsApplyingShrinkAndKeepsForeignObservers()
        {
            var originalOpacity = Settings.Instance.GLSOuterTrackGhostNodeOpacity;
            var originalShrink = Settings.Instance.GLSInnerEventPreviewShrink;
            var opacityKey = nameof(Settings.GLSOuterTrackGhostNodeOpacity);
            var shrinkKey = nameof(Settings.GLSInnerEventPreviewShrink);
            var opacityObserverInvoked = false;
            Action<object> opacityObserver = _ => opacityObserverInvoked = true;
            var scaleBefore = Vector3.zero;
            var scaleAfterNotify = Vector3.zero;
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            var unsubscribed = false;
            try
            {
                // Zero opacity disables ghost creation entirely, so guarantee a live ghost before spawning.
                if (Mathf.Approximately(Settings.Instance.GLSOuterTrackGhostNodeOpacity, 0f))
                {
                    Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.8f;
                }

                var group = SpawnGroup(GlsKind.Color, FirstGroupId);
                collection.RefreshPool(0f, 20f, true);
                var owner = (GLSGroupContainer)collection.LoadedContainers[group];
                var ghostVisuals = GetPreviewGhosts(owner).Single().transform.Find("GLS Preview Node Visuals");
                Assert.That(ghostVisuals, Is.Not.Null, "A bound preview ghost must own its shrinkable visual root.");
                scaleBefore = ghostVisuals.localScale;

                Settings.NotifyBySettingName(opacityKey, opacityObserver);
                InvokePrivate(collection, "UnsubscribeToCallbacks");
                unsubscribed = true;

                // A still-live shrink subscription forces a pool refresh that rewrites every ghost's visual scale.
                Settings.Instance.GLSInnerEventPreviewShrink = originalShrink > 0.5f
                    ? originalShrink - 0.4f
                    : originalShrink + 0.4f;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    shrinkKey,
                    Settings.Instance.GLSInnerEventPreviewShrink);
                Assert.That(collection.LoadedContainers.TryGetValue(group, out var reloaded), Is.True,
                    "The shrink notification must not unload the spawned group.");
                var reloadedGhostVisuals = GetPreviewGhosts((GLSGroupContainer)reloaded).Single()
                    .transform.Find("GLS Preview Node Visuals");
                Assert.That(reloadedGhostVisuals, Is.Not.Null);
                scaleAfterNotify = reloadedGhostVisuals.localScale;

                Settings.ManuallyNotifySettingUpdatedEvent(
                    opacityKey,
                    Settings.Instance.GLSOuterTrackGhostNodeOpacity);
            }
            finally
            {
                // Restore settings and subscriptions even after an assertion fails so later fixtures keep
                // working.
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = originalOpacity;
                Settings.Instance.GLSInnerEventPreviewShrink = originalShrink;
                Settings.StopNotifyingBySettingName(opacityKey, opacityObserver);
                if (unsubscribed)
                {
                    InvokePrivate(collection, "SubscribeToCallbacks");
                }

                collection.RefreshPool(0f, 20f, true);
            }

            Assert.That(scaleAfterNotify.x, Is.EqualTo(scaleBefore.x).Within(0.01f),
                "An unsubscribed collection must not keep applying shrink changes to its loaded ghosts.");
            Assert.That(opacityObserverInvoked, Is.True,
                "Unsubscribing this collection must not clear unrelated observers on the same setting.");
        }

        [Test]
        public void LiveGhostPreviewToggleHidesAndRestoresLoadedGhosts()
        {
            var enableField = typeof(Settings).GetField("EnableGLSGhostPreview");
            Assert.NotNull(enableField, "The ghost-preview toggle needs its EnableGLSGhostPreview setting.");
            var collidersField = typeof(ObjectContainer).GetField(
                "Colliders", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(collidersField);
            var originalEnabled = (bool)enableField.GetValue(Settings.Instance);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            try
            {
                enableField.SetValue(Settings.Instance, true);
                var group = SpawnGroup(GlsKind.Color, FirstGroupId);
                collection.RefreshPool(0f, 20f, true);
                var owner = (GLSGroupContainer)collection.LoadedContainers[group];
                var ghost = GetPreviewGhosts(owner).Single();
                var ghostColliders = (List<IntersectionCollider>)collidersField.GetValue(ghost);
                Assert.That(ghostColliders, Is.Not.Empty);
                var visualRoot = ghost.transform.Find("GLS Preview Node Visuals");
                Assert.That(visualRoot, Is.Not.Null);
                Assert.That(visualRoot.gameObject.activeSelf, Is.True);
                Assert.That(ghostColliders.All(collider => collider.enabled), Is.True);

                enableField.SetValue(Settings.Instance, false);
                Settings.ManuallyNotifySettingUpdatedEvent("EnableGLSGhostPreview", false);

                Assert.That(collection.LoadedContainers[group], Is.SameAs(owner),
                    "The toggle must refresh loaded groups without respawning them.");
                Assert.That(GetPreviewGhosts(owner).Single(), Is.SameAs(ghost),
                    "The live refresh must rebind the existing ghost node, not respawn it.");
                Assert.That(ghost.gameObject.activeSelf, Is.True);
                Assert.That(ghost.PreviewEventData, Is.Not.Null);
                Assert.That(visualRoot.gameObject.activeSelf, Is.False,
                    "The named notification must hide bound ghost visuals immediately.");
                Assert.That(ghostColliders.All(collider => !collider.enabled), Is.True,
                    "The named notification must disable bound ghost hit-test colliders immediately.");

                enableField.SetValue(Settings.Instance, true);
                Settings.ManuallyNotifySettingUpdatedEvent("EnableGLSGhostPreview", true);

                Assert.That(collection.LoadedContainers[group], Is.SameAs(owner));
                Assert.That(GetPreviewGhosts(owner).Single(), Is.SameAs(ghost));
                Assert.That(visualRoot.gameObject.activeSelf, Is.True);
                Assert.That(ghostColliders.All(collider => collider.enabled), Is.True);
            }
            finally
            {
                enableField.SetValue(Settings.Instance, originalEnabled);
                Settings.ManuallyNotifySettingUpdatedEvent("EnableGLSGhostPreview", originalEnabled);
                collection.RefreshPool(0f, 20f, true);
            }
        }

        private static TrackDefinitionGLS CreateTrack(
            int id,
            string page,
            bool color = true,
            bool rotation = true,
            bool translation = true,
            bool floatFx = true) => new()
        {
            ID = id,
            Group = page,
            Name = page,
            ColorTrack = color,
            RotationTracks = new[] { rotation, rotation, rotation },
            TranslationTracks = new[] { translation, translation, translation },
            FloatFXTrack = floatFx
        };

        private static int SlimTrackIdFor(GlsKind kind) => kind switch
        {
            GlsKind.Rotation => SlimRotationId,
            GlsKind.Translation => SlimTranslationId,
            GlsKind.FloatFX => SlimFloatFxId,
            _ => SlimColorId
        };

        private static BaseEventBoxGroup SpawnGroup(GlsKind kind, int id, float beat = 4f)
        {
            BaseEventBoxGroup group = kind switch
            {
                GlsKind.Color => new BaseLightColorEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightColorEventBox
                        {
                            Events = new[]
                            {
                                new BaseLightColorBase { RelativeJsonTime = 0f, Brightness = 1f },
                                new BaseLightColorBase { RelativeJsonTime = 1f, Brightness = 1f }
                            }
                        }
                    }
                },
                GlsKind.Rotation => new BaseLightRotationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightRotationEventBox
                        {
                            Events = new[]
                            {
                                new BaseLightRotationBase { RelativeJsonTime = 0f },
                                new BaseLightRotationBase { RelativeJsonTime = 1f }
                            }
                        }
                    }
                },
                GlsKind.Translation => new BaseLightTranslationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightTranslationEventBox
                        {
                            Events = new[]
                            {
                                new BaseLightTranslationBase { RelativeJsonTime = 0f },
                                new BaseLightTranslationBase { RelativeJsonTime = 1f }
                            }
                        }
                    }
                },
                GlsKind.FloatFX => new BaseVfxEventEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseVfxEventEventBox
                        {
                            Events = new[]
                            {
                                new BaseFxEventFloat { RelativeJsonTime = 0f },
                                new BaseFxEventFloat { RelativeJsonTime = 1f }
                            }
                        }
                    }
                },
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            group.ID = id;
            group.JsonTime = beat;
            Normalize(group);
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, out _, false, false, true);
            return group;
        }

        private static BaseLightColorEventBoxGroup SpawnColorGroup(
            int id,
            float beat,
            params float[] offsets)
        {
            var box = new BaseLightColorEventBox();
            box.SetEvents(offsets.Select(offset => (BaseGLSEvent)new BaseLightColorBase
            {
                RelativeJsonTime = offset,
                Brightness = 1f
            }).ToArray());
            var group = new BaseLightColorEventBoxGroup
            {
                ID = id,
                JsonTime = beat,
                Boxes = { box }
            };
            group.NormalizeLoadedEventConflicts();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, out _, false, false, true);
            return group;
        }

        private static void Normalize(BaseEventBoxGroup group)
        {
            switch (group)
            {
                case BaseLightColorEventBoxGroup color:
                    color.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightRotationEventBoxGroup rotation:
                    rotation.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightTranslationEventBoxGroup translation:
                    translation.NormalizeLoadedEventConflicts();
                    break;
                case BaseVfxEventEventBoxGroup floatFx:
                    floatFx.NormalizeLoadedEventConflicts();
                    break;
            }
        }

        // Read the owner's maintained preview list so assertions do not scan unrelated pooled scene objects.
        private static List<GLSGroupContainer> GetPreviewGhosts(GLSGroupContainer owner)
        {
            var field = typeof(GLSGroupContainer).GetField(
                "previewGhosts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (List<GLSGroupContainer>)field.GetValue(owner);
        }

        // TrackDefinitionsSO serializes its source list privately; populate that production input before Initialize.
        private static void SetGlsEntries(TrackDefinitionsSO tracks, List<TrackDefinitionGLS> entries)
        {
            var field = typeof(TrackDefinitionsSO).GetField(
                "glsEntries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(tracks, entries);
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            var type = target.GetType();
            MethodInfo method = null;
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        public enum GlsKind
        {
            Color,
            Rotation,
            Translation,
            FloatFX
        }
    }
}
